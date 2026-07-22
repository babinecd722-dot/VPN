using System;
using HarmonyLib;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.Representation;
using LabFusion.Senders;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel;

/// <summary>
/// Make Bring-to-me work as host <b>and</b> client.
///
/// Why vanilla RelayNative(PlayerRepTeleport, ToTarget) fails for clients:
///   PlayerRepTeleportMessage.ExpectedReceiver = ClientsOnly.
///   Client → server ToTarget hits CheckExpectedConditions → MessageExpectedClientException
///   → host never forwards the teleport.
///
/// Fix when we are host:
///   - Allow PlayerRepTeleport ToTarget/ToTargets through CheckExpectedConditions
///     so client Bring requests are relayed.
///   - Honor PermissionCommand TELEPORT_TO_ME / TELEPORT_TO_THEM without lobby
///     Teleportation permission gate (MonsterPanel host = always allow).
///
/// Fix when we Bring:
///   - Host: PlayerSender.SendPlayerTeleport (direct).
///   - Client: RelayNative ToTarget (works if host has this patch) + PermissionCommand
///     fallback (works on vanilla hosts that grant Teleportation).
/// </summary>
internal static class TeleportBring
{
    private static bool _installed;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        int ok = 0;
        try
        {
            ok += Patch(harmony,
                AccessTools.Method(typeof(MessageHandler), nameof(MessageHandler.CheckExpectedConditions)),
                nameof(CheckExpectedPrefix));

            ok += Patch(harmony,
                AccessTools.Method(typeof(PermissionCommandRequestMessage), "OnHandleMessage"),
                nameof(PermissionTeleportPrefix));

            if (ok < 2)
            {
                MelonLogger.Warning($"[TeleportBring] Incomplete ({ok}/2) — client Bring may still fail.");
                return;
            }

            _installed = true;
            MelonLogger.Msg($"[TeleportBring] Host relay unlock active ({ok} patches).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Install failed: {ex.Message}");
        }
    }

    private static int Patch(HarmonyLib.Harmony harmony, System.Reflection.MethodInfo method, string prefix)
    {
        if (method == null)
            return 0;
        harmony.Patch(method, prefix: new HarmonyMethod(typeof(TeleportBring), prefix));
        return 1;
    }

    /// <summary>
    /// Server receiving ClientsOnly PlayerRepTeleport for ToTarget must still relay.
    /// Skipping CheckExpectedConditions lets NativeMessageHandler.Handle forward it.
    /// </summary>
    private static bool CheckExpectedPrefix(MessageHandler __instance, ReceivedMessage received)
    {
        try
        {
            if (__instance is not PlayerRepTeleportMessage)
                return true;

            if (!received.IsServerHandled)
                return true;

            var type = received.Route.Type;
            if (type is RelayType.ToTarget or RelayType.ToTargets or RelayType.ToOtherClients)
                return false; // skip throw; Handle will relay

            return true;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Host-side: always execute TELEPORT_TO_ME / TELEPORT_TO_THEM (no permission gate).
    /// </summary>
    private static bool PermissionTeleportPrefix(ReceivedMessage received)
    {
        try
        {
            if (!received.IsServerHandled)
                return true;

            var data = received.ReadData<PermissionCommandRequestData>();
            if (data.Type is not (PermissionCommandType.TELEPORT_TO_ME or PermissionCommandType.TELEPORT_TO_THEM))
                return true;

            if (!received.Sender.HasValue)
                return false;

            var sender = PlayerIDManager.GetPlayerID(received.Sender.Value);
            if (sender == null)
                return false;

            PlayerID other = null;
            if (data.OtherPlayer.HasValue)
                other = PlayerIDManager.GetPlayerID(data.OtherPlayer.Value);
            if (other == null)
                return false;

            if (data.Type == PermissionCommandType.TELEPORT_TO_ME)
            {
                // Pull `other` to the requester (sender).
                if (!PlayerRepUtilities.TryGetReferences(sender, out var refs) || refs == null || !refs.IsValid)
                    return false;
                Vector3 pos = refs.RigManager.physicsRig.feet.transform.position;
                PlayerSender.SendPlayerTeleport(other, pos);
                MelonLogger.Msg(
                    $"[TeleportBring] Host relay TELEPORT_TO_ME → sid {other.SmallID} @ {pos}");
            }
            else
            {
                // Send requester to `other`.
                if (!PlayerRepUtilities.TryGetReferences(other, out var refs) || refs == null || !refs.IsValid)
                    return false;
                Vector3 pos = refs.RigManager.physicsRig.feet.transform.position;
                PlayerSender.SendPlayerTeleport(sender, pos);
                MelonLogger.Msg(
                    $"[TeleportBring] Host relay TELEPORT_TO_THEM → sid {sender.SmallID} @ {pos}");
            }

            return false; // skip vanilla permission-gated handler
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] PermissionTeleportPrefix: {ex.Message}");
            return true; // fall back to vanilla
        }
    }

    /// <summary>Execute Bring for a remote SmallID to a world landing position.</summary>
    internal static void BringPlayer(byte targetSid, Vector3 land)
    {
        var data = new PlayerRepTeleportData { Position = land };

        if (NetworkInfo.IsHost)
        {
            // Direct host path — same as Fusion admin teleport.
            PlayerSender.SendPlayerTeleport(targetSid, land);
            MelonLogger.Msg($"[TeleportBring] Host SendPlayerTeleport sid={targetSid} → {land}");
            return;
        }

        // Client: ask host via permission command (works on vanilla hosts with rights,
        // and always when host runs MonsterPanel TeleportBring patch).
        try
        {
            PermissionSender.SendPermissionRequest(PermissionCommandType.TELEPORT_TO_ME, targetSid);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Permission request failed: {ex.Message}");
        }

        // Also relay native ToTarget — forwarded when host has our CheckExpected patch.
        try
        {
            MessageRelay.RelayNative(
                data,
                NativeMessageTag.PlayerRepTeleport,
                new MessageRoute(targetSid, NetworkChannel.Reliable));
            MelonLogger.Msg($"[TeleportBring] Client relay+permission sid={targetSid} → {land}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] RelayNative failed: {ex.Message}");
        }
    }
}
