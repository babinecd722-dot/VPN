using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Софтверный 2D-рендерер экрана телефона.
    /// Рисуем в свой буфер Color32[] и заливаем в Texture2D одним Apply().
    /// Почему не Unity Canvas: в Il2Cpp он капризен, а на Quest перестройка Canvas
    /// дороже, чем залив текстуры, который мы делаем только при изменениях.
    /// </summary>
    internal sealed class Gfx
    {
        public readonly int W, H;
        private readonly Color32[] _buf;
        private readonly Texture2D _tex;
        private bool _firstPresentLogged, _firstApplyLogged;
        private bool _dirty = true;

        public Texture2D Texture => _tex;
        public void MarkDirty() => _dirty = true;

        public Gfx(int w, int h)
        {
            W = w; H = h;
            _buf = new Color32[w * h];
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        /// <summary>Заливаем накопленный кадр в текстуру (только если что-то менялось).</summary>
        public void Present()
        {
            if (!_dirty) return;
            _dirty = false;
            // Массовый конструктор из managed-массива — самый безопасный путь в Il2Cpp
            // (span поверх временного Il2Cpp-массива рискует «уехать» из-под GC).
            if (!_firstPresentLogged)
            {
                _firstPresentLogged = true;
                MelonLoader.MelonLogger.Msg($"[LPhone] ... первый Present {W}x{H}");
            }
            var gpu = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32>(_buf);
            _tex.SetPixels32(gpu);
            _tex.Apply(false);
            if (!_firstApplyLogged)
            {
                _firstApplyLogged = true;
                MelonLoader.MelonLogger.Msg("[LPhone] ... первый Apply ок");
            }
        }

        // ─────────────────── примитивы ───────────────────

        public void Clear(Color32 c)
        {
            for (int i = 0; i < _buf.Length; i++) _buf[i] = c;
            _dirty = true;
        }

        private static Color32 Blend(Color32 dst, Color32 src, float a)
        {
            if (a <= 0f) return dst;
            if (a >= 1f) return src;
            return new Color32(
                (byte)(dst.r + (src.r - dst.r) * a),
                (byte)(dst.g + (src.g - dst.g) * a),
                (byte)(dst.b + (src.b - dst.b) * a),
                255);
        }

        public void Px(int x, int y, Color32 c, float a = 1f)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            int i = y * W + x;
            _buf[i] = Blend(_buf[i], c, a * (c.a / 255f));
        }

        public void Rect(int x, int y, int w, int h, Color32 c, float alpha = 1f)
        {
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            for (int yy = y1; yy < y2; yy++)
                for (int xx = x1; xx < x2; xx++)
                {
                    int i = yy * W + xx;
                    _buf[i] = Blend(_buf[i], c, alpha * (c.a / 255f));
                }
            _dirty = true;
        }

        /// <summary>Скруглённый прямоугольник со сглаженным краем.</summary>
        public void RoundRect(int x, int y, int w, int h, int r, Color32 c, float alpha = 1f)
        {
            r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            for (int yy = y1; yy < y2; yy++)
            {
                for (int xx = x1; xx < x2; xx++)
                {
                    float a = 1f;
                    if (r > 0)
                    {
                        float dx = 0f, dy = 0f;
                        if (xx < x + r) dx = (x + r) - xx - 0.5f;
                        else if (xx > x + w - r - 1) dx = xx - (x + w - r - 1) + 0.5f;
                        if (yy < y + r) dy = (y + r) - yy - 0.5f;
                        else if (yy > y + h - r - 1) dy = yy - (y + h - r - 1) + 0.5f;
                        if (dx > 0f && dy > 0f)
                        {
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            a = Mathf.Clamp01(r - d + 0.5f);
                            if (a <= 0f) continue;
                        }
                    }
                    int i = yy * W + xx;
                    _buf[i] = Blend(_buf[i], c, a * alpha * (c.a / 255f));
                }
            }
            _dirty = true;
        }

        public void Circle(int cx, int cy, int r, Color32 c, float alpha = 1f)
            => RoundRect(cx - r, cy - r, r * 2, r * 2, r, c, alpha);

        /// <summary>Вертикальный градиент.</summary>
        public void VGradient(int x, int y, int w, int h, Color32 top, Color32 bottom)
        {
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            for (int yy = y1; yy < y2; yy++)
            {
                float t = h <= 1 ? 0f : (float)(yy - y) / (h - 1);
                var c = new Color32(
                    (byte)(top.r + (bottom.r - top.r) * t),
                    (byte)(top.g + (bottom.g - top.g) * t),
                    (byte)(top.b + (bottom.b - top.b) * t),
                    (byte)(top.a + (bottom.a - top.a) * t));
                for (int xx = x1; xx < x2; xx++)
                {
                    int i = yy * W + xx;
                    _buf[i] = Blend(_buf[i], c, c.a / 255f);
                }
            }
            _dirty = true;
        }

        // ─────────────────── картинки ───────────────────

        /// <summary>Рисуем RGBA-картинку с масштабированием (nearest + альфа).</summary>
        public void Blit(TexData img, int x, int y, int w, int h, float alpha = 1f)
        {
            if (img == null) return;
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            for (int yy = y1; yy < y2; yy++)
            {
                int sy = (int)((long)(yy - y) * img.H / h);
                if (sy < 0 || sy >= img.H) continue;
                for (int xx = x1; xx < x2; xx++)
                {
                    int sx = (int)((long)(xx - x) * img.W / w);
                    if (sx < 0 || sx >= img.W) continue;
                    var c = img.Pixels[sy * img.W + sx];
                    if (c.a == 0) continue;
                    int i = yy * W + xx;
                    _buf[i] = Blend(_buf[i], c, alpha * (c.a / 255f));
                }
            }
            _dirty = true;
        }

        /// <summary>Картинка, обрезанная по скруглённому прямоугольнику (для иконок/аватарок).</summary>
        public void BlitRounded(TexData img, int x, int y, int w, int h, int r, float alpha = 1f)
        {
            if (img == null) return;
            r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            for (int yy = y1; yy < y2; yy++)
            {
                int sy = (int)((long)(yy - y) * img.H / h);
                if (sy < 0 || sy >= img.H) continue;
                for (int xx = x1; xx < x2; xx++)
                {
                    float m = 1f;
                    float dx = 0f, dy = 0f;
                    if (xx < x + r) dx = (x + r) - xx - 0.5f;
                    else if (xx > x + w - r - 1) dx = xx - (x + w - r - 1) + 0.5f;
                    if (yy < y + r) dy = (y + r) - yy - 0.5f;
                    else if (yy > y + h - r - 1) dy = yy - (y + h - r - 1) + 0.5f;
                    if (dx > 0f && dy > 0f)
                    {
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        m = Mathf.Clamp01(r - d + 0.5f);
                        if (m <= 0f) continue;
                    }
                    int sx = (int)((long)(xx - x) * img.W / w);
                    if (sx < 0 || sx >= img.W) continue;
                    var c = img.Pixels[sy * img.W + sx];
                    if (c.a == 0) continue;
                    int i = yy * W + xx;
                    _buf[i] = Blend(_buf[i], c, m * alpha * (c.a / 255f));
                }
            }
            _dirty = true;
        }

        // ─────────────────── текст ───────────────────

        public enum Align { Left, Center, Right }

        public int TextWidth(string s, float scale)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            float w = 0f;
            foreach (char ch in s)
                if (Font.Glyphs.TryGetValue(ch, out var g)) w += g.Advance * scale;
            return Mathf.RoundToInt(w);
        }

        public int LineHeight(float scale) => Mathf.RoundToInt(Font.LineHeight * scale);

        /// <summary>Рисуем строку. y — верх строки.</summary>
        public void Text(string s, int x, int y, float scale, Color32 col,
                         Align align = Align.Left, float alpha = 1f)
        {
            if (string.IsNullOrEmpty(s) || Font.Atlas == null) return;
            if (align == Align.Center) x -= TextWidth(s, scale) / 2;
            else if (align == Align.Right) x -= TextWidth(s, scale);

            float pen = x;
            int baseline = Mathf.RoundToInt(Font.Baseline * scale);
            var atlas = Font.Atlas;

            foreach (char ch in s)
            {
                if (!Font.Glyphs.TryGetValue(ch, out var g)) continue;
                if (g.W > 0 && g.H > 0)
                {
                    int dx0 = Mathf.RoundToInt(pen + g.BearingX * scale);
                    int dy0 = y + baseline + Mathf.RoundToInt(g.BearingY * scale);
                    int dw = Mathf.Max(1, Mathf.RoundToInt(g.W * scale));
                    int dh = Mathf.Max(1, Mathf.RoundToInt(g.H * scale));

                    for (int yy = 0; yy < dh; yy++)
                    {
                        int py = dy0 + yy;
                        if (py < 0 || py >= H) continue;
                        int sy = g.Y + (int)((long)yy * g.H / dh);
                        for (int xx = 0; xx < dw; xx++)
                        {
                            int px = dx0 + xx;
                            if (px < 0 || px >= W) continue;
                            int sx = g.X + (int)((long)xx * g.W / dw);
                            byte a = atlas.Pixels[sy * atlas.W + sx].a;
                            if (a == 0) continue;
                            int i = py * W + px;
                            _buf[i] = Blend(_buf[i], col, (a / 255f) * alpha);
                        }
                    }
                }
                pen += g.Advance * scale;
            }
            _dirty = true;
        }
    }

    /// <summary>Простой RGBA-буфер картинки (распакованный PNG).</summary>
    internal sealed class TexData
    {
        public int W, H;
        public Color32[] Pixels;

        public static TexData FromTexture(Texture2D t)
        {
            if (t == null) return null;
            var raw = t.GetPixels32();          // держим ссылку живой, пока работаем со span
            var src = raw.AsSpan();
            var d = new TexData { W = t.width, H = t.height, Pixels = new Color32[src.Length] };
            // Unity отдаёт пиксели снизу вверх — переворачиваем построчно (по строке за раз)
            for (int y = 0; y < d.H; y++)
                src.Slice((d.H - 1 - y) * d.W, d.W).CopyTo(d.Pixels.AsSpan(y * d.W, d.W));
            return d;
        }
    }

    /// <summary>Растровый шрифт из запечённого атласа.</summary>
    internal static class Font
    {
        public struct Glyph
        {
            public short X, Y, W, H, BearingX, BearingY, Advance;
        }

        public static readonly Dictionary<char, Glyph> Glyphs = new Dictionary<char, Glyph>();
        public static TexData Atlas;
        public static int LineHeight = 57, Baseline = 45;
        private static bool _loaded;

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                Atlas = TexData.FromTexture(Assets.Texture("font.png"));
                using var ms = new MemoryStream(Assets.Raw("font.bin"));
                using var r = new BinaryReader(ms);
                if (new string(r.ReadChars(4)) != "LFN1") throw new InvalidDataException("font.bin");
                LineHeight = r.ReadInt16();
                Baseline = r.ReadInt16();
                int n = r.ReadInt32();
                for (int i = 0; i < n; i++)
                {
                    char c = (char)r.ReadUInt16();
                    Glyphs[c] = new Glyph
                    {
                        X = r.ReadInt16(), Y = r.ReadInt16(),
                        W = r.ReadInt16(), H = r.ReadInt16(),
                        BearingX = r.ReadInt16(), BearingY = r.ReadInt16(),
                        Advance = r.ReadInt16(),
                    };
                }
                MelonLoader.MelonLogger.Msg($"[LPhone] шрифт: {Glyphs.Count} глифов");
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Error("[LPhone] шрифт не загружен: " + e.Message);
            }
        }
    }
}
