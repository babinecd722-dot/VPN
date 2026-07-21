using System;
using HarmonyLib;
using Il2CppSLZ.Marrow;
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
/// Fusion text the user sees as "a lot of bounds" is:
///   Title: "Whoops! Sorry about that!"
///   Message: "The scene was reloaded due to being sent far out of bounds."
/// Only emitted from FusionPlayer.CheckFloatingPoint together with
/// Disconnect("Left Bounds") and SceneStreamer.Reload — that triad is what
/// cascades into reload→spawn→OOB→reload (crash-crash-crash before load finishes).
///
/// Defense:
///   Replace CheckFloatingPoint (never reload / disconnect / Whoops).
///   Block leftover Disconnect("Left Bounds") and suppress the Whoops popup.
///   Sanitize network + local teleports.
///   Cascade control: skip while loading, scene grace, recover cooldown, lockdown.
/// </summary>
internal static class AntiOob
{
    private const float SoftWorldLimit = NetworkTransformManager.WorldLimit;
    private const float SoftWorldLimitSq = SoftWorldLimit * SoftWorldLimit;
    private const float RecoverLimit = SoftWorldLimit * 0.9f;
    private const float RecoverLimitSq = RecoverLimit * RecoverLimit;

    // Cascade / thrash control — professional calm after first hit.
    private const float RecoverCooldownSec = 8f;
    private const float SceneGraceSec = 12f;
    private const float LockdownSec = 45f;
    private const int RecoverBurstLimit = 3;
    private const float RecoverBurstWindowSec = 25f;
    private const float LogCooldownSec = 5f;

    private const string LeftBoundsReason = "Left Bounds";
    private const string WhoopsTitle = "Whoops! Sorry about that!";

    private static bool _installed;
    private static bool _inRecover;
    private static Vector3 _lastGoodFeet;
    private static bool _hasLastGood;

    private static float _lastRecoverAt = -999f;
    private static float _sceneGraceUntil;
    private static float _lockdownUntil;
    private static float _holdPhysicsUntil;
    private static float _lastLogAt = -999f;

    private static readonly float[] _recentRecovers = new float[RecoverBurstLimit];
    private static int _recentRecoverCount;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        int ok = 0;
        int critical = 0;

        try
        {
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

            // Suppress the "far out of bounds" / Whoops popup if Fusion ever emits it.
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
                    $"[AntiOob] Incomplete shield ({ok} patches, critical={critical}/2). OOB kick may still fire.");
                return;
            }

            _installed = true;
            MelonLogger.Msg($"[AntiOob] Silent OOB shield active ({ok + critical} patches, cascade-safe).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] Install failed: {ex.Message}");
        }
    }

    /// <summary>Keep physics alive after an OOB event; call from Melon OnUpdate.</summary>
    internal static void Tick()
    {
        if (!_installed)
            return;

        try
        {
            if (Time.unscaledTime <= _holdPhysicsUntil && !Physics.autoSimulation)
                Physics.autoSimulation = true;
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
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(AntiOob), prefix));
        return 1;
    }

    private static void OnMainSceneInitialized()
    {
        // After each load: give spawn a quiet window so we never thrash mid-stream.
        _sceneGraceUntil = Time.unscaledTime + SceneGraceSec;
        Physics.autoSimulation = true;
        _holdPhysicsUntil = _sceneGraceUntil;
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

    private static void NoteRecover()
    {
        _lastRecoverAt = Time.unscaledTime;
        _holdPhysicsUntil = Time.unscaledTime + RecoverCooldownSec;

        // Ring of recent recovers → lockdown if burst (stops crash-crash-crash).
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

    // ---- Network teleport ----

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

            // During lockdown, reject ALL network teleports — stops OOB thrash cascades.
            if (InLockdown)
            {
                LogRateLimited("[AntiOob] Dropped network teleport during lockdown.");
                return false;
            }

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
        // Our own recover teleports are already validated.
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

    // ---- Core: replace Fusion OOB kick (never Reload / Disconnect / Whoops) ----

    private static bool CheckFloatingPointPrefix()
    {
        try
        {
            // Always keep simulation on — Fusion flips this off before reload and that
            // stacks with reload loops into "can't finish loading".
            Physics.autoSimulation = true;

            if (IsLoading() || !RigData.HasPlayer)
                return false; // skip original always

            var feet = RigData.Refs.RigManager.physicsRig.feet.transform.position;

            if (IsInSoftBounds(feet))
            {
                _lastGoodFeet = feet;
                _hasLastGood = true;
                return false;
            }

            // Mid-load / grace / cooldown / lockdown: hold steady, do not thrash teleports.
            if (InSceneGrace || InRecoverCooldown || InLockdown || _inRecover)
            {
                _holdPhysicsUntil = Time.unscaledTime + 2f;
                return false;
            }

            NoteRecover();

            Vector3 safe = ResolveSafePoint();
            LogRateLimited(
                $"[AntiOob] Recovered OOB/NaN feet={feet} → {safe} (no disconnect, no reload).");

            _inRecover = true;
            try
            {
                LocalPlayer.TeleportToPosition(safe);
            }
            finally
            {
                _inRecover = false;
            }

            return false;
        }
        catch (Exception ex)
        {
            Physics.autoSimulation = true;
            LogRateLimited($"[AntiOob] CheckFloatingPointPrefix: {ex.Message}");
            return false;
        }
    }

    private static bool DisconnectPrefix(string reason)
    {
        if (!string.Equals(reason, LeftBoundsReason, StringComparison.Ordinal))
            return true;

        LogRateLimited("[AntiOob] Blocked Disconnect(\"Left Bounds\").");
        Physics.autoSimulation = true;
        _holdPhysicsUntil = Time.unscaledTime + RecoverCooldownSec;

        if (!_inRecover && RigData.HasPlayer && !IsLoading() && !InRecoverCooldown)
        {
            NoteRecover();
            _inRecover = true;
            try
            {
                LocalPlayer.TeleportToPosition(ResolveSafePoint());
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

        return false;
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
