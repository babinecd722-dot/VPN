using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Хват телефона «как в жизни» — своя реализация, без компонентов SLZ.
    ///
    /// Почему не BoxGrip/InteractableHost: их AddComponent в рантайме роняет Il2Cpp
    /// НАСМЕРТЬ (подтверждено на устройстве — краш ровно на «ставлю BoxGrip»),
    /// т.к. их Awake() рассчитывает на настройку из SDK.
    ///
    /// Как достигается правильная поза, не зная соглашения об осях руки в Marrow:
    /// в момент захвата считаем ЖЕЛАЕМУЮ ориентацию в МИРОВЫХ координатах
    /// (экран смотрит в лицо игроку, верх телефона — вверх), а потом переводим её
    /// в локальную систему руки. Дальше телефон живёт в руке жёстко, как настоящий.
    /// Пальцы держат нижнюю треть корпуса, телефон торчит из кулака вверх.
    /// </summary>
    internal static class PhoneGrab
    {
        public static bool Enabled = true;

        // Подгонка позы (доступна ползунками в меню — довести под себя за полминуты).
        public static float HoldUp      = 0.030f;   // насколько телефон выше кисти
        public static float HoldForward = 0.020f;   // вынос вперёд от кулака
        public static float HoldTilt    = 12f;      // наклон верха от себя, градусы

        private const float GrabRange  = 0.13f;     // от ПОВЕРХНОСТИ корпуса, а не от центра
        // Притягивание: наводишь раскрытой рукой и сжимаешь кулак
        public static bool  PullEnabled = true;
        private const float PullRange   = 6f;
        private const float PullAngle   = 30f;      // конус наведения, градусы
        private const float PullSpeed   = 7f;
        private const float GripOn     = 0.55f;     // порог сжатия
        private const float GripOff    = 0.35f;     // гистерезис отпускания
        private const float BlendTime  = 0.12f;     // плавная доводка в позу
        private const float ThrowScale = 1.15f;
        private const float MaxThrow   = 9f;

        private sealed class Held
        {
            public Hand Hand;
            public Vector3 LocalPos;         // целевая поза в системе руки
            public Quaternion LocalRot;
            public Vector3 StartLocalPos;    // поза в момент захвата (для плавности)
            public Quaternion StartLocalRot;
            public float Blend;
            public Vector3 PrevPos;
            public Vector3 Velocity;
        }

        private static readonly Dictionary<int, Held> _held = new Dictionary<int, Held>();

        public static bool IsHeld(PhoneInstance p) =>
            p?.Root != null && _held.ContainsKey(p.Root.GetInstanceID());

        public static void Tick(PhoneInstance phone)
        {
            if (!Enabled || phone == null || !phone.Alive) return;
            int id = phone.Root.GetInstanceID();
            if (_held.TryGetValue(id, out var h)) { HoldTick(phone, h, id); return; }

            TryGrab(phone, id);

            // притягивание закончилось — возвращаем гравитацию
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
                float g = c._gripForce;
                if (g <= 0f) g = c._solvedGrip;
                return Mathf.Clamp01(g);
            }
            catch { return 0f; }
        }

        /// <summary>
        /// Идеальная мировая поза телефона в этой руке:
        /// экран (+Z модели) — в лицо игроку, верх телефона (+Y) — вверх,
        /// корпус приподнят над кистью и слегка наклонён.
        /// </summary>
        private static bool IdealWorldPose(Hand hand, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            var ht = hand.transform;
            var head = BoneLib.Player.Head;
            if (head == null) return false;

            Vector3 toHead = head.position - ht.position;
            if (toHead.sqrMagnitude < 1e-6f) return false;
            toHead.Normalize();

            // +Z смотрит на голову => экран обращён к игроку
            rot = Quaternion.LookRotation(toHead, Vector3.up);
            // наклон верха «от себя», как держат телефон в жизни
            rot = rot * Quaternion.Euler(HoldTilt, 0f, 0f);

            Vector3 up = rot * Vector3.up;
            pos = ht.position + up * HoldUp + toHead * HoldForward;
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
                    try { if (BoneLib.Player.GetObjectInHand(hand) != null) continue; } catch { }

                    var ht = hand.transform;
                    float grip = GripAmount(hand);

                    // расстояние до ПОВЕРХНОСТИ телефона — брать можно за любой край
                    float dist;
                    try { dist = Vector3.Distance(ht.position, col != null ? col.ClosestPoint(ht.position) : root.position); }
                    catch { dist = Vector3.Distance(ht.position, root.position); }

                    if (dist > GrabRange)
                    {
                        if (grip >= GripOn) TryPull(phone, hand, dist);
                        continue;
                    }
                    if (grip < GripOn) continue;

                    var rb = phone.Root.GetComponent<Rigidbody>();
                    if (rb != null) { rb.useGravity = true; rb.isKinematic = true; rb.detectCollisions = false; }

                    // целевая поза
                    Vector3 lp; Quaternion lr;
                    if (IdealWorldPose(hand, out var wpos, out var wrot))
                    {
                        lp = ht.InverseTransformPoint(wpos);
                        lr = Quaternion.Inverse(ht.rotation) * wrot;
                    }
                    else
                    {   // нет головы — держим как взяли
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
        /// Притягивание: раскрытой рукой наводишься на телефон и сжимаешь кулак —
        /// он летит в ладонь. Это наш аналог force pull (штатный ForcePullGrip от SLZ
        /// нам недоступен: его компонент нельзя добавить в рантайме).
        /// </summary>
        private static void TryPull(PhoneInstance phone, Hand hand, float dist)
        {
            if (!PullEnabled || dist > PullRange) return;
            try
            {
                var ht = hand.transform;
                var root = phone.Root.transform;
                Vector3 to = root.position - ht.position;
                if (to.sqrMagnitude < 1e-6f) return;

                // наводимся ладонью: телефон должен быть в конусе перед рукой
                if (Vector3.Angle(ht.forward, to.normalized) > PullAngle) return;

                var rb = phone.Root.GetComponent<Rigidbody>();
                if (rb == null || rb.isKinematic) return;

                rb.useGravity = false;
                Vector3 target = ht.position + ht.forward * 0.05f;
                Vector3 dir = (target - root.position);
                rb.velocity = dir.normalized * Mathf.Min(PullSpeed, dir.magnitude * 6f + 1f);
                rb.angularVelocity *= 0.85f;
                _pullingUntil = Time.time + 0.15f;
            }
            catch { }
        }

        private static float _pullingUntil;

        private static void HoldTick(PhoneInstance phone, Held h, int id)
        {
            try
            {
                if (h.Hand == null) { Release(phone, h, id); return; }
                var ht = h.Hand.transform;
                var root = phone.Root.transform;

                // плавная доводка от «как схватил» к правильной позе
                if (h.Blend < 1f)
                    h.Blend = Mathf.Clamp01(h.Blend + Time.deltaTime / BlendTime);
                float t = h.Blend * h.Blend * (3f - 2f * h.Blend);      // smoothstep

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
                    var v = h.Velocity * ThrowScale;                    // отпустил — падает/летит
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
