import Foundation
import CryptoKit
import TelegramCore

// AorusGram: SOCKS5 call proxy for voice/video calls.
//
// AorusProxyManager fetches a `callProxy` (SOCKS5) from /getProxy and seals it with
// AES-GCM into a dedicated, innocuously-named UserDefaults suite (the same scheme as
// the MTProxy bridge). This reads + decrypts it and returns a ProxyServerSettings the
// calls layer can use, so the WebRTC media leg of a call is routed through SOCKS5
// (Telegram only supports SOCKS5 — not MTProxy — for the call media leg). This is
// used to keep calls working where direct voice traffic is blocked.
//
// Returns nil when no valid call proxy lease is configured → calls fall back to the
// user's own proxy settings (if any). The suite name, blob key and pepper bytes MUST
// stay byte-identical to ProxyVault in AorusProxyManager.swift.
public func aorusCallProxyServerSettings() -> ProxyServerSettings? {
    guard let store = UserDefaults(suiteName: "ng.session.store"),
          let blob = store.string(forKey: "c9a3f1e7-2b48-4d6a-9e15-7c0d8b3f6a21"),
          let box = Data(base64Encoded: blob) else {
        return nil
    }

    let s0: [UInt8] = [0x8c, 0x21, 0x47, 0xf9, 0x03, 0xbe, 0x5a, 0xd7, 0x6e, 0x10, 0xc4, 0x9b]
    let s1: [UInt8] = [0x2f, 0xa8, 0x73, 0x14, 0xe6, 0x5d, 0x0a, 0xcf, 0x91, 0x46, 0xb2, 0x38]
    let s2: [UInt8] = [0x7d, 0xe1, 0x4c, 0x60, 0xaa, 0x05, 0xf3, 0x29, 0x8b, 0xd4, 0x17, 0x52]
    let key = SymmetricKey(data: Data(SHA256.hash(data: Data(s0 + s1 + s2))))

    guard let sealed = try? AES.GCM.SealedBox(combined: box),
          let plain = try? AES.GCM.open(sealed, using: key),
          let payload = String(data: plain, encoding: .utf8) else {
        return nil
    }

    // payload = "server\nport\nusername\npassword\nudp\nexpiresAt"
    let parts = payload.components(separatedBy: "\n")
    guard parts.count >= 6,
          !parts[0].isEmpty,
          let port = Int32(parts[1]), port > 0,
          let expiresAt = TimeInterval(parts[5]),
          Date().timeIntervalSince1970 < expiresAt else {
        return nil
    }
    let username = parts[2].isEmpty ? nil : parts[2]
    let password = parts[3].isEmpty ? nil : parts[3]
    return ProxyServerSettings(host: parts[0], port: port, connection: .socks5(username: username, password: password))
}
