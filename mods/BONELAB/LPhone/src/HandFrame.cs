using System;
using Il2CppSLZ.Marrow;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Система координат ЛАДОНИ, построенная по реальным костям пальцев.
    ///
    /// Зачем: у Marrow неизвестно, куда смотрят оси hand.transform, а
    /// hand.transform.position — это запястье. Если класть телефон от него,
    /// корпус длиной 16 см наполовину уходит внутрь кисти (что и было).
    ///
    /// По костям строим честный базис:
    ///   Along  — от запястья к основаниям пальцев (куда «смотрит» ладонь)
    ///   Across — от указательного к мизинцу (поперёк ладони)
    ///   Normal — нормаль ладони (наружу от тыльной стороны)
    /// Этого достаточно, чтобы положить предмет ровно в захват.
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
                if (anim == null) return Fallback(ht);

                var idx = anim.index1;
                var pky = anim.pinky1;
                if (idx == null || pky == null) return Fallback(ht);

                Vector3 wrist = ht.position;
                Vector3 iBase = idx.position;
                Vector3 pBase = pky.position;
                Vector3 fingers = (iBase + pBase) * 0.5f;

                Vector3 along = fingers - wrist;
                Vector3 across = pBase - iBase;
                if (along.sqrMagnitude < 1e-8f || across.sqrMagnitude < 1e-8f)
                    return Fallback(ht);

                along.Normalize();
                across.Normalize();

                Vector3 normal = Vector3.Cross(along, across).normalized;

                // Куда смотрит ладонь? Надёжный признак — сторона, в которую
                // ЗАГИБАЮТСЯ пальцы. Проверять по основанию большого пальца
                // нельзя: оно лежит почти в плоскости ладони, и знак скалярного
                // произведения там случайный.
                Vector3 curl = Vector3.zero;
                if (anim.middle3 != null && anim.middle1 != null)
                    curl = anim.middle3.position - anim.middle1.position;
                else if (anim.index3 != null)
                    curl = anim.index3.position - iBase;
                curl -= along * Vector3.Dot(curl, along);     // только поперечная часть

                if (curl.sqrMagnitude > 1e-7f)
                {
                    if (Vector3.Dot(normal, curl) < 0f) normal = -normal;
                }
                else
                {
                    // рука распрямлена — тогда хотя бы по кончику большого пальца
                    var thumbTip = anim.thumb3 != null ? anim.thumb3 : anim.thumb1;
                    if (thumbTip != null)
                    {
                        Vector3 toThumb = thumbTip.position - wrist;
                        toThumb -= along * Vector3.Dot(toThumb, along);
                        if (toThumb.sqrMagnitude > 1e-7f && Vector3.Dot(normal, toThumb) < 0f)
                            normal = -normal;
                    }
                }

                // ортогонализация, чтобы базис был чистым
                across = Vector3.Cross(normal, along).normalized;

                f.Valid = true;
                f.Palm = Vector3.Lerp(wrist, fingers, 0.55f);
                f.Along = along;
                f.Across = across;
                f.Normal = normal;
                return f;
            }
            catch { return Fallback(hand.transform); }
        }

        private static HandFrame Fallback(Transform ht)
        {
            var f = new HandFrame();
            if (ht == null) return f;
            f.Valid = true;
            f.Palm = ht.position;
            f.Along = ht.forward;
            f.Across = ht.right;
            f.Normal = ht.up;
            return f;
        }
    }
}
