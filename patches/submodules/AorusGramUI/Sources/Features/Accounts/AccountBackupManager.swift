import Foundation
import CryptoKit
import Security

// MARK: - Account Backup Manager
//
// Backs up Telegram account auth data — `accounts-metadata` plus the compact
// network/session state from every `account-*` directory — into a single
// AES-256-GCM encrypted archive stored inside the app data directory.
// Message databases, media, caches and other re-downloadable data are excluded
// so the Keychain copy stays small and responsive like Swiftgram's backup.
// The archive's encryption key lives in the device Keychain and never leaves it.
//
// Restore is two-phase so it can never corrupt a live postbox:
//   1. prepareRestore()  — decrypts the archive into a staging directory and
//      raises the pending-restore flag (runs while the user is in the app).
//   2. applyPendingRestoreIfNeeded(rootPath:) — runs at the very start of the
//      next cold launch, BEFORE any postbox is opened, snapshots the current
//      account data for safety, then swaps the staged data into place.
public final class AccountBackupManager {
    public static let shared = AccountBackupManager()
    private init() {}

    // MARK: - Constants

    private let keychainService   = "aorusgram_account_backup"
    private let keychainKeyName   = "aes256_key_v1"
    private let rootPathKey       = "aorusgram_root_path"
    private let pendingRestoreKey = "aorusgram_pending_restore_v1"
    private let metaDateKey       = "aorusgram_backup_date_v1"
    private let metaCountKey      = "aorusgram_backup_accounts_v1"
    private let metaSizeKey       = "aorusgram_backup_size_v1"
    // Persisted at backup time so the login-by-backup picker can list the
    // backed-up accounts without decrypting the archive (and even after the
    // on-disk account folders are gone, e.g. after a sign-out).
    private let metaIdsKey        = "aorusgram_backup_account_ids_v1"

    // Durable Keychain storage. iOS Keychain items (unlike files and UserDefaults)
    // survive an app uninstall/reinstall for the same bundle id, so the whole
    // encrypted backup — the AES key, the archive bytes (chunked), and the
    // account metadata — lives here. This is what makes the backup outlast a
    // reinstall the way the Swiftgram Keychain backup does.
    private let keychainMetaName   = "archive_meta_v1"
    private let keychainChunkPrefix = "archive_chunk_"
    private let keychainChunkSize  = 200 * 1024   // 200 KB per Keychain item — safely small.
    private let maxCompactBackupFileSize: UInt64 = 2 * 1024 * 1024

    // Per-account display data, captured at backup time, so the Sessions list and
    // the login-by-backup picker can show a real username / Telegram ID / avatar
    // even after a reinstall (the values live in the durable Keychain metadata).
    public struct AccountRecordMeta: Codable, Equatable {
        public var id: String            // account-record id (matches the on-disk dir)
        public var userId: Int64         // Telegram user id (shown as "ID: …")
        public var username: String?     // @handle, if any
        public var name: String          // display title
        public var avatarBase64: String? // small PNG, if a cached photo existed
        public init(id: String, userId: Int64, username: String?, name: String, avatarBase64: String?) {
            self.id = id
            self.userId = userId
            self.username = username
            self.name = name
            self.avatarBase64 = avatarBase64
        }
    }

    // Input to performBackup — the live display data gathered from the account contexts.
    public struct AccountDisplayInfo {
        public let id: String
        public let userId: Int64
        public let username: String?
        public let name: String
        public let avatarPNG: Data?
        public init(id: String, userId: Int64, username: String?, name: String, avatarPNG: Data?) {
            self.id = id
            self.userId = userId
            self.username = username
            self.name = name
            self.avatarPNG = avatarPNG
        }
    }

    private struct KeychainBackupMeta: Codable {
        var chunkCount: Int
        var sizeBytes: Int64
        var date: Double         // timeIntervalSince1970
        var accountCount: Int
        var accountIds: [String]
        var accounts: [AccountRecordMeta]?
    }

