using System;
using System.Reflection;
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
/// Bring / Teleport-to-player that work as host <b>and</b> client on EOS.
///
/// Client Bring used to fail: PlayerRepTeleport is ClientsOnly, so ToTarget via
/// server throws MessageExpectedClientException before relay. PermissionCommand
/// TELEPORT_* also needs lobby Teleportation rights (often OWNER-only).
///
/// Fix — same EOS hole as Kill Host / Remote Action:
///   MessageSender.SendFromServer(victimPlatformId, PlayerRepTeleport)
///   has no IsHost check on EpicGamesNetworkLayer. Victim receives ClientsOnly
///   teleport and runs LocalPlayer.TeleportToPosition.
///
/// Host path stays stock PlayerSender.SendPlayerTeleport.
/// Host-side Harmony patches still help when <b>we</b> are host (relay + no perm gate).
/// </summary>
internal static class TeleportBring
{
    private const int ClientSendBurst = 3;

    private static bool _installed;

    // Cached EOS send: SendFromServer(string,...) and/or SendPacket(ProductUserId,...)
    private static MethodInfo _eosSendFromServer;
    private static MethodInfo _eosSendPacket;
    private static bool _eosSendLookupDone;

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
                MelonLogger.Warning($"[TeleportBring] Incomplete ({ok}/2) — host relay assist partial.");
            }
            else
            {
                MelonLogger.Msg($"[TeleportBring] Host relay unlock active ({ok} patches).");
            }

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

    /// <summary>
    /// Bring remote player to <paramref name="land"/>.
    /// Host: stock SendPlayerTeleport. Client: SendFromServer PlayerRepTeleport (EOS hole).
    /// </summary>
    internal static void BringPlayer(byte targetSid, Vector3 land)
    {
        if (NetworkInfo.IsHost)
        {
            PlayerSender.SendPlayerTeleport(targetSid, land);
            MelonLogger.Msg($"[TeleportBring] Host SendPlayerTeleport sid={targetSid} → {land}");
            return;
        }

        PlayerID target = PlayerIDManager.GetPlayerID(targetSid);
        if (target == null || !target.IsValid || string.IsNullOrEmpty(target.PlatformID))
        {
            MelonLogger.Warning($"[TeleportBring] Bring: no PlatformID for sid {targetSid}");
            return;
        }

        int sent = ForceTeleportRemote(target.PlatformID, land, bursts: ClientSendBurst);
        MelonLogger.Msg(
            $"[TeleportBring] Client Bring sid={targetSid} → {land} sends={sent} (SendFromServer PlayerRepTeleport)");

        // Extra assists (MonsterPanel host / lobbies that grant Teleportation).
        try
        {
            PermissionSender.SendPermissionRequest(PermissionCommandType.TELEPORT_TO_ME, targetSid);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Permission assist failed: {ex.Message}");
        }

        try
        {
            MessageRelay.RelayNative(
                new PlayerRepTeleportData { Position = land },
                NativeMessageTag.PlayerRepTeleport,
                new MessageRoute(targetSid, NetworkChannel.Reliable));
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] RelayNative assist failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Move local player to <paramref name="land"/> facing <paramref name="forward"/>.
    /// Local Fusion teleport immediately; also SendFromServer to self (same ClientsOnly path
    /// a host uses) so pose authority matches server-forced teleports.
    /// </summary>
    internal static void TeleportLocalTo(Vector3 land, Vector3 forward)
    {
        try
        {
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
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
            string me = PlayerIDManager.LocalPlatformID;
            if (string.IsNullOrEmpty(me))
                return;

            // When we are host, also use stock path (ToTarget to self via relay).
            if (NetworkInfo.IsHost)
            {
                PlayerSender.SendPlayerTeleport(PlayerIDManager.LocalSmallID, land);
            }

            int sent = ForceTeleportRemote(me, land, bursts: 2);
            MelonLogger.Msg($"[TeleportBring] Teleport-to-player local+force → {land} sends={sent}");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] Self force teleport: {ex.Message}");
        }
    }

    /// <summary>
    /// Deliver PlayerRepTeleport to a specific PlatformID via SendFromServer (no IsHost on EOS).
    /// Victim's PlayerRepTeleportMessage → LocalPlayer.TeleportToPosition.
    /// </summary>
    private static int ForceTeleportRemote(string platformId, Vector3 land, int bursts)
    {
        if (string.IsNullOrEmpty(platformId) || bursts <= 0)
            return 0;

        int ok = 0;
        for (int i = 0; i < bursts; i++)
        {
            if (SendTeleportOnce(platformId, land))
                ok++;
        }
        return ok;
    }

    private static bool SendTeleportOnce(string platformId, Vector3 land)
    {
        try
        {
            var data = new PlayerRepTeleportData { Position = land };

            using (NetWriter writer = NetWriter.Create())
            {
                writer.SerializeValue(ref data);
                using NetMessage message = NetMessage.Create(
                    NativeMessageTag.PlayerRepTeleport,
                    writer,
                    CommonMessageRoutes.None);
                MessageSender.SendFromServer(platformId, NetworkChannel.Reliable, message);
                TryEosSendPacket(platformId, message, isServerHandled: false);
            }

            // SmallID path when target is still in PlayerID map.
            try
            {
                PlayerID id = PlayerIDManager.GetPlayerID(platformId);
                if (id != null && id.IsValid)
                {
                    using NetWriter writer = NetWriter.Create();
                    var data2 = new PlayerRepTeleportData { Position = land };
                    writer.SerializeValue(ref data2);
                    using NetMessage message = NetMessage.Create(
                        NativeMessageTag.PlayerRepTeleport,
                        writer,
                        CommonMessageRoutes.None);
                    MessageSender.SendFromServer(id.SmallID, NetworkChannel.Reliable, message);
                }
            }
            catch { /* */ }

            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] SendTeleportOnce: {ex.Message}");
            return false;
        }
    }

    private static void TryEosSendPacket(string platformId, NetMessage message, bool isServerHandled)
    {
        try
        {
            EnsureEosSendPacket();
            if (message == null || string.IsNullOrEmpty(platformId)) return;

            // Preferred: EOSMessenger.SendFromServer(string, channel, message) — same hole as Layer.
            if (_eosSendFromServer != null)
            {
                _eosSendFromServer.Invoke(null, new object[] { platformId, NetworkChannel.Reliable, message });
                return;
            }

            if (_eosSendPacket == null) return;

            Type puidType = AccessTools.TypeByName("Epic.OnlineServices.ProductUserId");
            if (puidType == null) return;

            MethodInfo fromString = AccessTools.Method(puidType, "FromString", new[] { typeof(string) });
            if (fromString == null) return;

            object userId = fromString.Invoke(null, new object[] { platformId });
            if (userId == null) return;

            _eosSendPacket.Invoke(null, new object[] { userId, message, NetworkChannel.Reliable, isServerHandled });
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] EOS SendPacket: {ex.Message}");
        }
    }

    private static void EnsureEosSendPacket()
    {
        if (_eosSendLookupDone) return;
        _eosSendLookupDone = true;
        try
        {
            // Fusion 0.1.x keeps EOSMessenger; some builds rename/move messaging — probe both.
            Type messenger = AccessTools.TypeByName("LabFusion.Network.EpicGames.EOSMessenger")
                             ?? AccessTools.TypeByName("LabFusion.Network.EpicGames.EOSMessagingComponent");
            if (messenger == null)
            {
                MelonLogger.Warning("[TeleportBring] EOS messenger type not found (MessageSender path still used).");
                return;
            }

            foreach (MethodInfo m in messenger.GetMethods(
                         BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ParameterInfo[] p = m.GetParameters();
                if (m.Name == "SendFromServer"
                    && p.Length == 3
                    && p[0].ParameterType == typeof(string)
                    && p[2].ParameterType == typeof(NetMessage))
                {
                    _eosSendFromServer = m;
                }
                else if (m.Name == "SendPacket"
                         && p.Length == 4
                         && p[1].ParameterType == typeof(NetMessage)
                         && p[3].ParameterType == typeof(bool))
                {
                    _eosSendPacket = m;
                }
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[TeleportBring] EOS lookup: {ex.Message}");
        }
    }
}
