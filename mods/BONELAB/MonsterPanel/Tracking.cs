using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BoneLib.BoneMenu;
using HarmonyLib;
using LabFusion.Entities;
using LabFusion.Menu;
using LabFusion.Marrow.Proxies;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.UI.Popups;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Friend-style presence tracking for Fusion players.
    /// Local pid list in UserData → poll /v1/track only while Tracking BoneMenu is open → Join by lobby_code.
    /// Completely inert while sitting in a Fusion lobby with the menu closed (no join hooks, no popups, no poll).
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

        private static bool _enabled = true;
        private static bool _hooked;
        private static bool _menuHooked;
        private static bool _polling;
        private static bool _rebuildQueued;
        private static bool _detailQueued;
        private static float _pollCd;
        private static string _lastError = "";
        private static string _detailQueuedPid = "";

        private static readonly object Gate = new object();
        private static readonly List<TrackedEntry> Entries = new List<TrackedEntry>();
        private static readonly Dictionary<string, TrackSnapshot> Snapshots =
            new Dictionary<string, TrackSnapshot>(StringComparer.OrdinalIgnoreCase);
        private static readonly HttpClient Http = CreateHttp();

        private static Page _rootPage;
        // One reusable friend card — never CreatePage-per-friend (BoneMenu index pages crash Quest GUIPool).
        private static Page _detailPage;
        private static string _detailPid = "";
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
            public int? OfflineSec;
            public DateTime FetchedUtc;
        }

        public static void Init(HarmonyLib.Harmony harmony)
        {
            LoadList();
            InstallFusionProfileHook(harmony);
            // No boot poll — stay inert until the player opens Tracking in BoneMenu.
            MelonLogger.Msg($"Tracking: enabled={_enabled} tracked={Entries.Count} api={ApiUrl} (menu-only poll)");
        }

        public static void InstallMenu(Page root)
        {
            // maxElements MUST be 0 — any non-zero enables BoneMenu index/arrow pages → GUIPool NRE on Quest.
            _rootPage = root.CreatePage("Tracking", new Color(0.2f, 0.85f, 0.95f), 0, true);
            _detailPage = _rootPage.CreatePage("Friend", new Color(0.3f, 0.9f, 0.55f), 0, false);
            if (!_menuHooked)
            {
                Menu.OnPageOpened += (Action<Page>)OnPageOpened;
                _menuHooked = true;
            }
            // Build once at install (menu not open yet) — later rebuilds are deferred.
            RebuildMenu();
        }

        /// <summary>
        /// Background poll ONLY while Tracking BoneMenu pages are open.
        /// Closed menu in a Fusion lobby → zero Tracking work (no HTTP, no BoneMenu rebuild).
        /// </summary>
        public static void Tick()
        {
            if (!_enabled || Entries.Count == 0) return;
            if (!IsTrackingMenuOpen()) return;
            if (Time.unscaledTime < _apiReadyAt) return;
            _pollCd -= Time.unscaledDeltaTime;
            if (_pollCd > 0f || _polling) return;
            _pollCd = PollIntervalSec;
            MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: true));
        }

        private static bool IsTrackingMenuOpen()
        {
            if (_rootPage == null) return false;
            try
            {
                Page cur = Menu.CurrentPage;
                return cur == _rootPage || cur == _detailPage;
            }
            catch { return false; }
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
                SaveList_NoLock();
            }
            Notify("Removed from Tracking", SafeMenu(removed));
            if (!string.IsNullOrEmpty(_detailPid) &&
                string.Equals(_detailPid, pid, StringComparison.OrdinalIgnoreCase))
            {
                _detailPid = "";
                try { if (_rootPage != null) Menu.OpenPage(_rootPage); } catch { /* */ }
            }
            RequestMenuRefresh();
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

                    try
                    {
                        string json = task.Result;
                        if (json != null)
                        {
                            ApplyTrackJson(json);
                            _lastError = "";
                            _apiReadyAt = Time.unscaledTime + PollIntervalSec;
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

        private static void ApplyTrackJson(string json)
        {
            // Minimal JSON walk — avoid Newtonsoft dependency in MelonLoader.
            int playersIdx = json.IndexOf("\"players\"", StringComparison.Ordinal);
            if (playersIdx < 0) return;
            int arr = json.IndexOf('[', playersIdx);
            if (arr < 0) return;

            int i = arr + 1;
            var now = DateTime.UtcNow;
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
                        OfflineSec = JsonInt(obj, "offline_sec"),
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

                    Snapshots[pid] = snap;
                }
                SaveList_NoLock();
            }
        }

        private static void OnPageOpened(Page opened)
        {
            if (opened != _rootPage) return;
            // NEVER RemoveAll inside OnPageOpened — BoneMenu is mid-draw → GUIPool NRE on Quest.
            ScheduleRebuild(poll: true);
        }

        private static void RequestMenuRefresh()
        {
            if (_rootPage == null) return;
            Page cur = null;
            try { cur = Menu.CurrentPage; } catch { /* */ }
            if (cur == _detailPage && !string.IsNullOrEmpty(_detailPid))
            {
                ScheduleDetailFill(_detailPid, open: false);
                return;
            }
            if (cur == _rootPage)
                ScheduleRebuild(poll: false);
        }

        private static void ScheduleRebuild(bool poll)
        {
            if (_rebuildQueued) return;
            _rebuildQueued = true;
            MelonCoroutines.Start(DeferredRebuildRoutine(poll));
        }

        private static IEnumerator DeferredRebuildRoutine(bool poll)
        {
            // Wait until BoneMenu finishes OnPageOpened / DrawElements.
            yield return null;
            yield return null;
            _rebuildQueued = false;
            try
            {
                Page cur = null;
                try { cur = Menu.CurrentPage; } catch { /* */ }
                if (cur == _detailPage) yield break;
                if (cur != null && cur != _rootPage) yield break;
                RebuildMenu();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking deferred rebuild: " + ex.Message);
            }

            if (poll && _enabled && Entries.Count > 0 && !_polling && Time.unscaledTime >= _apiReadyAt)
            {
                _pollCd = 0f;
                MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: true));
            }
        }

        private static void ScheduleDetailFill(string pid, bool open)
        {
            _detailQueuedPid = pid ?? "";
            if (_detailQueued) return;
            _detailQueued = true;
            MelonCoroutines.Start(DeferredDetailRoutine(open));
        }

        private static IEnumerator DeferredDetailRoutine(bool open)
        {
            yield return null;
            yield return null;
            _detailQueued = false;
            string pid = _detailQueuedPid;
            if (string.IsNullOrEmpty(pid)) yield break;
            try
            {
                Page cur = null;
                try { cur = Menu.CurrentPage; } catch { /* */ }
                if (!open && cur != _detailPage) yield break;
                FillDetailPage(pid, open);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking deferred detail: " + ex.Message);
            }
        }

        private static void RebuildMenu()
        {
            if (_rootPage == null) return;
            try
            {
                Page cur = null;
                try { cur = Menu.CurrentPage; } catch { /* */ }
                // Never RemoveAll the list while viewing the friend card.
                if (cur == _detailPage) return;

                _rootPage.RemoveAll();
                _rootPage.Color = new Color(0.2f, 0.85f, 0.95f);

                _rootPage.CreateFunction("Refresh now", new Color(0.45f, 0.85f, 1f), (Action)(() =>
                {
                    if (_pollCd > 0f && !_polling)
                    {
                        Notify("Tracking", "Wait " + Mathf.CeilToInt(_pollCd) + "s");
                        return;
                    }
                    _pollCd = 0f;
                    MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: true));
                }));

                _rootPage.CreateFunction("Clear all friends", new Color(1f, 0.45f, 0.35f), (Action)ClearAllFriends);

                lock (Gate)
                {
                    if (Entries.Count == 0)
                    {
                        _rootPage.CreateFunction("Empty list", new Color(0.55f, 0.55f, 0.6f), (Action)(() => { }));
                        _rootPage.CreateFunction("Add via Fusion profile", new Color(0.55f, 0.75f, 0.95f), (Action)(() => { }));
                        return;
                    }

                    int onlineN = 0;
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        if (Snapshots.TryGetValue(Entries[i].Pid, out var s) && s != null && s.Online)
                            onlineN++;
                    }
                    _rootPage.CreateFunction(
                        "Live  " + onlineN + " / " + Entries.Count,
                        new Color(0.4f, 1f, 0.55f),
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
                        _rootPage.CreateFunction(
                            FormatListTitle(e, snap),
                            ListColor(snap),
                            (Action)(() => OpenFriend(pid)));
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking menu: " + ex.Message);
            }
        }

        private static void ClearAllFriends()
        {
            lock (Gate)
            {
                Entries.Clear();
                Snapshots.Clear();
                SaveList_NoLock();
            }
            _detailPid = "";
            Notify("Tracking", "List cleared");
            MelonLogger.Msg("Tracking: clear all friends");
            ScheduleRebuild(poll: false);
        }

        /// <summary>
        /// Open friend card from cache immediately; soft-refresh that pid in background.
        /// No HTTP / Join / Disconnect on the click itself.
        /// </summary>
        private static void OpenFriend(string pid)
        {
            if (string.IsNullOrWhiteSpace(pid) || _detailPage == null) return;
            _detailPid = pid;
            // Defer RemoveAll so we don't fight the click that opened the row.
            ScheduleDetailFill(pid, open: true);
            MelonCoroutines.Start(SoftRefreshFriendRoutine(pid));
        }

        private static void FillDetailPage(string pid, bool open)
        {
            if (_detailPage == null) return;

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
            if (!found)
            {
                _detailPid = "";
                try { Menu.OpenPage(_rootPage); } catch { /* */ }
                return;
            }

            try
            {
                string name = DisplayName(entry, snap);
                _detailPage.Name = SafeMenu(name, 22);
                _detailPage.Color = ListColor(snap);
                _detailPage.RemoveAll();

                Color info = new Color(0.78f, 0.82f, 0.88f);
                _detailPage.CreateFunction("ID  " + ShortPid(pid), info, (Action)(() => { }));

                if (snap == null)
                {
                    _detailPage.CreateFunction("Status  loading…", new Color(0.9f, 0.8f, 0.4f), (Action)(() => { }));
                    _detailPage.CreateFunction("Language  —", info, (Action)(() => { }));
                }
                else if (!snap.Found)
                {
                    _detailPage.CreateFunction("Status  not in DB", new Color(1f, 0.55f, 0.35f), (Action)(() => { }));
                    _detailPage.CreateFunction("Language  —", info, (Action)(() => { }));
                }
                else if (snap.Online)
                {
                    string st = string.IsNullOrWhiteSpace(snap.Status) ? "IN GAME" : snap.Status;
                    _detailPage.CreateFunction("Status  " + SafeMenu(st, 22), new Color(0.35f, 1f, 0.5f), (Action)(() => { }));
                    _detailPage.CreateFunction("Language  " + SafeMenu(NullDash(snap.Language), 18), info, (Action)(() => { }));
                    _detailPage.CreateFunction("Server  " + SafeMenu(NullDash(snap.Server), 28), info, (Action)(() => { }));
                    _detailPage.CreateFunction("Map  " + SafeMenu(NullDash(snap.Map), 28), info, (Action)(() => { }));
                    _detailPage.CreateFunction("Lobby  " + SafeMenu(NullDash(snap.LobbyCode), 12), info, (Action)(() => { }));
                    _detailPage.CreateFunction("In game  " + FormatDuration(snap.SessionSec), info, (Action)(() => { }));
                }
                else
                {
                    _detailPage.CreateFunction("Status  OFFLINE", new Color(0.7f, 0.7f, 0.75f), (Action)(() => { }));
                    _detailPage.CreateFunction("Language  " + SafeMenu(NullDash(snap.Language), 18), info, (Action)(() => { }));
                    _detailPage.CreateFunction("Offline  " + FormatDuration(snap.OfflineSec), info, (Action)(() => { }));
                }

                bool canJoin = snap != null && snap.Online && !string.IsNullOrWhiteSpace(snap.LobbyCode);
                string joinName = name;
                if (canJoin)
                {
                    string codeCopy = snap.LobbyCode;
                    _detailPage.CreateFunction("JOIN LOBBY", new Color(0.2f, 1f, 0.45f), (Action)(() =>
                    {
                        MelonCoroutines.Start(JoinAndWatchRoutine(joinName, codeCopy));
                    }));
                }
                else
                {
                    _detailPage.CreateFunction("JOIN  (offline / no code)", new Color(0.4f, 0.45f, 0.5f), (Action)(() =>
                    {
                        Notify("Tracking", "Not joinable right now");
                    }));
                }

                string removePid = pid;
                _detailPage.CreateFunction("REMOVE FRIEND", new Color(1f, 0.35f, 0.35f), (Action)(() =>
                {
                    Remove(removePid);
                }));

                if (open)
                    Menu.OpenPage(_detailPage);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking friend page: " + ex.Message);
            }
        }

        /// <summary>Background poll so the card updates without blocking the open click.</summary>
        private static IEnumerator SoftRefreshFriendRoutine(string pid)
        {
            // Let the menu paint first — never block OpenFriend on HTTP.
            yield return null;
            yield return null;

            float wait = 0f;
            while ((_polling || Time.unscaledTime < _apiReadyAt) && wait < 12f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_polling)
                MelonCoroutines.Start(PollRoutine(force: false, refreshMenu: false));

            wait = 0f;
            while (_polling && wait < 15f)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!string.Equals(_detailPid, pid, StringComparison.OrdinalIgnoreCase))
                yield break;
            Page cur = null;
            try { cur = Menu.CurrentPage; } catch { /* */ }
            if (cur == _detailPage)
                ScheduleDetailFill(pid, open: false);
        }

        private static Color ListColor(TrackSnapshot snap)
        {
            if (snap == null) return new Color(0.85f, 0.75f, 0.35f);
            if (!snap.Found) return new Color(1f, 0.55f, 0.35f);
            if (snap.Online) return new Color(0.3f, 0.95f, 0.5f);
            return new Color(0.58f, 0.6f, 0.66f);
        }

        private static string DisplayName(TrackedEntry e, TrackSnapshot snap)
        {
            if (snap != null && !string.IsNullOrEmpty(snap.Name))
                return SafeMenu(snap.Name, 28);
            return SafeMenu(e.Name, 28);
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
            if (name.Length > 16) name = name.Substring(0, 16);
            if (snap == null) return name + "  …";
            if (!snap.Found) return name + "  ?";
            if (snap.Online) return name + "  ● LIVE";
            return name + "  ○ off";
        }

        private static string FormatDuration(int? sec)
        {
            if (sec == null) return "…";
            int s = sec.Value;
            if (s < 60) return s + "s";
            if (s < 3600) return (s / 60) + "m " + (s % 60) + "s";
            return (s / 3600) + "h " + ((s % 3600) / 60) + "m";
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
