using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;

namespace LPhone
{
    /// <summary>
    /// Ассеты зашиты в DLL — мод самодостаточен, паллет Marrow SDK не нужен.
    /// Модель телефона грузится из phone.bin, текстуры из PNG.
    /// </summary>
    internal static class Assets
    {
        public sealed class Part
        {
            public string Name;
            public int MaterialId;
            public Vector3[] Vertices;
            public Vector2[] UV;
            public int[] Triangles;
        }

        private static List<Part> _parts;
        private static readonly Dictionary<string, Texture2D> _tex =
            new Dictionary<string, Texture2D>();

        public static IReadOnlyList<Part> PhoneParts => _parts ?? (_parts = LoadMesh());

        /// <summary>Сырые байты встроенного ресурса.</summary>
        public static byte[] Raw(string name) => Res(name);

        private static byte[] Res(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream("LPhone." + name);
            if (s == null) throw new FileNotFoundException("ресурс не найден: " + name);
            var buf = new byte[s.Length];
            s.Read(buf, 0, buf.Length);
            return buf;
        }

        // ───────────────────────── меш ─────────────────────────
        private static List<Part> LoadMesh()
        {
            var list = new List<Part>();
            try
            {
                using var ms = new MemoryStream(Res("phone.bin"));
                using var r = new BinaryReader(ms);

                var magic = new string(r.ReadChars(4));
                if (magic != "LPH1") throw new InvalidDataException("плохая сигнатура: " + magic);

                int partCount = r.ReadInt32();
                for (int p = 0; p < partCount; p++)
                {
                    int nameLen = r.ReadByte();
                    string name = System.Text.Encoding.UTF8.GetString(r.ReadBytes(nameLen));
                    int matId = r.ReadByte();

                    int vc = r.ReadInt32();
                    var verts = new Vector3[vc];
                    for (int i = 0; i < vc; i++)
                        verts[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

                    Vector2[] uv = null;
                    if (r.ReadByte() == 1)
                    {
                        uv = new Vector2[vc];
                        for (int i = 0; i < vc; i++)
                            uv[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                    }

                    int ic = r.ReadInt32();
                    var tris = new int[ic];
                    for (int i = 0; i < ic; i++) tris[i] = r.ReadUInt16();

                    list.Add(new Part
                    {
                        Name = name, MaterialId = matId,
                        Vertices = verts, UV = uv, Triangles = tris,
                    });
                }
                MelonLogger.Msg($"[LPhone] модель загружена: {list.Count} частей");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[LPhone] не удалось загрузить модель: " + e);
            }
            return list;
        }

        // ───────────────────────── текстуры ─────────────────────────
        public static Texture2D Texture(string file)
        {
            if (_tex.TryGetValue(file, out var t) && t != null) return t;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var bytes = Res(file);
                var arr = new Il2CppStructArray<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++) arr[i] = bytes[i];
                ImageConversion.LoadImage(tex, arr);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags = HideFlags.HideAndDontSave;
                _tex[file] = tex;
                return tex;
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[LPhone] текстура {file}: {e.Message}");
                return null;
            }
        }

        public static Texture2D Wallpaper => Texture("wallpaper.png");
        public static Texture2D IconCamera => Texture("icon_camera.png");
        public static Texture2D IconGallery => Texture("icon_gallery.png");
        public static Texture2D IconStore => Texture("icon_store.png");
        public static Texture2D IconMessenger => Texture("icon_messenger.png");
    }
}
