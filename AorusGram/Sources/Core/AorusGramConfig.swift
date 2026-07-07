import Foundation

/// Shared feature flags (UserDefaults). Public so `TelegramUI` (e.g. AppDelegate hooks) can read them.
public enum AorusGramConfig {
    public static let appName = "AorusGram"
    public static let version = "1.0.0"
    public static let officialChannelURL = "https://t.me/aorusgram"
    public static let officialChannelUsername = "aorusgram"

    public enum Feature: String, CaseIterable {
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
    }

    public static func isEnabled(_ feature: Feature) -> Bool {
        // Hard license gate: flat feature flags are never authoritative while the
        // subscription layer is locked. This closes the simple UserDefaults bypass
        // where a patched client flips `aorusgram_feature_*` back to true after the
        // kill-switch has zeroed it.
        if UserDefaults.standard.bool(forKey: "aorusgram_license_locked") { return false }
        return UserDefaults.standard.object(forKey: "aorusgram_feature_\(feature.rawValue)") as? Bool ?? defaultEnabled(feature)
    }

    public static func setEnabled(_ feature: Feature, _ value: Bool) {
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
