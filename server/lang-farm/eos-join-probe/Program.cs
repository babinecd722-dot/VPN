using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;
using Epic.OnlineServices.RTC;
using Epic.OnlineServices.RTCAudio;

namespace EosJoinProbe;

/// <summary>
/// VPS "join substitute": EOS JoinLobby + Lobby RTC audio probe (no BONELAB client).
/// Tests whether Fusion lobbies expose EOS RTC voice we can harvest headlessly.
/// </summary>
internal static class Program
{
    private static readonly string ProductId = Env("EOS_PRODUCT_ID", "29e074d5b4724f3bb01f26b7e33d2582");
    private static readonly string SandboxId = Env("EOS_SANDBOX_ID", "26f32d66d87f4dfeb4a7449b776a41f1");
    private static readonly string DeploymentId = Env("EOS_DEPLOYMENT_ID", "76d456523b2d468dbde74e7ea6ddcd6b");
    private static readonly string ClientId = Env("EOS_CLIENT_ID", "xyza78915hKqxe2TNTavpq2sxBDvJ9AH");
    private static readonly string ClientSecret = Env("EOS_CLIENT_SECRET", "wBPaPmSI7dWUt87+nvs2pp7TeQVFXSDz+/PnSdYDyc0");
    private static readonly string GameName = Env("FUSION_GAME_NAME", "BONELAB");
    private static readonly int ListenSec = int.TryParse(Env("LISTEN_SEC", "12"), out var s) ? Math.Clamp(s, 3, 60) : 12;
    private static readonly int MaxJoinTries = int.TryParse(Env("MAX_JOIN_TRIES", "5"), out var m) ? Math.Clamp(m, 1, 20) : 5;

    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static readonly ConcurrentQueue<Action> MainQueue = new();
    private static readonly object AudioLock = new();
    private static long _audioFrames;
    private static long _audioSamples;
    private static readonly HashSet<string> Speakers = new(StringComparer.Ordinal);

    private static string Env(string k, string d)
        => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

    private static int Main(string[] args)
    {
        // Default: full Fusion P2P voice path.
        // --host = headless CreateLobby listing (ReallyWorld experiment)
        // --rtc-only = old Lobby-RTC probe
        if (args.Any(a => a == "--host") || Env("FUSION_HOST_MODE", "0") == "1")
            return FusionHostBot.Run();
        if (args.Any(a => a == "--rtc-only"))
            return RunRtcOnlyProbe();
        return FusionVoiceBot.Run();
    }

    private static int RunRtcOnlyProbe()
    {
        InstallNativeResolver();
        string data = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(data);
        string cache = Path.Combine(data, "eos-cache");
        Directory.CreateDirectory(cache);
        string key = RandomHex(64);

        Console.WriteLine("[probe] EOS JoinLobby+RTC substitute (headless VPS)");
        if (!InitEos(cache, key)) return 2;
        if (!LoginDeviceId()) return 3;
        Console.WriteLine($"[probe] logged in as {_localUser}");

        var rtc = _platform.GetRTCInterface();
        Console.WriteLine($"[probe] GetRTCInterface={(rtc != null ? "ok" : "null")} audio={(rtc?.GetAudioInterface() != null ? "ok" : "null")}");
        HookRtcAudio();

        var candidates = FindPublicLobbyDetails(Math.Max(MaxJoinTries * 3, 15));
        Console.WriteLine($"[probe] candidate lobbies={candidates.Count} rtcEnabled={candidates.Count(c => c.RtcEnabled)}");

        int tried = 0;
        int joinedOk = 0;
        foreach (var c in candidates)
        {
            if (tried >= MaxJoinTries) break;
            tried++;
            Console.WriteLine(
                $"[probe] try#{tried} lobbyId={c.LobbyId} rtcEnabled={c.RtcEnabled} " +
                $"members={c.MaxMembers - c.AvailableSlots}/{c.MaxMembers} name={c.Name}");

            if (!JoinWithRtc(c))
            {
                SafeRelease(c.Details);
                continue;
            }

            joinedOk++;
            SafeRelease(c.Details);

            ProbeRtcState(c.LobbyId);
            Console.WriteLine($"[probe] listening {ListenSec}s for RTC audio…");
            _audioFrames = 0;
            _audioSamples = 0;
            lock (AudioLock) Speakers.Clear();
            Pump(ListenSec);

            Console.WriteLine(
                $"[probe] RESULT join=OK frames={_audioFrames} samples={_audioSamples} " +
                $"speakers={Speakers.Count} ids=[{string.Join(",", Speakers.Take(8))}]");

            Leave(c.LobbyId);

            if (_audioFrames > 0)
            {
                Console.WriteLine("[probe] SUCCESS — EOS RTC audio received on VPS join substitute");
                return 0;
            }

            Console.WriteLine("[probe] joined but no RTC audio — next lobby");
        }

        foreach (var c in candidates) SafeRelease(c.Details);

        if (joinedOk > 0 && _audioFrames == 0)
        {
            Console.WriteLine(
                $"[probe] PARTIAL — JoinLobby OK ({joinedOk}x) but 0 RTC audio frames. " +
                "Подмена join на VPS работает как член лобби; голос Fusion не в EOS RTC.");
            return 5;
        }

        Console.WriteLine("[probe] FAIL — could not JoinLobby / no candidates");
        return 4;
    }