    private static let archiveName  = "aorus-account-backup.enc"
    private static let stagingName  = ".aorus-restore-staging"
    private static let snapshotPrefix = ".aorus-prerestore-"
    private static let pendingFlagKey = "aorusgram_pending_restore_v1"

    // "AORSBK" + format version 1
    private let magic: [UInt8] = [0x41, 0x4F, 0x52, 0x53, 0x42, 0x4B, 0x01]

    private enum BackupError: Error { case truncated, corrupt }

    private func localized(_ ru: String, _ en: String) -> String {
        let code = (UserDefaults.standard.string(forKey: "aorusgram_lang")
            ?? Locale.preferredLanguages.first
            ?? Locale.current.identifier).lowercased()
        return code.hasPrefix("ru") ? ru : en
    }

    // MARK: - Root path

    // Mirrored to UserDefaults so the AorusGramUI copy of this manager reads the
    // same value. Set once by AorusGramBootstrap.setup(accountPath:).
    public var rootPath: String {
        get { UserDefaults.standard.string(forKey: rootPathKey) ?? "" }
        set { UserDefaults.standard.set(newValue, forKey: rootPathKey) }
    }

    private var archiveURL: URL {
        URL(fileURLWithPath: rootPath).appendingPathComponent(Self.archiveName)
    }
    private var stagingURL: URL {
        URL(fileURLWithPath: rootPath).appendingPathComponent(Self.stagingName)
    }

    // MARK: - Public model

    public struct BackupInfo {
        public let date: Date
        public let accountCount: Int
        public let sizeBytes: Int64
    }

    public enum BackupOutcome {
        case success(BackupInfo)
        case failure(String)
    }

    // MARK: - Status

    public func hasBackup() -> Bool {
        // The AES key is mandatory; the archive may live on disk (fast path) or,
        // after a reinstall wiped the sandbox, only in the Keychain.
        guard loadKey() != nil else { return false }
        if FileManager.default.fileExists(atPath: archiveURL.path) { return true }
        return keychainMeta() != nil
    }

    // Account record IDs captured in the last backup — the source of truth for the
    // login-by-backup picker. Reads the durable Keychain metadata first (survives a
    // reinstall), then the UserDefaults mirror, then whatever is on disk.
    public func backupAccountIds() -> [String] {
        if let meta = keychainMeta(), !meta.accountIds.isEmpty {
            return meta.accountIds
        }
        if let ids = UserDefaults.standard.stringArray(forKey: metaIdsKey), !ids.isEmpty {
            return ids
        }
        return localAccountIds()
    }

    public func isRestorePending() -> Bool {
        return UserDefaults.standard.bool(forKey: pendingRestoreKey)
    }

    public func backupInfo() -> BackupInfo? {
        guard loadKey() != nil else { return nil }
        // Durable Keychain metadata wins — it is present even right after a reinstall,
        // when the UserDefaults mirror has been wiped.
        if let meta = keychainMeta() {
            return BackupInfo(
                date: Date(timeIntervalSince1970: meta.date),
                accountCount: meta.accountCount,
                sizeBytes: meta.sizeBytes
            )
        }
        let ud = UserDefaults.standard
        guard FileManager.default.fileExists(atPath: archiveURL.path),
              let date = ud.object(forKey: metaDateKey) as? Date else { return nil }
        return BackupInfo(
            date: date,
            accountCount: ud.integer(forKey: metaCountKey),
            sizeBytes: Int64(ud.integer(forKey: metaSizeKey))
        )
    }

    // Per-account display metadata captured in the last backup (for the Sessions
    // list). Empty if the backup predates display capture.
    public func backupAccounts() -> [AccountRecordMeta] {
        return keychainMeta()?.accounts ?? []
    }

    // Account record IDs present on disk (the `account-<id>` directory suffix).
    public func localAccountIds() -> [String] {
        let fm = FileManager.default
        guard !rootPath.isEmpty,
              let entries = try? fm.contentsOfDirectory(atPath: rootPath) else { return [] }
        return entries
            .filter { $0.hasPrefix("account-") && !$0.contains(".aorus") }
            .map { String($0.dropFirst("account-".count)) }
            .sorted()
    }

