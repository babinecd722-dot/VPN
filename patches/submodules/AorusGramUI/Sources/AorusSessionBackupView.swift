import Foundation
import UIKit
import SwiftUI
import AccountContext
import TelegramCore
import Postbox
import SwiftSignalKit
import TelegramPresentationData

// Pixel-faithful port of Swiftgram's SGProUI/SessionBackupController SwiftUI screen
// (SessionBackupManagerView + SessionBackupRow), wired to AorusGram's own
// AccountBackupManager. SwiftUI's List gives the exact inset-grouped look — carded
// action rows, a carded notice, a "Sessions" section and per-session rows with a
// "Logged in / Logged out" badge — matching the reference one-to-one. The theme is
// applied by the hosting controller (dark appearance + accent tint).

@available(iOS 13.0, *)
private struct AorusSessionL10n {
    let isRu: Bool
    private func t(_ ru: String, _ en: String) -> String { isRu ? ru : en }

    var backup: String { t("Бэкап в Keychain", "Back Up to Keychain") }
    var restore: String { t("Восстановить из Keychain", "Restore from Keychain") }
    var deleteAll: String { t("Удалить Бэкап из Keychain", "Delete Keychain Backup") }
    var notice: String {
        t("Сессии шифруются и хранятся в Keychain устройства. Сессии никогда не покидают ваше устройство.\n\nВАЖНО: Чтобы восстановить сессии на новом устройстве или после сброса системы, ОБЯЗАТЕЛЬНО включите шифрование резервных копий ОС, иначе Keychain будет утерян при восстановлении.\n\nПРИМЕЧАНИЕ: Сессии всё ещё могут быть разлогинены самим Telegram или с другого устройства.",
          "Sessions are encrypted and stored in this device's Keychain. Sessions never leave your device.\n\nIMPORTANT: To restore sessions on a new device or after a system reset, you MUST enable encrypted OS backups, otherwise the Keychain will be lost on restore.\n\nNOTE: Sessions can still be logged out by Telegram itself or from another device.")
    }
    var sessionsTitle: String { t("Сессии", "Sessions") }
    var loggedIn: String { t("Залогинен", "Logged in") }
    var loggedOut: String { t("Не залогинен", "Logged out") }
    func lastBackup(_ date: String) -> String { t("Последний бэкап: ", "Last backup: ") + date }

    var busy: String { t("Пожалуйста, подождите…", "Please wait…") }
    var done: String { t("Готово", "Done") }
    var error: String { t("Ошибка", "Error") }
    func backupCreated(_ n: Int) -> String { t("Бэкап создан. Аккаунтов: ", "Backup created. Accounts: ") + "\(n)." }
    var restoreTitle: String { t("Восстановить из бэкапа?", "Restore from backup?") }
    var restoreText: String { t("Данные аккаунтов будут заменены данными из бэкапа. После подготовки потребуется перезапуск приложения.", "Account data will be replaced with the backup. The app must be restarted after preparation.") }
    var restorePrepared: String { t("Полностью закройте и снова откройте приложение, чтобы завершить восстановление.", "Fully close and reopen the app to finish restoring.") }
    var deleteTitle: String { t("Удалить бэкап?", "Delete backup?") }
    var deleteText: String { t("Зашифрованный бэкап и ключ из Keychain будут удалены. Действие необратимо.", "The encrypted backup and its key will be removed from the Keychain. This cannot be undone.") }
    var backupDeleted: String { t("Бэкап удалён.", "Backup deleted.") }
    var cancel: String { t("Отмена", "Cancel") }
    var delete: String { t("Удалить", "Delete") }
    var restart: String { t("Перезапустить", "Restart") }
    var ok: String { t("OK", "OK") }
}

@available(iOS 13.0, *)
private struct AorusBackupSession: Identifiable {
    let id: String        // account-record id
    let userId: Int64     // Telegram user id
    let title: String     // @username or display name or user id
}

@available(iOS 13.0, *)
private enum AorusSessionBackupAlert: Identifiable {
    case info(String, String)
    case restoreConfirm
    case deleteConfirm

