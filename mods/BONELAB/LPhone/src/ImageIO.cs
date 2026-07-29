using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace LPhone
{
    /// <summary>PNG/JPEG в обе стороны. Скретч-текстуру переиспользуем — она
    /// одна на весь мод, иначе каждый принятый кадр видеозвонка течёт в память.</summary>
    internal static class Jpeg
    {
        private static Texture2D _scratch;

        private static Texture2D Scratch()
        {
            if (_scratch == null)
                _scratch = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave };
            return _scratch;
        }

        public static TexData Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                var t = Scratch();
                var arr = new Il2CppStructArray<byte>(bytes);
                if (!ImageConversion.LoadImage(t, arr, false)) return null;
                return TexData.FromTexture(t);
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Warning("[LPhone] декод: " + e.Message);
                return null;
            }
        }

        public static byte[] EncodePng(Texture2D t)
        {
            try { return ToManaged(ImageConversion.EncodeToPNG(t)); }
            catch (Exception e) { MelonLoader.MelonLogger.Warning("[LPhone] PNG: " + e.Message); return null; }
        }

        public static byte[] EncodeJpg(Texture2D t, int quality)
        {
            try { return ToManaged(ImageConversion.EncodeToJPG(t, quality)); }
            catch (Exception e) { MelonLoader.MelonLogger.Warning("[LPhone] JPG: " + e.Message); return null; }
        }

        private static byte[] ToManaged(Il2CppStructArray<byte> a)
        {
            if (a == null) return null;
            var r = new byte[a.Length];
            a.AsSpan().CopyTo(r.AsSpan());
            return r;
        }
    }
}
