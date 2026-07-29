using System;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Тач по экрану кончиком указательного пальца.
    /// Позиция пальца переводится в локальные координаты экранного меша,
    /// затем в пиксели UI. Касание считается по «проколу» плоскости экрана.
    /// </summary>
    internal static class TouchInput
    {
        private const float TouchDepth  = 0.008f;   // насколько глубоко палец «продавил» экран
        private const float HoverDepth  = 0.045f;   // на каком расстоянии уже следим
        private const float FingerTipExt = 0.022f;  // от кости index3 до подушечки
        private static bool _down;

        public static void Tick(PhoneInstance phone)
        {
            if (phone == null || !phone.Alive || phone.ScreenTransform == null) return;

            if (!TryFingerTip(phone, out Vector3 tip))
            {
                if (_down) { _down = false; }
                return;
            }

            // локальные координаты относительно экранного меша
            Vector3 lp = phone.ScreenTransform.InverseTransformPoint(tip);

            // за плоскостью экрана? (Z вперёд)
            float depth = lp.z;
            bool inside = Mathf.Abs(lp.x) <= Phone.ScreenW * 0.5f &&
                          Mathf.Abs(lp.y) <= Phone.ScreenH * 0.5f;

            if (!inside || depth > HoverDepth)
            {
                if (_down) { phone.OS.TouchUp(ToPixels(lp)); _down = false; }
                return;
            }

            Vector2 px = ToPixels(lp);

            if (depth <= TouchDepth)
            {
                if (!_down) { _down = true; phone.OS.TouchDown(px); }
                else phone.OS.TouchMove(px);
            }
            else if (_down)
            {
                _down = false;
                phone.OS.TouchUp(px);
            }
        }

        /// <summary>Локальная точка меша → пиксели UI (0..W слева направо, 0..H сверху вниз).</summary>
        private static Vector2 ToPixels(Vector3 local)
        {
            float u = (local.x + Phone.ScreenW * 0.5f) / Phone.ScreenW;
            float v = (local.y + Phone.ScreenH * 0.5f) / Phone.ScreenH;
            return new Vector2(u * Phone.ScreenPxW, (1f - v) * Phone.ScreenPxH);
        }

        /// <summary>
        /// Кончик указательного пальца руки, которая БЛИЖЕ К ЭКРАНУ.
        /// Берём настоящие кости (index2 -> index3) и продлеваем на подушечку —
        /// это точнее, чем прикидывать смещение от ладони, и не зависит от того,
        /// как ориентированы оси кисти.
        /// </summary>
        private static bool TryFingerTip(PhoneInstance phone, out Vector3 tip)
        {
            tip = Vector3.zero;
            try
            {
                Vector3 screenPos = phone.ScreenTransform.position;
                float best = float.MaxValue;
                bool found = false;

                foreach (var h in new[] { BoneLib.Player.LeftHand, BoneLib.Player.RightHand })
                {
                    if (h == null) continue;
                    if (!TipOf(h, out Vector3 t)) continue;
                    float d = (t - screenPos).sqrMagnitude;
                    if (d < best) { best = d; tip = t; found = true; }
                }
                return found;
            }
            catch { return false; }
        }

        private static bool TipOf(Hand hand, out Vector3 tip)
        {
            tip = Vector3.zero;
            try
            {
                var anim = hand.Animator;
                if (anim != null)
                {
                    var i3 = anim.index3;
                    var i2 = anim.index2;
                    if (i3 != null)
                    {
                        Vector3 dir = (i2 != null)
                            ? (i3.position - i2.position)
                            : hand.transform.forward;
                        if (dir.sqrMagnitude < 1e-8f) dir = hand.transform.forward;
                        tip = i3.position + dir.normalized * FingerTipExt;
                        return true;
                    }
                }
                // запас, если анимации пальцев нет
                tip = hand.transform.position + hand.transform.forward * 0.075f;
                return true;
            }
            catch { return false; }
        }
    }
}
