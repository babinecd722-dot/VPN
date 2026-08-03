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
        private static bool _cancel;
        private static bool _fingerprintActive;
        private static HarmonyLib.Harmony _harmony;

        /// <summary>Cached Connect getter — Fusion 0.0.x EOSInterfaces or 0.1.x EOSContext.</summary>
        private static Func<ConnectInterface> _connectResolver;
        private static string _connectResolverKind;
        private static bool _loggedResolver;

        private static readonly MethodInfo[] _deviceGetters = new MethodInfo[4];
        private static readonly HarmonyMethod[] _devicePrefixes = new HarmonyMethod[4];

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
            // Resolve EOS Connect lazily (Fusion 0.1.x builds EOSRuntime only after LogIn).
            InstallPidHook();
            // Device SystemInfo hooks are installed ONLY during CreateDeviceId — not at boot.
            if (Enabled)
                StartEnsure();
        }

        public static void InstallMenu(Page root)
        {
            root.CreateBool("Spoofing PID", new Color(0.85f, 0.55f, 0.15f), Enabled, OnToggle);
            root.CreateFunction("Reset spoof PID", new Color(1f, 0.45f, 0.3f), (Action)ResetSpoofIdentity);
        }

        /// <summary>Wipe minted spoof ProductUserId and provision a fresh one (if spoof is ON).</summary>
        private static void ResetSpoofIdentity()
        {
            MelonLogger.Msg("Spoofing PID: reset — clearing minted spoof id");
            SpoofPlatformId = "";
            SpoofDeviceModel = "";
            Save();
            _ensureStarted = false;
            _cancel = false;
            if (Enabled)
            {
                StartEnsure();
                Notify("Spoof PID", "Reset — minting new id…");
            }
            else
            {
                Notify("Spoof PID", "Cleared (toggle ON to mint)");
            }
        }

        private static void OnToggle(bool on)
        {
            Enabled = on;
            Save();
            MelonLogger.Msg(on ? "Spoofing PID: ON" : "Spoofing PID: OFF");

            if (!on)
            {
                _cancel = true;
                EndFingerprintSpoof();
                return;
            }

            _cancel = false;
            if (string.IsNullOrEmpty(SpoofPlatformId))
                StartEnsure();
            else
                MelonCoroutines.Start(ApplyImmediate(notify: true));
        }

        private static void StartEnsure()
        {
            if (_ensureStarted) return;
            _ensureStarted = true;
            _cancel = false;
            MelonCoroutines.Start(EnsureRoutine());
        }

        private static IEnumerator EnsureRoutine()
        {
            EnsureConnectResolver();
            MelonLogger.Msg("Spoofing PID: waiting for Fusion EOS login (Connect via " +
                            (_connectResolverKind ?? "auto") + ")…");
            float waited = 0f;
            float nextLog = 5f;
            ConnectInterface connect = GetConnect();
            string loggedIn = GetLoggedInProductUserId();
            // Fusion 0.1.x: Connect exists only after NetworkLayer.LogIn → EOSRuntime.InitializeAsync.
            while ((connect == null || string.IsNullOrEmpty(loggedIn)) && waited < ConnectWaitSeconds)
            {
                if (_cancel || !Enabled)
                {
                    MelonLogger.Msg("Spoofing PID: wait cancelled.");
                    _ensureStarted = false;
                    yield break;
                }
                waited += Time.unscaledDeltaTime;
                if (waited >= nextLog)
                {
                    MelonLogger.Msg(
                        "Spoofing PID: waiting EOS (" + (int)waited + "s) connect=" +
                        (connect != null ? "yes" : "no") +
                        " user=" + (string.IsNullOrEmpty(loggedIn) ? "no" : Short(loggedIn)) +
                        " — open Fusion → Epic Online Services / Log In.");
                    nextLog += 5f;
                }
                yield return null;
                connect = GetConnect();
                loggedIn = GetLoggedInProductUserId();
            }

            if (connect == null || string.IsNullOrEmpty(loggedIn))
            {
                MelonLogger.Warning(
                    "Spoofing PID: EOS not ready (connect=" + (connect != null) +
                    ", user=" + Short(loggedIn) + "). Log into Fusion EOS, then toggle Spoofing PID again.");
                NotifyError("PID spoof failed", "Fusion EOS not logged in");
                _ensureStarted = false;
                yield break;
            }

            MelonLogger.Msg("Spoofing PID: EOS ready (user=" + Short(loggedIn) + ", via " +
                            (_connectResolverKind ?? "?") + ").");
            TryRememberOriginal(PlayerIDManager.LocalPlatformID);
            if (string.IsNullOrEmpty(OriginalPlatformId))
                TryRememberOriginal(loggedIn);

            if (_cancel || !Enabled)
            {
                _ensureStarted = false;
                yield break;
            }

            if (string.IsNullOrEmpty(SpoofPlatformId))
            {
                yield return ProvisionZeroAccount();
            }
            else
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
                    yield return ApplyImmediate(notify: true);
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
            if (_cancel || !Enabled)
            {
                _provisioning = false;
                yield break;
            }

            MelonLogger.Msg("Spoofing PID: provisioning zero account…");
            BeginFingerprintSpoof();

            // Skip Connect.Logout — tears down the live Fusion session (freeze) and on Fusion 0.1.x
            // trips OnAuthExpiredUnrecoverable → ForceDisconnect.
            //
            // CreateDeviceId first (new fingerprint). Only DeleteDeviceId if we need a clean slate
            // after DuplicateNotAllowed + Login still returns the original account.

            // 1) CreateDeviceId with spoofed DeviceModel — retry on UnexpectedError (Fusion 0.1.x
            //    EOS deployment rotation / transient Epic flaps).
            bool deviceOk = false;
            string model = string.IsNullOrEmpty(_fakeDeviceModel) ? "Meta Quest 3" : _fakeDeviceModel;
            Result lastCreate = Result.UnexpectedError;
            for (int attempt = 0; attempt < 4 && !deviceOk; attempt++)
            {
                if (attempt > 0)
                {
                    MelonLogger.Msg($"Spoofing PID: CreateDeviceId retry {attempt + 1}/4…");
                    GenerateFingerprint();
                    model = _fakeDeviceModel;
                    float wait = 0f;
                    while (wait < 1.2f) { wait += Time.unscaledDeltaTime; yield return null; }
                }

                bool done = false;
                lastCreate = Result.UnexpectedError;
                try
                {
                    var create = new CreateDeviceIdOptions { DeviceModel = model };
                    connect.CreateDeviceId(ref create, null, (ref CreateDeviceIdCallbackInfo info) =>
                    {
                        lastCreate = info.ResultCode;
                        deviceOk = info.ResultCode == Result.Success || info.ResultCode == Result.DuplicateNotAllowed;
                        done = true;
                    });
                }
                catch (Exception e)
                {
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId — " + e.Message);
                    break;
                }

                float t = 0f;
                while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
                if (!deviceOk)
                    MelonLogger.Warning("Spoofing PID: CreateDeviceId → " + lastCreate);
            }

            if (!deviceOk)
            {
                // Last resort: delete device credential then mint once more.
                MelonLogger.Msg("Spoofing PID: CreateDeviceId failed — DeleteDeviceId then final mint…");
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
                    }
                    float t = 0f;
                    while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
                }

                GenerateFingerprint();
                model = _fakeDeviceModel;
                {
                    bool done = false;
                    lastCreate = Result.UnexpectedError;
                    try
                    {
                        var create = new CreateDeviceIdOptions { DeviceModel = model };
                        connect.CreateDeviceId(ref create, null, (ref CreateDeviceIdCallbackInfo info) =>
                        {
                            lastCreate = info.ResultCode;
                            deviceOk = info.ResultCode == Result.Success || info.ResultCode == Result.DuplicateNotAllowed;
                            done = true;
                        });
                    }
                    catch (Exception e)
                    {
                        MelonLogger.Warning("Spoofing PID: CreateDeviceId (final) — " + e.Message);
                    }
                    float t = 0f;
                    while (!done && t < 15f) { t += Time.unscaledDeltaTime; yield return null; }
                }
            }

            if (!deviceOk)
            {
                MelonLogger.Warning("Spoofing PID: CreateDeviceId failed (" + lastCreate +
                                    "). Fusion 0.1.x rotated EOS DeploymentId — update LabFusion, then retry.");
                NotifyError("PID spoof failed", "CreateDeviceId: " + lastCreate);
                EndFingerprintSpoof();
                _provisioning = false;
                yield break;
            }

            SpoofDeviceModel = model;

            // Fingerprint spoof only needed around CreateDeviceId — release before Login/CreateUser.
            EndFingerprintSpoof();

            // 3) Login → InvalidUser → CreateUser (new ProductUserId, separate from original).
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
            SyncAuthManagerLocalUserId(created);
            MelonLogger.Msg("Spoofing PID: zero account ready (pid=" + Short(pid) +
                            ", device=" + SpoofDeviceModel + ", original=" + Short(OriginalPlatformId) + ").");

            // Drop provisioning lock FIRST so SetPlatformID hook can force the spoof, then apply now
            // (no restart needed) and show confirmation.
            _provisioning = false;
            yield return ApplyImmediate(notify: true, createdUser: created);
        }

        /// <summary>
        /// Force LocalPlatformID → spoof PID right now, re-apply a few frames (beat Fusion races),
        /// optional Fusion popup "PID spoofed".
        /// </summary>
        private static IEnumerator ApplyImmediate(bool notify, ProductUserId createdUser = null)
        {
            if (!Enabled || string.IsNullOrEmpty(SpoofPlatformId))
                yield break;

            ApplyNow();
            if (createdUser != null)
                SyncAuthManagerLocalUserId(createdUser);
            else
            {
                try
                {
                    ProductUserId u = ProductUserId.FromString(SpoofPlatformId);
                    if (u != null && u.IsValid())
                        SyncAuthManagerLocalUserId(u);
                }
                catch { }
            }

            // Re-assert a few frames in case Fusion rewrites identity after CreateUser/login.
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                if (!Enabled || _cancel) yield break;
                ApplyNow();
            }

            string live = null;
            try { live = PlayerIDManager.LocalPlatformID; } catch { }
            bool ok = string.Equals(live, SpoofPlatformId, StringComparison.Ordinal);
            MelonLogger.Msg(ok
                ? "Spoofing PID: applied immediately (pid=" + Short(SpoofPlatformId) + ")."
                : "Spoofing PID: apply mismatch live=" + Short(live) + " spoof=" + Short(SpoofPlatformId));

            if (notify)
                Notify("PID spoofed", ok ? Short(SpoofPlatformId) : "check Latest.log");
        }

        private static void Notify(string title, string message)
        {
            try
            {
                var n = new LabFusion.UI.Popups.Notification();
                n.Title = title;
                n.Message = message;
                n.Type = LabFusion.UI.Popups.NotificationType.SUCCESS;
                n.ShowPopup = true;
                n.PopupLength = 3.5f;
                LabFusion.UI.Popups.Notifier.Send(n);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: notify — " + e.Message);
            }
        }

        private static void NotifyError(string title, string message)
        {
            try
            {
                var n = new LabFusion.UI.Popups.Notification();
                n.Title = title;
                n.Message = message;
                n.Type = LabFusion.UI.Popups.NotificationType.ERROR;
                n.ShowPopup = true;
                n.PopupLength = 4f;
                LabFusion.UI.Popups.Notifier.Send(n);
            }
            catch { }
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
            InstallDeviceHooks(); // patch only for this window
            _fingerprintActive = true;
            MelonLogger.Msg("Spoofing PID: device fingerprint spoof active (" + _fakeDeviceModel + ").");
        }

        private static void EndFingerprintSpoof()
        {
            _fingerprintActive = false;
            UninstallDeviceHooks(); // remove SystemInfo patches — no ongoing overhead
        }

        private static void GenerateFingerprint()
        {
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
                string[] props = { "deviceModel", "deviceName", "deviceUniqueIdentifier", "operatingSystem" };
                string[] prefixes = { nameof(DeviceModelPrefix), nameof(DeviceNamePrefix), nameof(DeviceUidPrefix), nameof(OperatingSystemPrefix) };
                for (int i = 0; i < props.Length; i++)
                {
                    MethodInfo getter = AccessTools.PropertyGetter(t, props[i]);
                    if (getter == null) continue;
                    var hm = new HarmonyMethod(typeof(PidSpoof), prefixes[i]);
                    _harmony.Patch(getter, prefix: hm);
                    _deviceGetters[i] = getter;
                    _devicePrefixes[i] = hm;
                }
                _deviceHooksInstalled = true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: device hooks — " + e.Message);
            }
        }

        private static void UninstallDeviceHooks()
        {
            if (!_deviceHooksInstalled || _harmony == null) return;
            try
            {
                for (int i = 0; i < _deviceGetters.Length; i++)
                {
                    if (_deviceGetters[i] == null) continue;
                    _harmony.Unpatch(_deviceGetters[i], HarmonyPatchType.Prefix);
                    _deviceGetters[i] = null;
                    _devicePrefixes[i] = null;
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Spoofing PID: device unhook — " + e.Message);
            }
            _deviceHooksInstalled = false;
        }

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

        /// <summary>
        /// Avoid AccessTools.TypeByName (scans all assemblies → typeref crashes on Quest).
        /// </summary>
        private static Type FindLabFusionType(string fullName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string an = asm.GetName().Name;
                    if (an != "LabFusion" && an != "Assembly-CSharp")
                        continue;
                    Type t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                    if (t != null)
                        return t;
                }
                catch { /* bad assembly */ }
            }
            try
            {
                return Type.GetType(fullName + ", LabFusion", throwOnError: false);
            }
            catch { return null; }
        }

        private static object GetEpicNetworkLayer()
        {
            try
            {
                Type mgr = FindLabFusionType("LabFusion.Network.NetworkLayerManager");
                object layer = mgr != null
                    ? AccessTools.Property(mgr, "Layer")?.GetValue(null)
                    : null;
                if (layer == null)
                {
                    Type netInfo = FindLabFusionType("LabFusion.Network.NetworkInfo");
                    layer = netInfo != null
                        ? AccessTools.Property(netInfo, "Layer")?.GetValue(null)
                        : null;
                }
                if (layer == null)
                    return null;

                // 0.2.0 moved type to LabFusion.Network; older builds keep EpicGames.*
                Type epic = FindLabFusionType("LabFusion.Network.EpicGamesNetworkLayer")
                            ?? FindLabFusionType("LabFusion.Network.EpicGames.EpicGamesNetworkLayer");
                if (epic != null && !epic.IsInstanceOfType(layer))
                    return null;
                return layer;
            }
            catch { return null; }
        }

        /// <summary>Fusion 0.2.0: <c>EpicGamesNetworkLayer.Runtime</c> (<c>EOSRuntime</c>).</summary>
        private static object GetEosRuntime()
        {
            try
            {
                object layer = GetEpicNetworkLayer();
                if (layer == null)
                    return null;
                Type t = layer.GetType();
                return AccessTools.Field(t, "Runtime")?.GetValue(layer)
                       ?? AccessTools.Field(t, "_runtime")?.GetValue(layer)
                       ?? AccessTools.Property(t, "Runtime")?.GetValue(layer);
            }
            catch { return null; }
        }

        /// <summary>
        /// 0.2.0: <c>Runtime.Connect</c> (<see cref="EOSConnect"/> wrapper).
        /// 0.1.x: <c>Runtime.Context</c> (has Connect + LocalUserId).
        /// </summary>
        private static object GetEosConnectWrapper()
        {
            try
            {
                object runtime = GetEosRuntime();
                if (runtime == null)
                    return null;
                Type rt = runtime.GetType();

                // 0.2.0 — EOSRuntime.Connect → EOSConnect
                object connect = AccessTools.Property(rt, "Connect")?.GetValue(runtime)
                                 ?? AccessTools.Field(rt, "Connect")?.GetValue(runtime);
                if (connect != null)
                    return connect;

                // 0.1.x — EOSRuntime.Context (then .Connect on context)
                object ctx = AccessTools.Property(rt, "Context")?.GetValue(runtime)
                             ?? AccessTools.Field(rt, "Context")?.GetValue(runtime);
                return ctx;
            }
            catch { return null; }
        }

        private static object GetEosContext() => GetEosConnectWrapper();

        private static void EnsureConnectResolver()
        {
            if (_connectResolver != null)
                return;

            // Path A — Fusion ≤0.0.6: static EOSInterfaces.Connect
            try
            {
                Type iface = FindLabFusionType("LabFusion.Network.EpicGames.EOSInterfaces");
                PropertyInfo connect = iface != null ? AccessTools.Property(iface, "Connect") : null;
                if (connect != null)
                {
                    _connectResolver = () =>
                    {
                        try { return connect.GetValue(null) as ConnectInterface; }
                        catch { return null; }
                    };
                    _connectResolverKind = "EOSInterfaces.Connect";
                }
            }
            catch { /* */ }

            // Path B/C — Fusion 0.1.x Context.Connect or 0.2.0 EOSConnect.ConnectInterface
            if (_connectResolver == null)
            {
                _connectResolver = () =>
                {
                    try
                    {
                        object wrap = GetEosConnectWrapper();
                        if (wrap == null)
                            return null;

                        // Already a ConnectInterface?
                        if (wrap is ConnectInterface direct)
                            return direct;

                        Type wt = wrap.GetType();
                        // 0.2.0 EOSConnect.ConnectInterface
                        object ci = AccessTools.Field(wt, "ConnectInterface")?.GetValue(wrap)
                                    ?? AccessTools.Property(wt, "ConnectInterface")?.GetValue(wrap);
                        if (ci is ConnectInterface c0)
                            return c0;

                        // 0.1.x Context.Connect (property may already be ConnectInterface)
                        object nested = AccessTools.Property(wt, "Connect")?.GetValue(wrap)
                                        ?? AccessTools.Field(wt, "Connect")?.GetValue(wrap);
                        return nested as ConnectInterface;
                    }
                    catch { return null; }
                };
                _connectResolverKind = "EOSRuntime.Connect/ConnectInterface";
            }

            if (!_loggedResolver)
            {
                _loggedResolver = true;
                MelonLogger.Msg("Spoofing PID: EOS Connect resolver = " + _connectResolverKind);
            }
        }

        private static ConnectInterface GetConnect()
        {
            try
            {
                EnsureConnectResolver();
                return _connectResolver?.Invoke();
            }
            catch { return null; }
        }

        private static string GetLoggedInProductUserId()
        {
            try
            {
                // 0.2.0: EpicGamesNetworkLayer.LocalUserId → Runtime.Connect.LocalUserId
                object layer = GetEpicNetworkLayer();
                if (layer != null)
                {
                    object local = AccessTools.Property(layer.GetType(), "LocalUserId")?.GetValue(layer);
                    if (local is ProductUserId lp && lp.IsValid())
                        return lp.ToString();
                }

                object wrap = GetEosConnectWrapper();
                if (wrap != null)
                {
                    object local = AccessTools.Field(wrap.GetType(), "LocalUserId")?.GetValue(wrap)
                                   ?? AccessTools.Property(wrap.GetType(), "LocalUserId")?.GetValue(wrap);
                    if (local is ProductUserId puid && puid.IsValid())
                        return puid.ToString();
                }

                ConnectInterface connect = GetConnect();
                if (connect == null)
                    return null;
                if (connect.GetLoggedInUsersCount() <= 0)
                    return null;
                ProductUserId user = connect.GetLoggedInUserByIndex(0);
                if (user == null || !user.IsValid())
                    return null;
                return user.ToString();
            }
            catch { return null; }
        }

        private static void SyncAuthManagerLocalUserId(ProductUserId user)
        {
            if (user == null || !user.IsValid())
                return;
            try
            {
                // 0.2.0: EOSConnect.LocalUserId field
                object wrap = GetEosConnectWrapper();
                if (wrap != null)
                {
                    MethodInfo setLocal = AccessTools.Method(wrap.GetType(), "SetLocalUser", new[] { typeof(ProductUserId) });
                    if (setLocal != null)
                    {
                        setLocal.Invoke(wrap, new object[] { user });
                        return;
                    }
                    FieldInfo localField = AccessTools.Field(wrap.GetType(), "LocalUserId");
                    if (localField != null)
                    {
                        localField.SetValue(wrap, user);
                        return;
                    }
                    PropertyInfo local = AccessTools.Property(wrap.GetType(), "LocalUserId");
                    if (local != null && local.CanWrite)
                    {
                        local.SetValue(wrap, user);
                        return;
                    }
                }

                // Legacy ≤0.0.6: EOSAuthManager.LocalUserId on the layer
                object layer = GetEpicNetworkLayer();
                if (layer == null)
                    return;
                object auth = AccessTools.Field(layer.GetType(), "_authManager")?.GetValue(layer);
                if (auth == null)
                    return;
                PropertyInfo authLocal = AccessTools.Property(auth.GetType(), "LocalUserId");
                if (authLocal != null && authLocal.CanWrite)
                    authLocal.SetValue(auth, user);
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
