using System;
using System.Collections.Generic;
using UnityEngine;

namespace LPhone
{
    internal enum ScreenId { Lock, Home, Camera, Gallery, Store, Messenger }

    /// <summary>
    /// «Операционка» телефона: состояние, экраны, ввод пальцем, перерисовка.
    /// Каждый экран рисует себя в Gfx и получает касания в координатах экрана.
    /// </summary>
    internal sealed class PhoneOS
    {
        private readonly PhoneInstance _phone;
        private readonly Gfx _g;

        public ScreenId Current { get; private set; } = ScreenId.Lock;
        public bool Locked { get; private set; } = true;

        private TexData _wall, _icCam, _icGal, _icStore, _icMsg;
        private string _lastClock = "";
        private float _repaintAt;

        // жест свайпа
        private bool _dragging;
        private Vector2 _dragStart, _dragNow;
        private float _unlockProgress;

        public readonly List<AppEntry> Installed = new List<AppEntry>();

        public sealed class AppEntry
        {
            public string Title;
            public ScreenId Screen;
            public TexData Icon;
        }

        public PhoneOS(PhoneInstance phone)
        {
            _phone = phone;
            _g = phone.Screen;

            Font.Load();
            _wall    = TexData.FromTexture(Assets.Wallpaper);
            _icCam   = TexData.FromTexture(Assets.IconCamera);
            _icGal   = TexData.FromTexture(Assets.IconGallery);
            _icStore = TexData.FromTexture(Assets.IconStore);
            _icMsg   = TexData.FromTexture(Assets.IconMessenger);

            // Предустановлены: Камера, Галерея, L Store. Messenger ставится из стора.
            Installed.Add(new AppEntry { Title = "Camera",  Screen = ScreenId.Camera,  Icon = _icCam });
            Installed.Add(new AppEntry { Title = "Gallery", Screen = ScreenId.Gallery, Icon = _icGal });
            Installed.Add(new AppEntry { Title = "L Store", Screen = ScreenId.Store,   Icon = _icStore });

            Repaint();
        }

        public bool MessengerInstalled { get; private set; }

        public void InstallMessenger()
        {
            if (MessengerInstalled) return;
            MessengerInstalled = true;
            Installed.Add(new AppEntry { Title = "Messenger", Screen = ScreenId.Messenger, Icon = _icMsg });
            Repaint();
        }

        // ─────────────────────── жизненный цикл ───────────────────────

        public void Tick()
        {
            // часы обновляем раз в секунду — перерисовка только при смене минуты
            if (Time.unscaledTime >= _repaintAt)
            {
                _repaintAt = Time.unscaledTime + 0.5f;
                string clock = DateTime.Now.ToString("HH:mm");
                if (clock != _lastClock)
                {
                    _lastClock = clock;
                    Repaint();
                }
            }
            _g.Present();
        }

        public void Lock()
        {
            Locked = true;
            Current = ScreenId.Lock;
            _unlockProgress = 0f;
            Repaint();
        }

        public void Open(ScreenId s)
        {
            Current = s;
            Repaint();
        }

        // ─────────────────────── ввод ───────────────────────

        /// <summary>Палец коснулся экрана. p — пиксели экрана (0..W, 0..H сверху вниз).</summary>
        public void TouchDown(Vector2 p)
        {
            _dragging = true;
            _dragStart = _dragNow = p;
        }

        public void TouchMove(Vector2 p)
        {
            if (!_dragging) return;
            _dragNow = p;
            if (Locked)
            {
                float dy = _dragStart.y - p.y;                 // вверх = положительно
                _unlockProgress = Mathf.Clamp01(dy / 420f);
                Repaint();
            }
        }

        public void TouchUp(Vector2 p)
        {
            if (!_dragging) return;
            _dragging = false;
            Vector2 d = p - _dragStart;

            if (Locked)
            {
                if (_dragStart.y > Phone.ScreenPxH * 0.55f && -d.y > 260f)
                {
                    Locked = false;
                    Current = ScreenId.Home;
                }
                _unlockProgress = 0f;
                Repaint();
                return;
            }

            // короткое касание = тап
            if (d.magnitude < 40f) Tap(p);
            else if (d.y > 320f && _dragStart.y < 260f) Lock();   // свайп вниз сверху = блокировка
        }

        private void Tap(Vector2 p)
        {
            if (Current == ScreenId.Home)
            {
                int idx = DockHitTest(p);
                if (idx >= 0 && idx < Installed.Count)
                {
                    Open(Installed[idx].Screen);
                    return;
                }
            }
            else if (Current != ScreenId.Lock)
            {
                // «домой» — нижняя полоска
                if (p.y > Phone.ScreenPxH - 90) Open(ScreenId.Home);
            }
        }

