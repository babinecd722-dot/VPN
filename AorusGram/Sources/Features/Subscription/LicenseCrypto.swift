import Foundation
import CryptoKit
import Security

// Low-level crypto helpers for the license layer.
//
// SECURITY: callers must never log the HMAC key, the reassembled key bytes, the
// request body when it carries an activation code, or the resulting signature.
enum LicenseCrypto {
    // Lowercase hex, no separators.
    static func hexEncode(_ data: Data) -> String {
        let alphabet = Array("0123456789abcdef".utf8)
        var out = [UInt8]()
        out.reserveCapacity(data.count * 2)
        for b in data {
            out.append(alphabet[Int(b >> 4)])
            out.append(alphabet[Int(b & 0x0F)])
        }
        return String(decoding: out, as: UTF8.self)
    }

    // Tolerant hex decode (ignores a trailing odd nibble). Used for the HMAC key:
    // decode hex -> raw bytes (NEVER treat the hex string as a UTF-8 key).
    static func hexDecode(_ hex: String) -> Data {
        var data = Data()
        data.reserveCapacity(hex.count / 2)
        var iterator = hex.unicodeScalars.makeIterator()
        func nibble(_ scalar: Unicode.Scalar?) -> UInt8? {
            guard let scalar = scalar else { return nil }
            switch scalar {
            case "0"..."9": return UInt8(scalar.value - 48)
            case "a"..."f": return UInt8(scalar.value - 87)
            case "A"..."F": return UInt8(scalar.value - 55)
            default: return nil
            }
        }
        while true {
            guard let hi = nibble(iterator.next()) else { break }
            guard let lo = nibble(iterator.next()) else { break }
            data.append((hi << 4) | lo)
        }
        return data
    }

    static func sha256Hex(_ data: Data) -> String {
        return hexEncode(Data(SHA256.hash(data: data)))
    }

    // HMAC-SHA256 using RAW key bytes (not a UTF-8 string).
    static func hmacSHA256Hex(message: Data, keyBytes: Data) -> String {
        let key = SymmetricKey(data: keyBytes)
        let mac = HMAC<SHA256>.authenticationCode(for: message, using: key)
        return hexEncode(Data(mac))
    }

    // Cryptographically random hex of `byteCount` bytes (-> 2*byteCount hex chars).
    static func randomHex(byteCount: Int) -> String {
        var bytes = [UInt8](repeating: 0, count: byteCount)
        if SecRandomCopyBytes(kSecRandomDefault, byteCount, &bytes) != errSecSuccess {
            for i in 0..<byteCount { bytes[i] = UInt8.random(in: 0...255) }
        }
        return hexEncode(Data(bytes))
    }
}
