using System;
using System.Reflection;
using HarmonyLib;
using LabFusion.Network;
using MelonLoader;

namespace MonsterPanel;

/// <summary>
/// Client→peer EOS sends that survive Fusion 0.2.0.
///
/// 0.2.0 added <c>if (!IsHost) return;</c> on
/// <c>EpicGamesNetworkLayer.SendFromServer</c>, so
/// <see cref="MessageSender.SendFromServer"/> is a no-op for clients.
/// The underlying <c>EOSP2PSender.Send</c> still has no host gate — call it
/// via <c>layer.Runtime.P2P.Sender</c>. Legacy <c>EOSMessenger</c> kept as fallback.
/// </summary>
internal static class EosDirectSend
{
    private static bool _lookupDone;
    private static MethodInfo _p2pSendNetMessage; // (ProductUserId, NetMessage, NetworkChannel, bool)
    private static MethodInfo _p2pSendBytes;      // (ProductUserId, byte[], NetworkChannel, bool)
    private static MethodInfo _legacySendPacket;  // EOSMessenger.SendPacket(...)
    private static MethodInfo _legacySendFromServer;
    private static string _resolvedPath = "none";

    /// <summary>
    /// Deliver a ClientsOnly / FromServer-shaped packet to <paramref name="platformId"/>.
    /// Tries MessageSender (works when we are host), then direct EOSP2PSender / legacy messenger.
    /// </summary>
    internal static bool SendToPeer(string platformId, NetMessage message, bool isServerHandled)
    {
        if (string.IsNullOrEmpty(platformId) || message == null)
            return false;

        bool any = false;
        try
        {
            MessageSender.SendFromServer(platformId, NetworkChannel.Reliable, message);
            any = true; // may be a no-op on 0.2.0 client — still try direct path
        }
        catch { /* */ }

        if (TryDirectP2P(platformId, message, isServerHandled))
            any = true;

        return any;
    }

