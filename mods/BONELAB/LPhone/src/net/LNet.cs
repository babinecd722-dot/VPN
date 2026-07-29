using System;
using System.Collections.Generic;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.SDK.Modules;
using MelonLoader;

namespace LPhone
{
    internal enum LKind : byte
    {
        Text = 1,
        PhotoChunk = 2,
        CallRequest = 3,
        CallAccept = 4,
        CallDecline = 5,
        CallEnd = 6,
        VideoFrame = 7,
        Typing = 8,
        MicState = 9,
    }

    /// <summary>
    /// Один пакет телефона. Держим плоским: так и сериализация дешёвая,
    /// и на приёме не нужен разбор схем.
    /// </summary>
    public sealed class LPacket : INetSerializable
    {
        public byte Kind;
        public byte From;
        public int A;
        public int B;
        public string Text = "";
        public byte[] Blob = Array.Empty<byte>();

        public int? GetSize() =>
            2 + 8 + 4 + (Text?.Length ?? 0) * 3 + 4 + (Blob?.Length ?? 0);

        public void Serialize(INetSerializer s)
        {
            s.SerializeValue(ref Kind);
            s.SerializeValue(ref From);
            s.SerializeValue(ref A);
            s.SerializeValue(ref B);
            s.SerializeValue(ref Text);
            s.SerializeValue(ref Blob);
        }
    }

    /// <summary>
    /// Приёмник. Тег считается Fusion'ом по имени сборки + полному имени типа,
    /// поэтому у всех с этим модом он совпадает автоматически.
    /// </summary>
    public sealed class LPhoneNetHandler : ModuleMessageHandler
    {
        protected override void OnHandleMessage(ReceivedMessage received)
        {
            try
            {
                var p = received.ReadData<LPacket>();
                if (p == null) return;
                if (received.Sender.HasValue) p.From = received.Sender.Value;
                LNet.Receive(p);
            }
            catch (Exception e) { MelonLogger.Warning("[LPhone] приём: " + e.Message); }
        }
    }

    // ─────────────────────────────────────────────────────────────

    internal sealed class ChatMessage
    {
        public bool Mine;
        public string Text;
        public Photo Image;
        public DateTime At;
    }

    internal enum CallState { None, Outgoing, Incoming, Active }

    /// <summary>
    /// Логика поверх транспорта: чаты, фотографии по кускам, звонки.
    /// Состояние общее на все телефоны игрока — как настоящий аккаунт.
    /// </summary>
    internal static class LNet
    {
        public const int ChunkSize = 4096;

        // ── чаты
        private static readonly Dictionary<byte, List<ChatMessage>> _chats =
            new Dictionary<byte, List<ChatMessage>>();
        private static readonly Dictionary<byte, int> _unread = new Dictionary<byte, int>();

        public static event Action Changed;          // перерисовать UI
        public static event Action<byte> NewMessage; // пришло сообщение (звук)

        public static List<ChatMessage> Chat(byte sid)
        {
            if (!_chats.TryGetValue(sid, out var l)) { l = new List<ChatMessage>(); _chats[sid] = l; }
            return l;
        }

        public static int Unread(byte sid) => _unread.TryGetValue(sid, out var n) ? n : 0;
        public static void MarkRead(byte sid) { _unread[sid] = 0; Fire(); }
        public static int UnreadTotal()
        {
            int n = 0;
            foreach (var kv in _unread) n += kv.Value;
            return n;
        }

        private static void Fire() { try { Changed?.Invoke(); } catch { } }

        // ── звонки
        public static CallState State { get; private set; } = CallState.None;
        public static byte Peer { get; private set; }
        public static bool Video { get; private set; }
        public static bool MicOn = true;
        public static bool SpeakerOn = true;
        public static bool CamOn;
        public static float CallStarted;
        public static TexData RemoteFrame;
        public static float RemoteFrameAt;

        public static event Action CallChanged;
        private static void FireCall() { try { CallChanged?.Invoke(); } catch { } Fire(); }

        // ── исходящие фото, собираемые на приёме
        private sealed class Incoming
        {
            public int Count;
            public byte[][] Parts;
            public int Have;
            public float At;
        }
        private static readonly Dictionary<long, Incoming> _inbox = new Dictionary<long, Incoming>();
        private static int _photoSeq = 1;

        // ─────────────── отправка ───────────────

        public static void SendText(byte to, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Chat(to).Add(new ChatMessage { Mine = true, Text = text, At = DateTime.Now });
            Fire();
            FusionBridge.SendTo(to, new LPacket { Kind = (byte)LKind.Text, Text = text });
        }

        public static void SendPhoto(byte to, Photo photo)
        {
            var bytes = photo?.Wire;
            if (bytes == null || bytes.Length == 0) return;
            Chat(to).Add(new ChatMessage { Mine = true, Image = photo, At = DateTime.Now });
            Fire();

            int id = _photoSeq++;
            int total = (bytes.Length + ChunkSize - 1) / ChunkSize;
            for (int i = 0; i < total; i++)
            {
                int off = i * ChunkSize;
                int len = Math.Min(ChunkSize, bytes.Length - off);
                var part = new byte[len];
                Buffer.BlockCopy(bytes, off, part, 0, len);
                FusionBridge.SendTo(to, new LPacket
                {
                    Kind = (byte)LKind.PhotoChunk,
                    A = id,
                    B = (i << 16) | (total & 0xffff),
                    Blob = part,
                });
            }
        }

