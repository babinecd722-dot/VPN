using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Platform;

namespace EosJoinProbe;

/// <summary>
/// Headless Fusion lobby host: CreateLobby + matchmaking attrs + P2P handshake.
/// Listing/handshake only — not a Unity game simulation.
/// </summary>
internal static class FusionHostBot
{
    private static readonly string GameName = Env("FUSION_GAME_NAME", "BONELAB");
    private static readonly string LobbyName = Env("HOST_LOBBY_NAME", "www.bonelab.fun");
    private static readonly string LobbyDesc = Env(
        "HOST_LOBBY_DESC",
        "Официальный сервер www.bonelab.fun");
    private static readonly string LevelTitle = Env("HOST_LEVEL_TITLE", "Halfway Park");
    private static readonly string LevelBarcode = Env(
        "HOST_LEVEL_BARCODE",
        "fa534c5a83ee4ec6bd641fec424c4142.Level.LevelHalfwayPark");
    private static readonly string BotNick = Env("BOT_NICK", "ADMIN");
    private static readonly string LobbyVersion = Env("HOST_LOBBY_VERSION", "1.14.2");
    private static readonly int MaxMembers = int.TryParse(Env("HOST_MAX_MEMBERS", "8"), out var m)
        ? Math.Clamp(m, 2, 32) : 8;
    // 0 = run forever (systemd 24/7). Otherwise hold N seconds then exit.
    private static readonly int HoldSec = int.TryParse(Env("HOST_HOLD_SEC", "0"), out var h)
        ? Math.Clamp(h, 0, 86400 * 30) : 0;
    private static readonly int VersionMajor = int.TryParse(Env("HOST_VERSION_MAJOR", "1"), out var vma) ? vma : 1;
    private static readonly int VersionMinor = int.TryParse(Env("HOST_VERSION_MINOR", "14"), out var vmi) ? vmi : 14;
    private static readonly ushort P2pPort = ushort.TryParse(Env("HOST_P2P_PORT", "17877"), out var pp) ? pp : (ushort)17877;
    // Cosmetic browser count only (LobbyInfo). 0 = real peers. Default max-1 so Full stays False.
    private static readonly int DisplayPlayers = int.TryParse(Env("HOST_DISPLAY_PLAYERS", "7"), out var dp)
        ? Math.Clamp(dp, 0, 32) : 7;
    private static readonly bool MarkFull = Env("HOST_MARK_FULL", "0") == "1";
    private static volatile bool _stop;

    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static EosIdentity _identity;
    private static string _lobbyId = "";
    private static string _lobbyCode = "";
    private static readonly SocketId FusionSocket = new() { SocketName = "FusionSocket" };
    private static readonly ConcurrentDictionary<string, byte> PeerSmallIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> PeerNames = new(StringComparer.OrdinalIgnoreCase);
    private static byte _nextSmallId = 1; // 0 = host
    private static long _p2pRequests;
    private static long _p2pEstablished;
    private static long _connRequests;
    private static long _packetsIn;
    private const byte TagConnectionRequest = 1;
    private const byte TagConnectionResponse = 2;
    private const byte TagSceneLoad = 12;
    private const byte TagDynamicsAssignment = 201;
    private const int AvatarStatsPad = 420; // Quest Fusion SerializedAvatarStats blob size

    private static string Env(string k, string d)
        => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

    public static int Run()
    {
        InstallNativeResolver();
        string data = Env("EOS_DATA_DIR", "/opt/fusion-lobby-host/data");
        Directory.CreateDirectory(data);
        // Reuse identity by default so VPS listing stays stable across restarts.
        bool forceNew = Env("EOS_FORCE_NEW_ACCOUNT", "0") == "1";

        Console.WriteLine("[host] Fusion headless lobby host (listing + P2P handshake)");
        string holdLabel = HoldSec <= 0 ? "forever" : $"{HoldSec}s";
        Console.WriteLine(
            $"[host] name={LobbyName} map={LevelTitle} nick={BotNick} hold={holdLabel} " +
            $"p2pPort={P2pPort} displayPlayers={DisplayPlayers} data={data}");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _stop = true;
            Console.WriteLine("[host] stop requested");
        };

