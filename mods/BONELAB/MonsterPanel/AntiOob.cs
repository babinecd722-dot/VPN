using System;
using System.Reflection;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.SceneStreaming;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Scene;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel;

/// <summary>
/// Silent shield against Fusion OOB kick/reload loops (no UI text).
///
/// Vanilla Fusion CheckFloatingPoint does:
///   Physics.autoSimulation = false  → frozen / black void feel
///   Disconnect("Left Bounds")
///   SceneStreamer.Reload()          → black load screen (host); clients already
///                                     blocked by Fusion's own Reload patch, so they
///                                     get stuck with physics OFF + mute
///   Whoops popup
///
/// That "protection half-worked" state (mute + black) is exactly physics-off without
/// a clean reload finish. We recover to a safe point and block Reload/Disconnect for
/// Left Bounds. Sim/audio restore is event-driven (OOB + reactive if sim is off) —
/// never a per-frame reflection hammer in a healthy lobby.
/// </summary>
internal static class AntiOob
{
    private const float SoftWorldLimit = NetworkTransformManager.WorldLimit;
    private const float SoftWorldLimitSq = SoftWorldLimit * SoftWorldLimit;
    private const float RecoverLimit = SoftWorldLimit * 0.9f;
    private const float RecoverLimitSq = RecoverLimit * RecoverLimit;

    private const float RecoverCooldownSec = 6f;
    private const float SceneGraceSec = 12f;
    private const float LockdownSec = 45f;
    private const int RecoverBurstLimit = 3;
    private const float RecoverBurstWindowSec = 25f;
    private const float LogCooldownSec = 5f;
    private const float PostRecoverGuardSec = 20f;
    private const float AudioGuardPollSec = 0.5f;

    private const string LeftBoundsReason = "Left Bounds";

    private static bool _installed;
    private static bool _inRecover;
    private static Vector3 _lastGoodFeet;
    private static bool _hasLastGood;

    private static float _lastRecoverAt = -999f;
    private static float _sceneGraceUntil;
    private static float _lockdownUntil;
    private static float _holdPhysicsUntil;
    private static float _lastLogAt = -999f;
    private static float _nextAudioGuardAt;

    private static PropertyInfo _audioPauseProp;
    private static bool _audioPauseResolved;

    private static readonly float[] _recentRecovers = new float[RecoverBurstLimit];
    private static int _recentRecoverCount;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        int ok = 0;
        int critical = 0;

        // Each patch is isolated — one failure must not abort Install after others
        // already applied (half-shield with Tick dead is worse than no shield).
        ok += Patch(harmony, AccessTools.Method(typeof(PlayerRepTeleportMessage), "OnHandleMessage"),
            nameof(TeleportMessagePrefix));

