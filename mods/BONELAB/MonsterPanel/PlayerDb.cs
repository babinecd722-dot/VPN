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
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Fusion lobby → Postgres client_data (direct).
    /// Connection password is XOR-obfuscated in the binary (not plaintext).
    /// </summary>
    internal static class PlayerDb
    {
        private const float BootDelaySeconds = 4f;
        private const float ScanIntervalSeconds = 8f;
        private const float FlushIntervalSeconds = 2f;
        private const int MaxBatch = 32;

        private const string Host = "62.109.21.131";
        private const int Port = 5432;
        private const string Database = "clientdb";
        private const string Username = "client_writer";

        // XOR-obfuscated password bytes (not stored as a plain string literal)
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
        private static string _connString;
        private static int _flushing;

        private static readonly ConcurrentDictionary<string, byte> Seen =
            new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
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
                MaxPoolSize = 2,
                SslMode = SslMode.Prefer,
                ApplicationName = "MonsterPanel",
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
            Task<bool> task = Task.Run(PingDatabase);
            while (!task.IsCompleted) yield return null;

            bool ok = false;
            try { ok = task.Result; }
            catch (Exception e)
            {
                MelonLogger.Warning("PlayerDb health: " + e.Message);
            }

            _dbConnected = ok;
            NotifyDb(ok, ok ? "PostgreSQL OK" : "Cannot reach PostgreSQL");
            if (ok) CollectLobby();
        }

        private static bool PingDatabase()
        {
            try
            {
                using var conn = new NpgsqlConnection(_connString);
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                cmd.ExecuteScalar();
                return true;
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
            if (pid.Length > 128) pid = pid.Substring(0, 128);

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
                try { WriteBatch(batch); }
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

        private static void WriteBatch(List<PlayerRow> batch)
        {
            using var conn = new NpgsqlConnection(_connString);
            conn.Open();
            using var tx = conn.BeginTransaction();

            using (var existsCmd = new NpgsqlCommand(
                       "SELECT 1 FROM client_data WHERE pid = @pid LIMIT 1", conn, tx))
            {
                existsCmd.Parameters.Add("pid", NpgsqlTypes.NpgsqlDbType.Text);

                using var insertCmd = new NpgsqlCommand(
                    "INSERT INTO client_data (name, pid) VALUES (@name, @pid)", conn, tx);
                insertCmd.Parameters.Add("name", NpgsqlTypes.NpgsqlDbType.Text);
                insertCmd.Parameters.Add("pid", NpgsqlTypes.NpgsqlDbType.Text);

                foreach (PlayerRow row in batch)
                {
                    existsCmd.Parameters["pid"].Value = row.Pid;
                    object hit = existsCmd.ExecuteScalar();
                    if (hit != null && hit != DBNull.Value)
                        continue;

                    insertCmd.Parameters["name"].Value = row.Name;
                    insertCmd.Parameters["pid"].Value = row.Pid;
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
