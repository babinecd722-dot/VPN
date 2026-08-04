using System;
using System.Collections;
using HarmonyLib;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Representation;
using LabFusion.Senders;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel;

/// <summary>
/// Bring / Teleport-to-player for Fusion 0.2.x EOS.
///
/// Why Bring broke again on 0.2.0+:
///   Clients never AcceptConnection (only the host does), so
///   <see cref="EosDirectSend"/> to another player never arrives.
///   <see cref="FusionPeerMesh"/> restores the peer mesh; this class
///   then multi-paths the teleport like stock host packets.
///
/// Paths (client Bring):
///   1. Open peer mesh → EosDirectSend PlayerRepTeleport (forged Sender=0)
///   2. ToServer ToTarget relay (works when host also runs MonsterPanel)
///   3. PermissionCommand TELEPORT_TO_ME (lobbies that allow Teleportation)
///   4. Timed retries (0.2s / 0.5s / 1.0s)
///
/// Host Bring stays stock <see cref="PlayerSender.SendPlayerTeleport"/>.
/// </summary>
internal static class TeleportBring
{
    private const int BurstCount = 4;
    private const byte HostSenderId = 0;

    private static bool _installed;
    private static readonly float[] RetryDelays = { 0.2f, 0.5f, 1.0f };

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
                MelonLogger.Warning($"[TeleportBring] Incomplete ({ok}/2) — host relay assist partial.");
            else
                MelonLogger.Msg($"[TeleportBring] Host relay unlock active ({ok} patches).");

