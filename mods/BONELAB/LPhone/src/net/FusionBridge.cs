using System;
using System.Collections.Generic;
using MelonLoader;

namespace LPhone
{
    /// <summary>Контакт в мессенджере.</summary>
    internal sealed class Contact
    {
        public byte SmallID;
        public string Name;
        public bool IsHost;
        public bool IsAuthor;          // постоянный контакт BE PRIME
        public bool Online = true;

        public string Key => IsAuthor ? "author" : "p" + SmallID;

        /// <summary>Инициалы для аватарки, если фото нет.</summary>
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return "?";
                var parts = Name.Split(new[] { ' ', '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2) return ("" + parts[0][0] + parts[1][0]).ToUpperInvariant();
                return Name.Substring(0, Math.Min(2, Name.Length)).ToUpperInvariant();
            }
        }

        /// <summary>Стабильный цвет аватарки по имени — как в мессенджерах.</summary>
        public UnityEngine.Color32 Tint
        {
            get
            {
                if (IsAuthor) return new UnityEngine.Color32(0, 122, 255, 255);
                int h = 17;
                string s = Name ?? "?";
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                float hue = ((h & 0x7fffffff) % 360) / 360f;
                var c = UnityEngine.Color.HSVToRGB(hue, 0.62f, 0.92f);
                return new UnityEngine.Color32((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255), 255);
            }
        }
    }

    /// <summary>
    /// Единственное место, где мод трогает LabFusion. Всё остальное ходит сюда,
    /// поэтому без Fusion телефон продолжает работать (просто без сети).
    ///
    /// Важно: ни одного статического поля типа LabFusion — иначе класс не
    /// инициализируется, когда мода Fusion нет.
    /// </summary>
    internal static class FusionBridge
    {
        private static bool _probed;
        private static bool _present;

        public static bool Present
        {
            get
            {
                if (!_probed)
                {
                    _probed = true;
                    try
                    {
                        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            var n = a.GetName().Name;
                            if (string.Equals(n, "LabFusion", StringComparison.OrdinalIgnoreCase))
                            { _present = true; break; }
                        }
                    }
                    catch { }
                    MelonLogger.Msg("[LPhone] LabFusion: " + (_present ? "есть" : "нет"));
                }
                return _present;
            }
        }

        /// <summary>Мы в сетевой сессии.</summary>
        public static bool InSession
        {
            get
            {
                if (!Present) return false;
                try { return LabFusion.Network.NetworkInfo.HasServer; }
                catch { return false; }
            }
        }

        public static byte LocalSmallID
        {
            get
            {
                if (!Present) return 0;
                try { return LabFusion.Player.PlayerIDManager.LocalSmallID; }
                catch { return 0; }
            }
        }

        public static string LocalName
        {
            get
            {
                if (!Present) return "Me";
                try
                {
                    var id = LabFusion.Player.PlayerIDManager.LocalID;
                    if (id != null && LabFusion.Network.MetadataHelper.TryGetDisplayName(id, out var n)
                        && !string.IsNullOrWhiteSpace(n)) return n;
                    return LabFusion.Player.LocalPlayer.Username;
                }
                catch { return "Me"; }
            }
        }

        /// <summary>Живые игроки лобби, кроме меня.</summary>
        public static void FillPlayers(List<Contact> into)
        {
            if (!Present) return;
            try
            {
                foreach (var id in LabFusion.Player.PlayerIDManager.PlayerIDs)
                {
                    if (id == null || !id.IsValid || id.IsMe) continue;
                    string name = null;
                    try { LabFusion.Network.MetadataHelper.TryGetDisplayName(id, out name); } catch { }
                    if (string.IsNullOrWhiteSpace(name)) name = "Player " + id.SmallID;
                    into.Add(new Contact { SmallID = id.SmallID, Name = name, IsHost = id.IsHost });
                }
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] список игроков: " + e.Message); }
        }