    // MARK: - Backup

    public func performBackup(displayInfos: [AccountDisplayInfo] = []) -> BackupOutcome {
        guard !rootPath.isEmpty else { return .failure(localized("Путь к данным аккаунтов недоступен", "Account data path is unavailable")) }
        let fm = FileManager.default

        var files: [(abs: String, rel: String)] = []
        collectBackupFiles(into: &files)
        guard !files.isEmpty else { return .failure(localized("Нет данных аккаунтов для бэкапа", "No account data available for backup")) }

        let key = loadKey() ?? SymmetricKey(size: .bits256)

        let tmpURL = URL(fileURLWithPath: rootPath)
            .appendingPathComponent(Self.archiveName + ".tmp")
        try? fm.removeItem(at: tmpURL)
        guard fm.createFile(atPath: tmpURL.path, contents: nil),
              let handle = try? FileHandle(forWritingTo: tmpURL) else {
            return .failure(localized("Не удалось создать файл бэкапа", "Failed to create backup file"))
        }

        handle.write(Data(magic))
        for file in files {
            guard let content = fm.contents(atPath: file.abs),
                  let encPath = (try? AES.GCM.seal(Data(file.rel.utf8), using: key))?.combined,
                  let encBody = (try? AES.GCM.seal(content, using: key))?.combined else {
                handle.closeFile()
                try? fm.removeItem(at: tmpURL)
                return .failure(localized("Ошибка шифрования данных", "Data encryption failed"))
            }
            handle.write(uint32LE(UInt32(encPath.count)))
            handle.write(encPath)
            handle.write(uint64LE(UInt64(encBody.count)))
            handle.write(encBody)
        }
        handle.write(uint32LE(0)) // end marker
        handle.closeFile()

        try? fm.removeItem(at: archiveURL)
        do {
            try fm.moveItem(at: tmpURL, to: archiveURL)
        } catch {
            try? fm.removeItem(at: tmpURL)
            return .failure(localized("Не удалось сохранить бэкап", "Failed to save backup"))
        }

        guard saveKey(key) else {
            try? fm.removeItem(at: archiveURL)
            return .failure(localized("Не удалось сохранить ключ в Keychain", "Failed to save key in Keychain"))
        }

        let attrs = try? fm.attributesOfItem(atPath: archiveURL.path)
        let size = (attrs?[.size] as? NSNumber)?.int64Value ?? 0
        let ids = localAccountIds()
        let info = BackupInfo(date: Date(), accountCount: ids.count, sizeBytes: size)

        // Build durable per-account display metadata (only for accounts still on disk).
        let idSet = Set(ids)
        let accountMetas: [AccountRecordMeta] = displayInfos.filter { idSet.contains($0.id) }.map {
            AccountRecordMeta(id: $0.id, userId: $0.userId, username: $0.username, name: $0.name, avatarBase64: $0.avatarPNG?.base64EncodedString())
        }

        // Mirror the encrypted archive + metadata into the Keychain so the whole
        // backup survives an app reinstall (files and UserDefaults do not).
        guard storeArchiveInKeychain(accountIds: ids, date: info.date, accountCount: info.accountCount, accounts: accountMetas) else {
            return .failure(localized("Не удалось сохранить бэкап в Keychain", "Failed to save backup to Keychain"))
        }

        let ud = UserDefaults.standard
        ud.set(info.date, forKey: metaDateKey)
        ud.set(info.accountCount, forKey: metaCountKey)
        ud.set(Int(info.sizeBytes), forKey: metaSizeKey)
        ud.set(ids, forKey: metaIdsKey)
        return .success(info)
    }