            _installed = true;
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
        try
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(TeleportBring), prefix));
            return 1;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Skip patch {prefix}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Server receiving ClientsOnly PlayerRepTeleport for ToTarget must still relay.
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
    /// Host-side: always honor TELEPORT_TO_ME / TELEPORT_TO_THEM (no lobby gate).
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
                if (!PlayerRepUtilities.TryGetReferences(sender, out var refs) || refs == null || !refs.IsValid)
                    return false;
                Vector3 pos = refs.RigManager.physicsRig.feet.transform.position;
                PlayerSender.SendPlayerTeleport(other, pos);
                MelonLogger.Msg($"[TeleportBring] Host TELEPORT_TO_ME → sid {other.SmallID} @ {pos}");
            }
            else
            {
                if (!PlayerRepUtilities.TryGetReferences(other, out var refs) || refs == null || !refs.IsValid)
                    return false;
                Vector3 pos = refs.RigManager.physicsRig.feet.transform.position;
                PlayerSender.SendPlayerTeleport(sender, pos);
                MelonLogger.Msg($"[TeleportBring] Host TELEPORT_TO_THEM → sid {sender.SmallID} @ {pos}");
            }

            return false;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] PermissionTeleportPrefix: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Bring remote player to <paramref name="land"/>.
    /// </summary>
    internal static void BringPlayer(byte targetSid, Vector3 land)
    {
        if (!NetworkInfo.HasServer)
        {
            MelonLogger.Warning("[TeleportBring] Bring: not in a lobby.");
            return;
        }

        if (NetworkInfo.IsHost)
        {
            BurstHostTeleport(targetSid, land);
            MelonLogger.Msg($"[TeleportBring] Host Bring sid={targetSid} → {land}");
            return;
        }

        PlayerID target = PlayerIDManager.GetPlayerID(targetSid);
        if (target == null || !target.IsValid || string.IsNullOrEmpty(target.PlatformID))
        {
            MelonLogger.Warning($"[TeleportBring] Bring: no PlatformID for sid {targetSid}");
            return;
        }

        MelonCoroutines.Start(ClientBringRoutine(targetSid, target.PlatformID, land));
    }

    /// <summary>
    /// Move local player to <paramref name="land"/> facing <paramref name="forward"/>.
    /// </summary>
    internal static void TeleportLocalTo(Vector3 land, Vector3 forward)
    {
        try
        {
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;

            // Clear velocities first so Fusion/physics don't yank us back.
            try
            {
                if (RigData.HasPlayer)
                {
                    var rm = RigData.Refs.RigManager;
                    if (rm?.physicsRig?.selfRbs != null)
                    {
                        foreach (var rb in rm.physicsRig.selfRbs)
                        {
                            if (rb == null) continue;
                            rb.velocity = Vector3.zero;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                }
            }
            catch { /* */ }

            LocalPlayer.TeleportToPosition(land, forward);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Local TeleportToPosition: {ex.Message}");
        }

        if (!NetworkInfo.HasServer)
            return;

        try
        {
            if (NetworkInfo.IsHost)
                BurstHostTeleport(PlayerIDManager.LocalSmallID, land);

            string me = PlayerIDManager.LocalPlatformID;
            if (!string.IsNullOrEmpty(me))
                BurstDirectTeleport(me, PlayerIDManager.LocalSmallID, land, BurstCount);

            MelonLogger.Msg($"[TeleportBring] Teleport-to-player → {land}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Self force teleport: {ex.Message}");
        }
    }

    private static IEnumerator ClientBringRoutine(byte targetSid, string platformId, Vector3 land)
    {
        // 1) Open peer mesh (Fusion 0.2.x clients otherwise never AcceptConnection).
        yield return FusionPeerMesh.OpenToPeer(platformId, 0.2f);

        int direct = BurstDirectTeleport(platformId, targetSid, land, BurstCount);
        int relay = TryServerRelay(targetSid, land) ? 1 : 0;
        int perm = TryPermissionAssist(targetSid) ? 1 : 0;

        MelonLogger.Msg(
            $"[TeleportBring] Client Bring sid={targetSid} → {land} direct={direct} relay={relay} perm={perm}");

        // 2) Retries — first poke often only establishes the socket.
        foreach (float delay in RetryDelays)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return FusionPeerMesh.OpenToPeer(platformId, 0.12f);
            BurstDirectTeleport(platformId, targetSid, land, BurstCount);
            TryServerRelay(targetSid, land);
            TryPermissionAssist(targetSid);
        }
    }

    private static void BurstHostTeleport(byte targetSid, Vector3 land)
    {
        for (int i = 0; i < BurstCount; i++)
        {
            try { PlayerSender.SendPlayerTeleport(targetSid, land); }
            catch { /* */ }
        }
    }

    /// <summary>
    /// Craft a stock-shaped PlayerRepTeleport (EncodePosition via Serialize) and
    /// deliver via EosDirectSend on the client P2P channel (isServerHandled=false),
    /// matching host <c>SendFromServer</c> wire format. Sender forged as host (0).
    /// </summary>
    private static int BurstDirectTeleport(string platformId, byte targetSid, Vector3 land, int bursts)
    {
        if (string.IsNullOrEmpty(platformId) || bursts <= 0)
            return 0;

        int ok = 0;
        for (int i = 0; i < bursts; i++)
        {
            if (SendDirectOnce(platformId, targetSid, land))
                ok++;
        }
        return ok;
    }

    private static bool SendDirectOnce(string platformId, byte targetSid, Vector3 land)
    {
        try
        {
            var data = new PlayerRepTeleportData { Position = land };

            // Match MessageRelay.RelayNative: explicit INetSerializable.Serialize
            // so NetworkTransformManager.EncodePosition runs.
            using NetWriter writer = NetWriter.Create(data.GetSize() ?? 16);
            data.Serialize(writer);

            // ToTarget + Sender=0 → looks like a host-forced teleport on the wire.
            var route = new MessageRoute(targetSid, NetworkChannel.Reliable);
            using NetMessage message = NetMessage.Create(
                NativeMessageTag.PlayerRepTeleport,
                writer,
                route,
                HostSenderId);

            bool sent = EosDirectSend.SendToPeer(platformId, message, isServerHandled: false);

            // Also fire a None-route copy (some receivers only care about payload).
            using NetWriter writer2 = NetWriter.Create(data.GetSize() ?? 16);
            var data2 = new PlayerRepTeleportData { Position = land };
            data2.Serialize(writer2);
            using NetMessage message2 = NetMessage.Create(
                NativeMessageTag.PlayerRepTeleport,
                writer2,
                CommonMessageRoutes.None,
                HostSenderId);
            sent |= EosDirectSend.SendToPeer(platformId, message2, isServerHandled: false);

            return sent;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] SendDirectOnce: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ask the lobby host to relay ToTarget (needs MonsterPanel on host, or our patch).
    /// </summary>
    private static bool TryServerRelay(byte targetSid, Vector3 land)
    {
        try
        {
            MessageRelay.RelayNative(
                new PlayerRepTeleportData { Position = land },
                NativeMessageTag.PlayerRepTeleport,
                new MessageRoute(targetSid, NetworkChannel.Reliable));
            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Server relay: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stock PermissionCommand — works when lobby Teleportation allows our level.
    /// Also forges TELEPORT_TO_THEM as the victim (Sender=target) asking to go to us:
    /// host then SendPlayerTeleport(victim → our feet). Same EOS Sender forge as Kick.
    /// </summary>
    private static bool TryPermissionAssist(byte targetSid)
    {
        bool any = false;
        try
        {
            PermissionSender.SendPermissionRequest(PermissionCommandType.TELEPORT_TO_ME, targetSid);
            any = true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Permission assist: {ex.Message}");
        }

        try
        {
            // Victim "requests" TELEPORT_TO_THEM → us: host pulls victim to our stand pose.
            var data = new PermissionCommandRequestData
            {
                Type = PermissionCommandType.TELEPORT_TO_THEM,
                OtherPlayer = PlayerIDManager.LocalSmallID
            };
            using NetWriter writer = NetWriter.Create();
            data.Serialize(writer);
            using NetMessage message = NetMessage.Create(
                NativeMessageTag.PermissionCommandRequest,
                writer,
                CommonMessageRoutes.ReliableToServer,
                targetSid); // forged sender = victim
            MessageSender.SendToServer(NetworkChannel.Reliable, message);
            any = true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Forged TELEPORT_TO_THEM: {ex.Message}");
        }

        return any;
    }
}
