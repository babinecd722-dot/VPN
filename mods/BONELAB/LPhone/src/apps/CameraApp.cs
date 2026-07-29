using System;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Камера в духе iPhone: видоискатель, переключение объективов 0,5x / 1x / 3x,
    /// кнопка смены основной и фронтальной камеры, спуск затвора.
    /// Кадр приходит из CameraRig уже готовым буфером.
    /// </summary>
    internal sealed class CameraApp : PhoneApp
    {
        public override string Id => "camera";
        public override string Title => "Camera";

        private float _flash;
        private string _toast;
        private float _toastUntil;

        public override void Open()
        {
            OS.Cam?.Enable();
            OS.Invalidate();
        }

        public override void Close()
        {
            OS.Cam?.Disable();
        }

        public override void Tick()
        {
            // Камере даём импульс, экран НЕ трогаем: картинку показывает квад,
            // а вокруг него интерфейс статичен. Раньше каждый новый кадр
            // видоискателя тянул за собой полную перерисовку экрана.
            OS.Cam?.Pulse(8f);
            if (_flash > 0f) { _flash -= Time.deltaTime * 3.5f; OS.Invalidate(); }
            if (_toastUntil > 0f && Time.unscaledTime > _toastUntil) { _toastUntil = 0f; OS.Invalidate(); }
        }

        // ─────────────── вёрстка ───────────────

        private RectInt Shutter() => R(742f / 2f - 90f, 1330f, 180f, 180f);
        private RectInt Flip() => R(742f - 210f, 1348f, 148f, 148f);
        private RectInt Thumb() => R(66f, 1370f, 110f, 110f);
        private RectInt LensChip(int i) => R(742f / 2f - 165f + i * 115f, 1210f, 100f, 76f);

        public override void Draw()
        {
            G.Clear(new Color32(6, 6, 8, 255));
            OS.DrawStatusBar();

            // Под областью видоискателя — просто чёрный фон: поверх неё стоит
            // квад с текстурой камеры, рисовать сюда пиксели не нужно.
            int vy = P(CameraRig.VfTop), vh = P(CameraRig.VfH);
            G.Rect(0, vy, W, vh, new Color32(0, 0, 0, 255));
            if (OS.Cam == null || !OS.Cam.Ready)
                G.Text("Камера недоступна", W / 2, vy + vh / 2, 0.42f * K, UI.Dim, Gfx.Align.Center);

            var corner = new Color32(255, 255, 255, 120);
            int cl = P(36), th = Mathf.Max(2, P(5));
            G.Rect(0, vy - th, cl, th, corner); G.Rect(W - cl, vy - th, cl, th, corner);
            G.Rect(0, vy + vh, cl, th, corner); G.Rect(W - cl, vy + vh, cl, th, corner);

            // объективы
            for (int i = 0; i < CameraRig.LensName.Length; i++)
            {
                var r = LensChip(i);
                bool on = OS.Cam != null && OS.Cam.Lens == i;
                G.RoundRect(r.x, r.y, r.width, r.height, r.height / 2,
                            on ? new Color32(255, 214, 10, 235) : new Color32(255, 255, 255, 40));
                G.Text(CameraRig.LensName[i], r.x + r.width / 2, r.y + P(16), 0.36f * K,
                       on ? new Color32(20, 20, 24, 255) : UI.White, Gfx.Align.Center);
            }

            bool front = OS.Cam != null && OS.Cam.Front;
            G.Text(front ? "ФРОНТАЛЬНАЯ" : "ОСНОВНАЯ",
                   W / 2, P(1156), 0.30f * K, new Color32(255, 214, 10, 235), Gfx.Align.Center);

            // затвор
            var s = Shutter();
            int scx = s.x + s.width / 2, scy = s.y + s.height / 2;
            G.Circle(scx, scy, s.width / 2, UI.White);
            G.Circle(scx, scy, s.width / 2 - P(12), new Color32(12, 12, 14, 255));
            G.Circle(scx, scy, s.width / 2 - P(18), UI.White);

            // последний снимок
            var t = Thumb();
            if (PhotoStore.All.Count > 0)
                G.BlitRounded(PhotoStore.All[0].Data, t.x, t.y, t.width, t.height, P(20));
            else
                G.RoundRect(t.x, t.y, t.width, t.height, P(20), new Color32(40, 40, 46, 255));

            // смена камеры: основная <-> фронтальная
            var f = Flip();
            int fcx = f.x + f.width / 2, fcy = f.y + f.height / 2;
            G.Circle(fcx, fcy, f.width / 2, front ? new Color32(255, 214, 10, 235)
                                                  : new Color32(62, 62, 70, 255));
            var fg = front ? new Color32(20, 20, 24, 255) : UI.White;
            UI.CamGlyph(G, fcx, fcy - P(6), P(52), fg);
            UI.SwapGlyph(G, fcx, fcy, P(120), fg);
            G.Text(front ? "фронт" : "тыл", fcx, f.y + f.height + P(8), 0.28f * K,
                   UI.White, Gfx.Align.Center);

            if (_flash > 0f) G.Rect(0, 0, W, H, UI.White, Mathf.Clamp01(_flash));

            if (_toastUntil > 0f)
            {
                G.RoundRect(P(80), P(1060), W - P(160), P(90), P(28), new Color32(0, 0, 0, 200));
                G.Text(_toast, W / 2, P(1086), 0.34f * K, UI.White, Gfx.Align.Center);
            }

            OS.DrawHomeBar();
        }

        public override void Up(Vector2 p, Vector2 d)
        {
            if (d.magnitude > P(50)) return;

            if (In(p, Shutter())) { Shoot(); return; }
            if (In(p, Flip())) { OS.Cam?.Flip(); OS.Audio?.Click(); OS.Invalidate(); return; }
            if (In(p, Thumb())) { OS.Launch(OS.AppGallery); return; }
            for (int i = 0; i < CameraRig.LensName.Length; i++)
                if (In(p, LensChip(i))) { OS.Cam?.SetLens(i); OS.Audio?.Click(); OS.Invalidate(); return; }
        }

        private void Shoot()
        {
            var ph = OS.Cam?.Capture();
            OS.Audio?.Click();
            _flash = 1f;
            _toast = ph != null ? "Снимок сохранён в галерею" : "Не удалось снять";
            _toastUntil = Time.unscaledTime + 1.6f;
            OS.Invalidate();
        }
    }
}