    private func collectBackupFiles(into files: inout [(abs: String, rel: String)]) {
        let fm = FileManager.default
        guard let top = try? fm.contentsOfDirectory(atPath: rootPath) else { return }
        for name in top {
            let isMeta = (name == "accounts-metadata")
            let isAccount = name.hasPrefix("account-") && !name.contains(".aorus")
            guard isMeta || isAccount else { continue }
            let dirAbs = (rootPath as NSString).appendingPathComponent(name)
            var isDir: ObjCBool = false
            guard fm.fileExists(atPath: dirAbs, isDirectory: &isDir) else { continue }
            if !isDir.boolValue {
                if shouldIncludeBackupFile(abs: dirAbs, rel: name) {
                    files.append((abs: dirAbs, rel: name))
                }
                continue
            }
            enumerate(dirAbs: dirAbs, relBase: name, into: &files)
        }
    }

    private func enumerate(dirAbs: String, relBase: String,
                           into files: inout [(abs: String, rel: String)]) {
        let fm = FileManager.default
        guard let en = fm.enumerator(atPath: dirAbs) else { return }
        while let sub = en.nextObject() as? String {
            let abs = (dirAbs as NSString).appendingPathComponent(sub)
            var isDir: ObjCBool = false
            guard fm.fileExists(atPath: abs, isDirectory: &isDir) else { continue }
            let rel = relBase + "/" + sub
            if isDir.boolValue {
                if shouldSkipBackupDirectory(rel: rel) {
                    en.skipDescendants()
                }
                continue
            }
            if shouldIncludeBackupFile(abs: abs, rel: rel) {
                files.append((abs: abs, rel: rel))
            }
        }
    }

    private func shouldSkipBackupDirectory(rel: String) -> Bool {
        let comps = rel.split(separator: "/").map { String($0).lowercased() }
        guard comps.count >= 2 else { return false }
        let heavyRoots: Set<String> = [
            "postbox", "mediabox", "media", "cache", "caches", "temp", "tmp",
            "resources", "resource-cache", "stickers", "video", "audio"
        ]
        return comps.contains(where: { heavyRoots.contains($0) || $0.contains("cache") || $0.contains("temp") || $0.contains("tmp") })
    }

    private func shouldIncludeBackupFile(abs: String, rel: String) -> Bool {
        let fm = FileManager.default
        let attrs = try? fm.attributesOfItem(atPath: abs)
        let size = (attrs?[.size] as? NSNumber)?.uint64Value ?? 0
        let comps = rel.split(separator: "/").map(String.init)
        guard let first = comps.first else { return false }

        if first == "accounts-metadata" {
            return size <= maxCompactBackupFileSize
        }
        guard first.hasPrefix("account-"), comps.count >= 2 else { return false }
        guard size <= maxCompactBackupFileSize else { return false }

        if shouldSkipBackupDirectory(rel: rel) {
            return false
        }

        let second = comps[1].lowercased()
        if second == "network" || second.contains("network") || second.contains("auth")
            || second.contains("session") || second.contains("state")
            || second.contains("metadata") || second.contains("config")
            || second.contains("preference") || second.contains("mtproto")
            || second.contains("datacenter") {
            return true
        }

        let lowerRel = rel.lowercased()
        if lowerRel.hasSuffix(".sqlite") || lowerRel.hasSuffix(".sqlite-wal")
            || lowerRel.hasSuffix(".sqlite-shm") || lowerRel.hasSuffix(".db")
            || lowerRel.hasSuffix(".ldb") || lowerRel.hasSuffix(".log") {
            return false
        }

        // Keep small root-level account metadata files, but never descend into broad
        // stores such as postbox/media. Telegram recreates message databases after login.
        return comps.count == 2
    }

    // MARK: - Restore phase 1: prepare

