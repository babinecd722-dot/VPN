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
    /// Local pid list in UserData → POST /v1/track every 10s → BoneMenu + Join by lobby_code.
    /// </summary>
    internal static class Tracking
    {
        private const string CfgName = "tracking.cfg";
        private const string ListName = "tracking.json";
        private const string DefaultApiUrl = "http://62.109.21.131:8787";
        private const float PollIntervalSec = 10f;
        private const int HttpTimeoutSeconds = 8;
        private const int MaxTracked = 32;

        private static string _apiUrl = DefaultApiUrl;
        private static string _apiKey = "";
        private static bool _enabled = true;
        private static bool _hooked;
        private static bool _menuHooked;
        private static bool _polling;
        private static float _pollCd;
        private static string _lastError = "";

        private static readonly object Gate = new object();
        private static readonly List<TrackedEntry> Entries = new List<TrackedEntry>();
        private static readonly Dictionary<string, TrackSnapshot> Snapshots =
            new Dictionary<string, TrackSnapshot>(StringComparer.OrdinalIgnoreCase);
        private static readonly HttpClient Http = CreateHttp();

        private static Page _rootPage;
        private static Page _listPage;

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
            LoadConfig();
            LoadList();
            InstallFusionProfileHook(harmony);
            if (_enabled)
                MelonCoroutines.Start(BootRoutine());
            MelonLogger.Msg($"Tracking: enabled={_enabled} tracked={Entries.Count} api={_apiUrl}");
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
            if (!_enabled || Entries.Count == 0) return;
            _pollCd -= Time.unscaledDeltaTime;
            if (_pollCd > 0f || _polling) return;
            _pollCd = PollIntervalSec;
            MelonCoroutines.Start(PollRoutine(force: false));
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
                    RebuildMenu();
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
            RebuildMenu();
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
            yield return PollRoutine(force: true);
        }

        private static IEnumerator PollRoutine(bool force)
        {
            if (_polling) yield break;
            List<string> pids;
            lock (Gate)
            {
                if (Entries.Count == 0) yield break;
                pids = new List<string>(Entries.Count);
                for (int i = 0; i < Entries.Count; i++)
                    pids.Add(Entries[i].Pid);
            }

            _polling = true;
            Task<string> task = Task.Run(() => FetchTrackJson(pids));
            while (!task.IsCompleted) yield return null;

            try
            {
                string json = task.Result;
                if (json == null)
                {
                    // keep last snapshots; surface last error once
                }
                else
                {
                    ApplyTrackJson(json);
                    _lastError = "";
                }
            }
            catch (Exception e)
            {
                _lastError = e.Message;
                MelonLogger.Warning("Tracking poll: " + e.Message);
            }
            finally
            {
                _polling = false;
                if (!force)
                    _pollCd = PollIntervalSec;
                RebuildMenu();
            }
        }

        private static string FetchTrackJson(List<string> pids)
        {
            if (string.IsNullOrEmpty(_apiUrl))
            {
                _lastError = "ApiUrl missing";
                return null;
            }
            if (string.IsNullOrEmpty(_apiKey))
            {
                _lastError = "ApiKey missing — set UserData/MonsterPanel/tracking.cfg";
                return null;
            }

            var sb = new StringBuilder(128 + pids.Count * 40);
            sb.Append("{\"pids\":[");
            for (int i = 0; i < pids.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(JsonEscape(pids[i])).Append('"');
            }
            sb.Append("]}");

            using var req = new HttpRequestMessage(HttpMethod.Post, Combine(_apiUrl, "/v1/track"));
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
            req.Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            using HttpResponseMessage resp = Http.Send(req);
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if (!resp.IsSuccessStatusCode)
            {
                _lastError = "HTTP " + (int)resp.StatusCode + " " + TrimOneLine(body, 120);
                MelonLogger.Warning("Tracking: " + _lastError);
                return null;
            }
            return body;
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
            if (opened == _rootPage || opened == _listPage)
                RebuildMenu();
        }

        private static void RebuildMenu()
        {
            if (_rootPage == null) return;
            try
            {
                _rootPage.RemoveAll();
                _rootPage.CreateFunction("Refresh now", new Color(0.7f, 0.7f, 0.7f), (Action)(() =>
                {
                    if (_pollCd > 0f && _polling == false)
                    {
                        Notify("Tracking", "Cooldown every 10s — wait " + Mathf.CeilToInt(_pollCd) + "s");
                        return;
                    }
                    _pollCd = 0f;
                    MelonCoroutines.Start(PollRoutine(force: false));
                }));

                if (!string.IsNullOrEmpty(_lastError))
                    _rootPage.CreateFunction("Err: " + SafeMenu(_lastError, 40), new Color(1f, 0.4f, 0.3f), (Action)(() => { }));

                lock (Gate)
                {
                    if (Entries.Count == 0)
                    {
                        _rootPage.CreateFunction("No tracked players", new Color(0.55f, 0.55f, 0.55f), (Action)(() => { }));
                        _rootPage.CreateFunction("Tip: open player profile → Add", new Color(0.55f, 0.55f, 0.55f), (Action)(() => { }));
                        return;
                    }

                    // Online first
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
                        string title = FormatListTitle(e, snap);
                        Color col = snap != null && snap.Online
                            ? new Color(0.35f, 1f, 0.45f)
                            : new Color(0.65f, 0.65f, 0.7f);
                        Page sub = _rootPage.CreatePage(title, col, 32, true);
                        FillPlayerPage(sub, e, snap);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Tracking menu: " + ex.Message);
            }
        }

        private static void FillPlayerPage(Page page, TrackedEntry e, TrackSnapshot snap)
        {
            page.RemoveAll();
            string name = SafeMenu(snap != null && !string.IsNullOrEmpty(snap.Name) ? snap.Name : e.Name);
            page.CreateFunction("Name: " + name, Color.white, (Action)(() => { }));
            page.CreateFunction("PID: " + ShortPid(e.Pid), new Color(1f, 0.45f, 0.45f), (Action)(() => { }));

            if (snap == null)
            {
                page.CreateFunction("Status: waiting poll…", new Color(0.8f, 0.8f, 0.4f), (Action)(() => { }));
            }
            else if (!snap.Found)
            {
                page.CreateFunction("Status: not in DB", new Color(1f, 0.5f, 0.3f), (Action)(() => { }));
            }
            else
            {
                page.CreateFunction("Status: " + SafeMenu(snap.Status), snap.Online ? new Color(0.4f, 1f, 0.5f) : new Color(0.75f, 0.75f, 0.75f), (Action)(() => { }));
                page.CreateFunction("Server: " + SafeMenu(NullDash(snap.Server), 42), Color.white, (Action)(() => { }));
                page.CreateFunction("Map: " + SafeMenu(NullDash(snap.Map), 42), Color.white, (Action)(() => { }));
                page.CreateFunction("Language: " + SafeMenu(NullDash(snap.Language)), Color.white, (Action)(() => { }));
                page.CreateFunction("Lobby: " + SafeMenu(NullDash(snap.LobbyCode)), new Color(0.6f, 0.85f, 1f), (Action)(() => { }));
                page.CreateFunction("Playing: " + FormatSession(snap), Color.white, (Action)(() => { }));
                page.CreateFunction("Last seen: " + FormatLastSeen(snap), Color.white, (Action)(() => { }));

                string code = snap.LobbyCode;
                bool canJoin = snap.Online && !string.IsNullOrWhiteSpace(code);
                page.CreateFunction(
                    canJoin ? "Join server" : "Join (no lobby code)",
                    canJoin ? new Color(0.25f, 0.95f, 0.55f) : new Color(0.45f, 0.45f, 0.45f),
                    (Action)(() =>
                    {
                        if (!canJoin)
                        {
                            NotifyError("Join", "No lobby code yet — wait for scraper");
                            return;
                        }
                        try
                        {
                            NetworkHelper.JoinServerByCode(code.Trim().ToUpperInvariant());
                            Notify("Joining", SafeMenu(name) + " / " + code);
                            MelonLogger.Msg("Tracking: JoinServerByCode " + code);
                        }
                        catch (Exception ex)
                        {
                            NotifyError("Join failed", ex.Message);
                        }
                    }));
            }

            string pid = e.Pid;
            page.CreateFunction("Remove from Tracking", new Color(1f, 0.35f, 0.35f), (Action)(() => Remove(pid)));
        }

        private static string FormatListTitle(TrackedEntry e, TrackSnapshot snap)
        {
            string name = SafeMenu(snap != null && !string.IsNullOrEmpty(snap.Name) ? snap.Name : e.Name, 18);
            if (snap == null) return "? " + name;
            if (!snap.Found) return "! " + name;
            if (snap.Online) return "* " + name;
            return "- " + name;
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
                group.AddElement<FunctionElement>(label)
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

        private static void LoadConfig()
        {
            try
            {
                string path = Path.Combine(UserDataDir(), CfgName);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? UserDataDir());
                if (!File.Exists(path))
                {
                    // Prefer existing PlayerDb key if present
                    string inheritedKey = TryReadLegacyApiKey();
                    File.WriteAllText(path,
                        "# MONSTER Panel — Tracking\n" +
                        "Enabled=true\n" +
                        "ApiUrl=" + DefaultApiUrl + "\n" +
                        "ApiKey=" + inheritedKey + "\n" +
                        "# Same key as INGEST_API_KEY on VPS player-ingest.\n" +
                        "# Poll interval fixed at 10 seconds.\n");
                    MelonLogger.Msg("Tracking: created " + path);
                }

                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                        _enabled = !(val.Equals("false", StringComparison.OrdinalIgnoreCase) || val == "0");
                    else if (key.Equals("ApiUrl", StringComparison.OrdinalIgnoreCase))
                        _apiUrl = string.IsNullOrEmpty(val) ? DefaultApiUrl : val.TrimEnd('/');
                    else if (key.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
                        _apiKey = val;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Tracking config: " + e.Message);
            }
        }

        private static string TryReadLegacyApiKey()
        {
            try
            {
                string legacy = Path.Combine(UserDataDir(), "player_db.cfg");
                if (!File.Exists(legacy)) return "";
                foreach (string raw in File.ReadAllLines(legacy))
                {
                    string line = raw.Trim();
                    if (!line.StartsWith("ApiKey=", StringComparison.OrdinalIgnoreCase)) continue;
                    return line.Substring("ApiKey=".Length).Trim();
                }
            }
            catch { /* */ }
            return "";
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
