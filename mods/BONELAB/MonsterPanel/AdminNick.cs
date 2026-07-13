using System;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace MonsterPanel
{
    /// <summary>
    /// ADMIN NICKNAME — typewriter + color shimmer nametag that reads like an
    /// internal SLZ/Marrow staff handle. Network sync is throttled so the lobby
    /// never stutters from ClientSettings spam.
    ///
    /// Visible phrase: "MARROW ARCHITECT" (16 chars). Short TMP color prefix
    /// `<#rgb>…</color>` keeps the full painted string ≤ 32 (Fusion limit).
    /// </summary>
    internal static class AdminNick
    {
        public static bool Enabled { get; private set; }

        // Official-looking staff title — uncommon enough to sell the bit.
        private const string Phrase = "MARROW ARCHITECT";

        // Short hex colors (TMP `<#rgb>`). Gold → cyan → white → amber.
        private static readonly string[] Palette =
        {
            "fd0", // gold
            "0ef", // cyan
            "fff", // white
            "f80", // amber
        };

        private const float LetterInterval = 0.11f;   // typewriter cadence (local)
        private const float HoldFull = 1.6f;           // pause on completed phrase
        private const float ColorInterval = 0.45f;     // shimmer step
        private const float NetMinInterval = 0.4f;     // never sync faster than this
        private const int FusionNickLimit = 32;

        private static int _len;                       // revealed letter count
        private static bool _shrinking;                // reverse typewriter
        private static float _letterTimer;
        private static float _holdTimer;
        private static int _colorIdx;
        private static float _colorTimer;
        private static string _lastSent = "";
        private static float _netCooldown;
        private static string _savedNick;               // restore on disable
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
                // Color change alone may push a sync (throttled).
                PushIfDue(force: false);
            }

            // Hold on full phrase, then reverse / rebuild.
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

            // Roster username: static staff-looking handle (not animated — no spam).
            try
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                md?.Username?.SetValue("marrow.architect");
                md?.AvatarTitle?.SetValue("MARROW");
            }
            catch { }

            LabFusion.Preferences.Client.ClientSettings.NicknameVisibility.Value =
                LabFusion.Senders.NicknameVisibility.SHOW;

            PushIfDue(force: true);
            MelonLogger.Msg("ADMIN NICKNAME: ON");
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

        /// <summary>
        /// Wrap plain text in a short TMP color tag. Empty → single space (hidden-ish).
        /// Budget: 6 (`<#rgb>`) + text + 8 (`</color>`) ≤ 32 → text ≤ 18.
        /// </summary>
        private static string Paint(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return " ";
            string hex = Palette[_colorIdx % Palette.Length];
            // If somehow over budget, drop color and send plain.
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
