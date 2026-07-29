using System;
using UnityEngine;

namespace LPhone
{
    /// <summary>Базовое приложение телефона.</summary>
    internal abstract class PhoneApp
    {
        public PhoneOS OS;
        public Gfx G;
        public TexData Icon;

        public abstract string Id { get; }
        public abstract string Title { get; }

        /// <summary>Цвет плитки, если своей иконки нет.</summary>
        public virtual Color32 Tile => new Color32(60, 60, 68, 255);

        public virtual void Open() { }
        public virtual void Close() { }
        public virtual void Tick() { }
        public abstract void Draw();

        public virtual void Down(Vector2 p) { }
        public virtual void Move(Vector2 p) { }
        /// <summary>d — смещение от точки нажатия.</summary>
        public virtual void Up(Vector2 p, Vector2 d) { }
        public virtual void LongPress(Vector2 p) { }

        /// <summary>Обработать «назад» самому. false — выходим на домашний экран.</summary>
        public virtual bool Back() => false;

        // короткие помощники вёрстки (база 742x1600)
        protected float K => G.W / 742f;
        protected int P(float v) => Mathf.RoundToInt(v * K);
        protected int W => G.W;
        protected int H => G.H;

        protected static bool In(Vector2 p, RectInt r) =>
            p.x >= r.x && p.x <= r.x + r.width && p.y >= r.y && p.y <= r.y + r.height;

        protected RectInt R(float x, float y, float w, float h) =>
            new RectInt(P(x), P(y), P(w), P(h));
    }

    /// <summary>Общие элементы интерфейса в стиле iOS.</summary>
    internal static class UI
    {
        public static readonly Color32 White = new Color32(255, 255, 255, 255);
        public static readonly Color32 Dim = new Color32(150, 152, 162, 255);
        public static readonly Color32 Blue = new Color32(10, 132, 255, 255);
        public static readonly Color32 Green = new Color32(48, 209, 88, 255);
        public static readonly Color32 Red = new Color32(255, 69, 58, 255);
        public static readonly Color32 Card = new Color32(28, 28, 32, 255);
        public static readonly Color32 Card2 = new Color32(44, 44, 50, 255);
        public static readonly Color32 Bg = new Color32(12, 12, 14, 255);

        /// <summary>Круглая аватарка контакта: фото нет — рисуем инициалы на цвете имени.</summary>
        public static void Avatar(Gfx g, int cx, int cy, int r, Contact c, float scale)
        {
            if (c == null) return;
            var t = c.Tint;
            g.Circle(cx, cy, r, t);
            // лёгкий блик сверху, чтобы кружок не выглядел плоским
            g.Circle(cx, cy - r / 3, (int)(r * 0.72f), new Color32(255, 255, 255, 38));

            if (c.IsAuthor) Bolt(g, cx, cy, (int)(r * 1.15f), White);
            else g.Text(c.Initials, cx, cy - Mathf.RoundToInt(20f * scale), 0.62f * scale, White, Gfx.Align.Center);
        }

