using System;
using System.Reflection;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Silent stock shield: Dev Manipulator / GravityManipulatorJob cannot lock or
    /// soft-pull the local player's physics rig. No menu entry, no UI strings.
    ///
    /// Mechanism (local client): the synced gun runs GravityManipulatorJob on every
    /// peer; FixedUpdate applies forces / ConfigurableJoints to nearby Rigidbodies.
    /// We strip our own RBs from its working set and block LockRigidbody on us.
    /// </summary>
    internal static class AntiManip
    {
        private static bool _installed;
        private static float _tickTimer;
        private const float TickInterval = 0.75f; // rare backup only — Harmony does the realtime work

        public static void Install(HarmonyLib.Harmony harmony)
        {
            if (_installed || harmony == null) return;
            _installed = true;

            Type job = AccessTools.TypeByName("Il2CppSLZ.Marrow.GravityManipulatorJob")
                       ?? typeof(GravityManipulatorJob);
            Type pull = AccessTools.TypeByName("Il2CppSLZ.Marrow.ForcePullGrip")
                       ?? typeof(ForcePullGrip);

            Try(harmony, job, "LockRigidbody", nameof(LockRigidbodyPrefix), new[] { typeof(Rigidbody) });
            Try(harmony, job, "ConfigureJoint", nameof(ConfigureJointPrefix), new[] { typeof(Rigidbody) });
            Try(harmony, job, "FixedUpdate", nameof(FixedUpdatePrefix), Type.EmptyTypes);
            Try(harmony, job, "GetClosestRigidbody", nameof(GetClosestPostfix), Type.EmptyTypes, postfix: true);

            Try(harmony, pull, "Pull", nameof(PullPrefix), new[] { typeof(Hand) });
            Try(harmony, pull, "OnFarHandHoverUpdate", nameof(FarHoverPrefix), new[] { typeof(Hand) });
        }

        /// <summary>
        /// Rare backup: if a job somehow still holds a lock on us, release it.
        /// Realtime immunity is Harmony FixedUpdate/LockRigidbody — this must stay rare
        /// (FindObjectsOfType is expensive on Quest).
        /// </summary>
        public static void Tick()
        {
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            var rig = BoneLib.Player.RigManager;
            if (rig == null) return;

            try
            {
                var jobs = UnityEngine.Object.FindObjectsOfType<GravityManipulatorJob>();
                if (jobs == null) return;
                for (int i = 0; i < jobs.Length; i++)
                {
                    var job = jobs[i];
                    if (job == null) continue;
                    StripJob(job, rig);
                }
            }
            catch { }
        }

        private static void Try(HarmonyLib.Harmony h, Type type, string method, string patch, Type[] args, bool postfix = false)
        {
            if (type == null) return;
            try
            {
                MethodInfo mi = AccessTools.Method(type, method, args);
                if (mi == null) mi = AccessTools.Method(type, method);
                if (mi == null) return;
                var hm = new HarmonyMethod(typeof(AntiManip), patch);
                if (postfix) h.Patch(mi, postfix: hm);
                else h.Patch(mi, prefix: hm);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Shield patch " + method + ": " + e.Message);
            }
        }

        // ---- Patches ----

        private static bool LockRigidbodyPrefix(Rigidbody rigidbody) =>
            !IsOwnRb(rigidbody);

        private static bool ConfigureJointPrefix(Rigidbody rigidbody) =>
            !IsOwnRb(rigidbody);

        private static void FixedUpdatePrefix(GravityManipulatorJob __instance)
        {
            var rig = BoneLib.Player.RigManager;
            if (rig == null || __instance == null) return;
            StripJob(__instance, rig);
        }

        private static void GetClosestPostfix(ref Rigidbody __result)
        {
            if (IsOwnRb(__result)) __result = null;
        }

        private static bool PullPrefix(ForcePullGrip __instance, Hand hand)
        {
            if (__instance == null) return true;
            try
            {
                // Force-pull on a grip that lives on our body → cancel.
                if (IsOwnTransform(__instance.transform))
                {
                    try { __instance.CancelPull(hand); } catch { }
                    return false;
                }
            }
            catch { }
            return true;
        }

        private static bool FarHoverPrefix(ForcePullGrip __instance, Hand hand) =>
            __instance == null || !IsOwnTransform(__instance.transform);

        // ---- Helpers ----

        private static void StripJob(GravityManipulatorJob job, RigManager rig)
        {
            try
            {
                // Hard lock on our body → release.
                Rigidbody locked = null;
                try { locked = job.m_LockedRigidbody; } catch { }
                if (IsOwnRb(locked))
                {
                    try { job.ReleaseLockedRigidbody(); } catch { }
                }
            }
            catch { }

            // Soft field: drop our RBs from the working set so HoverRigidbodies
            // / keep-field forces never touch the local player.
            try
            {
                var set = job.m_RigidbodyHashSet;
                if (set != null && set.Count > 0)
                {
                    Rigidbody[] buf = null;
                    try
                    {
                        int n = set.Count;
                        buf = new Rigidbody[n];
                        int i = 0;
                        foreach (var rb in set)
                        {
                            if (i >= n) break;
                            buf[i++] = rb;
                        }
                        for (int k = 0; k < i; k++)
                        {
                            var rb = buf[k];
                            if (IsOwnRb(rb))
                            {
                                try { set.Remove(rb); } catch { }
                                try { job.m_RigidbodyColliderDictionary?.Remove(rb); } catch { }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Helper joints that yank child bones toward the orb.
            try
            {
                var helpers = job._helperJoints;
                var helperRbs = job._helperRigidbodies;
                if (helpers != null)
                {
                    for (int i = helpers.Count - 1; i >= 0; i--)
                    {
                        ConfigurableJoint j = null;
                        try { j = helpers[i]; } catch { continue; }
                        if (j == null) continue;
                        bool ours = IsOwnRb(j.GetComponent<Rigidbody>()) || IsOwnRb(j.connectedBody);
                        if (!ours) continue;
                        try { UnityEngine.Object.Destroy(j); } catch { }
                        try { helpers.RemoveAt(i); } catch { }
                        try
                        {
                            if (helperRbs != null && i < helperRbs.Count)
                                helperRbs.RemoveAt(i);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static bool IsOwnRb(Rigidbody rb)
        {
            if (rb == null) return false;
            return IsOwnTransform(rb.transform);
        }

        private static bool IsOwnTransform(Transform t)
        {
            if (t == null) return false;
            var rig = BoneLib.Player.RigManager;
            if (rig == null) return false;
            try { return t.IsChildOf(rig.transform); }
            catch { return false; }
        }
    }
}