    public func prepareRestore() -> BackupOutcome {
        guard !rootPath.isEmpty else { return .failure(localized("Путь к данным аккаунтов недоступен", "Account data path is unavailable")) }
        // Rebuild the archive file from the durable Keychain copy if the sandbox
        // was wiped (e.g. after a reinstall) so the streaming decode below works.
        materializeArchiveFromKeychainIfNeeded()
        guard hasBackup(), let key = loadKey() else { return .failure(localized("Бэкап не найден", "Backup not found")) }
        let fm = FileManager.default

        try? fm.removeItem(at: stagingURL)
        do {
            try fm.createDirectory(at: stagingURL, withIntermediateDirectories: true)
        } catch {
            return .failure(localized("Не удалось создать папку восстановления", "Failed to create restore folder"))
        }

        guard let handle = try? FileHandle(forReadingFrom: archiveURL) else {
            try? fm.removeItem(at: stagingURL)
            return .failure(localized("Не удалось открыть бэкап", "Failed to open backup"))
        }

        let head = handle.readData(ofLength: magic.count)
        guard Array(head) == magic else {
            handle.closeFile()
            try? fm.removeItem(at: stagingURL)
            return .failure(localized("Файл бэкапа повреждён", "Backup file is corrupted"))
        }

        do {
            while true {
                let pathLenData = handle.readData(ofLength: 4)
                guard pathLenData.count == 4 else { break }
                let encPathLen = readUInt32LE(pathLenData)
                if encPathLen == 0 { break } // end marker

                let encPath = handle.readData(ofLength: Int(encPathLen))
                guard encPath.count == Int(encPathLen) else { throw BackupError.truncated }
                let bodyLenData = handle.readData(ofLength: 8)
                guard bodyLenData.count == 8 else { throw BackupError.truncated }
                let encBodyLen = readUInt64LE(bodyLenData)
                let encBody = handle.readData(ofLength: Int(encBodyLen))
                guard encBody.count == Int(encBodyLen) else { throw BackupError.truncated }

                let relData = try AES.GCM.open(AES.GCM.SealedBox(combined: encPath), using: key)
                let body = try AES.GCM.open(AES.GCM.SealedBox(combined: encBody), using: key)
                guard let rel = String(data: relData, encoding: .utf8),
                      isSafeRelativePath(rel) else { throw BackupError.corrupt }

                let dest = stagingURL.appendingPathComponent(rel)
                try fm.createDirectory(at: dest.deletingLastPathComponent(),
                                       withIntermediateDirectories: true)
                try body.write(to: dest)
            }
        } catch {
            handle.closeFile()
            try? fm.removeItem(at: stagingURL)
            return .failure(localized("Ошибка расшифровки бэкапа", "Backup decryption failed"))
        }
        handle.closeFile()

        UserDefaults.standard.set(true, forKey: pendingRestoreKey)
        let info = backupInfo()
            ?? BackupInfo(date: Date(), accountCount: localAccountIds().count, sizeBytes: 0)
        return .success(info)
    }

    public func cancelPendingRestore() {
        UserDefaults.standard.set(false, forKey: pendingRestoreKey)
        try? FileManager.default.removeItem(at: stagingURL)
    }

    // Reject path traversal — only `accounts-metadata` and `account-*` roots allowed.
    private func isSafeRelativePath(_ path: String) -> Bool {
        if path.isEmpty || path.hasPrefix("/") { return false }
        let comps = path.split(separator: "/").map(String.init)
        if comps.contains("..") || comps.contains(".") { return false }
        guard let first = comps.first else { return false }
        return first == "accounts-metadata" || first.hasPrefix("account-")
    }

    // MARK: - Restore phase 2: apply at launch

