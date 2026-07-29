using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSLZ.Marrow;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Собирает телефон в рантайме: меш из запечённых данных, материалы,
    /// физика и (опционально) хват. Паллет Marrow SDK не нужен.
    ///
    /// ВАЖНО про шейдеры: в собранном билде Shader.Find обычно возвращает null
    /// для URP-шейдеров (их нет в "always included"), а new Material(null)
    /// роняет Il2Cpp НАСМЕРТЬ — managed try/catch это не ловит.
    /// Поэтому шейдер берём у любого живого рендерера сцены.
    /// </summary>
    internal static class PhoneBuilder
    {
        private static Shader _shader;
        private static bool _shaderSearched;

        private static void Step(string s) => MelonLogger.Msg("[LPhone] ... " + s);

        /// <summary>Гарантированно живой шейдер: сначала по имени, потом «занимаем» у сцены.</summary>
        private static Shader GetShader()
        {
            if (_shaderSearched) return _shader;
            _shaderSearched = true;

            foreach (var n in new[]
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Simple Lit",
                "Universal Render Pipeline/Unlit",
            })
            {
                try
                {
                    var s = Shader.Find(n);
                    if (s != null)
                    {
                        _shader = s;
                        MelonLogger.Msg("[LPhone] шейдер найден по имени: " + n);
                        return _shader;
                    }
                }
                catch { }
            }

            // Игра на URP, а Shader.Find("Standard") дал бы шейдер старого пайплайна —
            // он в URP не рисуется. Поэтому берём шейдер у живого рендерера сцены.
            try
            {
                var rends = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                Shader any = null;
                if (rends != null)
                {
                    for (int i = 0; i < rends.Length; i++)
                    {
                        var m = rends[i] != null ? rends[i].sharedMaterial : null;
                        var sh2 = m != null ? m.shader : null;
                        if (sh2 == null) continue;
                        string n2 = sh2.name ?? "";
                        // отсеиваем UI/прозрачные/эффектные — для корпуса нужен обычный Lit
                        if (n2.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n2.IndexOf("Dither", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n2.IndexOf("Particle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n2.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n2.IndexOf("Skybox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            n2.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0)
                        { if (any == null) any = sh2; continue; }

                        if (n2.IndexOf("Lit", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _shader = sh2;
                            MelonLogger.Msg("[LPhone] шейдер взят из сцены: " + n2);
                            return _shader;
                        }
                        if (any == null) any = sh2;
                    }
                }
                if (any != null)
                {
                    _shader = any;
                    MelonLogger.Msg("[LPhone] шейдер из сцены (запасной): " + any.name);
                    return _shader;
                }
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] поиск шейдера: " + e.Message); }

            try
            {
                var last = Shader.Find("Standard");     // крайний случай: хоть что-то
                if (last != null)
                {
                    _shader = last;
                    MelonLogger.Warning("[LPhone] запасной шейдер Standard (в URP может не рисоваться)");
                    return _shader;
                }
            }
            catch { }

            MelonLogger.Error("[LPhone] шейдер не найден — телефон будет без материалов");
            return null;
        }

        private static Material MakeMat(int id, Shader sh)
        {
            if (sh == null) return null;                 // критично: new Material(null) = краш

            Color c; float metal, rough;
            switch (id)
            {
                case 1: c = new Color(0.660f, 0.135f, 0.030f); metal = 0.85f; rough = 0.34f; break;
                case 2: c = Color.white;                       metal = 0.00f; rough = 0.06f; break;
                case 3: c = new Color(0.015f, 0.025f, 0.055f); metal = 0.35f; rough = 0.05f; break;
                case 4: c = new Color(0.780f, 0.780f, 0.800f); metal = 1.00f; rough = 0.22f; break;
                case 5: c = new Color(0.030f, 0.030f, 0.030f); metal = 0.00f; rough = 0.35f; break;
                case 6: c = new Color(0.008f, 0.008f, 0.010f); metal = 0.00f; rough = 0.10f; break;
                case 7: c = new Color(0.855f, 0.215f, 0.055f); metal = 0.30f; rough = 0.28f; break;
                default: c = new Color(0.807f, 0.181f, 0.042f); metal = 0.85f; rough = 0.40f; break;
            }

            Material m;
            try { m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave }; }
            catch (Exception e) { MelonLogger.Warning("[LPhone] материал: " + e.Message); return null; }

            try { m.color = c; } catch { }
            try { m.SetColor("_BaseColor", c); } catch { }
            try { m.SetFloat("_Metallic", metal); } catch { }
            try { m.SetFloat("_Smoothness", 1f - rough); } catch { }
            try { m.SetFloat("_Glossiness", 1f - rough); } catch { }
            // экран должен светиться, а не зависеть от освещения сцены
            if (id == 2)
            {
                try { m.EnableKeyword("_EMISSION"); } catch { }
                try { m.SetColor("_EmissionColor", Color.white); } catch { }
            }
            return m;
        }

        public static PhoneInstance Build(Vector3 position, Quaternion rotation)
        {
            Step("старт сборки");
            var sh = GetShader();

            var root = new GameObject("LPhone_17_Pro_Max");
            root.transform.position = position;
            root.transform.rotation = rotation;

            Renderer screenRenderer = null;
            Transform screenTf = null;
            int built = 0;

            var parts = Assets.PhoneParts;
            Step($"деталей к сборке: {parts.Count}");

            foreach (var part in parts)
            {
                try
                {
                    var go = new GameObject(part.Name);
                    go.transform.SetParent(root.transform, false);

                    var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                    // массовые конструкторы — без поэлементного interop
                    mesh.vertices = new Il2CppStructArray<Vector3>(part.Vertices);
                    if (part.UV != null) mesh.uv = new Il2CppStructArray<Vector2>(part.UV);
                    mesh.triangles = new Il2CppStructArray<int>(part.Triangles);
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();

                    var mf = go.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;                       // не .mesh — тот делает копию

                    var mat = MakeMat(part.MaterialId, sh);
                    if (mat != null)
                    {
                        var mr = go.AddComponent<MeshRenderer>();
                        mr.sharedMaterial = mat;
                        if (part.Name == "Screen_UI_Anchor")
                        {
                            screenRenderer = mr;
                            screenTf = go.transform;
                        }
                    }
                    else if (part.Name == "Screen_UI_Anchor")
                    {
                        screenTf = go.transform;
                    }
                    built++;
                }
                catch (Exception e)
                {
                    MelonLogger.Warning($"[LPhone] деталь {part.Name}: {e.Message}");
                }
            }
            Step($"меши собраны: {built}");

            try
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.size = new Vector3(Phone.BodyW, Phone.BodyH, Phone.BodyT);
                bc.center = Vector3.zero;

                var rb = root.AddComponent<Rigidbody>();
                rb.mass = 0.22f;
                rb.drag = 0.05f;
                rb.angularDrag = 0.35f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                Step("физика ок, хват — свой (PhoneGrab)");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[LPhone] физика: " + e.Message);
            }

            Step("экран/ОС");
            var inst = new PhoneInstance(root, screenRenderer, screenTf);
            MelonLogger.Msg("[LPhone] телефон создан");
            return inst;
        }

    }

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
            if (ScreenRenderer != null)
            {
                var m = ScreenRenderer.sharedMaterial;
                if (m != null)
                {
                    try { m.mainTexture = Screen.Texture; } catch { }
                    try { m.SetTexture("_BaseMap", Screen.Texture); } catch { }
                    try { m.SetTexture("_EmissionMap", Screen.Texture); } catch { }
                }
            }

            OS = new PhoneOS(this);
        }

        public bool Alive => Root != null;
        public void Destroy() { if (Root != null) UnityEngine.Object.Destroy(Root); }
    }

    internal static class Phone
    {
        public const float BodyW = 0.0776f;
        public const float BodyH = 0.1634f;
        public const float BodyT = 0.00875f;

        public const float ScreenW = 0.0742f;
        public const float ScreenH = 0.1600f;

        // 371x800: на расстоянии вытянутой руки в VR этого с запасом хватает,
        // а работы вчетверо меньше, чем при 742x1600.
        public const int ScreenPxW = 371;
        public const int ScreenPxH = 800;

        public const int SafeTop = 75;
        public const int SafeBottom = 20;
    }
}
