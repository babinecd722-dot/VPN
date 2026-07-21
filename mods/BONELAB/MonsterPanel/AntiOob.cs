using System;
using HarmonyLib;
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
/// Attack surface (LabFusion / Checkerb0ard):
///   PlayerRepTeleportMessage — any peer can force a local TeleportToPosition(Vector3)
///   with no auth and no bounds check before apply.
///   FusionPlayer.CheckFloatingPoint then Disconnect("Left Bounds") + SceneStreamer.Reload
///   + popup when feet leave NetworkTransformManager soft world (radius 50000 / NaN).
///
/// Defense:
///   1) Drop bad PlayerRepTeleport payloads before teleport.
///   2) Sanitize LocalPlayer.TeleportToPosition overloads.
///   3) Replace CheckFloatingPoint disconnect/reload with silent checkpoint recover.
/// </summary>
internal static class AntiOob
{
    // Mirror NetworkTransformManager.WorldLimit with a small inner margin for recover target.
    private const float SoftWorldLimit = NetworkTransformManager.WorldLimit;
    private const float SoftWorldLimitSq = SoftWorldLimit * SoftWorldLimit;
    private const float RecoverLimit = SoftWorldLimit * 0.9f;
    private const float RecoverLimitSq = RecoverLimit * RecoverLimit;

    private static bool _installed;
    private static Vector3 _lastGoodFeet;
    private static bool _hasLastGood;
    private static float _lastRecoverAt;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        try
        {
            var onHandle = AccessTools.Method(typeof(PlayerRepTeleportMessage), "OnHandleMessage");
            if (onHandle != null)
            {
                harmony.Patch(onHandle,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportMessagePrefix)));
            }

            var teleport1 = AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition),
                new[] { typeof(Vector3) });
            if (teleport1 != null)
            {
                harmony.Patch(teleport1,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportToPositionPrefix)));
            }

            var teleport2 = AccessTools.Method(typeof(LocalPlayer), nameof(LocalPlayer.TeleportToPosition),
                new[] { typeof(Vector3), typeof(Vector3) });
            if (teleport2 != null)
            {
                harmony.Patch(teleport2,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(TeleportToPositionForwardPrefix)));
            }

            // private static void CheckFloatingPoint()
            var checkFp = AccessTools.Method(typeof(LabFusion.Utilities.FusionPlayer), "CheckFloatingPoint");
            if (checkFp != null)
            {
                harmony.Patch(checkFp,
                    prefix: new HarmonyMethod(typeof(AntiOob), nameof(CheckFloatingPointPrefix)));
            }

            _installed = true;
            MelonLogger.Msg("[AntiOob] Silent OOB shield active (teleport + floating-point recover).");
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

    private static bool TeleportToPositionPrefix(ref Vector3 position)
    {
        return SanitizeTeleport(ref position);
    }

    private static bool TeleportToPositionForwardPrefix(ref Vector3 position, Vector3 forward)
    {
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
                // Pull onto the soft sphere instead of applying the throw.
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
