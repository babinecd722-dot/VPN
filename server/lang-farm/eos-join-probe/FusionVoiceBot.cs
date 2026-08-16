using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.P2P;
using Epic.OnlineServices.Platform;

namespace EosJoinProbe;

/// <summary>
/// Headless Quest-Fusion language farm bot:
/// DeviceId → JoinLobby → Fusion P2P socket → decode tag67 voice → Whisper → languages.txt
/// </summary>
internal static class FusionVoiceBot
{
    private static readonly string ProductId = Env("EOS_PRODUCT_ID", "29e074d5b4724f3bb01f26b7e33d2582");
    private static readonly string SandboxId = Env("EOS_SANDBOX_ID", "26f32d66d87f4dfeb4a7449b776a41f1");
    // Fusion 0.2.1 LabFusion.dll — DeploymentId/ClientSecret rotated vs 0.2.0.
    private static readonly string DeploymentId = Env("EOS_DEPLOYMENT_ID", "f3fdf691aa6c4004abdb1e19665c1429");
    private static readonly string ClientId = Env("EOS_CLIENT_ID", "xyza78915hKqxe2TNTavpq2sxBDvJ9AH");
    private static readonly string ClientSecret = Env("EOS_CLIENT_SECRET", "SWDxYlWWsEgvmD0o3qAm2RMZoSZzOfYo5yvX/uikH94");
    private static readonly string GameName = Env("FUSION_GAME_NAME", "BONELAB");
    private static readonly int ListenSec = int.TryParse(Env("LISTEN_SEC", "35"), out var s) ? Math.Clamp(s, 5, 180) : 35;
    // Leave truly dead lobbies fast (no P2P at all). If packets flow, wait full LISTEN_SEC for voice.
    private static readonly int NoVoiceAbortSec = int.TryParse(Env("NO_VOICE_ABORT_SEC", "22"), out var nv) ? Math.Clamp(nv, 8, 120) : 22;
    private static readonly int MaxJoinTries = int.TryParse(Env("MAX_JOIN_TRIES", "8"), out var m) ? Math.Clamp(m, 1, 20) : 8;
    private static readonly string DetectUrl = Env("DETECT_URL", "http://127.0.0.1:8091/detect");
    private static readonly string ResultsTxt = Env("RESULTS_TXT", "/tmp/lang-farm/results/languages.txt");
    private static readonly string SessionDir = Env("SESSION_DIR", "/tmp/lang-farm/results/session");
    private static readonly string ClaimDir = Env("LOBBY_CLAIM_DIR", "/tmp/lang-farm/state/lobby_claims");
    private static readonly float MinConfidence = float.TryParse(Env("MIN_CONFIDENCE", "0.35"), out var c) ? c : 0.35f;
    // ≥1.5s @48k — 2.0s skipped too many real clips (81920–94464 samples)
    private static readonly int MinVoiceSamples = int.TryParse(Env("MIN_VOICE_SAMPLES", "72000"), out var mv) ? mv : 72000;

    private const byte TagVoice = 67;
    private const byte TagConnectionRequest = 1;
    private const byte TagConnectionResponse = 2;
    private const byte ClientChannel = 1;
    private const byte ServerChannel = 2;

    private static PlatformInterface _platform;
    private static ProductUserId _localUser;
    private static EosIdentity _identity;
    // Fusion 0.2.0 renamed P2P socket FusionSocket → Fusion.
    private static readonly SocketId FusionSocket = new() { SocketName = "Fusion" };
    private static readonly ConcurrentQueue<Action> MainQueue = new();
    private static readonly bool ForceNewAccount = Env("EOS_FORCE_NEW_ACCOUNT", "0") == "1";
    private static readonly string BotNick = Env("BOT_NICK", EosIdentity.DefaultDisplayName);

    private static long _packetsIn;
    private static long _voicePackets;
    private static long _voiceBytes;
    private static long _voiceDecoded;
    private static long _p2pEstablished;
    private static long _p2pRequests;
    private static readonly Dictionary<byte, int> TagHist = new();
    private static readonly HashSet<string> VoicePeers = new(StringComparer.Ordinal);
    private static readonly Dictionary<byte, PlayerRec> Players = new();
    // LobbyInfo has no SmallID — keep by pid until ConnectionResponse maps sid correctly.
    private static readonly Dictionary<string, PlayerRec> PendingByPid = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<byte, List<short>> PcmBySmall = new();
    private static readonly object StatLock = new();
    private static volatile bool _connected;
    private static string _currentLobbyId = "";
    private static long _fragSkipped;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private sealed class PlayerRec
    {
        public string PlatformId;
        public string Username;
        public string Nickname;
    }

    private static string Env(string k, string d)
        => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

