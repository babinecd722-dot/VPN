import Foundation

// HMAC key provider.
//
// The license key is stored split into chunks and reassembled + hex-decoded to RAW
// bytes only at signing time. The hex string is NEVER used directly as the key
// (HMAC uses the decoded bytes). Splitting only lightly raises the reverse-
// engineering bar — the key is still embedded in the binary.
//
// SECURITY: never log the key, the reassembled bytes, or any signature.
enum LicenseKeyProvider {
    static let keyVersion = "1"   // X-Aorus-Kv

    private static let keyChunks: [String] = [
        "0d0199727dbf6872",
        "c04f14712fc590d1",
        "d2e67bb7c25e37f0",
        "a52e287ca7979b78",
        "b500f59879b5a448",
        "03553fc94fabd4e5",
        "df4ba3d46e75f54c",
        "109d3548c61f40a1",
    ]

    static var isProvisioned: Bool { !keyChunks.isEmpty }

    // Reassemble → hex-decode → raw key bytes. The caller must not retain or log it.
    static func licenseHmacKeyBytes() -> Data {
        return LicenseCrypto.hexDecode(keyChunks.joined())
    }
}