    private static void InstallNativeResolver()
    {
        string so = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "native", "libEOSSDK-Linux-Shipping.so"));
        if (!File.Exists(so))
            throw new FileNotFoundException(so);
        NativeLibrary.SetDllImportResolver(typeof(Bindings).Assembly, (name, _, _) =>
        {
            if (name != null && name.IndexOf("EOSSDK", StringComparison.OrdinalIgnoreCase) >= 0)
                return NativeLibrary.Load(so);
            return IntPtr.Zero;
        });
    }

    private static bool InitEos(string cache, string encKey)
    {
        var init = new InitializeOptions { ProductName = "Fusion", ProductVersion = "0.0.1" };
        Result r = PlatformInterface.Initialize(ref init);
        if (r != Result.Success && r != Result.AlreadyConfigured)
        {
            Console.Error.WriteLine("EOS_Initialize: " + r);
            return false;
        }

        LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Info);

        // First try with RTCOptions (needed for Lobby voice). Fallback without — join still testable.
        _platform = CreatePlatform(cache, encKey, withRtc: true);
        if (_platform == null)
        {
            Console.Error.WriteLine("[probe] Platform.Create(RTC) null — retry without RTCOptions");
            _platform = CreatePlatform(cache, encKey, withRtc: false);
        }
        if (_platform == null)
        {
            Console.Error.WriteLine("Platform.Create null");
            return false;
        }
        Console.WriteLine("[probe] platform created");
        return true;
    }

    private static PlatformInterface CreatePlatform(string cache, string encKey, bool withRtc)
    {
        var options = new Options
        {
            ProductId = ProductId,
            SandboxId = SandboxId,
            DeploymentId = DeploymentId,
            ClientCredentials = new ClientCredentials { ClientId = ClientId, ClientSecret = ClientSecret },
            Flags = PlatformFlags.DisableOverlay | PlatformFlags.DisableSocialOverlay,
            TickBudgetInMilliseconds = 0,
            CacheDirectory = cache,
            EncryptionKey = encKey,
        };
        if (withRtc)
            options.RTCOptions = new RTCOptions { BackgroundMode = RTCBackgroundMode.KeepRoomsAlive };
        return PlatformInterface.Create(ref options);
    }

    private static bool LoginDeviceId()
    {
        var connect = _platform.GetConnectInterface();
        var createOpts = new CreateDeviceIdOptions { DeviceModel = "EosJoinProbe/" + Environment.MachineName };
        bool done = false;
        Result cr = Result.UnexpectedError;
        connect.CreateDeviceId(ref createOpts, null, (ref CreateDeviceIdCallbackInfo d) => { cr = d.ResultCode; done = true; });
        PumpUntil(() => done, 30);
        Console.WriteLine("[probe] CreateDeviceId: " + cr);
        if (cr != Result.Success && cr != Result.DuplicateNotAllowed) return false;

        done = false;
        Result lr = Result.UnexpectedError;
        ContinuanceToken continuance = null;
        ProductUserId user = null;
        var loginOpts = new LoginOptions
        {
            Credentials = new Credentials { Type = ExternalCredentialType.DeviceidAccessToken, Token = "" },
            UserLoginInfo = new UserLoginInfo { DisplayName = "bonelab.fun" },
        };
        connect.Login(ref loginOpts, null, (ref LoginCallbackInfo d) =>
        {
            lr = d.ResultCode;
            if (d.ResultCode == Result.Success) user = d.LocalUserId;
            else if (d.ResultCode == Result.InvalidUser) continuance = d.ContinuanceToken;
            done = true;
        });
        PumpUntil(() => done, 30);
        Console.WriteLine("[probe] Login: " + lr);

        if (lr == Result.InvalidUser && continuance != null)
        {
            done = false;
            Result cu = Result.UnexpectedError;
            var cuOpts = new CreateUserOptions { ContinuanceToken = continuance };
            connect.CreateUser(ref cuOpts, null, (ref CreateUserCallbackInfo d) =>
            {
                cu = d.ResultCode;
                if (d.ResultCode == Result.Success) user = d.LocalUserId;
                done = true;
            });
            PumpUntil(() => done, 30);
            Console.WriteLine("[probe] CreateUser: " + cu);
            if (cu != Result.Success) return false;
        }
        else if (lr != Result.Success) return false;

        _localUser = user;
        return _localUser != null;
    }

    private sealed class Cand
    {
        public LobbyDetails Details;
        public string LobbyId;
        public bool RtcEnabled;
        public uint AvailableSlots;
        public uint MaxMembers;
        public string Name;
    }

    private static List<Cand> FindPublicLobbyDetails(int want)
    {
        var list = new List<Cand>();
        var lobby = _platform.GetLobbyInterface();
        var createOpts = new CreateLobbySearchOptions { MaxResults = 50 };
        if (lobby.CreateLobbySearch(ref createOpts, out LobbySearch search) != Result.Success || search == null)
            return list;

        try
        {
            void Set(string key, string val)
            {
                var a = new AttributeData { Key = key, Value = new AttributeDataValue { AsUtf8 = val } };
                var o = new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = a };
                search.SetParameter(ref o);
            }

            // Same keys as eos-lobby-scraper (Fusion Quest).
            Set("Game", GameName);
            Set("Privacy", "0");
            Set("Full", "False");

            bool done = false;
            Result fr = Result.UnexpectedError;
            var findOpts = new LobbySearchFindOptions { LocalUserId = _localUser };
            search.Find(ref findOpts, null, (ref LobbySearchFindCallbackInfo info) =>
            {
                fr = info.ResultCode;
                done = true;
            });
            PumpUntil(() => done, 45);
            Console.WriteLine($"[probe] Find public: {fr}");
            if (fr != Result.Success) return list;

            var countOpts = default(LobbySearchGetSearchResultCountOptions);
            uint count = search.GetSearchResultCount(ref countOpts);
            Console.WriteLine($"[probe] find count={count}");

            // NOTE: LobbyDetails.CopyInfo SIGSEGV on many Quest Fusion lobbies (same as scraper).
            // Join via LobbyDetails handle; LobbyId comes from JoinLobby callback.
            for (uint i = 0; i < count && list.Count < want; i++)
            {
                var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success || details == null)
                    continue;

                string name = GetAttr(details, "LobbyName")
                              ?? GetAttr(details, "LobbyInfo")
                              ?? ("idx-" + i);
                // Prefer lobbies that advertise a LobbyCode (joinable / real session).
                string code = GetAttr(details, "LobbyCode");
                list.Add(new Cand
                {
                    Details = details,
                    LobbyId = code ?? ("pending-" + i),
                    RtcEnabled = false, // unknown without CopyInfo
                    AvailableSlots = 1,
                    MaxMembers = 0,
                    Name = Sanitize(name, 48),
                });
            }
        }
        finally
        {
            search.Release();
        }

        return list;
    }

    private static bool JoinWithRtc(Cand c)
    {
        var lobby = _platform.GetLobbyInterface();
        bool done = false;
        Result jr = Result.UnexpectedError;
        string joinedId = null;

        // Pass 1: join without LocalRTCOptions (null) — avoids RTC options version mismatch.
        // Pass 2: with manual audio I/O if available.
        foreach (bool withRtcOpts in new[] { false, true })
        {
            done = false;
            jr = Result.UnexpectedError;
            joinedId = null;
            var opts = new JoinLobbyOptions
            {
                LobbyDetailsHandle = c.Details,
                LocalUserId = _localUser,
                PresenceEnabled = false,
                RTCRoomJoinActionType = withRtcOpts
                    ? LobbyRTCRoomJoinActionType.AutomaticJoin
                    : LobbyRTCRoomJoinActionType.AutomaticJoin,
                LocalRTCOptions = withRtcOpts
                    ? new LocalRTCOptions
                    {
                        Flags = 0,
                        UseManualAudioInput = true,
                        UseManualAudioOutput = true,
                        LocalAudioDeviceInputStartsMuted = true,
                    }
                    : null,
            };

            lobby.JoinLobby(ref opts, null, (ref JoinLobbyCallbackInfo info) =>
            {
                jr = info.ResultCode;
                joinedId = info.LobbyId;
                done = true;
            });
            PumpUntil(() => done, 45);
            Console.WriteLine($"[probe] JoinLobby(rtcOpts={withRtcOpts}): {jr} lobbyId={joinedId}");
            if (jr == Result.Success) break;
            if (jr != Result.IncompatibleVersion) break;
        }

        if (jr != Result.Success) return false;
        if (!string.IsNullOrEmpty(joinedId))
            c.LobbyId = joinedId;
        return true;
    }

    private static void ProbeRtcState(string lobbyId)
    {
        var lobby = _platform.GetLobbyInterface();
        var nameOpts = new GetRTCRoomNameOptions { LobbyId = lobbyId, LocalUserId = _localUser };
        Result nr = lobby.GetRTCRoomName(ref nameOpts, out Utf8String roomName);
        Console.WriteLine($"[probe] GetRTCRoomName: {nr} room='{roomName}'");

        var connOpts = new IsRTCRoomConnectedOptions { LobbyId = lobbyId, LocalUserId = _localUser };
        Result cr = lobby.IsRTCRoomConnected(ref connOpts, out bool connected);
        Console.WriteLine($"[probe] IsRTCRoomConnected: {cr} connected={connected}");

        // Also try explicit RTC interface participant notify
        try
        {
            var rtc = _platform.GetRTCInterface();
            if (rtc != null && roomName != null)
            {
                var pOpts = new AddNotifyParticipantStatusChangedOptions
                {
                    LocalUserId = _localUser,
                    RoomName = roomName,
                };
                ulong nid = rtc.AddNotifyParticipantStatusChanged(ref pOpts, null,
                    (ref ParticipantStatusChangedCallbackInfo info) =>
                    {
                        Console.WriteLine(
                            $"[probe] RTC participant {info.ParticipantId} status={info.ParticipantStatus}");
                    });
                Console.WriteLine($"[probe] participant notify id={nid}");
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[probe] rtc notify: " + e.Message);
        }
    }

    private static void HookRtcAudio()
    {
        try
        {
            var rtc = _platform.GetRTCInterface();
            var audio = rtc?.GetAudioInterface();
            if (audio == null)
            {
                Console.Error.WriteLine("[probe] RTCAudio interface null");
                return;
            }

            // RoomName null/empty = all rooms (per EOS docs for some notify APIs)
            // UnmixedAudio=true → per-participant buffers (needed to map pid → language).
            var opts = new AddNotifyAudioBeforeRenderOptions
            {
                LocalUserId = _localUser,
                RoomName = null,
                UnmixedAudio = true,
            };
            ulong id = audio.AddNotifyAudioBeforeRender(ref opts, null,
                (ref AudioBeforeRenderCallbackInfo info) =>
                {
                    var buf = info.Buffer;
                    short[] frames = buf?.Frames;
                    if (frames == null || frames.Length == 0) return;
                    Interlocked.Add(ref _audioFrames, 1);
                    Interlocked.Add(ref _audioSamples, frames.Length);
                    string pid = info.ParticipantId?.ToString() ?? "?";
                    lock (AudioLock) Speakers.Add(pid);
                });
            Console.WriteLine($"[probe] AudioBeforeRender notify={id}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[probe] HookRtcAudio: " + e.Message);
        }
    }

    private static void Leave(string lobbyId)
    {
        try
        {
            var lobby = _platform.GetLobbyInterface();
            bool done = false;
            Result lr = Result.UnexpectedError;
            var opts = new LeaveLobbyOptions { LocalUserId = _localUser, LobbyId = lobbyId };
            lobby.LeaveLobby(ref opts, null, (ref LeaveLobbyCallbackInfo info) =>
            {
                lr = info.ResultCode;
                done = true;
            });
            PumpUntil(() => done, 20);
            Console.WriteLine("[probe] LeaveLobby: " + lr);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[probe] leave: " + e.Message);
        }
    }

    private static string GetAttr(LobbyDetails details, string key)
    {
        try
        {
            var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key };
            if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) != Result.Success || attr == null)
                return null;
            return attr.Value.Data?.Value.AsUtf8;
        }
        catch
        {
            return null;
        }
    }

    private static void SafeRelease(LobbyDetails d)
    {
        try { d?.Release(); } catch { /* */ }
    }

    private static string Sanitize(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "?";
        s = s.Replace('\n', ' ').Trim();
        return s.Length <= max ? s : s.Substring(0, max);
    }

    private static string RandomHex(int chars)
    {
        var buf = RandomNumberGenerator.GetBytes(chars / 2);
        var sb = new StringBuilder(chars);
        foreach (byte b in buf) sb.Append(b.ToString("x2"));
        return sb.ToString();
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
        if (!done()) throw new TimeoutException("EOS callback timed out");
    }
}
