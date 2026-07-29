using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Хват телефона. Ключевое отличие от прошлой версии: поза строится по
    /// СИСТЕМЕ КООРДИНАТ ЛАДОНИ (HandFrame), а не от hand.transform.position.
    ///
    /// Раньше телефон ставился от начала трансформа кисти — а это запястье,
    /// поэтому корпус длиной 16 см наполовину уходил внутрь руки.
    /// Теперь: корпус лежит НА ладони (сдвиг по нормали наружу), а ладонь
    /// держит нижнюю треть — ровно как держат телефон в жизни.
    /// </summary>
    internal static class PhoneGrab
    {
        public static bool Enabled = true;
        // Притягивание выключено по умолчанию: оно ловило любое сжатие кулака
        // в радиусе шести метров, и телефон прилетал в руку сам собой.
        public static bool PullEnabled = false;

        // Подгонка позы ползунками в меню.
        public static float HoldOut  = 0.012f;   // от ладони наружу (полтолщины + зазор)
        public static float HoldUp   = 0.045f;   // вдоль пальцев: ладонь держит нижнюю треть
        public static float HoldSide = 0.0f;     // поперёк ладони
        public static float HoldTilt = 0f;       // доворот вокруг оси пальцев
        /// <summary>Аварийный переворот стороны ладони, если знак всё же не тот.</summary>
        public static bool FlipPalm = false;

        private const float GrabRange = 0.20f;   // от поверхности корпуса
        private const float GripOn    = 0.32f;   // берём легко, с первого раза
        private const float GripOff   = 0.18f;
        private const float BlendTime = 0.10f;

        private const float PullRange = 6f;
        private const float PullAngle = 35f;
        private const float PullSpeed = 7f;

        private const float ThrowScale = 1.15f;
        private const float MaxThrow   = 9f;

        private sealed class Held
        {
            public Hand Hand;
            public Vector3 LocalPos;
            public Quaternion LocalRot;
            public Vector3 StartLocalPos;
            public Quaternion StartLocalRot;
            public float Blend;
            public Vector3 PrevPos;
            public Vector3 Velocity;
        }

        private static readonly Dictionary<int, Held> _held = new Dictionary<int, Held>();
        private static float _pullingUntil;
        private static float _nextGripLog;

        public static bool IsHeld(PhoneInstance p) =>
            p?.Root != null && _held.ContainsKey(p.Root.GetInstanceID());

        /// <summary>Рука, которая сейчас держит телефон (или null). Нужна тачу,
        /// чтобы пальцы хвата не «нажимали» на собственный экран.</summary>
        public static Hand HandOf(PhoneInstance p)
        {
            if (p?.Root == null) return null;
            return _held.TryGetValue(p.Root.GetInstanceID(), out var h) ? h.Hand : null;
        }

        public static void Tick(PhoneInstance phone)
        {
            if (!Enabled || phone == null || !phone.Alive) return;
            int id = phone.Root.GetInstanceID();

            if (_held.TryGetValue(id, out var h)) { HoldTick(phone, h, id); return; }

            TryGrab(phone, id);

            if (Time.time > _pullingUntil)
            {
                try
                {
                    var rb = phone.Root.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic && !rb.useGravity) rb.useGravity = true;
                }
                catch { }
            }
        }

        /// <summary>Сила сжатия кулака 0..1.</summary>
        private static float GripAmount(Hand hand)
        {
            try
            {
                var c = hand.Controller;
                if (c == null) return 0f;
                float g = Mathf.Max(c._gripForce, c._solvedGrip);
                return Mathf.Clamp01(g);
            }
            catch { return 0f; }
        }

        /// <summary>Целевая поза телефона в ладони, в мировых координатах.</summary>
        private static bool PoseInHand(Hand hand, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            var f = HandFrame.Of(hand);
            if (!f.Valid) return false;

            // +Z телефона (экран) — по нормали ладони наружу
            // +Y телефона (верх)  — вдоль пальцев
            Vector3 nrm = FlipPalm ? -f.Normal : f.Normal;
            rot = Quaternion.LookRotation(nrm, f.Along);
            if (Mathf.Abs(HoldTilt) > 0.01f)
                rot = Quaternion.AngleAxis(HoldTilt, f.Along) * rot;

            pos = f.Palm
                + nrm * HoldOut          // лежит НА ладони, не внутри неё
                + f.Along  * HoldUp      // ладонь держит нижнюю треть
                + f.Across * HoldSide;
            return true;
        }

        private static void TryGrab(PhoneInstance phone, int id)
        {
            try
            {
                var root = phone.Root.transform;
                var col = phone.Root.GetComponent<Collider>();

                foreach (var hand in new[] { BoneLib.Player.RightHand, BoneLib.Player.LeftHand })
                {
                    if (hand == null) continue;

                    var f = HandFrame.Of(hand);
                    Vector3 handPos = f.Valid ? f.Palm : hand.transform.position;
                    float grip = GripAmount(hand);

                    float dist;
                    try { dist = Vector3.Distance(handPos, col != null ? col.ClosestPoint(handPos) : root.position); }
                    catch { dist = Vector3.Distance(handPos, root.position); }

                    if (dist > GrabRange)
                    {
                        if (grip >= GripOn) TryPull(phone, hand, dist);
                        continue;
                    }
                    if (grip < GripOn)
                    {
                        // рука рядом, а взять не выходит — покажем в логе реальную силу хвата
                        if (Time.time > _nextGripLog)
                        {
                            _nextGripLog = Time.time + 2f;
                            MelonLogger.Msg($"[LPhone] рядом (d={dist:0.000}) grip={grip:0.00} < {GripOn:0.00}");
                        }
                        continue;
                    }

                    // рука занята штатным предметом — не перехватываем
                    try { if (BoneLib.Player.GetObjectInHand(hand) != null) continue; } catch { }

                    var rb = phone.Root.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.useGravity = true;
                        rb.isKinematic = true;
                        rb.detectCollisions = false;
                    }

                    var ht = hand.transform;
                    Vector3 lp; Quaternion lr;
                    if (PoseInHand(hand, out var wpos, out var wrot))
                    {
                        lp = ht.InverseTransformPoint(wpos);
                        lr = Quaternion.Inverse(ht.rotation) * wrot;
                    }
                    else
                    {
                        lp = ht.InverseTransformPoint(root.position);
                        lr = Quaternion.Inverse(ht.rotation) * root.rotation;
                    }

                    _held[id] = new Held
                    {
                        Hand = hand,
                        LocalPos = lp,
                        LocalRot = lr,
                        StartLocalPos = ht.InverseTransformPoint(root.position),
                        StartLocalRot = Quaternion.Inverse(ht.rotation) * root.rotation,
                        Blend = 0f,
                        PrevPos = root.position,
                    };
                    MelonLogger.Msg("[LPhone] взят в руку");
                    return;
                }
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] захват: " + e.Message); }
        }

        /// <summary>Притягивание: наводишь ладонью с расстояния и сжимаешь кулак.</summary>
        private static void TryPull(PhoneInstance phone, Hand hand, float dist)
        {
            if (!PullEnabled || dist > PullRange) return;
            try
            {
                var f = HandFrame.Of(hand);
                Vector3 from = f.Valid ? f.Palm : hand.transform.position;
                Vector3 aim  = f.Valid ? f.Along : hand.transform.forward;

                var root = phone.Root.transform;
                Vector3 to = root.position - from;
                if (to.sqrMagnitude < 1e-6f) return;
                if (Vector3.Angle(aim, to.normalized) > PullAngle) return;

                var rb = phone.Root.GetComponent<Rigidbody>();
                if (rb == null || rb.isKinematic) return;

                rb.useGravity = false;
                Vector3 target = from + aim * 0.05f;
                Vector3 dir = target - root.position;
                rb.velocity = dir.normalized * Mathf.Min(PullSpeed, dir.magnitude * 6f + 1f);
                rb.angularVelocity *= 0.85f;
                _pullingUntil = Time.time + 0.15f;
            }
            catch { }
        }

        private static void HoldTick(PhoneInstance phone, Held h, int id)
        {
            try
            {
                if (h.Hand == null) { Release(phone, h, id); return; }
                var ht = h.Hand.transform;
                var root = phone.Root.transform;

                if (h.Blend < 1f)
                    h.Blend = Mathf.Clamp01(h.Blend + Time.deltaTime / BlendTime);
                float t = h.Blend * h.Blend * (3f - 2f * h.Blend);

                Vector3 lp = Vector3.Lerp(h.StartLocalPos, h.LocalPos, t);
                Quaternion lr = Quaternion.Slerp(h.StartLocalRot, h.LocalRot, t);

                Vector3 target = ht.TransformPoint(lp);
                float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                h.Velocity = (target - h.PrevPos) / dt;
                h.PrevPos = target;

                root.position = target;
                root.rotation = ht.rotation * lr;

                if (GripAmount(h.Hand) < GripOff) Release(phone, h, id);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] удержание: " + e.Message);
                Release(phone, h, id);
            }
        }

        private static void Release(PhoneInstance phone, Held h, int id)
        {
            _held.Remove(id);
            try
            {
                var rb = phone.Root != null ? phone.Root.GetComponent<Rigidbody>() : null;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.detectCollisions = true;
                    rb.useGravity = true;
                    var v = h.Velocity * ThrowScale;
                    if (v.magnitude > MaxThrow) v = v.normalized * MaxThrow;
                    rb.velocity = v;
                }
                MelonLogger.Msg("[LPhone] отпущен");
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] отпускание: " + e.Message); }
        }

        public static void Forget(PhoneInstance phone)
        {
            if (phone?.Root != null) _held.Remove(phone.Root.GetInstanceID());
        }
    }
}
