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

        // Экран поделён на горизонтальные полосы. Заливаем в текстуру только
        // те, что реально менялись: перерисовка одной кнопки не должна стоить
        // копирования всех 4.7 МБ кадра.
        private const int Bands = 10;
        private readonly bool[] _bandDirty = new bool[Bands];
        private readonly int _bandH;
        private Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32> _band;
        private Color32[] _bandBuf;

        // Один раз выделенный Il2Cpp-массив под заливку. Раньше он создавался
        // на КАЖДЫЙ Present — это 1.2 МБ мусора 30 раз в секунду при свайпе,
        // отсюда и были фризы. Теперь копируем в него спаном.
        private Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32> _gpu;

        // Кэш готовых фонов (обои + затемнение). Ключ — затемнение в сотых.
        private readonly Dictionary<int, Color32[]> _layers = new Dictionary<int, Color32[]>();

        public Texture2D Texture => _tex;
        public void MarkDirty() => MarkRows(0, H);

        /// <summary>Пометить строки [y0, y1) как изменившиеся.</summary>
        private void MarkRows(int y0, int y1)
        {
            _dirty = true;
            if (y0 < 0) y0 = 0;
            if (y1 > H) y1 = H;
            if (y1 <= y0) return;
            int b0 = y0 / _bandH, b1 = (y1 - 1) / _bandH;
            if (b1 >= Bands) b1 = Bands - 1;
            for (int b = b0; b <= b1; b++) _bandDirty[b] = true;
        }

        public Gfx(int w, int h)
        {
            W = w; H = h;
            _bandH = (h + Bands - 1) / Bands;
            _buf = new Color32[w * h];
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        // ─────────────────── кэш фоновых слоёв ───────────────────

        /// <summary>Есть ли готовый слой с таким ключом.</summary>
        public bool HasLayer(int key) => _layers.ContainsKey(key);

        /// <summary>Запомнить текущий буфер как фоновый слой.</summary>
        public void StoreLayer(int key)
        {
            // при 742x1600 слой весит 4.7 МБ, поэтому держим только два
            if (_layers.Count >= 2 && !_layers.ContainsKey(key)) _layers.Clear();
            if (!_layers.TryGetValue(key, out var l) || l.Length != _buf.Length)
            {
                l = new Color32[_buf.Length];
                _layers[key] = l;
            }
            Array.Copy(_buf, l, _buf.Length);
        }

        /// <summary>Мгновенно вернуть фоновый слой (memcpy вместо попиксельного блита).</summary>
        public bool RestoreLayer(int key)
        {
            if (!_layers.TryGetValue(key, out var l) || l.Length != _buf.Length) return false;
            Array.Copy(l, _buf, _buf.Length);
            MarkRows(0, H);
            return true;
        }

        public void DropLayers() => _layers.Clear();

        /// <summary>Заливаем изменившиеся полосы кадра в текстуру.</summary>
        public void Present()
        {
            if (!_dirty) return;
            _dirty = false;

            if (!_firstPresentLogged)
            {
                _firstPresentLogged = true;
                MelonLoader.MelonLogger.Msg($"[LPhone] ... первый Present {W}x{H}, полоса {_bandH}");
            }

            int dirty = 0;
            for (int b = 0; b < Bands; b++) if (_bandDirty[b]) dirty++;
            if (dirty == 0) return;

            if (dirty >= Bands - 1) PresentFull();
            else PresentBands();

            for (int b = 0; b < Bands; b++) _bandDirty[b] = false;

            _tex.Apply(false);
            if (!_firstApplyLogged)
            {
                _firstApplyLogged = true;
                MelonLoader.MelonLogger.Msg("[LPhone] ... первый Apply ок");
            }
        }

        private void PresentFull()
        {
            if (_gpu == null || _gpu.Length != _buf.Length)
            {
                // ВНИМАНИЕ. Здесь нельзя писать new Il2CppStructArray<Color32>(_buf.Length):
                // в C# 11 преобразование int -> nint считается лучше, чем int -> long,
                // и вызов уходит в конструктор Il2CppStructArray(IntPtr pointer) —
                // длина буфера уезжает туда как нативный указатель и игра падает
                // на первом же SetPixels32. Конструктор от managed-массива
                // однозначен, поэтому создаём массив им — один раз за всё время.
                _gpu = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32>(_buf);
                MelonLoader.MelonLogger.Msg($"[LPhone] ... буфер заливки {_gpu.Length}");
            }
            else
            {
                _buf.AsSpan().CopyTo(_gpu.AsSpan());
            }
            _tex.SetPixels32(_gpu);
            if (!_firstApplyLogged) MelonLoader.MelonLogger.Msg("[LPhone] ... SetPixels32 ок");
        }

        /// <summary>
        /// Заливка только изменившихся полос. Строки нашего буфера ложатся в
        /// текстуру подряд и без переворота, поэтому полоса — это непрерывный
        /// кусок _buf, который копируется одним спаном.
        /// </summary>
        private void PresentBands()
        {
            int len = W * _bandH;
            if (_bandBuf == null || _bandBuf.Length != len)
            {
                _bandBuf = new Color32[len];
                _band = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32>(_bandBuf);
            }

            for (int b = 0; b < Bands; b++)
            {
                if (!_bandDirty[b]) continue;
                int y0 = b * _bandH;
                int h = Mathf.Min(_bandH, H - y0);
                if (h <= 0) continue;

                _buf.AsSpan(y0 * W, h * W).CopyTo(_band.AsSpan());
                _tex.SetPixels32(0, y0, W, h, _band);
            }
        }

        // ─────────────────── примитивы ───────────────────

        public void Clear(Color32 c)
        {
            Array.Fill(_buf, c);      // заметно быстрее ручного цикла на 1.2 млн пикселей
            MarkRows(0, H);
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
            MarkRows(y, y + 1);
        }

        public void Rect(int x, int y, int w, int h, Color32 c, float alpha = 1f)
        {
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            if (x2 <= x1 || y2 <= y1) return;

            // Множитель альфы в цикле не меняется — считаем его один раз,
            // а полностью непрозрачную заливку пишем без смешения вообще.
            float a = alpha * (c.a / 255f);
            if (a <= 0f) return;
            if (a >= 1f)
            {
                for (int yy = y1; yy < y2; yy++)
                {
                    int row = yy * W;
                    for (int xx = x1; xx < x2; xx++) _buf[row + xx] = c;
                }
            }
            else
            {
                for (int yy = y1; yy < y2; yy++)
                {
                    int row = yy * W;
                    for (int xx = x1; xx < x2; xx++)
                    {
                        int i = row + xx;
                        _buf[i] = Blend(_buf[i], c, a);
                    }
                }
            }
            MarkRows(y1, y2);
        }

        /// <summary>Скруглённый прямоугольник со сглаженным краем.</summary>
        public void RoundRect(int x, int y, int w, int h, int r, Color32 c, float alpha = 1f)
        {
            r = Mathf.Clamp(r, 0, Mathf.Min(w, h) / 2);
            if (r <= 0) { Rect(x, y, w, h, c, alpha); return; }

            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            if (x2 <= x1 || y2 <= y1) return;

            float baseA = alpha * (c.a / 255f);
            if (baseA <= 0f) return;
            bool opaque = baseA >= 1f;

            // Скругление живёт только в четырёх углах. Раньше корень и две
            // проверки считались для КАЖДОГО пикселя прямоугольника — на доке
            // и карточках это сотни тысяч лишних вычислений на кадр.
            int innerX0 = x + r, innerX1 = x + w - r;      // [innerX0, innerX1) — без скругления по X
            int innerY0 = y + r, innerY1 = y + h - r;

            for (int yy = y1; yy < y2; yy++)
            {
                int row = yy * W;
                bool midRow = yy >= innerY0 && yy < innerY1;

                if (midRow)
                {
                    if (opaque) { for (int xx = x1; xx < x2; xx++) _buf[row + xx] = c; }
                    else
                    {
                        for (int xx = x1; xx < x2; xx++)
                        { int i = row + xx; _buf[i] = Blend(_buf[i], c, baseA); }
                    }
                    continue;
                }

                float dy = yy < innerY0 ? innerY0 - yy - 0.5f : yy - (innerY1 - 1) + 0.5f;
                float dy2 = dy * dy;

                for (int xx = x1; xx < x2; xx++)
                {
                    float a = baseA;
                    if (xx < innerX0 || xx >= innerX1)
                    {
                        float dx = xx < innerX0 ? innerX0 - xx - 0.5f : xx - (innerX1 - 1) + 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy2);
                        float cov = Mathf.Clamp01(r - d + 0.5f);
                        if (cov <= 0f) continue;
                        a = cov * baseA;
                    }
                    int i = row + xx;
                    _buf[i] = a >= 1f ? c : Blend(_buf[i], c, a);
                }
            }
            MarkRows(y1, y2);
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
            MarkRows(y1, y2);
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
                    if (c.a == 255 && alpha >= 1f) _buf[i] = c;
                    else _buf[i] = Blend(_buf[i], c, alpha * (c.a / 255f));
                }
            }
            MarkRows(y1, y2);
        }

        // таблица исходных столбцов для быстрого блита — чтобы не делить на пиксель
        private int[] _sxTab;

        /// <summary>
        /// Непрозрачный блит с масштабированием. Кадр видоискателя занимает
        /// две трети экрана, и гонять его через общий Blit с альфа-смешением
        /// и делением на каждый пиксель — чистая трата: здесь только выборка
        /// и присваивание.
        /// </summary>
        public void BlitOpaque(TexData img, int x, int y, int w, int h)
        {
            if (img == null || w <= 0 || h <= 0) return;
            int x1 = Mathf.Max(0, x), y1 = Mathf.Max(0, y);
            int x2 = Mathf.Min(W, x + w), y2 = Mathf.Min(H, y + h);
            if (x2 <= x1 || y2 <= y1) return;

            if (_sxTab == null || _sxTab.Length < W) _sxTab = new int[W];
            for (int xx = x1; xx < x2; xx++)
            {
                int sx = (int)((long)(xx - x) * img.W / w);
                _sxTab[xx] = sx < 0 ? 0 : (sx >= img.W ? img.W - 1 : sx);
            }

            var src = img.Pixels;
            for (int yy = y1; yy < y2; yy++)
            {
                int sy = (int)((long)(yy - y) * img.H / h);
                if (sy < 0) sy = 0; else if (sy >= img.H) sy = img.H - 1;
                int srow = sy * img.W, drow = yy * W;
                for (int xx = x1; xx < x2; xx++) _buf[drow + xx] = src[srow + _sxTab[xx]];
            }
            MarkRows(y1, y2);
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
            MarkRows(y1, y2);
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
            int tMin = int.MaxValue, tMax = int.MinValue;

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
                            if (py < tMin) tMin = py;
                            if (py > tMax) tMax = py;
                        }
                    }
                }
                pen += g.Advance * scale;
            }
            if (tMax >= tMin) MarkRows(tMin, tMax + 1);
        }
    }

    /// <summary>Простой RGBA-буфер картинки (распакованный PNG).</summary>
    internal sealed class TexData
    {
        public int W, H;
        public Color32[] Pixels;

        /// <summary>
        /// Перелить пиксели в уже существующий буфер. Видоискатель обновляется
        /// восемь раз в секунду, и каждый новый TexData — это 440 КБ в мусор.
        /// </summary>
        public static TexData Reuse(TexData dst, Texture2D t)
        {
            if (t == null) return dst;
            if (dst == null || dst.W != t.width || dst.H != t.height) return FromTexture(t);
            var raw = t.GetPixels32();
            var src = raw.AsSpan();
            for (int y = 0; y < dst.H; y++)
                src.Slice((dst.H - 1 - y) * dst.W, dst.W).CopyTo(dst.Pixels.AsSpan(y * dst.W, dst.W));
            return dst;
        }

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
