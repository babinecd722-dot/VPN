using System;
using System.Text;
using BoneLib.BoneMenu;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using CS = LabFusion.Preferences.Client.ClientSettings;

namespace MonsterPanel
{
    /// <summary>
    /// ADMIN NICKNAME — looks like official SLZ staff to the whole lobby:
    /// - Typewriter nametag "DEV. OF BONELAB" with a curated OWNER/dev shimmer
    ///   (gold → champagne → platinum → lab cyan — not a toy HSV rainbow)
    ///   AnimatedName-style per-letter &lt;color=#RRGGBB&gt; via Metadata.Nickname only
    /// - White NameTag multiply so rich colors stay true (sat=0)
    /// - Description everyone sees in the player card
    /// - PermissionLevel OWNER (Fusion reads remote metadata → Permissions: OWNER)
    /// - AvatarModID = -1 (nil) → no mod.io face; Fusion shows Mods stub icon
    /// </summary>
    internal static class AdminNick
    {
        public static bool Enabled { get; private set; }

        private const string Phrase = "DEV. OF BONELAB";
        private const string OfficialDescription =
            "Stress Level Zero · Official BONELAB Developer";
        private const string OfficialUsername = "dev.bonelab";
        private const string OwnerPerm = "OWNER";
        /// <summary>Fusion ElementIconHelper: modID == -1 skips mod.io thumbnail (placeholder only).</summary>
        private const int NilAvatarModId = -1;

        // White multiply so per-letter rich colors are not tinted.
        private const float ShimmerMulHue = 0f;
        private const float ShimmerMulSat = 0f;
        private const float ShimmerMulVal = 1f;

        private const float LetterInterval = 0.14f;
        private const float HoldFull = 1.8f;
        private const float CredRefresh = 4f;

        /// <summary>Network metadata push rate — AnimatedName-style timer, not every frame.</summary>
        private const float ShimmerNetInterval = 0.1f; // 10 Hz
        /// <summary>One full palette lap (~4s) — slower = more “premium staff”, less disco.</summary>
        private const float ShimmerPhaseSpeed = 0.25f; // cycles / second

        /// <summary>
        /// Official-dev palette: Fusion OWNER gold, champagne, ivory, platinum, lab cyan.
        /// Loops seamlessly — no green/magenta circus.
        /// </summary>
        private static readonly Color[] DevPalette =
        {
            new Color(1.00f, 0.88f, 0.40f), // soft gold
            new Color(1.00f, 0.82f, 0.12f), // Fusion OWNER gold
            new Color(1.00f, 0.72f, 0.22f), // amber
            new Color(1.00f, 0.94f, 0.72f), // champagne / ivory
            new Color(0.92f, 0.95f, 1.00f), // platinum
            new Color(0.55f, 0.88f, 1.00f), // lab cyan
            new Color(0.40f, 0.72f, 1.00f), // soft staff blue
            new Color(0.85f, 0.90f, 1.00f), // cool silver
            new Color(1.00f, 0.90f, 0.50f), // return toward gold
        };

        private static int _len;
        private static bool _shrinking;
        private static float _letterTimer;
        private static float _holdTimer;
        private static float _credTimer;
        private static float _shimmerPhase; // 0..1
        private static float _shimmerNetTimer;
        private static bool _lenDirty;

        private static string _savedNick;
        private static bool _haveSavedNick;
        private static string _savedDesc;
        private static bool _haveSavedDesc;
        private static string _savedPerm;
        private static bool _haveSavedPerm;
        private static int _savedAvatarModId;
        private static bool _haveSavedAvatarModId;
        private static float _savedHue, _savedSat, _savedVal;
        private static bool _haveSavedColor;
        private static string _lastMeta = "";

        private static readonly StringBuilder _sb = new StringBuilder(256);
        private static readonly char[] Hex = "0123456789ABCDEF".ToCharArray();

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

            // Curated palette scroll (local clock; network push is throttled below).
            _shimmerPhase += ShimmerPhaseSpeed * dt;
            if (_shimmerPhase >= 1f) _shimmerPhase -= 1f;

            // Typewriter length machine (shimmer keeps running during the full-phrase hold).
            if (!_shrinking && _len >= Phrase.Length)
            {
                if (_holdTimer > 0f)
                    _holdTimer -= dt;
                else
                    _shrinking = true;
            }
            else
            {
                _letterTimer -= dt;
                if (_letterTimer <= 0f)
                {
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

                    _lenDirty = true;
                }
            }

            _shimmerNetTimer -= dt;
            if (!_lenDirty && _shimmerNetTimer > 0f) return;
            _shimmerNetTimer = ShimmerNetInterval;
            _lenDirty = false;

            PushNickMeta(BuildDevShimmer(CurrentPlain(), _shimmerPhase));
        }