    // Called from AppDelegate immediately after `rootPath` is known and BEFORE
    // any postbox / account context is created.
    public static func applyPendingRestoreIfNeeded(rootPath: String) {
        let ud = UserDefaults.standard
        guard ud.bool(forKey: pendingFlagKey), !rootPath.isEmpty else { return }
        // Clear the flag up-front — a restore must never loop and brick every launch.
        ud.set(false, forKey: pendingFlagKey)

        let fm = FileManager.default
        let staging = URL(fileURLWithPath: rootPath).appendingPathComponent(stagingName)
        guard fm.fileExists(atPath: staging.path),
              let staged = try? fm.contentsOfDirectory(atPath: staging.path),
              !staged.isEmpty else {
            try? fm.removeItem(at: staging)
            return
        }

        // Drop snapshots from earlier restores — keep only the one we make now,
        // otherwise old account data (incl. media caches) accumulates forever.
        if let rootEntries = try? fm.contentsOfDirectory(atPath: rootPath) {
            for name in rootEntries where name.hasPrefix(snapshotPrefix) {
                try? fm.removeItem(at: URL(fileURLWithPath: rootPath).appendingPathComponent(name))
            }
        }

        // Snapshot whatever currently occupies those top-level slots, so a failed
        // or unwanted restore stays recoverable.
        let snapshot = URL(fileURLWithPath: rootPath)
            .appendingPathComponent(snapshotPrefix + String(Int(Date().timeIntervalSince1970)))
        try? fm.createDirectory(at: snapshot, withIntermediateDirectories: true)
        for name in staged {
            let current = URL(fileURLWithPath: rootPath).appendingPathComponent(name)
            if fm.fileExists(atPath: current.path) {
                try? fm.moveItem(at: current, to: snapshot.appendingPathComponent(name))
            }
        }

        var allMoved = true
        for name in staged {
            let src = staging.appendingPathComponent(name)
            let dst = URL(fileURLWithPath: rootPath).appendingPathComponent(name)
            do {
                try fm.moveItem(at: src, to: dst)
            } catch {
                allMoved = false
            }
        }
        try? fm.removeItem(at: staging)

        if !allMoved {
            // Roll back from the snapshot.
            for name in staged {
                let dst = URL(fileURLWithPath: rootPath).appendingPathComponent(name)
                let snap = snapshot.appendingPathComponent(name)
                if fm.fileExists(atPath: snap.path) {
                    try? fm.removeItem(at: dst)
                    try? fm.moveItem(at: snap, to: dst)
                }
            }
        }
    }

    // MARK: - Delete

    public func deleteBackup() {
        try? FileManager.default.removeItem(at: archiveURL)
        deleteKey()
        deleteKeychainArchive()
        let ud = UserDefaults.standard
        ud.removeObject(forKey: metaDateKey)
        ud.removeObject(forKey: metaCountKey)
        ud.removeObject(forKey: metaSizeKey)
        ud.removeObject(forKey: metaIdsKey)
    }

    // MARK: - Durable Keychain archive (survives reinstall)

    // Generic Keychain data slot under our service, keyed by an account name.
    private func keychainSet(_ account: String, _ data: Data) -> Bool {
        let base: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: account,
        ]
        SecItemDelete(base as CFDictionary)
        var add = base
        add[kSecValueData as String] = data
        add[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlock
        return SecItemAdd(add as CFDictionary, nil) == errSecSuccess
    }

    private func keychainGet(_ account: String) -> Data? {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: account,
            kSecReturnData as String:  true,
            kSecMatchLimit as String:  kSecMatchLimitOne,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data else { return nil }
        return data
    }

