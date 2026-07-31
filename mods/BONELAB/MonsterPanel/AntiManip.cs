using System;
using System.Reflection;
using HarmonyLib;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// Silent stock shield: others' Dev Manipulator cannot lock / soft-pull YOUR
    /// physics body. YOUR Dev Manipulator against others is unaffected.
    ///
    /// Rule: only strip / block when the *victim* rigidbody is under the local
    /// physicsRig. Never touch joints just because connectedBody is on your gun
    /// (that would break your own locks on other players).
    /// </summary>
    internal static class AntiManip
    {
        private static bool _installed;
        private static float _tickTimer;
        private const float TickInterval = 0.75f;

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

        public static void Tick()
        {
            _tickTimer -= Time.deltaTime;
            if (_tickTimer > 0f) return;
            _tickTimer = TickInterval;

            if (BoneLib.Player.RigManager == null) return;

            try
            {
                var jobs = UnityEngine.Object.FindObjectsOfType<GravityManipulatorJob>();
                if (jobs == null) return;
                for (int i = 0; i < jobs.Length; i++)
                {
                    var job = jobs[i];
                    if (job == null) continue;
                    StripForeignLocksOnSelf(job);
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

        // ---- Patches (victim-only: never blocks locking someone else) ----

        private static bool LockRigidbodyPrefix(Rigidbody rigidbody) =>
            !IsOwnBodyRb(rigidbody);

        private static bool ConfigureJointPrefix(Rigidbody rigidbody) =>
            !IsOwnBodyRb(rigidbody);

        private static void FixedUpdatePrefix(GravityManipulatorJob __instance)
        {
            if (__instance == null) return;
            StripForeignLocksOnSelf(__instance);
        }

        private static void GetClosestPostfix(ref Rigidbody __result)
        {
            // Don't pick YOUR body as the lock target — others stay valid.
            if (IsOwnBodyRb(__result)) __result = null;
        }

        private static bool PullPrefix(ForcePullGrip __instance, Hand hand)
        {
            if (!IsOwnBodyForcePull(__instance)) return true;
            try { __instance.CancelPull(hand); } catch { }
            return false;
        }

        private static bool FarHoverPrefix(ForcePullGrip __instance, Hand hand) =>
            !IsOwnBodyForcePull(__instance);

        // ---- Strip only effects whose victim is the local body ----

        private static void StripForeignLocksOnSelf(GravityManipulatorJob job)
        {
            // Hard lock on our body → release. Lock on someone else → leave alone.
            try
            {
                Rigidbody locked = null;
                try { locked = job.m_LockedRigidbody; } catch { }
                if (IsOwnBodyRb(locked))
                {
                    try { job.ReleaseLockedRigidbody(); } catch { }
                }
            }
            catch { }

            // Soft field: remove only OUR body RBs from the working set.
            // Other players' RBs stay so YOUR gun can still lift them.
            try
            {
                var set = job.m_RigidbodyHashSet;
                if (set != null && set.Count > 0)
                {
                    try
                    {
                        int n = set.Count;
                        var buf = new Rigidbody[n];
                        int i = 0;
                        foreach (var rb in set)
                        {
                            if (i >= n) break;
                            buf[i++] = rb;
                        }
                        for (int k = 0; k < i; k++)
                        {
                            var rb = buf[k];
                            if (!IsOwnBodyRb(rb)) continue;
                            try { set.Remove(rb); } catch { }
                            try { job.m_RigidbodyColliderDictionary?.Remove(rb); } catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Helper joints: destroy only when the VICTIM rb is our body.
            // Do NOT check connectedBody — when YOU lock someone else, connectedBody
            // is often your manipulator anchor under your rig.
            try
            {
                var helpers = job._helperJoints;
                var helperRbs = job._helperRigidbodies;
                if (helpers == null) return;

                for (int i = helpers.Count - 1; i >= 0; i--)
                {
                    ConfigurableJoint j = null;
                    try { j = helpers[i]; } catch { continue; }
                    if (j == null) continue;

                    bool victimOurs = false;
                    try
                    {
                        if (helperRbs != null && i < helperRbs.Count)
                            victimOurs = IsOwnBodyRb(helperRbs[i]);
                    }
                    catch { }
                    if (!victimOurs)
                    {
                        try { victimOurs = IsOwnBodyRb(j.GetComponent<Rigidbody>()); }
                        catch { }
                    }
                    if (!victimOurs) continue;

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
            catch { }
        }

        /// <summary>
        /// Local player avatar / physics body only — never the Dev Manipulator gun
        /// and never a held world prop under the hand.
        /// </summary>
        private static bool IsOwnBodyRb(Rigidbody rb)
        {
            if (rb == null) return false;
            var rig = BoneLib.Player.RigManager;
            if (rig == null) return false;

            try
            {
                // Own gun must never count as "body" or we break self-use.
                if (rb.GetComponentInParent<DevManipulatorGun>() != null) return false;
                if (rb.GetComponentInParent<GravityManipulatorJob>() != null) return false;

                var pr = rig.physicsRig;
                if (pr == null || !rb.transform.IsChildOf(pr.transform)) return false;

                // Clear body marker.
                if (rb.GetComponentInParent<AvatarGrip>() != null) return true;

                // Held prop under a hand — not body; leave manipulable.
                if (rb.GetComponentInParent<Hand>() != null &&
                    rb.GetComponentInParent<InteractableHost>() != null)
                    return false;

                // Physics-rig bone / marrow body.
                return true;
            }
            catch { return false; }
        }

        private static bool IsOwnBodyForcePull(ForcePullGrip pull)
        {
            if (pull == null) return false;
            try
            {
                if (pull.GetComponentInParent<DevManipulatorGun>() != null) return false;
                if (pull.GetComponentInParent<AvatarGrip>() == null) return false;
                var rig = BoneLib.Player.RigManager;
                if (rig == null) return false;
                return pull.transform.IsChildOf(rig.transform);
            }
            catch { return false; }
        }
    }
}