    private static bool TryDirectP2P(string platformId, NetMessage message, bool isServerHandled)
    {
        try
        {
            EnsureLookup();
            object userId = ProductUserIdFromString(platformId);
            if (userId == null)
                return false;

            if (_p2pSendNetMessage != null)
            {
                object sender = GetP2PSender();
                if (sender != null)
                {
                    _p2pSendNetMessage.Invoke(sender, new object[]
                    {
                        userId, message, NetworkChannel.Reliable, isServerHandled
                    });
                    return true;
                }
            }

            if (_p2pSendBytes != null)
            {
                object sender = GetP2PSender();
                if (sender != null)
                {
                    byte[] bytes = message.ToByteArray();
                    _p2pSendBytes.Invoke(sender, new object[]
                    {
                        userId, bytes, NetworkChannel.Reliable, isServerHandled
                    });
                    return true;
                }
            }

            if (_legacySendFromServer != null)
            {
                _legacySendFromServer.Invoke(null, new object[] { platformId, NetworkChannel.Reliable, message });
                return true;
            }

            if (_legacySendPacket != null)
            {
                _legacySendPacket.Invoke(null, new object[] { userId, message, NetworkChannel.Reliable, isServerHandled });
                return true;
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("[EosDirectSend] " + ex.Message);
        }
        return false;
    }

    private static object GetP2PSender()
    {
        object layer = GetEpicLayer();
        if (layer == null) return null;

        object runtime = AccessTools.Field(layer.GetType(), "Runtime")?.GetValue(layer)
                         ?? AccessTools.Field(layer.GetType(), "_runtime")?.GetValue(layer);
        if (runtime == null) return null;

        object p2p = AccessTools.Property(runtime.GetType(), "P2P")?.GetValue(runtime)
                     ?? AccessTools.Field(runtime.GetType(), "P2P")?.GetValue(runtime);
        if (p2p == null) return null;

        return AccessTools.Field(p2p.GetType(), "Sender")?.GetValue(p2p)
               ?? AccessTools.Property(p2p.GetType(), "Sender")?.GetValue(p2p);
    }

    private static object GetEpicLayer()
    {
        try
        {
            Type mgr = FindLabFusionType("LabFusion.Network.NetworkLayerManager");
            object layer = mgr != null ? AccessTools.Property(mgr, "Layer")?.GetValue(null) : null;
            if (layer == null)
            {
                Type netInfo = FindLabFusionType("LabFusion.Network.NetworkInfo");
                layer = netInfo != null ? AccessTools.Property(netInfo, "Layer")?.GetValue(null) : null;
            }
            if (layer == null) return null;

            Type epic = FindLabFusionType("LabFusion.Network.EpicGamesNetworkLayer")
                        ?? FindLabFusionType("LabFusion.Network.EpicGames.EpicGamesNetworkLayer");
            if (epic != null && !epic.IsInstanceOfType(layer))
                return null;
            return layer;
        }
        catch { return null; }
    }

    private static void EnsureLookup()
    {
        if (_lookupDone) return;
        _lookupDone = true;
        try
        {
            // Fusion 0.2.0 — EOSP2PSender instance methods
            Type senderType = FindLabFusionType("LabFusion.Network.EOSP2PSender");
            if (senderType != null)
            {
                foreach (MethodInfo m in senderType.GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (m.Name != "Send") continue;
                    ParameterInfo[] p = m.GetParameters();
                    if (p.Length != 4) continue;
                    if (p[2].ParameterType != typeof(NetworkChannel) || p[3].ParameterType != typeof(bool))
                        continue;
                    if (p[1].ParameterType == typeof(NetMessage))
                        _p2pSendNetMessage = m;
                    else if (p[1].ParameterType == typeof(byte[]))
                        _p2pSendBytes = m;
                }
                if (_p2pSendNetMessage != null || _p2pSendBytes != null)
                    _resolvedPath = "EOSP2PSender.Send";
            }

            // Legacy ≤0.1.x EOSMessenger
            Type messenger = FindLabFusionType("LabFusion.Network.EpicGames.EOSMessenger")
                             ?? FindLabFusionType("LabFusion.Network.EOSMessenger");
            if (messenger != null)
            {
                foreach (MethodInfo m in messenger.GetMethods(
                             BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    ParameterInfo[] p = m.GetParameters();
                    if (m.Name == "SendFromServer" && p.Length == 3
                        && p[0].ParameterType == typeof(string)
                        && p[2].ParameterType == typeof(NetMessage))
                    {
                        _legacySendFromServer = m;
                    }
                    else if (m.Name == "SendPacket" && p.Length == 4
                             && p[1].ParameterType == typeof(NetMessage)
                             && p[3].ParameterType == typeof(bool))
                    {
                        _legacySendPacket = m;
                    }
                }
                if (_resolvedPath == "none" && (_legacySendPacket != null || _legacySendFromServer != null))
                    _resolvedPath = "EOSMessenger";
            }

            MelonLogger.Msg("[EosDirectSend] path = " + _resolvedPath);
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("[EosDirectSend] lookup: " + ex.Message);
        }
    }

    private static object ProductUserIdFromString(string platformId)
    {
        Type puidType = FindLabFusionType("Epic.OnlineServices.ProductUserId")
                        ?? AccessTools.TypeByName("Epic.OnlineServices.ProductUserId");
        if (puidType == null) return null;
        MethodInfo fromString = AccessTools.Method(puidType, "FromString", new[] { typeof(string) });
        return fromString?.Invoke(null, new object[] { platformId });
    }

    private static Type FindLabFusionType(string fullName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                string an = asm.GetName().Name;
                if (an != "LabFusion" && an != "Assembly-CSharp" && !an.StartsWith("Epic.", StringComparison.Ordinal))
                    continue;
                Type t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t != null) return t;
            }
            catch { /* */ }
        }
        try { return Type.GetType(fullName + ", LabFusion", throwOnError: false); }
        catch { return null; }
    }
}