    var id: String {
        switch self {
        case .info:
            return "info"
        case .restoreConfirm:
            return "restore"
        case .deleteConfirm:
            return "delete"
        }
    }
}

@available(iOS 13.0, *)
struct AorusSessionBackupRow: View {
    fileprivate let session: AorusBackupSession
    let isLoggedIn: Bool
    let dateText: String
    fileprivate let l10n: AorusSessionL10n

    var body: some View {
        HStack {
            VStack(alignment: .leading, spacing: 2) {
                Text(session.title)
                    .font(.body)
                Text("ID: \(formatId(session.userId))")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
                Text(l10n.lastBackup(dateText))
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            Spacer()
            Text(isLoggedIn ? l10n.loggedIn : l10n.loggedOut)
                .font(.caption)
                .foregroundColor(isLoggedIn ? .white : .secondary)
                .padding(.horizontal, 6)
                .padding(.vertical, 2)
                .background(isLoggedIn ? Color.accentColor : Color.secondary.opacity(0.1))
                .cornerRadius(4)
        }
        .padding(.vertical, 4)
    }

    private func formatId(_ userId: Int64) -> String {
        let digits = Array(String(userId))
        var out = ""
        for (i, ch) in digits.enumerated() {
            if i > 0 && (digits.count - i) % 3 == 0 { out.append(" ") }
            out.append(ch)
        }
        return out
    }
}

@available(iOS 13.0, *)
struct AorusSessionBackupView: View {
    let context: AccountContext
    let isRu: Bool

    @State private var sessions: [AorusBackupSession] = []
    @State private var loggedInIds: Set<String> = []
    @State private var dateText: String = ""
    @State private var isBusy = false

    @State private var activeAlert: AorusSessionBackupAlert?

    private var l10n: AorusSessionL10n { AorusSessionL10n(isRu: isRu) }
    private var hasBackup: Bool { AccountBackupManager.shared.hasBackup() }

    var body: some View {
        List {
            Section {
                Button(action: performBackup) {
                    HStack {
                        Image(systemName: "key.fill").frame(width: 30)
                        Text(l10n.backup)
                        Spacer()
                    }
                }
                .disabled(isBusy)

                Button(action: { activeAlert = .restoreConfirm }) {
                    HStack {
                        Image(systemName: "arrow.2.circlepath").frame(width: 30)
                        Text(l10n.restore)
                        Spacer()
                    }
                }
                .disabled(isBusy || !hasBackup)

                Button(action: { activeAlert = .deleteConfirm }) {
                    HStack {
                        Image(systemName: "trash").frame(width: 30)
                        Text(l10n.deleteAll)
                        Spacer()
                    }
                }
                .foregroundColor(hasBackup ? .red : .secondary)
                .disabled(isBusy || !hasBackup)
            }

            Text(l10n.notice)
                .font(.caption)
                .foregroundColor(.secondary)

            Section(header: Text(l10n.sessionsTitle)) {
                ForEach(sessions) { session in
                    AorusSessionBackupRow(
                        session: session,
                        isLoggedIn: loggedInIds.contains(session.id),
                        dateText: dateText,
                        l10n: l10n
                    )
                }
            }
        }
        .onAppear(perform: reload)
        .alert(item: $activeAlert) { alert in
            switch alert {
            case let .info(title, text):
                return Alert(title: Text(title), message: Text(text), dismissButton: .default(Text(l10n.ok)))
            case .restoreConfirm:
                return Alert(
                    title: Text(l10n.restoreTitle),
                    message: Text(l10n.restoreText),
                    primaryButton: .default(Text(l10n.restart), action: performRestore),
                    secondaryButton: .cancel(Text(l10n.cancel))
                )
            case .deleteConfirm:
                return Alert(
                    title: Text(l10n.deleteTitle),
                    message: Text(l10n.deleteText),
                    primaryButton: .destructive(Text(l10n.delete), action: performDeleteAll),
                    secondaryButton: .cancel(Text(l10n.cancel))
                )
            }
        }
    }