    public static int Run()
    {
        InstallNativeResolver();
        string data = Env("EOS_DATA_DIR", Path.Combine(AppContext.BaseDirectory, "data-voice"));
        Directory.CreateDirectory(data);
        string cache = Path.Combine(data, "eos-cache");
        Directory.CreateDirectory(cache);
        Directory.CreateDirectory(Path.GetDirectoryName(ResultsTxt)!);
        Directory.CreateDirectory(SessionDir);

        Console.WriteLine("[langfarm] Fusion P2P voice → language detect → txt");
        Console.WriteLine($"[langfarm] results={ResultsTxt}");
        Console.WriteLine($"[langfarm] EOS_FORCE_NEW_ACCOUNT={ForceNewAccount} data={data}");

        try
        {
            _identity = EosIdentity.LoadOrMint(data, forceNew: ForceNewAccount);
            // Fresh cache dir for brand-new DeviceId/PUID
            if (ForceNewAccount)
            {
                try
                {
                    if (Directory.Exists(cache) && cache != _identity.CacheDirectory)
                        Directory.Delete(cache, recursive: true);
                }
                catch { /* */ }
            }
            _platform = EosIdentity.CreatePlatform(_identity);
            Console.WriteLine("[langfarm] platform ok");
            _localUser = EosIdentity.LoginDeviceAccount(
                _platform,
                _identity,
                forceNew: ForceNewAccount || _identity.FreshMint,
                tick: () => _platform?.Tick());
            EosIdentity.Persist(_identity, EosIdentity.IdentityPath(data));
            Console.WriteLine($"[langfarm] logged in as {_localUser} install={_identity.InstallId}");
            File.AppendAllText(
                Path.Combine(Path.GetDirectoryName(ResultsTxt)!, "bot_identities.txt"),
                $"{DateTime.UtcNow:O}\tbot_data={data}\tinstall={_identity.InstallId}\tpuid={_localUser}\tnick={BotNick}\n");
            // Ensure bot is visible in site DB (parallel farm: mark online on login)
            // OFFLINE in presence DB — language farm must not create IN GAME ghosts.
            RegisterBotInClientDb(_localUser.ToString(), BotNick, "OFFLINE");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[langfarm] EOS identity/login failed: " + e.Message);
            return SafeExit(3);
        }

        var p2p = _platform.GetP2PInterface();
        if (p2p == null)
        {
            Console.Error.WriteLine("[langfarm] P2P interface NULL");
            return SafeExit(6);
        }
        ConfigureP2P(p2p);
        RegisterP2P(p2p);

        var candidates = FindPublicLobbies(MaxJoinTries * 4);
        Console.WriteLine($"[langfarm] candidates={candidates.Count}");

        int joined = 0;
        int detections = 0;
        foreach (var details in candidates)
        {
            if (joined >= MaxJoinTries) { SafeRelease(details); continue; }

            IngestLobbyInfo(details);
            // Never join our own visual host (www·bonelab·fun) — farm only foreign lobbies.
            string forceCodeJoin = Environment.GetEnvironmentVariable("FORCE_LOBBY_CODE");
            bool forceJoinOwn = !string.IsNullOrEmpty(forceCodeJoin)
                && string.Equals(GetLobbyAttr(details, "LobbyCode"), forceCodeJoin, StringComparison.OrdinalIgnoreCase);
            if (!forceJoinOwn && IsOwnVisualHost(details, out string hostSkipReason))
            {
                Console.WriteLine($"[langfarm] skip own-host ({hostSkipReason})");
                SafeRelease(details);
                continue;
            }
            // Anti-stack: skip lobbies already claimed by another farm bot / already full of bonelab.fun
            string previewId = TryGetLobbyId(details);
            int ourBots = CountNickInLobbyInfo(details, BotNick);
            if (ourBots >= 1 || (!string.IsNullOrEmpty(previewId) && !TryClaimLobby(previewId)))
            {
                Console.WriteLine($"[langfarm] skip lobby={previewId} stacked ourBots≈{ourBots}");
                SafeRelease(details);
                continue;
            }

            ResetStats();
            if (!JoinLobby(details, out string lobbyId))
            {
                ReleaseClaim(previewId);
                SafeRelease(details);
                continue;
            }
            joined++;
            _currentLobbyId = lobbyId ?? previewId ?? "";
            if (!string.IsNullOrEmpty(lobbyId) && lobbyId != previewId)
                TryClaimLobby(lobbyId); // claim real id too
            SafeRelease(details);
            Console.WriteLine($"[langfarm] joined lobby={lobbyId}");
            RegisterBotInClientDb(_localUser.ToString(), BotNick, "OFFLINE");

            ProductUserId owner = GetLobbyOwner(lobbyId);
            Console.WriteLine($"[langfarm] owner={owner}");
            if (owner != null)
            {
                Console.WriteLine($"[langfarm] AcceptConnection: {Accept(owner)}");
                Console.WriteLine($"[langfarm] poke: {SendRaw(owner, BuildNetMessage(0, Array.Empty<byte>()), true, true)}");
                Console.WriteLine($"[langfarm] connectionRequest: {SendRaw(owner, BuildMinimalConnectionRequest(), true, true)}");
            }

            Console.WriteLine(
                $"[langfarm] listening up to {ListenSec}s (dead-lobby abort@{NoVoiceAbortSec}s if no P2P)…");
            var until = DateTime.UtcNow.AddSeconds(ListenSec);
            var started = DateTime.UtcNow;
            bool abortedMute = false;
            try
            {
                while (DateTime.UtcNow < until)
                {
                    try { _platform.Tick(); } catch { /* */ }
                    DrainP2P(p2p);
                    while (MainQueue.TryDequeue(out var a))
                    {
                        try { a(); } catch (Exception e) { Console.Error.WriteLine(e.Message); }
                    }
                    double elapsed = (DateTime.UtcNow - started).TotalSeconds;
                    // Only early-exit when the lobby is truly dead (no packets). If P2P is alive,
                    // stay for full LISTEN_SEC — voice often starts after connection settle.
                    if (_voicePackets == 0 && _packetsIn == 0 && elapsed >= NoVoiceAbortSec)
                    {
                        abortedMute = true;
                        Console.WriteLine(
                            $"[langfarm] dead-lobby abort after {NoVoiceAbortSec}s (no P2P) — next lobby");
                        break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[langfarm] listen loop: " + e.Message);
            }

            PrintStats();
            if (!abortedMute || _voiceDecoded > 0 || _voicePackets > 0)
                detections += ProcessSessionAndDetect();

            try
            {
                var close = new CloseConnectionsOptions { LocalUserId = _localUser, SocketId = FusionSocket };
                Console.WriteLine("[langfarm] CloseConnections: " + p2p.CloseConnections(ref close));
            }
            catch (Exception e) { Console.Error.WriteLine("[langfarm] close: " + e.Message); }
            Leave(lobbyId);
            ReleaseClaim(lobbyId);
            ReleaseClaim(previewId);
            RegisterBotInClientDb(_localUser.ToString(), BotNick, "OFFLINE");

            // Keep the EOS session alive across lobbies — do not teardown after one hit.
            // (Platform.Release / process exit runs EOSSDK atexit and SIGSEGVs on Linux.)
            if (detections > 0)
                Console.WriteLine($"[langfarm] hit — {detections} confident language update(s) so far → {ResultsTxt}");
            if (_voiceDecoded > 0)
                Console.WriteLine("[langfarm] decoded voice but not confident — next lobby");
            else if (_voicePackets > 0)
                Console.WriteLine("[langfarm] voice packets but decode failed — next lobby");
            else if (_packetsIn > 0)
                Console.WriteLine("[langfarm] P2P ok, no voice yet — next lobby");
        }

        if (detections > 0)
            Console.WriteLine($"[langfarm] SUCCESS — {detections} confident language update(s) → {ResultsTxt}");
        else
            Console.WriteLine("[langfarm] no confident language detections this run");

        // Soft leave/close, then hard-exit so EOSSDK shutdown handlers never run.
        SoftCleanup();
        return SafeExit(detections > 0 ? 0 : (_voicePackets > 0 || _packetsIn > 0 ? 5 : 4));
    }

    private static void ResetStats()
    {
        _packetsIn = _voicePackets = _voiceBytes = _voiceDecoded = _p2pEstablished = _p2pRequests = 0;
        _fragSkipped = 0;
        _connected = false;
        _currentLobbyId = "";
        lock (StatLock)
        {
            TagHist.Clear();
            VoicePeers.Clear();
            PcmBySmall.Clear();
            Players.Clear();
            PendingByPid.Clear();
        }
    }

    private static void PrintStats()
    {
        Console.WriteLine(
            $"[langfarm] stats packets={_packetsIn} voicePkts={_voicePackets} decoded={_voiceDecoded} " +
            $"voiceBytes={_voiceBytes} established={_p2pEstablished} connected={_connected} fragSkip={_fragSkipped}");
        lock (StatLock)
        {
            if (TagHist.Count > 0)
                Console.WriteLine("[langfarm] tags: " + string.Join(", ", TagHist.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}:{kv.Value}")));
            if (Players.Count > 0)
                Console.WriteLine("[langfarm] players: " + string.Join("; ", Players.Select(kv =>
                    $"sid={kv.Key} pid={kv.Value.PlatformId} user={kv.Value.Username}")));
            foreach (var kv in PcmBySmall)
                Console.WriteLine($"[langfarm] pcm sid={kv.Key} samples={kv.Value.Count} sec={kv.Value.Count / (double)VoiceCodec.SampleRate:F2}");
        }
    }

    private static int ProcessSessionAndDetect()
    {
        Dictionary<byte, short[]> clips;
        Dictionary<byte, PlayerRec> players;
        lock (StatLock)
        {
            clips = PcmBySmall.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
            players = Players.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        int wrote = 0;
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string sess = Path.Combine(SessionDir, $"{stamp}_{_currentLobbyId}");
        Directory.CreateDirectory(sess);

        foreach (var kv in clips.OrderBy(x => x.Key))
        {
            byte sid = kv.Key;
            short[] pcm = kv.Value;
            if (pcm.Length < MinVoiceSamples)
            {
                Console.WriteLine($"[langfarm] skip sid={sid} too short ({pcm.Length} samples)");
                continue;
            }

            // RMS gate
            double sum = 0;
            for (int i = 0; i < pcm.Length; i++) sum += (double)pcm[i] * pcm[i];
            double rms = Math.Sqrt(sum / pcm.Length);
            if (rms < 80)
            {
                Console.WriteLine($"[langfarm] skip sid={sid} silence rms={rms:F1}");
                continue;
            }

            players.TryGetValue(sid, out var rec);
            string pid = rec?.PlatformId ?? $"unknown-sid-{sid}";
            string user = rec?.Username ?? rec?.Nickname ?? "?";
            string wav = Path.Combine(sess, $"sid{sid}_{pid}.wav");
            VoiceCodec.WriteWav(wav, pcm);
            Console.WriteLine($"[langfarm] wav sid={sid} pid={pid} user={user} sec={pcm.Length / (double)VoiceCodec.SampleRate:F2} rms={rms:F0} → {wav}");

            var det = DetectWav(wav);
            string lang = det.GetValueOrDefault("language", "unknown");
            string confStr = det.GetValueOrDefault("confidence", "0");
            string text = det.GetValueOrDefault("text", "");
            string ok = det.GetValueOrDefault("ok", "false");
            Console.WriteLine($"[langfarm] detect sid={sid} lang={lang} conf={confStr} ok={ok} text={text}");

            float.TryParse(confStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float conf);
            bool isOk = ok is "True" or "true" or "1";
            bool isSelf =
                string.Equals(pid, _localUser?.ToString(), StringComparison.OrdinalIgnoreCase) ||
                IsFarmNick(user) ||
                (user?.StartsWith("primex-host.", StringComparison.OrdinalIgnoreCase) ?? false);
            // Always append a line for audit (even low conf); SUCCESS counts only confident DB writes.
            AppendResult(pid, user, lang, conf, text, isOk, sid, wav);
            if (isSelf)
            {
                Console.WriteLine($"[langfarm] skip self language write sid={sid} pid={pid}");
            }
            else if (isOk && !pid.StartsWith("unknown-", StringComparison.Ordinal))
            {
                UpdatePlayerLanguage(pid, lang, conf);
                wrote++;
            }
        }

        File.AppendAllText(Path.Combine(sess, "summary.txt"),
            $"lobby={_currentLobbyId}\nvoicePkts={_voicePackets}\ndecoded={_voiceDecoded}\nclips={clips.Count}\nconfident={wrote}\n");
        return wrote;
    }

    private static Dictionary<string, string> DetectWav(string wavPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["language"] = "unknown",
            ["confidence"] = "0",
            ["text"] = "",
            ["ok"] = "false",
        };
        try
        {
            using var form = new MultipartFormDataContent();
            var bytes = File.ReadAllBytes(wavPath);
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            form.Add(content, "file", Path.GetFileName(wavPath));
            string url = $"{DetectUrl}?min_confidence={MinConfidence:0.##}&sr={VoiceCodec.SampleRate}";
            using var resp = Http.PostAsync(url, form).GetAwaiter().GetResult();
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                Console.Error.WriteLine($"[langfarm] detect HTTP {(int)resp.StatusCode}: {body}");
                // fallback: local python one-shot
                return DetectLocalFallback(wavPath);
            }
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("language", out var l)) result["language"] = l.GetString() ?? "unknown";
            if (root.TryGetProperty("confidence", out var c)) result["confidence"] = c.ToString();
            if (root.TryGetProperty("text", out var t)) result["text"] = t.GetString() ?? "";
            if (root.TryGetProperty("ok", out var o)) result["ok"] = o.ValueKind == JsonValueKind.True ? "true" : o.ToString();
            return result;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[langfarm] detect error: " + e.Message);
            return DetectLocalFallback(wavPath);
        }
    }

    private static Dictionary<string, string> DetectLocalFallback(string wavPath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["language"] = "unknown",
            ["confidence"] = "0",
            ["text"] = "",
            ["ok"] = "false",
        };
        try
        {
            string py = "/tmp/lang-farm/langdetect/detect_oneshot.py";
            if (!File.Exists(py)) return result;
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{py}\" \"{wavPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using var p = System.Diagnostics.Process.Start(psi);
            string stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit(120000);
            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;
            if (root.TryGetProperty("language", out var l)) result["language"] = l.GetString() ?? "unknown";
            if (root.TryGetProperty("confidence", out var c)) result["confidence"] = c.ToString();
            if (root.TryGetProperty("text", out var t)) result["text"] = t.GetString() ?? "";
            if (root.TryGetProperty("ok", out var o)) result["ok"] = o.ValueKind == JsonValueKind.True ? "true" : o.ToString();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[langfarm] oneshot fallback: " + e.Message);
        }
        return result;
    }

