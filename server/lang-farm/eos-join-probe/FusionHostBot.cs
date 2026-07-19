using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Platform;

namespace EosJoinProbe;

/// <summary>
/// Experimental headless Fusion "host": CreateLobby + Fusion matchmaking attributes
/// so ReallyWorld appears in the public browser (not a full Unity/Fusion game host).
/// </summary>
internal static class FusionHostBot
{
    private static readonly string GameName = Env("FUSION_GAME_NAME", "BONELAB");
    private static readonly string LobbyName = Env("HOST_LOBBY_NAME", "ReallyWorld");
    private static readonly string LobbyDesc = Env(
        "HOST_LOBBY_DESC",
        "Официальный сервер от www.bonelab.fun");
    private static readonly string LevelTitle = Env("HOST_LEVEL_TITLE", "Halfway Park");
    private static readonly string LevelBarcode = Env(
        "HOST_LEVEL_BARCODE",
        "fa534c5a83ee4ec6bd641fec424c4142.Level.LevelHalfwayPark");
    private static readonly string BotNick = Env("BOT_NICK", "ADMIN");
    private static readonly string LobbyVersion = Env("HOST_LOBBY_VERSION", "1.14.2");
    private static readonly int MaxMembers = int.TryParse(Env("HOST_MAX_MEMBERS", "8"), out var m)
        ? Math.Clamp(m, 2, 32) : 8;
    private static readonly int HoldSec = int.TryParse(Env("HOST_HOLD_SEC", "600"), out var h)
        ? Math.Clamp(h, 30, 86400) : 600;
    private static readonly int VersionMajor = int.TryParse(Env("HOST_VERSION_MAJOR", "1"), out var vma) ? vma : 1;
    private static readonly int VersionMinor = int.TryParse(Env("HOST_VERSION_MINOR", "14"), out var vmi) ? vmi : 14;

    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static EosIdentity _identity;
    private static string _lobbyId = "";
    private static string _lobbyCode = "";

    private static string Env(string k, string d)
        => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

    public static int Run()
    {
        InstallNativeResolver();
        string data = Env("EOS_DATA_DIR", "/tmp/lang-farm/state/host-reallyworld");
        Directory.CreateDirectory(data);
        bool forceNew = Env("EOS_FORCE_NEW_ACCOUNT", "1") == "1";

        Console.WriteLine("[host] Fusion headless lobby host (matchmaking listing)");
        Console.WriteLine($"[host] name={LobbyName} map={LevelTitle} nick={BotNick} hold={HoldSec}s data={data}");

        try
        {
            _identity = EosIdentity.LoadOrMint(data, forceNew: forceNew);
            // Force ADMIN display name for this host identity.
            _identity.DisplayName = BotNick;
            EosIdentity.Persist(_identity, EosIdentity.IdentityPath(data));
            _platform = EosIdentity.CreatePlatform(_identity);
            _localUser = EosIdentity.LoginDeviceAccount(
                _platform,
                _identity,
                forceNew: forceNew || _identity.FreshMint,
                tick: () => _platform?.Tick());
            // Login uses UserLoginInfo.DisplayName — re-login path already used BotNick if set on identity.
            _identity.DisplayName = BotNick;
            _identity.ProductUserId = _localUser.ToString();
            EosIdentity.Persist(_identity, EosIdentity.IdentityPath(data));
            Console.WriteLine($"[host] logged in puid={_localUser} nick={BotNick}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[host] login failed: " + e.Message);
            return 3;
        }

        // Optional: dump a real Halfway Park LobbyInfo template from browser.
        if (Env("HOST_DUMP_ONLY", "0") == "1")
        {
            DumpHalfwayTemplate();
            Shutdown();
            return 0;
        }

        if (!CreateAndPublishLobby())
        {
            Shutdown();
            return 4;
        }

        Console.WriteLine($"[host] LIVE lobbyId={_lobbyId} code={_lobbyCode} — holding {HoldSec}s (Ctrl+C to stop)");
        Console.WriteLine($"[host] Promote tips: PUBLIC Privacy=0 Full=False, max={MaxMembers}, name starts with '!' or 'AAA' often sorts up in clients — we use ReallyWorld as requested.");

        var until = DateTime.UtcNow.AddSeconds(HoldSec);
        var nextPulse = DateTime.UtcNow;
        while (DateTime.UtcNow < until)
        {
            try { _platform.Tick(); } catch { /* */ }
            if (DateTime.UtcNow >= nextPulse)
            {
                // Refresh LobbyInfo so lastUpdated stays fresh in browsers.
                if (!UpdateLobbyAttributes(pulse: true))
                    Console.Error.WriteLine("[host] pulse update failed");
                else
                    Console.WriteLine($"[host] pulse ok {DateTime.UtcNow:HH:mm:ss}Z code={_lobbyCode}");
                nextPulse = DateTime.UtcNow.AddSeconds(45);
            }
            Thread.Sleep(50);
        }

        LeaveLobby();
        Shutdown();
        Console.WriteLine("[host] stopped");
        return 0;
    }

