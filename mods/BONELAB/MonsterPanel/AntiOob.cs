using System;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Scene;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel;

/// <summary>
/// Silent shield against Fusion network OOB abuse (no UI text).
///
/// LabFusion audit (Checkerb0ard): the ONLY path that Disconnect("Left Bounds") +
/// SceneStreamer.Reload + "Whoops" popup is FusionPlayer.CheckFloatingPoint.
/// The network vector that teleports the LOCAL client with no auth/bounds is
/// PlayerRepTeleportMessage (tag 69). Pose updates refuse LocalSmallID.
/// PDController already zeros forces for OOB remote targets.
/// Truncated packets are try/caught in NativeMessageHandler.ReadMessage.
///
/// Defense layers:
///   1) Drop bad PlayerRepTeleport payloads.
///   2) Sanitize LocalPlayer.TeleportToPosition overloads.
///   3) Sanitize RigManagerExtensions.TeleportToPosition when target is local.
///   4) Replace CheckFloatingPoint (no disconnect / reload / popup).
///   5) Belt: block NetworkHelper.Disconnect("Left Bounds") if (4) ever misses.
/// </summary>
internal static class AntiOob
{
    // Mirror NetworkTransformManager.WorldLimit with a small inner margin for recover target.
    private const float SoftWorldLimit = NetworkTransformManager.WorldLimit;
    private const float SoftWorldLimitSq = SoftWorldLimit * SoftWorldLimit;
    private const float RecoverLimit = SoftWorldLimit * 0.9f;
    private const float RecoverLimitSq = RecoverLimit * RecoverLimit;
    private const string LeftBoundsReason = "Left Bounds";

    private static bool _installed;
    private static Vector3 _lastGoodFeet;
    private static bool _hasLastGood;
    private static float _lastRecoverAt;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        int ok = 0;
        int critical = 0;

