using System;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Камера телефона: Unity-камера на корпусе рисует в RenderTexture,
    /// а видоискатель — квад поверх экрана, показывающий эту текстуру напрямую.
    ///
    /// Две вещи, которые роняли FPS, и почему сделано именно так.
    ///
    /// Первое: включённая камера заставляет URP гонять ВСЮ сцену ещё раз каждый
    /// кадр — при 72 FPS это 72 лишних прохода рендера в секунду. Поэтому
    /// камера живёт ровно один кадр на обновление: включаем, на следующем кадре
    /// гасим. Между обновлениями RenderTexture хранит последний кадр, и квад
    /// продолжает его показывать.
    ///
    /// Второе: ReadPixels. На тайловом GPU Quest это принудительная
    /// синхронизация — конвейер встаёт и ждёт завершения рендера. В видоискателе
    /// его больше нет вообще: квад берёт текстуру напрямую, без единого
    /// прочитанного пикселя. Читаем ровно один раз — в момент снимка.
    ///
    /// Раньше квад оставался чёрным не из-за шейдера, а потому что камера тогда
    /// вообще не рисовала: стояло enabled = false, а Camera.Render() в URP
    /// не поддерживается. Текстура просто была пустой.
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
        private GameObject _camGo, _quad;
        private Camera _cam;
        private Material _quadMat;
        private RenderTexture _rt, _smallRt;
        private Texture2D _read, _small;
        private float _nextShot, _nextVideo;
        private bool _on;
        private int _phase;                 // 0 — ждём, 1 — камера включена на кадр

        public bool Front;                 // селфи-камера
        public int Lens = 1;               // 0 = 0.5x, 1 = 1x, 2 = 3x
        public bool Ready => _cam != null;

        public static readonly float[] Fov = { 95f, 62f, 26f };
        public static readonly string[] LensName = { "0,5x", "1x", "3x" };

        public CameraRig(PhoneInstance phone) { _phone = phone; }

        // ─────────────── жизненный цикл ───────────────

        /// <summary>Приложение открыто: показываем квад, камеру включаем импульсами.</summary>
        public void Enable(bool viewfinder = true)
        {
            try
            {
                if (_cam == null) Create();
                if (_cam == null) return;
                _on = true;
                _nextShot = 0f;
                Place();
                if (_quad != null && _quad.activeSelf != viewfinder) _quad.SetActive(viewfinder);
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] камера вкл: " + e.Message); }
        }

        public void Disable()
        {
            _on = false;
            _phase = 0;
            try { if (_cam != null) _cam.enabled = false; } catch { }
            try { if (_quad != null) _quad.SetActive(false); } catch { }
        }

        public void Destroy()
        {
            try { if (_camGo != null) UnityEngine.Object.Destroy(_camGo); } catch { }
            try { if (_quad != null) UnityEngine.Object.Destroy(_quad); } catch { }
            Release(ref _rt); Release(ref _smallRt);
            _camGo = null; _cam = null; _quad = null;
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

            BuildQuad();
            MelonLogger.Msg("[LPhone] камера создана");
        }

        /// <summary>
        /// Квад видоискателя ровно поверх области предпросмотра. Материал
        /// настраиваем так же, как экран телефона — там этот путь работает,
        /// значит и здесь текстура покажется.
        /// </summary>
        private void BuildQuad()
        {
            var parent = _phone.ScreenTransform != null ? _phone.ScreenTransform : _phone.Root.transform;
            Vector3 c = _phone.ScreenCenter;

            float top = c.y + Phone.ScreenH * (0.5f - VfTop / 1600f);
            float bottom = c.y + Phone.ScreenH * (0.5f - (VfTop + VfH) / 1600f);
            float hw = Phone.ScreenW * 0.5f;
            float z = c.z + 0.0006f;

            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(new[]
            {
                new Vector3(-hw, bottom, z), new Vector3(hw, bottom, z),
                new Vector3( hw, top,    z), new Vector3(-hw, top,   z),
            });
            // UV зеркалим по горизонтали: игрок смотрит на экран со стороны +Z,
            // где ось +X меша идёт для него влево — как и у самого экрана.
            mesh.uv = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector2>(new[]
            {
                new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
            });
            mesh.triangles = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<int>(
                new[] { 0, 2, 1, 0, 3, 2 });
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _quad = new GameObject("LPhone_Viewfinder");
            _quad.transform.SetParent(parent, false);
            _quad.transform.localPosition = Vector3.zero;
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
        /// Двухтактный импульс: на одном кадре включаем камеру, на следующем
        /// гасим. Пиксели не читаются — картинку показывает квад напрямую.
        /// </summary>
        public void Pulse(float hz = 8f)
        {
            if (_cam == null || !_on) return;

            if (_phase == 1)
            {
                _phase = 0;
                _cam.enabled = false;         // один проход рендера — и хватит
                return;
            }

            if (Time.unscaledTime < _nextShot) return;
            _nextShot = Time.unscaledTime + 1f / Mathf.Max(1f, hz);
            _cam.enabled = true;
            _phase = 1;
        }

        // ─────────────── снимок ───────────────

        /// <summary>
        /// Спуск затвора. Берём последний кадр видоискателя — он не старше
        /// одной восьмой секунды. Кодируем ТОЛЬКО в JPEG: PNG на 110 тысяч
        /// пикселей кодируется десятки миллисекунд и давал заметный фриз.
        /// </summary>
        public Photo Capture()
        {
            if (_rt == null) return null;
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
            if (_cam == null || !_on || _rt == null) return null;
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