    private func keychainDelete(_ account: String) {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: account,
        ]
        SecItemDelete(query as CFDictionary)
    }

    private func keychainMeta() -> KeychainBackupMeta? {
        guard let data = keychainGet(keychainMetaName) else { return nil }
        return try? JSONDecoder().decode(KeychainBackupMeta.self, from: data)
    }

    // Stream the on-disk archive into chunked Keychain items + a metadata slot.
    private func storeArchiveInKeychain(accountIds: [String], date: Date, accountCount: Int, accounts: [AccountRecordMeta]) -> Bool {
        deleteKeychainArchive()
        guard let handle = try? FileHandle(forReadingFrom: archiveURL) else { return false }
        defer { try? handle.close() }
        var index = 0
        var total: Int64 = 0
        while true {
            let chunk = handle.readData(ofLength: keychainChunkSize)
            if chunk.isEmpty { break }
            total += Int64(chunk.count)
            guard keychainSet(keychainChunkPrefix + String(index), chunk) else {
                deleteKeychainArchive()
                return false
            }
            index += 1
        }
        let meta = KeychainBackupMeta(chunkCount: index, sizeBytes: total, date: date.timeIntervalSince1970, accountCount: accountCount, accountIds: accountIds, accounts: accounts.isEmpty ? nil : accounts)
        guard let metaData = try? JSONEncoder().encode(meta), keychainSet(keychainMetaName, metaData) else {
            deleteKeychainArchive()
            return false
        }
        return true
    }

    // Rebuild the archive file from Keychain chunks when the sandbox copy is gone.
    private func materializeArchiveFromKeychainIfNeeded() {
        let fm = FileManager.default
        guard !fm.fileExists(atPath: archiveURL.path) else { return }
        guard let meta = keychainMeta(), meta.chunkCount > 0 else { return }
        guard fm.createFile(atPath: archiveURL.path, contents: nil),
              let handle = try? FileHandle(forWritingTo: archiveURL) else { return }
        var ok = true
        for i in 0 ..< meta.chunkCount {
            guard let chunk = keychainGet(keychainChunkPrefix + String(i)) else { ok = false; break }
            handle.write(chunk)
        }
        try? handle.close()
        if !ok {
            try? fm.removeItem(at: archiveURL)
        }
    }

    private func deleteKeychainArchive() {
        let count = keychainMeta()?.chunkCount ?? 0
        // Remove the recorded chunks, plus a generous overscan in case a previous
        // (larger) backup left extra items behind.
        for i in 0 ..< max(count, 0) {
            keychainDelete(keychainChunkPrefix + String(i))
        }
        var extra = count
        while keychainGet(keychainChunkPrefix + String(extra)) != nil {
            keychainDelete(keychainChunkPrefix + String(extra))
            extra += 1
        }
        keychainDelete(keychainMetaName)
    }

    // MARK: - Keychain (AES key)

    private func saveKey(_ key: SymmetricKey) -> Bool {
        let data = key.withUnsafeBytes { Data($0) }
        let base: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: keychainKeyName,
        ]
        SecItemDelete(base as CFDictionary)
        var add = base
        add[kSecValueData as String] = data
        add[kSecAttrAccessible as String] = kSecAttrAccessibleAfterFirstUnlock
        return SecItemAdd(add as CFDictionary, nil) == errSecSuccess
    }

    private func loadKey() -> SymmetricKey? {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: keychainKeyName,
            kSecReturnData as String:  true,
            kSecMatchLimit as String:  kSecMatchLimitOne,
        ]
        var result: AnyObject?
        guard SecItemCopyMatching(query as CFDictionary, &result) == errSecSuccess,
              let data = result as? Data, data.count == 32 else { return nil }
        return SymmetricKey(data: data)
    }

    private func deleteKey() {
        let query: [String: Any] = [
            kSecClass as String:       kSecClassGenericPassword,
            kSecAttrService as String: keychainService,
            kSecAttrAccount as String: keychainKeyName,
        ]
        SecItemDelete(query as CFDictionary)
    }

    // MARK: - Byte helpers (little-endian, version-safe)

    private func uint32LE(_ v: UInt32) -> Data {
        return Data([
            UInt8(v & 0xFF),
            UInt8((v >> 8) & 0xFF),
            UInt8((v >> 16) & 0xFF),
            UInt8((v >> 24) & 0xFF),
        ])
    }

    private func uint64LE(_ v: UInt64) -> Data {
        var bytes = [UInt8]()
        for i in 0..<8 { bytes.append(UInt8((v >> (UInt64(i) * 8)) & 0xFF)) }
        return Data(bytes)
    }

    private func readUInt32LE(_ d: Data) -> UInt32 {
        guard d.count >= 4 else { return 0 }
        let b = [UInt8](d)
        return UInt32(b[0]) | (UInt32(b[1]) << 8) | (UInt32(b[2]) << 16) | (UInt32(b[3]) << 24)
    }

    private func readUInt64LE(_ d: Data) -> UInt64 {
        guard d.count >= 8 else { return 0 }
        let b = [UInt8](d)
        var v: UInt64 = 0
        for i in 0..<8 { v |= UInt64(b[i]) << (UInt64(i) * 8) }
        return v
    }
}
