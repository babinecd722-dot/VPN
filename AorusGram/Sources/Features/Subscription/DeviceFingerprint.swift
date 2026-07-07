import Foundation
import UIKit
import Security

// Stable per-device fingerprint sent in X-Aorus-Device.
//
//   device_hash = SHA256( installId | bundleId | appSalt )  (64 hex lower)
//
// DRM-style stability: the identity is anchored ONLY on a Keychain-stored UUID.
// iOS keeps Keychain items across app deletion/reinstall, so a reinstall keeps the
// same device_hash → the free trial cannot be farmed by reinstalling.
//
// We deliberately do NOT mix in identifierForVendor: idfv RESETS once all of the
// vendor's apps are removed, which is exactly the reinstall case we must survive.
//
// The UUID is stored REDUNDANTLY in two Keychain items that heal each other, and
// with kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly so it is bound to this
// physical device and is not carried to a new device by an encrypted backup
// restore (a restored device is correctly treated as new).
enum DeviceFingerprint {
    // Three redundant slots under different service/account names. They heal each
    // other, so the identity survives unless ALL three are wiped at once — much
    // harder to "reset the trial" by poking the keychain.
    private static let slots: [(service: String, account: String)] = [
        ("com.aorusgram.license", "install_id"),
        ("com.aorusgram.license", "install_id_b"),
        ("com.aorusgram.device", "did"),
    ]

    static func deviceHash() -> String {
        let installId = keychainInstallId()
        let bundleId = Bundle.main.bundleIdentifier ?? "com.aorusgram"

        var input = Data()
        input.append(Data(installId.utf8))
        input.append(0x1f)
        input.append(Data(bundleId.utf8))
        input.append(0x1f)
        input.append(appSalt())
        return LicenseCrypto.sha256Hex(input)
    }

    // Get-or-create the persistent install id, self-healing across all slots.
    static func keychainInstallId() -> String {
        let values = slots.map { readInstallId(service: $0.service, account: $0.account) }

        // Use the first surviving value (majority would be even stronger, but any
        // single survivor is enough to keep the same identity).
        if let value = values.compactMap({ $0 }).first {
            for (i, slot) in slots.enumerated() where values[i] != value {
                writeInstallId(value, service: slot.service, account: slot.account)
            }
            return value
        }

        let generated = UUID().uuidString
        for slot in slots { writeInstallId(generated, service: slot.service, account: slot.account) }
        return generated
    }

    // Obfuscated app salt (XOR rotating pad), reassembled at runtime. Embedded in
    // the binary; obfuscation only raises the reverse-engineering bar.
    private static func appSalt() -> Data {
        let pad: [UInt8] = [0x3C, 0x9E, 0x71, 0xD5, 0x2A, 0x6B, 0xF7, 0x18]
        let obfuscated: [UInt8] = [
            0x7F, 0xE2, 0x05, 0xB3, 0x44, 0x1C, 0x8A, 0xD0,
            0x21, 0x55, 0x99, 0x0E, 0x63, 0xC7, 0x3B, 0xAA
        ]
        var out = [UInt8]()
        out.reserveCapacity(obfuscated.count)
        for (i, b) in obfuscated.enumerated() {
            out.append(b ^ pad[i % pad.count])
        }
        return Data(out)
    }

    // MARK: - Keychain

    private static func readInstallId(service: String, account: String) -> String? {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
            kSecReturnData as String:  true,
            kSecMatchLimit as String:  kSecMatchLimitOne,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data,
              let value = String(data: data, encoding: .utf8) else { return nil }
        return value
    }

    private static func writeInstallId(_ value: String, service: String, account: String) {
        let base: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account,
        ]
        SecItemDelete(base as CFDictionary)
        var add = base
        add[kSecValueData as String] = Data(value.utf8)
        add[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly
        _ = SecItemAdd(add as CFDictionary, nil)
    }
}
