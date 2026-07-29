using System;
using System.Collections.Generic;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Галерея: сетка снимков, просмотр, удаление, сохранение в папку
    /// загрузок Quest и отправка снимка в чат.
    /// </summary>
    internal sealed class GalleryApp : PhoneApp
    {
        public override string Id => "gallery";
        public override string Title => "Gallery";

        private enum Mode { Grid, View, Pick }
        private Mode _mode = Mode.Grid;
        private Photo _open;
        private int _scroll;
        private string _toast;
        private float _toastUntil;

        /// <summary>Куда вернуть выбранный снимок (режим выбора из мессенджера).</summary>
        public Action<Photo> Picker;

        public override void Open()
        {
            if (Picker == null) { _mode = Mode.Grid; _open = null; }
            else _mode = Mode.Pick;
            _scroll = 0;
            OS.Invalidate();
        }

        public override void Close()
        {
            Picker = null;
            _mode = Mode.Grid;
            _open = null;
        }

        public void OpenPicker(Action<Photo> onPick)
        {
            Picker = onPick;
            _mode = Mode.Pick;
            _scroll = 0;
        }

        public override void Tick()
        {
            if (_toastUntil > 0f && Time.unscaledTime > _toastUntil) { _toastUntil = 0f; OS.Invalidate(); }
        }

        public override bool Back()
        {
            if (_mode == Mode.View) { _mode = Picker != null ? Mode.Pick : Mode.Grid; _open = null; OS.Invalidate(); return true; }
            return false;
        }

        // ─────────────── вёрстка ───────────────

        private const int Cols = 3;
        private int CellW => (W - P(40) - (Cols - 1) * P(12)) / Cols;
        private int CellH => Mathf.RoundToInt(CellW * 4f / 3f);
        private int GridTop => P(230);

        private RectInt Cell(int i)
        {
            int col = i % Cols, row = i / Cols;
            return new RectInt(P(20) + col * (CellW + P(12)),
                               GridTop + row * (CellH + P(12)) - _scroll,
                               CellW, CellH);
        }

        private RectInt BtnDelete() => R(40f, 1400f, 200f, 110f);
        private RectInt BtnSave() => R(271f, 1400f, 200f, 110f);
        private RectInt BtnSend() => R(502f, 1400f, 200f, 110f);
        private RectInt BtnBack() => R(20f, 120f, 150f, 110f);

        public override void Draw()
        {
            G.Clear(UI.Bg);
            OS.DrawStatusBar();

            if (_mode == Mode.View && _open != null) { DrawView(); return; }
            DrawGrid();
        }

        private void DrawGrid()
        {
            G.Text(_mode == Mode.Pick ? "Выбери фото" : "Галерея", P(40), P(130), 0.78f * K, UI.White);
            G.Text(PhotoStore.All.Count + " из " + PhotoStore.Limit, P(40), P(196), 0.32f * K, UI.Dim);

            if (PhotoStore.All.Count == 0)
            {
                G.Text("Здесь пока пусто", W / 2, P(660), 0.46f * K, UI.Dim, Gfx.Align.Center);
                G.Text("Сними что-нибудь в Camera", W / 2, P(720), 0.34f * K,
                       new Color32(110, 112, 122, 255), Gfx.Align.Center);
                OS.DrawHomeBar();
                return;
            }

            for (int i = 0; i < PhotoStore.All.Count; i++)
            {
                var c = Cell(i);
                if (c.y + c.height < GridTop || c.y > H) continue;
                G.RoundRect(c.x, c.y, c.width, c.height, P(16), new Color32(26, 26, 30, 255));
                G.BlitRounded(PhotoStore.All[i].Data, c.x, c.y, c.width, c.height, P(16));
                if (PhotoStore.All[i].From != null)
                {
                    G.RoundRect(c.x + P(8), c.y + P(8), P(46), P(30), P(12), new Color32(0, 0, 0, 170));
                    UI.Bolt(G, c.x + P(31), c.y + P(23), P(26), UI.White);
                }
            }

            if (_toastUntil > 0f) DrawToast();
            OS.DrawHomeBar();
        }

        private void DrawView()
        {
            // фото по центру, с сохранением пропорций
            int availW = W - P(40), availH = P(1050);
            float ar = (float)_open.Data.W / Mathf.Max(1, _open.Data.H);
            int w = availW, h = Mathf.RoundToInt(availW / ar);
            if (h > availH) { h = availH; w = Mathf.RoundToInt(availH * ar); }
            int x = (W - w) / 2, y = P(250) + (availH - h) / 2;

            G.RoundRect(x - P(6), y - P(6), w + P(12), h + P(12), P(22), new Color32(30, 30, 34, 255));
            G.Blit(_open.Data, x, y, w, h);

            var b = BtnBack();
            UI.BackArrow(G, b.x + P(30), b.y + P(20), P(34), UI.White);
            G.Text(_open.From != null ? "От " + _open.From : _open.Taken.ToString("d MMMM, HH:mm"),
                   W / 2, P(140), 0.38f * K, UI.White, Gfx.Align.Center);

            UI.Pill(G, BtnDelete(), "Удалить", new Color32(58, 30, 34, 255), UI.Red, 0.34f * K);
            UI.Pill(G, BtnSave(), "В Downloads", new Color32(28, 40, 60, 255), UI.Blue, 0.30f * K);
            UI.Pill(G, BtnSend(), "Отправить", new Color32(26, 48, 32, 255), UI.Green, 0.34f * K);

            if (_toastUntil > 0f) DrawToast();
            OS.DrawHomeBar();
        }

        private void DrawToast()
        {
            G.RoundRect(P(60), P(1250), W - P(120), P(96), P(30), new Color32(0, 0, 0, 210));
            G.Text(_toast, W / 2, P(1278), 0.32f * K, UI.White, Gfx.Align.Center);
        }

        private void Toast(string s)
        {
            _toast = s;
            _toastUntil = Time.unscaledTime + 2.4f;
            OS.Invalidate();
        }

        // ─────────────── ввод ───────────────

        public override void Up(Vector2 p, Vector2 d)
        {
            // прокрутка сетки
            if (_mode != Mode.View && Mathf.Abs(d.y) > P(60))
            {
                int rows = (PhotoStore.All.Count + Cols - 1) / Cols;
                int max = Mathf.Max(0, rows * (CellH + P(12)) - (H - GridTop - P(120)));
                _scroll = Mathf.Clamp(_scroll - Mathf.RoundToInt(d.y), 0, max);
                OS.Invalidate();
                return;
            }
            if (d.magnitude > P(50)) return;

            if (_mode == Mode.View)
            {
                if (In(p, BtnBack())) { Back(); return; }
                if (In(p, BtnDelete()))
                {
                    PhotoStore.Remove(_open);
                    _open = null;
                    _mode = Picker != null ? Mode.Pick : Mode.Grid;
                    OS.Audio?.Click();
                    OS.Invalidate();
                    return;
                }
                if (In(p, BtnSave()))
                {
                    bool ok = PhotoStore.Save(_open, out var path);
                    Toast(ok ? "Сохранено: " + Short(path) : "Не удалось сохранить");
                    OS.Audio?.Click();
                    return;
                }
                if (In(p, BtnSend()))
                {
                    OS.AppMessenger.PendingPhoto = _open;
                    OS.Launch(OS.AppMessenger);
                    return;
                }
                return;
            }

            for (int i = 0; i < PhotoStore.All.Count; i++)
            {
                if (!In(p, Cell(i))) continue;
                var ph = PhotoStore.All[i];
                if (_mode == Mode.Pick && Picker != null)
                {
                    var cb = Picker;
                    Picker = null;
                    cb(ph);
                    return;
                }
                _open = ph;
                _mode = Mode.View;
                OS.Audio?.Click();
                OS.Invalidate();
                return;
            }
        }

        private static string Short(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            int i = path.LastIndexOf('/');
            return i >= 0 ? path.Substring(i + 1) : path;
        }
    }
}