        private static void Enable()
        {
            Enabled = true;
            _len = 0;
            _shrinking = false;
            _letterTimer = 0f;
            _holdTimer = 0f;
            _credTimer = CredRefresh;
            _shimmerPhase = 0f;
            _shimmerNetTimer = 0f;
            _lenDirty = true;
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
                _savedAvatarModId = LabFusion.Player.LocalPlayer.Metadata?.AvatarModID?.GetValue() ?? NilAvatarModId;
                _haveSavedAvatarModId = true;
            }
            catch
            {
                _haveSavedAvatarModId = false;
                _savedAvatarModId = NilAvatarModId;
            }

            try
            {
                _savedHue = CS.NameTagHue.Value;
                _savedSat = CS.NameTagSaturation.Value;
                _savedVal = CS.NameTagValue.Value;
                _haveSavedColor = true;

                // White multiply — rich-text letter colors stay true.
                CS.NameTagHue.Value = ShimmerMulHue;
                CS.NameTagSaturation.Value = ShimmerMulSat;
                CS.NameTagValue.Value = ShimmerMulVal;
            }
            catch
            {
                _haveSavedColor = false;
            }

            try
            {
                CS.NicknameVisibility.Value = LabFusion.Senders.NicknameVisibility.SHOW;
                CS.Nickname.Value = Phrase; // prefs once; live display = Metadata rainbow
                CS.Description.Value = OfficialDescription;
                SendSettingsOnce();
            }
            catch (Exception e) { MelonLogger.Warning("ADMIN NICKNAME enable: " + e.Message); }

            ApplyCredentials(quiet: false);
            PushNickMeta(BuildDevShimmer("", _shimmerPhase));
            MelonLogger.Msg("ADMIN NICKNAME: ON (dev-gold shimmer + OWNER + nil avatar preview)");
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

                if (_haveSavedAvatarModId)
                    md?.AvatarModID?.SetValue(_savedAvatarModId);
            }
            catch { }

            _haveSavedNick = false;
            _haveSavedDesc = false;
            _haveSavedPerm = false;
            _haveSavedAvatarModId = false;
            _haveSavedColor = false;
            _lastMeta = "";
            MelonLogger.Msg("ADMIN NICKNAME: OFF");
        }

        /// <summary>
        /// Credentials every client can read without our mod:
        /// roster username, avatar title, Description, PermissionLevel OWNER,
        /// AvatarModID=-1 (no mod.io face → Fusion Mods stub).
        /// Re-applied every CredRefresh so lobby joins / avatar swaps don't restore the face.
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
                md.AvatarModID?.SetValue(NilAvatarModId);
                if (!quiet)
                    MelonLogger.Msg("ADMIN NICKNAME: credentials set (OWNER + nil avatar preview)");
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

        /// <summary>
        /// Scrolling curated OWNER/dev palette across letters (AnimatedName open-tag style).
        /// Phase 0..1 walks the gold↔cyan staff loop; letters sample along the same gradient.
        /// </summary>
        private static string BuildDevShimmer(string plain, float phase)
        {
            if (string.IsNullOrEmpty(plain)) return " ";

            int colored = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                if (plain[i] != ' ') colored++;
            }
            if (colored < 1) colored = 1;

            _sb.Clear();
            int vi = 0;
            for (int i = 0; i < plain.Length; i++)
            {
                char c = plain[i];
                if (c == ' ')
                {
                    _sb.Append(' ');
                    continue;
                }

                // Spread letters across ~70% of one palette lap so the name reads as a band, not noise.
                float t = phase + vi * (0.7f / colored);
                t -= Mathf.Floor(t);
                Color col = SampleDevPalette(t);
                int r = Mathf.Clamp(Mathf.RoundToInt(col.r * 255f), 0, 255);
                int g = Mathf.Clamp(Mathf.RoundToInt(col.g * 255f), 0, 255);
                int b = Mathf.Clamp(Mathf.RoundToInt(col.b * 255f), 0, 255);

                _sb.Append("<color=#");
                AppendByteHex(r);
                AppendByteHex(g);
                AppendByteHex(b);
                _sb.Append('>');
                _sb.Append(c);
                vi++;
            }

            return _sb.ToString();
        }

        private static Color SampleDevPalette(float t01)
        {
            int n = DevPalette.Length;
            float x = t01 * n;
            int i0 = Mathf.FloorToInt(x) % n;
            if (i0 < 0) i0 += n;
            int i1 = (i0 + 1) % n;
            float f = x - Mathf.Floor(x);
            // Smoothstep — less “cheap lerp flash” between stops.
            f = f * f * (3f - 2f * f);
            return Color.Lerp(DevPalette[i0], DevPalette[i1], f);
        }

        private static void AppendByteHex(int v)
        {
            _sb.Append(Hex[(v >> 4) & 0xF]);
            _sb.Append(Hex[v & 0xF]);
        }

        private static void PushNickMeta(string rich)
        {
            if (rich == _lastMeta) return;
            _lastMeta = rich;
            try
            {
                string v = string.IsNullOrEmpty(rich) ? " " : rich;
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