        try
        {
            _identity = EosIdentity.LoadOrMint(data, forceNew: forceNew);
            _identity.DisplayName = BotNick;
            EosIdentity.Persist(_identity, EosIdentity.IdentityPath(data));
            _platform = EosIdentity.CreatePlatform(_identity);
            _localUser = EosIdentity.LoginDeviceAccount(
                _platform,
                _identity,
                forceNew: forceNew || _identity.FreshMint,
                tick: () => _platform?.Tick());
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

        var p2p = _platform.GetP2PInterface();
        if (p2p != null)
        {
            ConfigureP2P(p2p);
            RegisterP2P(p2p);
            RegisterLobbyMemberHooks();
            Console.WriteLine("[host] P2P host hooks armed (Accept + ConnectionResponse/SceneLoad)");
        }
        else
            Console.Error.WriteLine("[host] P2P interface null — join handshake impossible");

        Console.WriteLine($"[host] LIVE lobbyId={_lobbyId} code={_lobbyCode} — holding {holdLabel}");
        Console.WriteLine("[host] NOTE: listing+handshake only. Playable join needs real BONELAB+Fusion (Unity).");

        var until = HoldSec <= 0 ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(HoldSec);
        var nextPulse = DateTime.UtcNow;
        int pulseFail = 0;
        while (!_stop && DateTime.UtcNow < until)
        {
            try { _platform.Tick(); } catch { /* */ }
            if (p2p != null) DrainP2P(p2p);
            if (DateTime.UtcNow >= nextPulse)
            {
                if (!UpdateLobbyAttributes(pulse: true))
                {
                    pulseFail++;
                    Console.Error.WriteLine($"[host] pulse update failed ({pulseFail})");
                    if (pulseFail >= 3)
                    {
                        Console.Error.WriteLine("[host] recreating lobby after pulse failures");
                        LeaveLobby();
                        if (!CreateAndPublishLobby())
                        {
                            Console.Error.WriteLine("[host] recreate failed — exiting for systemd restart");
                            break;
                        }
                        pulseFail = 0;
                    }
                }
                else
                {
                    pulseFail = 0;
                    Console.WriteLine(
                        $"[host] pulse ok {DateTime.UtcNow:HH:mm:ss}Z code={_lobbyCode} " +
                        $"p2pReq={_p2pRequests} p2pOk={_p2pEstablished} connReq={_connRequests} pkt={_packetsIn}");
                }
                nextPulse = DateTime.UtcNow.AddSeconds(45);
            }
            Thread.Sleep(15);
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

            int shown = ShownPlayerCount();
            bool full = MarkFull || shown >= MaxMembers;
            // Matchmaking keys Fusion scrapers/browsers filter on.
            Attr("Game", GameName);
            Attr("Privacy", "0");
            // Keep Full=False by default so Find(Full=False) still returns us while count looks packed.
            Attr("Full", full ? "True" : "False");
            Attr("VersionMajor", VersionMajor.ToString());
            Attr("VersionMinor", VersionMinor.ToString());
            Attr("LobbyCode", _lobbyCode);
            Attr("HasLobbyOpen", full ? "False" : "True");
            Attr("MarrowFusion", "True");
            Attr("LobbyName", LobbyName);
            Attr("LevelTitle", LevelTitle);
            Attr("LevelBarcode", LevelBarcode);
            Attr("HostName", BotNick);

            string lobbyInfo = BuildLobbyInfoJson(pulse);
            Console.WriteLine(
                $"[host] LobbyInfo bytes={Encoding.UTF8.GetByteCount(lobbyInfo)} " +
                $"shown={shown}/{MaxMembers} full={full} attrs_ok_so_far={ok}");
            Attr("LobbyInfo", lobbyInfo);
            if (!pulse)
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(lobbyInfo);
                    var players = doc.RootElement.GetProperty("playerList").GetProperty("players");
                    var bits = new System.Collections.Generic.List<string>();
                    foreach (var pl in players.EnumerateArray())
                    {
                        string u = pl.GetProperty("username").GetString();
                        string a = pl.GetProperty("avatarTitle").GetString();
                        int mid = pl.GetProperty("avatarModID").GetInt32();
                        bits.Add($"{u}|{a}|{mid}");
                    }
                    Console.WriteLine("[host] roster: " + string.Join(" ; ", bits));
                }
                catch (Exception ex) { Console.WriteLine("[host] roster parse: " + ex.Message); }
            }
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
            ["playerCount"] = ShownPlayerCount(),
            ["playerList"] = new Dictionary<string, object>
            {
                ["players"] = BuildPlayerListObjects(puid),
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


    private static void RegisterLobbyMemberHooks()
    {
        var lobby = _platform.GetLobbyInterface();
        if (lobby == null) return;
        var opts = new AddNotifyLobbyMemberStatusReceivedOptions();
        lobby.AddNotifyLobbyMemberStatusReceived(ref opts, null, (ref LobbyMemberStatusReceivedCallbackInfo info) =>
        {
            if (info.TargetUserId == null || info.TargetUserId == _localUser) return;
            if (info.CurrentStatus != LobbyMemberStatus.Joined) return;
            var p2p = _platform.GetP2PInterface();
            if (p2p == null) return;
            Result ar = AcceptPeer(p2p, info.TargetUserId);
            Console.WriteLine($"[host] lobby member joined {info.TargetUserId} accept={ar}");
        });
    }

    private static void ConfigureP2P(P2PInterface p2p)
    {
        // Isolated UDP range (default 17877+) so we never collide with farm bots on 7777.
        var port = new SetPortRangeOptions { Port = P2pPort, MaxAdditionalPortsToTry = 32 };
        Console.WriteLine("[host] SetPortRange(" + P2pPort + "): " + p2p.SetPortRange(ref port));
        var relay = new SetRelayControlOptions { RelayControl = RelayControl.ForceRelays };
        Console.WriteLine("[host] SetRelayControl(ForceRelays): " + p2p.SetRelayControl(ref relay));
    }

    private static void RegisterP2P(P2PInterface p2p)
    {
        var reqOpts = new AddNotifyPeerConnectionRequestOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionRequest(ref reqOpts, null, (ref OnIncomingConnectionRequestInfo info) =>
        {
            Interlocked.Increment(ref _p2pRequests);
            if (info.RemoteUserId == null) return;
            Result ar = AcceptPeer(p2p, info.RemoteUserId);
            Console.WriteLine($"[host] inbound P2P request from {info.RemoteUserId} accept={ar}");
        });

        var estOpts = new AddNotifyPeerConnectionEstablishedOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionEstablished(ref estOpts, null, (ref OnPeerConnectionEstablishedInfo info) =>
        {
            Interlocked.Increment(ref _p2pEstablished);
            Console.WriteLine($"[host] P2P established with {info.RemoteUserId}");
            // Re-accept + poke so delayed ConnectionRequest can land (same-host NAT is flaky).
            if (info.RemoteUserId != null)
            {
                AcceptPeer(p2p, info.RemoteUserId);
                Result poke = SendTo(p2p, info.RemoteUserId, BuildNetMessage(0, Array.Empty<byte>()));
                Console.WriteLine($"[host] post-establish poke → {info.RemoteUserId}: {poke}");
            }
        });

        var cloOpts = new AddNotifyPeerConnectionClosedOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionClosed(ref cloOpts, null, (ref OnRemoteConnectionClosedInfo info) =>
        {
            string pid = info.RemoteUserId?.ToString();
            if (!string.IsNullOrEmpty(pid))
            {
                PeerSmallIds.TryRemove(pid, out _);
                PeerNames.TryRemove(pid, out _);
            }
            Console.WriteLine($"[host] P2P closed with {info.RemoteUserId} reason={info.Reason}");
        });
    }

    private static Result AcceptPeer(P2PInterface p2p, ProductUserId remote)
    {
        var acc = new AcceptConnectionOptions
        {
            LocalUserId = _localUser,
            RemoteUserId = remote,
            SocketId = FusionSocket,
        };
        return p2p.AcceptConnection(ref acc);
    }

    private static void DrainP2P(P2PInterface p2p)
    {
        for (int i = 0; i < 100; i++)
        {
            var sizeOpts = new GetNextReceivedPacketSizeOptions { LocalUserId = _localUser, RequestedChannel = null };
            if (p2p.GetNextReceivedPacketSize(ref sizeOpts, out uint sz) != Result.Success || sz == 0)
                break;
            byte[] buf = new byte[sz];
            var recv = new ReceivePacketOptions
            {
                LocalUserId = _localUser,
                MaxDataSizeBytes = sz,
                RequestedChannel = null,
            };
            ProductUserId peer = null;
            SocketId sock = FusionSocket;
            var data = new ArraySegment<byte>(buf);
            if (p2p.ReceivePacket(ref recv, ref peer, ref sock, out _, data, out uint written) != Result.Success || written == 0)
                break;
            Interlocked.Increment(ref _packetsIn);
            HandleHostPacket(p2p, peer, buf, (int)written);
        }
    }

    private static int RealPlayerCount() => 1 + PeerSmallIds.Count;

    private static int ShownPlayerCount()
    {
        int real = RealPlayerCount();
        if (DisplayPlayers <= 0) return real;
        return Math.Clamp(Math.Max(real, DisplayPlayers), 1, MaxMembers);
    }

    // Realistic Quest-style nicks sampled from our client_data (not bots).
    private static readonly string[] FakeNickPool =
    {
        "solarwalker527", "HeyGunner", "nuggetreal", "Jayvr", "gmpan",
        "gekko", "Zeldon6367", "Glub", "friskalisk", "french",
        "guy_010", "Chilaquiles_VR", "Linodergamer", "Zenny", "peanut",
        "Vraptor10", "AIDEN", "Trylix", "quietone", "fzitsalex",
        "clowny47", "nickai", "GamerKid20", "dagoat", "Biggins",
        "veil", "Nosbik", "deftimes13", "skelly", "toast",
        "astro", "coolguy", "DexterFetch", "dino", "bobby",
        "ghost", "luke", "cam", "blue", "Ace",
    };

    // Vanilla content avatars (mod.io id = -1) + popular public mod.io avatar mods seen in live lobbies.
    private static readonly (string Title, int ModId)[] VanillaAvatars =
    {
        ("Ford", -1),
        ("Ford", -1),
        ("PolyBlank", -1),
        ("Strong", -1),
    };

    private static readonly (string Title, int ModId)[] ModAvatars =
    {
        ("Koffee", 4518835),
        ("Charple Charlie", 5251392),
        ("Ford NikeTech", 5476781),
        ("Gunslinger Ford", 5977616),
        ("Fat Ford", 5574456),
        ("Ford (BlackTrey)", 5602466),
        ("Super-Ford", 6119996),
        ("Male Hoodie Peasant (ST1, Half Life)", 6002577),
        ("Jason Part 6", 6114112),
        ("Albert Wesker (The Mastermind)", 6139862),
        ("MD Serial Designation N", 5017189),
        ("Spider-Man (Black Suit)", 6117575),
        ("Cardboard Buddy", 4295722),
        ("GTAV Franklin", 4576665),
        ("The Joker (Batman Arkham Asylum)", 5216927),
        ("WW1 German Soldier", 5779160),
        ("Arthur Morgan-Winter", 6160920),
        ("Nullbody Agent (Fancy)", 3131330),
        ("Jacket", 3417924),
        ("Mahoraga", 5662756),
    };

    private static object[] BuildPlayerListObjects(string hostPuid)
    {
        // Host preview: vanilla Ford looks official.
        var list = new List<object>
        {
            new Dictionary<string, object>
            {
                ["platformID"] = hostPuid,
                ["username"] = BotNick,
                ["nickname"] = BotNick,
                ["description"] = LobbyDesc,
                ["permissionLevel"] = 2,
                ["avatarTitle"] = "Ford",
                ["avatarModID"] = -1,
            },
        };
        foreach (var kv in PeerSmallIds.OrderBy(k => k.Value))
        {
            string name = PeerNames.TryGetValue(kv.Key, out var n) && !string.IsNullOrEmpty(n) ? n : "Player";
            list.Add(new Dictionary<string, object>
            {
                ["platformID"] = kv.Key,
                ["username"] = name,
                ["nickname"] = name,
                ["description"] = "",
                ["permissionLevel"] = 0,
                ["avatarTitle"] = "Ford",
                ["avatarModID"] = -1,
            });
        }
        // Pad LobbyInfo only — no EOS members, no P2P, no CPU. Stable fake IDs per lobby code.
        int need = ShownPlayerCount() - list.Count;
        var usedNicks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in list)
        {
            if (o is Dictionary<string, object> d && d.TryGetValue("username", out var u) && u is string s)
                usedNicks.Add(s);
        }
        // ~40% vanilla Ford/PolyBlank/Strong, ~60% popular mod.io avatars.
        for (int i = 0; i < need; i++)
        {
            string fakeId = FakePlatformId(_lobbyCode, i);
            string fakeName = FakeDisplayName(_lobbyCode, i, usedNicks);
            usedNicks.Add(fakeName);
            var av = PickAvatar(_lobbyCode, i);
            list.Add(new Dictionary<string, object>
            {
                ["platformID"] = fakeId,
                ["username"] = fakeName,
                ["nickname"] = fakeName,
                ["description"] = "",
                ["permissionLevel"] = 0,
                ["avatarTitle"] = av.Title,
                ["avatarModID"] = av.ModId,
            });
        }
        return list.ToArray();
    }

