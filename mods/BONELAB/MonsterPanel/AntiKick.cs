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
/// we would LeaveLobby. Block that while a real session is active.
///
/// Always allow:
///   - voluntary leave (empty reason / menu)
///   - host left / lobby dead ("Lobby closed") — Kill Host, host quit, EOS drop
///   - Tracking Join disconnect, OOB ("Left Bounds" → AntiOob), join-deny before session
///
/// Does not stop EOS Lobby.KickMember hard drop; that becomes "Lobby closed" and is allowed.
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

        int ok = 0;
        try
        {
            ok += Patch(harmony,
                AccessTools.Method(typeof(DisconnectMessage), "OnHandleMessage"),
                nameof(DisconnectMessagePrefix));

            ok += Patch(harmony,
                AccessTools.Method(typeof(NetworkHelper), nameof(NetworkHelper.Disconnect), new[] { typeof(string) }),
                nameof(DisconnectPrefix));

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

            if (ok < 2)
            {
                MelonLogger.Warning($"[AntiKick] Incomplete ({ok}/2) — soft Kick/Ban may still land.");
                if (ok >= 1)
                    _installed = true;
                return;
            }

            _installed = true;
            MelonLogger.Msg("[AntiKick] Silent Kick/Ban shield active (session-scoped; lobby-close allowed).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiKick] Install failed: {ex.Message}");
        }
    }

    private static int Patch(HarmonyLib.Harmony harmony, System.Reflection.MethodInfo method, string prefix)
    {
        if (method == null)
            return 0;
        try
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AntiKick), prefix));
            return 1;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiKick] Skip patch {prefix}: {ex.Message}");
            return 0;
        }
    }

    private static void OnJoinedServer() => _sessionActive = true;

    private static void OnStartedServer() => _sessionActive = true;

    private static void OnDisconnected() => _sessionActive = false;

    /// <summary>
    /// Block only when the Disconnect packet targets us with a Kick/Ban reason
    /// during an established session. Other players leaving still runs vanilla.
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

            if (data.PlatformID != PlayerIDManager.LocalPlatformID)
                return true;

            if (!IsKickOrBanReason(data.Reason))
                return true;

            NotifyBlocked(data.Reason);
            return false; // skip LeaveLobby
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[AntiKick] DisconnectMessagePrefix: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Belt-and-suspenders: anything calling NetworkHelper.Disconnect with Kick/Ban
    /// reason mid-session is blocked. Empty / Lobby closed / Tracking / OOB pass.
    /// </summary>
    private static bool DisconnectPrefix(string reason)
    {
        try
        {
            if (!_sessionActive || NetworkInfo.IsHost)
                return true;

            if (!IsKickOrBanReason(reason))
                return true;

            NotifyBlocked(reason);
            return false;
        }
        catch
        {
            return true;
        }
    }

    private static bool IsKickOrBanReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        // Exact Fusion stock strings first.
        if (string.Equals(reason, "Kicked from Server", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(reason, "Banned from Server", StringComparison.OrdinalIgnoreCase))
            return true;

        // Soft match for minor wording variants — never match "Lobby closed".
        string r = reason.Trim();
        if (r.IndexOf("lobby closed", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (r.IndexOf("left bounds", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (r.IndexOf("tracking", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        if (r.IndexOf("kicked", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (r.IndexOf("banned", StringComparison.OrdinalIgnoreCase) >= 0)
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
