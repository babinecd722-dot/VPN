using System;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Камера телефона: Unity-камера на корпусе рисует в RenderTexture,
    /// а кадр видоискателя читается в наш программный буфер и блитится в UI.
    ///
    /// Первая версия показывала RenderTexture отдельным квадом поверх экрана —
    /// и видоискатель оставался чёрным. Квад зависит и от свойств шейдера,
    /// который мы занимаем у сцены (SLZ/LitMAS), и от того, рисует ли URP
    /// нашу камеру. Чтение пикселей от этого не зависит вообще, поэтому
    /// картинка есть всегда. Цена сдержана: читаем уменьшенный кадр 288x384
    /// и не чаще 8 раз в секунду, полный размер — только в момент снимка.
    /// </summary>
    internal sealed class CameraRig
    {
        // Видоискатель в координатах вёрстки (база 742x1600), 3:4 портрет.
        public const float VfTop = 200f;
        public const float VfH = 989f;

        private const int RtW = 384, RtH = 512;      // снимок
        private const int PrevW = 288, PrevH = 384;  // видоискатель
        private const int VideoW = 96, VideoH = 128; // кадр видеозвонка

        private readonly PhoneInstance _phone;
        private GameObject _camGo;
        private Camera _cam;
        private RenderTexture _rt, _prevRt, _smallRt;
        private Texture2D _read, _prevTex, _small;
        private float _nextPreview, _nextVideo;
        private bool _on, _diagDone;

        /// <summary>Последний кадр видоискателя, готовый к отрисовке.</summary>
        public TexData Preview;

        public bool Front;                 // селфи-камера
        public int Lens = 1;               // 0 = 0.5x, 1 = 1x, 2 = 3x
        public bool Ready => _cam != null;

        public static readonly float[] Fov = { 95f, 62f, 26f };
        public static readonly string[] LensName = { "0,5x", "1x", "3x" };

        public CameraRig(PhoneInstance phone) { _phone = phone; }

        // ─────────────── жизненный цикл ───────────────

        public void Enable()
        {
            try
            {
                if (_cam == null) Create();
                if (_cam == null || _on) return;
                _on = true;
                Place();
                _cam.enabled = true;
                _nextPreview = 0f;
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] камера вкл: " + e.Message); }
        }

        public void Disable()
        {
            if (!_on) return;
            _on = false;
            try { if (_cam != null) _cam.enabled = false; } catch { }
        }

        public void Destroy()
        {
            try { if (_camGo != null) UnityEngine.Object.Destroy(_camGo); } catch { }
            Release(ref _rt); Release(ref _prevRt); Release(ref _smallRt);
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
            _cam.farClipPlane = 220f;
            _cam.fieldOfView = Fov[Lens];
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.depth = -20f;               // не мешаем основной камере
            _cam.enabled = false;
            // NameToLayer вернёт -1, если слоя нет, а 1 << -1 в C# — это бит 31,
            // и мы бы случайно выключили фон. Поэтому только при валидном слое.
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
            _nextPreview = 0f;
        }

        public void Flip()
        {
            Front = !Front;
            Place();
            _nextPreview = 0f;
        }

        // ─────────────── видоискатель ───────────────

        /// <summary>Обновляет Preview. true — кадр сменился, экран надо перерисовать.</summary>
        public bool UpdatePreview(float hz = 8f)
        {
            if (_cam == null || !_on) return false;
            if (Time.unscaledTime < _nextPreview) return false;
            _nextPreview = Time.unscaledTime + 1f / Mathf.Max(1f, hz);

            try
            {
                if (_prevRt == null) _prevRt = NewRT(PrevW, PrevH, "LPhone_PrevRT");
                if (_prevTex == null)
                    _prevTex = new Texture2D(PrevW, PrevH, TextureFormat.RGBA32, false)
                    { hideFlags = HideFlags.HideAndDontSave };

                Graphics.Blit(_rt, _prevRt);          // уменьшаем на GPU

                var prev = RenderTexture.active;
                RenderTexture.active = _prevRt;
                _prevTex.ReadPixels(new Rect(0, 0, PrevW, PrevH), 0, 0, false);
                _prevTex.Apply(false);
                RenderTexture.active = prev;

                Preview = TexData.FromTexture(_prevTex);
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
        /// не отрисовала нашу камеру — пробуем дёрнуть её вручную и пишем в лог,
        /// что именно помогло. Без этого «чёрный видоискатель» не отличить от
        /// «в кадре действительно темно».
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

            if (avg > 1.5f) return;

            MelonLogger.Warning("[LPhone] кадр пустой — пробуем ручной Render()");
            try
            {
                _cam.Render();
                MelonLogger.Msg("[LPhone] ручной Render() прошёл");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] ручной Render() не поддерживается: " + e.Message);
            }
        }

        // ─────────────── снимок ───────────────

        /// <summary>Спуск затвора: снимок уходит в галерею.</summary>
        public Photo Capture()
        {
            if (_rt == null || _cam == null) return null;
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

                var png = Jpeg.EncodePng(_read);
                var jpg = Jpeg.EncodeJpg(_read, 78);      // по сети гоним лёгкую копию
                var data = TexData.FromTexture(_read);
                if (data == null) return null;
                return PhotoStore.Add(data, png, jpg);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] снимок: " + e.Message);
                return null;
            }
        }

        // ─────────────── кадр для видеозвонка ───────────────

        /// <summary>Маленький JPEG для видеозвонка. Сжатие и частоту держим низкими,
        /// иначе канал Fusion захлебнётся, а Quest просядет по FPS.</summary>
        public byte[] VideoFrame(float hz = 4f)
        {
            if (_cam == null || !_on) return null;
            if (Time.unscaledTime < _nextVideo) return null;
            _nextVideo = Time.unscaledTime + 1f / Mathf.Max(1f, hz);

            try
            {
                if (_smallRt == null) _smallRt = NewRT(VideoW, VideoH, "LPhone_VidRT");
                if (_small == null)
                    _small = new Texture2D(VideoW, VideoH, TextureFormat.RGB24, false)
                    { hideFlags = HideFlags.HideAndDontSave };

                Graphics.Blit(_rt, _smallRt);

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
