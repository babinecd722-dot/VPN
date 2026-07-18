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
/// DeviceId → bucketed Find (+ LobbyCode cache) → Postgres presence sync.
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
    private static readonly int IntervalSec = int.TryParse(Env("SCRAPE_INTERVAL_SEC", "15"), out var s) ? Math.Max(5, s) : 15;
    private static readonly bool Once = Env("SCRAPE_ONCE", "0") == "1";
    private static readonly int CodeProbeBudget = int.TryParse(Env("CODE_PROBE_BUDGET", "25"), out var c) ? Math.Clamp(c, 0, 200) : 25;
    private static readonly double LoadingSec = double.TryParse(Env("LOADING_SEC", "3"), out var ls) ? Math.Clamp(ls, 0.5, 30) : 3;
    private static readonly string CodeCachePath = Env("CODE_CACHE_PATH", Path.Combine(AppContext.BaseDirectory, "data", "lobby_codes.txt"));

    private const string StatusOffline = "OFFLINE";
    private const string StatusLoading = "LOADING";
    private const string StatusInGame = "IN GAME";

    // ServerPrivacy: PUBLIC=0 PRIVATE=1 FRIENDS_ONLY=2 LOCKED=3
    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static readonly ConcurrentQueue<Action> MainQueue = new();
    private static readonly object CodeLock = new();
    private static readonly HashSet<string> KnownCodes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Queue<string> CodeProbeQueue = new();
    private static readonly HashSet<string> CodesSeenThisCycle = new(StringComparer.OrdinalIgnoreCase);
    // pid → first time seen in a lobby playerList (cleared on OFFLINE)
    private static readonly Dictionary<string, DateTime> FirstSeenUtc = new(StringComparer.Ordinal);

    private static string Env(string key, string fallback)
        => Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : fallback;

    private static string ResolvePostgresDsn()
    {
        string raw = Env("POSTGRES_DSN", "");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Set POSTGRES_DSN (postgresql:// or Npgsql key=value).");

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
                Timeout = 15,
                CommandTimeout = 30,
                KeepAlive = 30,
                MaxAutoPrepare = 20,
            }.ConnectionString;
        }

        return raw;
    }

    private static int Main(string[] args)
    {
        InstallNativeResolver();
        LoadCodeCache();
        SeedCodesFromEnv();

        Console.WriteLine("[scraper] EOS SDK bind → native/libEOSSDK-Linux-Shipping.so");
        Console.WriteLine($"[scraper] game={GameName} interval={IntervalSec}s once={Once} loading_sec={LoadingSec:0.#} code_cache={KnownCodes.Count} probe_budget={CodeProbeBudget}");

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
                var snapshot = ScrapeOnce();
                Console.WriteLine(
                    $"[scraper] lobbies={snapshot.LobbyCount} players={snapshot.Players.Count} " +
                    $"privacy[pub={snapshot.PrivacyCounts.GetValueOrDefault(0)} priv={snapshot.PrivacyCounts.GetValueOrDefault(1)} " +
                    $"friends={snapshot.PrivacyCounts.GetValueOrDefault(2)} locked={snapshot.PrivacyCounts.GetValueOrDefault(3)}] " +
                    $"codes_cached={KnownCodes.Count}");

                foreach (var p in snapshot.Players.Values.Take(5))
                    Console.WriteLine($"  sample: {p.Pid} | {p.Name} | {p.Server ?? "-"} | {p.ServerMap ?? "-"}");

                var stats = SyncPresence(snapshot.Players);
                Console.WriteLine(
                    $"[scraper] db upserted={stats.Upserted} inserted={stats.Inserted} " +
                    $"loading={stats.Loading} ingame={stats.InGame} " +
                    $"offline={stats.MarkedOffline} unchanged={stats.Unchanged}");
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
                Token = "",
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

    private static ScrapeSnapshot ScrapeOnce()
    {
        var players = new Dictionary<string, PlayerPresence>(StringComparer.Ordinal);
        var privacyCounts = new Dictionary<int, int>();
        var seenLobbyIds = new HashSet<string>(StringComparer.Ordinal);
        int lobbyCount = 0;

        lock (CodeLock)
            CodesSeenThisCycle.Clear();

        // Bucketed searches cover modes Fusion's public browser hides (PRIVATE/LOCKED).
        lobbyCount += RunSearch(players, privacyCounts, seenLobbyIds, "public+friends", excludePrivateAndLocked: true, privacyEq: null, code: null);
        Pump(0.25);
        lobbyCount += RunSearch(players, privacyCounts, seenLobbyIds, "private", excludePrivateAndLocked: false, privacyEq: "1", code: null);
        Pump(0.25);
        try
        {
            lobbyCount += RunSearch(players, privacyCounts, seenLobbyIds, "locked", excludePrivateAndLocked: false, privacyEq: "3", code: null);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[scraper] locked search skipped: " + e.Message);
        }

        // Codes are 8×[A-Z0-9] — not brute-forceable. Re-probe harvested/env codes not already seen.
        foreach (string code in TakeCodeProbes(CodeProbeBudget))
        {
            lobbyCount += RunSearch(players, privacyCounts, seenLobbyIds, "code:" + code, excludePrivateAndLocked: false, privacyEq: null, code: code);
        }

        PersistCodeCache();
        return new ScrapeSnapshot(players, lobbyCount, privacyCounts);
    }

    private static int RunSearch(
        Dictionary<string, PlayerPresence> players,
        Dictionary<int, int> privacyCounts,
        HashSet<string> seenLobbyIds,
        string label,
        bool excludePrivateAndLocked,
        string privacyEq,
        string code)
    {
        var lobbyIface = _platform.GetLobbyInterface();
        uint maxResults = string.IsNullOrEmpty(code) ? 200u : 1u;
        var createOpts = new CreateLobbySearchOptions { MaxResults = maxResults };
        Result cr = lobbyIface.CreateLobbySearch(ref createOpts, out LobbySearch search);
        if (cr != Result.Success || search == null)
        {
            Console.Error.WriteLine($"[scraper] CreateLobbySearch({label}): {cr}");
            return 0;
        }

        int added = 0;
        try
        {
            SetEq(search, "HasLobbyOpen", bool.TrueString);
            SetEq(search, "MarrowFusion", bool.TrueString);
            SetEq(search, "Game", GameName);

            if (!string.IsNullOrWhiteSpace(code))
            {
                SetEq(search, "LobbyCode", code.Trim().ToUpperInvariant());
            }
            else if (!string.IsNullOrEmpty(privacyEq))
            {
                SetEq(search, "Privacy", privacyEq);
            }
            else if (excludePrivateAndLocked)
            {
                SetNeq(search, "Privacy", "1");
                SetNeq(search, "Privacy", "3");
            }

            bool findDone = false;
            Result findResult = Result.UnexpectedError;
            var findOpts = new LobbySearchFindOptions { LocalUserId = _localUser };
            search.Find(ref findOpts, null, (ref LobbySearchFindCallbackInfo info) =>
            {
                findResult = info.ResultCode;
                findDone = true;
            });
            PumpUntil(() => findDone, 45);

            if (findResult != Result.Success)
            {
                if (string.IsNullOrEmpty(code))
                    Console.Error.WriteLine($"[scraper] Find({label}): {findResult}");
                return 0;
            }

            var countOpts = default(LobbySearchGetSearchResultCountOptions);
            uint count = search.GetSearchResultCount(ref countOpts);
            if (string.IsNullOrEmpty(code) || count > 0)
                Console.WriteLine($"[scraper] Find({label}): {findResult} lobbies={count}");

            for (uint i = 0; i < count; i++)
            {
                var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success || details == null)
                    continue;
                try
                {
                    // Avoid LobbyDetails.CopyInfo — native EOS 1.15.5 can SIGSEGV on some Quest lobbies.
                    string lobbyKey = BuildLobbyKey(details, label, i);
                    if (!seenLobbyIds.Add(lobbyKey))
                        continue;
                    added++;
                    ParseLobby(details, players, privacyCounts);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"[scraper] lobby parse ({label}#{i}): {e.Message}");
                }
                finally
                {
                    try { details.Release(); } catch { /* ignore */ }
                }
            }
        }
        finally
        {
            search.Release();
        }

        return added;
    }

    private static string BuildLobbyKey(LobbyDetails details, string label, uint index)
    {
        string code = GetAttr(details, "LobbyCode");
        string privacy = GetAttr(details, "Privacy");
        string host = null;
        string lobbyInfoJson = GetAttr(details, "LobbyInfo");
        if (!string.IsNullOrEmpty(lobbyInfoJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(lobbyInfoJson);
                if (doc.RootElement.TryGetProperty("lobbyID", out var idEl))
                {
                    string id = idEl.ValueKind == JsonValueKind.String ? idEl.GetString() : idEl.ToString();
                    if (!string.IsNullOrWhiteSpace(id) && id != "0")
                        return "id:" + id;
                }
                if (doc.RootElement.TryGetProperty("lobbyHostName", out var hn) && hn.ValueKind == JsonValueKind.String)
                    host = hn.GetString();
            }
            catch
            {
                // fall through
            }
        }

        if (!string.IsNullOrWhiteSpace(code))
            return "code:" + code.Trim().ToUpperInvariant();
        return $"{label}:{privacy}:{host}:{index}";
    }

    private static void ParseLobby(
        LobbyDetails details,
        Dictionary<string, PlayerPresence> players,
        Dictionary<int, int> privacyCounts)
    {
        string lobbyInfoJson = GetAttr(details, "LobbyInfo");
        string server = null;
        string map = null;
        int privacy = -1;
        string code = GetAttr(details, "LobbyCode");

        string privacyRaw = GetAttr(details, "Privacy");
        if (int.TryParse(privacyRaw, out var pAttr))
            privacy = pAttr;

        if (!string.IsNullOrEmpty(lobbyInfoJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(lobbyInfoJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("lobbyName", out var ln) && ln.ValueKind == JsonValueKind.String)
                    server = CleanText(ln.GetString(), 128);
                if (string.IsNullOrWhiteSpace(server) && root.TryGetProperty("lobbyHostName", out var host) && host.ValueKind == JsonValueKind.String)
                    server = CleanText(host.GetString(), 128);

                if (root.TryGetProperty("levelTitle", out var lt) && lt.ValueKind == JsonValueKind.String)
                    map = CleanText(lt.GetString(), 128);
                if (string.IsNullOrWhiteSpace(map) && root.TryGetProperty("levelBarcode", out var lb) && lb.ValueKind == JsonValueKind.String)
                    map = CleanText(lb.GetString(), 128);

                if (root.TryGetProperty("privacy", out var pr))
                {
                    if (pr.ValueKind == JsonValueKind.Number && pr.TryGetInt32(out var pi))
                        privacy = pi;
                    else if (pr.ValueKind == JsonValueKind.String && int.TryParse(pr.GetString(), out var ps))
                        privacy = ps;
                }

                if (root.TryGetProperty("lobbyCode", out var lc) && lc.ValueKind == JsonValueKind.String)
                    code = lc.GetString() ?? code;

                RememberCode(code, seenThisCycle: true);

                if (privacy >= 0)
                    privacyCounts[privacy] = privacyCounts.GetValueOrDefault(privacy) + 1;

                if (root.TryGetProperty("playerList", out var pl) &&
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
                        name = CleanText(name, 128) ?? ("pid:" + Short(pid));

                        UpsertPresence(players, pid.Trim(), name, server, map);
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[scraper] LobbyInfo parse: " + e.Message);
            }
        }

        RememberCode(code, seenThisCycle: true);
        if (privacy >= 0)
            privacyCounts[privacy] = privacyCounts.GetValueOrDefault(privacy) + 1;

        var mcOpts = default(LobbyDetailsGetMemberCountOptions);
        uint members = details.GetMemberCount(ref mcOpts);
        for (uint m = 0; m < members; m++)
        {
            var byIdx = new LobbyDetailsGetMemberByIndexOptions { MemberIndex = m };
            ProductUserId mid = details.GetMemberByIndex(ref byIdx);
            if (mid == null) continue;
            string pid = mid.ToString();
            if (string.IsNullOrWhiteSpace(pid)) continue;
            UpsertPresence(players, pid, "pid:" + Short(pid), server, map);
        }
    }

    private static void UpsertPresence(
        Dictionary<string, PlayerPresence> players,
        string pid,
        string name,
        string server,
        string map)
    {
        if (players.TryGetValue(pid, out var existing))
        {
            if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("pid:", StringComparison.Ordinal))
                existing = existing with { Name = name };
            if (string.IsNullOrWhiteSpace(existing.Server) && !string.IsNullOrWhiteSpace(server))
                existing = existing with { Server = server };
            if (string.IsNullOrWhiteSpace(existing.ServerMap) && !string.IsNullOrWhiteSpace(map))
                existing = existing with { ServerMap = map };
            players[pid] = existing;
            return;
        }

        players[pid] = new PlayerPresence(pid, name, server, map);
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

    private static string CleanText(string text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var sb = new System.Text.StringBuilder(text.Length);
        bool inTag = false;
        foreach (char ch in text)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; continue; }
            if (inTag) continue;
            if (char.IsControl(ch)) continue;
            sb.Append(ch);
        }
        string cleaned = sb.ToString().Trim();
        if (cleaned.Length == 0) return null;
        return cleaned.Length > maxLen ? cleaned.Substring(0, maxLen) : cleaned;
    }

    private static string Short(string pid) => pid.Length <= 8 ? pid : pid.Substring(0, 8);

    private static SyncStats SyncPresence(Dictionary<string, PlayerPresence> online)
    {
        Exception last = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                return SyncPresenceOnce(online);
            }
            catch (Exception e) when (attempt < 3)
            {
                last = e;
                Console.Error.WriteLine($"[scraper] db retry {attempt}/3: {e.Message}");
                Thread.Sleep(400 * attempt);
            }
        }
        throw last ?? new Exception("db sync failed");
    }

    private static string ResolveStatus(string pid, DateTime nowUtc)
    {
        if (!FirstSeenUtc.ContainsKey(pid))
            FirstSeenUtc[pid] = nowUtc;

        double age = (nowUtc - FirstSeenUtc[pid]).TotalSeconds;
        return age < LoadingSec ? StatusLoading : StatusInGame;
    }

    private static SyncStats SyncPresenceOnce(Dictionary<string, PlayerPresence> online)
    {
        var onlineList = online.Values.ToList();
        string[] seenPids = onlineList.Select(p => p.Pid).ToArray();
        var seenSet = new HashSet<string>(seenPids, StringComparer.Ordinal);
        DateTime nowUtc = DateTime.UtcNow;

        // Drop first-seen for anyone who left — next reappear gets LOADING again.
        var stale = FirstSeenUtc.Keys.Where(pid => !seenSet.Contains(pid)).ToList();
        foreach (string pid in stale)
            FirstSeenUtc.Remove(pid);

        var desired = new Dictionary<string, (PlayerPresence P, string Status)>(StringComparer.Ordinal);
        var loadingPids = new List<string>();
        foreach (var p in onlineList)
        {
            string status = ResolveStatus(p.Pid, nowUtc);
            desired[p.Pid] = (p, status);
            if (status == StatusLoading)
                loadingPids.Add(p.Pid);
        }

        SyncStats stats = WritePresence(desired, seenPids);

        // Hold LOADING briefly so DB consumers can see it, then promote to IN GAME.
        if (loadingPids.Count > 0)
        {
            Console.WriteLine($"[scraper] loading→ingame in {LoadingSec:0.#}s for {loadingPids.Count} players");
            Pump(LoadingSec);

            nowUtc = DateTime.UtcNow;
            var promote = new Dictionary<string, (PlayerPresence P, string Status)>(StringComparer.Ordinal);
            foreach (string pid in loadingPids)
            {
                if (!online.TryGetValue(pid, out var p)) continue;
                FirstSeenUtc[pid] = nowUtc.AddSeconds(-LoadingSec - 0.01);
                promote[pid] = (p, StatusInGame);
            }

            if (promote.Count > 0)
            {
                var promoteStats = WritePresence(promote, seenPids, markOffline: false);
                stats = new SyncStats(
                    stats.Upserted + promoteStats.Upserted,
                    stats.Inserted + promoteStats.Inserted,
                    stats.MarkedOffline,
                    stats.Unchanged + promoteStats.Unchanged,
                    Loading: loadingPids.Count,
                    InGame: desired.Count - loadingPids.Count + promote.Count);
            }
            else
            {
                stats = stats with { Loading = loadingPids.Count, InGame = desired.Count - loadingPids.Count };
            }
        }
        else
        {
            stats = stats with { Loading = 0, InGame = desired.Count };
        }

        return stats;
    }

    private static SyncStats WritePresence(
        Dictionary<string, (PlayerPresence P, string Status)> desired,
        string[] seenPids,
        bool markOffline = true)
    {
        using var conn = new NpgsqlConnection(PostgresDsn);
        conn.Open();
        using var tx = conn.BeginTransaction();

        string[] lookupPids = desired.Keys.ToArray();
        var current = new Dictionary<string, DbRow>(StringComparer.Ordinal);
        using (var cmd = new NpgsqlCommand(
                   @"SELECT pid, name, status, server, server_map
                     FROM client_data
                     WHERE pid = ANY(@seen)
                        OR status IN ('ONLINE', 'IN GAME', 'LOADING')", conn, tx))
        {
            cmd.Parameters.AddWithValue(
                "seen",
                NpgsqlDbType.Array | NpgsqlDbType.Text,
                lookupPids.Length == 0 ? Array.Empty<string>() : lookupPids);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string pid = reader.GetString(0);
                current[pid] = new DbRow(
                    pid,
                    reader.GetString(1),
                    reader.IsDBNull(2) ? StatusOffline : reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetString(4));
            }
        }

        int upserted = 0;
        int inserted = 0;
        int unchanged = 0;
        int offlineCount = 0;

        using (var upd = new NpgsqlCommand(
                   @"UPDATE client_data
                     SET name = @name,
                         status = @status,
                         server = @server,
                         server_map = @map
                     WHERE pid = @pid", conn, tx))
        using (var ins = new NpgsqlCommand(
                   @"INSERT INTO client_data (name, pid, status, server, server_map)
                     VALUES (@name, @pid, @status, @server, @map)", conn, tx))
        {
            var uName = upd.Parameters.Add("name", NpgsqlDbType.Text);
            var uStatus = upd.Parameters.Add("status", NpgsqlDbType.Text);
            var uServer = upd.Parameters.Add("server", NpgsqlDbType.Text);
            var uMap = upd.Parameters.Add("map", NpgsqlDbType.Text);
            var uPid = upd.Parameters.Add("pid", NpgsqlDbType.Text);
            upd.Prepare();

            var iName = ins.Parameters.Add("name", NpgsqlDbType.Text);
            var iPid = ins.Parameters.Add("pid", NpgsqlDbType.Text);
            var iStatus = ins.Parameters.Add("status", NpgsqlDbType.Text);
            var iServer = ins.Parameters.Add("server", NpgsqlDbType.Text);
            var iMap = ins.Parameters.Add("map", NpgsqlDbType.Text);
            ins.Prepare();

            foreach (var kv in desired)
            {
                var p = kv.Value.P;
                string status = kv.Value.Status;

                if (current.TryGetValue(p.Pid, out var row))
                {
                    bool same =
                        string.Equals(row.Name, p.Name, StringComparison.Ordinal) &&
                        string.Equals(row.Status, status, StringComparison.Ordinal) &&
                        string.Equals(row.Server ?? "", p.Server ?? "", StringComparison.Ordinal) &&
                        string.Equals(row.ServerMap ?? "", p.ServerMap ?? "", StringComparison.Ordinal);
                    if (same)
                    {
                        unchanged++;
                        continue;
                    }

                    uName.Value = p.Name;
                    uStatus.Value = status;
                    uServer.Value = (object)p.Server ?? DBNull.Value;
                    uMap.Value = (object)p.ServerMap ?? DBNull.Value;
                    uPid.Value = p.Pid;
                    upd.ExecuteNonQuery();
                    upserted++;
                }
                else
                {
                    iName.Value = p.Name;
                    iPid.Value = p.Pid;
                    iStatus.Value = status;
                    iServer.Value = (object)p.Server ?? DBNull.Value;
                    iMap.Value = (object)p.ServerMap ?? DBNull.Value;
                    ins.ExecuteNonQuery();
                    inserted++;
                }
            }
        }

        if (markOffline)
        {
            if (seenPids.Length == 0)
            {
                using (var offAll = new NpgsqlCommand(
                           @"UPDATE client_data
                             SET status = 'OFFLINE', server = NULL, server_map = NULL
                             WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')", conn, tx))
                {
                    offlineCount = offAll.ExecuteNonQuery();
                }
            }
            else
            {
                using (var off = new NpgsqlCommand(
                           @"UPDATE client_data
                             SET status = 'OFFLINE', server = NULL, server_map = NULL
                             WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')
                               AND NOT (pid = ANY(@seen))", conn, tx))
                {
                    off.Parameters.AddWithValue("seen", NpgsqlDbType.Array | NpgsqlDbType.Text, seenPids);
                    offlineCount = off.ExecuteNonQuery();
                }
            }
        }

        tx.Commit();
        return new SyncStats(upserted, inserted, offlineCount, unchanged, 0, 0);
    }

    private static void SeedCodesFromEnv()
    {
        string raw = Env("LOBBY_CODES", "");
        if (string.IsNullOrWhiteSpace(raw)) return;
        foreach (string part in raw.Split(new[] { ',', ';', ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            RememberCode(part, seenThisCycle: false);
    }

    private static void LoadCodeCache()
    {
        try
        {
            if (!File.Exists(CodeCachePath)) return;
            foreach (string line in File.ReadAllLines(CodeCachePath))
            {
                string code = NormalizeCode(line);
                if (code == null) continue;
                lock (CodeLock)
                {
                    if (KnownCodes.Add(code))
                        CodeProbeQueue.Enqueue(code);
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[scraper] code cache load: " + e.Message);
        }
    }

    private static void PersistCodeCache()
    {
        try
        {
            string dir = Path.GetDirectoryName(CodeCachePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            string[] codes;
            lock (CodeLock)
                codes = KnownCodes.OrderBy(c => c, StringComparer.Ordinal).ToArray();
            File.WriteAllLines(CodeCachePath, codes);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[scraper] code cache save: " + e.Message);
        }
    }

    private static void RememberCode(string code, bool seenThisCycle)
    {
        code = NormalizeCode(code);
        if (code == null) return;
        lock (CodeLock)
        {
            if (seenThisCycle)
                CodesSeenThisCycle.Add(code);
            if (KnownCodes.Add(code))
                CodeProbeQueue.Enqueue(code);
        }
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        code = code.Trim().ToUpperInvariant();
        if (code.Length < 4 || code.Length > 16) return null;
        foreach (char ch in code)
        {
            if (!char.IsLetterOrDigit(ch)) return null;
        }
        return code;
    }

    private static List<string> TakeCodeProbes(int budget)
    {
        if (budget <= 0) return new List<string>();
        lock (CodeLock)
        {
            if (CodeProbeQueue.Count == 0)
            {
                foreach (string known in KnownCodes)
                    CodeProbeQueue.Enqueue(known);
            }

            var batch = new List<string>(Math.Min(budget, KnownCodes.Count));
            int guard = CodeProbeQueue.Count;
            while (batch.Count < budget && guard-- > 0 && CodeProbeQueue.Count > 0)
            {
                string code = CodeProbeQueue.Dequeue();
                CodeProbeQueue.Enqueue(code);
                if (CodesSeenThisCycle.Contains(code))
                    continue;
                batch.Add(code);
            }
            return batch;
        }
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

    private readonly record struct PlayerPresence(string Pid, string Name, string Server, string ServerMap);
    private readonly record struct DbRow(string Pid, string Name, string Status, string Server, string ServerMap);
    private readonly record struct SyncStats(
        int Upserted,
        int Inserted,
        int MarkedOffline,
        int Unchanged,
        int Loading,
        int InGame);

    private sealed class ScrapeSnapshot
    {
        public ScrapeSnapshot(Dictionary<string, PlayerPresence> players, int lobbyCount, Dictionary<int, int> privacyCounts)
        {
            Players = players;
            LobbyCount = lobbyCount;
            PrivacyCounts = privacyCounts;
        }

        public Dictionary<string, PlayerPresence> Players { get; }
        public int LobbyCount { get; }
        public Dictionary<int, int> PrivacyCounts { get; }
    }
}