    private static void AppendResult(string pid, string user, string lang, float conf, string text, bool ok, byte sid, string wav)
    {
        string line =
            $"{DateTime.UtcNow:O}\tpid={pid}\tuser={Sanitize(user)}\tlang={lang}\tconf={conf:F3}\tok={(ok ? 1 : 0)}\t" +
            $"sid={sid}\tlobby={_currentLobbyId}\twav={wav}\ttext={Sanitize(text)}\n";
        File.AppendAllText(ResultsTxt, line);
        Console.WriteLine("[langfarm] TXT << " + line.TrimEnd());
    }

    private static string Sanitize(string s)
        => (s ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');

    private static void ConfigureP2P(P2PInterface p2p)
    {
        var port = new SetPortRangeOptions { Port = 7777, MaxAdditionalPortsToTry = 99 };
        Console.WriteLine("[langfarm] SetPortRange: " + p2p.SetPortRange(ref port));
        var relay = new SetRelayControlOptions { RelayControl = RelayControl.ForceRelays };
        Console.WriteLine("[langfarm] SetRelayControl(ForceRelays): " + p2p.SetRelayControl(ref relay));
    }

    private static void RegisterP2P(P2PInterface p2p)
    {
        var reqOpts = new AddNotifyPeerConnectionRequestOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionRequest(ref reqOpts, null, (ref OnIncomingConnectionRequestInfo info) =>
        {
            Interlocked.Increment(ref _p2pRequests);
            if (info.RemoteUserId != null)
                Console.WriteLine($"[langfarm] inbound request from {info.RemoteUserId} accept={Accept(info.RemoteUserId)}");
        });

        var estOpts = new AddNotifyPeerConnectionEstablishedOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionEstablished(ref estOpts, null, (ref OnPeerConnectionEstablishedInfo info) =>
        {
            Interlocked.Increment(ref _p2pEstablished);
            _connected = true;
            Console.WriteLine($"[langfarm] P2P established with {info.RemoteUserId}");
            if (info.RemoteUserId != null)
                SendRaw(info.RemoteUserId, BuildMinimalConnectionRequest(), reliable: true, toServer: true);
        });

        var cloOpts = new AddNotifyPeerConnectionClosedOptions { LocalUserId = _localUser, SocketId = FusionSocket };
        p2p.AddNotifyPeerConnectionClosed(ref cloOpts, null, (ref OnRemoteConnectionClosedInfo info) =>
        {
            Console.WriteLine($"[langfarm] P2P closed with {info.RemoteUserId} reason={info.Reason}");
        });
    }

    private static Result Accept(ProductUserId remote)
    {
        var p2p = _platform.GetP2PInterface();
        var opts = new AcceptConnectionOptions
        {
            LocalUserId = _localUser,
            RemoteUserId = remote,
            SocketId = FusionSocket,
        };
        return p2p.AcceptConnection(ref opts);
    }

    private static Result SendRaw(ProductUserId remote, byte[] data, bool reliable, bool toServer)
    {
        var p2p = _platform.GetP2PInterface();
        // Fusion 0.1.x: KindSingle prefix on every datagram (see FragmentHeader.KindPrefixSize).
        byte[] packet = FusionP2PWire.WrapSingle(data);
        var opts = new SendPacketOptions
        {
            LocalUserId = _localUser,
            RemoteUserId = remote,
            SocketId = FusionSocket,
            Channel = toServer ? ServerChannel : ClientChannel,
            Data = new ArraySegment<byte>(packet),
            AllowDelayedDelivery = false,
            Reliability = reliable ? PacketReliability.ReliableUnordered : PacketReliability.UnreliableUnordered,
            DisableAutoAcceptConnection = false,
        };
        return p2p.SendPacket(ref opts);
    }

    private static void DrainP2P(P2PInterface p2p)
    {
        for (int i = 0; i < 200; i++)
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
            if (p2p.ReceivePacket(ref recv, ref peer, ref sock, out byte channel, data, out uint written) != Result.Success || written == 0)
                break;

            Interlocked.Increment(ref _packetsIn);
            HandlePacket(peer, channel, buf, (int)written);
        }
    }

