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
        private const float TouchDepth  = 0.006f;   // насколько глубоко палец «продавил» экран
        private const float HoverDepth  = 0.030f;   // на каком расстоянии уже следим
        private static bool _down;

        public static void Tick(PhoneInstance phone)
        {
            if (phone == null || !phone.Alive || phone.ScreenTransform == null) return;

            if (!TryFingerTip(out Vector3 tip))
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

        /// <summary>Кончик указательного пальца ближайшей руки.</summary>
        private static bool TryFingerTip(out Vector3 tip)
        {
            tip = Vector3.zero;
            try
            {
                var rig = BoneLib.Player.RigManager;
                if (rig == null) return false;

                Hand best = null;
                float bestD = float.MaxValue;
                foreach (var h in new[] { BoneLib.Player.LeftHand, BoneLib.Player.RightHand })
                {
                    if (h == null) continue;
                    float d = h.transform.position.sqrMagnitude;
                    if (d < bestD) { bestD = d; best = h; }
                }
                if (best == null) return false;

                // указательный: смещение вперёд от ладони
                tip = best.transform.position
                    + best.transform.forward * 0.075f
                    + best.transform.up * 0.005f;
                return true;
            }
            catch { return false; }
        }
    }
}
