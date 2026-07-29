using System;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Экранная клавиатура телефона. Без неё мессенджер был бы игрушкой:
    /// в VR системной клавиатуры под рукой нет, поэтому раскладку рисуем сами.
    /// Есть русская и английская раскладки, Shift, пробел, забой и отправка.
    /// </summary>
    internal sealed class Keyboard
    {
        private static readonly string[] En = { "qwertyuiop", "asdfghjkl", "zxcvbnm" };
        private static readonly string[] Ru = { "йцукенгшщзх", "фывапролджэ", "ячсмитьбю" };

        public bool Cyrillic = true;
        public bool Shift;
        public string Text = "";
        public int MaxLen = 120;

        public Action OnChanged;
        public Action OnSend;

        // вёрстка в базовых координатах 742x1600
        public const float Top = 1052f;
        private const float RowH = 96f, Gap = 9f, Margin = 8f;

        private string[] Rows => Cyrillic ? Ru : En;

        private static RectInt Key(float K, int W, int row, int idx, int count)
        {
            float w = (742f - 2f * Margin - (count - 1) * Gap) / count;
            float x = Margin + idx * (w + Gap);
            float y = Top + row * (RowH + Gap);
            return new RectInt(Mathf.RoundToInt(x * K), Mathf.RoundToInt(y * K),
                               Mathf.RoundToInt(w * K), Mathf.RoundToInt(RowH * K));
        }

        private static RectInt Ctrl(float K, float x, float w)
        {
            float y = Top + 3 * (RowH + Gap);
            return new RectInt(Mathf.RoundToInt(x * K), Mathf.RoundToInt(y * K),
                               Mathf.RoundToInt(w * K), Mathf.RoundToInt(RowH * K));
        }

        private RectInt BtnLang(float K) => Ctrl(K, Margin, 118f);
        private RectInt BtnShift(float K) => Ctrl(K, Margin + 127f, 100f);
        private RectInt BtnSpace(float K) => Ctrl(K, Margin + 236f, 240f);
        private RectInt BtnBack(float K) => Ctrl(K, Margin + 485f, 100f);
        private RectInt BtnSend(float K) => Ctrl(K, Margin + 594f, 140f);

        public void Draw(Gfx g, float K)
        {
            int kbTop = Mathf.RoundToInt((Top - 14f) * K);
            g.Rect(0, kbTop, g.W, g.H - kbTop, new Color32(18, 18, 22, 255));

            var rows = Rows;
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                for (int i = 0; i < row.Length; i++)
                {
                    var k = Key(K, g.W, r, i, row.Length);
                    g.RoundRect(k.x, k.y, k.width, k.height, Mathf.RoundToInt(12 * K),
                                new Color32(58, 58, 66, 255));
                    char ch = Shift ? char.ToUpper(row[i]) : row[i];
                    g.Text(ch.ToString(), k.x + k.width / 2,
                           k.y + (k.height - g.LineHeight(0.44f * K)) / 2, 0.44f * K,
                           UI.White, Gfx.Align.Center);
                }
            }

            Cap(g, K, BtnLang(K), Cyrillic ? "EN" : "РУ", new Color32(40, 40, 46, 255));
            Cap(g, K, BtnShift(K), "↑", Shift ? UI.White : new Color32(40, 40, 46, 255),
                Shift ? new Color32(20, 20, 24, 255) : UI.White, 0.46f);
            Cap(g, K, BtnSpace(K), "пробел", new Color32(58, 58, 66, 255));
            Cap(g, K, BtnBack(K), "←", new Color32(40, 40, 46, 255), null, 0.46f);
            Cap(g, K, BtnSend(K), "Отправить", UI.Blue, UI.White, 0.30f);
        }

        private static void Cap(Gfx g, float K, RectInt r, string label, Color32 bg,
                                Color32? fg = null, float scale = 0.36f)
        {
            g.RoundRect(r.x, r.y, r.width, r.height, Mathf.RoundToInt(12 * K), bg);
            g.Text(label, r.x + r.width / 2, r.y + (r.height - g.LineHeight(scale * K)) / 2,
                   scale * K, fg ?? UI.White, Gfx.Align.Center);
        }

        /// <summary>Обработать касание. true — клавиша нажата.</summary>
        public bool Tap(Vector2 p, float K, int W)
        {
            var rows = Rows;
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                for (int i = 0; i < row.Length; i++)
                {
                    var k = Key(K, W, r, i, row.Length);
                    if (!Inside(p, k)) continue;
                    char ch = Shift ? char.ToUpper(row[i]) : row[i];
                    if (Text.Length < MaxLen) Text += ch;
                    Shift = false;
                    Fire();
                    return true;
                }
            }

            if (Inside(p, BtnLang(K))) { Cyrillic = !Cyrillic; Fire(); return true; }
            if (Inside(p, BtnShift(K))) { Shift = !Shift; Fire(); return true; }
            if (Inside(p, BtnSpace(K))) { if (Text.Length < MaxLen) Text += ' '; Fire(); return true; }
            if (Inside(p, BtnBack(K)))
            {
                if (Text.Length > 0) Text = Text.Substring(0, Text.Length - 1);
                Fire();
                return true;
            }
            if (Inside(p, BtnSend(K))) { try { OnSend?.Invoke(); } catch { } return true; }
            return false;
        }

        private void Fire() { try { OnChanged?.Invoke(); } catch { } }

        private static bool Inside(Vector2 p, RectInt r) =>
            p.x >= r.x && p.x <= r.x + r.width && p.y >= r.y && p.y <= r.y + r.height;
    }
}