    private static void HandlePacket(ProductUserId peer, byte channel, byte[] buf, int len)
    {
        // Fusion 0.1.x KindSingle unwrap; skip fragments (legacy 0xF2A9 or KindFragment).
        if (!FusionP2PWire.TryUnwrap(buf.AsSpan(0, len), out var msg))
        {
            Interlocked.Increment(ref _fragSkipped);
            return;
        }
        if (msg.Length < 3) return;
        byte tag = msg[0];
        lock (StatLock) TagHist[tag] = TagHist.GetValueOrDefault(tag) + 1;

        if (tag == TagConnectionResponse)
        {
            if (VoiceCodec.TryParseConnectionResponse(msg, out var pid, out var sid, out var user))
            {
                lock (StatLock)
                {
                    string nick = user;
                    if (!string.IsNullOrEmpty(pid) && PendingByPid.TryGetValue(pid, out var pending))
                    {
                        if (!string.IsNullOrEmpty(pending.Username) && pending.Username != "?")
                            user = pending.Username;
                        if (!string.IsNullOrEmpty(pending.Nickname))
                            nick = pending.Nickname;
                    }
                    Players[sid] = new PlayerRec { PlatformId = pid, Username = user, Nickname = nick };
                }
                Console.WriteLine($"[langfarm] player map sid={sid} pid={pid} user={user}");
            }
            return;
        }

        if (tag != TagVoice) return;

        Interlocked.Increment(ref _voicePackets);
        Interlocked.Add(ref _voiceBytes, msg.Length);
        string peerId = peer?.ToString() ?? "?";
        lock (StatLock) VoicePeers.Add(peerId);

        if (!VoiceCodec.TryParseVoice(msg, out byte speakerSid, out short[] pcm))
            return;

        Interlocked.Increment(ref _voiceDecoded);
        lock (StatLock)
        {
            if (!PcmBySmall.TryGetValue(speakerSid, out var list))
            {
                list = new List<short>(pcm.Length * 32);
                PcmBySmall[speakerSid] = list;
            }
            list.AddRange(pcm);

            // If we don't know this speaker yet, at least remember peer as hint for host (sid0)
            if (!Players.ContainsKey(speakerSid) && speakerSid == 0 && peer != null)
            {
                Players[speakerSid] = new PlayerRec { PlatformId = peerId, Username = "?", Nickname = "?" };
            }
        }
    }

