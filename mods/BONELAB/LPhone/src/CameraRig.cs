using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Настоящая камера телефона: Unity-камера на корпусе, картинка в RenderTexture,
    /// видоискатель — отдельный квад ровно поверх нужного куска экрана.
    ///
    /// Почему квад, а не рисование кадра в наш софтовый буфер: чтение
    /// RenderTexture каждый кадр — это стоп GPU, на Quest так делать нельзя.
    /// Квад показывает текстуру напрямую, а вокруг него UI рисует наш Gfx.
    /// Пиксели читаем ровно один раз — в момент спуска затвора.
    /// </summary>
    internal sealed class CameraRig
    {
        // Видоискатель в координатах вёрстки (база 742x1600), 3:4 портрет.
        public const float VfTop = 200f;
        public const float VfH = 989f;

        private const int RtW = 384, RtH = 512;
        private const int VideoW = 96, VideoH = 128;

        private readonly PhoneInstance _phone;
        private GameObject _camGo, _quad;
        private Camera _cam;
        private RenderTexture _rt;
        private Texture2D _read;
        private Material _quadMat;

        public bool Front;                 // селфи-камера
        public int Lens = 1;               // 0 = 0.5x, 1 = 1x, 2 = 3x
        public bool Ready => _cam != null;

        public static readonly float[] Fov = { 95f, 62f, 26f };
        public static readonly string[] LensName = { "0,5x", "1x", "3x" };

        public CameraRig(PhoneInstance phone) { _phone = phone; }

        // ─────────────── жизненный цикл ───────────────

        private bool _on;

        public void Enable(bool showViewfinder)
        {
            try
            {
                if (_cam == null) Create();
                if (_cam == null) return;
                if (_on && _quad != null && _quad.activeSelf == showViewfinder) return;
                _on = true;
                Place();
                _cam.enabled = true;
                if (_quad != null) _quad.SetActive(showViewfinder);
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] камера вкл: " + e.Message); }
        }

        public void Disable()
        {
            _on = false;
            try { if (_cam != null) _cam.enabled = false; } catch { }
            try { if (_quad != null) _quad.SetActive(false); } catch { }
        }

        public void Destroy()
        {
            try { if (_camGo != null) UnityEngine.Object.Destroy(_camGo); } catch { }
            try { if (_quad != null) UnityEngine.Object.Destroy(_quad); } catch { }
            try { if (_rt != null) { _rt.Release(); UnityEngine.Object.Destroy(_rt); } } catch { }
            _camGo = null; _quad = null; _rt = null; _cam = null;
        }

        private void Create()
        {
            _rt = new RenderTexture(RtW, RtH, 16, RenderTextureFormat.ARGB32)
            {
                name = "LPhone_RT",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                hideFlags = HideFlags.HideAndDontSave,
            };
            _rt.Create();

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
            // В URP ручной Camera.Render() не поддерживается, поэтому камеру
            // просто включаем на время работы приложения и гасим сразу после.
            _cam.enabled = false;
            // NameToLayer вернёт -1, если слоя нет, а 1 << -1 в C# — это бит 31,
            // и мы бы случайно выключили фон. Поэтому только при валидном слое.
            try
            {
                int ui = LayerMask.NameToLayer("UI");
                if (ui >= 0) _cam.cullingMask = ~(1 << ui);
            }
            catch { }

            BuildQuad();
            MelonLogger.Msg("[LPhone] камера создана");
        }

        /// <summary>Квад видоискателя ровно поверх области предпросмотра.</summary>
        private void BuildQuad()
        {
            var parent = _phone.ScreenTransform != null ? _phone.ScreenTransform : _phone.Root.transform;

            float top = Phone.ScreenH * (0.5f - VfTop / 1600f);
            float bottom = Phone.ScreenH * (0.5f - (VfTop + VfH) / 1600f);
            float hw = Phone.ScreenW * 0.5f;

            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new Il2CppStructArray<Vector3>(new[]
            {
                new Vector3(-hw, bottom, 0f),
                new Vector3( hw, bottom, 0f),
                new Vector3( hw, top,    0f),
                new Vector3(-hw, top,    0f),
            });
            mesh.uv = new Il2CppStructArray<Vector2>(new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f),
            });
            mesh.triangles = new Il2CppStructArray<int>(new[] { 0, 2, 1, 0, 3, 2 });
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _quad = new GameObject("LPhone_Viewfinder");
            _quad.transform.SetParent(parent, false);
            _quad.transform.localPosition = new Vector3(0f, 0f, 0.0006f);
            _quad.transform.localRotation = Quaternion.identity;
            _quad.layer = parent.gameObject.layer;

            _quad.AddComponent<MeshFilter>().sharedMesh = mesh;

            var sh = PhoneBuilder.GetShader();
            if (sh != null)
            {
                _quadMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                try { _quadMat.mainTexture = _rt; } catch { }
                try { _quadMat.SetTexture("_BaseMap", _rt); } catch { }
                try { _quadMat.EnableKeyword("_EMISSION"); } catch { }
                try { _quadMat.SetTexture("_EmissionMap", _rt); } catch { }
                try { _quadMat.SetColor("_EmissionColor", Color.white); } catch { }
                try { _quadMat.SetFloat("_Metallic", 0f); } catch { }
                try { _quadMat.SetFloat("_Smoothness", 0f); } catch { }
                _quad.AddComponent<MeshRenderer>().sharedMaterial = _quadMat;
            }
            _quad.SetActive(false);
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

        /// <summary>Оставлено для совместимости: в URP кадр рисует сама включённая камера.</summary>
        public void Render(float hz = 15f) { }

        public void SetLens(int lens)
        {
            Lens = Mathf.Clamp(lens, 0, Fov.Length - 1);
            if (_cam != null) _cam.fieldOfView = Fov[Lens];
        }

        public void Flip()
        {
            Front = !Front;
            Place();
        }

        // ─────────────── снимок ───────────────

        private Texture2D ReadBack()
        {
            if (_rt == null || _cam == null) return null;

            if (_read == null || _read.width != RtW || _read.height != RtH)
            {
                _read = new Texture2D(RtW, RtH, TextureFormat.RGBA32, false)
                { hideFlags = HideFlags.HideAndDontSave };
            }

            var prev = RenderTexture.active;
            RenderTexture.active = _rt;
            _read.ReadPixels(new Rect(0, 0, RtW, RtH), 0, 0, false);
            _read.Apply(false);
            RenderTexture.active = prev;
            return _read;
        }

        /// <summary>Спуск затвора: снимок уходит в галерею.</summary>
        public Photo Capture()
        {
            try
            {
                var t = ReadBack();
                if (t == null) return null;
                var png = Jpeg.EncodePng(t);
                var jpg = Jpeg.EncodeJpg(t, 78);      // по сети гоним лёгкую копию
                var data = TexData.FromTexture(t);
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

        private Texture2D _small;
        private RenderTexture _smallRt;
        private float _nextVideo;

        /// <summary>Маленький JPEG для видеозвонка. Сжатие и частоту держим низкими,
        /// иначе канал Fusion захлебнётся, а Quest просядет по FPS.</summary>
        public byte[] VideoFrame(float hz = 4f)
        {
            if (_cam == null) return null;
            if (Time.unscaledTime < _nextVideo) return null;
            _nextVideo = Time.unscaledTime + 1f / Mathf.Max(1f, hz);

            try
            {
                if (_smallRt == null)
                {
                    _smallRt = new RenderTexture(VideoW, VideoH, 16, RenderTextureFormat.ARGB32)
                    { hideFlags = HideFlags.HideAndDontSave };
                    _smallRt.Create();
                }
                if (_small == null)
                {
                    _small = new Texture2D(VideoW, VideoH, TextureFormat.RGB24, false)
                    { hideFlags = HideFlags.HideAndDontSave };
                }

                // уменьшаем на GPU: читать 384x512 ради кадра видеозвонка — расточительно
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
