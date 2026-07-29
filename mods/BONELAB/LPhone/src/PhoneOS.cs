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

        /// <summary>Вёрстка задумана под ширину 742 — масштабируем под фактическую.</summary>
        private float K => _g.W / 742f;
        private int P(float v) => Mathf.RoundToInt(v * K);

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
                _unlockProgress = Mathf.Clamp01(dy / P(420));
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
                if (_dragStart.y > _g.H * 0.55f && -d.y > P(260))
                {
                    Locked = false;
                    Current = ScreenId.Home;
                }
                _unlockProgress = 0f;
                Repaint();
                return;
            }

            // короткое касание = тап
            if (d.magnitude < P(40)) Tap(p);
            else if (d.y > P(320) && _dragStart.y < P(260)) Lock();   // свайп вниз сверху = блокировка
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
                if (p.y > _g.H - P(90)) Open(ScreenId.Home);
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
            _g.Text(DateTime.Now.ToString("HH:mm"), P(92), P(58), 0.44f * K, white);
            // батарея
            int bx = _g.W - P(128), by = P(62);
            _g.RoundRect(bx, by, P(56), P(26), P(8), new Color32(255, 255, 255, 90));
            _g.RoundRect(bx + P(3), by + P(3), P(44), P(20), P(5), white);
            _g.RoundRect(bx + P(60), by + P(9), P(5), P(10), P(2), new Color32(255, 255, 255, 90));
            // сеть
            for (int i = 0; i < 4; i++)
                _g.RoundRect(_g.W - P(232) + P(i * 15), by + P(22) - P((i + 1) * 5),
                             P(10), P((i + 1) * 5), P(3), white);
        }

        private void DrawLock()
        {
            DrawWallpaper(0.10f + 0.35f * _unlockProgress);
            DrawStatusBar();

            var white = new Color32(255, 255, 255, 255);
            int cx = _g.W / 2;
            int shift = P(_unlockProgress * -90f);

            var now = DateTime.Now;
            _g.Text(now.ToString("dddd, d MMMM"), cx, P(300) + shift, 0.52f * K,
                    new Color32(235, 235, 245, 255), Gfx.Align.Center);
            _g.Text(now.ToString("HH:mm"), cx, P(350) + shift, 2.55f * K, white, Gfx.Align.Center);

            // подсказка разблокировки
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.2f);
            _g.Text("Свайп вверх для разблокировки", cx, _g.H - P(200), 0.40f * K,
                    new Color32(240, 240, 250, 255), Gfx.Align.Center, 0.55f + 0.35f * pulse);
            _g.RoundRect(cx - P(90), _g.H - P(60), P(180), P(9), P(5), white, 0.9f);
        }

        private int DockH => P(210);

        private void DrawHome()
        {
            DrawWallpaper(0.05f);
            DrawStatusBar();

            // док
            int dockY = _g.H - DockH - P(60);
            _g.RoundRect(P(28), dockY, _g.W - P(56), DockH, P(56), new Color32(255, 255, 255, 46));

            for (int i = 0; i < Installed.Count && i < 4; i++)
            {
                var r = IconRect(i);
                _g.BlitRounded(Installed[i].Icon, r.x, r.y, r.width, r.height,
                               Mathf.RoundToInt(r.width * 0.235f));
                _g.Text(Installed[i].Title, r.x + r.width / 2, r.y + r.height + P(12), 0.32f * K,
                        new Color32(255, 255, 255, 255), Gfx.Align.Center);
            }

            _g.RoundRect(_g.W / 2 - P(90), _g.H - P(46), P(180), P(9), P(5),
                         new Color32(255, 255, 255, 255), 0.85f);
        }

        private RectInt IconRect(int i)
        {
            int n = Mathf.Max(1, Mathf.Min(Installed.Count, 4));
            int size = P(132);
            int gap = (_g.W - P(80) - n * size) / Mathf.Max(1, n + 1);
            int x = P(40) + gap + i * (size + gap);
            int y = _g.H - DockH - P(60) + P(30);
            return new RectInt(x, y, size, size);
        }

        private int DockHitTest(Vector2 p)
        {
            for (int i = 0; i < Installed.Count && i < 4; i++)
            {
                var r = IconRect(i);
                if (p.x >= r.x - P(14) && p.x <= r.x + r.width + P(14) &&
                    p.y >= r.y - P(14) && p.y <= r.y + r.height + P(26))
                    return i;
            }
            return -1;
        }

        private void DrawStub()
        {
            _g.Clear(new Color32(16, 17, 22, 255));
            DrawStatusBar();
            _g.Text(Current.ToString(), _g.W / 2, P(420), 1.05f * K,
                    new Color32(255, 255, 255, 255), Gfx.Align.Center);
            _g.Text("в разработке", _g.W / 2, P(520), 0.42f * K,
                    new Color32(150, 155, 170, 255), Gfx.Align.Center);
            _g.RoundRect(_g.W / 2 - P(90), _g.H - P(46), P(180), P(9), P(5),
                         new Color32(255, 255, 255, 255), 0.85f);
        }
    }
}
