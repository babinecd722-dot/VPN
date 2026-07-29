using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Тач по экрану. Работает ЛЮБЫМ пальцем обеих рук — как на настоящем
    /// телефоне: касание регистрирует тот палец, который реально продавил
    /// стекло глубже всех и попал в границы экрана.
    ///
    /// Рука, которая держит телефон, из опроса исключается, иначе собственные
    /// пальцы хвата постоянно «нажимали» бы на экран.
    /// </summary>
    internal static class TouchInput
    {
        // Всё считается от плоскости стекла (см. PhoneInstance.ScreenCenter).
        private const float TouchDepth = 0.006f;    // палец коснулся стекла
        private const float HoverDepth = 0.045f;    // ближе этого уже следим
        private const float ThroughDepth = -0.030f; // пролетел насквозь — не касание
        private const float TipExt = 0.014f;       // от последней фаланги до подушечки
        private const float EdgePad = 0.004f;      // допуск за краем экрана

        private sealed class State
        {
            public bool Down;
            public Vector2 Last;
            public float DownTime;
            public bool LongFired;
        }

        private static readonly Dictionary<int, State> _st = new Dictionary<int, State>();

        public static void Forget(PhoneInstance p)
        {
            if (p?.Root != null) _st.Remove(p.Root.GetInstanceID());
        }

        public static void Tick(PhoneInstance phone)
        {
            if (phone == null || !phone.Alive || phone.ScreenTransform == null) return;
            int id = phone.Root.GetInstanceID();
            if (!_st.TryGetValue(id, out var s)) { s = new State(); _st[id] = s; }

            var holder = PhoneGrab.HandOf(phone);

            // Ищем палец, который ближе всех к плоскости экрана и попадает в его габарит.
            bool found = false;
            Vector3 bestLocal = Vector3.zero;
            float bestDepth = float.MaxValue;

            for (int hi = 0; hi < 2; hi++)
            {
                var hand = hi == 0 ? BoneLib.Player.LeftHand : BoneLib.Player.RightHand;
                if (hand == null || hand == holder) continue;
                int cnt = FillTips(hand);
                for (int k = 0; k < cnt; k++)
                {
                    // Вершины экрана запечены со смещением, поэтому и границы,
                    // и глубину считаем от фактического центра плоскости.
                    Vector3 lp = phone.ScreenTransform.InverseTransformPoint(_tips[k])
                                 - phone.ScreenCenter;
                    if (Mathf.Abs(lp.x) > Phone.ScreenW * 0.5f + EdgePad) continue;
                    if (Mathf.Abs(lp.y) > Phone.ScreenH * 0.5f + EdgePad) continue;
                    if (lp.z > HoverDepth || lp.z < ThroughDepth) continue;
                    if (lp.z < bestDepth) { bestDepth = lp.z; bestLocal = lp; found = true; }
                }
            }

            if (!found)
            {
                if (s.Down) { s.Down = false; phone.OS.TouchUp(s.Last); }
                return;
            }

            Vector2 px = ToPixels(bestLocal);
            s.Last = px;

            if (bestDepth <= TouchDepth)
            {
                if (!s.Down)
                {
                    s.Down = true;
                    s.DownTime = Time.unscaledTime;
                    s.LongFired = false;
                    phone.OS.TouchDown(px);
                }
                else
                {
                    phone.OS.TouchMove(px);
                    if (!s.LongFired && Time.unscaledTime - s.DownTime > 0.55f)
                    {
                        s.LongFired = true;
                        phone.OS.LongPress(px);
                    }
                }
            }
            else if (s.Down)
            {
                s.Down = false;
                phone.OS.TouchUp(px);
            }
        }

        /// <summary>
        /// Локальная точка меша → пиксели UI (0..W слева направо, 0..H сверху вниз).
        ///
        /// Игрок смотрит на экран со стороны +Z, поэтому его «вправо» — это
        /// УМЕНЬШЕНИЕ локального x, а «вниз» — уменьшение локального y.
        /// Раньше x не переворачивался, и касания уезжали по горизонтали
        /// на зеркальную сторону экрана.
        /// </summary>
        private static Vector2 ToPixels(Vector3 local)
        {
            float rx = (Phone.ScreenW * 0.5f - local.x) / Phone.ScreenW;
            float dy = (Phone.ScreenH * 0.5f - local.y) / Phone.ScreenH;
            return new Vector2(Mathf.Clamp01(rx) * Phone.ScreenPxW,
                               Mathf.Clamp01(dy) * Phone.ScreenPxH);
        }

        // Переиспользуемый буфер: не мусорим массивом на каждый кадр.
        private static readonly Vector3[] _tips = new Vector3[5];

        /// <summary>Складывает подушечки всех пяти пальцев в _tips, возвращает их число.</summary>
        private static int FillTips(Hand hand)
        {
            int n = 0;
            try
            {
                var anim = hand.Animator;
                if (anim != null)
                {
                    if (Tip(anim.index2,  anim.index3,  hand, out _tips[n])) n++;
                    if (Tip(anim.middle2, anim.middle3, hand, out _tips[n])) n++;
                    if (Tip(anim.thumb2,  anim.thumb3,  hand, out _tips[n])) n++;
                    if (Tip(anim.ring2,   anim.ring3,   hand, out _tips[n])) n++;
                    if (Tip(anim.pinky2,  anim.pinky3,  hand, out _tips[n])) n++;
                }
                if (n == 0)
                {
                    var ht = hand.transform;
                    _tips[0] = ht.position + ht.forward * 0.075f;
                    n = 1;
                }
            }
            catch { return 0; }
            return n;
        }

        private static bool Tip(Transform mid, Transform last, Hand hand, out Vector3 outTip)
        {
            outTip = Vector3.zero;
            if (last == null) return false;
            Vector3 dir = (mid != null) ? (last.position - mid.position) : hand.transform.forward;
            if (dir.sqrMagnitude < 1e-8f) dir = hand.transform.forward;
            outTip = last.position + dir.normalized * TipExt;
            return true;
        }
    }
}
