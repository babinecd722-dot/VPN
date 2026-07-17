using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using LabFusion.Entities;
using LabFusion.Player;
using LabFusion.Utilities;
using MelonLoader;
using Npgsql;
using NpgsqlTypes;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Fusion lobby → Postgres client_data (direct, async, deduped).
    /// Password XOR-obfuscated in binary. Single-file via EmbeddedDeps.
    /// </summary>
    internal static class PlayerDb
    {
        private const float BootDelaySeconds = 4f;
        private const float ScanIntervalIdle = 10f;
        private const float ScanIntervalWaitingNames = 3f;
        private const float FlushIntervalSeconds = 1.5f;
        private const float FlushBackoffSeconds = 15f;
        private const int MaxBatch = 48;
        private const int MaxNameMissesBeforeFallback = 5;

        private const string Host = "62.109.21.131";
        private const int Port = 5432;
        private const string Database = "clientdb";
        private const string Username = "client_writer";

        private static readonly byte[] PassBlob =
        {
            57, 164, 117, 36, 223, 84, 169, 60, 11, 178, 74, 8, 249, 82, 216, 65,
            52, 147, 72, 63, 193, 79, 252, 108
        };
        private static readonly byte[] PassKey = { 0x5A, 0xC3, 0x19, 0x7E, 0xB4, 0x22, 0x91, 0x0D };

        private static bool _enabled = true;
        private static bool _hooked;
        private static bool _bootStarted;
        private static bool _dbConnected;
        private static float _scanTimer;
        private static float _flushTimer;
        private static long _flushBackoffUntilMs; // Environment.TickCount64 — safe from worker threads
        private static string _connString;
        private static int _flushing;
        private static int _waitingNames;

        private static readonly ConcurrentDictionary<string, byte> Seen =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, int> NameMisses =
            new ConcurrentDictionary<string, int>(StringComparer.Ordinal);
        private static readonly ConcurrentQueue<PlayerRow> Pending = new ConcurrentQueue<PlayerRow>();

        private struct PlayerRow
        {
            public string Name;
            public string Pid;
        }

        public static bool DatabaseConnected => _dbConnected;

        public static void Init()
        {
            try
            {
                _connString = BuildConnString();
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb: conn string — " + e.Message);
                _enabled = false;
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

            float scanEvery = _waitingNames > 0 ? ScanIntervalWaitingNames : ScanIntervalIdle;
            _scanTimer += Time.unscaledDeltaTime;
            if (_scanTimer >= scanEvery)
            {
                _scanTimer = 0f;
                if (InFusionSession())
                    CollectLobby();
            }

            if (Environment.TickCount64 < Interlocked.Read(ref _flushBackoffUntilMs)) return;

            _flushTimer += Time.unscaledDeltaTime;
            if (_flushTimer >= FlushIntervalSeconds)
            {
                _flushTimer = 0f;
                TryFlushAsync();
            }
        }

        private static bool InFusionSession()
        {
            try { return LabFusion.Network.NetworkInfo.HasServer; }
            catch { return true; } // if API missing, still try scan
        }

        private static string BuildConnString()
        {
            string password = Reveal(PassBlob, PassKey);
            var cs = new NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = Database,
                Username = Username,
                Password = password,
                Timeout = 8,
                CommandTimeout = 8,
                Pooling = true,
                MinPoolSize = 0,
                MaxPoolSize = 2,
                ConnectionIdleLifetime = 30,
                SslMode = SslMode.Prefer,
                ApplicationName = "MonsterPanel",
                KeepAlive = 0,
            };
            return cs.ConnectionString;
        }

        private static string Reveal(byte[] blob, byte[] key)
        {
            var buf = new byte[blob.Length];
            for (int i = 0; i < blob.Length; i++)
                buf[i] = (byte)(blob[i] ^ key[i % key.Length]);
            return Encoding.UTF8.GetString(buf);
        }

        private static IEnumerator BootRoutine()
        {
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
            var task = Task.Run(PingDatabase);
            while (!task.IsCompleted) yield return null;

            bool ok = false;
            try
            {
                if (task.IsFaulted)
                {
                    Exception ex = task.Exception?.GetBaseException();
                    MelonLogger.Warning("PlayerDb health: " + (ex != null ? ex.Message : "faulted"));
                }
                else
                {
                    ok = task.Result;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb health: " + e.Message);
            }

            _dbConnected = ok;
            NotifyDb(ok, ok ? "PostgreSQL OK" : "Cannot reach PostgreSQL");
            if (ok && InFusionSession())
                CollectLobby();
        }

        private static bool PingDatabase()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                object v = cmd.ExecuteScalar();
                return v != null && v != DBNull.Value;
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
            // Nick usually empty on join — never lock PID / never burn name-miss budget here.
            try { EnqueuePlayer(id, null, countMiss: false, allowFallback: false); }
            catch (Exception e) { MelonLogger.Warning("PlayerDb join: " + e.Message); }
        }

        private static void CollectLobby()
        {
            int waiting = 0;
            try
            {
                foreach (NetworkPlayer np in NetworkPlayer.Players)
                {
                    if (np == null || np.PlayerID == null) continue;
                    if (EnqueuePlayer(np.PlayerID, np.Username, countMiss: true, allowFallback: true))
                        waiting++; // still waiting on name
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb scan: " + e.Message);
            }
            _waitingNames = waiting;
        }

        /// <returns>true if we are still waiting for a real username for this pid</returns>
        private static bool EnqueuePlayer(PlayerID id, string usernameOverride, bool countMiss, bool allowFallback)
        {
            if (id == null) return false;
            try { if (id.IsMe) return false; } catch { return false; }

            string pid;
            try { pid = id.PlatformID; }
            catch { return false; }
            if (string.IsNullOrWhiteSpace(pid)) return false;
            pid = pid.Trim();
            if (pid.Length > 128) pid = pid.Substring(0, 128);

            if (Seen.ContainsKey(pid)) return false;

            string name = usernameOverride;
            if (string.IsNullOrWhiteSpace(name))
                name = ResolveUsername(id);
            name = CleanNameForDb(name);

            byte sid = 0;
            try { sid = id.SmallID; } catch { }

            if (string.IsNullOrEmpty(name))
            {
                if (countMiss)
                {
                    int misses = NameMisses.AddOrUpdate(pid, 1, (_, n) => n + 1);
                    if (!allowFallback || misses < MaxNameMissesBeforeFallback)
                        return true; // keep waiting
                    name = "Player " + sid;
                }
                else
                {
                    return true; // join frame — wait for scan
                }
            }
            else
            {
                NameMisses.TryRemove(pid, out _);
            }

            if (!Seen.TryAdd(pid, 0)) return false;
            Pending.Enqueue(new PlayerRow { Name = name, Pid = pid });
            return false;
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

        private static string CleanNameForDb(string username)
        {
            if (string.IsNullOrEmpty(username)) return null;

            var sb = new StringBuilder(username.Length);
            bool inTag = false;
            foreach (char c in username)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;
                if (c == '\n' || c == '\r' || c == '\t') { sb.Append(' '); continue; }
                if (char.IsControl(c)) continue;
                sb.Append(c);
            }

            string s = sb.ToString().Trim();
            if (s.Length == 0) return null;
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
            var dedup = new HashSet<string>(StringComparer.Ordinal);
            while (batch.Count < MaxBatch && Pending.TryDequeue(out PlayerRow row))
            {
                if (!dedup.Add(row.Pid)) continue;
                batch.Add(row);
            }

            if (batch.Count == 0)
            {
                Interlocked.Exchange(ref _flushing, 0);
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    WriteBatch(batch);
                    Interlocked.Exchange(ref _flushBackoffUntilMs, 0);
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("PlayerDb flush: " + e.Message);
                    Interlocked.Exchange(
                        ref _flushBackoffUntilMs,
                        Environment.TickCount64 + (long)(FlushBackoffSeconds * 1000));
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

        /// <summary>One SELECT for the whole batch, then INSERT only missing pids.</summary>
        private static void WriteBatch(List<PlayerRow> batch)
        {
            var byPid = new Dictionary<string, string>(batch.Count, StringComparer.Ordinal);
            foreach (PlayerRow row in batch)
                byPid[row.Pid] = row.Name;

            string[] pids = new string[byPid.Count];
            byPid.Keys.CopyTo(pids, 0);

            using var conn = new NpgsqlConnection(_connString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            var existing = new HashSet<string>(StringComparer.Ordinal);
            using (var existsCmd = new NpgsqlCommand(
                       "SELECT pid FROM client_data WHERE pid = ANY(@pids)", conn, tx))
            {
                existsCmd.Parameters.AddWithValue("pids", NpgsqlDbType.Array | NpgsqlDbType.Text, pids);
                using var reader = existsCmd.ExecuteReader();
                while (reader.Read())
                    existing.Add(reader.GetString(0));
            }

            if (existing.Count < byPid.Count)
            {
                using var insertCmd = new NpgsqlCommand(
                    "INSERT INTO client_data (name, pid) VALUES (@name, @pid)", conn, tx);
                var pName = insertCmd.Parameters.Add("name", NpgsqlDbType.Text);
                var pPid = insertCmd.Parameters.Add("pid", NpgsqlDbType.Text);
                insertCmd.Prepare();

                foreach (KeyValuePair<string, string> kv in byPid)
                {
                    if (existing.Contains(kv.Key)) continue;
                    pName.Value = kv.Value;
                    pPid.Value = kv.Key;
                    insertCmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
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
    }
}
