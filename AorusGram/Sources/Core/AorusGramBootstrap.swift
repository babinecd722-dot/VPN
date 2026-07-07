import Foundation
import UIKit

// Single entry point called once from AppDelegate after the account is ready.
// aorus_branding.py patches AppDelegate.swift to insert:
//   AorusGramBootstrap.shared.setup(accountPath:)
public final class AorusGramBootstrap {
    public static let shared = AorusGramBootstrap()
    private init() {}

    private var didSetup = false

    public func setup(accountPath: String? = nil) {
        guard !didSetup else { return }
        didSetup = true

        // License-layer hardening (audience is non-jailbroken sideload, so this is
        // safe). Refuse debugger attach, then hard-stop on a jailbreak/injection.
        AorusEnvGuard.denyDebuggerAttach()
        AorusEnvGuard.enforceAtLaunch()

        // Persist the account-data root so AccountBackupManager can locate the
        // accounts-metadata / account-* directories for backup & restore.
        if let accountPath = accountPath, !accountPath.isEmpty {
            AccountBackupManager.shared.rootPath = accountPath
        }

        // Integrity check — runs async so it never blocks app launch
        DispatchQueue.global(qos: .utility).async {
            AorusTamperGuard.shared.verify()
        }

        // System proxy (network shield) — touching `.shared` runs load(), which
        // synchronously republishes the cached proxy to the flat UserDefaults
        // keys read by the injected network layer, so a returning user connects
        // through the proxy from the very first packet. Then refresh from the
        // control API; on success it posts .aorusProxyConfigUpdated and the
        // injected Account.swift observer hot-applies it without a relaunch.
        AorusProxyManager.shared.refresh()

        // Client spoof — must be before any MTProto connection is made
        ClientSpoofManager.applySwizzle()

        // Ghost Mode — restore persisted state only. The MTProto-level ObjC swizzle
        // (GhostModeSwizzler) was REMOVED because its body.perform("serialize") path
        // caused intermittent crashes on toggling. Source-level patches injected by
        // aorus_branding.py (ManagedAccountPresence, ManagedLocalInputActivities,
        // SynchronizePeerReadState) are the sole and sufficient enforcement layer now.
        GhostModeManager.shared.load()

        // Deleted messages — register BGTask and schedule first sync
        DeletedMessagesCache.shared.registerBackgroundTask()
        DeletedMessagesCache.shared.scheduleBackgroundSync()

        // Anti-screenshot
        if AorusGramConfig.isEnabled(.antiScreenshot) {
            DispatchQueue.main.asyncAfter(deadline: .now() + 1) {
                AntiScreenshotManager.shared.enable()
            }
        }

        // Secret pin
        SecretPinManager.shared.load()

        // Streaks
        StreakManager.shared.tick()

        // Performance HUD — transparent pass-through overlay and RAM cleanup timer.
        // Lives in core AorusGram so AppDelegate does not need to import AorusGramUI.
        AorusPerformanceHUDManager.shared.restorePersistedHUDAfterLaunch()

        // Auto-reply
        AutoReplyManager.shared.load()

        // Anti-spam
        AntiSpamManager.shared.setEnabled(AorusGramConfig.isEnabled(.antiSpam))

        // Anti-spoof — touch shared instance so load() runs and mirrors current
        // dictionary state to flat UserDefaults keys that TelegramCore patches read.
        _ = AntiSpoofManager.shared

        // Subscribe to TelegramCore delete events (cross-module NotificationCenter bridge)
        NotificationCenter.default.addObserver(
            forName: .aorusWillDeleteMessage,
            object: nil,
            queue: nil
        ) { note in
            DeletedMessagesCache.shared.handleWillDeleteNotification(note)
        }
        NotificationCenter.default.addObserver(
            forName: .aorusWillDeleteMessageGlobalId,
            object: nil,
            queue: nil
        ) { note in
            DeletedMessagesCache.shared.handleWillDeleteByGlobalIdNotification(note)
        }
        NotificationCenter.default.addObserver(
            forName: .aorusWillEditMessage,
            object: nil,
            queue: nil
        ) { note in
            DeletedMessagesCache.shared.handleWillEditNotification(note)
        }

        // Subscribe to incoming message events (injected by branding.py into AccountStateManager)
        NotificationCenter.default.addObserver(
            forName: NSNotification.Name("aorusgram.didReceiveMessage"),
            object: nil,
            queue: nil
        ) { [weak self] note in
            self?.handleIncomingMessage(note)
        }

        // Siri Shortcuts — donate on bootstrap if enabled, re-donate / clear when toggled.
        // We use a raw notification name because `.aorusSettingsChanged` is defined in
        // the AorusGramUI module (AorusGramManager) which AorusGram doesn't depend on.
        if #available(iOS 16.0, *) {
            if AorusGramConfig.isEnabled(.siriShortcuts) {
                SiriShortcutsManager.shared.donateAllDefaults()
            }
            NotificationCenter.default.addObserver(
                forName: NSNotification.Name("aorusgram_settings_changed"),
                object: nil,
                queue: .main
            ) { _ in
                if AorusGramConfig.isEnabled(.siriShortcuts) {
                    SiriShortcutsManager.shared.donateAllDefaults()
                } else {
                    NSUserActivity.deleteAllSavedUserActivities(completionHandler: {})
                }
            }
        }

