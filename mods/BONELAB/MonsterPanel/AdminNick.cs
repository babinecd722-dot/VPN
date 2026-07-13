using System;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// ADMIN NICKNAME — typewriter + color shimmer. Phrase: "DEV. OF BONELAB"
    /// (15 chars). With TMP `<#rgb>…</color>` = 29 ≤ Fusion's 32-char limit.
    /// Network sync throttled to avoid lobby hitching.
    /// </summary>
    internal static class AdminNick
    {
        public static bool Enabled { get; private set; }

        private const string Phrase = "DEV. OF BONELAB";

        // Short hex colors (TMP `<#rgb>`). Gold → cyan → white → amber.
        private static readonly string[] Palette =
        {
            "fd0", // gold
            "0ef", // cyan
            "fff", // white
            "f80", // amber
        };

        private const float LetterInterval = 0.11f;
        private const float HoldFull = 1.8f;
        private const float ColorInterval = 0.45f;
        private const float NetMinInterval = 0.4f;
        private const int FusionNickLimit = 32;

        private static int _len;
        private static bool _shrinking;
        private static float _letterTimer;
        private static float _holdTimer;
        private static int _colorIdx;
        private static float _colorTimer;
        private static string _lastSent = "";
        private static float _netCooldown;
        private static string _savedNick;
        private static bool _haveSaved;

        public static void Install(Page root)
        {
            root.CreateBool("ADMIN NICKNAME", new Color(1f, 0.82f, 0.12f), Enabled, v =>
            {
                if (v) Enable();
                else Disable();
            });
        }

        public static void Tick()
        {
            if (!Enabled) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) dt = 0.016f;

            _netCooldown -= dt;
            _colorTimer -= dt;
            if (_colorTimer <= 0f)
            {
                _colorTimer = ColorInterval;
                _colorIdx = (_colorIdx + 1) % Palette.Length;
                PushIfDue(force: false);
            }

            if (!_shrinking && _len >= Phrase.Length)
            {
                if (_holdTimer > 0f)
                {
                    _holdTimer -= dt;
                    return;
                }
                _shrinking = true;
            }

            _letterTimer -= dt;
            if (_letterTimer > 0f) return;
            _letterTimer = LetterInterval;

            if (_shrinking)
            {
                _len--;
                if (_len <= 0)
                {
                    _len = 0;
                    _shrinking = false;
                }
            }
            else
            {
                _len++;
                if (_len >= Phrase.Length)
                {
                    _len = Phrase.Length;
                    _holdTimer = HoldFull;
                }
            }

            PushIfDue(force: false);
        }

        private static void Enable()
        {
            Enabled = true;
            _len = 0;
            _shrinking = false;
            _letterTimer = 0f;
            _holdTimer = 0f;
            _colorIdx = 0;
            _colorTimer = 0f;
            _netCooldown = 0f;
            _lastSent = "";

            try
            {
                _savedNick = LabFusion.Preferences.Client.ClientSettings.Nickname.Value;
                _haveSaved = true;
            }
            catch
            {
                _haveSaved = false;
                _savedNick = "";
            }

            try
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                md?.Username?.SetValue("dev.bonelab");
                md?.AvatarTitle?.SetValue("DEV. OF BONELAB");
            }
            catch { }

            LabFusion.Preferences.Client.ClientSettings.NicknameVisibility.Value =
                LabFusion.Senders.NicknameVisibility.SHOW;

            PushIfDue(force: true);
            MelonLogger.Msg("ADMIN NICKNAME: ON (DEV. OF BONELAB)");
        }

        private static void Disable()
        {
            Enabled = false;
            try
            {
                if (_haveSaved)
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = _savedNick ?? "";
                else
                    LabFusion.Preferences.Client.ClientSettings.Nickname.Value = "";
                SendSettings();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME off: " + e.Message); }

            _haveSaved = false;
            _lastSent = "";
            MelonLogger.Msg("ADMIN NICKNAME: OFF");
        }

        private static void PushIfDue(bool force)
        {
            string painted = Paint(CurrentPlain());
            if (!force && painted == _lastSent) return;
            if (!force && _netCooldown > 0f) return;

            _netCooldown = NetMinInterval;
            _lastSent = painted;
            try
            {
                if (painted.Length > FusionNickLimit)
                    painted = painted.Substring(0, FusionNickLimit);
                LabFusion.Preferences.Client.ClientSettings.Nickname.Value = painted;
                SendSettings();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME sync: " + e.Message); }
        }

        private static string CurrentPlain()
        {
            if (_len <= 0) return "";
            if (_len >= Phrase.Length) return Phrase;
            return Phrase.Substring(0, _len);
        }

        private static string Paint(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return " ";
            string hex = Palette[_colorIdx % Palette.Length];
            int budget = FusionNickLimit - (6 + 8); // <#rgb> + </color>
            if (plain.Length > budget)
                return plain.Length <= FusionNickLimit ? plain : plain.Substring(0, FusionNickLimit);
            return "<#" + hex + ">" + plain + "</color>";
        }

        private static void SendSettings()
        {
            try
            {
                AccessTools.Method("LabFusion.Preferences.FusionPreferences:SendClientSettings")
                    ?.Invoke(null, null);
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME send: " + e.Message); }
        }
    }
}
