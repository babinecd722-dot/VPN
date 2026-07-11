using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SaveInstaller.SaveInstallerMod), "SaveInstaller", "1.0.0", "you")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace SaveInstaller
{
    public class SaveInstallerMod : MelonMod
    {
        // Ссылка вшита напрямую — url.txt не нужен.
        private const string SaveUrl = "https://g-3809.modapi.io/v1/games/3809/mods/2855181/files/5611752/download";

        private const string GamePackage = "com.StressLevelZero.BONELAB";
        private const float StatusDisplaySeconds = 15f;

        private static readonly string ModDir = Path.Combine(MelonHandler.ModsDirectory, "SaveInstaller");
        private static readonly string MarkerFile = Path.Combine(ModDir, "installed.marker");

        private static volatile string _status;
        private static volatile bool _statusIsError;
        private static float _statusShownAt;
        private static GUIStyle _style;

        public override void OnInitializeMelon()
        {
            // Скачивание — в фоновом потоке, чтобы не подвесить старт игры.
            new Thread(InstallSave) { IsBackground = true }.Start();
        }

        private static void InstallSave()
        {
            try
            {
                Directory.CreateDirectory(ModDir);

                string urlHash = SaveUrl.GetHashCode().ToString();
                if (File.Exists(MarkerFile) && File.ReadAllText(MarkerFile) == urlHash)
                {
                    SetStatus("SaveInstaller: уже установлено, пропускаю.", error: false);
                    return;
                }

                string downloadPath = Path.Combine(ModDir, "download.tmp");
                MelonLogger.Msg("SaveInstaller: скачиваю " + SaveUrl);

                using (var client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0");
                    client.DownloadFile(SaveUrl, downloadPath);
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
                    SetStatus("SaveInstaller: ОШИБКА — .save.json не найден в скачанном файле", error: true);
                    return;
                }

                string savesDir = Path.Combine(
                    "/storage/emulated/0/Android/data", GamePackage, "files", "Saves");
                Directory.CreateDirectory(savesDir);

                foreach (var f in Directory.GetFiles(savesDir, "slot_*.save.json"))
                    File.Delete(f);

                File.Copy(jsonSourcePath, Path.Combine(savesDir, "slot_0.save.json"), true);
                File.WriteAllText(MarkerFile, urlHash);
                SetStatus("SaveInstaller: УСПЕШНО — сохранение установлено", error: false);
            }
            catch (Exception e)
            {
                MelonLogger.Error("SaveInstaller: ошибка — " + e);
                SetStatus("SaveInstaller: ОШИБКА — " + e.Message, error: true);
            }
        }

        private static void SetStatus(string text, bool error)
        {
            _status = text;
            _statusIsError = error;
            _statusShownAt = Time.realtimeSinceStartup;
            if (error) MelonLogger.Error(text);
            else MelonLogger.Msg(text);
        }

        // Простая on-screen надпись поверх экрана (IMGUI). На Il2Cpp-сборках Unity
        // это не всегда гарантированно рендерится так же, как на Mono — проверено
        // только компиляцией против референсной сборки, не на реальном устройстве.
        public override void OnGUI()
        {
            if (_status == null) return;
            if (Time.realtimeSinceStartup - _statusShownAt > StatusDisplaySeconds) return;

            _style ??= new GUIStyle(GUI.skin.box) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
            var prevColor = GUI.color;
            GUI.color = _statusIsError ? Color.red : Color.green;
            GUI.Label(new Rect(20, 20, Screen.width - 40, 60), _status, _style);
            GUI.color = prevColor;
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