    private static string FakePlatformId(string seed, int index)
    {
        // 32-hex ProductUserId-shaped id, stable across pulses.
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"pad|{seed}|{index}"));
        var sb = new StringBuilder(32);
        sb.Append("0002");
        for (int i = 0; i < 14; i++)
            sb.Append(hash[i].ToString("x2"));
        return sb.ToString()[..32];
    }

    private static string FakeDisplayName(string seed, int index, HashSet<string> used)
    {
        for (int attempt = 0; attempt < FakeNickPool.Length; attempt++)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"nick|{seed}|{index}|{attempt}"));
            int pick = hash[0] | (hash[1] << 8);
            string name = FakeNickPool[(pick + attempt) % FakeNickPool.Length];
            if (used.Add(name))
                return name;
        }
        return "player" + (index + 2);
    }

    private static (string Title, int ModId) PickAvatar(string seed, int index)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"av|{seed}|{index}"));
        // First ~2 of every 5 pads → vanilla Ford/PolyBlank/Strong; rest → mod.io.
        bool vanilla = (hash[0] % 5) < 2;
        if (vanilla)
            return VanillaAvatars[hash[1] % VanillaAvatars.Length];
        return ModAvatars[hash[1] % ModAvatars.Length];
    }

    private static void HandleHostPacket(P2PInterface p2p, ProductUserId peer, byte[] buf, int len)
    {
        if (len < 3 || peer == null) return;
        // skip fragments
        if (len >= 8 && BinaryPrimitives.ReadUInt16LittleEndian(buf.AsSpan(0, 2)) == 62121)
            return;
        byte tag = buf[0];
        if (tag == 0)
        {
            Console.WriteLine($"[host] poke/Unknown ← {peer}");
            return;
        }
        if (tag != TagConnectionRequest)
        {
            // Pose/etc. spam is normal after a real client handshake — don't flood journal.
            if (tag is not (4 or 17 or 67))
                Console.WriteLine($"[host] pkt tag={tag} len={len} ← {peer}");
            return;
        }
        Interlocked.Increment(ref _connRequests);
        string peerId = peer.ToString();
        TryParseConnectionRequest(buf.AsSpan(0, len), out string reqUser, out string avatarBarcode);
        if (string.IsNullOrEmpty(reqUser)) reqUser = "Player";
        if (string.IsNullOrEmpty(avatarBarcode))
            avatarBarcode = "SLZ.BONELAB.Content.Avatar.Ford";
        PeerNames[peerId] = reqUser;
        byte sid = PeerSmallIds.GetOrAdd(peerId, _ =>
        {
            byte n = _nextSmallId;
            if (_nextSmallId < 254) _nextSmallId++;
            return n;
        });

        // Mirror real host order: catchup host → join response → SceneLoad → empty DynamicsAssignment.
        string hostId = _localUser.ToString();
        Result r1 = SendTo(p2p, peer, BuildConnectionResponse(hostId, 0, BotNick, "SLZ.BONELAB.Content.Avatar.Ford", isInitialJoin: false));
        Result r2 = SendTo(p2p, peer, BuildConnectionResponse(peerId, sid, reqUser, avatarBarcode, isInitialJoin: true));
        Result r3 = SendTo(p2p, peer, BuildSceneLoad(LevelBarcode, ""));
        Result r4 = SendTo(p2p, peer, BuildEmptyDynamicsAssignment());
        Console.WriteLine(
            $"[host] ConnectionRequest ← {peerId} user={reqUser} sid={sid} " +
            $"catchup={r1} join={r2} scene={r3} dyn={r4}");
        try { UpdateLobbyAttributes(pulse: true); } catch { /* */ }
    }

    private static Result SendTo(P2PInterface p2p, ProductUserId peer, byte[] msg)
    {
        var send = new SendPacketOptions
        {
            LocalUserId = _localUser,
            RemoteUserId = peer,
            SocketId = FusionSocket,
            Channel = 1,
            Data = new ArraySegment<byte>(msg),
            AllowDelayedDelivery = true,
            Reliability = PacketReliability.ReliableUnordered,
            DisableAutoAcceptConnection = false,
        };
        return p2p.SendPacket(ref send);
    }

    private static bool TryParseConnectionRequest(ReadOnlySpan<byte> packet, out string username, out string avatarBarcode)
    {
        username = null;
        avatarBarcode = null;
        if (packet.Length < 16 || packet[0] != TagConnectionRequest) return false;
        int off = 1;
        byte relay = packet[off++];
        off++; // channel
        if (relay != 0)
        {
            bool hasSender = packet[off++] != 0;
            if (hasSender) off++;
        }
        if (!TryReadBytes(packet, ref off, out var body)) return false;
        int b = 0;
        if (!TryReadString(body, ref b, out _)) return false; // platform id
        // Version: 3 ints (major/minor/patch) as used by Quest bots / older Fusion
        if (b + 12 > body.Length) return false;
        b += 12;
        if (!TryReadString(body, ref b, out avatarBarcode)) return false;
        if (b + AvatarStatsPad > body.Length) return false;
        b += AvatarStatsPad;
        if (b + 4 > body.Length) return false;
        int metaCount = BinaryPrimitives.ReadInt32BigEndian(body.AsSpan(b, 4));
        b += 4;
        if (metaCount < 0 || metaCount > 256) return false;
        for (int i = 0; i < metaCount; i++)
        {
            if (!TryReadString(body, ref b, out var key)) return false;
            if (!TryReadString(body, ref b, out var val)) return false;
            if (key is "Username" or "username" or "Nickname" or "nickname")
                username = val;
        }
        return true;
    }

    private static byte[] BuildConnectionResponse(
        string platformId, byte smallId, string username, string avatarBarcode, bool isInitialJoin)
    {
        // Quest MarrowFusion wire: string PlatformID + SmallID + metadata (+ avatar/stats/join flag).
        var payload = new MemoryStream();
        void WInt(int v)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, v);
            payload.Write(b);
        }
        void WStr(string s)
        {
            byte[] utf = Encoding.UTF8.GetBytes(s ?? "");
            WInt(utf.Length);
            payload.Write(utf, 0, utf.Length);
        }
        WStr(platformId);
        payload.WriteByte(smallId);
        WInt(2);
        WStr("Username"); WStr(username ?? "Player");
        WStr("Nickname"); WStr(username ?? "Player");
        WInt(0); // equipped items
        WStr(avatarBarcode ?? "SLZ.BONELAB.Content.Avatar.Ford");
        payload.Write(new byte[AvatarStatsPad], 0, AvatarStatsPad);
        payload.WriteByte(isInitialJoin ? (byte)1 : (byte)0);
        return BuildNetMessage(TagConnectionResponse, payload.ToArray());
    }

    private static byte[] BuildSceneLoad(string levelBarcode, string loadingScreenBarcode)
    {
        var payload = new MemoryStream();
        void WInt(int v)
        {
            Span<byte> b = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(b, v);
            payload.Write(b);
        }
        void WStr(string s)
        {
            byte[] utf = Encoding.UTF8.GetBytes(s ?? "");
            WInt(utf.Length);
            payload.Write(utf, 0, utf.Length);
        }
        WStr(levelBarcode);
        WStr(loadingScreenBarcode ?? "");
        return BuildNetMessage(TagSceneLoad, payload.ToArray());
    }

    private static byte[] BuildEmptyDynamicsAssignment()
    {
        // Dictionary count = 0
        byte[] body = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(body, 0);
        return BuildNetMessage(TagDynamicsAssignment, body);
    }

    private static byte[] BuildNetMessage(byte tag, byte[] payload)
    {
        byte[] msg = new byte[3 + 4 + payload.Length];
        msg[0] = tag;
        msg[1] = 0; // RelayType.None
        msg[2] = 0; // NetworkChannel.Reliable
        BinaryPrimitives.WriteInt32BigEndian(msg.AsSpan(3, 4), payload.Length);
        if (payload.Length > 0)
            Buffer.BlockCopy(payload, 0, msg, 7, payload.Length);
        return msg;
    }

    private static bool TryReadBytes(ReadOnlySpan<byte> buf, ref int off, out byte[] data)
    {
        data = Array.Empty<byte>();
        if (off + 4 > buf.Length) return false;
        int n = BinaryPrimitives.ReadInt32BigEndian(buf.Slice(off, 4));
        off += 4;
        if (n < 0 || off + n > buf.Length) return false;
        data = buf.Slice(off, n).ToArray();
        off += n;
        return true;
    }

    private static bool TryReadString(ReadOnlySpan<byte> buf, ref int off, out string s)
    {
        s = null;
        if (off + 4 > buf.Length) return false;
        int n = BinaryPrimitives.ReadInt32BigEndian(buf.Slice(off, 4));
        off += 4;
        if (n < 0) { s = null; return true; }
        if (off + n > buf.Length) return false;
        s = Encoding.UTF8.GetString(buf.Slice(off, n));
        off += n;
        return true;
    }

    private static bool TryReadString(byte[] buf, ref int off, out string s)
        => TryReadString(buf.AsSpan(), ref off, out s);

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