        ok += Patch(harmony,
            AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition), new[] { typeof(Vector3) }),
            nameof(TeleportToPositionPrefix));
        ok += Patch(harmony,
            AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition),
                new[] { typeof(Vector3), typeof(Vector3) }),
            nameof(TeleportToPositionForwardPrefix));

        ok += Patch(harmony,
            AccessTools.Method(typeof(LabFusion.Extensions.RigManagerExtensions), "TeleportToPosition",
                new[] { typeof(RigManager), typeof(Vector3), typeof(bool) }),
            nameof(RigTeleportPrefix));
        ok += Patch(harmony,
            AccessTools.Method(typeof(LabFusion.Extensions.RigManagerExtensions), "TeleportToPosition",
                new[] { typeof(RigManager), typeof(Vector3), typeof(Vector3), typeof(bool) }),
            nameof(RigTeleportForwardPrefix));

        if (Patch(harmony,
                AccessTools.Method(typeof(FusionPlayer), "CheckFloatingPoint"),
                nameof(CheckFloatingPointPrefix)) > 0)
            critical++;

        if (Patch(harmony,
                AccessTools.Method(typeof(NetworkHelper), nameof(NetworkHelper.Disconnect), new[] { typeof(string) }),
                nameof(DisconnectPrefix)) > 0)
            critical++;

        // Hosts still call SceneStreamer.Reload on OOB; clients get physics-off
        // without reload (Fusion already blocks client Reload). Block both.
        ok += Patch(harmony,
            AccessTools.Method(typeof(SceneStreamer), nameof(SceneStreamer.Reload)),
            nameof(SceneReloadPrefix));

        // NOTE: do NOT Harmony-patch Physics.autoSimulation setter.
        // It is a Unity engine property (often IL2CPP icall without a detourable stub);
        // a failed Patch() used to abort Install after critical patches already applied,
        // leaving Tick/_installed dead — worse than no shield. Tick() forces sim ON instead.

        ok += Patch(harmony,
            AccessTools.Method(typeof(Notifier), nameof(Notifier.Send), new[] { typeof(Notification) }),
            nameof(NotifierSendPrefix));

        try
        {
            MultiplayerHooking.OnMainSceneInitialized += OnMainSceneInitialized;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] MainScene hook failed: {ex.Message}");
        }

        if (critical < 2)
        {
            MelonLogger.Error(
                $"[AntiOob] Incomplete shield ({ok} optional, critical={critical}/2). OOB kick may still fire.");
            // Still mark installed if we got at least CheckFloatingPoint — Tick can help.
            if (critical >= 1)
                _installed = true;
            return;
        }

        _installed = true;
        MelonLogger.Msg(
            $"[AntiOob] Silent OOB shield active ({ok + critical} patches; reactive sim/audio recover only).");
    }

    /// <summary>
    /// Cheap idle tick: only touch physics/audio when sim was killed or during a short
    /// post-OOB guard. Never run reflection every frame in a healthy lobby.
    /// </summary>
    internal static void Tick()
    {
        if (!_installed)
            return;

        try
        {
            // Hot path: one bool read. Healthy lobby → return immediately.
            if (!Physics.autoSimulation)
            {
                Physics.autoSimulation = true;
                UnpauseAudioIfPaused();
                return;
            }

            // Post-OOB guard only — do NOT poll for the entire session / HasServer.
            if (!InActiveOobGuard)
                return;

            float now = Time.unscaledTime;
            if (now < _nextAudioGuardAt)
                return;
            _nextAudioGuardAt = now + AudioGuardPollSec;
            UnpauseAudioIfPaused();
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>True only while recovering from a real OOB event (not every lobby frame).</summary>
    private static bool InActiveOobGuard =>
        Time.unscaledTime <= _holdPhysicsUntil
        || Time.unscaledTime - _lastRecoverAt < PostRecoverGuardSec
        || InLockdown;

    /// <summary>Per-patch try/catch — never let one bad target poison the whole Install.</summary>
    private static int Patch(HarmonyLib.Harmony harmony, System.Reflection.MethodInfo method, string prefix)
    {
        if (method == null)
            return 0;
        try
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AntiOob), prefix));
            return 1;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] Skip patch {prefix} on {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
            return 0;
        }
    }

    private static void OnMainSceneInitialized()
    {
        _sceneGraceUntil = Time.unscaledTime + SceneGraceSec;
        // One-shot sense restore on scene load — do not extend OOB guard for the whole grace.
        EnsureSimulationOn();
        UnpauseAudioIfPaused();
    }

    private static bool IsFinite(Vector3 v) =>
        !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
          || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

    private static bool IsInSoftBounds(Vector3 v)
    {
        if (!IsFinite(v))
            return false;
        float sqr = v.sqrMagnitude;
        return !float.IsNaN(sqr) && sqr < SoftWorldLimitSq;
    }

    private static bool IsInRecoverBounds(Vector3 v) =>
        IsFinite(v) && v.sqrMagnitude < RecoverLimitSq;

    private static bool IsLocalRig(RigManager rigManager)
    {
        try
        {
            return RigData.HasPlayer && rigManager != null && rigManager == RigData.Refs.RigManager;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsLoading()
    {
        try
        {
            return FusionSceneManager.IsLoading();
        }
        catch
        {
            return false;
        }
    }

    private static void LogRateLimited(string message)
    {
        if (Time.unscaledTime - _lastLogAt < LogCooldownSec)
            return;
        _lastLogAt = Time.unscaledTime;
        MelonLogger.Warning(message);
    }

    private static bool InLockdown => Time.unscaledTime < _lockdownUntil;
    private static bool InSceneGrace => Time.unscaledTime < _sceneGraceUntil;
    private static bool InRecoverCooldown => Time.unscaledTime - _lastRecoverAt < RecoverCooldownSec;
    private static bool InPostRecoverGuard => Time.unscaledTime - _lastRecoverAt < PostRecoverGuardSec;

    private static void NoteRecover()
    {
        _lastRecoverAt = Time.unscaledTime;
        _holdPhysicsUntil = Time.unscaledTime + Math.Max(RecoverCooldownSec, 8f);

        if (_recentRecoverCount < _recentRecovers.Length)
            _recentRecovers[_recentRecoverCount++] = _lastRecoverAt;
        else
        {
            Array.Copy(_recentRecovers, 1, _recentRecovers, 0, _recentRecovers.Length - 1);
            _recentRecovers[_recentRecovers.Length - 1] = _lastRecoverAt;
        }

        int inWindow = 0;
        for (int i = 0; i < _recentRecoverCount; i++)
        {
            if (_lastRecoverAt - _recentRecovers[i] <= RecoverBurstWindowSec)
                inWindow++;
        }

        if (inWindow >= RecoverBurstLimit)
        {
            _lockdownUntil = Time.unscaledTime + LockdownSec;
            LogRateLimited(
                $"[AntiOob] Lockdown {LockdownSec:0}s after {inWindow} recovers — blocking OOB teleports, no reload.");
        }
    }

    /// <summary>Called only on real OOB recover paths — arms a short Tick guard.</summary>
    private static void RestoreSenses()
    {
        EnsureSimulationOn();
        UnpauseAudioIfPaused();
        _holdPhysicsUntil = Time.unscaledTime + 8f;
        _nextAudioGuardAt = 0f;
    }

    private static void EnsureSimulationOn()
    {
        try
        {
            if (!Physics.autoSimulation)
                Physics.autoSimulation = true;
        }
        catch
        {
            // ignored
        }
    }

    private static void ResolveAudioPauseProp()
    {
        if (_audioPauseResolved)
            return;
        _audioPauseResolved = true;
        try
        {
            // Il2Cpp AudioListener lives in AudioModule — resolve once (no hard ref).
            var t = AccessTools.TypeByName("UnityEngine.AudioListener");
            _audioPauseProp = t != null ? AccessTools.Property(t, "pause") : null;
        }
        catch
        {
            _audioPauseProp = null;
        }
    }

    /// <summary>Write AudioListener.pause=false only when it is actually paused.</summary>
    private static void UnpauseAudioIfPaused()
    {
        try
        {
            ResolveAudioPauseProp();
            if (_audioPauseProp == null || !_audioPauseProp.CanWrite)
                return;

            if (_audioPauseProp.CanRead)
            {
                object cur = _audioPauseProp.GetValue(null);
                if (cur is bool paused && !paused)
                    return;
            }

            _audioPauseProp.SetValue(null, false);
        }
        catch
        {
            // ignored
        }
    }

    private static void ResetLocalVelocity()
    {
        try
        {
            if (!RigData.HasPlayer)
                return;
            var rm = RigData.Refs.RigManager;
            if (rm?.physicsRig?.selfRbs == null)
                return;
            foreach (var rb in rm.physicsRig.selfRbs)
            {
                if (rb == null)
                    continue;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        catch
        {
            // best-effort
        }
    }

    // ---- Network teleport (only reject true OOB — never blanket-block Bring) ----

    private static bool TeleportMessagePrefix(ReceivedMessage received)
    {
        try
        {
            var data = received.ReadData<PlayerRepTeleportData>();
            var position = data.Position;

            if (!IsInSoftBounds(position))
            {
                LogRateLimited(
                    $"[AntiOob] Blocked network teleport to {position} (NaN/Inf or past world limit).");
                return false;
            }

            // Lockdown only drops OOB spam; sane Bring/admin teleports still apply.
            return true;
        }
        catch (Exception ex)
        {
            LogRateLimited($"[AntiOob] TeleportMessagePrefix: {ex.Message}");
            return false;
        }
    }

    private static bool TeleportToPositionPrefix(ref Vector3 position) =>
        SanitizeTeleport(ref position);

    private static bool TeleportToPositionForwardPrefix(ref Vector3 position, Vector3 forward) =>
        SanitizeTeleport(ref position);

    private static bool RigTeleportPrefix(RigManager rigManager, ref Vector3 position, bool resetVelocity)
    {
        if (!IsLocalRig(rigManager))
            return true;
        return SanitizeTeleport(ref position);
    }

    private static bool RigTeleportForwardPrefix(
        RigManager rigManager, ref Vector3 position, Vector3 forward, bool resetVelocity)
    {
        if (!IsLocalRig(rigManager))
            return true;
        return SanitizeTeleport(ref position);
    }

    private static bool SanitizeTeleport(ref Vector3 position)
    {
        if (_inRecover)
            return true;

        try
        {
            if (!IsFinite(position))
            {
                LogRateLimited("[AntiOob] Blocked TeleportToPosition with NaN/Inf.");
                return false;
            }

            if (!IsInSoftBounds(position))
            {
                float mag = position.magnitude;
                if (mag < 1e-3f || float.IsNaN(mag))
                {
                    LogRateLimited("[AntiOob] Blocked TeleportToPosition with unusable magnitude.");
                    return false;
                }

                position = position * (RecoverLimit / mag);
                LogRateLimited($"[AntiOob] Clamped TeleportToPosition → {position}");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // ---- Core: replace Fusion OOB kick ----

    private static bool CheckFloatingPointPrefix()
    {
        try
        {
            // Fusion calls this often — never RestoreSenses() on the happy path
            // (that used to re-arm the audio guard every tick → 1 FPS).
            if (IsLoading() || !RigData.HasPlayer)
            {
                EnsureSimulationOn();
                return false;
            }

            var feet = RigData.Refs.RigManager.physicsRig.feet.transform.position;

            if (IsInSoftBounds(feet))
            {
                _lastGoodFeet = feet;
                _hasLastGood = true;
                // Reactive only: if Fusion already froze sim, flip it back. No audio spam.
                EnsureSimulationOn();
                return false;
            }

            if (InSceneGrace || InRecoverCooldown || InLockdown || _inRecover)
            {
                RestoreSenses();
                return false;
            }

            NoteRecover();
            Vector3 safe = ResolveSafePoint();
            LogRateLimited(
                $"[AntiOob] Recovered OOB/NaN feet={feet} → {safe} (no disconnect, no reload, senses on).");

            _inRecover = true;
            try
            {
                ResetLocalVelocity();
                LocalPlayer.TeleportToPosition(safe);
                ResetLocalVelocity();
            }
            finally
            {
                _inRecover = false;
            }

            RestoreSenses();
            return false;
        }
        catch (Exception ex)
        {
            RestoreSenses();
            LogRateLimited($"[AntiOob] CheckFloatingPointPrefix: {ex.Message}");
            return false;
        }
    }

    private static bool DisconnectPrefix(string reason)
    {
        if (!string.Equals(reason, LeftBoundsReason, StringComparison.Ordinal))
            return true;

        LogRateLimited("[AntiOob] Blocked Disconnect(\"Left Bounds\").");
        RestoreSenses();

        if (!_inRecover && RigData.HasPlayer && !IsLoading() && !InRecoverCooldown)
        {
            NoteRecover();
            _inRecover = true;
            try
            {
                ResetLocalVelocity();
                LocalPlayer.TeleportToPosition(ResolveSafePoint());
                ResetLocalVelocity();
            }
            catch
            {
                // best-effort
            }
            finally
            {
                _inRecover = false;
            }
        }

        RestoreSenses();
        return false;
    }

    /// <summary>
    /// Block Reload while we are handling / recently handled OOB. Prevents the
    /// host black-screen reload and the client "physics off, reload skipped" hang.
    /// </summary>
    private static bool SceneReloadPrefix()
    {
        if (!_installed)
            return true;

        try
        {
            if (!NetworkInfo.HasServer)
                return true;

            if (_inRecover || InPostRecoverGuard || InLockdown || InRecoverCooldown)
            {
                LogRateLimited("[AntiOob] Blocked SceneStreamer.Reload during OOB guard.");
                RestoreSenses();
                return false;
            }
        }
        catch
        {
            // allow reload if we can't evaluate
        }

        return true;
    }

    private static bool NotifierSendPrefix(Notification notification)
    {
        try
        {
            if (notification == null)
                return true;

            string title = notification.Title.Text ?? string.Empty;
            string message = notification.Message.Text ?? string.Empty;

            bool whoops = title.IndexOf("Whoops", StringComparison.OrdinalIgnoreCase) >= 0;
            bool farBounds = message.IndexOf("out of bounds", StringComparison.OrdinalIgnoreCase) >= 0
                             || message.IndexOf("far out of bounds", StringComparison.OrdinalIgnoreCase) >= 0;

            if (whoops && farBounds)
            {
                LogRateLimited("[AntiOob] Suppressed Fusion Whoops / far-out-of-bounds popup.");
                RestoreSenses();
                return false;
            }
        }
        catch
        {
            // if reflection/text fails, allow notification
        }

        return true;
    }

    private static Vector3 ResolveSafePoint()
    {
        try
        {
            if (RigData.HasPlayer)
            {
                var cp = RigData.Refs.RigManager.checkpointPosition;
                if (IsInRecoverBounds(cp))
                    return cp;
            }
        }
        catch
        {
            // fall through
        }

        if (_hasLastGood && IsInRecoverBounds(_lastGoodFeet))
            return _lastGoodFeet;

        return new Vector3(0f, 2f, 0f);
    }
}