        public static string NameOf(byte smallId)
        {
            if (!Present) return "Player " + smallId;
            try
            {
                if (LabFusion.Player.PlayerIDManager.SmallIDLookup.TryGetValue(smallId, out var id)
                    && id != null
                    && LabFusion.Network.MetadataHelper.TryGetDisplayName(id, out var n)
                    && !string.IsNullOrWhiteSpace(n)) return n;
            }
            catch { }
            return "Player " + smallId;
        }

        // ─────────────── отправка ───────────────

        public static void RegisterHandler()
        {
            if (!Present) return;
            try
            {
                LabFusion.SDK.Modules.ModuleMessageManager.RegisterHandler<LPhoneNetHandler>();
                MelonLogger.Msg("[LPhone] сетевой канал зарегистрирован");
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] сетевой канал: " + e.Message); }
        }

        public static void SendTo(byte target, LPacket p)
        {
            if (!Present) return;
            try
            {
                var route = new LabFusion.Network.MessageRoute(
                    target, LabFusion.Network.NetworkChannel.Reliable);
                LabFusion.Network.MessageRelay.RelayModule<LPhoneNetHandler, LPacket>(p, route);
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] отправка: " + e.Message); }
        }

        public static void SendToUnreliable(byte target, LPacket p)
        {
            if (!Present) return;
            try
            {
                var route = new LabFusion.Network.MessageRoute(
                    target, LabFusion.Network.NetworkChannel.Unreliable);
                LabFusion.Network.MessageRelay.RelayModule<LPhoneNetHandler, LPacket>(p, route);
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] отправка: " + e.Message); }
        }
    }

    /// <summary>
    /// Голос в разговоре ведёт сам Fusion (он уже гоняет звук между игроками),
    /// поэтому кнопки «микрофон» и «динамик» в звонке дёргают его настройки,
    /// а не пытаются построить второй голосовой канал.
    /// </summary>
    internal static class FusionVoice
    {
        private static bool _savedMute, _savedDeaf, _saved;

        public static void BeginCall()
        {
            if (!FusionBridge.Present || _saved) return;
            try
            {
                _savedMute = LabFusion.Preferences.Client.ClientSettings.VoiceChat.Muted.Value;
                _savedDeaf = LabFusion.Preferences.Client.ClientSettings.VoiceChat.Deafened.Value;
                _saved = true;
            }
            catch { }
        }

        public static void EndCall()
        {
            if (!FusionBridge.Present || !_saved) return;
            try
            {
                LabFusion.Preferences.Client.ClientSettings.VoiceChat.Muted.Value = _savedMute;
                LabFusion.Preferences.Client.ClientSettings.VoiceChat.Deafened.Value = _savedDeaf;
            }
            catch { }
            _saved = false;
        }

        public static void SetMuted(bool muted)
        {
            if (!FusionBridge.Present) return;
            try { LabFusion.Preferences.Client.ClientSettings.VoiceChat.Muted.Value = muted; } catch { }
        }

        public static void SetDeafened(bool deaf)
        {
            if (!FusionBridge.Present) return;
            try { LabFusion.Preferences.Client.ClientSettings.VoiceChat.Deafened.Value = deaf; } catch { }
        }
    }

    /// <summary>Список контактов: автор + все игроки лобби.</summary>
    internal static class Contacts
    {
        public static readonly Contact Author = new Contact
        {
            SmallID = 255,
            Name = "BE PRIME",
            IsAuthor = true,
        };

        private static readonly List<Contact> _cache = new List<Contact>();
        private static float _next;

        public static List<Contact> All()
        {
            if (UnityEngine.Time.unscaledTime >= _next)
            {
                _next = UnityEngine.Time.unscaledTime + 2f;
                _cache.Clear();
                _cache.Add(Author);
                FusionBridge.FillPlayers(_cache);
            }
            return _cache;
        }

        public static Contact ByID(byte smallId)
        {
            foreach (var c in All()) if (!c.IsAuthor && c.SmallID == smallId) return c;
            return new Contact { SmallID = smallId, Name = FusionBridge.NameOf(smallId) };
        }
    }
}