    private static void IngestLobbyInfo(LobbyDetails details)
    {
        try
        {
            var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "LobbyInfo" };
            if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) != Result.Success || attr == null)
                return;
            string json = attr.Value.Data?.Value.AsUtf8;
            if (string.IsNullOrEmpty(json)) return;

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("playerList", out var pl)) return;
            if (!pl.TryGetProperty("players", out var players) || players.ValueKind != JsonValueKind.Array) return;

            // LobbyInfo has no SmallID — never invent sid=index (wrong pid→voice mapping).
            int i = 0;
            foreach (var p in players.EnumerateArray())
            {
                string pid = p.TryGetProperty("platformID", out var a) ? a.GetString()
                    : p.TryGetProperty("PlatformID", out var b) ? b.GetString() : null;
                string user = p.TryGetProperty("username", out var u) ? u.GetString() : "?";
                string nick = p.TryGetProperty("nickname", out var n) ? n.GetString() : user;
                if (string.IsNullOrEmpty(pid)) continue;
                lock (StatLock)
                {
                    PendingByPid[pid] = new PlayerRec { PlatformId = pid, Username = user, Nickname = nick };
                }
                i++;
            }
            Console.WriteLine($"[langfarm] LobbyInfo players≈{i} (pending pid map, no fake sid)");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[langfarm] LobbyInfo: " + e.Message);
        }
    }

    private static byte[] BuildNetMessage(byte tag, byte[] payload)
    {
        int size = 3 + 4 + payload.Length;
        byte[] msg = new byte[size];
        msg[0] = tag;
        msg[1] = 0;
        msg[2] = 0;
        BinaryPrimitives.WriteInt32BigEndian(msg.AsSpan(3, 4), payload.Length);
        if (payload.Length > 0)
            Buffer.BlockCopy(payload, 0, msg, 7, payload.Length);
        return msg;
    }

    private static byte[] BuildMinimalConnectionRequest()
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

        WStr(_localUser.ToString());
        WInt(1); WInt(14); WInt(2);
        WStr("SLZ.BONELAB.Content.Avatar.Ford");
        payload.Write(new byte[420], 0, 420);
        WInt(2);
        WStr("Username"); WStr(BotNick);
        WStr("Nickname"); WStr(BotNick);
        WInt(0);
        return BuildNetMessage(TagConnectionRequest, payload.ToArray());
    }

    // ServerPrivacy: PUBLIC=0 PRIVATE=1 FRIENDS_ONLY=2 LOCKED=3
    // Search all shards (like the VPS scraper) so LID covers more than public-only.
    private static readonly string[] PrivacyShards = ParsePrivacyShards();

    private static string[] ParsePrivacyShards()
    {
        // JOIN_PRIVACY=0,1,2,3 (default all). Example: JOIN_PRIVACY=0,1 for public+private only.
        string raw = Env("JOIN_PRIVACY", "0,1,2,3");
        var list = new List<string>();
        foreach (string part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part is "0" or "1" or "2" or "3")
                list.Add(part);
        }
        return list.Count > 0 ? list.ToArray() : new[] { "0", "1", "2", "3" };
    }

    private static string PrivacyLabel(string privacyEq) => privacyEq switch
    {
        "0" => "public",
        "1" => "private",
        "2" => "friends",
        "3" => "locked",
        _ => privacyEq,
    };

    private static List<LobbyDetails> FindPublicLobbies(int want)
    {
        var scored = new List<(LobbyDetails d, int score, int players, int ours, string privacy)>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Full=False first (joinable), then Full=True (attr can be stale / still has slots).
        foreach (string privacy in PrivacyShards)
        {
            foreach (string full in new[] { "False", "True" })
            {
                CollectLobbyShard(scored, seenIds, privacy, full);
            }
        }

        Console.WriteLine(
            $"[langfarm] pool={scored.Count} privacy=[{string.Join(',', PrivacyShards.Select(PrivacyLabel))}] want={want}");

        var list = new List<LobbyDetails>();
        foreach (var item in scored.OrderByDescending(x => x.score).Take(want))
        {
            Console.WriteLine(
                $"[langfarm] candidate {PrivacyLabel(item.privacy)} players≈{item.players} ours≈{item.ours} score={item.score}");
            list.Add(item.d);
        }
        foreach (var item in scored.OrderByDescending(x => x.score).Skip(want))
            SafeRelease(item.d);
        return list;
    }

    private static void CollectLobbyShard(
        List<(LobbyDetails d, int score, int players, int ours, string privacy)> scored,
        HashSet<string> seenIds,
        string privacyEq,
        string fullEq)
    {
        var lobby = _platform.GetLobbyInterface();
        var createOpts = new CreateLobbySearchOptions { MaxResults = 50 };
        if (lobby.CreateLobbySearch(ref createOpts, out LobbySearch search) != Result.Success || search == null)
            return;

        string label = $"{PrivacyLabel(privacyEq)}/full={fullEq}";
        try
        {
            void Set(string key, string val)
            {
                var a = new AttributeData { Key = key, Value = new AttributeDataValue { AsUtf8 = val } };
                var o = new LobbySearchSetParameterOptions { ComparisonOp = ComparisonOp.Equal, Parameter = a };
                search.SetParameter(ref o);
            }
            Set("Game", GameName);
            Set("Privacy", privacyEq);
            Set("Full", fullEq);

            bool done = false;
            Result fr = Result.UnexpectedError;
            var findOpts = new LobbySearchFindOptions { LocalUserId = _localUser };
            search.Find(ref findOpts, null, (ref LobbySearchFindCallbackInfo info) => { fr = info.ResultCode; done = true; });
            if (!PumpUntil(() => done, 45, $"Find:{label}"))
            {
                Console.WriteLine($"[langfarm] Find({label}): timeout");
                return;
            }
            Console.WriteLine($"[langfarm] Find({label}): {fr}");
            if (fr != Result.Success) return;

            var countOpts = default(LobbySearchGetSearchResultCountOptions);
            uint count = search.GetSearchResultCount(ref countOpts);
            Console.WriteLine($"[langfarm] find({label}) count={count}");

            string forceName = Environment.GetEnvironmentVariable("FORCE_LOBBY_NAME");
            string forceCode = Environment.GetEnvironmentVariable("FORCE_LOBBY_CODE");

            for (uint i = 0; i < count; i++)
            {
                var copyOpts = new LobbySearchCopySearchResultByIndexOptions { LobbyIndex = i };
                if (search.CopySearchResultByIndex(ref copyOpts, out LobbyDetails details) != Result.Success || details == null)
                    continue;

                string id = TryGetLobbyId(details);
                // Dedupe across privacy/full shards (same lobby can appear twice).
                string dedupeKey = !string.IsNullOrEmpty(id)
                    ? id
                    : $"idx:{privacyEq}:{fullEq}:{i}:{GetLobbyAttr(details, "LobbyName")}";
                if (!seenIds.Add(dedupeKey))
                {
                    SafeRelease(details);
                    continue;
                }

                // Drop our visual host from the candidate pool entirely.
                // FORCE_LOBBY_CODE may target our host for join diagnostics.
                string forceCodeEarly = Environment.GetEnvironmentVariable("FORCE_LOBBY_CODE");
                string codeAttrEarly = GetLobbyAttr(details, "LobbyCode") ?? "";
                bool forceOwn = !string.IsNullOrEmpty(forceCodeEarly)
                    && codeAttrEarly.Equals(forceCodeEarly, StringComparison.OrdinalIgnoreCase);
                if (!forceOwn && IsOwnVisualHost(details, out string hostSkip))
                {
                    Console.WriteLine($"[langfarm] skip own-host ({hostSkip})");
                    SafeRelease(details);
                    continue;
                }

                int players = EstimatePlayers(details);
                int ours = CountNickInLobbyInfo(details, BotNick);
                bool claimed = !string.IsNullOrEmpty(id) && IsLobbyClaimed(id);
                // Prefer fuller lobbies; soft boost non-public so we actually sample them.
                int privacyBoost = privacyEq == "0" ? 0 : 15;
                int score = players * 10 - ours * 50 - (claimed ? 100 : 0) + privacyBoost;

                if (!string.IsNullOrEmpty(forceName) || !string.IsNullOrEmpty(forceCode))
                {
                    string json = ReadLobbyInfoJson(details) ?? "";
                    string nameAttr = GetLobbyAttr(details, "LobbyName") ?? "";
                    string codeAttr = GetLobbyAttr(details, "LobbyCode") ?? "";
                    bool codePin = !string.IsNullOrEmpty(forceCode)
                        && codeAttr.Equals(forceCode, StringComparison.OrdinalIgnoreCase);
                    bool namePin = !string.IsNullOrEmpty(forceName)
                        && (nameAttr.Equals(forceName, StringComparison.OrdinalIgnoreCase)
                            || json.IndexOf(forceName, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!string.IsNullOrEmpty(forceCode))
                    {
                        if (!codePin)
                        {
                            SafeRelease(details);
                            continue;
                        }
                        score += 100000;
                    }
                    else if (namePin) score += 100000;
                    else score -= 100000;
                }

                scored.Add((details, score, players, ours, privacyEq));
            }
        }
        finally { search.Release(); }
    }

    private static int EstimatePlayers(LobbyDetails details)
    {
        string json = ReadLobbyInfoJson(details);
        if (string.IsNullOrEmpty(json)) return 0;
        int n = 0, idx = 0;
        while ((idx = json.IndexOf("\"username\"", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        { n++; idx += 10; }
        return n;
    }

    private static string ReadLobbyInfoJson(LobbyDetails details)
    {
        try
        {
            var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = "LobbyInfo" };
            if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) != Result.Success || attr == null)
                return null;
            return attr.Value.Data?.Value.AsUtf8;
        }
        catch { return null; }
    }

    private static string GetLobbyAttr(LobbyDetails details, string key)
    {
        try
        {
            var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key };
            if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) != Result.Success || attr == null)
                return null;
            return attr.Value.Data?.Value.AsUtf8;
        }
        catch { return null; }
    }

    private static int CountNickInLobbyInfo(LobbyDetails details, string nick)
    {
        string json = ReadLobbyInfoJson(details);
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(nick)) return 0;
        // Count current nick + legacy ZWSP / plain "bonelab.fun" variants.
        int n = CountOccurrences(json, nick);
        string plain = NormalizeFarmNick(nick);
        if (!string.Equals(plain, nick, StringComparison.Ordinal))
            n += CountOccurrences(json, plain);
        if (!string.Equals(plain, "bonelab.fun", StringComparison.OrdinalIgnoreCase))
            n += CountOccurrences(json, "bonelab.fun");
        // Legacy ZWSP form still present in some lobbies.
        n += CountOccurrences(json, "bonelab.\u200bfun");
        return n;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle)) return 0;
        return System.Text.RegularExpressions.Regex.Matches(
            haystack, System.Text.RegularExpressions.Regex.Escape(needle),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
    }

    /// <summary>Map LinkFilter-bypass nick forms back to plain bonelab.fun.</summary>
    private static string NormalizeFarmNick(string user)
    {
        if (string.IsNullOrEmpty(user)) return user;
        return user
            .Replace("\u200b", "", StringComparison.Ordinal)   // legacy ZWSP
            .Replace("\u00b7", ".", StringComparison.Ordinal) // middle dot
            .Replace("\u2024", ".", StringComparison.Ordinal); // one-dot leader
    }

    /// <summary>Farm nick match ignoring LinkFilter bypass chars (ZWSP / middle-dot).</summary>
    private static bool IsFarmNick(string user)
    {
        if (string.IsNullOrEmpty(user)) return false;
        if (string.Equals(user, BotNick, StringComparison.OrdinalIgnoreCase)) return true;
        string u = NormalizeFarmNick(user);
        string mine = NormalizeFarmNick(BotNick);
        return string.Equals(u, mine, StringComparison.OrdinalIgnoreCase)
            || string.Equals(u, "bonelab.fun", StringComparison.OrdinalIgnoreCase);
    }

    // Visual host lobby name (LinkFilter bypass forms). Farm bots must not join it.
    private static readonly string[] DefaultSkipLobbyNames =
    {
        "www\u00b7bonelab\u00b7fun",      // middle-dot (current host)
        "www.bonelab.fun",                // plain ASCII
        "www.\\u00b7bonelab.\\u00b7fun",  // JSON-escaped middle-dot (System.Text.Json default)
        "www.\u200bbonelab.\u200bfun",    // legacy ZWSP-after-dot
    };

    private static readonly HashSet<string> SkipLobbyNames = ParseSkipSet(
        "SKIP_LOBBY_NAMES", DefaultSkipLobbyNames, normalizeDots: true);

    private static readonly HashSet<string> SkipLobbyCodes = ParseSkipSet(
        "SKIP_LOBBY_CODES", Array.Empty<string>(), normalizeDots: false);

    private static HashSet<string> ParseSkipSet(string envKey, string[] defaults, bool normalizeDots)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string v = raw.Trim();
            set.Add(v);
            if (normalizeDots)
            {
                string n = NormalizeFarmNick(v);
                if (!string.IsNullOrEmpty(n)) set.Add(n);
            }
        }
        foreach (string d in defaults) Add(d);
        string env = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(env))
        {
            foreach (string part in env.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                Add(part);
        }
        return set;
    }

    /// <summary>Merge env SKIP_LOBBY_CODES with live host code file (host remints).</summary>
    private static HashSet<string> CurrentSkipLobbyCodes()
    {
        var set = new HashSet<string>(SkipLobbyCodes, StringComparer.OrdinalIgnoreCase);
        try
        {
            string path = Env("HOST_LOBBY_CODE_FILE", "/tmp/lang-farm/state/host_lobby_code.txt");
            if (File.Exists(path))
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string code = line.Trim();
                    if (code.Length >= 4 && code.Length <= 12)
                        set.Add(code);
                }
            }
        }
        catch { /* */ }
        return set;
    }

    /// <summary>True for our www·bonelab·fun visual host (by LobbyName / LobbyCode / LobbyInfo).</summary>
    private static bool IsOwnVisualHost(LobbyDetails details, out string reason)
    {
        reason = null;
        var skipCodes = CurrentSkipLobbyCodes();
        if (SkipLobbyNames.Count == 0 && skipCodes.Count == 0) return false;

        string code = GetLobbyAttr(details, "LobbyCode")?.Trim();
        if (!string.IsNullOrEmpty(code) && skipCodes.Contains(code))
        {
            reason = $"code={code}";
            return true;
        }

        string name = GetLobbyAttr(details, "LobbyName")?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            if (SkipLobbyNames.Contains(name) || SkipLobbyNames.Contains(NormalizeFarmNick(name)))
            {
                reason = $"name={name}";
                return true;
            }
        }

        string hostName = GetLobbyAttr(details, "HostName")?.Trim();
        string json = ReadLobbyInfoJson(details) ?? "";
        // System.Text.Json often writes middle-dot as \u00b7 — normalize before matching.
        string jsonNorm = NormalizeFarmNick(json.Replace("\\u00b7", ".", StringComparison.OrdinalIgnoreCase)
            .Replace("\\u00B7", ".", StringComparison.OrdinalIgnoreCase)
            .Replace("\\u200b", "", StringComparison.OrdinalIgnoreCase)
            .Replace("\\u200B", "", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(json))
        {
            foreach (string skip in SkipLobbyNames)
            {
                if (json.IndexOf(skip, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = $"lobbyinfo~{skip}";
                    return true;
                }
                string skipNorm = NormalizeFarmNick(skip);
                if (!string.IsNullOrEmpty(skipNorm) && jsonNorm.IndexOf(skipNorm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = $"lobbyinfo-norm~{skipNorm}";
                    return true;
                }
            }
            foreach (string skipCode in skipCodes)
            {
                if (json.IndexOf($"\"lobbyCode\":\"{skipCode}\"", StringComparison.OrdinalIgnoreCase) >= 0
                    || json.IndexOf($"\"lobbyCode\": \"{skipCode}\"", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = $"lobbyinfo-code={skipCode}";
                    return true;
                }
            }
        }

        // Last resort: our visual host always advertises HostName=coolguy + www.bonelab.fun identity.
        if (string.Equals(hostName, "coolguy", StringComparison.OrdinalIgnoreCase)
            && jsonNorm.IndexOf("www.bonelab.fun", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            reason = "hostname=coolguy+www.bonelab.fun";
            return true;
        }
        return false;
    }

    private static string TryGetLobbyId(LobbyDetails details)
    {
        // CopyInfo SIGSEGVs on Quest Fusion lobbies — use LobbyCode / LobbyName attrs instead
        try
        {
            foreach (string key in new[] { "LobbyCode", "LobbyName" })
            {
                var opts = new LobbyDetailsCopyAttributeByKeyOptions { AttrKey = key };
                if (details.CopyAttributeByKey(ref opts, out Epic.OnlineServices.Lobby.Attribute? attr) == Result.Success
                    && attr != null)
                {
                    string v = attr.Value.Data?.Value.AsUtf8;
                    if (!string.IsNullOrWhiteSpace(v))
                        return v.Trim();
                }
            }
            // fallback: short hash of LobbyInfo so claims still de-dupe
            string json = ReadLobbyInfoJson(details);
            if (!string.IsNullOrEmpty(json))
            {
                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
                return "li-" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
            }
        }
        catch { }
        return null;
    }

    private static string ClaimPath(string lobbyId)
    {
        Directory.CreateDirectory(ClaimDir);
        // sanitize id for filename
        string safe = string.Concat((lobbyId ?? "").Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
        return Path.Combine(ClaimDir, safe + ".claim");
    }

    private static bool IsLobbyClaimed(string lobbyId)
    {
        try
        {
            string path = ClaimPath(lobbyId);
            if (!File.Exists(path)) return false;
            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
            // stale claims (>3 min) = bot crashed mid-listen
            if (age.TotalMinutes > 3)
            {
                try { File.Delete(path); } catch { }
                return false;
            }
            // allow reclaim if we own it
            string body = File.ReadAllText(path);
            string me = _localUser?.ToString() ?? "";
            return !string.IsNullOrEmpty(body) && body != me;
        }
        catch { return false; }
    }

    private static bool TryClaimLobby(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return true;
        try
        {
            // Drop stale foreign claims first.
            IsLobbyClaimed(lobbyId);
            string me = _localUser?.ToString() ?? Environment.ProcessId.ToString();
            string path = ClaimPath(lobbyId);
            try
            {
                // Atomic create — two bots cannot both "win" the same lobby.
                using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                using var sw = new StreamWriter(fs);
                sw.Write(me);
            }
            catch (IOException)
            {
                // Lost the race — allow only if we already own the claim file.
                try
                {
                    string body = File.ReadAllText(path);
                    if (body == me) return true;
                }
                catch { /* */ }
                Console.WriteLine($"[langfarm] claim lost race lobby={lobbyId}");
                return false;
            }
            Console.WriteLine($"[langfarm] claimed lobby={lobbyId}");
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"[langfarm] claim fail: {e.Message}");
            return true; // fail-open so farm still runs
        }
    }

    private static void ReleaseClaim(string lobbyId)
    {
        if (string.IsNullOrEmpty(lobbyId)) return;
        try
        {
            string path = ClaimPath(lobbyId);
            if (!File.Exists(path)) return;
            string body = File.ReadAllText(path);
            string me = _localUser?.ToString() ?? "";
            if (string.IsNullOrEmpty(body) || body == me || body == Environment.ProcessId.ToString())
                File.Delete(path);
        }
        catch { }
    }

    private static bool JoinLobby(LobbyDetails details, out string lobbyId)
    {
        lobbyId = null;
        var lobby = _platform.GetLobbyInterface();
        bool done = false;
        Result jr = Result.UnexpectedError;
        string id = null;
        var opts = new JoinLobbyOptions
        {
            LobbyDetailsHandle = details,
            LocalUserId = _localUser,
            // Presence on so VPS scraper / site can see bots in lobbies (was false → "AFK / missing")
            PresenceEnabled = true,
            LocalRTCOptions = null,
            RTCRoomJoinActionType = LobbyRTCRoomJoinActionType.AutomaticJoin,
        };
        lobby.JoinLobby(ref opts, null, (ref JoinLobbyCallbackInfo info) =>
        {
            jr = info.ResultCode;
            id = info.LobbyId;
            done = true;
        });
        if (!PumpUntil(() => done, 45, "JoinLobby"))
        {
            Console.WriteLine("[langfarm] JoinLobby: timeout");
            return false;
        }
        Console.WriteLine($"[langfarm] JoinLobby: {jr}");
        if (jr != Result.Success) return false;
        lobbyId = id;
        return true;
    }

    private static ProductUserId GetLobbyOwner(string lobbyId)
    {
        try
        {
            var lobby = _platform.GetLobbyInterface();
            var copy = new CopyLobbyDetailsHandleOptions { LobbyId = lobbyId, LocalUserId = _localUser };
            if (lobby.CopyLobbyDetailsHandle(ref copy, out LobbyDetails details) != Result.Success || details == null)
                return null;
            try
            {
                var o = default(LobbyDetailsGetLobbyOwnerOptions);
                return details.GetLobbyOwner(ref o);
            }
            finally { details.Release(); }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("[langfarm] GetLobbyOwner: " + e.Message);
            return null;
        }
    }

    private static void Leave(string lobbyId)
    {
        try
        {
            var lobby = _platform.GetLobbyInterface();
            bool done = false;
            var opts = new LeaveLobbyOptions { LocalUserId = _localUser, LobbyId = lobbyId };
            lobby.LeaveLobby(ref opts, null, (ref LeaveLobbyCallbackInfo info) =>
            {
                done = true;
                Console.WriteLine("[langfarm] Leave: " + info.ResultCode);
            });
            PumpUntil(() => done, 20, "LeaveLobby");
        }
        catch (Exception e) { Console.Error.WriteLine("[langfarm] leave: " + e.Message); }
    }

    /// <summary>
    /// Leave lobby + close P2P only. Never Platform.Release / Connect.Logout on Linux —
    /// those tear down EOSSDK and SIGSEGV (exit 139) after otherwise-successful runs.
    /// </summary>
    private static void SoftCleanup()
    {
        try
        {
            if (!string.IsNullOrEmpty(_currentLobbyId))
                Leave(_currentLobbyId);
        }
        catch { /* */ }
        try
        {
            var p2p = _platform?.GetP2PInterface();
            if (p2p != null && _localUser != null)
            {
                var close = new CloseConnectionsOptions { LocalUserId = _localUser, SocketId = FusionSocket };
                p2p.CloseConnections(ref close);
            }
        }
        catch { /* */ }
        try
        {
            if (_localUser != null)
                RegisterBotInClientDb(_localUser.ToString(), BotNick, "OFFLINE");
        }
        catch { /* */ }
        _currentLobbyId = "";
    }

    /// <summary>
    /// Flush + libc _exit so EOSSDK atexit/"Shutdown handler" never runs (avoids SIGSEGV).
    /// Marked as returning int for call-site convenience; never returns on Linux.
    /// </summary>
    private static int SafeExit(int code)
    {
        try { Console.Out.Flush(); Console.Error.Flush(); } catch { /* */ }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine($"[langfarm] hard-exit code={code} (skip EOSSDK teardown)");
            try { Console.Out.Flush(); } catch { /* */ }
            LibcExit(code);
        }

        // Non-Linux fallback — still skip Release (known-unsafe).
        try { EosIdentity.Logout(_platform, _localUser, () => _platform?.Tick()); } catch { /* */ }
        Environment.Exit(code);
        return code;
    }

    [DllImport("libc", EntryPoint = "_exit", SetLastError = false)]
    private static extern void LibcExit(int status);

    /// <summary>
    /// Upsert bot into client_data so all 10 PUIDs appear on the site (scraper may be stopped during mint).
    /// Uses POSTGRES_DSN; never logs the password. Server/map left to VPS scraper.
    /// </summary>
    private static void RegisterBotInClientDb(string pid, string name, string status = "OFFLINE")
    {
        RunDbPy("register_bot.py", $"\"{pid}\" \"{name}\" \"{status}\"", "db-register");
    }

    /// <summary>UPDATE client_data SET language='English'|... WHERE pid=...</summary>
    private static void UpdatePlayerLanguage(string pid, string langCode, float conf)
    {
        RunDbPy("update_language.py", $"\"{pid}\" \"{langCode}\" {conf.ToString(System.Globalization.CultureInfo.InvariantCulture)}", "db-language");
    }

    private static void RunDbPy(string script, string args, string tag)
    {
        string dsn = Env("POSTGRES_DSN", "");
        if (string.IsNullOrEmpty(dsn))
        {
            Console.WriteLine($"[langfarm] POSTGRES_DSN unset — skip {tag}");
            return;
        }
        try
        {
            string py = Path.Combine("/tmp/lang-farm/orchestrator", script);
            if (!File.Exists(py))
            {
                Console.Error.WriteLine($"[langfarm] {script} missing");
                return;
            }
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = $"\"{py}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.Environment["POSTGRES_DSN"] = dsn;
            // Bot process runs with HOME=/tmp/lang-farm/homes/botN for EOS DeviceId isolation.
            // psycopg2 lives in the real user site-packages — force that path for DB helpers.
            psi.Environment["PYTHONPATH"] = "/home/ubuntu/.local/lib/python3.12/site-packages";
            psi.Environment["HOME"] = "/home/ubuntu";
            using var p = System.Diagnostics.Process.Start(psi);
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(20000);
            Console.WriteLine($"[langfarm] {tag}: " + stdout.Trim());
            if (p.ExitCode != 0)
                Console.Error.WriteLine($"[langfarm] {tag} err: " + stderr.Trim());
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[langfarm] {tag} failed: " + e.Message);
        }
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

    private static void SafeRelease(LobbyDetails d) { try { d?.Release(); } catch { } }

    private static string RandomHex(int chars)
    {
        var buf = RandomNumberGenerator.GetBytes(chars / 2);
        var sb = new StringBuilder(chars);
        foreach (byte b in buf) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static bool PumpUntil(Func<bool> done, int timeoutSec, string tag = "EOS")
    {
        var until = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (!done() && DateTime.UtcNow < until)
        {
            try { _platform?.Tick(); } catch { /* */ }
            Thread.Sleep(15);
        }
        if (done()) return true;
        Console.Error.WriteLine($"[langfarm] {tag} timed out after {timeoutSec}s");
        return false;
    }
}
