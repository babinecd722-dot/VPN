using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Собирает телефон в рантайме: меш из запечённых данных, материалы,
    /// физика и хват. Паллет Marrow SDK не нужен — всё в моде.
    /// </summary>
    internal static class PhoneBuilder
    {
        private static Shader _lit, _unlit;

        private static Shader Lit
        {
            get
            {
                if (_lit != null) return _lit;
                foreach (var n in new[]
                {
                    "Universal Render Pipeline/Lit",
                    "Universal Render Pipeline/Simple Lit",
                    "Standard",
                })
                {
                    _lit = Shader.Find(n);
                    if (_lit != null) { MelonLogger.Msg("[LPhone] шейдер: " + n); return _lit; }
                }
                return Unlit;
            }
        }

        private static Shader Unlit
        {
            get
            {
                if (_unlit != null) return _unlit;
                foreach (var n in new[]
                {
                    "Universal Render Pipeline/Unlit",
                    "Unlit/Texture",
                    "Sprites/Default",
                })
                {
                    _unlit = Shader.Find(n);
                    if (_unlit != null) return _unlit;
                }
                return null;
            }
        }

        private static Material MakeMat(int id)
        {
            // Цвета в ЛИНЕЙНОМ пространстве — как в glTF.
            Color c;
            float metal, rough;
            switch (id)
            {
                case 1: c = new Color(0.660f, 0.135f, 0.030f); metal = 0.85f; rough = 0.34f; break;
                case 2: c = Color.white;                        metal = 0.00f; rough = 0.06f; break;
                case 3: c = new Color(0.015f, 0.025f, 0.055f);  metal = 0.35f; rough = 0.05f; break;
                case 4: c = new Color(0.780f, 0.780f, 0.800f);  metal = 1.00f; rough = 0.22f; break;
                case 5: c = new Color(0.030f, 0.030f, 0.030f);  metal = 0.00f; rough = 0.35f; break;
                case 6: c = new Color(0.008f, 0.008f, 0.010f);  metal = 0.00f; rough = 0.10f; break;
                case 7: c = new Color(0.855f, 0.215f, 0.055f);  metal = 0.30f; rough = 0.28f; break;
                default: c = new Color(0.807f, 0.181f, 0.042f); metal = 0.85f; rough = 0.40f; break;
            }

            // Экран рисуем без освещения — он «светится» сам.
            var sh = id == 2 ? (Unlit ?? Lit) : Lit;
            var m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            try { m.color = c; } catch { }
            try { m.SetColor("_BaseColor", c); } catch { }
            try { m.SetFloat("_Metallic", metal); } catch { }
            try { m.SetFloat("_Smoothness", 1f - rough); } catch { }
            try { m.SetFloat("_Glossiness", 1f - rough); } catch { }
            return m;
        }

        /// <summary>Строит объект телефона. Возвращает корень; экран доступен через PhoneInstance.</summary>
        public static PhoneInstance Build(Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("LPhone_17_Pro_Max");
            root.transform.position = position;
            root.transform.rotation = rotation;

            Renderer screenRenderer = null;
            Transform screenTf = null;

            foreach (var part in Assets.PhoneParts)
            {
                var go = new GameObject(part.Name);
                go.transform.SetParent(root.transform, false);

                var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                var verts = new Il2CppStructArray<Vector3>(part.Vertices.Length);
                for (int i = 0; i < part.Vertices.Length; i++) verts[i] = part.Vertices[i];
                mesh.vertices = verts;

                if (part.UV != null)
                {
                    var uvs = new Il2CppStructArray<Vector2>(part.UV.Length);
                    for (int i = 0; i < part.UV.Length; i++) uvs[i] = part.UV[i];
                    mesh.uv = uvs;
                }

                var tris = new Il2CppStructArray<int>(part.Triangles.Length);
                for (int i = 0; i < part.Triangles.Length; i++) tris[i] = part.Triangles[i];
                mesh.triangles = tris;

                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.material = MakeMat(part.MaterialId);

                if (part.Name == "Screen_UI_Anchor")
                {
                    screenRenderer = mr;
                    screenTf = go.transform;
                }
            }

            // Физика: один коллайдер по габаритам корпуса.
            var bc = root.AddComponent<BoxCollider>();
            bc.size = new Vector3(Phone.BodyW, Phone.BodyH, Phone.BodyT);
            bc.center = Vector3.zero;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.22f;                    // ~как настоящий телефон
            rb.drag = 0.05f;
            rb.angularDrag = 0.35f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            TryAddGrip(root, bc);

            var inst = new PhoneInstance(root, screenRenderer, screenTf);
            MelonLogger.Msg("[LPhone] телефон создан");
            return inst;
        }

        /// <summary>
        /// Хват: InteractableHost + BoxGrip, чтобы телефон брался рукой как обычный предмет
        /// (и корректно ронялся при отпускании — это штатная физика Marrow).
        /// </summary>
        private static void TryAddGrip(GameObject root, BoxCollider bc)
        {
            try
            {
                var host = root.AddComponent<InteractableHost>();
                var grip = root.AddComponent<BoxGrip>();

                try { grip.isThrowable = true; } catch { }
                try
                {
                    var cols = new Il2CppReferenceArray<Collider>(1);
                    cols[0] = bc;
                    grip.gripColliders = cols;
                } catch { }

                try { host.DecorateHostOnChildGrips(root.transform); } catch { }
                MelonLogger.Msg("[LPhone] хват установлен (InteractableHost + BoxGrip)");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] хват не установлен: " + e.Message +
                                    " — телефон останется физическим предметом");
            }
        }
    }

    /// <summary>Живой экземпляр телефона в мире.</summary>
    internal sealed class PhoneInstance
    {
        public readonly GameObject Root;
        public readonly Renderer ScreenRenderer;
        public readonly Transform ScreenTransform;
        public readonly Gfx Screen;
        public readonly PhoneOS OS;

        public PhoneInstance(GameObject root, Renderer screen, Transform screenTf)
        {
            Root = root;
            ScreenRenderer = screen;
            ScreenTransform = screenTf;

            Screen = new Gfx(Phone.ScreenPxW, Phone.ScreenPxH);
            if (ScreenRenderer != null && ScreenRenderer.material != null)
            {
                try { ScreenRenderer.material.mainTexture = Screen.Texture; } catch { }
                try { ScreenRenderer.material.SetTexture("_BaseMap", Screen.Texture); } catch { }
            }

            OS = new PhoneOS(this);
        }

        public bool Alive => Root != null;

        public void Destroy()
        {
            if (Root != null) UnityEngine.Object.Destroy(Root);
        }
    }

    /// <summary>Константы телефона (метры / пиксели экрана).</summary>
    internal static class Phone
    {
        public const float BodyW = 0.0776f;
        public const float BodyH = 0.1634f;
        public const float BodyT = 0.00875f;

        public const float ScreenW = 0.0742f;      // ширина экранного меша
        public const float ScreenH = 0.1600f;
        public const float ScreenZ = 0.00449f;     // BodyT/2 + вынос

        public const int ScreenPxW = 742;          // 1 px = 0.1 мм
        public const int ScreenPxH = 1600;

        /// <summary>Верхняя зона под Dynamic Island — туда UI не лезет.</summary>
        public const int SafeTop = 150;
        public const int SafeBottom = 40;
    }
}