    private static bool CreateAndPublishLobby()
    {
        var lobby = _platform.GetLobbyInterface();
        if (lobby == null)
        {
            Console.Error.WriteLine("[host] Lobby interface null");
            return false;
        }

        _lobbyCode = GenerateLobbyCode();
        bool done = false;
        Result cr = Result.UnexpectedError;
        string id = null;
        var opts = new CreateLobbyOptions
        {
            LocalUserId = _localUser,
            MaxLobbyMembers = (uint)MaxMembers,
            PermissionLevel = LobbyPermissionLevel.Publicadvertised,
            PresenceEnabled = true,
            AllowInvites = true,
            BucketId = GameName,
            DisableHostMigration = true,
            EnableRTCRoom = false,
            EnableJoinById = true,
            RejoinAfterKickRequiresInvite = false,
            RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin,
        };
        lobby.CreateLobby(ref opts, null, (ref CreateLobbyCallbackInfo info) =>
        {
            cr = info.ResultCode;
            id = info.LobbyId;
            done = true;
        });
        if (!Pump(() => done, 45, "CreateLobby") || cr != Result.Success || string.IsNullOrEmpty(id))
        {
            Console.Error.WriteLine($"[host] CreateLobby failed: {cr}");
            return false;
        }
        _lobbyId = id;
        Console.WriteLine($"[host] CreateLobby ok id={_lobbyId}");
        return UpdateLobbyAttributes(pulse: false);
    }

    private static bool UpdateLobbyAttributes(bool pulse)
    {
        var lobby = _platform.GetLobbyInterface();
        bool done = false;
        Result ur = Result.UnexpectedError;
        LobbyModification mod = null;
        var updOpts = new UpdateLobbyModificationOptions
        {
            LocalUserId = _localUser,
            LobbyId = _lobbyId,
        };
        Result begin = lobby.UpdateLobbyModification(ref updOpts, out mod);
        if (begin != Result.Success || mod == null)
        {
            Console.Error.WriteLine("[host] UpdateLobbyModification: " + begin);
            return false;
        }

        try
        {
            int ok = 0, fail = 0;
            void Attr(string key, string val, LobbyAttributeVisibility vis = LobbyAttributeVisibility.Public)
            {
                var data = new AttributeData
                {
                    Key = key,
                    Value = val, // implicit AttributeDataValue string setter
                };
                var a = new LobbyModificationAddAttributeOptions
                {
                    Attribute = data,
                    Visibility = vis,
                };
                Result r = mod.AddAttribute(ref a);
                if (r != Result.Success)
                {
                    fail++;
                    Console.Error.WriteLine($"[host] AddAttribute {key} len={val?.Length}: {r}");
                }
                else ok++;
            }

            // Matchmaking keys Fusion scrapers/browsers filter on.
            Attr("Game", GameName);
            Attr("Privacy", "0");
            Attr("Full", "False");
            Attr("VersionMajor", VersionMajor.ToString());
            Attr("VersionMinor", VersionMinor.ToString());
            Attr("LobbyCode", _lobbyCode);
            Attr("HasLobbyOpen", "True");
            Attr("MarrowFusion", "True");
            Attr("LobbyName", LobbyName);
            Attr("LevelTitle", LevelTitle);
            Attr("LevelBarcode", LevelBarcode);
            Attr("HostName", BotNick);

            string lobbyInfo = BuildLobbyInfoJson(pulse);
            Console.WriteLine($"[host] LobbyInfo bytes={Encoding.UTF8.GetByteCount(lobbyInfo)} attrs_ok_so_far={ok}");
            Attr("LobbyInfo", lobbyInfo);
            Console.WriteLine($"[host] attributes ok={ok} fail={fail}");

            var apply = new UpdateLobbyOptions { LobbyModificationHandle = mod };
            lobby.UpdateLobby(ref apply, null, (ref UpdateLobbyCallbackInfo info) =>
            {
                ur = info.ResultCode;
                done = true;
            });
            if (!Pump(() => done, 30, "UpdateLobby") || ur != Result.Success)
            {
                Console.Error.WriteLine("[host] UpdateLobby: " + ur);
                return false;
            }
            return true;
        }
        finally
        {
            try { mod.Release(); } catch { /* */ }
        }
    }

