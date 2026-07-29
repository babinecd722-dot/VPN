using System;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Камера телефона: Unity-камера на корпусе рисует в RenderTexture,
    /// а кадр видоискателя читается в наш программный буфер.
    ///
    /// Главное про производительность. Включённая камера заставляет URP гонять
    /// ВСЮ сцену ещё раз каждый кадр — при 72 FPS это 72 лишних прохода рендера
    /// в секунду, отчего игра и падала до одного кадра. Поэтому камера живёт
    /// ровно один кадр на каждый снимок видоискателя: включаем, на следующем
    /// кадре забираем готовую текстуру и сразу гасим. Восемь проходов в секунду
    /// вместо семидесяти двух.
    ///
    /// Квада с RenderTexture здесь нет намеренно: он зависел и от свойств
    /// занятого у сцены шейдера SLZ/LitMAS, и от того, рисует ли URP нашу
    /// камеру, и оставался чёрным. Чтение пикселей от этого не зависит.
    /// </summary>
    internal sealed class CameraRig
    {
        // Видоискатель в координатах вёрстки (база 742x1600), 3:4 портрет.
        public const float VfTop = 200f;
        public const float VfH = 989f;

        // Один RenderTexture на всё: и видоискатель, и снимок, и видеозвонок.
        // Лишние Blit и вторые текстуры — это лишние копии и лишняя память.
        private const int RtW = 288, RtH = 384;
        private const int VideoW = 96, VideoH = 128;

        private readonly PhoneInstance _phone;
        private GameObject _camGo;
        private Camera _cam;
        private RenderTexture _rt, _smallRt;
        private Texture2D _read, _small;
        private float _nextShot, _nextVideo;
        private bool _on, _diagDone;
        private int _phase;                 // 0 — ждём, 1 — камера включена на кадр

        /// <summary>Последний кадр видоискателя. Буфер переиспользуется.</summary>
        public TexData Preview { get; private set; }

        public bool Front;                 // селфи-камера
        public int Lens = 1;               // 0 = 0.5x, 1 = 1x, 2 = 3x
        public bool Ready => _cam != null;

        public static readonly float[] Fov = { 95f, 62f, 26f };
        public static readonly string[] LensName = { "0,5x", "1x", "3x" };

        public CameraRig(PhoneInstance phone) { _phone = phone; }

        // ─────────────── жизненный цикл ───────────────

        /// <summary>Приложение открыто. Саму камеру пока НЕ включаем.</summary>
        public void Enable()
        {
            try
            {
                if (_cam == null) Create();
                if (_cam == null) return;
                _on = true;
                _nextShot = 0f;
                Place();
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] камера вкл: " + e.Message); }
        }

        public void Disable()
        {
            _on = false;
            _phase = 0;
            try { if (_cam != null) _cam.enabled = false; } catch { }
        }

        public void Destroy()
        {
            try { if (_camGo != null) UnityEngine.Object.Destroy(_camGo); } catch { }
            Release(ref _rt); Release(ref _smallRt);
            _camGo = null; _cam = null;
        }

        private static void Release(ref RenderTexture rt)
        {
            try { if (rt != null) { rt.Release(); UnityEngine.Object.Destroy(rt); } } catch { }
            rt = null;
        }

        private static RenderTexture NewRT(int w, int h, string name)
        {
            var rt = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32)
            {
                name = name,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave,
            };
            rt.Create();
            return rt;
        }

        private void Create()
        {
            _rt = NewRT(RtW, RtH, "LPhone_RT");

            _camGo = new GameObject("LPhone_Cam");
            _camGo.transform.SetParent(_phone.Root.transform, false);
            _cam = _camGo.AddComponent<Camera>();
            _cam.targetTexture = _rt;
            _cam.nearClipPlane = 0.04f;      // не ловим собственный корпус
            _cam.farClipPlane = 150f;
            _cam.fieldOfView = Fov[Lens];
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.useOcclusionCulling = false;
            _cam.depth = -20f;               // не мешаем основной камере
            _cam.enabled = false;
            try
            {
                int ui = LayerMask.NameToLayer("UI");
                if (ui >= 0) _cam.cullingMask = ~(1 << ui);
            }
            catch { }

            MelonLogger.Msg("[LPhone] камера создана");
        }

        private void Place()
        {
            if (_camGo == null) return;
            float z = Phone.BodyT * 0.5f + 0.004f;
            _camGo.transform.localPosition = new Vector3(
                Front ? 0f : Phone.BodyW * 0.28f,
                Phone.BodyH * 0.33f,
                Front ? z : -z);
            // задняя камера смотрит от спинки, фронтальная — туда же, куда экран
            _camGo.transform.localRotation = Front
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
            _cam.fieldOfView = Fov[Mathf.Clamp(Lens, 0, Fov.Length - 1)];
        }

        public void SetLens(int lens)
        {
            Lens = Mathf.Clamp(lens, 0, Fov.Length - 1);
            if (_cam != null) _cam.fieldOfView = Fov[Lens];
            _nextShot = 0f;
        }

        public void Flip()
        {
            Front = !Front;
            Place();
            _nextShot = 0f;
        }

        // ─────────────── видоискатель ───────────────

        /// <summary>
        /// Двухтактный цикл: на одном кадре включаем камеру, на следующем
        /// забираем результат и гасим. true — появился новый кадр.
        /// </summary>
        public bool UpdatePreview(float hz = 8f)
        {
            if (_cam == null || !_on) return false;

            if (_phase == 1)
            {
                _phase = 0;
                _cam.enabled = false;         // один проход рендера — и хватит
                return ReadPreview();
            }

            if (Time.unscaledTime < _nextShot) return false;
            _nextShot = Time.unscaledTime + 1f / Mathf.Max(1f, hz);
            _cam.enabled = true;
            _phase = 1;
            return false;
        }

        private bool ReadPreview()
        {
            try
            {
                if (_read == null)
                    _read = new Texture2D(RtW, RtH, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave };

                var prev = RenderTexture.active;
                RenderTexture.active = _rt;
                _read.ReadPixels(new Rect(0, 0, RtW, RtH), 0, 0, false);
                _read.Apply(false);
                RenderTexture.active = prev;

                // пишем в тот же буфер — иначе каждый кадр в мусор уходит 440 КБ
                Preview = TexData.Reuse(Preview, _read);
                Diagnose();
                return true;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] видоискатель: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Разовая проверка первого кадра. Если он полностью чёрный, значит URP
        /// не отрисовала нашу камеру — пишем это в лог, иначе «чёрный экран»
        /// не отличить от «в кадре темно».
        /// </summary>
        private void Diagnose()
        {
            if (_diagDone || Preview == null) return;
            _diagDone = true;

            long sum = 0;
            var px = Preview.Pixels;
            for (int i = 0; i < px.Length; i += 97) sum += px[i].r + px[i].g + px[i].b;
            int n = (px.Length + 96) / 97;
            float avg = n > 0 ? sum / (float)(n * 3) : 0f;
            MelonLogger.Msg($"[LPhone] видоискатель: средняя яркость {avg:0.0}/255");
            if (avg <= 1.5f) MelonLogger.Warning("[LPhone] кадр пустой — URP не рисует нашу камеру");
        }

        // ─────────────── снимок ───────────────

        /// <summary>
        /// Спуск затвора. Берём последний кадр видоискателя — он не старше
        /// одной восьмой секунды. Кодируем ТОЛЬКО в JPEG: PNG на 110 тысяч
        /// пикселей кодируется десятки миллисекунд и давал заметный фриз.
        /// </summary>
        public Photo Capture()
        {
            if (_read == null) return null;
            try
            {
                var jpg = Jpeg.EncodeJpg(_read, 88);
                var data = TexData.FromTexture(_read);
                if (data == null) return null;
                return PhotoStore.Add(data, null, jpg);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] снимок: " + e.Message);
                return null;
            }
        }

        // ─────────────── кадр для видеозвонка ───────────────

        /// <summary>Маленький JPEG для видеозвонка.</summary>
        public byte[] VideoFrame(float hz = 4f)
        {
            if (_cam == null || !_on || _read == null) return null;
            if (Time.unscaledTime < _nextVideo) return null;
            _nextVideo = Time.unscaledTime + 1f / Mathf.Max(1f, hz);

            try
            {
                if (_smallRt == null) _smallRt = NewRT(VideoW, VideoH, "LPhone_VidRT");
                if (_small == null)
                    _small = new Texture2D(VideoW, VideoH, TextureFormat.RGB24, false)
                    { hideFlags = HideFlags.HideAndDontSave };

                Graphics.Blit(_rt, _smallRt);   // уменьшаем на GPU

                var prev = RenderTexture.active;
                RenderTexture.active = _smallRt;
                _small.ReadPixels(new Rect(0, 0, VideoW, VideoH), 0, 0, false);
                _small.Apply(false);
                RenderTexture.active = prev;

                return Jpeg.EncodeJpg(_small, 40);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] видеокадр: " + e.Message);
                return null;
            }
        }
    }
}
