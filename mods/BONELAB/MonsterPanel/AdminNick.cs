using System;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using CS = LabFusion.Preferences.Client.ClientSettings;

namespace MonsterPanel
{
    /// <summary>
    /// ADMIN NICKNAME — looks like official SLZ staff to the whole lobby:
    /// - Typewriter nametag "DEV. OF BONELAB" (Metadata only — no freeze)
    /// - Stock Fusion gold NameTagHue
    /// - Description everyone sees in the player card
    /// - PermissionLevel OWNER (Fusion reads remote metadata → Permissions: OWNER)
    /// </summary>
    internal static class AdminNick
    {
        public static bool Enabled { get; private set; }

        private const string Phrase = "DEV. OF BONELAB";
        private const string OfficialDescription =
            "Stress Level Zero · Official BONELAB Developer";
        private const string OfficialUsername = "dev.bonelab";
        private const string OwnerPerm = "OWNER";

        private const float GoldHue = 0.12f;
        private const float GoldSat = 0.9f;
        private const float GoldVal = 1f;

        private const float LetterInterval = 0.14f;
        private const float HoldFull = 1.8f;
        private const float CredRefresh = 4f; // re-assert OWNER/description if Fusion overwrites

        private static int _len;
        private static bool _shrinking;
        private static float _letterTimer;
        private static float _holdTimer;
        private static float _credTimer;

        private static string _savedNick;
        private static bool _haveSavedNick;
        private static string _savedDesc;
        private static bool _haveSavedDesc;
        private static string _savedPerm;
        private static bool _haveSavedPerm;
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

            // Keep staff credentials sticky (Fusion may reset PermissionLevel on host events).
            _credTimer -= dt;
            if (_credTimer <= 0f)
            {
                _credTimer = CredRefresh;
                ApplyCredentials(quiet: true);
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

            PushNickMeta(CurrentPlain());
        }

        private static void Enable()
        {
            Enabled = true;
            _len = 0;
            _shrinking = false;
            _letterTimer = 0f;
            _holdTimer = 0f;
            _credTimer = CredRefresh;
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

            try
            {
                _savedDesc = CS.Description.Value;
                _haveSavedDesc = true;
            }
            catch
            {
                _haveSavedDesc = false;
                _savedDesc = "";
            }

            try
            {
                _savedPerm = LabFusion.Player.LocalPlayer.Metadata?.PermissionLevel?.GetValue() ?? "";
                _haveSavedPerm = true;
            }
            catch
            {
                _haveSavedPerm = false;
                _savedPerm = "";
            }

            try
            {
                _savedHue = CS.NameTagHue.Value;
                _savedSat = CS.NameTagSaturation.Value;
                _savedVal = CS.NameTagValue.Value;
                _haveSavedColor = true;

                CS.NameTagHue.Value = GoldHue;
                CS.NameTagSaturation.Value = GoldSat;
                CS.NameTagValue.Value = GoldVal;
            }
            catch
            {
                _haveSavedColor = false;
            }

            try
            {
                CS.NicknameVisibility.Value = LabFusion.Senders.NicknameVisibility.SHOW;
                CS.Nickname.Value = Phrase;
                CS.Description.Value = OfficialDescription; // → metadata via OnValueChanged
                SendSettingsOnce();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME enable: " + e.Message); }

            ApplyCredentials(quiet: false);
            PushNickMeta("");
            MelonLogger.Msg("ADMIN NICKNAME: ON (typewriter + OWNER + official description)");
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
                CS.Description.Value = _haveSavedDesc ? (_savedDesc ?? "") : "";
                SendSettingsOnce();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME off prefs: " + e.Message); }

            try
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                if (_haveSavedPerm)
                    md?.PermissionLevel?.SetValue(_savedPerm ?? "DEFAULT");
                else
                    md?.PermissionLevel?.SetValue("DEFAULT");
            }
            catch { }

            _haveSavedNick = false;
            _haveSavedDesc = false;
            _haveSavedPerm = false;
            _haveSavedColor = false;
            _lastMeta = "";
            MelonLogger.Msg("ADMIN NICKNAME: OFF");
        }

        /// <summary>
        /// Credentials every client can read without our mod:
        /// roster username, avatar title, Description, PermissionLevel OWNER.
        /// </summary>
        private static void ApplyCredentials(bool quiet)
        {
            try
            {
                var md = LabFusion.Player.LocalPlayer.Metadata;
                if (md == null) return;
                md.Username?.SetValue(OfficialUsername);
                md.AvatarTitle?.SetValue(Phrase);
                md.Description?.SetValue(OfficialDescription);
                md.PermissionLevel?.SetValue(OwnerPerm);
                if (!quiet)
                    MelonLogger.Msg("ADMIN NICKNAME: credentials set (OWNER + description)");
            }
            catch (Exception e)
            {
                if (!quiet) MelonLogger.Warning("ADMIN NICKNAME credentials: " + e.Message);
            }
        }

        private static string CurrentPlain()
        {
            if (_len <= 0) return "";
            if (_len >= Phrase.Length) return Phrase;
            return Phrase.Substring(0, _len);
        }

        private static void PushNickMeta(string plain)
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
