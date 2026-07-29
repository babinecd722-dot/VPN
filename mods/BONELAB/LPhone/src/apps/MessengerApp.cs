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

        // ─────────────── канал автора ───────────────

        private static readonly string[][] Posts =
        {
            new[] { "LPhone 17 PRO MAX", "Телефон целиком внутри BONELAB: камера, галерея, магазин и мессенджер." },
            new[] { "MONSTER Panel", "Панель, которую знают во всех лобби. Обновления выходят первыми здесь." },
            new[] { "Подписывайся", "Новые моды, ранние сборки и разборы механик — в канале @be_primex." },
        };

        private void DrawChannel()
        {
            G.Clear(new Color32(14, 16, 20, 255));
            OS.DrawStatusBar();

            var b = BtnBack();
            UI.BackArrow(G, b.x + P(28), b.y + P(22), P(34), UI.Blue);

            // шапка канала
            G.VGradient(0, P(230), W, P(300), new Color32(0, 92, 200, 255), new Color32(14, 16, 20, 255));
            UI.Avatar(G, W / 2, P(330), P(90), Contacts.Author, K);
            G.Text("BE PRIME", W / 2, P(440), 0.64f * K, UI.White, Gfx.Align.Center);

            int bw = P(150);
            G.RoundRect(W / 2 - bw / 2, P(510), bw, P(46), P(16), UI.Blue);
            G.Text("AUTHOR", W / 2, P(518), 0.26f * K, UI.White, Gfx.Align.Center);

            G.Text("@be_primex", W / 2, P(576), 0.38f * K, new Color32(120, 170, 255, 255), Gfx.Align.Center);
            G.Text("Telegram-канал · моды для BONELAB", W / 2, P(628), 0.30f * K, UI.Dim, Gfx.Align.Center);

            int y = P(710);
            for (int i = 0; i < Posts.Length; i++)
            {
                int h = P(210);
                G.RoundRect(P(30), y, W - P(60), h, P(30), new Color32(24, 26, 32, 255));
                UI.Bolt(G, P(86), y + P(70), P(54), new Color32(90, 160, 255, 255));
                G.Text(Posts[i][0], P(130), y + P(38), 0.40f * K, UI.White);
                int ty = y + P(96);
                foreach (var line in Wrap(Posts[i][1], W - P(180)))
                {
                    G.Text(line, P(60), ty, 0.30f * K, new Color32(180, 184, 196, 255));
                    ty += G.LineHeight(0.30f * K);
                }
                y += h + P(18);
            }

            G.Text("Открой Telegram и найди @be_primex", W / 2, H - P(140), 0.30f * K,
                   new Color32(120, 122, 134, 255), Gfx.Align.Center);
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
                if (In(p, BtnBack()) || d.x > P(200)) { _mode = Mode.Contacts; OS.Invalidate(); }
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
