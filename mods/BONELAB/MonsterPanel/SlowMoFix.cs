using System;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using LabFusion.Network;
using LabFusion.Patching;
using LabFusion.Player;
using LabFusion.Preferences;
using LabFusion.Preferences.Server;
using LabFusion.Senders;
using LabFusion.Utilities;
using MelonLoader;

namespace MonsterPanel;

/// <summary>
/// Silent fix so the stock Fusion Slow Mo button does real timescale slow-mo.
/// No BoneMenu / UI mentions.
///
/// Why vanilla often "doesn't Slow Mo":
///   Default lobby mode is LOW_GRAVITY — button never touches Time.timeScale.
///   DISABLED turns the button off. HOST_ONLY blocks the button for non-hosts.
///
/// Non-host bypass (primary):
///   For DISABLED / LOW_GRAVITY / HOST_ONLY, arm TimeManagerPatches.IgnorePatches
///   so Fusion cannot block or replace the stock TimeManager path — local vanilla
///   timescale runs even when you are not host. Remap those modes to CLIENT_SIDE
///   so FixedUpdate low-gravity forces also stay off.
///
/// Host (bonus): upgrade DISABLED / LOW_GRAVITY → EVERYONE so the lobby syncs.
/// </summary>
internal static class SlowMoFix
{
    private static bool _installed;
    private static float _nextHostEnsureAt;
    private static bool _loggedHostUpgrade;
    private static bool _armedIgnore;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        int ok = 0;
        try
        {
            ok += Patch(harmony,
                AccessTools.PropertyGetter(typeof(CommonPreferences), nameof(CommonPreferences.SlowMoMode)),
                nameof(SlowMoModePrefix));

            ok += Patch(harmony,
                AccessTools.PropertyGetter(typeof(LocalControls), nameof(LocalControls.SlowMoEnabled)),
                nameof(SlowMoEnabledPrefix));

            // Run BEFORE Fusion's TimeManagerPatches so IgnorePatches is already set
            // when their HOST_ONLY / LOW_GRAVITY logic would kill the stock button.
            ok += PatchPriority(harmony,
                AccessTools.Method(typeof(TimeManager), nameof(TimeManager.DECREASE_TIMESCALE)),
                nameof(DecreaseArmPrefix), nameof(ClearIgnorePostfix), Priority.First);

            ok += PatchPriority(harmony,
                AccessTools.Method(typeof(TimeManager), nameof(TimeManager.TOGGLE_TIMESCALE)),
                nameof(ToggleArmPrefix), nameof(ClearIgnorePostfix), Priority.First);

            try
            {
                MultiplayerHooking.OnMainSceneInitialized += OnMainSceneInitialized;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[SlowMo] MainScene hook failed: {ex.Message}");
            }

            try
            {
                LobbyInfoManager.OnLobbyInfoChanged += OnLobbyInfoChanged;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[SlowMo] LobbyInfo hook failed: {ex.Message}");
            }

            if (ok < 4)
            {
                MelonLogger.Warning($"[SlowMo] Incomplete ({ok}/4 patches) — non-host bypass may fail.");
                return;
            }

            _installed = true;
            EnsureHostMode(force: true);
            MelonLogger.Msg($"[SlowMo] Stock Slow Mo unlocked ({ok} patches, non-host bypass).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[SlowMo] Install failed: {ex.Message}");
        }
    }

    /// <summary>Call from Melon OnUpdate — host lobby upgrade + keep button armed.</summary>
    internal static void Tick()
    {
        if (!_installed)
            return;

        try
        {
            if (!NetworkInfo.HasServer)
                return;

            // Stock input reads TimeManager.slowMoEnabled; keep it on even if a
            // gamemode flipped LocalControls.DisableSlowMo or lobby was DISABLED.
            TimeManager.slowMoEnabled = true;

            float now = UnityEngine.Time.unscaledTime;
            if (now >= _nextHostEnsureAt)
            {
                _nextHostEnsureAt = now + 2f;
                EnsureHostMode(force: false);
            }
        }
        catch
        {
            // ignored
        }
    }

    private static int Patch(HarmonyLib.Harmony harmony, System.Reflection.MethodInfo method, string prefix)
    {
        if (method == null)
            return 0;
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(SlowMoFix), prefix));
        return 1;
    }

    private static int PatchPriority(
        HarmonyLib.Harmony harmony,
        System.Reflection.MethodInfo method,
        string prefix,
        string postfix,
        int priority)
    {
        if (method == null)
            return 0;

        var pre = new HarmonyMethod(typeof(SlowMoFix), prefix) { priority = priority };
        var post = new HarmonyMethod(typeof(SlowMoFix), postfix) { priority = priority };
        harmony.Patch(method, prefix: pre, postfix: post);
        return 1;
    }

    private static void OnMainSceneInitialized()
    {
        EnsureHostMode(force: true);
        try { TimeManager.slowMoEnabled = true; } catch { /* ignored */ }
    }

    private static void OnLobbyInfoChanged()
    {
        EnsureHostMode(force: true);
        try { TimeManager.slowMoEnabled = true; } catch { /* ignored */ }
    }

    /// <summary>
    /// Modes where Fusion blocks or fakes Slow Mo for non-hosts / everyone.
    /// EVERYONE / CLIENT_SIDE already allow a real timescale path — leave them.
    /// </summary>
    private static bool NeedsClientBypass(TimeScaleMode mode) =>
        mode == TimeScaleMode.DISABLED
        || mode == TimeScaleMode.LOW_GRAVITY
        || mode == TimeScaleMode.HOST_ONLY;

    /// <summary>
    /// Non-host: force Fusion's TimeManagerPatches to stand down so vanilla
    /// TimeManager.DECREASE/TOGGLE run locally (works without host).
    /// </summary>
    private static void ArmClientIgnoreIfNeeded()
    {
        _armedIgnore = false;
        try
        {
            if (!NetworkInfo.HasServer || NetworkInfo.IsHost)
                return;

            // Read raw lobby mode (not CommonPreferences — that is remapped by us).
            if (!NeedsClientBypass(LobbyInfoManager.LobbyInfo.SlowMoMode))
                return;

            TimeManagerPatches.IgnorePatches = true;
            _armedIgnore = true;
        }
        catch
        {
            // ignored
        }
    }

    private static void DecreaseArmPrefix() => ArmClientIgnoreIfNeeded();

    private static void ToggleArmPrefix() => ArmClientIgnoreIfNeeded();

    private static void ClearIgnorePostfix()
    {
        if (!_armedIgnore)
            return;

        _armedIgnore = false;
        try { TimeManagerPatches.IgnorePatches = false; }
        catch { /* ignored */ }
    }

    /// <summary>
    /// Host-only bonus: persist EVERYONE so LobbyInfo broadcasts real timescale
    /// mode and other clients accept SlowMoButton messages.
    /// </summary>
    private static void EnsureHostMode(bool force)
    {
        try
        {
            if (!NetworkInfo.HasServer || !NetworkInfo.IsHost)
                return;

            var pref = SavedServerSettings.SlowMoMode;
            if (pref == null)
                return;

            var current = pref.Value;
            if (current != TimeScaleMode.DISABLED && current != TimeScaleMode.LOW_GRAVITY)
                return;

            pref.Value = TimeScaleMode.EVERYONE;

            if (!_loggedHostUpgrade)
            {
                _loggedHostUpgrade = true;
                MelonLogger.Msg("[SlowMo] Host Time Scale Mode → EVERYONE (stock button sync).");
            }
        }
        catch (Exception ex)
        {
            if (force)
                MelonLogger.Warning($"[SlowMo] Host upgrade failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Remap dead / host-gated modes so Fusion code paths see a real timescale mode.
    /// Non-host always gets CLIENT_SIDE for bypass modes (local vanilla, no host needed).
    /// </summary>
    private static bool SlowMoModePrefix(ref TimeScaleMode __result)
    {
        try
        {
            if (!NetworkInfo.HasServer)
                return true;

            var mode = LobbyInfoManager.LobbyInfo.SlowMoMode;
            if (!NeedsClientBypass(mode))
                return true;

            if (NetworkInfo.IsHost)
            {
                // Until SavedServerSettings upgrade lands in LobbyInfo.
                __result = TimeScaleMode.EVERYONE;
            }
            else
            {
                // Non-host bypass: local timescale, no host permission / relay.
                __result = TimeScaleMode.CLIENT_SIDE;
            }

            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Stock button is gated by SlowMoEnabled → TimeManager.slowMoEnabled.</summary>
    private static bool SlowMoEnabledPrefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
