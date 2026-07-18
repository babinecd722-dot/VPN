using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using Npgsql;
using NpgsqlTypes;

namespace EosLobbyScraper;

/// <summary>
/// Headless Fusion matchmaking scraper (Quest EOS).
/// DeviceId login → FindLobbies → LobbyInfo.playerList → Postgres.
/// </summary>
internal static class Program
{
    // From LabFusion.Network.EpicGames.EOSCredentials (Quest LabFusion 1.14.2)
    private const string ProductName = "Fusion";
    private const string ProductVersion = "0.0.1";
    private static readonly string ProductId = Env("EOS_PRODUCT_ID", "29e074d5b4724f3bb01f26b7e33d2582");
    private static readonly string SandboxId = Env("EOS_SANDBOX_ID", "26f32d66d87f4dfeb4a7449b776a41f1");
    private static readonly string DeploymentId = Env("EOS_DEPLOYMENT_ID", "76d456523b2d468dbde74e7ea6ddcd6b");
    private static readonly string ClientId = Env("EOS_CLIENT_ID", "xyza78915hKqxe2TNTavpq2sxBDvJ9AH");
    private static readonly string ClientSecret = Env("EOS_CLIENT_SECRET", "wBPaPmSI7dWUt87+nvs2pp7TeQVFXSDz+/PnSdYDyc0");

    private static readonly string GameName = Env("FUSION_GAME_NAME", "BONELAB");
    private static readonly string PostgresDsn = ResolvePostgresDsn();
    private static readonly int IntervalSec = int.TryParse(Env("SCRAPE_INTERVAL_SEC", "60"), out var s) ? s : 60;
    private static readonly bool Once = Env("SCRAPE_ONCE", "0") == "1";

    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static readonly ConcurrentQueue<Action> MainQueue = new();

    private static string Env(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    private static string ResolvePostgresDsn()
    {
        string raw = Env("POSTGRES_DSN", "");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Set POSTGRES_DSN (postgresql:// or Npgsql key=value).");

        // Accept both URI and key=value forms.
        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            string user = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
            string pass = uri.UserInfo.Contains(':')
                ? Uri.UnescapeDataString(uri.UserInfo[(uri.UserInfo.IndexOf(':') + 1)..])
                : "";
            string db = uri.AbsolutePath.TrimStart('/');
            return new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = db,
                Username = user,
                Password = pass,
                SslMode = SslMode.Prefer,
                Timeout = 8,
            }.ConnectionString;
        }

        return raw;
    }

    private static int Main(string[] args)
    {
        InstallNativeResolver();

        Console.WriteLine($"[scraper] EOS SDK bind → native/libEOSSDK-Linux-Shipping.so");
        Console.WriteLine($"[scraper] game={GameName} interval={IntervalSec}s once={Once}");

        if (!InitEos())
        {
            Console.Error.WriteLine("[scraper] EOS init failed");
            return 2;
        }

        if (!LoginDeviceId())
        {
            Console.Error.WriteLine("[scraper] DeviceId login failed");
            return 3;
        }

        Console.WriteLine($"[scraper] logged in as {_localUser}");

        do
        {
            try
            {
                var players = ScrapeOnce();
                Console.WriteLine($"[scraper] unique players scraped: {players.Count}");
                foreach (var p in players.Take(5))
                    Console.WriteLine($"  sample: {p.Pid} | {p.Name}");
                int written = UpsertPlayers(players);
                Console.WriteLine($"[scraper] postgres inserted new: {written}");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[scraper] cycle error: " + e);
            }

            if (Once) break;
            Pump(IntervalSec);
        } while (true);

        return 0;
    }

