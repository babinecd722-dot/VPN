using System;
using HarmonyLib;
using LabFusion.Player;
using LabFusion.Utilities;
using MelonLoader;

namespace MonsterPanel;

/// <summary>
/// Silent unlock of stock Fusion host/permission-gated tools. No BoneMenu UI.
///
/// 1) Dev Tools / Spawn Gun / Nimbus (FlyingGun):
///    Lobby <c>DevTools</c> permission + gamemode DisableDevTools/DisableSpawnGun
///    make Fusion despawn the tool or no-op SpawnGun.OnFire. We force allow for
///    the local player so stock guns work even when the host locked them.
///
/// 2) Constrainer:
///    Lobby <c>Constrainer</c> permission despawns the gun on grab; lobby
///    <c>PlayerConstraining</c> blocks constraining players. Unlock both for us.
///    Player-constraint sync still needs the host to accept ConstraintCreate —
///    same authority limit as Bring.
/// </summary>
internal static class HostToolsUnlock
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
                AccessTools.Method(typeof(FusionDevTools), nameof(FusionDevTools.PreventSpawnGun)),
                nameof(PreventSpawnGunPrefix));

            ok += Patch(harmony,
                AccessTools.Method(typeof(FusionDevTools), nameof(FusionDevTools.DespawnDevTool)),
                nameof(DespawnDevToolPrefix));

            ok += Patch(harmony,
                AccessTools.Method(typeof(FusionDevTools), nameof(FusionDevTools.DespawnConstrainer)),
                nameof(DespawnConstrainerPrefix));

            ok += Patch(harmony,
                AccessTools.PropertyGetter(typeof(FusionDevTools), nameof(FusionDevTools.DevToolsDisabled)),
                nameof(DevToolsDisabledPrefix));

            ok += Patch(harmony,
                AccessTools.PropertyGetter(typeof(ConstrainerUtilities),
                    nameof(ConstrainerUtilities.PlayerConstraintsEnabled)),
                nameof(PlayerConstraintsEnabledPrefix));

            if (ok < 5)
            {
                MelonLogger.Warning($"[HostTools] Incomplete ({ok}/5) — some stock tools may stay locked.");
                return;
            }

            _installed = true;
            MelonLogger.Msg($"[HostTools] DevTools/SpawnGun + Constrainer unlocked ({ok} patches).");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[HostTools] Install failed: {ex.Message}");
        }
    }

    private static int Patch(HarmonyLib.Harmony harmony, System.Reflection.MethodInfo method, string prefix)
    {
        if (method == null)
            return 0;
        try
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(HostToolsUnlock), prefix));
            return 1;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"[HostTools] Skip patch {prefix}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>false = allow spawn gun fire (vanilla true = block).</summary>
    private static bool PreventSpawnGunPrefix(PlayerID id, ref bool __result)
    {
        if (!IsLocal(id))
            return true;
        __result = false;
        return false;
    }

    /// <summary>false = keep nimbus/devtools (vanilla true = despawn on grab).</summary>
    private static bool DespawnDevToolPrefix(PlayerID id, ref bool __result)
    {
        if (!IsLocal(id))
            return true;
        __result = false;
        return false;
    }

    /// <summary>false = keep constrainer (vanilla true = despawn on grab).</summary>
    private static bool DespawnConstrainerPrefix(PlayerID id, ref bool __result)
    {
        if (!IsLocal(id))
            return true;
        __result = false;
        return false;
    }

    /// <summary>false = nimbus/devtools not disabled by active gamemode.</summary>
    private static bool DevToolsDisabledPrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    /// <summary>true = allow constraining players locally (and accept inbound).</summary>
    private static bool PlayerConstraintsEnabledPrefix(ref bool __result)
    {
        __result = true;
        return false;
    }

    private static bool IsLocal(PlayerID id)
    {
        try
        {
            if (id == null)
                return false;
            return id.IsMe;
        }
        catch
        {
            return false;
        }
    }
}
