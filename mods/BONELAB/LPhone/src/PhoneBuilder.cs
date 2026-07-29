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

        /// <summary>Расцветка корпуса. Три цвета — ровно те, в которых выпускается
        /// iPhone 17 Pro Max: Cosmic Orange, Deep Blue и Silver.</summary>
        internal struct BodyStyle
        {
            public string Name;
            public Color32 Frame;   // рама по периметру
            public Color32 Dark;    // тёмная рама, плато камер, кнопки
            public Color32 Back;    // задняя стеклянная панель
        }

        internal static readonly BodyStyle[] Styles =
        {
            new BodyStyle { Name = "Cosmic Orange",
                            Frame = new Color32(0xE8, 0x76, 0x3A, 255),
                            Dark  = new Color32(0xD4, 0x66, 0x2D, 255),
                            Back  = new Color32(0xEE, 0x7C, 0x42, 255) },
            new BodyStyle { Name = "Deep Blue",
                            Frame = new Color32(0x2E, 0x4E, 0x7E, 255),
                            Dark  = new Color32(0x24, 0x40, 0x6A, 255),
                            Back  = new Color32(0x35, 0x57, 0x8A, 255) },
            new BodyStyle { Name = "Silver",
                            Frame = new Color32(0xD8, 0xD9, 0xDC, 255),
                            Dark  = new Color32(0xC4, 0xC6, 0xCA, 255),
                            Back  = new Color32(0xE6, 0xE7, 0xEA, 255) },
        };

        private static BodyStyle _style = Styles[0];
        private static readonly System.Random _rnd = new System.Random();
        private static int _lastStyle = -1;

        /// <summary>
        /// sRGB в линейное. Шейдеру цвет уходит как есть, а проект в линейном
        /// пространстве — если подать байты напрямую, оранжевый выйдет бурым.
        /// </summary>
        private static Color Lin(Color32 c) => new Color(Ch(c.r), Ch(c.g), Ch(c.b));

        private static float Ch(byte b)
        {
            float v = b / 255f;
            return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
        }

        private static void Step(string s) => MelonLogger.Msg("[LPhone] ... " + s);

        /// <summary>Гарантированно живой шейдер: сначала по имени, потом «занимаем» у сцены.</summary>
        internal static Shader GetShader()
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
                case 1: c = Lin(_style.Dark);                  metal = 0.85f; rough = 0.34f; break;
                case 2: c = Color.white;                       metal = 0.00f; rough = 0.06f; break;
                case 3: c = new Color(0.015f, 0.025f, 0.055f); metal = 0.35f; rough = 0.05f; break;
                case 4: c = new Color(0.780f, 0.780f, 0.800f); metal = 1.00f; rough = 0.22f; break;
                case 5: c = new Color(0.030f, 0.030f, 0.030f); metal = 0.00f; rough = 0.35f; break;
                case 6: c = new Color(0.008f, 0.008f, 0.010f); metal = 0.00f; rough = 0.10f; break;
                case 7: c = Lin(_style.Back);                  metal = 0.30f; rough = 0.28f; break;
                default: c = Lin(_style.Frame);                metal = 0.85f; rough = 0.40f; break;
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

        private static bool _layersLogged;

        /// <summary>
        /// Ставим телефону тот же слой физики, что у настоящих пропов игры.
        /// Слой берём у любого InteractableHost в сцене — это ровно тот слой,
        /// на котором лежат хватаемые предметы (гадать по именам не нужно).
        /// Компонент только ИЩЕМ, не добавляем — добавление роняет игру.
        /// </summary>
        private static void ApplyPropLayer(GameObject root)
        {
            try
            {
                if (!_layersLogged)
                {
                    _layersLogged = true;
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < 32; i++)
                    {
                        string n = LayerMask.LayerToName(i);
                        if (!string.IsNullOrEmpty(n)) sb.Append(i).Append('=').Append(n).Append("  ");
                    }
                    MelonLogger.Msg("[LPhone] слои: " + sb);
                }

                // Физические пропы BONELAB живут на слое Dynamic. Эвристика «взять слой
                // у первого InteractableHost» дала Default — она ненадёжна, берём по имени.
                int layer = -1;
                foreach (var n in new[] { "Dynamic", "Interactable" })
                {
                    int l = LayerMask.NameToLayer(n);
                    if (l >= 0) { layer = l; Step($"слой: {n} ({l})"); break; }
                }

                if (layer >= 0) SetLayerRecursive(root.transform, layer);
                else Step("слой не определён — остаётся Default");
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] слой: " + e.Message); }
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }

        /// <summary>
        /// Приведение UV экрана к нашему буферу.
        ///
        /// Игрок смотрит на панель СО СТОРОНЫ +Z, и в этом положении ось +X
        /// меша идёт для него влево — по горизонтали запечённые UV этому уже
        /// соответствуют (в phone.bin u=1 при x=-w/2). А вот по вертикали
        /// расхождение: SetPixels32 кладёт нулевую строку буфера (визуальный
        /// верх) в НИЗ текстуры, поэтому картинка выходила перевёрнутой.
        /// Инвертируем ТОЛЬКО v: если тронуть ещё и u, горизонталь уедет
        /// зеркально в другую сторону.
        /// </summary>
        private static Vector2[] ScreenUV(Vector2[] src)
        {
            var r = new Vector2[src.Length];
            for (int i = 0; i < src.Length; i++)
                r[i] = new Vector2(src[i].x, 1f - src[i].y);
            return r;
        }

        public static PhoneInstance Build(Vector3 position, Quaternion rotation)
        {
            Step("старт сборки");

            // Цвет каждый раз случайный, но не тот же, что в прошлый спавн —
            // иначе из трёх вариантов два подряд совпадения выглядят как баг.
            int si = _rnd.Next(Styles.Length);
            if (si == _lastStyle && Styles.Length > 1)
                si = (si + 1 + _rnd.Next(Styles.Length - 1)) % Styles.Length;
            _lastStyle = si;
            _style = Styles[si];
            Step("цвет корпуса: " + _style.Name);

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
                    var uv = part.UV;
                    if (uv != null && part.Name == "Screen_UI_Anchor") uv = ScreenUV(uv);
                    if (uv != null) mesh.uv = new Il2CppStructArray<Vector2>(uv);
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

                ApplyPropLayer(root);

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

        /// <summary>
        /// Где на самом деле лежит плоскость стекла в локальных координатах
        /// экранного меша. Вершины запечены со смещением (z = +0.0045), а не
        /// вокруг нуля, поэтому «глубину» касания надо мерить от этой плоскости.
        /// Иначе палец нажимал бы, не долетев до стекла нескольких миллиметров.
        /// </summary>
        public readonly Vector3 ScreenCenter;
        /// <summary>true — собран модом; false — пришёл из паллета (хват/физика от SLZ).</summary>
        public bool RuntimeBuilt;

        public PhoneInstance(GameObject root, Renderer screen, Transform screenTf)
        {
            Root = root;
            ScreenRenderer = screen;
            ScreenTransform = screenTf;

            try
            {
                var mf = screenTf != null ? screenTf.GetComponent<MeshFilter>() : null;
                if (mf != null && mf.sharedMesh != null) ScreenCenter = mf.sharedMesh.bounds.center;
            }
            catch { }
            MelonLogger.Msg($"[LPhone] ... плоскость экрана: {ScreenCenter.x:0.0000} " +
                            $"{ScreenCenter.y:0.0000} {ScreenCenter.z:0.0000}");

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

        public void Destroy()
        {
            try { OS?.Dispose(); } catch { }
            if (Root != null) UnityEngine.Object.Destroy(Root);
        }
    }

    internal static class Phone
    {
        public const float BodyW = 0.0776f;
        public const float BodyH = 0.1634f;
        public const float BodyT = 0.00875f;

        public const float ScreenW = 0.0742f;
        public const float ScreenH = 0.1600f;

        // 742x1600 — ровно 0.1 мм на пиксель, около 254 PPI. На 371x800 текст
        // на дистанции вытянутой руки читался плохо. Цена подъёма гасится
        // полосной заливкой в Gfx: перерисовывается только изменившаяся часть.
        public const int ScreenPxW = 742;
        public const int ScreenPxH = 1600;

        public const int SafeTop = 75;
        public const int SafeBottom = 20;
    }
}
