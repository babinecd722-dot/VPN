using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Хват телефона.
    ///
    /// Вход берём ШТАТНЫЙ — Controller.isGrabInputPressedFinal, тот же сигнал,
    /// которым игра берёт любой предмет. Самодельный порог по силе сжатия
    /// срабатывал от чего угодно, вплоть до нажатия стика, и телефон летел
    /// в руку сам собой.
    ///
    /// Взять можно только с касания (5.5 см от корпуса) — как обычный предмет.
    /// Притягивание отдельным жестом: навести ладонью и НАЖАТЬ хват; конус
    /// узкий, импульс один на нажатие, а не постоянная тяга.
    ///
    /// Поза строится по системе координат ЛАДОНИ (HandFrame), а не от
    /// hand.transform.position: там начало трансформа — запястье, и корпус
    /// длиной 16 см наполовину уходил внутрь кисти.
    /// </summary>
    internal static class PhoneGrab
    {
        public static bool Enabled = true;
        public static bool PullEnabled = true;
        /// <summary>Требовать вдобавок нажатый триггер (как просят «две кнопки»).</summary>
        public static bool RequireTrigger = false;

        // Подгонка позы ползунками в меню.
        public static float HoldOut  = 0.012f;   // от ладони наружу (полтолщины + зазор)
        public static float HoldUp   = 0.045f;   // вдоль пальцев: ладонь держит нижнюю треть
        public static float HoldSide = 0.0f;     // поперёк ладони
        public static float HoldTilt = 0f;       // доворот вокруг оси пальцев
        /// <summary>Аварийный переворот стороны ладони, если знак всё же не тот.</summary>
        public static bool FlipPalm = false;

        // Берём с касания, как любой предмет игры: двадцать сантиметров — это
        // не «взял», это «телефон прыгнул в руку сам».
        private const float GrabRange = 0.055f;
        private const float BlendTime = 0.08f;

        // Притягивание — по НАЖАТИЮ хвата с наведения, а не пока сжат кулак.
        private const float PullRange = 4f;
        private const float PullAngle = 14f;
        private const float PullSpeed = 9f;

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

        // Фронт нажатия хвата, по одному разу за кадр на каждую руку.
        private static bool _grabR, _grabL, _prevR, _prevL;
        private static int _inputFrame = -1;

        /// <summary>
        /// Опрос хвата один раз за кадр. Берём ШТАТНЫЙ сигнал SLZ
        /// (isGrabInputPressedFinal) — тот же, которым игра берёт все предметы,
        /// а не самодельный порог по силе сжатия: он срабатывал от чего угодно,
        /// вплоть до нажатия стика.
        /// </summary>
        public static void PollInput()
        {
            if (_inputFrame == Time.frameCount) return;
            _inputFrame = Time.frameCount;
            _prevR = _grabR; _prevL = _grabL;
            _grabR = Pressed(BoneLib.Player.RightHand);
            _grabL = Pressed(BoneLib.Player.LeftHand);
        }

        private static bool Pressed(Hand hand)
        {
            if (hand == null) return false;
            try
            {
                var c = hand.Controller;
                if (c == null) return false;
                bool grab = c.isGrabInputPressedFinal;
                if (RequireTrigger) grab = grab && c._primaryInteractionButton;
                return grab;
            }
            catch { return false; }
        }

        private static bool IsRightHand(Hand h)
        {
            try { return h == BoneLib.Player.RightHand; } catch { return true; }
        }

        /// <summary>Хват зажат сейчас.</summary>
        private static bool GrabHeld(Hand h) => IsRightHand(h) ? _grabR : _grabL;

        /// <summary>Хват нажали именно в этом кадре.</summary>
        private static bool GrabDown(Hand h) =>
            IsRightHand(h) ? (_grabR && !_prevR) : (_grabL && !_prevL);

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

                    float dist;
                    try { dist = Vector3.Distance(handPos, col != null ? col.ClosestPoint(handPos) : root.position); }
                    catch { dist = Vector3.Distance(handPos, root.position); }

                    if (dist > GrabRange)
                    {
                        // издалека — только по свежему нажатию и точному наведению
                        if (GrabDown(hand)) TryPull(phone, hand, dist);
                        continue;
                    }
                    if (!GrabHeld(hand)) continue;

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

        /// <summary>
        /// Притягивание в духе штатного: навести ладонью и НАЖАТЬ хват.
        /// Конус узкий (14 градусов) и импульс даётся один раз на нажатие —
        /// иначе телефон летел в руку от любого сжатия кулака в комнате.
        /// </summary>
        private static void TryPull(PhoneInstance phone, Hand hand, float dist)
        {
            if (!PullEnabled || dist > PullRange) return;
            try
            {
                var f = HandFrame.Of(hand);
                Vector3 from = f.Valid ? f.Palm : hand.transform.position;
                // целимся ладонью: предмет должен быть перед раскрытой ладонью
                Vector3 aim  = f.Valid ? f.Normal : hand.transform.forward;

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

                if (!GrabHeld(h.Hand)) Release(phone, h, id);
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
