using System;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Interaction;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Система координат ЛАДОНИ, построенная по реальным костям пальцев.
    ///
    /// Зачем: у Marrow неизвестно, куда смотрят оси hand.transform, а
    /// hand.transform.position — это запястье. Если класть телефон от него,
    /// корпус длиной 16 см наполовину уходит внутрь кисти.
    ///
    /// Базис:
    ///   Along  — от запястья к основаниям пальцев
    ///   Across — от указательного к мизинцу
    ///   Normal — из ЛАДОНИ наружу (не с тыльной стороны)
    ///
    /// Сторону ладони определяем по hand.handedness, а не по позам пальцев.
    /// Через кости это гадание: у распрямлённой кисти вектор загиба почти
    /// нулевой, знак скалярного произведения случайный — и телефон оказывался
    /// на ТЫЛЬНОЙ стороне. Через сторону руки знак задан геометрией и не
    /// зависит ни от анимации, ни от того, сжат кулак или нет.
    /// </summary>
    internal struct HandFrame
    {
        public bool Valid;
        public Vector3 Palm;      // центр ладони
        public Vector3 Along;     // к пальцам
        public Vector3 Across;    // указательный -> мизинец
        public Vector3 Normal;    // из ладони наружу

        public static HandFrame Of(Hand hand)
        {
            var f = new HandFrame();
            if (hand == null) return f;
            try
            {
                var ht = hand.transform;
                var anim = hand.Animator;
                if (anim == null) return Fallback(ht, hand);

                var idx = anim.index1;
                var pky = anim.pinky1;
                if (idx == null || pky == null) return Fallback(ht, hand);

                Vector3 wrist = ht.position;
                Vector3 iBase = idx.position;
                Vector3 pBase = pky.position;
                Vector3 fingers = (iBase + pBase) * 0.5f;

                Vector3 along = fingers - wrist;
                Vector3 across = pBase - iBase;
                if (along.sqrMagnitude < 1e-8f || across.sqrMagnitude < 1e-8f)
                    return Fallback(ht, hand);

                along.Normalize();
                across.Normalize();

                // Знак нормали. Большой палец всегда снаружи от тела: у правой
                // руки он со стороны, противоположной мизинцу справа, у левой —
                // слева. Значит across (указательный->мизинец) у правой руки
                // смотрит в -X, у левой в +X, если держать ладонь от себя
                // пальцами вверх. Отсюда: правая = +Cross(along, across),
                // левая = -Cross. В прошлой версии знак был перевёрнут, и
                // телефон ложился экраном ВНУТРЬ ладони — пальцы закрывали экран.
                Vector3 normal = Vector3.Cross(along, across).normalized;
                if (!IsRight(hand)) normal = -normal;

                // ортогонализация, чтобы базис был чистым
                across = Vector3.Cross(normal, along).normalized;

                f.Valid = true;
                f.Palm = Vector3.Lerp(wrist, fingers, 0.55f);
                f.Along = along;
                f.Across = across;
                f.Normal = normal;
                return f;
            }
            catch { return Fallback(hand.transform, hand); }
        }

        /// <summary>Правая ли это кисть. UNDEFINED считаем правой — ошибка обратима тумблером.</summary>
        public static bool IsRight(Hand hand)
        {
            try
            {
                if (hand.handedness == Handedness.LEFT) return false;
                if (hand.handedness == Handedness.RIGHT) return true;
            }
            catch { }
            // запасной путь: сверяемся со ссылкой BoneLib
            try { return hand == BoneLib.Player.RightHand; } catch { }
            return true;
        }

        private static HandFrame Fallback(Transform ht, Hand hand)
        {
            var f = new HandFrame();
            if (ht == null) return f;
            f.Valid = true;
            f.Palm = ht.position;
            f.Along = ht.forward;
            f.Across = ht.right;
            f.Normal = IsRight(hand) ? ht.up : -ht.up;
            return f;
        }
    }
}