        // Active Telegram account id → license gate. Published from AppDelegate
        // (cold start + each foreground) and from TelegramCore after a state sync,
        // both via NotificationCenter (branding.py injects the publishers). The gate
        // de-dupes, so multiple publishers are harmless. This is what lets keys issued
        // to a specific Telegram ID activate (check/bootstrap/activate carry the id).
        NotificationCenter.default.addObserver(
            forName: NSNotification.Name("aorusgram.activeAccountId"),
            object: nil,
            queue: .main
        ) { note in
            if let uid = (note.userInfo?["telegramUserId"] as? NSNumber)?.int64Value {
                LicenseGate.shared.setTelegramUserId(uid)
            }
        }

        // Subscription / license gate. INERT unless an HMAC key is provisioned, and
        // strictly fail-open — it can never lock the user out without a definitive
        // server/cache "expired/banned" verdict.
        LicenseGate.shared.start()

        observeAppLifecycle()
    }

    // MARK: - Incoming message handler (anti-spam + auto-reply)

    private func handleIncomingMessage(_ note: Notification) {
        guard let info = note.userInfo else { return }
        let peerId = (info["peerId"] as? NSNumber)?.int64Value ?? 0
        let text   = info["text"]   as? String ?? ""
        let msgId = (info["msgId"] as? NSNumber)?.int32Value
        let senderId = (note.userInfo?["senderId"] as? NSNumber)?.int64Value ?? peerId

        // Anti-spam gate must run before any local Aorus caches. If a message is
        // classified as spam, it should not be stored by deleted-message recovery,
        // auto-reply, or presence heuristics.
        // Only gate one-on-one private chats from non-contacts (the native
        // prefilter injected into AccountStateManager computes this the same way).
        let spamEligible = (info["spamEligible"] as? NSNumber)?.boolValue ?? false
        if spamEligible, AorusGramConfig.isEnabled(.antiSpam) {
            let verdict = AntiSpamManager.shared.check(peerId: senderId, text: text)
            if verdict.isSpam {
                AntiSpamManager.shared.processIncoming(peerId: senderId, text: text, messageId: msgId, verdict: verdict)
                if !verdict.isThreat {
                    return
                }
            }
        }

        // Pre-cache for deleted-messages feature — captures content before any deletion.
        // Without this the cache only sees messages whose delete-hook fires, which is unreliable.
        DeletedMessagesCache.shared.handleIncomingNotification(note)

        // Anti-spoof online — record peer activity so we can show real "last seen"
        // even when the peer hides it client-side. Each incoming message is direct
        // proof they were online at that moment.
        AntiSpoofManager.shared.recordActivity(peerId: senderId, kind: .message)

        // Auto-reply
        if AorusGramConfig.isEnabled(.autoReply) {
            AutoReplyManager.shared.handleIncoming(peerId: peerId, text: text)
        }
    }

    // MARK: - App lifecycle

    private func observeAppLifecycle() {
        let nc = NotificationCenter.default
        nc.addObserver(self, selector: #selector(appDidBecomeActive),
                       name: UIApplication.didBecomeActiveNotification, object: nil)
        nc.addObserver(self, selector: #selector(appDidEnterBackground),
                       name: UIApplication.didEnterBackgroundNotification, object: nil)
    }

    @objc private func appDidBecomeActive() {
        DeletedMessagesCache.shared.scheduleBackgroundSync()
        StreakManager.shared.tick()
        // Re-validate the system proxy on every foreground (TTL-aware inside).
        AorusProxyManager.shared.refresh()
        AorusPerformanceHUDManager.shared.restorePersistedHUDAfterLaunch()
    }

    @objc private func appDidEnterBackground() {
        DeletedMessagesCache.shared.scheduleBackgroundSync()
    }
}