    // MARK: - Data

    private func reload() {
        let mgr = AccountBackupManager.shared
        let accounts = mgr.backupAccounts()
        self.loggedInIds = Set(mgr.localAccountIds())
        self.sessions = accounts.map { acc in
            let title: String
            if let username = acc.username, !username.isEmpty {
                title = "@\(username)"
            } else if !acc.name.isEmpty {
                title = acc.name
            } else {
                title = String(acc.userId)
            }
            return AorusBackupSession(id: acc.id, userId: acc.userId, title: title)
        }
        if let info = mgr.backupInfo() {
            let df = DateFormatter()
            df.locale = isRu ? Locale(identifier: "ru_RU") : Locale(identifier: "en_US")
            df.dateFormat = isRu ? "d MMMM yyyy 'г.,' HH:mm" : "MMM d, yyyy, HH:mm"
            self.dateText = df.string(from: info.date)
        } else {
            self.dateText = ""
        }
    }

    // MARK: - Actions

    private func performBackup() {
        isBusy = true
        let _ = (context.sharedContext.activeAccountsWithInfo
        |> take(1)
        |> deliverOnMainQueue).startStandalone(next: { value in
            var infos: [AccountBackupManager.AccountDisplayInfo] = []
            for account in value.accounts {
                let recordId = String(UInt64(bitPattern: account.account.id.int64))
                let userId = account.peer.id.id._internalGetInt64Value()
                var avatarPNG: Data? = nil
                if let representation = account.peer.profileImageRepresentations.first,
                   let path = account.account.postbox.mediaBox.completedResourcePath(representation.resource),
                   let image = UIImage(contentsOfFile: path) {
                    avatarPNG = downscalePNG(image)
                }
                infos.append(AccountBackupManager.AccountDisplayInfo(
                    id: recordId,
                    userId: userId,
                    username: account.peer.addressName,
                    name: account.peer.debugDisplayTitle,
                    avatarPNG: avatarPNG
                ))
            }
            DispatchQueue.global(qos: .userInitiated).async {
                let result = AccountBackupManager.shared.performBackup(displayInfos: infos)
                DispatchQueue.main.async {
                    self.isBusy = false
                    self.reload()
                    switch result {
                    case let .success(info):
                        self.present(self.l10n.done, self.l10n.backupCreated(info.accountCount))
                    case let .failure(message):
                        self.present(self.l10n.error, message)
                    }
                }
            }
        })
    }

    private func performRestore() {
        isBusy = true
        DispatchQueue.global(qos: .userInitiated).async {
            let result = AccountBackupManager.shared.prepareRestore()
            DispatchQueue.main.async {
                self.isBusy = false
                self.reload()
                switch result {
                case .success:
                    self.present(self.l10n.done, self.l10n.restorePrepared)
                case let .failure(message):
                    self.present(self.l10n.error, message)
                }
            }
        }
    }

    private func performDeleteAll() {
        guard !isBusy else { return }
        isBusy = true
        DispatchQueue.global(qos: .userInitiated).async {
            AccountBackupManager.shared.deleteBackup()
            DispatchQueue.main.async {
                self.isBusy = false
                self.reload()
                self.present(self.l10n.done, self.l10n.backupDeleted)
            }
        }
    }

    private func present(_ title: String, _ text: String) {
        activeAlert = .info(title, text)
    }

    private func downscalePNG(_ image: UIImage, maxSide: CGFloat = 120.0) -> Data? {
        let side = max(image.size.width, image.size.height)
        let scale = side > maxSide ? maxSide / side : 1.0
        let target = CGSize(width: floor(image.size.width * scale), height: floor(image.size.height * scale))
        guard target.width > 0, target.height > 0 else { return image.pngData() }
        let format = UIGraphicsImageRendererFormat()
        format.opaque = false
        format.scale = 1.0
        return UIGraphicsImageRenderer(size: target, format: format).image { _ in
            image.draw(in: CGRect(origin: .zero, size: target))
        }.pngData()
    }
}
