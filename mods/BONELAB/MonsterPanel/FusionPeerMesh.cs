using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel;

/// <summary>
/// Fusion 0.2.x clients only listen for ConnectionClosed — they never
/// <c>AcceptConnection</c>. Hosts do. Client→client EOS P2P therefore never
/// completes, so direct <see cref="EosDirectSend"/> Bring packets never arrive
/// (host-targeted actions like Kill Host still work).
///
/// Fix: when Fusion arms client peer notifications, run the host-style Accept
/// mesh instead. Also poke a peer before teleport bursts so the session opens.
/// </summary>
internal static class FusionPeerMesh
{
    private static bool _installed;
    private static readonly HashSet<string> _opening = new HashSet<string>(StringComparer.Ordinal);

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed || harmony == null)
            return;
        _installed = true;

        try
        {
            Type eosP2P = AccessTools.TypeByName("LabFusion.Network.EOSP2P");
            MethodInfo addClient = eosP2P != null
                ? AccessTools.Method(eosP2P, "AddClientPeerNotifications")
                : null;

            if (addClient == null)
            {
                MelonLogger.Warning("[PeerMesh] EOSP2P.AddClientPeerNotifications missing.");
                return;
            }

            harmony.Patch(
                addClient,
                prefix: new HarmonyMethod(typeof(FusionPeerMesh), nameof(AddClientPeerPrefix)));
            MelonLogger.Msg("[PeerMesh] Client→host-style P2P Accept mesh armed.");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("[PeerMesh] Install: " + ex.Message);
        }
    }

    /// <summary>
    /// Replace weak client notifications with host Accept + Established + Closed.
    /// </summary>
    private static bool AddClientPeerPrefix(object __instance)
    {
        try
        {
            MethodInfo hostStyle = AccessTools.Method(__instance.GetType(), "AddHostPeerNotifications");
            if (hostStyle == null)
                return true;

            hostStyle.Invoke(__instance, null);
            return false; // skip stock client-only Closed handler
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("[PeerMesh] host-style arm failed: " + ex.Message);
            return true;
        }
    }

    /// <summary>
    /// Brief pause so Accept mesh / prior bursts can finish connecting.
    /// </summary>
    internal static IEnumerator OpenToPeer(string platformId, float waitSec = 0.18f)
    {
        if (string.IsNullOrEmpty(platformId))
            yield break;

        if (!_opening.Add(platformId))
        {
            yield return new WaitForSecondsRealtime(waitSec);
            yield break;
        }

        yield return new WaitForSecondsRealtime(Mathf.Clamp(waitSec, 0.05f, 0.75f));
        _opening.Remove(platformId);
    }
}