    private static string BuildLobbyInfoJson(bool pulse)
    {
        string puid = _localUser.ToString();
        // Exact shape sampled from live Fusion Halfway Park lobbies (Quest EOS).
        var payload = new Dictionary<string, object>
        {
            ["lobbyID"] = _lobbyId,
            ["lobbyCode"] = _lobbyCode,
            ["lobbyName"] = LobbyName,
            ["lobbyDescription"] = LobbyDesc,
            ["lobbyVersion"] = LobbyVersion,
            ["lobbyHostName"] = BotNick,
            ["lobbyHostID"] = puid,
            ["playerCount"] = 1,
            ["playerList"] = new Dictionary<string, object>
            {
                ["players"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["platformID"] = puid,
                        ["username"] = BotNick,
                        ["nickname"] = BotNick,
                        ["description"] = LobbyDesc,
                        ["permissionLevel"] = 2, // host/owner
                        ["avatarTitle"] = "Strong",
                        ["avatarModID"] = -1,
                    },
                },
            },
            ["levelTitle"] = LevelTitle,
            ["levelBarcode"] = LevelBarcode,
            ["levelModID"] = -1,
            ["gamemodeTitle"] = "",
            ["gamemodeBarcode"] = "",
            ["timeBetweenGamemodeRounds"] = 30,
            ["nameTags"] = true,
            ["privacy"] = 0,
            ["slowMoMode"] = 0,
            ["maxPlayers"] = MaxMembers,
            ["voiceChat"] = true,
            ["playerConstraining"] = false,
            ["mortality"] = true,
            ["friendlyFire"] = true,
            ["knockout"] = false,
            ["knockoutLength"] = 10,
            ["maxAvatarHeight"] = 20,
            ["devTools"] = 0,
            ["constrainer"] = 0,
            ["customAvatars"] = 0,
            ["kicking"] = 1,
            ["banning"] = 1,
            ["teleportation"] = 1,
            ["updatedPulse"] = pulse,
        };
        return JsonSerializer.Serialize(payload);
    }

    private static void DumpHalfwayTemplate()
    {
        Console.WriteLine("[host] dumping public lobbies with Halfway in LobbyInfo…");
        var lobby = _platform.GetLobbyInterface();
        var createOpts = new CreateLobbySearchOptions { MaxResults = 50 };
        if (lobby.CreateLobbySearch(ref createOpts, out LobbySearch search) != Result.Success || search == null)
            return;
        try
        {
            void Set(string key, string val)
            {
                var a = new AttributeData { Key = key, Value = new AttributeDataValue { AsUtf8 = val } };
                var o = new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = a };
                search.SetParameter(ref o);
            }
            Set("Game", GameName);
            Set("Privacy", "0");
            Set("Full", "False");
            bool done = false;
            Result fr = Result.UnexpectedError;
            var findOpts = new LobbySearchFindOptions { LocalUserId = _localUser };
            search.Find(ref findOpts, null, (ref LobbySearchFindCallbackInfo info) => { fr = info.ResultCode; done = true; });
            Pump(() => done, 45, "Find");
            Console.WriteLine("[host] Find: " + fr);
            var countOpts = default(LobbySearchGetSearchResultCountOptions);
            uint count = search.GetSearchResultCount(ref countOpts);
            int dumped = 0;
            for (uint i = 0; i < count && dumped < 3; i++)
            {
                var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success || details == null)
                    continue;
                try
                {
                    var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "LobbyInfo" };
                    if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) != Result.Success || attr == null)
                        continue;
                    string json = attr.Value.Data?.Value.AsUtf8;
                    if (string.IsNullOrEmpty(json) || json.IndexOf("Halfway", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    Console.WriteLine("----- LobbyInfo sample -----");
                    Console.WriteLine(json.Length > 4000 ? json[..4000] : json);
                    dumped++;
                }
                finally { try { details.Release(); } catch { /* */ } }
            }
            if (dumped == 0)
                Console.WriteLine("[host] no Halfway sample in this shard — will use built-in template");
        }
        finally { search.Release(); }
    }

    private static void LeaveLobby()
    {
        if (string.IsNullOrEmpty(_lobbyId) || _platform == null || _localUser == null) return;
        try
        {
            var lobby = _platform.GetLobbyInterface();
            bool done = false;
            var opts = new LeaveLobbyOptions { LocalUserId = _localUser, LobbyId = _lobbyId };
            lobby.LeaveLobby(ref opts, null, (ref LeaveLobbyCallbackInfo info) =>
            {
                done = true;
                Console.WriteLine("[host] Leave: " + info.ResultCode);
            });
            Pump(() => done, 20, "Leave");
        }
        catch (Exception e) { Console.Error.WriteLine("[host] leave: " + e.Message); }
    }

    private static void Shutdown()
    {
        try { LeaveLobby(); } catch { /* */ }
        try { _platform?.Release(); } catch { /* */ }
        _platform = null;
        _localUser = null;
    }

    private static string GenerateLobbyCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var sb = new StringBuilder(8);
        foreach (byte b in bytes)
            sb.Append(alphabet[b % alphabet.Length]);
        return sb.ToString();
    }

    private static bool Pump(Func<bool> done, int timeoutSec, string tag)
    {
        var until = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (!done() && DateTime.UtcNow < until)
        {
            try { _platform?.Tick(); } catch { /* */ }
            Thread.Sleep(15);
        }
        if (done()) return true;
        Console.Error.WriteLine($"[host] {tag} timed out");
        return false;
    }

    private static void InstallNativeResolver()
    {
        string so = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "native", "libEOSSDK-Linux-Shipping.so"));
        NativeLibrary.SetDllImportResolver(typeof(Bindings).Assembly, (name, _, _) =>
        {
            if (name != null && name.IndexOf("EOSSDK", StringComparison.OrdinalIgnoreCase) >= 0)
                return NativeLibrary.Load(so);
            return IntPtr.Zero;
        });
    }
}
