using System;
using HarmonyLib;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using MelonLoader;

namespace MonsterPanel;

/// <summary>
/// Silent anti Kick/Ban (on by default, no BoneMenu).
///
/// Soft Fusion path: host Broadcasts Disconnect("Kicked/Banned from Server") →
/// DisconnectMessage would call NetworkHelper.Disconnect → LeaveLobby.
/// We intercept <b>only</b> that network message for our PlatformID(s).
///
/// Critical: do <b>not</b> prefix-patch NetworkHelper.Disconnect.
/// Menu leave / lobby switch / Tracking Join all call Disconnect("") (or Lobby closed).
/// Patching that API trapped players after a blocked kick (stuck session).
///
/// Always allow:
///   - voluntary leave (menu Disconnect)
///   - host left / lobby dead ("Lobby closed")
///   - Tracking Join, OOB ("Left Bounds"), join-deny before session
///
/// Does not stop EOS Lobby.KickMember hard drop; that becomes "Lobby closed".
/// </summary>
internal static class AntiKick
{
    private const string NotifyTag = "MonsterPanel.Protection";
    private const float NotifyCooldownSec = 2.0f;

    private static bool _installed;
    private static bool _sessionActive;
    private static float _nextNotifyAt;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;

        try
        {
            var method = AccessTools.Method(typeof(DisconnectMessage), "OnHandleMessage");
            if (method == null)
            {
                MelonLogger.Warning("[AntiKick] DisconnectMessage.OnHandleMessage not found.");
                return;
            }

            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AntiKick), nameof(DisconnectMessagePrefix)));

            try
            {
                MultiplayerHooking.OnJoinedServer += OnJoinedServer;
                MultiplayerHooking.OnStartedServer += OnStartedServer;
                MultiplayerHooking.OnDisconnected += OnDisconnected;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[AntiKick] Session hooks failed: {ex.Message}");
            }

            _installed = true;
            MelonLogger.Msg("[AntiKick] Silent Kick/Ban shield active (message-only; leave/switch allowed).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiKick] Install failed: {ex.Message}");
        }
    }

    private static void OnJoinedServer() => _sessionActive = true;

    private static void OnStartedServer() => _sessionActive = true;

    private static void OnDisconnected() => _sessionActive = false;

    /// <summary>
    /// Block only inbound Kick/Ban Disconnect messages targeting us.
    /// Never touches NetworkHelper.Disconnect — voluntary leave always works.
    /// </summary>
    private static bool DisconnectMessagePrefix(ReceivedMessage received)
    {
        try
        {
            if (!_sessionActive || NetworkInfo.IsHost)
                return true;

            DisconnectMessageData data = received.ReadData<DisconnectMessageData>();
            if (data == null || string.IsNullOrEmpty(data.PlatformID))
                return true;

            if (!IsOurPlatformId(data.PlatformID))
                return true;

            if (!IsKickOrBanReason(data.Reason))
                return true;

            NotifyBlocked(data.Reason);
            return false; // skip LeaveLobby from soft kick/ban
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiKick] DisconnectMessagePrefix: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Match Fusion LocalPlatformID and PidSpoof original/spoof identities
    /// (host may target either id depending on when spoof applied).
    /// </summary>
    private static bool IsOurPlatformId(string platformId)
    {
        if (string.IsNullOrEmpty(platformId))
            return false;

        try
        {
            string local = PlayerIDManager.LocalPlatformID;
            if (!string.IsNullOrEmpty(local) &&
                string.Equals(platformId, local, StringComparison.Ordinal))
                return true;
        }
        catch { /* */ }

        try
        {
            if (!string.IsNullOrEmpty(PidSpoof.SpoofPlatformId) &&
                string.Equals(platformId, PidSpoof.SpoofPlatformId, StringComparison.Ordinal))
                return true;
            if (!string.IsNullOrEmpty(PidSpoof.OriginalPlatformId) &&
                string.Equals(platformId, PidSpoof.OriginalPlatformId, StringComparison.Ordinal))
                return true;
        }
        catch { /* PidSpoof not loaded */ }

        return false;
    }

    private static bool IsKickOrBanReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        // Exact Fusion stock strings only — no substring "kicked"/"banned"
        // (avoids false positives that could confuse leave flows if reintroduced later).
        if (string.Equals(reason, "Kicked from Server", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(reason, "Banned from Server", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static void NotifyBlocked(string reason)
    {
        bool ban = !string.IsNullOrEmpty(reason)
                   && reason.IndexOf("ban", StringComparison.OrdinalIgnoreCase) >= 0;
        string body = ban ? "Ban blocked" : "Kick blocked";

        float now = UnityEngine.Time.unscaledTime;
        if (now < _nextNotifyAt)
            return;
        _nextNotifyAt = now + NotifyCooldownSec;

        try
        {
            try { Notifier.Cancel(NotifyTag); } catch { /* */ }

            Notifier.Send(new Notification
            {
                Title = "Protection",
                Message = body,
                Tag = NotifyTag,
                SaveToMenu = false,
                ShowPopup = true,
                PopupLength = 2.5f,
                Type = NotificationType.SUCCESS,
            });
        }
        catch
        {
            MelonLogger.Msg("Protection: " + body);
        }
    }
}