        public static void StartCall(byte to, bool video)
        {
            if (State != CallState.None) return;
            State = CallState.Outgoing;
            Peer = to; Video = video; CamOn = video;
            MicOn = true; SpeakerOn = true;
            FireCall();
            FusionBridge.SendTo(to, new LPacket { Kind = (byte)LKind.CallRequest, A = video ? 1 : 0 });
        }

        public static void Accept()
        {
            if (State != CallState.Incoming) return;
            State = CallState.Active;
            CallStarted = UnityEngine.Time.unscaledTime;
            CamOn = Video;
            FireCall();
            FusionBridge.SendTo(Peer, new LPacket { Kind = (byte)LKind.CallAccept, A = Video ? 1 : 0 });
        }

        public static void Decline()
        {
            if (State == CallState.None) return;
            var kind = State == CallState.Incoming ? LKind.CallDecline : LKind.CallEnd;
            FusionBridge.SendTo(Peer, new LPacket { Kind = (byte)kind });
            Reset();
        }

        public static void EndCall()
        {
            if (State == CallState.None) return;
            FusionBridge.SendTo(Peer, new LPacket { Kind = (byte)LKind.CallEnd });
            Reset();
        }

        private static void Reset()
        {
            State = CallState.None;
            Video = false; CamOn = false;
            RemoteFrame = null;
            FireCall();
        }

        public static void SendVideoFrame(byte[] jpg)
        {
            if (State != CallState.Active || jpg == null || jpg.Length == 0) return;
            // кадры шлём надёжным каналом: ненадёжный режет крупные пакеты
            FusionBridge.SendTo(Peer, new LPacket { Kind = (byte)LKind.VideoFrame, Blob = jpg });
        }

        // ─────────────── приём ───────────────

        public static void Receive(LPacket p)
        {
            switch ((LKind)p.Kind)
            {
                case LKind.Text:
                    Chat(p.From).Add(new ChatMessage { Mine = false, Text = p.Text, At = DateTime.Now });
                    Bump(p.From);
                    try { NewMessage?.Invoke(p.From); } catch { }
                    Fire();
                    break;

                case LKind.PhotoChunk:
                    OnChunk(p);
                    break;

                case LKind.CallRequest:
                    if (State != CallState.None)
                    {
                        FusionBridge.SendTo(p.From, new LPacket { Kind = (byte)LKind.CallDecline });
                        break;
                    }
                    State = CallState.Incoming;
                    Peer = p.From;
                    Video = p.A == 1;
                    FireCall();
                    break;

                case LKind.CallAccept:
                    if (State == CallState.Outgoing && p.From == Peer)
                    {
                        State = CallState.Active;
                        CallStarted = UnityEngine.Time.unscaledTime;
                        CamOn = Video;
                        FireCall();
                    }
                    break;

                case LKind.CallDecline:
                case LKind.CallEnd:
                    if (State != CallState.None && p.From == Peer) Reset();
                    break;

                case LKind.VideoFrame:
                    if (State == CallState.Active && p.From == Peer && p.Blob != null && p.Blob.Length > 0)
                    {
                        RemoteFrame = Jpeg.Decode(p.Blob);
                        RemoteFrameAt = UnityEngine.Time.unscaledTime;
                        Fire();
                    }
                    break;
            }
        }

        private static void Bump(byte sid)
        {
            _unread[sid] = Unread(sid) + 1;
        }

        private static void OnChunk(LPacket p)
        {
            long key = ((long)p.From << 32) | (uint)p.A;
            int idx = (p.B >> 16) & 0xffff;
            int total = p.B & 0xffff;
            if (total <= 0 || total > 4096) return;

            if (!_inbox.TryGetValue(key, out var inc))
            {
                inc = new Incoming { Count = total, Parts = new byte[total][] };
                _inbox[key] = inc;
            }
            inc.At = UnityEngine.Time.unscaledTime;
            if (idx < 0 || idx >= inc.Count) return;
            if (inc.Parts[idx] != null) return;
            inc.Parts[idx] = p.Blob;
            inc.Have++;
            if (inc.Have < inc.Count) return;

            _inbox.Remove(key);

            int len = 0;
            for (int i = 0; i < inc.Count; i++) len += inc.Parts[i].Length;
            var png = new byte[len];
            int off = 0;
            for (int i = 0; i < inc.Count; i++)
            {
                Buffer.BlockCopy(inc.Parts[i], 0, png, off, inc.Parts[i].Length);
                off += inc.Parts[i].Length;
            }

            var data = Jpeg.Decode(png);
            if (data == null) return;
            var photo = PhotoStore.Add(data, null, png, FusionBridge.NameOf(p.From));
            Chat(p.From).Add(new ChatMessage { Mine = false, Image = photo, At = DateTime.Now });
            Bump(p.From);
            try { NewMessage?.Invoke(p.From); } catch { }
            Fire();
        }

        /// <summary>Чистим недособранные передачи и мёртвые звонки.</summary>
        public static void Tick()
        {
            if (_inbox.Count > 0)
            {
                float now = UnityEngine.Time.unscaledTime;
                List<long> dead = null;
                foreach (var kv in _inbox)
                    if (now - kv.Value.At > 20f) (dead ??= new List<long>()).Add(kv.Key);
                if (dead != null) foreach (var k in dead) _inbox.Remove(k);
            }

            // собеседник вышел из лобби — вешаем трубку
            if (State != CallState.None && FusionBridge.Present && !FusionBridge.InSession) Reset();
        }
    }
}
