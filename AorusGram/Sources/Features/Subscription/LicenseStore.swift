import Foundation
import Security

// Persistent license cache.
//
// The authoritative snapshot lives in the Keychain. A light, non-sensitive mirror
// is kept in UserDefaults only for fast UI reads. The SERVER is the source of
// truth; this cache exists for UX and a bounded offline grace based on
// server_now / active_until — never raw local time alone.
final class LicenseStore {
    static let shared = LicenseStore()
    private init() {}

    private let kcService = "com.aorusgram.license"
    private let kcAccount = "cache_v1"
    private let udTelegramKey = "aorusgram_lic_tg_uid"   // non-sensitive UI/id mirror

    struct Snapshot: Codable {
        var statusRaw: String
        var plan: String?
        var activeUntil: Int64?
        var serverNow: Int64?
        var daysLeft: Int?
        var lastCheckWall: Double      // Date().timeIntervalSince1970 at the check
        var telegramUserId: Int64?
        // Tamper / theft protection (optional → old blobs decode and are re-signed).
        var deviceHash: String?        // device this cache belongs to
        var sig: String?               // HMAC over the fields above (key = license key)
    }

    private(set) var snapshot: Snapshot?

    func load() {
        guard let s = readKeychain() else { snapshot = nil; return }
        // Integrity + device binding: a tampered blob, or one lifted from another
        // device, is ignored. This only forces a fresh online check — it can never
        // lock out a legitimate user (the server verdict restores access).
        if s.sig == sign(s), s.deviceHash == DeviceFingerprint.deviceHash() {
            snapshot = s
        } else {
            snapshot = nil
        }
    }

    func save(response: LicenseResponse, telegramUserId: Int64?) {
        let snap = Snapshot(
            statusRaw: response.status.rawValue,
            plan: response.plan,
            activeUntil: response.activeUntil,
            serverNow: response.serverNow,
            daysLeft: response.daysLeft,
            lastCheckWall: Date().timeIntervalSince1970,
            telegramUserId: telegramUserId ?? snapshot?.telegramUserId
        )
        snapshot = snap
        writeKeychain(snap)

        // UI mirror — non-sensitive only.
        let ud = UserDefaults.standard
        ud.set(snap.statusRaw, forKey: "aorusgram_lic_status")
        ud.set(snap.daysLeft ?? -1, forKey: "aorusgram_lic_days_left")
    }

    func clear() {
        snapshot = nil
        let base: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: kcService,
            kSecAttrAccount as String: kcAccount,
        ]
        SecItemDelete(base as CFDictionary)
        let ud = UserDefaults.standard
        ud.removeObject(forKey: "aorusgram_lic_status")
        ud.removeObject(forKey: "aorusgram_lic_days_left")
    }

    // Prefer the id captured with the last license snapshot; fall back to the
    // standalone mirror set right after Telegram login (before any license check).
    var telegramUserId: Int64? {
        if let id = snapshot?.telegramUserId, id != 0 { return id }
        let v = UserDefaults.standard.object(forKey: udTelegramKey) as? NSNumber
        return v.map { $0.int64Value }
    }
    var daysLeft: Int? { snapshot?.daysLeft }

    // Persist the Telegram account id independently of the license snapshot, so the
    // first /license/check, /bootstrap and /activate after login carry the real id.
    func setTelegramUserId(_ id: Int64) {
        guard id != 0 else { return }
        UserDefaults.standard.set(NSNumber(value: id), forKey: udTelegramKey)
        if var snap = snapshot {
            snap.telegramUserId = id
            snapshot = snap
            writeKeychain(snap)
        }
    }

    // Estimated current server time = server anchor + elapsed wall clock since the
    // last check. If the clock moved backwards we do NOT extend validity.
    private func estimatedServerNow(_ snap: Snapshot) -> Int64? {
        guard let serverNow = snap.serverNow else { return nil }
        let elapsed = Date().timeIntervalSince1970 - snap.lastCheckWall
        if elapsed < 0 { return serverNow }
        return serverNow + Int64(elapsed)
    }

    // Offline effective status: an active cache is trusted only while active_until
    // has not passed (server-anchored estimate); otherwise it is treated expired.
    func effectiveOfflineStatus() -> LicenseStatus {
        guard let snap = snapshot else { return .notStarted }
        let cached = LicenseStatus.parse(snap.statusRaw)
        switch cached {
        case .trialActive, .paidActive:
            guard let until = snap.activeUntil, let now = estimatedServerNow(snap) else {
                // No timing info — allow briefly; the next online check corrects it.
                return cached
            }
            return now < until ? cached : .expired
        default:
            return cached
        }
    }

    func needsRecheck(interval: TimeInterval) -> Bool {
        guard let last = snapshot?.lastCheckWall else { return true }
        return (Date().timeIntervalSince1970 - last) > interval
    }

    // MARK: - Integrity

    // Canonical, deterministic representation of the signed fields (excludes `sig`).
    private func canonicalString(_ s: Snapshot) -> String {
        var parts: [String] = []
        parts.append(s.statusRaw)
        parts.append(s.plan ?? "")
        parts.append(s.activeUntil.map { String($0) } ?? "")
        parts.append(s.serverNow.map { String($0) } ?? "")
        parts.append(s.daysLeft.map { String($0) } ?? "")
        parts.append(String(format: "%.0f", s.lastCheckWall))
        parts.append(s.telegramUserId.map { String($0) } ?? "")
        parts.append(s.deviceHash ?? "")
        return parts.joined(separator: "|")
    }

    // HMAC-SHA256 with the embedded license key. Never logged.
    private func sign(_ s: Snapshot) -> String {
        let key = LicenseKeyProvider.licenseHmacKeyBytes()
        return LicenseCrypto.hmacSHA256Hex(message: Data(canonicalString(s).utf8), keyBytes: key)
    }

    // MARK: - Keychain (JSON blob)

    private func writeKeychain(_ snap: Snapshot) {
        var s = snap
        s.deviceHash = DeviceFingerprint.deviceHash()
        s.sig = nil
        s.sig = sign(s)            // sign over the canonical fields (incl. deviceHash)
        guard let data = try? JSONEncoder().encode(s) else { return }
        let base: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: kcService,
            kSecAttrAccount as String: kcAccount,
        ]
        SecItemDelete(base as CFDictionary)
        var add = base
        add[kSecValueData as String] = data
        add[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlock
        _ = SecItemAdd(add as CFDictionary, nil)
    }

    private func readKeychain() -> Snapshot? {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: kcService,
            kSecAttrAccount as String: kcAccount,
            kSecReturnData as String:  true,
            kSecMatchLimit as String:  kSecMatchLimitOne,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data else { return nil }
        return try? JSONDecoder().decode(Snapshot.self, from: data)
    }
}
