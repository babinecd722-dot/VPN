using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using MelonLoader;

[assembly: MelonInfo(typeof(SaveInstaller.SaveInstallerMod), "SaveInstaller", "1.0.0", "you")]
[assembly: MelonGame("StressLevelZero", "BONELAB")]

namespace SaveInstaller
{
    public class SaveInstallerMod : MelonMod
    {
        private const string GamePackage = "com.StressLevelZero.BONELAB";

        private static readonly string ModDir = Path.Combine(MelonHandler.ModsDirectory, "SaveInstaller");
        private static readonly string UrlFile = Path.Combine(ModDir, "url.txt");
        private static readonly string MarkerFile = Path.Combine(ModDir, "installed.marker");

        public override void OnInitializeMelon()
        {
            try
            {
                Directory.CreateDirectory(ModDir);

                if (!File.Exists(UrlFile))
                {
                    MelonLogger.Msg($"SaveInstaller: положи прямую ссылку на файл сохранения в {UrlFile}");
                    return;
                }

                string url = File.ReadAllText(UrlFile).Trim();
                if (string.IsNullOrEmpty(url))
                {
                    MelonLogger.Warning("SaveInstaller: url.txt пустой");
                    return;
                }

                string urlHash = url.GetHashCode().ToString();
                if (File.Exists(MarkerFile) && File.ReadAllText(MarkerFile) == urlHash)
                {
                    MelonLogger.Msg("SaveInstaller: уже установлено для этой ссылки, пропускаю (измени url.txt чтобы переустановить).");
                    return;
                }

                string downloadPath = Path.Combine(ModDir, "download.tmp");
                MelonLogger.Msg("SaveInstaller: скачиваю " + url);

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    client.DownloadFile(url, downloadPath);
                }

                string jsonSourcePath;
                if (IsZip(downloadPath))
                {
                    string extractDir = Path.Combine(ModDir, "extracted");
                    if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                    ZipFile.ExtractToDirectory(downloadPath, extractDir);
                    jsonSourcePath = FindSaveJson(extractDir);
                }
                else
                {
                    jsonSourcePath = downloadPath;
                }

                if (jsonSourcePath == null || !File.Exists(jsonSourcePath))
                {
                    MelonLogger.Error("SaveInstaller: .save.json не найден в скачанном файле");
                    return;
                }

                string savesDir = Path.Combine(
                    "/storage/emulated/0/Android/data", GamePackage, "files", "Saves");
                Directory.CreateDirectory(savesDir);

                foreach (var f in Directory.GetFiles(savesDir, "slot_*.save.json"))
                    File.Delete(f);

                File.Copy(jsonSourcePath, Path.Combine(savesDir, "slot_0.save.json"), true);
                File.WriteAllText(MarkerFile, urlHash);
                MelonLogger.Msg("SaveInstaller: готово, сохранение установлено.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("SaveInstaller: ошибка — " + e);
            }
        }

        private static bool IsZip(string path)
        {
            using var fs = File.OpenRead(path);
            var header = new byte[2];
            return fs.Read(header, 0, 2) == 2 && header[0] == 0x50 && header[1] == 0x4B;
        }

        private static string FindSaveJson(string dir)
        {
            foreach (var f in Directory.GetFiles(dir, "*.save.json", SearchOption.AllDirectories)) return f;
            foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories)) return f;
            return null;
        }
    }
}
