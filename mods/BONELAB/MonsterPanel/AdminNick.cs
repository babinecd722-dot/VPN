using System;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using CS = LabFusion.Preferences.Client.ClientSettings;

namespace MonsterPanel
{
    /// <summary>
    /// ADMIN NICKNAME — typewriter "DEV. OF BONELAB" without lobby freezes.
    ///
    /// Why it lagged before:
    /// - ClientSettings.Nickname.Value saves MelonPreferences to disk on EVERY set
    /// - SendClientSettings broadcasts the full client blob
    /// - Color cycling did both every ~0.4s
    ///
    /// Fix:
    /// - Typewriter updates LocalPlayer.Metadata.Nickname only (small metadata packet)
    /// - Stock Fusion nametag color via NameTagHue/Sat/Value — set ONCE (RigNameTag
    ///   applies Graphic.color; that is the built-in nametag tint)
    /// - ClientSettings.Nickname + SendClientSettings only on enable/disable
    /// </summary>
    internal static class AdminNick
    {
        public static bool Enabled { get; private set; }

        private const string Phrase = "DEV. OF BONELAB";

        // Stock Fusion HSV for nametag Graphic.color (gold).
        private const float GoldHue = 0.12f;
        private const float GoldSat = 0.9f;
        private const float GoldVal = 1f;

        private const float LetterInterval = 0.14f;
        private const float HoldFull = 1.8f;

        private static int _len;
        private static bool _shrinking;
        private static float _letterTimer;
        private static float _holdTimer;

        private static string _savedNick;
        private static bool _haveSavedNick;
        private static float _savedHue, _savedSat, _savedVal;
        private static bool _haveSavedColor;
        private static string _lastMeta = "";

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

            PushMeta(CurrentPlain());
        }

        private static void Enable()
        {
            Enabled = true;
            _len = 0;
            _shrinking = false;
            _letterTimer = 0f;
            _holdTimer = 0f;
            _lastMeta = "";

            try
            {
                _savedNick = CS.Nickname.Value;
                _haveSavedNick = true;
            }
            catch
            {
                _haveSavedNick = false;
                _savedNick = "";
            }

            // Stock nametag tint (Fusion NameTag HSV → RigNameTag.Color). Once only.
            try
            {
                _savedHue = CS.NameTagHue.Value;
                _savedSat = CS.NameTagSaturation.Value;
                _savedVal = CS.NameTagValue.Value;
                _haveSavedColor = true;

                CS.NameTagHue.Value = GoldHue;
                CS.NameTagSaturation.Value = GoldSat;
                CS.NameTagValue.Value = GoldVal;
                // NameTag* prefs are CLIENT_UPDATE → each set already SendClientSettings.
            }
            catch
            {
                _haveSavedColor = false;
            }

            try
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                md?.Username?.SetValue("dev.bonelab");
                md?.AvatarTitle?.SetValue(Phrase);
            }
            catch { }

            try
            {
                CS.NicknameVisibility.Value = LabFusion.Senders.NicknameVisibility.SHOW;
                // Prefs copy once (disk write once). Animation goes through Metadata only.
                CS.Nickname.Value = Phrase;
                SendSettingsOnce();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME enable: " + e.Message); }

            PushMeta("");
            MelonLogger.Msg("ADMIN NICKNAME: ON (typewriter + stock NameTag gold)");
        }

        private static void Disable()
        {
            Enabled = false;
            try
            {
                if (_haveSavedColor)
                {
                    CS.NameTagHue.Value = _savedHue;
                    CS.NameTagSaturation.Value = _savedSat;
                    CS.NameTagValue.Value = _savedVal;
                }
                CS.Nickname.Value = _haveSavedNick ? (_savedNick ?? "") : "";
                SendSettingsOnce();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME off: " + e.Message); }

            _haveSavedNick = false;
            _haveSavedColor = false;
            _lastMeta = "";
            MelonLogger.Msg("ADMIN NICKNAME: OFF");
        }

        private static string CurrentPlain()
        {
            if (_len <= 0) return "";
            if (_len >= Phrase.Length) return Phrase;
            return Phrase.Substring(0, _len);
        }

        /// <summary>
        /// Lightweight path: metadata packet only — no MelonPreferences SaveToFile,
        /// no full SendClientSettings. RigNameTag reads this via OnMetadataChanged.
        /// </summary>
        private static void PushMeta(string plain)
        {
            if (plain == _lastMeta) return;
            _lastMeta = plain;
            try
            {
                string v = string.IsNullOrEmpty(plain) ? " " : plain;
                LabFusion.Player.LocalPlayer.Metadata?.Nickname?.SetValue(v);
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME meta: " + e.Message); }
        }

        private static void SendSettingsOnce()
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