    private static void InstallNativeResolver()
    {
        string so = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "native", "libEOSSDK-Linux-Shipping.so"));
        if (!File.Exists(so))
            so = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "native", "libEOSSDK-Linux-Shipping.so"));
        if (!File.Exists(so))
            throw new FileNotFoundException("libEOSSDK-Linux-Shipping.so not found", so);

        NativeLibrary.SetDllImportResolver(typeof(Bindings).Assembly, (name, assembly, path) =>
        {
            if (name != null && name.IndexOf("EOSSDK", StringComparison.OrdinalIgnoreCase) >= 0)
                return NativeLibrary.Load(so);
            return IntPtr.Zero;
        });
    }

    private static bool InitEos()
    {
        var init = new InitializeOptions
        {
            ProductName = ProductName,
            ProductVersion = ProductVersion,
        };
        Result r = PlatformInterface.Initialize(ref init);
        if (r != Result.Success && r != Result.AlreadyConfigured)
        {
            Console.Error.WriteLine($"EOS_Initialize: {r}");
            return false;
        }

        LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Warning);

        var options = new Options
        {
            ProductId = ProductId,
            SandboxId = SandboxId,
            DeploymentId = DeploymentId,
            ClientCredentials = new ClientCredentials
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
            },
            Flags = PlatformFlags.DisableOverlay | PlatformFlags.DisableSocialOverlay,
            TickBudgetInMilliseconds = 0,
        };

        _platform = PlatformInterface.Create(ref options);
        if (_platform == null)
        {
            Console.Error.WriteLine("EOS_Platform_Create returned null");
            return false;
        }

        Console.WriteLine("[scraper] platform created");
        return true;
    }

    private static bool LoginDeviceId()
    {
        var connect = _platform.GetConnectInterface();
        if (connect == null)
        {
            Console.Error.WriteLine("Connect interface null");
            return false;
        }

        // Ensure device credential exists
        bool createDone = false;
        Result createResult = Result.UnexpectedError;
        var createOpts = new CreateDeviceIdOptions { DeviceModel = "EosLobbyScraper-VPS" };
        connect.CreateDeviceId(ref createOpts, null, (ref CreateDeviceIdCallbackInfo data) =>
        {
            createResult = data.ResultCode;
            createDone = true;
        });
        PumpUntil(() => createDone, 30);
        Console.WriteLine($"[scraper] CreateDeviceId: {createResult}");
        if (createResult != Result.Success && createResult != Result.DuplicateNotAllowed)
            return false;

        bool loginDone = false;
        Result loginResult = Result.UnexpectedError;
        ContinuanceToken continuance = null;
        ProductUserId user = null;

        var loginOpts = new LoginOptions
        {
            Credentials = new Credentials
            {
                Type = ExternalCredentialType.DeviceidAccessToken,
                Token = "", // device-id login: empty token
            },
            UserLoginInfo = new UserLoginInfo
            {
                DisplayName = "ScraperBot",
            },
        };

        connect.Login(ref loginOpts, null, (ref LoginCallbackInfo data) =>
        {
            loginResult = data.ResultCode;
            if (data.ResultCode == Result.Success)
                user = data.LocalUserId;
            else if (data.ResultCode == Result.InvalidUser)
                continuance = data.ContinuanceToken;
            loginDone = true;
        });
        PumpUntil(() => loginDone, 30);
        Console.WriteLine($"[scraper] Connect.Login: {loginResult}");

        if (loginResult == Result.InvalidUser && continuance != null)
        {
            bool cuDone = false;
            Result cuResult = Result.UnexpectedError;
            var cuOpts = new CreateUserOptions { ContinuanceToken = continuance };
            connect.CreateUser(ref cuOpts, null, (ref CreateUserCallbackInfo data) =>
            {
                cuResult = data.ResultCode;
                if (data.ResultCode == Result.Success)
                    user = data.LocalUserId;
                cuDone = true;
            });
            PumpUntil(() => cuDone, 30);
            Console.WriteLine($"[scraper] CreateUser: {cuResult}");
            if (cuResult != Result.Success)
                return false;
        }
        else if (loginResult != Result.Success)
        {
            return false;
        }

        _localUser = user;
        return _localUser != null;
    }

    private static List<PlayerRow> ScrapeOnce()
    {
        var lobbyIface = _platform.GetLobbyInterface();
        var players = new Dictionary<string, string>(StringComparer.Ordinal); // pid -> name

        var createOpts = new CreateLobbySearchOptions { MaxResults = 200 };
        Result cr = lobbyIface.CreateLobbySearch(ref createOpts, out LobbySearch search);
        if (cr != Result.Success || search == null)
            throw new Exception($"CreateLobbySearch: {cr}");

        try
        {
            SetEq(search, "HasLobbyOpen", bool.TrueString);
            SetEq(search, "MarrowFusion", bool.TrueString);
            SetEq(search, "Game", GameName);
            // Skip private(1) and locked(3) — same as Fusion EOSMatchmaker
            SetNeq(search, "Privacy", "1");
            SetNeq(search, "Privacy", "3");

            bool findDone = false;
            Result findResult = Result.UnexpectedError;
            var findOpts = new LobbySearchFindOptions { LocalUserId = _localUser };
            search.Find(ref findOpts, null, (ref LobbySearchFindCallbackInfo info) =>
            {
                findResult = info.ResultCode;
                findDone = true;
            });
            PumpUntil(() => findDone, 45);
            Console.WriteLine($"[scraper] LobbySearch.Find: {findResult}");
            if (findResult != Result.Success)
                return new List<PlayerRow>();

            var countOpts = default(LobbySearchGetSearchResultCountOptions);
            uint count = search.GetSearchResultCount(ref countOpts);
            Console.WriteLine($"[scraper] lobbies found: {count}");

            for (uint i = 0; i < count; i++)
            {
                var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success || details == null)
                    continue;
                try
                {
                    ParseLobbyPlayers(details, players);
                }
                finally
                {
                    details.Release();
                }
            }
        }
        finally
        {
            search.Release();
        }

        return players.Select(kv => new PlayerRow(kv.Value, kv.Key)).ToList();
    }

    private static void SetEq(LobbySearch search, string key, string value)
    {
        var p = new LobbySearchSetParameterOptions
        {
            Parameter = new AttributeData { Key = key, Value = value },
            ComparisonOp = ComparisonOp.Equal,
        };
        search.SetParameter(ref p);
    }

    private static void SetNeq(LobbySearch search, string key, string value)
    {
        var p = new LobbySearchSetParameterOptions
        {
            Parameter = new AttributeData { Key = key, Value = value },
            ComparisonOp = ComparisonOp.Notequal,
        };
        search.SetParameter(ref p);
    }

    private static void ParseLobbyPlayers(LobbyDetails details, Dictionary<string, string> players)
    {
        // Prefer LobbyInfo JSON (has playerList). Fallback: enumerate members.
        string lobbyInfoJson = GetAttr(details, "LobbyInfo");
        if (!string.IsNullOrEmpty(lobbyInfoJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(lobbyInfoJson);
                if (doc.RootElement.TryGetProperty("playerList", out var pl) &&
                    pl.TryGetProperty("players", out var arr) &&
                    arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in arr.EnumerateArray())
                    {
                        string pid = null;
                        if (p.TryGetProperty("platformID", out var pidEl))
                            pid = pidEl.ValueKind == JsonValueKind.String ? pidEl.GetString() : pidEl.ToString();
                        if (string.IsNullOrWhiteSpace(pid) || pid == "0") continue;

                        string name = null;
                        if (p.TryGetProperty("nickname", out var nick) && nick.ValueKind == JsonValueKind.String)
                            name = nick.GetString();
                        if (string.IsNullOrWhiteSpace(name) && p.TryGetProperty("username", out var user) && user.ValueKind == JsonValueKind.String)
                            name = user.GetString();
                        name = CleanName(name) ?? ("pid:" + Short(pid));

                        players[pid.Trim()] = name;
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[scraper] LobbyInfo parse: " + e.Message);
            }
        }

        // Fallback: member ProductUserIds (no display names)
        var mcOpts = default(LobbyDetailsGetMemberCountOptions);
        uint members = details.GetMemberCount(ref mcOpts);
        for (uint m = 0; m < members; m++)
        {
            var byIdx = new LobbyDetailsGetMemberByIndexOptions { MemberIndex = m };
            ProductUserId mid = details.GetMemberByIndex(ref byIdx);
            if (mid == null) continue;
            string pid = mid.ToString();
            if (string.IsNullOrWhiteSpace(pid)) continue;
            if (!players.ContainsKey(pid))
                players[pid] = "pid:" + Short(pid);
        }
    }

    private static string GetAttr(LobbyDetails details, string key)
    {
        var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key };
        Result r = details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr);
        if (r != Result.Success || !attr.HasValue) return null;
        try
        {
            AttributeData? data = attr.Value.Data;
            if (!data.HasValue) return null;
            Utf8String asString = data.Value.Value.AsUtf8;
            return asString?.ToString();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[scraper] GetAttr " + key + ": " + e.Message);
            return null;
        }
    }

    private static string CleanName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var sb = new System.Text.StringBuilder(name.Length);
        bool inTag = false;
        foreach (char c in name)
        {
            if (c == '<') { inTag = true; continue; }
            if (c == '>') { inTag = false; continue; }
            if (inTag) continue;
            if (char.IsControl(c)) continue;
            sb.Append(c);
        }
        string s = sb.ToString().Trim();
        if (s.Length == 0) return null;
        return s.Length > 128 ? s.Substring(0, 128) : s;
    }

    private static string Short(string pid) => pid.Length <= 8 ? pid : pid.Substring(0, 8);

    private static int UpsertPlayers(List<PlayerRow> rows)
    {
        if (rows.Count == 0) return 0;

        using var conn = new NpgsqlConnection(PostgresDsn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        string[] pids = rows.Select(r => r.Pid).Distinct().ToArray();
        var existing = new HashSet<string>(StringComparer.Ordinal);
        using (var exists = new NpgsqlCommand("SELECT pid FROM client_data WHERE pid = ANY(@pids)", conn, tx))
        {
            exists.Parameters.AddWithValue("pids", NpgsqlDbType.Array | NpgsqlDbType.Text, pids);
            using var reader = exists.ExecuteReader();
            while (reader.Read()) existing.Add(reader.GetString(0));
        }

        int inserted = 0;
        using (var insert = new NpgsqlCommand(
                   "INSERT INTO client_data (name, pid) VALUES (@name, @pid)", conn, tx))
        {
            var pName = insert.Parameters.Add("name", NpgsqlDbType.Text);
            var pPid = insert.Parameters.Add("pid", NpgsqlDbType.Text);
            insert.Prepare();
            foreach (var row in rows)
            {
                if (existing.Contains(row.Pid)) continue;
                pName.Value = row.Name;
                pPid.Value = row.Pid;
                insert.ExecuteNonQuery();
                existing.Add(row.Pid);
                inserted++;
            }
        }

        tx.Commit();
        return inserted;
    }

    private static void Pump(double seconds)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < until)
        {
            _platform?.Tick();
            while (MainQueue.TryDequeue(out var a))
            {
                try { a(); } catch (Exception e) { Console.Error.WriteLine(e); }
            }
            Thread.Sleep(15);
        }
    }

    private static void PumpUntil(Func<bool> done, int timeoutSec)
    {
        var until = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (!done() && DateTime.UtcNow < until)
        {
            _platform?.Tick();
            Thread.Sleep(15);
        }
        if (!done())
            throw new TimeoutException("EOS callback timed out");
    }

    private readonly record struct PlayerRow(string Name, string Pid);
}
