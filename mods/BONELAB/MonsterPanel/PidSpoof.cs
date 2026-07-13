using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
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
    /// Spoofing PID — Quest / EOS ProductUserId.
    ///
    /// Zero-account is a SEPARATE EOS ProductUserId created via Connect.CreateUser
    /// after DeleteDeviceId + CreateDeviceId (fresh device credential, not the original).
    /// Original PID is remembered in the config file; LocalPlatformID is forced to the
    /// zero PID while the toggle is on.
    ///
    /// During first provision we temporarily spoof SystemInfo device fingerprint and
    /// pass a randomized DeviceModel into CreateDeviceId so the new credential isn't
    /// tied to the real device label. No IP changes.
    /// </summary>
    internal static class PidSpoof
    {
        public static bool Enabled { get; private set; }
        public static string SpoofPlatformId { get; private set; } = "";
        public static string OriginalPlatformId { get; private set; } = "";
        public static string SpoofDeviceModel { get; private set; } = "";

        private static bool _hooked;
        private static bool _deviceHooksInstalled;
        private static bool _provisioning;
        private static bool _ensureStarted;
        private static bool _fingerprintActive;
        private static HarmonyLib.Harmony _harmony;

        private static string _fakeDeviceModel = "";
        private static string _fakeDeviceName = "";
        private static string _fakeDeviceUid = "";
        private static string _fakeOs = "";

        private static readonly string[] QuestModels =
        {
            "Quest 2", "Quest 3", "Quest 3S", "Meta Quest 2", "Meta Quest 3",
            "Oculus Quest 2", "Pacific", "Seacliff", "Eureka",
        };

        private const string FileName = "pid_spoof.cfg";
        private const float ConnectWaitSeconds = 90f;

        public static void Init(HarmonyLib.Harmony harmony)
        {
            _harmony = harmony;
            Load();
            InstallPidHook();
            InstallDeviceHooks();
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
                StartEnsure();
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
            float waited = 0f;
            while (GetConnect() == null && waited < ConnectWaitSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (GetConnect() == null)
            {
                MelonLogger.Warning("Spoofing PID: EOS Connect not ready — turn it on after Fusion EOS login.");
                _ensureStarted = false;
                yield break;
            }

            TryRememberOriginal(PlayerIDManager.LocalPlatformID);

            if (Enabled && string.IsNullOrEmpty(SpoofPlatformId))
            {
                yield return ProvisionZeroAccount();
            }
            else if (Enabled && !string.IsNullOrEmpty(SpoofPlatformId))
            {
                string live = GetLoggedInProductUserId();
                if (!string.IsNullOrEmpty(live) &&
                    !string.Equals(live, SpoofPlatformId, StringComparison.Ordinal))
                {
                    MelonLogger.Msg("Spoofing PID: saved PID not bound on this device — provisioning new zero account.");
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
        /// Silent provision of a SEPARATE EOS zero account:
        /// Logout → DeleteDeviceId (drop original device credential) →
        /// CreateDeviceId (fresh credential + spoofed DeviceModel/SystemInfo) →
        /// Login(DeviceidAccessToken) → CreateUser → save new ProductUserId.
        /// Original ProductUserId stays in config as original= (separate identity).
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
            BeginFingerprintSpoof();

            // 1) Logout current Connect session (best-effort).
            yield return LogoutCurrent(connect);

            // 2) Delete existing EOS device credential so Login returns InvalidUser + ContinuanceToken.
            {
                bool done = false;
                try
                {
                    var del = new DeleteDeviceIdOptions();
                    connect.DeleteDeviceId(ref del, null, (ref DeleteDeviceIdCallbackInfo _) => { done = true; });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: DeleteDeviceId — " + e.Message);
                    EndFingerprintSpoof();
                    _provisioning = false;
                    yield break;
                }
                float t = 0f;
                while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
            }

            // 3) CreateDeviceId with spoofed DeviceModel (new device credential, separate from original).
            bool deviceOk = false;
            {
                bool done = false;
                string model = string.IsNullOrEmpty(_fakeDeviceModel) ? "Meta Quest 3" : _fakeDeviceModel;
                try
                {
                    var create = new CreateDeviceIdOptions { DeviceModel = model };
                    connect.CreateDeviceId(ref create, null, (ref CreateDeviceIdCallbackInfo info) =>
                    {
                        deviceOk = info.ResultCode == Result.Success || info.ResultCode == Result.DuplicateNotAllowed;
                        if (!deviceOk)
                            MelonLogger.Warning("Spoofing PID: CreateDeviceId → " + info.ResultCode);
                        done = true;
                    });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId — " + e.Message);
                    EndFingerprintSpoof();
                    _provisioning = false;
                    yield break;
                }
                float t = 0f;
                while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
                if (!deviceOk)
                {
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId failed.");
                    EndFingerprintSpoof();
                    _provisioning = false;
                    yield break;
                }
                SpoofDeviceModel = model;
            }

            // Fingerprint spoof only needed around CreateDeviceId — release before Login/CreateUser.
            EndFingerprintSpoof();

            // 4) Login → InvalidUser → CreateUser (new ProductUserId, separate from original).
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
            MelonLogger.Msg("Spoofing PID: zero account ready (pid=" + Short(pid) +
                            ", device=" + SpoofDeviceModel + ", original=" + Short(OriginalPlatformId) + ").");
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
            try { PlayerIDManager.SetPlatformID(SpoofPlatformId); }
            catch (Exception e) { MelonLogger.Warning("Spoofing PID: ApplyNow — " + e.Message); }
        }

        // ---------------- Device fingerprint spoof (provision window only) ----------------

        private static void BeginFingerprintSpoof()
        {
            GenerateFingerprint();
            _fingerprintActive = true;
            MelonLogger.Msg("Spoofing PID: device fingerprint spoof active (" + _fakeDeviceModel + ").");
        }

        private static void EndFingerprintSpoof()
        {
            _fingerprintActive = false;
        }

        private static void GenerateFingerprint()
        {
            // Stable-looking but random Quest-side identity for this provision.
            string model = QuestModels[UnityEngine.Random.Range(0, QuestModels.Length)];
            string uid = RandomHex(16);
            _fakeDeviceModel = model;
            _fakeDeviceName = model + "-" + uid.Substring(0, 4);
            _fakeDeviceUid = uid;
            _fakeOs = "Android OS 12 / API-32 (arm64-v8a)";
        }

        private static string RandomHex(int bytes)
        {
            var sb = new StringBuilder(bytes * 2);
            for (int i = 0; i < bytes; i++)
                sb.Append(UnityEngine.Random.Range(0, 256).ToString("x2"));
            return sb.ToString();
        }

        private static void InstallDeviceHooks()
        {
            if (_deviceHooksInstalled || _harmony == null) return;
            try
            {
                Type t = typeof(SystemInfo);
                PatchGetter(t, "deviceModel", nameof(DeviceModelPrefix));
                PatchGetter(t, "deviceName", nameof(DeviceNamePrefix));
                PatchGetter(t, "deviceUniqueIdentifier", nameof(DeviceUidPrefix));
                PatchGetter(t, "operatingSystem", nameof(OperatingSystemPrefix));
                _deviceHooksInstalled = true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: device hooks — " + e.Message);
            }
        }

        private static void PatchGetter(Type type, string prop, string prefixName)
        {
            MethodInfo getter = AccessTools.PropertyGetter(type, prop);
            if (getter == null) return;
            _harmony.Patch(getter, prefix: new HarmonyMethod(typeof(PidSpoof), prefixName));
        }

        // Prefixes only rewrite while _fingerprintActive — otherwise fall through to real values.
        private static bool DeviceModelPrefix(ref string __result)
        {
            if (!_fingerprintActive || string.IsNullOrEmpty(_fakeDeviceModel)) return true;
            __result = _fakeDeviceModel;
            return false;
        }

        private static bool DeviceNamePrefix(ref string __result)
        {
            if (!_fingerprintActive || string.IsNullOrEmpty(_fakeDeviceName)) return true;
            __result = _fakeDeviceName;
            return false;
        }

        private static bool DeviceUidPrefix(ref string __result)
        {
            if (!_fingerprintActive || string.IsNullOrEmpty(_fakeDeviceUid)) return true;
            __result = _fakeDeviceUid;
            return false;
        }

        private static bool OperatingSystemPrefix(ref string __result)
        {
            if (!_fingerprintActive || string.IsNullOrEmpty(_fakeOs)) return true;
            __result = _fakeOs;
            return false;
        }

        // ---------------- PID hook ----------------

        private static void InstallPidHook()
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
                MelonLogger.Warning("Spoofing PID: PID hook failed — " + e.Message);
            }
        }

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
                if (connect.GetLoggedInUsersCount() <= 0) return null;
                ProductUserId user = connect.GetLoggedInUserByIndex(0);
                if (user == null || !user.IsValid()) return null;
                return user.ToString();
            }
            catch { return null; }
        }

        private static void SyncAuthManagerLocalUserId(ProductUserId user)
        {
            if (user == null || !user.IsValid()) return;
            try
            {
                Type layerType = AccessTools.TypeByName("LabFusion.Network.EpicGames.EpicGamesNetworkLayer");
                Type netInfo = AccessTools.TypeByName("LabFusion.Network.NetworkInfo");
                if (layerType == null || netInfo == null) return;

                object layer = AccessTools.Property(netInfo, "CurrentNetworkLayer")?.GetValue(null);
                if (layer == null || !layerType.IsInstanceOfType(layer)) return;

                object auth = AccessTools.Field(layerType, "_authManager")?.GetValue(layer);
                if (auth == null) return;

                PropertyInfo local = AccessTools.Property(auth.GetType(), "LocalUserId");
                if (local != null && local.CanWrite)
                    local.SetValue(auth, user);
                else
                    AccessTools.Field(auth.GetType(), "<LocalUserId>k__BackingField")?.SetValue(auth, user);
            }
            catch { }
        }

        // ---------------- Persist ----------------

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
                        case "device":
                            SpoofDeviceModel = val ?? "";
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
                File.WriteAllText(ConfigPath(),
                    "# MONSTER Panel — Spoofing PID (EOS ProductUserId)\n" +
                    "# spoof = zero-account PID (separate CreateUser identity)\n" +
                    "# original = real PID remembered before provision\n" +
                    "enabled=" + (Enabled ? "1" : "0") + "\n" +
                    "spoof=" + (SpoofPlatformId ?? "") + "\n" +
                    "original=" + (OriginalPlatformId ?? "") + "\n" +
                    "device=" + (SpoofDeviceModel ?? "") + "\n");
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
