using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;
using HarmonyLib;
using LabFusion.Entities;
using LabFusion.Menu;
using LabFusion.Marrow.Proxies;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterPanel
{
    /// <summary>
    /// Friend-style presence tracking for Fusion players.
    /// Local pid list in UserData → POST /v1/track every 10s → BoneMenu + Join by lobby_code.
    /// </summary>
    internal static class Tracking
    {
        private const string ListName = "tracking.json";
        // Baked-in presence API (VPS player-ingest). No UserData secrets required.
        private const string ApiUrl = "http://62.109.21.131:8787";
        private const string ApiKey = "e63d7b2ae9d5006d109712e6c3ea2592611f563e380724de";
        private const float PollIntervalSec = 10f;
        private const int HttpTimeoutSeconds = 8;
        private const int MaxTracked = 32;
        // With PID spoof: wait for spoof popup (~3.5s). Without: short settle, then notify.
        private const float JoinNotifyDelayWithSpoofSec = 4.0f;
        private const float JoinNotifyDelayNoSpoofSec = 0.5f;
        private const float JoinNotifyGapSec = 0.6f;
        private const float JoinNotifyCooldownSec = 8f;

        private static bool _enabled = true;
        private static bool _hooked;
        private static bool _menuHooked;
        private static bool _joinHooked;
        private static bool _polling;
        private static bool _joinNotifyRunning;
        private static float _pollCd;
        private static float _joinNotifyCd;
        private static string _lastError = "";

        private static readonly object Gate = new object();
        private static readonly List<TrackedEntry> Entries = new List<TrackedEntry>();
        private static readonly Dictionary<string, TrackSnapshot> Snapshots =
            new Dictionary<string, TrackSnapshot>(StringComparer.OrdinalIgnoreCase);
        // Baseline online flags — first poll after Add/boot does not notify; only offline→online.
        private static readonly Dictionary<string, bool> LastOnline =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, float> OnlineNotifyCdUntil =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private const float PresenceNotifyCooldownSec = 45f;
        private static readonly HttpClient Http = CreateHttp();

        private static Page _rootPage;
        private static int _lastHttpStatus;
        private static float _apiReadyAt; // unscaledTime when /v1/track may be called again
        private static float _retryAfterSec;

        private struct TrackedEntry
        {
            public string Pid;
            public string Name;
            public string AddedAt;
        }

        private sealed class TrackSnapshot
        {
            public bool Found;
            public bool Online;
            public string Name = "";
            public string Status = "UNKNOWN";
            public string Server = "";
            public string Map = "";
            public string Language = "";
            public string LobbyCode = "";
            public string LastSeenAt = "";
            public int? SessionSec;
            public DateTime FetchedUtc;
        }

        public static void Init(HarmonyLib.Harmony harmony)
        {
            LoadList();
            InstallFusionProfileHook(harmony);
            InstallJoinHooks();
            if (_enabled)
                MelonCoroutines.Start(BootRoutine());
            MelonLogger.Msg($"Tracking: enabled={_enabled} tracked={Entries.Count} api={ApiUrl}");
        }

        public static void InstallMenu(Page root)
        {
            _rootPage = root.CreatePage("Tracking", new Color(0.35f, 0.85f, 0.95f), 64, true);
            if (!_menuHooked)
            {
                Menu.OnPageOpened += (Action<Page>)OnPageOpened;
                _menuHooked = true;
            }
            RebuildMenu();
        }

        public static void Tick()
        {
            if (_joinNotifyCd > 0f)
                _joinNotifyCd -= Time.unscaledDeltaTime;

            if (!_enabled || Entries.Count == 0) return;
            if (Time.unscaledTime < _apiReadyAt) return;
            _pollCd -= Time.unscaledDeltaTime;
            if (_pollCd > 0f || _polling) return;
            _pollCd = PollIntervalSec;
            MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: true));
        }

        private static void InstallJoinHooks()
        {
            if (_joinHooked) return;
            try
            {
                if (AccessTools.TypeByName("LabFusion.Utilities.MultiplayerHooking") == null)
                    return;
                MultiplayerHooking.OnJoinedServer += OnEnteredFusion;
                MultiplayerHooking.OnStartedServer += OnEnteredFusion;
                // Join-by-code often lands after scene load; only fire if already in a server.
                MultiplayerHooking.OnMainSceneInitialized += OnSceneWhileInServer;
                MultiplayerHooking.OnTargetLevelLoaded += OnSceneWhileInServer;
                _joinHooked = true;
                MelonLogger.Msg("Tracking: hooked Fusion join/start/scene for online alerts");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking join hook: " + e.Message);
            }
        }

        private static void OnSceneWhileInServer()
        {
            try
            {
                if (!NetworkInfo.HasServer) return;
            }
            catch { return; }
            OnEnteredFusion();
        }

        private static void OnEnteredFusion()
        {
            if (!_enabled) return;
            int count;
            lock (Gate) { count = Entries.Count; }
            if (count == 0) return;
            if (_joinNotifyRunning || _joinNotifyCd > 0f) return;
            MelonLogger.Msg("Tracking: Fusion enter — scheduling online alerts");
            MelonCoroutines.Start(OnlineAlertRoutine());
        }

        /// <summary>
        /// On Fusion enter: notify each tracked player who is online.
        /// Poll is inlined (MelonCoroutines does not reliably nest yield return IEnumerator).
        /// </summary>
        private static IEnumerator OnlineAlertRoutine()
        {
            if (_joinNotifyRunning) yield break;
            _joinNotifyRunning = true;

            try
            {
                bool spoofOn = false;
                try { spoofOn = PidSpoof.Enabled; } catch { /* */ }

                float wait = spoofOn ? JoinNotifyDelayWithSpoofSec : JoinNotifyDelayNoSpoofSec;
                MelonLogger.Msg("Tracking: join alert wait " + wait.ToString("0.0", CultureInfo.InvariantCulture) + "s (spoof=" + spoofOn + ")");
                float t = 0f;
                while (t < wait)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Wait for any in-flight poll + API cooldown, then fetch inline.
                float busy = 0f;
                while ((_polling || Time.unscaledTime < _apiReadyAt) && busy < 20f)
                {
                    busy += Time.unscaledDeltaTime;
                    yield return null;
                }

                List<string> pids;
                lock (Gate)
                {
                    pids = new List<string>(Entries.Count);
                    for (int i = 0; i < Entries.Count; i++)
                        pids.Add(Entries[i].Pid);
                }

                if (pids.Count > 0)
                {
                    for (int attempt = 0; attempt < 6; attempt++)
                    {
                        while (Time.unscaledTime < _apiReadyAt)
                            yield return null;

                        Task<string> task = Task.Run(() => FetchTrackJson(pids));
                        while (!task.IsCompleted) yield return null;
                        try
                        {
                            string json = task.Result;
                            if (json != null)
                            {
                                ApplyTrackJson(json);
                                _lastError = "";
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            MelonLogger.Warning("Tracking alert poll: " + e.Message);
                        }

                        if (_lastHttpStatus == 429)
                        {
                            float delay = _retryAfterSec > 0.1f ? _retryAfterSec : 2.5f;
                            _apiReadyAt = Time.unscaledTime + delay;
                            continue;
                        }
                        break;
                    }
                }

                List<(string name, string pid)> online = new List<(string, string)>();
                lock (Gate)
                {
                    foreach (var e in Entries)
                    {
                        if (!Snapshots.TryGetValue(e.Pid, out var snap) || snap == null)
                            continue;
                        if (!snap.Found || !snap.Online)
                            continue;
                        string nick = !string.IsNullOrWhiteSpace(snap.Name) ? snap.Name : e.Name;
                        if (string.IsNullOrWhiteSpace(nick)) nick = ShortPid(e.Pid);
                        online.Add((nick, e.Pid));
                    }
                }

                if (online.Count == 0)
                {
                    MelonLogger.Msg("Tracking: join alert — no tracked players online (http=" + _lastHttpStatus + ")");
                    _joinNotifyCd = 3f;
                    yield break;
                }

                MelonLogger.Msg("Tracking: join alert — " + online.Count + " online");
                float nowT = Time.unscaledTime;
                foreach (var row in online)
                {
                    // Avoid double popup from presence poll right after join digest.
                    lock (Gate)
                    {
                        LastOnline[row.pid] = true;
                        OnlineNotifyCdUntil[row.pid] = nowT + PresenceNotifyCooldownSec;
                    }
                    NotifyOnline(SafeMenu(row.name));
                    float g = 0f;
                    while (g < JoinNotifyGapSec)
                    {
                        g += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
                _joinNotifyCd = JoinNotifyCooldownSec;
            }
            finally
            {
                _joinNotifyRunning = false;
            }
        }

        private static void NotifyOnline(string nick)
        {
            MelonLogger.Msg("Tracking: " + nick + " is online");
            // Same shape as working Notify() elsewhere — Title/Message as plain strings.
            try
            {
                var n = new Notification();
                n.Title = new NotificationText(nick, new Color(0.35f, 0.95f, 0.55f));
                n.Message = new NotificationText("is online");
                n.Type = NotificationType.SUCCESS;
                n.ShowPopup = true;
                n.SaveToMenu = true;
                n.PopupLength = 4f;
                Notifier.Send(n);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking notify failed: " + e.Message);
                Notify("Tracking", nick + " is online");
            }
        }

        public static bool IsTracked(string pid)
        {
            if (string.IsNullOrWhiteSpace(pid)) return false;
            lock (Gate)
            {
                for (int i = 0; i < Entries.Count; i++)
                    if (string.Equals(Entries[i].Pid, pid, StringComparison.OrdinalIgnoreCase))
                        return true;
            }
            return false;
        }

        public static void Add(string pid, string name)
        {
            pid = (pid ?? "").Trim();
            if (pid.Length == 0) return;
            name = string.IsNullOrWhiteSpace(name) ? pid : name.Trim();

            lock (Gate)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (!string.Equals(Entries[i].Pid, pid, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var e = Entries[i];
                    e.Name = name;
                    Entries[i] = e;
                    SaveList_NoLock();
                    Notify("Tracking", SafeMenu(name) + " already tracked");
                    RequestMenuRefresh();
                    return;
                }

                if (Entries.Count >= MaxTracked)
                {
                    NotifyError("Tracking full", "Max " + MaxTracked + " players");
                    return;
                }

                Entries.Add(new TrackedEntry
                {
                    Pid = pid,
                    Name = name,
                    AddedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                });
                SaveList_NoLock();
            }

            Notify("Added to Tracking", SafeMenu(name));
            MelonLogger.Msg("Tracking: add " + pid + " (" + name + ")");
            RequestMenuRefresh();
            _pollCd = 0f;
        }

        public static void Remove(string pid)
        {
            if (string.IsNullOrWhiteSpace(pid)) return;
            string removed = pid;
            lock (Gate)
            {
                for (int i = Entries.Count - 1; i >= 0; i--)
                {
                    if (!string.Equals(Entries[i].Pid, pid, StringComparison.OrdinalIgnoreCase))
                        continue;
                    removed = Entries[i].Name;
                    Entries.RemoveAt(i);
                }
                Snapshots.Remove(pid);
                LastOnline.Remove(pid);
                OnlineNotifyCdUntil.Remove(pid);
                SaveList_NoLock();
            }
            Notify("Removed from Tracking", SafeMenu(removed));
            RebuildMenu();
        }

        private static IEnumerator BootRoutine()
        {
            float t = 0f;
            while (t < 3f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            MelonCoroutines.Start(PollRoutine(force: true, refreshMenu: true));
        }

        private static IEnumerator PollRoutine(bool force, bool refreshMenu = true)
        {
            float waitOther = 0f;
            while (_polling && waitOther < 15f)
            {
                waitOther += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_polling) yield break;

            if (!force && Time.unscaledTime < _apiReadyAt)
                yield break;

            List<string> pids;
            lock (Gate)
            {
                if (Entries.Count == 0) yield break;
                pids = new List<string>(Entries.Count);
                for (int i = 0; i < Entries.Count; i++)
                    pids.Add(Entries[i].Pid);
            }

            _polling = true;
            try
            {
                int attempts = force ? 4 : 1;
                for (int attempt = 0; attempt < attempts; attempt++)
                {
                    while (Time.unscaledTime < _apiReadyAt)
                        yield return null;

                    Task<string> task = Task.Run(() => FetchTrackJson(pids));
                    while (!task.IsCompleted) yield return null;

                    List<string> cameOnline = null;
                    try
                    {
                        string json = task.Result;
                        if (json != null)
                        {
                            cameOnline = ApplyTrackJson(json);
                            _lastError = "";
                            _apiReadyAt = Time.unscaledTime + PollIntervalSec;
                            if (cameOnline != null && cameOnline.Count > 0)
                            {
                                for (int n = 0; n < cameOnline.Count; n++)
                                    NotifyOnline(SafeMenu(cameOnline[n]));
                            }
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        _lastError = e.Message;
                        MelonLogger.Warning("Tracking poll: " + e.Message);
                    }

                    if (_lastHttpStatus == 429 && attempt + 1 < attempts)
                    {
                        float delay = _retryAfterSec > 0.1f ? _retryAfterSec : 2.5f;
                        _apiReadyAt = Time.unscaledTime + delay;
                        continue;
                    }
                    break;
                }
            }
            finally
            {
                _polling = false;
                if (!force)
                    _pollCd = PollIntervalSec;
                if (refreshMenu)
                    RequestMenuRefresh();
            }
        }

        private static string FetchTrackJson(List<string> pids)
        {
            var sb = new StringBuilder(128 + pids.Count * 40);
            sb.Append("{\"pids\":[");
            for (int i = 0; i < pids.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(JsonEscape(pids[i])).Append('"');
            }
            sb.Append("]}");

            using var req = new HttpRequestMessage(HttpMethod.Post, Combine(ApiUrl, "/v1/track"));
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + ApiKey);
            req.Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            using HttpResponseMessage resp = Http.Send(req);
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            _lastHttpStatus = (int)resp.StatusCode;
            _retryAfterSec = 0f;
            if (!resp.IsSuccessStatusCode)
            {
                if (_lastHttpStatus == 429)
                    _retryAfterSec = ParseRetryAfter(body, resp);
                _lastError = "HTTP " + _lastHttpStatus + " " + TrimOneLine(body, 120);
                MelonLogger.Warning("Tracking: " + _lastError);
                return null;
            }
            return body;
        }

        private static float ParseRetryAfter(string body, HttpResponseMessage resp)
        {
            try
            {
                if (resp.Headers.RetryAfter?.Delta != null)
                    return (float)resp.Headers.RetryAfter.Delta.Value.TotalSeconds + 0.2f;
            }
            catch { /* */ }
            // detail: "track cooldown 7.4s"
            if (!string.IsNullOrEmpty(body))
            {
                int i = body.IndexOf("cooldown ", StringComparison.OrdinalIgnoreCase);
                if (i >= 0)
                {
                    i += "cooldown ".Length;
                    int j = i;
                    while (j < body.Length && (char.IsDigit(body[j]) || body[j] == '.')) j++;
                    if (j > i && float.TryParse(body.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out float sec))
                        return sec + 0.3f;
                }
            }
            return 2.5f;
        }

        /// <returns>Display names that just transitioned offline→online (for popup).</returns>
        private static List<string> ApplyTrackJson(string json)
        {
            var cameOnline = new List<string>();
            // Minimal JSON walk — avoid Newtonsoft dependency in MelonLoader.
            int playersIdx = json.IndexOf("\"players\"", StringComparison.Ordinal);
            if (playersIdx < 0) return cameOnline;
            int arr = json.IndexOf('[', playersIdx);
            if (arr < 0) return cameOnline;

            int i = arr + 1;
            var now = DateTime.UtcNow;
            float nowT = Time.unscaledTime;
            lock (Gate)
            {
                while (i < json.Length)
                {
                    while (i < json.Length && (json[i] == ',' || char.IsWhiteSpace(json[i]))) i++;
                    if (i >= json.Length || json[i] == ']') break;
                    if (json[i] != '{') { i++; continue; }
                    int start = i;
                    int depth = 0;
                    for (; i < json.Length; i++)
                    {
                        if (json[i] == '{') depth++;
                        else if (json[i] == '}')
                        {
                            depth--;
                            if (depth == 0) { i++; break; }
                        }
                    }
                    string obj = json.Substring(start, i - start);
                    string pid = JsonString(obj, "pid");
                    if (string.IsNullOrEmpty(pid)) continue;

                    var snap = new TrackSnapshot
                    {
                        Found = JsonBool(obj, "found", false),
                        Online = JsonBool(obj, "online", false),
                        Name = JsonString(obj, "name") ?? "",
                        Status = JsonString(obj, "status") ?? "UNKNOWN",
                        Server = JsonString(obj, "server") ?? "",
                        Map = JsonString(obj, "server_map") ?? "",
                        Language = JsonString(obj, "language") ?? "",
                        LobbyCode = JsonString(obj, "lobby_code") ?? "",
                        LastSeenAt = JsonString(obj, "last_seen_at") ?? "",
                        SessionSec = JsonInt(obj, "session_sec"),
                        FetchedUtc = now,
                    };

                    // Keep local nickname if API name empty
                    if (string.IsNullOrWhiteSpace(snap.Name))
                    {
                        for (int e = 0; e < Entries.Count; e++)
                            if (string.Equals(Entries[e].Pid, pid, StringComparison.OrdinalIgnoreCase))
                            {
                                snap.Name = Entries[e].Name;
                                break;
                            }
                    }
                    else
                    {
                        for (int e = 0; e < Entries.Count; e++)
                        {
                            if (!string.Equals(Entries[e].Pid, pid, StringComparison.OrdinalIgnoreCase))
                                continue;
                            var ent = Entries[e];
                            ent.Name = snap.Name;
                            Entries[e] = ent;
                        }
                    }

                    bool nowOnline = snap.Found && snap.Online;
                    bool hadPrev = LastOnline.TryGetValue(pid, out bool wasOnline);
                    LastOnline[pid] = nowOnline;
                    // First observation = baseline (no spam). Later offline→online → notify.
                    if (hadPrev && !wasOnline && nowOnline)
                    {
                        bool cooled = !OnlineNotifyCdUntil.TryGetValue(pid, out float until) || nowT >= until;
                        if (cooled)
                        {
                            OnlineNotifyCdUntil[pid] = nowT + PresenceNotifyCooldownSec;
                            string nick = !string.IsNullOrWhiteSpace(snap.Name) ? snap.Name : ShortPid(pid);
                            cameOnline.Add(nick);
                        }
                    }

                    Snapshots[pid] = snap;
                }
                SaveList_NoLock();
            }
            return cameOnline;
        }

        private static void OnPageOpened(Page opened)
        {
            // Flat list only — no player subpages (those froze BoneMenu via RemoveAll/OnPageUpdated).
            if (opened == _rootPage)
                RebuildMenu();
        }

        private static void RequestMenuRefresh()
        {
            if (_rootPage == null) return;
            Page cur = null;
            try { cur = Menu.CurrentPage; } catch { /* */ }
            // Only rebuild when the Tracking list is the active page.
            if (cur == null || cur == _rootPage)
                RebuildMenu();
        }

        private static void RebuildMenu()
        {
            if (_rootPage == null) return;
            try
            {
                _rootPage.RemoveAll();
                _rootPage.Color = new Color(0.35f, 0.85f, 0.95f);

                _rootPage.CreateFunction("Refresh", new Color(0.55f, 0.78f, 0.88f), (Action)(() =>
                {
                    if (_pollCd > 0f && !_polling)
                    {
                        Notify("Tracking", "Cooldown every 10s — wait " + Mathf.CeilToInt(_pollCd) + "s");
                        return;
                    }
                    _pollCd = 0f;
                    MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: true));
                }));

                lock (Gate)
                {
                    if (Entries.Count == 0)
                    {
                        _rootPage.CreateFunction("No friends yet", new Color(0.55f, 0.55f, 0.6f), (Action)(() => { }));
                        _rootPage.CreateFunction("Add from Fusion profile", new Color(0.5f, 0.5f, 0.55f), (Action)(() => { }));
                        return;
                    }

                    int onlineN = 0;
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        if (Snapshots.TryGetValue(Entries[i].Pid, out var s) && s != null && s.Online)
                            onlineN++;
                    }
                    _rootPage.CreateFunction(
                        "Online  " + onlineN + " / " + Entries.Count,
                        new Color(0.45f, 0.95f, 0.55f),
                        (Action)(() => { }));

                    var order = new List<TrackedEntry>(Entries);
                    order.Sort((a, b) =>
                    {
                        bool ao = Snapshots.TryGetValue(a.Pid, out var sa) && sa.Online;
                        bool bo = Snapshots.TryGetValue(b.Pid, out var sb) && sb.Online;
                        if (ao != bo) return ao ? -1 : 1;
                        return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                    });

                    foreach (var e in order)
                    {
                        Snapshots.TryGetValue(e.Pid, out var snap);
                        string pid = e.Pid;
                        // List = names only. JOIN / Delete live on the profile dialog.
                        _rootPage.CreateFunction(
                            FormatListTitle(e, snap),
                            ListColor(snap),
                            (Action)(() => OpenPlayerBoard(pid)));
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking menu: " + ex.Message);
            }
        }

        private static Color ListColor(TrackSnapshot snap)
        {
            if (snap == null) return new Color(0.75f, 0.7f, 0.35f);
            if (!snap.Found) return new Color(1f, 0.55f, 0.35f);
            if (snap.Online) return new Color(0.35f, 0.95f, 0.5f);
            return new Color(0.62f, 0.62f, 0.68f);
        }

        private static string DisplayName(TrackedEntry e, TrackSnapshot snap)
        {
            if (snap != null && !string.IsNullOrEmpty(snap.Name))
                return SafeMenu(snap.Name, 28);
            return SafeMenu(e.Name, 28);
        }

        /// <summary>
        /// Profile dialog: compact info (incl. Language). Join / Delete / Close (Close does not delete).
        /// </summary>
        private static void OpenPlayerBoard(string pid)
        {
            TrackedEntry entry = default;
            bool found = false;
            TrackSnapshot snap = null;
            lock (Gate)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (!string.Equals(Entries[i].Pid, pid, StringComparison.OrdinalIgnoreCase))
                        continue;
                    entry = Entries[i];
                    found = true;
                    break;
                }
                if (found)
                    Snapshots.TryGetValue(pid, out snap);
            }
            if (!found) return;

            string name = DisplayName(entry, snap);
            var body = new StringBuilder(220);
            body.Append("ID: ").Append(ShortPid(entry.Pid)).Append('\n');

            if (snap == null)
            {
                body.Append("Status: waiting…\n");
                body.Append("Language: —\n");
            }
            else if (!snap.Found)
            {
                body.Append("Status: not in DB\n");
                body.Append("Language: —\n");
            }
            else
            {
                string st = snap.Online
                    ? (string.IsNullOrWhiteSpace(snap.Status) ? "IN GAME" : snap.Status)
                    : "OFFLINE";
                body.Append("Status: ").Append(SafeMenu(st, 28)).Append('\n');
                body.Append("Language: ").Append(SafeMenu(NullDash(snap.Language), 20)).Append('\n');
                body.Append("Server: ").Append(SafeMenu(NullDash(snap.Server), 36)).Append('\n');
                body.Append("Map: ").Append(SafeMenu(NullDash(snap.Map), 36)).Append('\n');
                body.Append("Lobby: ").Append(SafeMenu(NullDash(snap.LobbyCode), 12)).Append('\n');
                body.Append("Session: ").Append(FormatSession(snap)).Append('\n');
                body.Append("Last seen: ").Append(FormatLastSeen(snap)).Append('\n');
            }

            bool canJoin = snap != null && snap.Online && !string.IsNullOrWhiteSpace(snap.LobbyCode);
            body.Append('\n');
            if (canJoin)
                body.Append("Join / Delete / X=close");
            else
                body.Append("Delete / X=close");

            string code = canJoin ? snap.LobbyCode : null;
            string joinName = name;
            string removePid = entry.Pid;
            try
            {
                // Okay = Join, Cancel = Delete. BoneLib X also calls Decline — we rewire X after Draw.
                Menu.DisplayDialog(
                    name,
                    body.ToString(),
                    Dialog.InfoIcon,
                    canJoin
                        ? (Action)(() => MelonCoroutines.Start(JoinAndWatchRoutine(joinName, code)))
                        : null,
                    () => Remove(removePid));
                MelonCoroutines.Start(SetupTrackingDialogButtonsRoutine(canJoin));
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking board: " + ex.Message);
                Notify("Tracking", name + " — open failed");
            }
        }

        /// <summary>
        /// Prefab labels are Okay/Cancel — rename to Join/Delete.
        /// BoneLib X calls OnDeclinePressed (Delete) — rewire X to dismiss only.
        /// </summary>
        private static IEnumerator SetupTrackingDialogButtonsRoutine(bool canJoin)
        {
            yield return null;
            yield return null;
            try
            {
                if (GUIMenu.Instance == null) yield break;
                Transform root = GUIMenu.Instance.transform.Find("Dialog");
                if (root == null || !root.gameObject.activeInHierarchy) yield break;

                GUIDialog gui = root.GetComponent<GUIDialog>();
                if (gui == null)
                {
                    MelonLogger.Warning("Tracking dialog: GUIDialog missing");
                    yield break;
                }

                Button acceptBtn = AccessTools.Field(typeof(GUIDialog), "_acceptButton")?.GetValue(gui) as Button;
                Button denyBtn = AccessTools.Field(typeof(GUIDialog), "_denyButton")?.GetValue(gui) as Button;
                Button closeBtn = AccessTools.Field(typeof(GUIDialog), "_closeButton")?.GetValue(gui) as Button;

                if (canJoin && acceptBtn != null)
                    SetButtonLabel(acceptBtn, "Join");
                if (denyBtn != null)
                {
                    denyBtn.gameObject.SetActive(true);
                    SetButtonLabel(denyBtn, "Delete");
                }

                // X: close only (do not invoke Delete / OnDeclinePressed).
                if (closeBtn != null)
                {
                    GameObject dialogGo = root.gameObject;
                    closeBtn.onClick.RemoveAllListeners();
                    closeBtn.onClick.AddListener((Action)(() =>
                    {
                        dialogGo.SetActive(false);
                        try { GUIMenu.Instance.ShowView(); } catch { /* */ }
                    }));
                }

                MelonLogger.Msg("Tracking dialog: labels Join/Delete, X=close only");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking dialog buttons: " + e.Message);
            }
        }

        private static void SetButtonLabel(Button btn, string text)
        {
            if (btn == null || string.IsNullOrEmpty(text)) return;
            Type tmpType = AccessTools.TypeByName("Il2CppTMPro.TextMeshProUGUI")
                ?? AccessTools.TypeByName("TMPro.TextMeshProUGUI");
            if (tmpType == null) return;
            var textProp = AccessTools.Property(tmpType, "text");
            if (textProp == null || !textProp.CanWrite) return;

            Component[] all = btn.GetComponentsInChildren<Component>(true);
            if (all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                Type ct = all[i].GetType();
                if (ct != tmpType && !tmpType.IsAssignableFrom(ct)) continue;
                textProp.SetValue(all[i], text);
            }
        }

        private static IEnumerator JoinAndWatchRoutine(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                NotifyError("Join", "No lobby code");
                yield break;
            }

            string c = code.Trim().ToUpperInvariant();

            // Already in a lobby → leave first. Old logic reported false "join OK" after 1.5s.
            bool inServer = false;
            try { inServer = NetworkInfo.HasServer; } catch { /* */ }
            if (inServer)
            {
                MelonLogger.Msg("Tracking: disconnect before Join " + c);
                Notify("Joining", "Leaving current lobby…");
                try { NetworkHelper.Disconnect("Tracking Join"); }
                catch (Exception ex)
                {
                    NotifyError("Join failed", "Disconnect: " + ex.Message);
                    yield break;
                }

                float leaveT = 0f;
                while (leaveT < 8f)
                {
                    leaveT += Time.unscaledDeltaTime;
                    bool still = false;
                    try { still = NetworkInfo.HasServer; } catch { /* */ }
                    if (!still) break;
                    yield return null;
                }
                try { inServer = NetworkInfo.HasServer; } catch { inServer = true; }
                if (inServer)
                {
                    NotifyError("Join failed", "Could not leave current lobby");
                    yield break;
                }
                // Brief settle so EOS matchmaker accepts a new join.
                float settle = 0f;
                while (settle < 0.75f)
                {
                    settle += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            try
            {
                NetworkHelper.JoinServerByCode(c);
                Notify("Joining", SafeMenu(name) + " / " + c);
                MelonLogger.Msg("Tracking: JoinServerByCode " + c);
            }
            catch (Exception ex)
            {
                NotifyError("Join failed", ex.Message);
                yield break;
            }

            // Success = transition into a server from offline (not "already was in one").
            float t = 0f;
            while (t < 12f)
            {
                t += Time.unscaledDeltaTime;
                bool nowIn = false;
                try { nowIn = NetworkInfo.HasServer; } catch { /* */ }
                if (nowIn)
                {
                    string got = null;
                    try { got = NetworkHelper.GetServerCode(); } catch { /* */ }
                    MelonLogger.Msg(
                        "Tracking: join OK — target=" + c +
                        " code=" + (got ?? "?") +
                        " t=" + t.ToString("0.0", CultureInfo.InvariantCulture) + "s");
                    if (!_joinNotifyRunning && _joinNotifyCd <= 0f)
                        MelonCoroutines.Start(OnlineAlertRoutine());
                    yield break;
                }
                yield return null;
            }

            MelonLogger.Warning("Tracking: join timed out for " + c);
            NotifyError(
                "Join failed",
                "Lobby not found or not joinable (private/locked/stale). Refresh Tracking and retry.");
        }

        private static string FormatListTitle(TrackedEntry e, TrackSnapshot snap)
        {
            string name = DisplayName(e, snap);
            if (name.Length > 18) name = name.Substring(0, 18);
            if (snap == null) return name + "   …";
            if (!snap.Found) return name + "   ?";
            if (snap.Online) return name + "   ONLINE";
            return name + "   offline";
        }

        private static string FormatSession(TrackSnapshot snap)
        {
            if (!snap.Online || snap.SessionSec == null) return snap.Online ? "…" : "offline";
            int s = snap.SessionSec.Value;
            if (s < 60) return s + "s";
            if (s < 3600) return (s / 60) + "m " + (s % 60) + "s";
            return (s / 3600) + "h " + ((s % 3600) / 60) + "m";
        }

        private static string FormatLastSeen(TrackSnapshot snap)
        {
            if (snap.Online) return "now";
            if (string.IsNullOrEmpty(snap.LastSeenAt)) return "-";
            if (!DateTime.TryParse(snap.LastSeenAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return SafeMenu(snap.LastSeenAt, 24);
            var ago = DateTime.UtcNow - dt.ToUniversalTime();
            if (ago.TotalSeconds < 90) return "just now";
            if (ago.TotalMinutes < 60) return ((int)ago.TotalMinutes) + "m ago";
            if (ago.TotalHours < 48) return ((int)ago.TotalHours) + "h ago";
            return ((int)ago.TotalDays) + "d ago";
        }

        private static void InstallFusionProfileHook(HarmonyLib.Harmony harmony)
        {
            if (_hooked) return;
            try
            {
                var t = AccessTools.TypeByName("LabFusion.Menu.MenuLocation");
                var m = AccessTools.Method(t, "ApplyPlayerToElement");
                if (m == null)
                {
                    MelonLogger.Warning("Tracking: MenuLocation.ApplyPlayerToElement not found");
                    return;
                }
                harmony.Patch(m, postfix: new HarmonyMethod(typeof(Tracking), nameof(ApplyPlayerToElementPostfix)));
                _hooked = true;
                MelonLogger.Msg("Tracking: hooked Fusion player profile actions");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking hook: " + e.Message);
            }
        }

        /// <summary>Fusion lobby player profile → Add / Remove Tracking button.</summary>
        private static void ApplyPlayerToElementPostfix(PlayerElement element, PlayerID player)
        {
            try
            {
                if (element == null || player == null) return;
                try { if (player.IsMe) return; } catch { return; }

                string pid = null;
                try { pid = player.PlatformID; } catch { }
                if (string.IsNullOrWhiteSpace(pid)) return;
                pid = pid.Trim();

                string username = "";
                try { username = player.Metadata?.Username?.GetValueOrEmpty() ?? ""; } catch { }
                if (string.IsNullOrWhiteSpace(username))
                {
                    try { username = player.PlatformID; } catch { username = pid; }
                }

                var actions = element.ActionsElement;
                if (actions == null) return;
                PageElement page = null;
                try
                {
                    if (actions.Pages != null && actions.Pages.Count > 0)
                        page = actions.Pages[0];
                }
                catch { /* */ }
                if (page == null)
                    page = actions.AddPage();

                var group = page.AddElement<GroupElement>("Tracking");
                bool tracked = IsTracked(pid);
                string label = tracked ? "Remove from Tracking" : "Add to Tracking";
                Color color = tracked ? new Color(1f, 0.4f, 0.35f) : new Color(0.35f, 0.9f, 1f);
                string pidCopy = pid;
                string nameCopy = username;
                group.AddElement<LabFusion.Marrow.Proxies.FunctionElement>(label)
                    .WithColor(color)
                    .Do(() =>
                    {
                        if (IsTracked(pidCopy)) Remove(pidCopy);
                        else Add(pidCopy, nameCopy);
                    });
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking profile btn: " + e.Message);
            }
        }

        private static HttpClient CreateHttp()
        {
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
            c.DefaultRequestHeaders.ExpectContinue = false;
            return c;
        }

        private static void LoadList()
        {
            lock (Gate)
            {
                Entries.Clear();
                try
                {
                    string path = Path.Combine(UserDataDir(), ListName);
                    if (!File.Exists(path)) return;
                    string json = File.ReadAllText(path);
                    // Very small schema: {"entries":[{"pid":"...","name":"...","added_at":"..."}]}
                    int arr = json.IndexOf('[');
                    int end = json.LastIndexOf(']');
                    if (arr < 0 || end <= arr) return;
                    string body = json.Substring(arr + 1, end - arr - 1);
                    foreach (string part in SplitObjects(body))
                    {
                        string pid = JsonString(part, "pid");
                        if (string.IsNullOrEmpty(pid)) continue;
                        Entries.Add(new TrackedEntry
                        {
                            Pid = pid,
                            Name = JsonString(part, "name") ?? pid,
                            AddedAt = JsonString(part, "added_at") ?? "",
                        });
                        if (Entries.Count >= MaxTracked) break;
                    }
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Tracking list load: " + e.Message);
                }
            }
        }

        private static void SaveList_NoLock()
        {
            try
            {
                string path = Path.Combine(UserDataDir(), ListName);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? UserDataDir());
                var sb = new StringBuilder();
                sb.Append("{\"version\":1,\"entries\":[");
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var e = Entries[i];
                    sb.Append("{\"pid\":\"").Append(JsonEscape(e.Pid))
                      .Append("\",\"name\":\"").Append(JsonEscape(e.Name ?? ""))
                      .Append("\",\"added_at\":\"").Append(JsonEscape(e.AddedAt ?? ""))
                      .Append("\"}");
                }
                sb.Append("]}");
                File.WriteAllText(path, sb.ToString());
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking list save: " + e.Message);
            }
        }

        private static string UserDataDir()
        {
            string root;
            try { root = MelonLoader.Utils.MelonEnvironment.UserDataDirectory; }
            catch { root = "UserData"; }
            return Path.Combine(root, "MonsterPanel");
        }

        private static string Combine(string baseUrl, string path) => baseUrl.TrimEnd('/') + path;

        private static string NullDash(string s) => string.IsNullOrWhiteSpace(s) ? "-" : s;

        private static string ShortPid(string pid) =>
            string.IsNullOrEmpty(pid) ? "-" : (pid.Length <= 12 ? pid : pid.Substring(0, 12) + "…");

        private static string SafeMenu(string s, int max = 48)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            var sb = new StringBuilder(Math.Min(s.Length, max));
            bool inTag = false;
            foreach (char c in s)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;
                if (c >= 32 && c < 127) sb.Append(c);
                if (sb.Length >= max) break;
            }
            string outS = sb.ToString().Trim();
            return outS.Length == 0 ? "-" : outS;
        }

        private static string TrimOneLine(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace('\n', ' ').Replace('\r', ' ');
            return s.Length <= max ? s : s.Substring(0, max);
        }

        private static string JsonEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 8);
            foreach (char c in s)
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static List<string> SplitObjects(string body)
        {
            var list = new List<string>();
            int depth = 0, start = -1;
            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '{')
                {
                    if (depth == 0) start = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        list.Add(body.Substring(start, i - start + 1));
                        start = -1;
                    }
                }
            }
            return list;
        }

        private static string JsonString(string obj, string key)
        {
            string pattern = "\"" + key + "\"";
            int k = obj.IndexOf(pattern, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = obj.IndexOf(':', k + pattern.Length);
            if (colon < 0) return null;
            int i = colon + 1;
            while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
            if (i >= obj.Length) return null;
            if (obj[i] == 'n' && obj.IndexOf("null", i, StringComparison.Ordinal) == i) return null;
            if (obj[i] != '"') return null;
            i++;
            var sb = new StringBuilder();
            for (; i < obj.Length; i++)
            {
                char c = obj[i];
                if (c == '\\' && i + 1 < obj.Length)
                {
                    char n = obj[++i];
                    sb.Append(n);
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool JsonBool(string obj, string key, bool fallback)
        {
            string pattern = "\"" + key + "\"";
            int k = obj.IndexOf(pattern, StringComparison.Ordinal);
            if (k < 0) return fallback;
            int colon = obj.IndexOf(':', k + pattern.Length);
            if (colon < 0) return fallback;
            int i = colon + 1;
            while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
            if (i < obj.Length && obj.IndexOf("true", i, StringComparison.Ordinal) == i) return true;
            if (i < obj.Length && obj.IndexOf("false", i, StringComparison.Ordinal) == i) return false;
            return fallback;
        }

        private static int? JsonInt(string obj, string key)
        {
            string pattern = "\"" + key + "\"";
            int k = obj.IndexOf(pattern, StringComparison.Ordinal);
            if (k < 0) return null;
            int colon = obj.IndexOf(':', k + pattern.Length);
            if (colon < 0) return null;
            int i = colon + 1;
            while (i < obj.Length && char.IsWhiteSpace(obj[i])) i++;
            if (i < obj.Length && obj[i] == 'n') return null;
            int start = i;
            if (i < obj.Length && obj[i] == '-') i++;
            while (i < obj.Length && char.IsDigit(obj[i])) i++;
            if (i == start || (i == start + 1 && obj[start] == '-')) return null;
            if (int.TryParse(obj.Substring(start, i - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                return v;
            return null;
        }

        private static void Notify(string title, string message)
        {
            try
            {
                var n = new Notification();
                n.Title = title;
                n.Message = message;
                n.Type = NotificationType.SUCCESS;
                n.ShowPopup = true;
                n.PopupLength = 2.5f;
                Notifier.Send(n);
            }
            catch { MelonLogger.Msg(title + ": " + message); }
        }

        private static void NotifyError(string title, string message)
        {
            try
            {
                var n = new Notification();
                n.Title = title;
                n.Message = message;
                n.Type = NotificationType.ERROR;
                n.ShowPopup = true;
                n.PopupLength = 3.5f;
                Notifier.Send(n);
            }
            catch { MelonLogger.Warning(title + ": " + message); }
        }
    }
}
