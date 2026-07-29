using System;
using System.Collections.Generic;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// «Операционка» телефона: экран блокировки, рабочий стол, роутинг в
    /// приложения, режим редактирования иконок, звонки поверх всего.
    ///
    /// Перерисовка ленивая: экраны сами зовут Invalidate(), а кадр собирается
    /// не чаще 24 раз в секунду, и в текстуру уходят только изменившиеся полосы.
    /// Обои лежат в кэше слоёв Gfx, поэтому фон
    /// восстанавливается memcpy, а не попиксельным блитом.
    /// </summary>
    internal sealed class PhoneOS
    {
        public readonly PhoneInstance Phone;
        private readonly Gfx _g;
        public Gfx G => _g;

        public PhoneAudio Audio;
        public CameraRig Cam;

        public bool Locked = true;
        public PhoneApp Active;

        public readonly CameraApp AppCamera;
        public readonly GalleryApp AppGallery;
        public readonly StoreApp AppStore;
        public readonly MessengerApp AppMessenger;
        private readonly List<PhoneApp> _all = new List<PhoneApp>();

        public readonly List<PhoneApp> Dock = new List<PhoneApp>();
        public readonly List<PhoneApp> Grid = new List<PhoneApp>();

        // Распакованные картинки общие на все телефоны: иначе каждый экземпляр
        // тащил бы свою копию обоев на полтора мегабайта.
        private static TexData _wallShared, _icCam, _icGal, _icStore, _icMsg;
        private TexData _wall;
        private string _lastClock = "";
        private float _clockAt;

        private bool _need = true;
        private float _nextPaint;

        // жесты
        private bool _dragging;
        private Vector2 _dragStart, _dragNow;
        private float _unlock;

        // редактирование рабочего стола
        public bool Editing;
        private PhoneApp _carry;
        private Vector2 _carryAt;

        // баннер уведомления
        private string _banTitle, _banText;
        private float _banUntil;

        // приём звонка свайпом
        private float _answer;

        public PhoneOS(PhoneInstance phone)
        {
            Phone = phone;
            _g = phone.Screen;

            Font.Load();
            if (_wallShared == null)
            {
                _wallShared = TexData.FromTexture(Assets.Wallpaper);
                _icCam = TexData.FromTexture(Assets.IconCamera);
                _icGal = TexData.FromTexture(Assets.IconGallery);
                _icStore = TexData.FromTexture(Assets.IconStore);
                _icMsg = TexData.FromTexture(Assets.IconMessenger);
            }
            _wall = _wallShared;

            Cam = new CameraRig(phone);
            Audio = new PhoneAudio(phone.Root);

            AppCamera = new CameraApp { Icon = _icCam };
            AppGallery = new GalleryApp { Icon = _icGal };
            AppStore = new StoreApp { Icon = _icStore };
            AppMessenger = new MessengerApp { Icon = _icMsg };

            foreach (var a in new PhoneApp[] { AppCamera, AppGallery, AppStore, AppMessenger })
            {
                a.OS = this; a.G = _g;
                _all.Add(a);
            }

            // как просили: в доке камера, галерея и стор; мессенджер ставится из стора
            Dock.Add(AppCamera);
            Dock.Add(AppGallery);
            Dock.Add(AppStore);

            LNet.Changed += Invalidate;
            LNet.CallChanged += OnCallChanged;
            LNet.NewMessage += OnNewMessage;
            PhotoStore.Changed += Invalidate;
        }

        public void Dispose()
        {
            LNet.Changed -= Invalidate;
            LNet.CallChanged -= OnCallChanged;
            LNet.NewMessage -= OnNewMessage;
            PhotoStore.Changed -= Invalidate;
            try { Cam?.Destroy(); } catch { }
        }

        public IEnumerable<PhoneApp> AllApps => _all;
        public bool IsInstalled(PhoneApp a) => Dock.Contains(a) || Grid.Contains(a);

        public void Install(PhoneApp a)
        {
            if (a == null || IsInstalled(a)) return;
            if (Dock.Count < 4) Dock.Add(a); else Grid.Add(a);
            Invalidate();
        }

        public void Uninstall(PhoneApp a)
        {
            if (a == null) return;
            Dock.Remove(a); Grid.Remove(a);
            if (Active == a) GoHome();
            Invalidate();
        }

        // ─────────────────────── вёрстка ───────────────────────

        private float K => _g.W / 742f;
        private int P(float v) => Mathf.RoundToInt(v * K);
        private int W => _g.W;
        private int H => _g.H;

        /// <summary>Насколько нужно провести вверх, чтобы разблокировать (≈2.4 см).</summary>
        private int UnlockDist => P(240);

        public void Invalidate() => _need = true;

        // ─────────────────────── жизненный цикл ───────────────────────

        public void Tick()
        {
            try
            {
                LNet.Tick();

                if (Time.unscaledTime >= _clockAt)
                {
                    _clockAt = Time.unscaledTime + 0.5f;
                    string c = DateTime.Now.ToString("HH:mm");
                    if (c != _lastClock) { _lastClock = c; Invalidate(); }
                }

                TickCall();

                if (LNet.State == CallState.None && Active != null) Active.Tick();

                if (_banUntil > 0f && Time.unscaledTime > _banUntil) { _banUntil = 0f; Invalidate(); }

                if (_need && Time.unscaledTime >= _nextPaint)
                {
                    _nextPaint = Time.unscaledTime + 1f / 24f;
                    _need = false;
                    Paint();
                }

                _g.Present();
            }
            catch (Exception e) { MelonLoader.MelonLogger.Warning("[LPhone] OS: " + e.Message); }
        }

        public void GoHome()
        {
            if (Active != null) { Active.Close(); Active = null; }
            Editing = false;
            _carry = null;
            Invalidate();
        }

        public void Launch(PhoneApp a)
        {
            if (a == null) return;
            if (Active != null && Active != a) Active.Close();
            Active = a;
            Editing = false;
            Locked = false;
            a.Open();
            Audio?.Click();
            Invalidate();
        }

        public void Lock()
        {
            if (Active != null) { Active.Close(); Active = null; }
            Locked = true;
            Editing = false;
            _unlock = 0f;
            Invalidate();
        }

        public void Notify(string title, string text)
        {
            _banTitle = title; _banText = text;
            _banUntil = Time.unscaledTime + 3.5f;
            Invalidate();
        }

        private void OnNewMessage(byte from)
        {
            var c = Contacts.ByID(from);
            Audio?.Ding();
            if (!(Active == AppMessenger && AppMessenger.IsChatWith(from)))
                Notify(c.Name, LastPreview(from));
        }

        private static string LastPreview(byte sid)
        {
            var l = LNet.Chat(sid);
            if (l.Count == 0) return "";
            var m = l[l.Count - 1];
            return m.Image != null ? "Фото" : m.Text;
        }

        private void OnCallChanged()
        {
            Invalidate();
            if (LNet.State == CallState.Incoming) { _answer = 0f; Audio?.RingStart(); }
            else if (LNet.State == CallState.Outgoing) Audio?.DialStart();
            else if (LNet.State == CallState.Active)
            {
                Audio?.LoopStop();
                FusionVoice.BeginCall();
                FusionVoice.SetMuted(!LNet.MicOn);
                FusionVoice.SetDeafened(!LNet.SpeakerOn);
            }
            else
            {
                Audio?.LoopStop();
                Audio?.Hangup();
                FusionVoice.EndCall();
                Cam?.Disable();
            }
        }

        private void TickCall()
        {
            if (LNet.State == CallState.None) return;
            Invalidate();      // таймер и анимация звонка

            if (LNet.State == CallState.Active && LNet.CamOn && Cam != null)
            {
                Cam.Enable();
                var jpg = Cam.VideoFrame(4f);
                if (jpg != null) LNet.SendVideoFrame(jpg);
            }
        }

        // ─────────────────────── ввод ───────────────────────

        public void TouchDown(Vector2 p)
        {
            _dragging = true;
            _dragStart = _dragNow = p;

            if (LNet.State != CallState.None) return;
            if (Locked) return;

            if (Active != null) { Active.Down(p); return; }

            if (Editing)
            {
                var a = HitApp(p);
                if (a != null) { _carry = a; _carryAt = p; }
            }
        }

        public void TouchMove(Vector2 p)
        {
            if (!_dragging) return;
            _dragNow = p;

            if (LNet.State == CallState.Incoming)
            {
                float dx = p.x - _dragStart.x;
                _answer = Mathf.Clamp01(dx / P(360));
                Invalidate();
                return;
            }
            if (LNet.State != CallState.None) return;

            if (Locked)
            {
                // Прогресс считаем по тому же порогу, что и срабатывание, иначе
                // индикатор доходил только до 57% в момент разблокировки.
                float dy = _dragStart.y - p.y;
                float nu = Mathf.Clamp01(dy / UnlockDist);
                if (Mathf.Abs(nu - _unlock) > 0.02f) { _unlock = nu; Invalidate(); }
                return;
            }

            if (Active != null) { Active.Move(p); return; }

            if (_carry != null) { _carryAt = p; Invalidate(); }
        }

        public void TouchUp(Vector2 p)
        {
            if (!_dragging) return;
            _dragging = false;
            Vector2 d = p - _dragStart;

            // ── звонок поверх всего
            if (LNet.State != CallState.None)
            {
                UpCall(p, d);
                return;
            }

            // ── экран блокировки
            if (Locked)
            {
                // Начать свайп можно с любой точки ниже верхней трети — жёсткая
                // «нижняя половина» отсекала привычный свайп от середины экрана.
                if (_dragStart.y > H * 0.34f && -d.y > UnlockDist)
                {
                    Locked = false;
                    Audio?.Click();
                }
                _unlock = 0f;
                Invalidate();
                return;
            }

            // ── перенос иконки
            if (_carry != null)
            {
                DropCarried(p);
                _carry = null;
                Invalidate();
                return;
            }

            // ── приложение
            if (Active != null)
            {
                Active.Up(p, d);
                // жест «домой» снизу вверх работает в любом приложении
                if (d.y < -P(220) && _dragStart.y > H - P(150))
                {
                    if (!Active.Back()) GoHome();
                }
                return;
            }

            // ── рабочий стол
            if (d.magnitude < P(40))
            {
                if (Editing)
                {
                    // крестик удаления
                    var del = HitDelete(p);
                    if (del != null) { Uninstall(del); Audio?.Click(); return; }
                    var hit = HitApp(p);
                    if (hit == null) { Editing = false; Invalidate(); }
                    return;
                }
                var a = HitApp(p);
                if (a != null) Launch(a);
            }
            else if (d.y > P(300) && _dragStart.y < P(220)) Lock();
        }

        public void LongPress(Vector2 p)
        {
            if (LNet.State != CallState.None || Locked) return;
            if (Active != null) { Active.LongPress(p); return; }
            if (!Editing)
            {
                var a = HitApp(p);
                if (a != null)
                {
                    Editing = true;
                    _carry = a;
                    _carryAt = p;
                    Audio?.Click();
                    Invalidate();
                }
            }
        }

        private void UpCall(Vector2 p, Vector2 d)
        {
            if (LNet.State == CallState.Incoming)
            {
                if (_answer > 0.75f) LNet.Accept();
                else if (In(p, DeclineBtn())) LNet.Decline();
                _answer = 0f;
                Invalidate();
                return;
            }
            if (LNet.State == CallState.Outgoing)
            {
                if (In(p, EndBtn())) LNet.EndCall();
                return;
            }
            // активный разговор
            if (In(p, EndBtn())) { LNet.EndCall(); return; }
            if (In(p, CallBtn(0)))
            {
                LNet.SpeakerOn = !LNet.SpeakerOn;
                FusionVoice.SetDeafened(!LNet.SpeakerOn);
                Audio?.Click(); Invalidate(); return;
            }
            if (In(p, CallBtn(1)))
            {
                LNet.MicOn = !LNet.MicOn;
                FusionVoice.SetMuted(!LNet.MicOn);
                Audio?.Click(); Invalidate(); return;
            }
            if (In(p, CallBtn(2)))
            {
                LNet.CamOn = !LNet.CamOn;
                if (!LNet.CamOn) Cam?.Disable();
                Audio?.Click();
                Invalidate();
            }
        }

        private static bool In(Vector2 p, RectInt r) =>
            p.x >= r.x && p.x <= r.x + r.width && p.y >= r.y && p.y <= r.y + r.height;

        // ─────────────────────── рабочий стол ───────────────────────

        private int IconSize => P(132);
        private int DockH => P(210);
        private int DockY => H - DockH - P(60);

        private RectInt DockSlot(int i)
        {
            int n = 4;
            int size = IconSize;
            int gap = (W - P(80) - n * size) / (n + 1);
            return new RectInt(P(40) + gap + i * (size + gap), DockY + P(30), size, size);
        }

        private RectInt GridSlot(int i)
        {
            int col = i % 4, row = i / 4;
            int size = IconSize;
            int gap = (W - P(56) - 4 * size) / 5;
            int x = P(28) + gap + col * (size + gap);
            int y = P(210) + row * (size + P(74));
            return new RectInt(x, y, size, size);
        }

        private PhoneApp HitApp(Vector2 p)
        {
            for (int i = 0; i < Grid.Count; i++)
                if (Near(p, GridSlot(i))) return Grid[i];
            for (int i = 0; i < Dock.Count; i++)
                if (Near(p, DockSlot(i))) return Dock[i];
            return null;
        }

        private bool Near(Vector2 p, RectInt r) =>
            p.x >= r.x - P(14) && p.x <= r.x + r.width + P(14) &&
            p.y >= r.y - P(14) && p.y <= r.y + r.height + P(30);

        private PhoneApp HitDelete(Vector2 p)
        {
            int rr = P(30);
            for (int i = 0; i < Grid.Count; i++)
            {
                var s = GridSlot(i);
                if (Vector2.Distance(p, new Vector2(s.x, s.y)) < rr) return Grid[i];
            }
            for (int i = 0; i < Dock.Count; i++)
            {
                var s = DockSlot(i);
                if (Vector2.Distance(p, new Vector2(s.x, s.y)) < rr) return Dock[i];
            }
            return null;
        }

        private void DropCarried(Vector2 p)
        {
            var a = _carry;
            Dock.Remove(a); Grid.Remove(a);

            if (p.y > DockY - P(20) && Dock.Count < 4)
            {
                int idx = 0;
                for (int i = 0; i < 4; i++) if (p.x > DockSlot(i).x + IconSize / 2) idx = i + 1;
                Dock.Insert(Mathf.Clamp(idx, 0, Dock.Count), a);
            }
            else
            {
                int best = Grid.Count;
                for (int i = 0; i < Grid.Count; i++)
                {
                    var s = GridSlot(i);
                    if (p.y < s.y + IconSize / 2 && p.x < s.x + IconSize / 2) { best = i; break; }
                }
                Grid.Insert(Mathf.Clamp(best, 0, Grid.Count), a);
            }
        }

        // ─────────────────────── отрисовка ───────────────────────

        private void Paint()
        {
            if (LNet.State != CallState.None) { DrawCall(); return; }
            if (Locked) { DrawLock(); DrawBanner(); return; }
            if (Active != null) { Active.Draw(); DrawHomeBar(); DrawBanner(); return; }
            DrawHome();
            DrawBanner();
        }

        /// <summary>Обои с затемнением — из кэша слоёв, поэтому это memcpy.</summary>
        public void DrawWallpaper(float dim)
        {
            int key = Mathf.RoundToInt(dim * 100f);
            if (_g.RestoreLayer(key)) return;
            if (_wall != null) _g.Blit(_wall, 0, 0, W, H);
            else _g.Clear(new Color32(18, 12, 28, 255));
            if (dim > 0f) _g.Rect(0, 0, W, H, new Color32(0, 0, 0, 255), dim);
            _g.StoreLayer(key);
        }

        public void DrawStatusBar(bool light = true)
        {
            var col = light ? UI.White : new Color32(20, 20, 22, 255);
            _g.Text(DateTime.Now.ToString("HH:mm"), P(92), P(46), 0.44f * K, col);

            int bx = W - P(128), by = P(50);
            _g.RoundRect(bx, by, P(56), P(26), P(8), new Color32(col.r, col.g, col.b, 90));
            _g.RoundRect(bx + P(3), by + P(3), P(44), P(20), P(5), col);
            _g.RoundRect(bx + P(60), by + P(9), P(5), P(10), P(2), new Color32(col.r, col.g, col.b, 90));

            for (int i = 0; i < 4; i++)
                _g.RoundRect(W - P(232) + P(i * 15), by + P(22) - P((i + 1) * 5),
                             P(10), P((i + 1) * 5), P(3), col);
        }

        public void DrawHomeBar()
        {
            _g.RoundRect(W / 2 - P(90), H - P(46), P(180), P(9), P(5), UI.White, 0.85f);
        }

        private void DrawLock()
        {
            DrawWallpaper(0.12f);
            DrawStatusBar();

            int cx = W / 2;
            int shift = P(_unlock * -70f);
            var now = DateTime.Now;

            _g.Text(now.ToString("dddd, d MMMM"), cx, P(290) + shift, 0.52f * K,
                    new Color32(235, 235, 245, 255), Gfx.Align.Center);
            _g.Text(now.ToString("HH:mm"), cx, P(340) + shift, 2.55f * K, UI.White, Gfx.Align.Center);

            int un = LNet.UnreadTotal();
            if (un > 0)
            {
                int cy = P(700);
                _g.RoundRect(P(50), cy, W - P(100), P(150), P(40), new Color32(255, 255, 255, 42));
                UI.Bolt(_g, P(110), cy + P(75), P(70), UI.White);
                _g.Text("Messenger", P(165), cy + P(34), 0.40f * K, new Color32(230, 232, 240, 255));
                _g.Text(un == 1 ? "1 новое сообщение" : un + " новых сообщений",
                        P(165), cy + P(80), 0.38f * K, UI.White);
            }

            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 2.2f);
            _g.Text("Свайп вверх", cx, H - P(190), 0.40f * K,
                    new Color32(240, 240, 250, 255), Gfx.Align.Center, 0.45f + 0.4f * pulse);
            DrawHomeBar();
        }

        private void DrawHome()
        {
            DrawWallpaper(0.06f);
            DrawStatusBar();

            float wig = Editing ? Mathf.Sin(Time.unscaledTime * 11f) * P(3) : 0f;

            _g.RoundRect(P(28), DockY, W - P(56), DockH, P(56), new Color32(255, 255, 255, 46));

            for (int i = 0; i < Grid.Count; i++)
            {
                if (Grid[i] == _carry) continue;
                DrawIcon(Grid[i], GridSlot(i), wig);
            }
            for (int i = 0; i < Dock.Count; i++)
            {
                if (Dock[i] == _carry) continue;
                DrawIcon(Dock[i], DockSlot(i), wig);
            }

            if (_carry != null)
            {
                var s = new RectInt((int)_carryAt.x - IconSize / 2, (int)_carryAt.y - IconSize / 2,
                                    IconSize, IconSize);
                DrawIcon(_carry, s, 0f, 0.9f);
            }

            if (Editing)
                _g.Text("Перетащи иконку · крестик удаляет", W / 2, H - P(660), 0.34f * K,
                        new Color32(240, 240, 250, 220), Gfx.Align.Center);

            DrawHomeBar();
        }

        private void DrawIcon(PhoneApp a, RectInt r, float wiggle, float alpha = 1f)
        {
            int x = r.x + Mathf.RoundToInt(wiggle), y = r.y;
            if (a.Icon != null)
                _g.BlitRounded(a.Icon, x, y, r.width, r.height, Mathf.RoundToInt(r.width * 0.235f), alpha);
            else
                _g.RoundRect(x, y, r.width, r.height, Mathf.RoundToInt(r.width * 0.235f), a.Tile, alpha);

            _g.Text(a.Title, x + r.width / 2, y + r.height + P(12), 0.32f * K, UI.White, Gfx.Align.Center, alpha);

            if (a == AppMessenger)
            {
                int un = LNet.UnreadTotal();
                if (un > 0)
                {
                    int bx = x + r.width - P(16), by = y + P(16);
                    _g.Circle(bx, by, P(26), UI.Red);
                    _g.Text(un > 99 ? "99+" : un.ToString(), bx, by - P(20), 0.34f * K, UI.White, Gfx.Align.Center);
                }
            }

            if (Editing)
            {
                _g.Circle(x, y, P(26), new Color32(230, 230, 235, 255));
                UI.Cross(_g, x, y, P(24), Mathf.Max(2, P(6)), new Color32(30, 30, 34, 255));
            }
        }

        private void DrawBanner()
        {
            if (_banUntil <= 0f || Time.unscaledTime > _banUntil) return;
            float life = _banUntil - Time.unscaledTime;
            float a = Mathf.Clamp01(Mathf.Min(life, 0.3f) / 0.3f);

            int y = P(96);
            _g.RoundRect(P(30), y, W - P(60), P(150), P(42), new Color32(38, 38, 44, 245), a);
            UI.Bolt(_g, P(100), y + P(75), P(64), new Color32(255, 255, 255, 255));
            _g.Text(_banTitle ?? "", P(160), y + P(30), 0.40f * K, UI.White, Gfx.Align.Left, a);
            _g.Text(Trim(_banText, 26), P(160), y + P(80), 0.36f * K, UI.Dim, Gfx.Align.Left, a);
        }

        public static string Trim(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n - 1) + "…";
        }

        // ─────────────────────── звонки ───────────────────────

        private RectInt DeclineBtn() => new RectInt(W / 2 - P(240), H - P(300), P(160), P(160));
        private RectInt EndBtn() => new RectInt(W / 2 - P(80), H - P(300), P(160), P(160));
        private RectInt CallBtn(int i)
        {
            int size = P(150);
            int gap = (W - P(80) - 3 * size) / 4;
            return new RectInt(P(40) + gap + i * (size + gap), H - P(530), size, size);
        }

        private void DrawCall()
        {
            var c = Contacts.ByID(LNet.Peer);
            DrawWallpaper(0.62f);
            DrawStatusBar();

            switch (LNet.State)
            {
                case CallState.Incoming: DrawIncoming(c); break;
                case CallState.Outgoing: DrawOutgoing(c); break;
                default: DrawActive(c); break;
            }
        }

        private void DrawIncoming(Contact c)
        {
            int cx = W / 2;
            UI.Avatar(_g, cx, P(430), P(160), c, K);
            _g.Text(c.Name, cx, P(640), 0.72f * K, UI.White, Gfx.Align.Center);
            _g.Text(LNet.Video ? "Входящий видеовызов" : "Входящий вызов",
                    cx, P(720), 0.42f * K, new Color32(210, 212, 225, 255), Gfx.Align.Center);

            // слайдер «свайп чтобы ответить»
            int y = H - P(300), h = P(160);
            _g.RoundRect(P(40), y, W - P(80), h, h / 2, new Color32(255, 255, 255, 40));
            int knob = P(40) + h / 2 + Mathf.RoundToInt(_answer * (W - P(80) - h));
            _g.Circle(knob, y + h / 2, h / 2 - P(8), UI.Green);
            UI.PhoneGlyph(_g, knob, y + h / 2, P(80), UI.White, false);

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            _g.Text("Свайп вправо — ответить", cx + P(50), y + h / 2 - P(18), 0.36f * K,
                    UI.White, Gfx.Align.Center, 0.35f + 0.4f * pulse);

            var d = DeclineBtn();
            _g.Circle(d.x + d.width / 2, d.y + d.height / 2, d.width / 2, UI.Red);
            UI.PhoneGlyph(_g, d.x + d.width / 2, d.y + d.height / 2, P(80), UI.White, true);
        }

        private void DrawOutgoing(Contact c)
        {
            int cx = W / 2;
            UI.Avatar(_g, cx, P(430), P(160), c, K);
            _g.Text(c.Name, cx, P(640), 0.72f * K, UI.White, Gfx.Align.Center);

            int dots = (int)(Time.unscaledTime * 2f) % 4;
            _g.Text("Вызов" + new string('.', dots), cx, P(720), 0.42f * K,
                    new Color32(210, 212, 225, 255), Gfx.Align.Center);

            var e = EndBtn();
            _g.Circle(e.x + e.width / 2, e.y + e.height / 2, e.width / 2, UI.Red);
            UI.PhoneGlyph(_g, e.x + e.width / 2, e.y + e.height / 2, P(80), UI.White, true);
        }

        private void DrawActive(Contact c)
        {
            int cx = W / 2;

            if (LNet.Video && LNet.RemoteFrame != null && Time.unscaledTime - LNet.RemoteFrameAt < 6f)
            {
                // кадр собеседника на всю верхнюю часть
                int vh = P(880);
                _g.Blit(LNet.RemoteFrame, 0, P(120), W, vh);
                _g.Rect(0, P(120), W, P(120), new Color32(0, 0, 0, 255), 0.35f);
                _g.Text(c.Name, P(40), P(150), 0.46f * K, UI.White);
                _g.Text(Timer(), P(40), P(210), 0.36f * K, new Color32(220, 222, 235, 255));
            }
            else
            {
                UI.Avatar(_g, cx, P(400), P(150), c, K);
                _g.Text(c.Name, cx, P(600), 0.68f * K, UI.White, Gfx.Align.Center);
                _g.Text(Timer(), cx, P(680), 0.42f * K, new Color32(210, 212, 225, 255), Gfx.Align.Center);
                if (LNet.Video)
                    _g.Text("Ожидание видео…", cx, P(740), 0.34f * K, UI.Dim, Gfx.Align.Center);
            }

            DrawCallBtn(0, LNet.SpeakerOn, "Динамик");
            DrawCallBtn(1, LNet.MicOn, "Микрофон");
            DrawCallBtn(2, LNet.CamOn, "Камера");

            var e = EndBtn();
            _g.Circle(e.x + e.width / 2, e.y + e.height / 2, e.width / 2, UI.Red);
            UI.PhoneGlyph(_g, e.x + e.width / 2, e.y + e.height / 2, P(80), UI.White, true);
        }

        private void DrawCallBtn(int i, bool on, string label)
        {
            var r = CallBtn(i);
            int cx = r.x + r.width / 2, cy = r.y + r.height / 2;
            _g.Circle(cx, cy, r.width / 2, on ? new Color32(255, 255, 255, 235) : new Color32(255, 255, 255, 48));
            var fg = on ? new Color32(20, 20, 24, 255) : UI.White;
            switch (i)
            {
                case 0: UI.SpeakerGlyph(_g, cx, cy, P(76), fg, on); break;
                case 1: UI.MicGlyph(_g, cx, cy, P(76), fg, on); break;
                default: UI.CamGlyph(_g, cx, cy, P(76), fg); break;
            }
            _g.Text(label, cx, r.y + r.height + P(10), 0.30f * K, UI.White, Gfx.Align.Center);
        }

        private static string Timer()
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - LNet.CallStarted));
            return $"{s / 60:00}:{s % 60:00}";
        }
    }
}
