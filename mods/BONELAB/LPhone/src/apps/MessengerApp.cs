using System;
using System.Collections.Generic;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Messenger: контакты = игроки лобби (аватар + ник), переписка, отправка
    /// фото из галереи, голосовые и видеозвонки. Первым контактом всегда
    /// закреплён автор — карточка канала @be_primex с меткой AUTHOR.
    /// </summary>
    internal sealed class MessengerApp : PhoneApp
    {
        public override string Id => "messenger";
        public override string Title => "Messenger";
        public override Color32 Tile => new Color32(0, 122, 255, 255);

        private enum Mode { Contacts, Chat, Channel }
        private Mode _mode = Mode.Contacts;
        private byte _peer;
        private int _scroll;
        private readonly Keyboard _kb = new Keyboard();

        /// <summary>Фото, которое пользователь отправил из галереи.</summary>
        public Photo PendingPhoto;

        private string _toast;
        private float _toastUntil;

        public bool IsChatWith(byte sid) => _mode == Mode.Chat && _peer == sid;

        public override void Open()
        {
            _kb.OnSend = Send;
            _kb.OnChanged = () => OS.Invalidate();
            if (PendingPhoto == null) _mode = Mode.Contacts;
            _scroll = 0;
            OS.Invalidate();
        }

        public override void Close() { _scroll = 0; }

        public override void Tick()
        {
            if (_toastUntil > 0f && Time.unscaledTime > _toastUntil) { _toastUntil = 0f; OS.Invalidate(); }
        }

        private void Toast(string s)
        {
            _toast = s;
            _toastUntil = Time.unscaledTime + 4f;
            OS.Invalidate();
        }

        public override bool Back()
        {
            if (_mode != Mode.Contacts) { _mode = Mode.Contacts; _scroll = 0; OS.Invalidate(); return true; }
            return false;
        }

        // ─────────────── вёрстка ───────────────

        private int RowH => P(150);
        private int ListTop => P(250);
        private RectInt Row(int i) => new RectInt(0, ListTop + i * RowH - _scroll, W, RowH);

        private RectInt BtnBack() => R(16f, 110f, 150f, 110f);
        private RectInt BtnCall() => R(742f - 300f, 110f, 120f, 110f);
        private RectInt BtnVideo() => R(742f - 165f, 110f, 120f, 110f);
        private RectInt BtnPlus() => R(20f, 940f, 110f, 92f);
        private int InputTop => P(940);
        private int ChatTop => P(250);
        private int ChatBottom => P(930);

        public override void Draw()
        {
            G.Clear(UI.Bg);
            OS.DrawStatusBar();
            switch (_mode)
            {
                case Mode.Chat: DrawChat(); break;
                case Mode.Channel: DrawChannel(); break;
                default: DrawContacts(); break;
            }
        }

        // ─────────────── контакты ───────────────

        private void DrawContacts()
        {
            G.Text("Messenger", P(40), P(120), 0.74f * K, UI.White);

            if (PendingPhoto != null)
            {
                G.Text("Кому отправить фото?", P(40), P(196), 0.34f * K, UI.Blue);
            }
            else if (!FusionBridge.InSession)
            {
                G.Text("Не в лобби — только канал автора", P(40), P(196), 0.32f * K, UI.Dim);
            }
            else
            {
                G.Text("В лобби: " + Mathf.Max(0, Contacts.All().Count - 1), P(40), P(196), 0.32f * K, UI.Dim);
            }

            var list = Contacts.All();
            for (int i = 0; i < list.Count; i++)
            {
                var r = Row(i);
                if (r.y + r.height < ListTop || r.y > H) continue;
                DrawContactRow(list[i], r);
            }

            OS.DrawHomeBar();
        }

        private void DrawContactRow(Contact c, RectInt r)
        {
            int av = P(52);
            int cy = r.y + r.height / 2;
            UI.Avatar(G, P(90), cy, av, c, K);

            int tx = P(170);
            G.Text(PhoneOS.Trim(c.Name, 16), tx, cy - P(50), 0.44f * K, UI.White);

            if (c.IsAuthor)
            {
                int bw = P(150);
                G.RoundRect(tx + P(20) + G.TextWidth(PhoneOS.Trim(c.Name, 16), 0.44f * K), cy - P(46),
                            bw, P(46), P(16), new Color32(0, 122, 255, 255));
                G.Text("AUTHOR", tx + P(20) + G.TextWidth(PhoneOS.Trim(c.Name, 16), 0.44f * K) + bw / 2,
                       cy - P(38), 0.26f * K, UI.White, Gfx.Align.Center);
                G.Text("@be_primex · канал", tx, cy + P(6), 0.32f * K, UI.Dim);
            }
            else
            {
                string prev = Preview(c.SmallID);
                G.Text(prev.Length > 0 ? PhoneOS.Trim(prev, 28) : (c.IsHost ? "Хост лобби" : "В лобби"),
                       tx, cy + P(6), 0.32f * K, UI.Dim);

                if (c.IsHost)
                {
                    G.RoundRect(W - P(120), cy - P(46), P(80), P(40), P(14), new Color32(255, 214, 10, 220));
                    G.Text("HOST", W - P(80), cy - P(40), 0.24f * K, new Color32(20, 20, 24, 255), Gfx.Align.Center);
                }

                int un = LNet.Unread(c.SmallID);
                if (un > 0)
                {
                    G.Circle(W - P(70), cy + P(16), P(26), UI.Blue);
                    G.Text(un > 9 ? "9+" : un.ToString(), W - P(70), cy + P(2), 0.30f * K, UI.White, Gfx.Align.Center);
                }
            }

            G.Rect(P(170), r.y + r.height - 1, W - P(170), 1, new Color32(48, 48, 54, 255));
        }

        private static string Preview(byte sid)
        {
            var l = LNet.Chat(sid);
            if (l.Count == 0) return "";
            var m = l[l.Count - 1];
            string s = m.Image != null ? "Фото" : m.Text;
            return (m.Mine ? "Ты: " : "") + s;
        }

        // ─────────────── чат ───────────────

        private void DrawChat()
        {
            var c = Contacts.ByID(_peer);

            G.Rect(0, 0, W, ChatTop - P(10), new Color32(22, 22, 26, 255));
            OS.DrawStatusBar();          // шапка чата закрасила бы строку состояния
            var b = BtnBack();
            UI.BackArrow(G, b.x + P(28), b.y + P(22), P(34), UI.Blue);

            UI.Avatar(G, P(210), P(165), P(46), c, K);
            G.Text(PhoneOS.Trim(c.Name, 14), P(272), P(140), 0.42f * K, UI.White);
            G.Text(FusionBridge.InSession ? "в лобби" : "не в сети", P(272), P(190), 0.28f * K, UI.Dim);

            var cb = BtnCall();
            G.Circle(cb.x + cb.width / 2, cb.y + cb.height / 2, cb.width / 2, new Color32(40, 40, 46, 255));
            UI.PhoneGlyph(G, cb.x + cb.width / 2, cb.y + cb.height / 2, P(56), UI.Green, false);

            var vb = BtnVideo();
            G.Circle(vb.x + vb.width / 2, vb.y + vb.height / 2, vb.width / 2, new Color32(40, 40, 46, 255));
            UI.CamGlyph(G, vb.x + vb.width / 2, vb.y + vb.height / 2, P(58), UI.Blue);

            // лента сообщений — снизу вверх, свежие внизу
            var list = LNet.Chat(_peer);
            int y = ChatBottom + _scroll;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int h = BubbleH(list[i]);
                y -= h + P(14);
                if (y > ChatBottom) continue;
                if (y + h < ChatTop) break;
                DrawBubble(list[i], y, h);
            }

            if (list.Count == 0)
                G.Text("Напиши первым", W / 2, P(560), 0.38f * K, UI.Dim, Gfx.Align.Center);

            // строка ввода
            G.Rect(0, InputTop - P(12), W, P(112), new Color32(22, 22, 26, 255));
            var pl = BtnPlus();
            G.Circle(pl.x + pl.width / 2, pl.y + pl.height / 2, pl.width / 2, new Color32(48, 48, 56, 255));
            UI.Plus(G, pl.x + pl.width / 2, pl.y + pl.height / 2, P(46), Mathf.Max(3, P(8)), UI.White);

            G.RoundRect(P(150), InputTop, W - P(190), P(92), P(30), new Color32(44, 44, 52, 255));
            string t = _kb.Text;
            if (PendingPhoto != null && t.Length == 0) t = "[фото готово к отправке]";
            G.Text(t.Length > 0 ? PhoneOS.Trim(t, 24) : "Сообщение",
                   P(180), InputTop + P(24), 0.36f * K,
                   t.Length > 0 ? UI.White : new Color32(110, 112, 122, 255));

            _kb.Draw(G, K);
        }

        private int BubbleH(ChatMessage m)
        {
            if (m.Image != null) return P(300);
            int w = Mathf.Min(P(470), G.TextWidth(m.Text ?? "", 0.36f * K) + P(48));
            int lines = Mathf.Max(1, Wrap(m.Text ?? "", w - P(48)).Count);
            return P(40) + lines * G.LineHeight(0.36f * K);
        }

        private void DrawBubble(ChatMessage m, int y, int h)
        {
            int maxW = P(470);
            int w = m.Image != null ? P(230)
                                    : Mathf.Min(maxW, G.TextWidth(m.Text ?? "", 0.36f * K) + P(48));
            int x = m.Mine ? W - P(28) - w : P(28);
            var bg = m.Mine ? UI.Blue : new Color32(44, 44, 52, 255);

            G.RoundRect(x, y, w, h, P(28), bg);

            if (m.Image != null)
            {
                G.BlitRounded(m.Image.Data, x + P(10), y + P(10), w - P(20), h - P(20), P(20));
            }
            else
            {
                // перенос по ширине пузыря
                int lineH = G.LineHeight(0.36f * K);
                int ty = y + P(20);
                foreach (var line in Wrap(m.Text ?? "", w - P(48)))
                {
                    G.Text(line, x + P(24), ty, 0.36f * K, UI.White);
                    ty += lineH;
                }
            }
        }

        private List<string> Wrap(string s, int width)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(s)) { res.Add(""); return res; }
            var cur = "";
            foreach (var word in s.Split(' '))
            {
                string trial = cur.Length == 0 ? word : cur + " " + word;
                if (G.TextWidth(trial, 0.36f * K) <= width) { cur = trial; continue; }
                if (cur.Length > 0) res.Add(cur);
                cur = word;
            }
            if (cur.Length > 0) res.Add(cur);
            if (res.Count == 0) res.Add("");
            return res;
        }

        // ─────────────── карточка автора ───────────────

        public const string Tg = "@be_primex";
        public const string Site = "bonelab.fun";

        private static readonly string[][] Posts =
        {
            new[] { "LPhone 17 PRO MAX", "Телефон целиком внутри BONELAB: камера, галерея, магазин и мессенджер." },
            new[] { "MONSTER Panel", "Панель, которую знают во всех лобби. Обновления выходят первыми в канале." },
        };

        private RectInt LinkTg() => R(30f, 660f, 742f - 60f, 150f);
        private RectInt LinkSite() => R(30f, 828f, 742f - 60f, 150f);

        private void DrawChannel()
        {
            G.Clear(new Color32(13, 15, 19, 255));

            // шапка с градиентом под цвет молнии
            G.VGradient(0, 0, W, P(560), new Color32(14, 58, 128, 255), new Color32(13, 15, 19, 255));
            OS.DrawStatusBar();

            var b = BtnBack();
            UI.BackArrow(G, b.x + P(28), b.y + P(22), P(34), UI.White);

            // аватар с мягким ореолом
            int acx = W / 2, acy = P(320);
            G.Circle(acx, acy, P(112), new Color32(120, 175, 255, 60));
            G.Circle(acx, acy, P(96), new Color32(0, 122, 255, 255));
            G.Circle(acx, acy - P(30), P(70), new Color32(255, 255, 255, 40));
            UI.Bolt(G, acx, acy, P(110), UI.White);

            G.Text("BE PRIME", acx, P(438), 0.70f * K, UI.White, Gfx.Align.Center);

            int bw = P(168), bh = P(50);
            G.RoundRect(acx - bw / 2, P(516), bw, bh, P(18), new Color32(0, 122, 255, 255));
            G.Text("AUTHOR", acx, P(526), 0.28f * K, UI.White, Gfx.Align.Center);

            G.Text("Автор мода · моды для BONELAB", acx, P(590), 0.30f * K,
                   new Color32(160, 168, 186, 255), Gfx.Align.Center);

            // ── соцсети
            var tg = LinkTg();
            G.RoundRect(tg.x, tg.y, tg.width, tg.height, P(36), new Color32(23, 27, 36, 255));
            int tcx = tg.x + P(84), tcy = tg.y + tg.height / 2;
            G.Circle(tcx, tcy, P(48), new Color32(41, 161, 226, 255));
            UI.TelegramGlyph(G, tcx, tcy, P(52), UI.White);
            G.Text("Telegram", tg.x + P(160), tg.y + P(34), 0.40f * K, UI.White);
            G.Text(Tg, tg.x + P(160), tg.y + P(88), 0.36f * K, new Color32(105, 185, 255, 255));
            UI.Chevron(G, tg.x + tg.width - P(70), tcy - P(20), P(34), new Color32(110, 114, 128, 255));

            var st = LinkSite();
            var card = new Color32(23, 27, 36, 255);
            G.RoundRect(st.x, st.y, st.width, st.height, P(36), card);
            int scx = st.x + P(84), scy = st.y + st.height / 2;
            G.Circle(scx, scy, P(48), new Color32(255, 138, 42, 255));
            UI.GlobeGlyph(G, scx, scy, P(56), UI.White, new Color32(255, 138, 42, 255));
            G.Text("Сайт", st.x + P(160), st.y + P(34), 0.40f * K, UI.White);
            G.Text(Site, st.x + P(160), st.y + P(88), 0.36f * K, new Color32(255, 176, 106, 255));
            UI.Chevron(G, st.x + st.width - P(70), scy - P(20), P(34), new Color32(110, 114, 128, 255));

            // ── о чём канал
            int y = P(1012);
            G.Text("В КАНАЛЕ", P(44), y, 0.28f * K, new Color32(120, 126, 142, 255));
            y += P(50);
            for (int i = 0; i < Posts.Length; i++)
            {
                int h = P(196);
                G.RoundRect(P(30), y, W - P(60), h, P(32), card);
                UI.Bolt(G, P(92), y + P(66), P(52), new Color32(96, 166, 255, 255));
                G.Text(Posts[i][0], P(140), y + P(36), 0.38f * K, UI.White);
                int ty = y + P(92);
                foreach (var line in Wrap(Posts[i][1], W - P(180)))
                {
                    G.Text(line, P(60), ty, 0.29f * K, new Color32(168, 174, 190, 255));
                    ty += G.LineHeight(0.29f * K);
                }
                y += h + P(18);
            }

            if (_toastUntil > 0f)
            {
                G.RoundRect(P(40), H - P(230), W - P(80), P(120), P(38), new Color32(0, 0, 0, 225));
                G.Text(_toast, W / 2, H - P(190), 0.42f * K, UI.White, Gfx.Align.Center);
            }

            OS.DrawHomeBar();
        }

        // ─────────────── ввод ───────────────

        public override void Up(Vector2 p, Vector2 d)
        {
            if (_mode == Mode.Contacts)
            {
                if (Mathf.Abs(d.y) > P(60))
                {
                    int total = Contacts.All().Count * RowH;
                    int max = Mathf.Max(0, total - (H - ListTop - P(80)));
                    _scroll = Mathf.Clamp(_scroll - Mathf.RoundToInt(d.y), 0, max);
                    OS.Invalidate();
                    return;
                }
                if (d.magnitude > P(50)) return;

                var list = Contacts.All();
                for (int i = 0; i < list.Count; i++)
                {
                    if (!In(p, Row(i))) continue;
                    var c = list[i];
                    OS.Audio?.Click();
                    if (c.IsAuthor) { _mode = Mode.Channel; _scroll = 0; OS.Invalidate(); return; }

                    _peer = c.SmallID;
                    _mode = Mode.Chat;
                    _scroll = 0;
                    LNet.MarkRead(_peer);
                    if (PendingPhoto != null)
                    {
                        LNet.SendPhoto(_peer, PendingPhoto);
                        PendingPhoto = null;
                    }
                    OS.Invalidate();
                    return;
                }
                return;
            }

            if (_mode == Mode.Channel)
            {
                if (In(p, BtnBack()) || d.x > P(200)) { _mode = Mode.Contacts; OS.Invalidate(); return; }
                if (d.magnitude > P(50)) return;
                // Открыть ссылку из VR нельзя, поэтому показываем адрес крупно,
                // чтобы его можно было спокойно прочитать и набрать на телефоне.
                if (In(p, LinkTg())) { OS.Audio?.Click(); Toast("Telegram: " + Tg); return; }
                if (In(p, LinkSite())) { OS.Audio?.Click(); Toast("Сайт: " + Site); return; }
                return;
            }

            // ── чат
            if (p.y > P(Keyboard.Top - 14f))
            {
                // по клавише засчитываем только тап, а не свайп через клавиатуру
                if (d.magnitude < P(60) && _kb.Tap(p, K, W)) { OS.Audio?.Click(); OS.Invalidate(); }
                return;
            }

            if (Mathf.Abs(d.y) > P(60) && p.y < ChatBottom)
            {
                _scroll = Mathf.Clamp(_scroll - Mathf.RoundToInt(d.y), 0, P(4000));
                OS.Invalidate();
                return;
            }
            if (d.magnitude > P(50)) return;

            if (In(p, BtnBack())) { _mode = Mode.Contacts; _scroll = 0; OS.Invalidate(); return; }
            if (In(p, BtnCall())) { LNet.StartCall(_peer, false); return; }
            if (In(p, BtnVideo())) { LNet.StartCall(_peer, true); return; }
            if (In(p, BtnPlus()))
            {
                byte target = _peer;
                OS.AppGallery.OpenPicker(ph =>
                {
                    if (ph != null) LNet.SendPhoto(target, ph);
                    OS.Launch(this);
                    _mode = Mode.Chat;
                    _peer = target;
                });
                OS.Launch(OS.AppGallery);
            }
        }

        private void Send()
        {
            if (_mode != Mode.Chat) return;
            if (PendingPhoto != null)
            {
                LNet.SendPhoto(_peer, PendingPhoto);
                PendingPhoto = null;
            }
            if (!string.IsNullOrWhiteSpace(_kb.Text))
            {
                LNet.SendText(_peer, _kb.Text.Trim());
                _kb.Text = "";
            }
            _scroll = 0;
            OS.Audio?.Click();
            OS.Invalidate();
        }
    }
}
