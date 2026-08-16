using System.Buffers.Binary;
using System.IO.Compression;

namespace EosJoinProbe;

/// <summary>
/// LabFusion voice: MessagePrefix → PlayerVoiceChatData → Deflate(raw) → G.711 A-law → PCM16 @ 48 kHz.
/// </summary>
internal static class VoiceCodec
{
    public const int SampleRate = 48000;

    private static readonly short[] ALawTable = BuildALawTable();

    public static bool TryParseVoice(ReadOnlySpan<byte> packet, out byte senderSmallId, out short[] pcm)
    {
        senderSmallId = 0;
        pcm = Array.Empty<short>();
        if (packet.Length < 8 || packet[0] != 67)
            return false;

        int off = 1;
        byte relay = packet[off++];
        off++; // network channel
        if (relay != 0)
        {
            bool hasSender = packet[off++] != 0;
            if (hasSender)
            {
                if (off >= packet.Length) return false;
                senderSmallId = packet[off++];
            }
        }

        if (!TryReadBytes(packet, ref off, out var outer))
            return false;
        // PlayerVoiceChatData = length-prefixed compressed A-law
        int innerOff = 0;
        if (!TryReadBytes(outer, ref innerOff, out var compressed))
            return false;
        if (compressed.Length == 0)
            return false;

        byte[] alaw;
        try { alaw = InflateRaw(compressed); }
        catch { return false; }
        if (alaw.Length == 0) return false;

        pcm = new short[alaw.Length];
        for (int i = 0; i < alaw.Length; i++)
            pcm[i] = ALawTable[alaw[i]];
        return true;
    }

    /// <summary>Best-effort ConnectionResponse (tag 2) → PlatformID + SmallID + Username.</summary>
    public static bool TryParseConnectionResponse(ReadOnlySpan<byte> packet, out string platformId, out byte smallId, out string username)
    {
        platformId = null;
        smallId = 0;
        username = null;
        if (packet.Length < 16 || packet[0] != 2)
            return false;

        int off = 1;
        byte relay = packet[off++];
        off++; // channel
        if (relay != 0)
        {
            bool hasSender = packet[off++] != 0;
            if (hasSender) off++; // skip sender byte
        }

        if (!TryReadBytes(packet, ref off, out var bodyArr))
            return false;
        ReadOnlySpan<byte> body = bodyArr;

        int b = 0;
        if (!TryReadString(body, ref b, out platformId) || string.IsNullOrEmpty(platformId))
            return false;
        if (b >= body.Length) return false;
        smallId = body[b++];

        // metadata dictionary
        if (b + 4 > body.Length) return false;
        int metaCount = BinaryPrimitives.ReadInt32BigEndian(body.Slice(b, 4));
        b += 4;
        if (metaCount < 0 || metaCount > 256) return false;
        for (int i = 0; i < metaCount; i++)
        {
            if (!TryReadString(body, ref b, out var key)) return false;
            if (!TryReadString(body, ref b, out var val)) return false;
            if (key is "Username" or "username")
                username = val;
        }
        return true;
    }

    public static void WriteWav(string path, short[] pcm, int sampleRate = SampleRate)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);
        int dataBytes = pcm.Length * 2;
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + dataBytes);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataBytes);
        foreach (short s in pcm) bw.Write(s);
    }

    private static bool TryReadBytes(ReadOnlySpan<byte> buf, ref int off, out byte[] data)
    {
        data = Array.Empty<byte>();
        if (off + 4 > buf.Length) return false;
        int len = BinaryPrimitives.ReadInt32BigEndian(buf.Slice(off, 4));
        off += 4;
        if (len < 0 || off + len > buf.Length) return false;
        data = buf.Slice(off, len).ToArray();
        off += len;
        return true;
    }

    private static bool TryReadString(ReadOnlySpan<byte> buf, ref int off, out string s)
    {
        s = null;
        if (off + 4 > buf.Length) return false;
        int len = BinaryPrimitives.ReadInt32BigEndian(buf.Slice(off, 4));
        off += 4;
        if (len < 0)
        {
            s = null;
            return true;
        }
        if (off + len > buf.Length) return false;
        s = System.Text.Encoding.UTF8.GetString(buf.Slice(off, len));
        off += len;
        return true;
    }

    private static byte[] InflateRaw(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static short[] BuildALawTable()
    {
        // Exact GroovyCodecs / LabFusion ALawDecoder table
        return new short[]
        {
            -5504, -5248, -6016, -5760, -4480, -4224, -4992, -4736, -7552, -7296,
            -8064, -7808, -6528, -6272, -7040, -6784, -2752, -2624, -3008, -2880,
            -2240, -2112, -2496, -2368, -3776, -3648, -4032, -3904, -3264, -3136,
            -3520, -3392, -22016, -20992, -24064, -23040, -17920, -16896, -19968, -18944,
            -30208, -29184, -32256, -31232, -26112, -25088, -28160, -27136, -11008, -10496,
            -12032, -11520, -8960, -8448, -9984, -9472, -15104, -14592, -16128, -15616,
            -13056, -12544, -14080, -13568, -344, -328, -376, -360, -280, -264,
            -312, -296, -472, -456, -504, -488, -408, -392, -440, -424,
            -88, -72, -120, -104, -24, -8, -56, -40, -216, -200,
            -248, -232, -152, -136, -184, -168, -1376, -1312, -1504, -1440,
            -1120, -1056, -1248, -1184, -1888, -1824, -2016, -1952, -1632, -1568,
            -1760, -1696, -688, -656, -752, -720, -560, -528, -624, -592,
            -944, -912, -1008, -976, -816, -784, -880, -848, 5504, 5248,
            6016, 5760, 4480, 4224, 4992, 4736, 7552, 7296, 8064, 7808,
            6528, 6272, 7040, 6784, 2752, 2624, 3008, 2880, 2240, 2112,
            2496, 2368, 3776, 3648, 4032, 3904, 3264, 3136, 3520, 3392,
            22016, 20992, 24064, 23040, 17920, 16896, 19968, 18944, 30208, 29184,
            32256, 31232, 26112, 25088, 28160, 27136, 11008, 10496, 12032, 11520,
            8960, 8448, 9984, 9472, 15104, 14592, 16128, 15616, 13056, 12544,
            14080, 13568, 344, 328, 376, 360, 280, 264, 312, 296,
            472, 456, 504, 488, 408, 392, 440, 424, 88, 72,
            120, 104, 24, 8, 56, 40, 216, 200, 248, 232,
            152, 136, 184, 168, 1376, 1312, 1504, 1440, 1120, 1056,
            1248, 1184, 1888, 1824, 2016, 1952, 1632, 1568, 1760, 1696,
            688, 656, 752, 720, 560, 528, 624, 592, 944, 912,
            1008, 976, 816, 784, 880, 848
        };
    }
}
