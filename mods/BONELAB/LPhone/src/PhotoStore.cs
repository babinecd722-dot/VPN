using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LPhone
{
    internal sealed class Photo
    {
        public int Id;
        public TexData Data;      // для показа
        public byte[] Full;       // PNG для сохранения в Downloads (может быть null)
        public byte[] Wire;       // лёгкий JPEG для отправки по сети
        public DateTime Taken;
        public string From;       // null = снято мной
    }

    /// <summary>
    /// Галерея. Живёт столько же, сколько лобби — в файл ничего не пишем,
    /// пока пользователь сам не нажмёт «сохранить в Downloads».
    /// </summary>
    internal static class PhotoStore
    {
        public const int Limit = 12;               // каждый снимок ~1 МБ в памяти

        public static readonly List<Photo> All = new List<Photo>();
        private static int _next = 1;

        public static event Action Changed;

        public static Photo Add(TexData d, byte[] full, byte[] wire, string from = null)
        {
            var p = new Photo
            {
                Id = _next++,
                Data = d,
                Full = full,
                Wire = wire ?? full,
                Taken = DateTime.Now,
                From = from,
            };
            All.Insert(0, p);
            while (All.Count > Limit) All.RemoveAt(All.Count - 1);
            try { Changed?.Invoke(); } catch { }
            return p;
        }

        public static void Remove(Photo p)
        {
            if (p == null) return;
            All.Remove(p);
            try { Changed?.Invoke(); } catch { }
        }

        public static Photo ById(int id)
        {
            foreach (var p in All) if (p.Id == id) return p;
            return null;
        }

        private static readonly string[] Dirs =
        {
            "/storage/emulated/0/Download",
            "/storage/emulated/0/Downloads",
            "/sdcard/Download",
        };

        /// <summary>Сохранить снимок в папку загрузок Quest.</summary>
        public static bool Save(Photo p, out string path)
        {
            path = null;
            if (p == null) return false;
            // снятое нами лежит в PNG, принятое по сети — в JPEG
            byte[] bytes = (p.Full != null && p.Full.Length > 0) ? p.Full : p.Wire;
            if (bytes == null || bytes.Length == 0) return false;
            string ext = (p.Full != null && p.Full.Length > 0) ? "png" : "jpg";

            string file = $"LPhone_{p.Taken:yyyyMMdd_HHmmss}_{p.Id}.{ext}";

            foreach (var d in Dirs)
            {
                try
                {
                    if (!Directory.Exists(d)) continue;
                    string f = Path.Combine(d, file);
                    File.WriteAllBytes(f, bytes);
                    path = f;
                    return true;
                }
                catch { }
            }

            // ПК или нет доступа к /sdcard — кладём в UserData мода
            try
            {
                string d = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "LPhone");
                Directory.CreateDirectory(d);
                string f = Path.Combine(d, file);
                File.WriteAllBytes(f, bytes);
                path = f;
                return true;
            }
            catch (Exception e)
            {
                MelonLoader.MelonLogger.Warning("[LPhone] сохранение: " + e.Message);
            }
            return false;
        }
    }
}
