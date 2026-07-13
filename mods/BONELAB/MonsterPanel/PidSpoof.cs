using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BoneLib.BoneMenu;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using HarmonyLib;
using LabFusion.Player;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Spoofing PID (Quest / EOS ProductUserId).
    ///
    /// First enable: silently provisions a fresh EOS DeviceId + CreateUser ("zero" Fusion
    /// account), stores its Platform ID to disk, and swaps LocalPlatformID to it.
    /// While enabled: every SetPlatformID call is forced to the saved zero-account ID
    /// so Fusion never keeps the real account PID.
    /// </summary>
    internal static class PidSpoof
    {
        public static bool Enabled { get; private set; }
        public static string SpoofPlatformId { get; private set; } = "";
        public static string OriginalPlatformId { get; private set; } = "";

        private static bool _hooked;
        private static bool _provisioning;
        private static bool _ensureStarted;
        private static HarmonyLib.Harmony _harmony;

        private const string FileName = "pid_spoof.cfg";
        private const float ConnectWaitSeconds = 90f;

        public static void Init(HarmonyLib.Harmony harmony)
        {
            _harmony = harmony;
            Load();
            InstallHook();
            if (Enabled)
                StartEnsure();
        }

        public static void InstallMenu(Page root)
        {
            root.CreateBool("Spoofing PID", new Color(0.85f, 0.55f, 0.15f), Enabled, OnToggle);
        }

        private static void OnToggle(bool on)
        {
            Enabled = on;
            Save();
            MelonLogger.Msg(on ? "Spoofing PID: ON" : "Spoofing PID: OFF");

            if (!on)
                return;

            if (string.IsNullOrEmpty(SpoofPlatformId))
                StartEnsure(); // first enable → silent EOS CreateUser
            else
                ApplyNow();
        }

        private static void StartEnsure()
        {
            if (_ensureStarted) return;
            _ensureStarted = true;
            MelonCoroutines.Start(EnsureRoutine());
        }

        private static IEnumerator EnsureRoutine()
        {
            // Wait until EOS Connect is up (Fusion logged into Epic Online Services).
            float waited = 0f;
            while (GetConnect() == null && waited < ConnectWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (GetConnect() == null)
            {
                MelonLogger.Warning("Spoofing PID: EOS Connect not ready — enable Spoofing PID after Fusion EOS login.");
                _ensureStarted = false;
                yield break;
            }

            // Capture whatever Fusion currently thinks is us (real account), once.
            TryRememberOriginal(PlayerIDManager.LocalPlatformID);

            if (Enabled && string.IsNullOrEmpty(SpoofPlatformId))
            {
                yield return ProvisionZeroAccount();
            }
            else if (Enabled && !string.IsNullOrEmpty(SpoofPlatformId))
            {
                // Device binding lost (e.g. app data wipe) → Connect user ≠ saved spoof.
                // Re-provision a fresh zero account and overwrite the file.
                string live = GetLoggedInProductUserId();
                if (!string.IsNullOrEmpty(live) && !string.Equals(live, SpoofPlatformId, StringComparison.Ordinal))
                {
                    MelonLogger.Msg("Spoofing PID: saved PID not bound to this device — provisioning new zero account.");
                    SpoofPlatformId = "";
                    Save();
                    yield return ProvisionZeroAccount();
                }
                else
                {
                    ApplyNow();
                }
            }

            _ensureStarted = false;
        }

        /// <summary>
        /// Silent EOS zero-account provision (Quest DeviceId path):
        /// Logout → DeleteDeviceId → CreateDeviceId → Login(DeviceidAccessToken) →
        /// CreateUser(ContinuanceToken) → save ProductUserId → SetPlatformID.
        /// </summary>
        private static IEnumerator ProvisionZeroAccount()
        {
            if (_provisioning) yield break;
            _provisioning = true;

            ConnectInterface connect = GetConnect();
            if (connect == null)
            {
                MelonLogger.Warning("Spoofing PID: provision aborted — no ConnectInterface.");
                _provisioning = false;
                yield break;
            }

            TryRememberOriginal(PlayerIDManager.LocalPlatformID);

            // 1) Logout current Connect session if any (ignore failures).
            yield return LogoutCurrent(connect);

            // 2) Delete existing device id so Login returns InvalidUser + ContinuanceToken.
            {
                bool done = false;
                try
                {
                    var del = new DeleteDeviceIdOptions();
                    connect.DeleteDeviceId(ref del, null, (ref DeleteDeviceIdCallbackInfo info) => { done = true; });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: DeleteDeviceId — " + e.Message);
                    _provisioning = false;
                    yield break;
                }
                float t = 0f;
                while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            }

            // 3) CreateDeviceId (Success or DuplicateNotAllowed are both fine).
            bool deviceOk = false;
            {
                bool done = false;
                try
                {
                    var create = new CreateDeviceIdOptions
                    {
                        DeviceModel = string.IsNullOrEmpty(SystemInfo.deviceModel) ? "Quest" : SystemInfo.deviceModel,
                    };
                    connect.CreateDeviceId(ref create, null, (ref CreateDeviceIdCallbackInfo info) =>
                    {
                        deviceOk = info.ResultCode == Result.Success || info.ResultCode == Result.DuplicateNotAllowed;
                        if (!deviceOk) MelonLogger.Warning("Spoofing PID: CreateDeviceId → " + info.ResultCode);
                        done = true;
                    });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId — " + e.Message);
                    _provisioning = false;
                    yield break;
                }
                float t = 0f;
                while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
                if (!deviceOk)
                {
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId failed.");
                    _provisioning = false;
                    yield break;
                }
            }

            // 4) Login with DeviceidAccessToken (Quest EOS path). Expect InvalidUser → CreateUser.
            ContinuanceToken continuance = null;
            ProductUserId created = null;
            {
                bool done = false;
                Result code = Result.UnexpectedError;
                try
                {
                    var login = new LoginOptions
                    {
                        Credentials = new Credentials
                        {
                            Type = ExternalCredentialType.DeviceidAccessToken,
                            Token = string.Empty,
                        },
                        UserLoginInfo = new UserLoginInfo
                        {
                            DisplayName = "Player" + UnityEngine.Random.Range(1000, 99999),
                        },
                    };

                    connect.Login(ref login, null, (ref LoginCallbackInfo info) =>
                    {
                        code = info.ResultCode;
                        if (info.ResultCode == Result.Success)
                            created = info.LocalUserId;
                        else if (info.ResultCode == Result.InvalidUser)
                            continuance = info.ContinuanceToken;
                        else
                            MelonLogger.Warning("Spoofing PID: Login → " + info.ResultCode);
                        done = true;
                    });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: Login — " + e.Message);
                    _provisioning = false;
                    yield break;
                }

                float t = 0f;
                while (!done && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }

                if (code == Result.InvalidUser && continuance != null)
                {
                    bool cDone = false;
                    try
                    {
                        var cu = new CreateUserOptions { ContinuanceToken = continuance };
                        connect.CreateUser(ref cu, null, (ref CreateUserCallbackInfo info) =>
                        {
                            if (info.ResultCode == Result.Success)
                                created = info.LocalUserId;
                            else
                                MelonLogger.Warning("Spoofing PID: CreateUser → " + info.ResultCode);
                            cDone = true;
                        });
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Warning("Spoofing PID: CreateUser — " + e.Message);
                        _provisioning = false;
                        yield break;
                    }
                    t = 0f;
                    while (!cDone && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
                }
            }

            string pid = null;
            try { if (created != null && created.IsValid()) pid = created.ToString(); } catch { }
            if (string.IsNullOrEmpty(pid))
            {
                MelonLogger.Warning("Spoofing PID: failed to obtain zero-account Platform ID.");
                _provisioning = false;
                yield break;
            }

            SpoofPlatformId = pid;
            Save();
            ApplyNow();
            SyncAuthManagerLocalUserId(created);
            MelonLogger.Msg("Spoofing PID: zero account ready (" + Short(pid) + ").");
            _provisioning = false;
        }

        private static IEnumerator LogoutCurrent(ConnectInterface connect)
        {
            string current = PlayerIDManager.LocalPlatformID;
            if (string.IsNullOrEmpty(current)) yield break;

            ProductUserId user = null;
            try { user = ProductUserId.FromString(current); } catch { }
            if (user == null || !user.IsValid()) yield break;

            bool done = false;
            try
            {
                var opts = new LogoutOptions { LocalUserId = user };
                connect.Logout(ref opts, null, (ref LogoutCallbackInfo _) => { done = true; });
            }
            catch
            {
                yield break;
            }

            float t = 0f;
            while (!done && t < 10f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private static void ApplyNow()
        {
            if (!Enabled || string.IsNullOrEmpty(SpoofPlatformId)) return;
            try
            {
                // Goes through our Harmony prefix (provisioning flag off → forced to SpoofPlatformId).
                PlayerIDManager.SetPlatformID(SpoofPlatformId);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: ApplyNow — " + e.Message);
            }
        }

        private static void InstallHook()
        {
            if (_hooked || _harmony == null) return;
            try
            {
                MethodInfo target = AccessTools.Method(typeof(PlayerIDManager), nameof(PlayerIDManager.SetPlatformID), new[] { typeof(string) });
                if (target == null)
                {
                    MelonLogger.Warning("Spoofing PID: PlayerIDManager.SetPlatformID not found.");
                    return;
                }
                _harmony.Patch(target, prefix: new HarmonyMethod(typeof(PidSpoof), nameof(SetPlatformIDPrefix)));
                _hooked = true;
                MelonLogger.Msg("Spoofing PID: hooked PlayerIDManager.SetPlatformID.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: hook failed — " + e.Message);
            }
        }

        /// <summary>Harmony prefix: remember real PID, force spoof PID while enabled.</summary>
        private static void SetPlatformIDPrefix(ref string platformID)
        {
            if (_provisioning) return;

            TryRememberOriginal(platformID);

            if (!Enabled) return;
            if (string.IsNullOrEmpty(SpoofPlatformId)) return;
            if (string.Equals(platformID, SpoofPlatformId, StringComparison.Ordinal)) return;

            platformID = SpoofPlatformId;
        }

        private static void TryRememberOriginal(string platformID)
        {
            if (string.IsNullOrEmpty(platformID)) return;
            if (!string.IsNullOrEmpty(SpoofPlatformId) &&
                string.Equals(platformID, SpoofPlatformId, StringComparison.Ordinal))
                return;
            if (string.Equals(platformID, OriginalPlatformId, StringComparison.Ordinal)) return;

            OriginalPlatformId = platformID;
            Save();
        }

        private static ConnectInterface GetConnect()
        {
            try
            {
                Type t = AccessTools.TypeByName("LabFusion.Network.EpicGames.EOSInterfaces");
                if (t == null) return null;
                PropertyInfo p = AccessTools.Property(t, "Connect");
                return p?.GetValue(null) as ConnectInterface;
            }
            catch { return null; }
        }

        private static string GetLoggedInProductUserId()
        {
            try
            {
                ConnectInterface connect = GetConnect();
                if (connect == null) return null;
                int count = connect.GetLoggedInUsersCount();
                if (count <= 0) return null;
                ProductUserId user = connect.GetLoggedInUserByIndex(0);
                if (user == null || !user.IsValid()) return null;
                return user.ToString();
            }
            catch { return null; }
        }

        /// <summary>Keep Fusion's EOSAuthManager.LocalUserId aligned with the spoofed PUID when possible.</summary>
        private static void SyncAuthManagerLocalUserId(ProductUserId user)
        {
            if (user == null || !user.IsValid()) return;
            try
            {
                // EpicGamesNetworkLayer instance → private _authManager → LocalUserId setter
                Type layerType = AccessTools.TypeByName("LabFusion.Network.EpicGames.EpicGamesNetworkLayer");
                Type netInfo = AccessTools.TypeByName("LabFusion.Network.NetworkInfo");
                if (layerType == null || netInfo == null) return;

                object layer = AccessTools.Property(netInfo, "CurrentNetworkLayer")?.GetValue(null);
                if (layer == null || !layerType.IsInstanceOfType(layer)) return;

                FieldInfo authField = AccessTools.Field(layerType, "_authManager");
                object auth = authField?.GetValue(layer);
                if (auth == null) return;

                PropertyInfo local = AccessTools.Property(auth.GetType(), "LocalUserId");
                if (local != null && local.CanWrite)
                    local.SetValue(auth, user);
                else
                {
                    FieldInfo bak = AccessTools.Field(auth.GetType(), "<LocalUserId>k__BackingField");
                    bak?.SetValue(auth, user);
                }
            }
            catch { /* best-effort */ }
        }

        private static string ConfigPath()
        {
            string root;
            try { root = MelonLoader.Utils.MelonEnvironment.UserDataDirectory; }
            catch { root = "UserData"; }
            string dir = Path.Combine(root, "MonsterPanel");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, FileName);
        }

        private static void Load()
        {
            try
            {
                string path = ConfigPath();
                if (!File.Exists(path)) return;
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw?.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    switch (key)
                    {
                        case "enabled":
                            Enabled = val == "1" || val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "spoof":
                            SpoofPlatformId = val ?? "";
                            break;
                        case "original":
                            OriginalPlatformId = val ?? "";
                            break;
                    }
                }
                if (Enabled)
                    MelonLogger.Msg("Spoofing PID: loaded (spoof=" + Short(SpoofPlatformId) + ").");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: load failed — " + e.Message);
            }
        }

        private static void Save()
        {
            try
            {
                string path = ConfigPath();
                File.WriteAllText(path,
                    "# MONSTER Panel — Spoofing PID (EOS ProductUserId)\n" +
                    "enabled=" + (Enabled ? "1" : "0") + "\n" +
                    "spoof=" + (SpoofPlatformId ?? "") + "\n" +
                    "original=" + (OriginalPlatformId ?? "") + "\n");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: save failed — " + e.Message);
            }
        }

        private static string Short(string id)
        {
            if (string.IsNullOrEmpty(id)) return "(none)";
            return id.Length <= 12 ? id : id.Substring(0, 6) + "…" + id.Substring(id.Length - 4);
        }
    }
}