        // ─────────────────────── отрисовка ───────────────────────

        public void Repaint()
        {
            switch (Current)
            {
                case ScreenId.Lock: DrawLock(); break;
                case ScreenId.Home: DrawHome(); break;
                default:            DrawStub(); break;
            }
        }

        private void DrawWallpaper(float dim = 0f)
        {
            if (_wall != null) _g.Blit(_wall, 0, 0, _g.W, _g.H);
            else _g.Clear(new Color32(18, 12, 28, 255));
            if (dim > 0f) _g.Rect(0, 0, _g.W, _g.H, new Color32(0, 0, 0, 255), dim);
        }

        private void DrawStatusBar()
        {
            var white = new Color32(255, 255, 255, 255);
            _g.Text(DateTime.Now.ToString("HH:mm"), 92, 58, 0.44f, white);
            // батарея
            int bx = _g.W - 128, by = 62;
            _g.RoundRect(bx, by, 56, 26, 8, new Color32(255, 255, 255, 90));
            _g.RoundRect(bx + 3, by + 3, 44, 20, 5, white);
            _g.RoundRect(bx + 60, by + 9, 5, 10, 2, new Color32(255, 255, 255, 90));
            // сеть
            for (int i = 0; i < 4; i++)
                _g.RoundRect(_g.W - 232 + i * 15, by + 22 - (i + 1) * 5, 10, (i + 1) * 5, 3, white);
        }

        private void DrawLock()
        {
            DrawWallpaper(0.10f + 0.35f * _unlockProgress);
            DrawStatusBar();

            var white = new Color32(255, 255, 255, 255);
            int cx = _g.W / 2;
            int shift = Mathf.RoundToInt(_unlockProgress * -90f);

            var now = DateTime.Now;
            _g.Text(now.ToString("dddd, d MMMM"), cx, 300 + shift, 0.52f,
                    new Color32(235, 235, 245, 255), Gfx.Align.Center);
            _g.Text(now.ToString("HH:mm"), cx, 350 + shift, 2.55f, white, Gfx.Align.Center);

            // подсказка разблокировки
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.2f);
            _g.Text("Свайп вверх для разблокировки", cx, _g.H - 200, 0.40f,
                    new Color32(240, 240, 250, 255), Gfx.Align.Center, 0.55f + 0.35f * pulse);
            _g.RoundRect(cx - 90, _g.H - 60, 180, 9, 5, white, 0.9f);
        }

        private const int DockH = 210;

        private void DrawHome()
        {
            DrawWallpaper(0.05f);
            DrawStatusBar();

            // док
            int dockY = _g.H - DockH - 60;
            _g.RoundRect(28, dockY, _g.W - 56, DockH, 56, new Color32(255, 255, 255, 46));

            for (int i = 0; i < Installed.Count && i < 4; i++)
            {
                var r = IconRect(i);
                _g.BlitRounded(Installed[i].Icon, r.x, r.y, r.width, r.height,
                               Mathf.RoundToInt(r.width * 0.235f));
                _g.Text(Installed[i].Title, r.x + r.width / 2, r.y + r.height + 12, 0.32f,
                        new Color32(255, 255, 255, 255), Gfx.Align.Center);
            }

            _g.RoundRect(_g.W / 2 - 90, _g.H - 46, 180, 9, 5,
                         new Color32(255, 255, 255, 255), 0.85f);
        }

        private RectInt IconRect(int i)
        {
            int n = Mathf.Max(1, Mathf.Min(Installed.Count, 4));
            int size = 132;
            int gap = (_g.W - 80 - n * size) / Mathf.Max(1, n + 1);
            int x = 40 + gap + i * (size + gap);
            int y = _g.H - DockH - 60 + 30;
            return new RectInt(x, y, size, size);
        }

        private int DockHitTest(Vector2 p)
        {
            for (int i = 0; i < Installed.Count && i < 4; i++)
            {
                var r = IconRect(i);
                if (p.x >= r.x - 14 && p.x <= r.x + r.width + 14 &&
                    p.y >= r.y - 14 && p.y <= r.y + r.height + 26)
                    return i;
            }
            return -1;
        }

        private void DrawStub()
        {
            _g.Clear(new Color32(16, 17, 22, 255));
            DrawStatusBar();
            _g.Text(Current.ToString(), _g.W / 2, 420, 1.05f,
                    new Color32(255, 255, 255, 255), Gfx.Align.Center);
            _g.Text("в разработке", _g.W / 2, 520, 0.42f,
                    new Color32(150, 155, 170, 255), Gfx.Align.Center);
            _g.RoundRect(_g.W / 2 - 90, _g.H - 46, 180, 9, 5,
                         new Color32(255, 255, 255, 255), 0.85f);
        }
    }
}
