using System;
using System.Collections.Generic;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// L Store — витрина в духе App Store. GET запускает пятисекундную
    /// «загрузку» с кольцом прогресса, после чего приложение появляется
    /// на рабочем столе. Отсюда же возвращаются удалённые приложения.
    /// </summary>
    internal sealed class StoreApp : PhoneApp
    {
        public override string Id => "store";
        public override string Title => "L Store";

        private const float DownloadTime = 5f;

        private PhoneApp _downloading;
        private float _startedAt;
        private float _nextTick;
        private int _scroll;

        public override void Open() { _scroll = 0; OS.Invalidate(); }

        public override void Tick()
        {
            if (_downloading == null) return;
            // Кольцо прогресса дискретное, обновлять его 30 раз в секунду
            // незачем: полная перерисовка экрана на каждый кадр и давала
            // просадку во время «загрузки».
            if (Time.unscaledTime >= _nextTick)
            {
                _nextTick = Time.unscaledTime + 1f / 8f;
                OS.Invalidate();
            }
            if (Time.unscaledTime - _startedAt >= DownloadTime)
            {
                OS.Install(_downloading);
                OS.Audio?.Ding();
                _downloading = null;
            }
        }

        /// <summary>Что можно поставить: всё, чего нет на рабочем столе.</summary>
        private List<PhoneApp> Catalog()
        {
            var l = new List<PhoneApp>();
            l.Add(OS.AppMessenger);
            foreach (var a in OS.AllApps)
                if (a != OS.AppMessenger && a != this && !l.Contains(a)) l.Add(a);
            return l;
        }

        // ─────────────── вёрстка ───────────────

        private int CardTop => P(560);
        private int CardH => P(230);
        private RectInt Card(int i) => new RectInt(P(30), CardTop + i * (CardH + P(20)) - _scroll,
                                                   W - P(60), CardH);
        private RectInt GetBtn(int i)
        {
            var c = Card(i);
            return new RectInt(c.x + c.width - P(200), c.y + (c.height - P(96)) / 2, P(170), P(96));
        }

        private RectInt Hero() => R(30f, 210f, 742f - 60f, 320f);

        public override void Draw()
        {
            G.Clear(UI.Bg);
            OS.DrawStatusBar();

            G.Text("L Store", P(40), P(120), 0.80f * K, UI.White);

            // баннер «сегодня»
            var h = Hero();
            G.RoundRect(h.x, h.y, h.width, h.height, P(40), new Color32(24, 26, 34, 255));
            G.VGradient(h.x, h.y, h.width, h.height / 2,
                        new Color32(30, 64, 120, 200), new Color32(24, 26, 34, 0));
            G.Text("СЕГОДНЯ", h.x + P(36), h.y + P(34), 0.30f * K, new Color32(120, 170, 255, 255));
            G.Text("Messenger от BE PRIME", h.x + P(36), h.y + P(90), 0.50f * K, UI.White);
            G.Text("Пиши и звони игрокам лобби", h.x + P(36), h.y + P(160), 0.34f * K, UI.Dim);
            G.Text("прямо с телефона", h.x + P(36), h.y + P(206), 0.34f * K, UI.Dim);
            UI.Bolt(G, h.x + h.width - P(90), h.y + h.height / 2, P(120), new Color32(90, 160, 255, 255));

            var cat = Catalog();
            for (int i = 0; i < cat.Count; i++) DrawRow(cat[i], i);

            OS.DrawHomeBar();
        }

        private void DrawRow(PhoneApp a, int i)
        {
            var c = Card(i);
            if (c.y + c.height < P(540) || c.y > H) return;

            G.RoundRect(c.x, c.y, c.width, c.height, P(34), new Color32(22, 22, 26, 255));

            int isz = P(150);
            int ix = c.x + P(26), iy = c.y + (c.height - isz) / 2;
            if (a.Icon != null) G.BlitRounded(a.Icon, ix, iy, isz, isz, P(34));
            else G.RoundRect(ix, iy, isz, isz, P(34), a.Tile);

            G.Text(a.Title, ix + isz + P(28), c.y + P(56), 0.46f * K, UI.White);
            G.Text(Sub(a), ix + isz + P(28), c.y + P(120), 0.32f * K, UI.Dim);

            var b = GetBtn(i);
            if (OS.IsInstalled(a))
            {
                UI.Pill(G, b, "ОТКРЫТЬ", new Color32(44, 44, 50, 255), UI.Blue, 0.32f * K);
            }
            else if (_downloading == a)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - _startedAt) / DownloadTime);
                int cx = b.x + b.width / 2, cy = b.y + b.height / 2, r = b.height / 2;
                G.Circle(cx, cy, r, new Color32(44, 44, 50, 255));
                // кольцо прогресса: сегментами по дуге
                int seg = 44;
                for (int k = 0; k < seg * t; k++)
                {
                    float ang = -Mathf.PI / 2f + k * 2f * Mathf.PI / seg;
                    int px = cx + Mathf.RoundToInt(Mathf.Cos(ang) * (r - P(9)));
                    int py = cy + Mathf.RoundToInt(Mathf.Sin(ang) * (r - P(9)));
                    G.Circle(px, py, Mathf.Max(2, P(5)), UI.Blue);
                }
                G.RoundRect(cx - P(12), cy - P(12), P(24), P(24), P(5), UI.Blue);
            }
            else
            {
                UI.Pill(G, b, "ЗАГРУЗИТЬ", new Color32(44, 44, 50, 255), UI.Blue, 0.30f * K);
            }
        }

        private static string Sub(PhoneApp a)
        {
            if (a is MessengerApp) return "Сообщения, фото и звонки";
            if (a is CameraApp) return "Съёмка фото";
            if (a is GalleryApp) return "Твои снимки";
            return "Приложение";
        }

        public override void Up(Vector2 p, Vector2 d)
        {
            var cat = Catalog();

            if (Mathf.Abs(d.y) > P(60))
            {
                int total = cat.Count * (CardH + P(20));
                int max = Mathf.Max(0, total - (H - CardTop - P(120)));
                _scroll = Mathf.Clamp(_scroll - Mathf.RoundToInt(d.y), 0, max);
                OS.Invalidate();
                return;
            }
            if (d.magnitude > P(50)) return;

            for (int i = 0; i < cat.Count; i++)
            {
                if (!In(p, GetBtn(i)) && !In(p, Card(i))) continue;
                var a = cat[i];
                if (OS.IsInstalled(a)) { OS.Launch(a); return; }
                if (_downloading == null)
                {
                    _downloading = a;
                    _startedAt = Time.unscaledTime;
                    OS.Audio?.Click();
                    OS.Invalidate();
                }
                return;
            }
        }
    }
}
