using System.Buffers.Binary;

namespace EosJoinProbe;

/// <summary>
/// LabFusion 0.1.0+ EOS P2P framing (Checkerb0ard BONELAB-Fusion).
/// Every packet is prefixed with a kind byte: KindSingle (0) or KindFragment (1).
/// Pre-0.1.0 used a 0xF2A9 magic fragment header and raw NetMessages with no kind prefix.
/// </summary>
internal static class FusionP2PWire
{
    public const byte KindSingle = 0;
    public const byte KindFragment = 1;
    public const int KindPrefixSize = 1;
    public const int FragmentHeaderSize = KindPrefixSize + 6;
    public const ushort LegacyFragmentMagic = 0xF2A9; // 62121
    public const int MaxPacketSize = 1170;

    public static byte[] WrapSingle(byte[] message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var packet = new byte[message.Length + KindPrefixSize];
        packet[0] = KindSingle;
        if (message.Length > 0)
            Buffer.BlockCopy(message, 0, packet, KindPrefixSize, message.Length);
        return packet;
    }

    /// <summary>
    /// Unwrap a received P2P payload into a Fusion NetMessage.
    /// Returns false for fragments (legacy or 0.1.x) that need reassembly — caller should skip.
    /// Accepts legacy unprefixed packets for mixed-version lobbies.
    /// </summary>
    public static bool TryUnwrap(ReadOnlySpan<byte> raw, out ReadOnlySpan<byte> message)
    {
        message = default;
        if (raw.Length < 1)
            return false;

        // Pre-0.1.0 fragment magic
        if (raw.Length >= 8 && BinaryPrimitives.ReadUInt16LittleEndian(raw) == LegacyFragmentMagic)
            return false;

        byte kind = raw[0];
        if (kind == KindFragment)
        {
            // Ambiguous with legacy ConnectionRequest (tag=1). Prefer fragment when header is sane.
            if (LooksLikeFragment(raw))
                return false;
            message = raw; // legacy tag=1 NetMessage
            return true;
        }

        if (kind == KindSingle)
        {
            // 0.1.x path: strip kind. Legacy poke also starts with tag=0 — stripping one byte
            // is harmless for our host (poke is ignored either way).
            message = raw.Length > KindPrefixSize ? raw[KindPrefixSize..] : ReadOnlySpan<byte>.Empty;
            return true;
        }

        // Legacy raw NetMessage (tag >= 2): ConnectionResponse, SceneLoad, Voice, …
        message = raw;
        return true;
    }

    public static bool LooksLikeFragment(ReadOnlySpan<byte> raw)
    {
        if (raw.Length < FragmentHeaderSize || raw[0] != KindFragment)
            return false;
        ushort index = BinaryPrimitives.ReadUInt16LittleEndian(raw.Slice(3, 2));
        ushort total = BinaryPrimitives.ReadUInt16LittleEndian(raw.Slice(5, 2));
        return total is >= 1 and <= 1000 && index < total;
    }
}
