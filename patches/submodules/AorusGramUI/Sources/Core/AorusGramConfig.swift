import Foundation

enum AorusGramConfig {
    static let appName = "AorusGram"
    static let version = "1.0.0"
    static let officialChannelURL = "https://t.me/aorusgram"
    static let officialChannelUsername = "aorusgram"

    enum Feature: String, CaseIterable {
        case ghostMode          = "ghost_mode"
        case deletedMessages    = "deleted_messages"
        case maxMediaQuality    = "max_media_quality"
        case antiSpam           = "anti_spam"
        case downloadAccel      = "download_accel"
        case siriShortcuts      = "siri_shortcuts"
        case unlimitedAccounts  = "unlimited_accounts"
        case glassUI            = "glass_ui"
        case messageScheduler   = "message_scheduler"
        case mediaManager       = "media_manager"
        case translator         = "translator"
        case voiceTranscription = "voice_transcription"
        case chatSummary        = "chat_summary"
        case antiScreenshot     = "anti_screenshot"
        case secretPin          = "secret_pin"
        case smartFolders       = "smart_folders"
        case streaks            = "streaks"
        case customIcons        = "custom_icons"
        case autoReply          = "auto_reply"
        case pinboard           = "pinboard"
        case editLocally        = "edit_locally"
    }

    static func isEnabled(_ feature: Feature) -> Bool {
        // License kill-switch: when the subscription lock is active (set by
        // LicenseGate on an expired/banned/no-access verdict) every AorusGram feature
        // reads as disabled — so the background logic stops too, not just the UI.
        // Fail-open: absent/false key = normal behaviour → an active user is never hit.
        if UserDefaults.standard.bool(forKey: "aorusgram_license_locked") { return false }
        return UserDefaults.standard.object(forKey: "aorusgram_feature_\(feature.rawValue)") as? Bool ?? defaultEnabled(feature)
    }

    static func setEnabled(_ feature: Feature, _ value: Bool) {
        let effectiveValue = UserDefaults.standard.bool(forKey: "aorusgram_license_locked") ? false : value
        UserDefaults.standard.set(effectiveValue, forKey: "aorusgram_feature_\(feature.rawValue)")
    }

    private static func defaultEnabled(_ feature: Feature) -> Bool {
        switch feature {
        case .glassUI, .downloadAccel, .maxMediaQuality, .antiSpam, .deletedMessages: return true
        default: return false
        }
    }
}
