using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using LabFusion.Entities;
using LabFusion.Player;
using LabFusion.Utilities;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Silent Fusion lobby scrapers → HTTP ingest → Postgres.
    /// DB password never lives in the mod; only ApiUrl + ApiKey for the ingest proxy.
    /// </summary>
    internal static class PlayerDb
    {
        private const string CfgName = "player_db.cfg";
        private const string DefaultApiUrl = "http://62.109.21.131:8787";
        private const float BootDelaySeconds = 4f;
        private const float ScanIntervalSeconds = 8f;
        private const float FlushIntervalSeconds = 2f;
        private const int MaxBatch = 32;
        private const int HttpTimeoutSeconds = 8;

        private static string _apiUrl = DefaultApiUrl;
        private static string _apiKey = "";
        private static bool _enabled = true;
        private static bool _hooked;
        private static bool _bootStarted;
        private static bool _dbConnected;
        private static float _scanTimer;
        private static float _flushTimer;

        private static readonly ConcurrentDictionary<string, byte> Seen = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static readonly ConcurrentQueue<PlayerRow> Pending = new ConcurrentQueue<PlayerRow>();
        private static readonly HttpClient Http = CreateHttp();
        private static int _flushing;

        private struct PlayerRow
        {
            public string Name;
            public string Pid;
        }

        public static bool DatabaseConnected => _dbConnected;

        public static void Init()
        {
            LoadConfig();
            if (!_enabled)
            {
                MelonLogger.Msg("PlayerDb: disabled in config.");
                return;
            }

            if (!_bootStarted)
            {
                _bootStarted = true;
                MelonCoroutines.Start(BootRoutine());
            }

            TryHookFusion();
        }

        public static void Tick()
        {
            if (!_enabled || !_dbConnected) return;

            _scanTimer += Time.unscaledDeltaTime;
            if (_scanTimer >= ScanIntervalSeconds)
            {
                _scanTimer = 0f;
                CollectLobby();
            }

            _flushTimer += Time.unscaledDeltaTime;
            if (_flushTimer >= FlushIntervalSeconds)
            {
                _flushTimer = 0f;
                TryFlushAsync();
            }
        }

        private static HttpClient CreateHttp()
        {
            var c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
            c.DefaultRequestHeaders.ExpectContinue = false;
            return c;
        }

        private static IEnumerator BootRoutine()
        {
            // Wait until Fusion UI / scene can show popups
            float t = 0f;
            while (t < BootDelaySeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            TryHookFusion();
            yield return HealthCheckAndNotify();
        }

        private static IEnumerator HealthCheckAndNotify()
        {
            if (string.IsNullOrEmpty(_apiUrl))
            {
                _dbConnected = false;
                NotifyDb(false, "API URL missing");
                yield break;
            }

            Task<bool> task = Task.Run(PingHealth);
            while (!task.IsCompleted) yield return null;

            bool ok = false;
            try { ok = task.Result; }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb health: " + e.Message);
                ok = false;
            }

            _dbConnected = ok;
            NotifyDb(ok, ok ? "Ingest + Postgres OK" : "Check ingest service / API key");
            if (ok) CollectLobby();
        }

        private static bool PingHealth()
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, Combine(_apiUrl, "/health"));
                using HttpResponseMessage resp = Http.Send(req);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb ping: " + e.Message);
                return false;
            }
        }

        private static void TryHookFusion()
        {
            if (_hooked) return;
            try
            {
                if (AccessTools.TypeByName("LabFusion.Utilities.MultiplayerHooking") == null) return;
                MultiplayerHooking.OnPlayerJoined += OnPlayerJoined;
                _hooked = true;
                MelonLogger.Msg("PlayerDb: hooked OnPlayerJoined.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb hook: " + e.Message);
            }
        }

        private static void OnPlayerJoined(PlayerID id)
        {
            try { EnqueuePlayer(id); }
            catch (Exception e) { MelonLogger.Warning("PlayerDb join: " + e.Message); }
        }

        private static void CollectLobby()
        {
            try
            {
                foreach (NetworkPlayer np in NetworkPlayer.Players)
                {
                    if (np == null || np.PlayerID == null) continue;
                    EnqueuePlayer(np.PlayerID, np.Username);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb scan: " + e.Message);
            }
        }

        private static void EnqueuePlayer(PlayerID id, string usernameOverride = null)
        {
            if (id == null) return;
            try { if (id.IsMe) return; } catch { return; }

            string pid = null;
            try { pid = id.PlatformID; } catch { }
            if (string.IsNullOrWhiteSpace(pid)) return;
            pid = pid.Trim();

            if (!Seen.TryAdd(pid, 0)) return;

            string name = usernameOverride;
            if (string.IsNullOrWhiteSpace(name))
                name = ResolveUsername(id);
            name = CleanName(name, id);

            Pending.Enqueue(new PlayerRow { Name = name, Pid = pid });
        }

        private static string ResolveUsername(PlayerID id)
        {
            try
            {
                byte sid = id.SmallID;
                foreach (NetworkPlayer np in NetworkPlayer.Players)
                {
                    if (np == null || np.PlayerID == null) continue;
                    if (np.PlayerID.SmallID == sid)
                        return np.Username;
                }
            }
            catch { }
            return null;
        }

        private static string CleanName(string username, PlayerID id)
        {
            byte sid = 0;
            try { sid = id.SmallID; } catch { }

            if (string.IsNullOrEmpty(username))
                return "Player " + sid;

            var sb = new StringBuilder(username.Length);
            bool inTag = false;
            foreach (char c in username)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;
                if (c >= 32 && c != 127) sb.Append(c);
            }

            string s = sb.ToString().Trim();
            if (s.Length == 0) return "Player " + sid;
            if (s.Length > 128) s = s.Substring(0, 128);
            return s;
        }

        private static void TryFlushAsync()
        {
            if (Interlocked.CompareExchange(ref _flushing, 1, 0) != 0) return;
            if (Pending.IsEmpty)
            {
                Interlocked.Exchange(ref _flushing, 0);
                return;
            }

            var batch = new List<PlayerRow>(MaxBatch);
            while (batch.Count < MaxBatch && Pending.TryDequeue(out PlayerRow row))
                batch.Add(row);

            if (batch.Count == 0)
            {
                Interlocked.Exchange(ref _flushing, 0);
                return;
            }

            Task.Run(() =>
            {
                try { PostBatch(batch); }
                catch (Exception e)
                {
                    MelonLogger.Warning("PlayerDb flush: " + e.Message);
                    foreach (PlayerRow row in batch)
                    {
                        Seen.TryRemove(row.Pid, out _);
                        Pending.Enqueue(row);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _flushing, 0);
                }
            });
        }

        private static void PostBatch(List<PlayerRow> batch)
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("ApiKey empty — set it in " + CfgName);

            var sb = new StringBuilder(256 + batch.Count * 96);
            sb.Append("{\"players\":[");
            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"name\":").Append(JsonString(batch[i].Name))
                  .Append(",\"pid\":").Append(JsonString(batch[i].Pid)).Append('}');
            }
            sb.Append("]}");

            using var req = new HttpRequestMessage(HttpMethod.Post, Combine(_apiUrl, "/v1/players"));
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
            req.Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");

            using HttpResponseMessage resp = Http.Send(req);
            if (!resp.IsSuccessStatusCode)
            {
                string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                throw new Exception(((int)resp.StatusCode) + " " + body);
            }
        }

        private static string JsonString(string s)
        {
            if (s == null) s = "";
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string Combine(string baseUrl, string path)
        {
            return baseUrl.TrimEnd('/') + path;
        }

        private static void NotifyDb(bool connected, string detail)
        {
            string title = connected ? "Database connected" : "Database not connected";
            MelonLogger.Msg("PlayerDb: " + title + " — " + detail);

            try
            {
                var n = new LabFusion.UI.Popups.Notification();
                n.Title = title;
                n.Message = detail;
                n.Type = connected
                    ? LabFusion.UI.Popups.NotificationType.SUCCESS
                    : LabFusion.UI.Popups.NotificationType.ERROR;
                n.ShowPopup = true;
                n.PopupLength = connected ? 3.5f : 4.5f;
                LabFusion.UI.Popups.Notifier.Send(n);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb notify: " + e.Message);
            }
        }

        private static void LoadConfig()
        {
            try
            {
                string path = CfgPath();
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(path))
                {
                    File.WriteAllText(path,
                        "# MonsterPanel PlayerDb\n" +
                        "Enabled=true\n" +
                        "ApiUrl=" + DefaultApiUrl + "\n" +
                        "ApiKey=\n" +
                        "# Put the same key as INGEST_API_KEY on the ingest server.\n");
                    MelonLogger.Warning("PlayerDb: created " + path + " — set ApiKey.");
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
                MelonLogger.Warning("PlayerDb config: " + e.Message);
            }
        }

        private static string CfgPath()
        {
            string root;
            try { root = MelonLoader.Utils.MelonEnvironment.UserDataDirectory; }
            catch { root = "UserData"; }
            return Path.Combine(root, "MonsterPanel", CfgName);
        }
    }
}