        try
        {
            var onHandle = AccessTools.Method(typeof(PlayerRepTeleportMessage), "OnHandleMessage");
            if (onHandle != null)
            {
                harmony.Patch(onHandle,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportMessagePrefix)));
                ok++;
            }

            var teleport1 = AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition),
                new[] { typeof(Vector3) });
            if (teleport1 != null)
            {
                harmony.Patch(teleport1,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportToPositionPrefix)));
                ok++;
            }

            var teleport2 = AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition),
                new[] { typeof(Vector3), typeof(Vector3) });
            if (teleport2 != null)
            {
                harmony.Patch(teleport2,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportToPositionForwardPrefix)));
                ok++;
            }

            // Extension teleports used by NetworkPlayer reps and LocalPlayer internals.
            var rigTp1 = AccessTools.Method(typeof(LabFusion.Extensions.RigManagerExtensions),
                "TeleportToPosition", new[] { typeof(RigManager), typeof(Vector3), typeof(bool) });
            if (rigTp1 != null)
            {
                harmony.Patch(rigTp1,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(RigTeleportPrefix)));
                ok++;
            }

            var rigTp2 = AccessTools.Method(typeof(LabFusion.Extensions.RigManagerExtensions),
                "TeleportToPosition",
                new[] { typeof(RigManager), typeof(Vector3), typeof(Vector3), typeof(bool) });
            if (rigTp2 != null)
            {
                harmony.Patch(rigTp2,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(RigTeleportForwardPrefix)));
                ok++;
            }

            // private static void CheckFloatingPoint() — the only OOB kick path in Fusion.
            var checkFp = AccessTools.Method(typeof(LabFusion.Utilities.FusionPlayer), "CheckFloatingPoint");
            if (checkFp != null)
            {
                harmony.Patch(checkFp,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(CheckFloatingPointPrefix)));
                ok++;
                critical++;
            }

            var disconnect = AccessTools.Method(typeof(NetworkHelper), nameof(NetworkHelper.Disconnect),
                new[] { typeof(string) });
            if (disconnect != null)
            {
                harmony.Patch(disconnect,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(DisconnectPrefix)));
                ok++;
                critical++;
            }

            if (critical < 2)
            {
                MelonLogger.Error(
                    $"[AntiOob] Incomplete shield ({ok} patches, critical={critical}/2). OOB kick may still fire.");
                return;
            }

            _installed = true;
            MelonLogger.Msg($"[AntiOob] Silent OOB shield active ({ok} patches).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] Install failed: {ex.Message}");
        }
    }

    private static bool IsFinite(Vector3 v) =>
        !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
          || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

    /// <summary>Same rule as NetworkTransformManager.IsInBounds (sphere + NaN).</summary>
    private static bool IsInSoftBounds(Vector3 v)
    {
        if (!IsFinite(v))
            return false;

        float sqr = v.sqrMagnitude;
        if (float.IsNaN(sqr))
            return false;

        return sqr < SoftWorldLimitSq;
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

    // Prefix PlayerRepTeleportMessage.OnHandleMessage — drop before LocalPlayer.TeleportToPosition.
    private static bool TeleportMessagePrefix(ReceivedMessage received)
    {
        try
        {
            var data = received.ReadData<PlayerRepTeleportData>();
            var position = data.Position;

            if (!IsInSoftBounds(position))
            {
                MelonLogger.Warning(
                    $"[AntiOob] Blocked network teleport to {position} (NaN/Inf or past world limit).");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] TeleportMessagePrefix: {ex.Message}");
            return false;
        }
    }

    private static bool TeleportToPositionPrefix(ref Vector3 position) =>
        SanitizeTeleport(ref position);

    private static bool TeleportToPositionForwardPrefix(ref Vector3 position, Vector3 forward) =>
        SanitizeTeleport(ref position);

    private static bool RigTeleportPrefix(RigManager rigManager, ref Vector3 position, bool resetVelocity)
    {
        // Remote NetworkPlayer reps may legitimately snap; only gate the local body.
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
        try
        {
            if (!IsFinite(position))
            {
                MelonLogger.Warning("[AntiOob] Blocked TeleportToPosition with NaN/Inf.");
                return false;
            }

            if (!IsInSoftBounds(position))
            {
                float mag = position.magnitude;
                if (mag < 1e-3f || float.IsNaN(mag))
                {
                    MelonLogger.Warning("[AntiOob] Blocked TeleportToPosition with unusable magnitude.");
                    return false;
                }

                position = position * (RecoverLimit / mag);
                MelonLogger.Warning($"[AntiOob] Clamped TeleportToPosition into soft world → {position}");
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Full replacement for FusionPlayer.CheckFloatingPoint — never disconnect / reload / popup.
    private static bool CheckFloatingPointPrefix()
    {
        try
        {
            if (!RigData.HasPlayer)
                return false;

            var feet = RigData.Refs.RigManager.physicsRig.feet.transform.position;

            if (IsInSoftBounds(feet))
            {
                _lastGoodFeet = feet;
                _hasLastGood = true;
                return false;
            }

            if (Time.unscaledTime - _lastRecoverAt < 1.5f)
                return false;
            _lastRecoverAt = Time.unscaledTime;

            // Keep physics simulating (Fusion would set autoSimulation=false before reload).
            Physics.autoSimulation = true;

            Vector3 safe = ResolveSafePoint();
            MelonLogger.Warning(
                $"[AntiOob] Recovered from OOB/NaN feet={feet} → {safe} (no disconnect).");

            LocalPlayer.TeleportToPosition(safe);
            return false;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiOob] CheckFloatingPointPrefix: {ex.Message}");
            return false;
        }
    }

    // Last resort: never leave the lobby solely because Fusion thinks we Left Bounds.
    private static bool DisconnectPrefix(string reason)
    {
        if (string.Equals(reason, LeftBoundsReason, StringComparison.Ordinal))
        {
            MelonLogger.Warning("[AntiOob] Blocked Disconnect(\"Left Bounds\").");
            Physics.autoSimulation = true;
            try
            {
                if (RigData.HasPlayer)
                    LocalPlayer.TeleportToPosition(ResolveSafePoint());
            }
            catch
            {
                // best-effort
            }
            return false;
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