        /// <summary>Белая молния — фирменный знак мессенджера и автора.</summary>
        public static void Bolt(Gfx g, int cx, int cy, int size, Color32 col)
        {
            // молния как набор горизонтальных отрезков: дёшево и без сглаживания краёв не обойтись
            float h = size, w = size * 0.56f;
            int steps = Mathf.Max(8, Mathf.RoundToInt(h));
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);           // 0 сверху .. 1 снизу
                float y = cy - h * 0.5f + h * t;
                float xl, xr;
                if (t < 0.55f)
                {
                    float u = t / 0.55f;
                    xl = Mathf.Lerp(0.10f, -0.30f, u);
                    xr = Mathf.Lerp(0.42f, 0.10f, u);
                }
                else
                {
                    float u = (t - 0.55f) / 0.45f;
                    xl = Mathf.Lerp(-0.10f, -0.02f, u);
                    xr = Mathf.Lerp(0.32f, -0.02f, u);
                }
                int x0 = Mathf.RoundToInt(cx + xl * w);
                int x1 = Mathf.RoundToInt(cx + xr * w);
                if (x1 <= x0) continue;
                g.Rect(x0, Mathf.RoundToInt(y), x1 - x0, Mathf.Max(1, Mathf.RoundToInt(h / steps) + 1), col);
            }
        }

        /// <summary>Кнопка-пилюля.</summary>
        public static void Pill(Gfx g, RectInt r, string label, Color32 bg, Color32 fg, float textScale)
        {
            g.RoundRect(r.x, r.y, r.width, r.height, r.height / 2, bg);
            g.Text(label, r.x + r.width / 2, r.y + (r.height - g.LineHeight(textScale)) / 2 + 2,
                   textScale, fg, Gfx.Align.Center);
        }

        /// <summary>Круглая кнопка с подписью-глифом.</summary>
        public static void RoundBtn(Gfx g, int cx, int cy, int r, Color32 bg, string glyph, Color32 fg, float scale)
        {
            g.Circle(cx, cy, r, bg);
            if (!string.IsNullOrEmpty(glyph))
                g.Text(glyph, cx, cy - g.LineHeight(scale) / 2, scale, fg, Gfx.Align.Center);
        }

        /// <summary>Стрелка «назад».</summary>
        public static void BackArrow(Gfx g, int x, int y, int size, Color32 col)
        {
            for (int i = 0; i < size; i++)
            {
                int t = Mathf.RoundToInt(size * 0.16f);
                g.Rect(x + i, y + size / 2 - i - t / 2, Mathf.Max(2, t / 2 + 1), t, col);
                g.Rect(x + i, y + size / 2 + i - t / 2, Mathf.Max(2, t / 2 + 1), t, col);
            }
        }

        public static void Chevron(Gfx g, int x, int y, int size, Color32 col)
        {
            for (int i = 0; i < size; i++)
            {
                int t = Mathf.Max(2, Mathf.RoundToInt(size * 0.14f));
                g.Rect(x + size - i, y + size / 2 - i - t / 2, t, t, col);
                g.Rect(x + size - i, y + size / 2 + i - t / 2, t, t, col);
            }
        }

        /// <summary>Значок «плюс».</summary>
        public static void Plus(Gfx g, int cx, int cy, int size, int thick, Color32 col)
        {
            g.RoundRect(cx - size / 2, cy - thick / 2, size, thick, thick / 2, col);
            g.RoundRect(cx - thick / 2, cy - size / 2, thick, size, thick / 2, col);
        }

        /// <summary>Крестик (закрыть / удалить).</summary>
        public static void Cross(Gfx g, int cx, int cy, int size, int thick, Color32 col)
        {
            for (int i = -size / 2; i <= size / 2; i++)
            {
                g.Rect(cx + i - thick / 2, cy + i - thick / 2, thick, thick, col);
                g.Rect(cx + i - thick / 2, cy - i - thick / 2, thick, thick, col);
            }
        }

        /// <summary>Заливка треугольника построчно — основа для стрелок и логотипов.</summary>
        public static void Tri(Gfx g, Vector2 a, Vector2 b, Vector2 c, Color32 col)
        {
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)));
            int y1 = Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 1e-5f) return;

            for (int y = y0; y <= y1; y++)
            {
                int x0 = Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)));
                int x1 = Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)));
                int run = -1;
                for (int x = x0; x <= x1; x++)
                {
                    float w0 = ((b.y - c.y) * (x - c.x) + (c.x - b.x) * (y - c.y)) / den;
                    float w1 = ((c.y - a.y) * (x - c.x) + (a.x - c.x) * (y - c.y)) / den;
                    float w2 = 1f - w0 - w1;
                    bool inside = w0 >= -0.001f && w1 >= -0.001f && w2 >= -0.001f;
                    if (inside) { if (run < 0) run = x; }
                    else if (run >= 0) { g.Rect(run, y, x - run, 1, col); run = -1; }
                }
                if (run >= 0) g.Rect(run, y, x1 - run + 1, 1, col);
            }
        }

        /// <summary>Самолётик Telegram.</summary>
        public static void TelegramGlyph(Gfx g, int cx, int cy, int size, Color32 col)
        {
            float s = size * 0.5f;
            var tip = new Vector2(cx + s, cy - s * 0.75f);        // правый верхний угол
            var tail = new Vector2(cx - s, cy - s * 0.05f);       // левый край
            var mid = new Vector2(cx + s * 0.05f, cy + s * 0.32f);
            var low = new Vector2(cx + s * 0.42f, cy + s * 0.85f);

            Tri(g, tip, tail, mid, col);          // верхнее крыло
            Tri(g, tip, mid, low, col);           // нижний «хвост»
        }

        /// <summary>
        /// Глобус — значок сайта. Кольцо получается вычитанием: внутренний круг
        /// закрашивается цветом подложки, поэтому её и передаём (прозрачным
        /// «вырезать» нельзя — альфа 0 просто ничего не пишет).
        /// </summary>
        public static void GlobeGlyph(Gfx g, int cx, int cy, int size, Color32 col, Color32 bg)
        {
            int r = size / 2;
            int th = Mathf.Max(2, Mathf.RoundToInt(size * 0.09f));
            g.Circle(cx, cy, r, col);
            g.Circle(cx, cy, r - th, bg);

            g.Rect(cx - r + th / 2, cy - th / 2, r * 2 - th, th, col);   // экватор
            for (int i = -r + th; i <= r - th; i++)                       // меридиан
            {
                float k = i / (float)r;
                int hw = Mathf.RoundToInt(r * 0.45f * Mathf.Cos(k * Mathf.PI * 0.5f));
                if (hw <= 0) continue;
                g.Rect(cx - hw, cy + i, Mathf.Max(1, th / 2), 1, col);
                g.Rect(cx + hw - Mathf.Max(1, th / 2), cy + i, Mathf.Max(1, th / 2), 1, col);
            }
        }

        /// <summary>Круговые стрелки — «сменить камеру».</summary>
        public static void SwapGlyph(Gfx g, int cx, int cy, int size, Color32 col)
        {
            int r = Mathf.RoundToInt(size * 0.36f);
            int th = Mathf.Max(2, Mathf.RoundToInt(size * 0.09f));
            // две дуги: верхняя слева направо, нижняя справа налево
            for (int i = 0; i <= 90; i += 2)
            {
                float a1 = Mathf.Deg2Rad * (20 + i * 1.6f);
                float a2 = Mathf.Deg2Rad * (200 + i * 1.6f);
                g.Rect(cx + Mathf.RoundToInt(Mathf.Cos(a1) * r) - th / 2,
                       cy - Mathf.RoundToInt(Mathf.Sin(a1) * r) - th / 2, th, th, col);
                g.Rect(cx + Mathf.RoundToInt(Mathf.Cos(a2) * r) - th / 2,
                       cy - Mathf.RoundToInt(Mathf.Sin(a2) * r) - th / 2, th, th, col);
            }
            int h = Mathf.RoundToInt(size * 0.16f);
            Tri(g, new Vector2(cx + r + h * 0.2f, cy - h),
                   new Vector2(cx + r + h * 1.4f, cy - h * 0.1f),
                   new Vector2(cx + r - h * 0.4f, cy - h * 0.1f), col);
            Tri(g, new Vector2(cx - r - h * 0.2f, cy + h),
                   new Vector2(cx - r - h * 1.4f, cy + h * 0.1f),
                   new Vector2(cx - r + h * 0.4f, cy + h * 0.1f), col);
        }

        /// <summary>Значок камеры (для кнопки видео).</summary>
        public static void CamGlyph(Gfx g, int cx, int cy, int size, Color32 col)
        {
            int w = size, h = Mathf.RoundToInt(size * 0.62f);
            g.RoundRect(cx - w / 2, cy - h / 2, Mathf.RoundToInt(w * 0.68f), h, Mathf.RoundToInt(h * 0.24f), col);
            for (int i = 0; i < Mathf.RoundToInt(w * 0.3f); i++)
            {
                int hh = Mathf.RoundToInt(h * (0.35f + 0.65f * i / Mathf.Max(1f, w * 0.3f)));
                g.Rect(cx + Mathf.RoundToInt(w * 0.20f) + i, cy - hh / 2, 1, hh, col);
            }
        }

        /// <summary>Значок трубки.</summary>
        public static void PhoneGlyph(Gfx g, int cx, int cy, int size, Color32 col, bool hangup)
        {
            int r = Mathf.Max(2, Mathf.RoundToInt(size * 0.16f));
            int arm = Mathf.RoundToInt(size * 0.34f);
            for (int i = 0; i <= arm; i++)
            {
                float t = (float)i / arm;
                int x = Mathf.RoundToInt(cx - size * 0.30f + size * 0.60f * t);
                int y = Mathf.RoundToInt(cy + (hangup ? -1 : 1) * Mathf.Sin(t * Mathf.PI) * size * 0.20f);
                g.Circle(x, y, r, col);
            }
            g.Circle(Mathf.RoundToInt(cx - size * 0.30f), cy, Mathf.RoundToInt(r * 1.5f), col);
            g.Circle(Mathf.RoundToInt(cx + size * 0.30f), cy, Mathf.RoundToInt(r * 1.5f), col);
        }

        /// <summary>Значок динамика.</summary>
        public static void SpeakerGlyph(Gfx g, int cx, int cy, int size, Color32 col, bool on)
        {
            int b = Mathf.RoundToInt(size * 0.26f);
            g.Rect(cx - Mathf.RoundToInt(size * 0.34f), cy - b / 2, b, b, col);
            for (int i = 0; i < Mathf.RoundToInt(size * 0.24f); i++)
            {
                int hh = Mathf.RoundToInt(b + i * 2.4f);
                g.Rect(cx - Mathf.RoundToInt(size * 0.34f) + b + i, cy - hh / 2, 1, hh, col);
            }
            if (on)
            {
                g.Circle(cx + Mathf.RoundToInt(size * 0.24f), cy, Mathf.RoundToInt(size * 0.10f), col);
                g.Circle(cx + Mathf.RoundToInt(size * 0.24f), cy, Mathf.RoundToInt(size * 0.06f), new Color32(0, 0, 0, 0));
            }
        }

        /// <summary>Значок микрофона.</summary>
        public static void MicGlyph(Gfx g, int cx, int cy, int size, Color32 col, bool on)
        {
            int w = Mathf.RoundToInt(size * 0.26f), h = Mathf.RoundToInt(size * 0.46f);
            g.RoundRect(cx - w / 2, cy - Mathf.RoundToInt(size * 0.34f), w, h, w / 2, col);
            g.RoundRect(cx - Mathf.RoundToInt(size * 0.22f), cy + Mathf.RoundToInt(size * 0.06f),
                        Mathf.RoundToInt(size * 0.44f), Mathf.Max(2, Mathf.RoundToInt(size * 0.06f)),
                        2, col);
            g.RoundRect(cx - 1, cy + Mathf.RoundToInt(size * 0.12f), Mathf.Max(2, Mathf.RoundToInt(size * 0.06f)),
                        Mathf.RoundToInt(size * 0.20f), 1, col);
            if (!on)
                for (int i = -size / 2; i <= size / 2; i++)
                    g.Rect(cx + i - 1, cy + i - 1, 3, 3, Red);
        }
    }
}
