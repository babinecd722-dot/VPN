import Foundation

// AorusGram localization.
//
// The client supports two languages — Russian and English — and follows the
// language selected inside Telegram. Resolution rule: a Telegram base-language
// code of "ru" (or any "ru-*" variant) → Russian; anything else → English.
//
// Two consumption paths:
//   1. UI with direct access to PresentationData → AorusL10n(strings.baseLanguageCode).
//   2. Cross-module / context-free call sites (e.g. the chat bubble that renders
//      deleted messages, or TelegramCore postbox markers) → AorusL10n.current,
//      which reads the resolved language persisted by AppDelegate's
//      presentationData observer under the "aorusgram_lang" UserDefaults key.
public enum AorusLang: String {
    case en
    case ru

    // Map a Telegram base-language code to one of the two supported languages.
    // Any language other than Russian falls back to English, as requested.
    public static func resolve(_ code: String?) -> AorusLang {
        guard let code = code?.lowercased() else { return .en }
        if code == "ru" || code.hasPrefix("ru-") || code.hasPrefix("ru_") {
            return .ru
        }
        return .en
    }

    // UserDefaults key shared with TelegramCore-injected code (postbox markers).
    public static let storageKey = "aorusgram_lang"

    // Persist the resolved language so context-free call sites can read it.
    public static func store(_ code: String?) {
        UserDefaults.standard.set(resolve(code).rawValue, forKey: storageKey)
    }

    // Best-effort current language for call sites without PresentationData.
    // Prefers the value persisted by the AppDelegate observer; before that fires
    // it falls back to the device language (Russian only for a Russian device).
    public static var current: AorusLang {
        if let raw = UserDefaults.standard.string(forKey: storageKey), let lang = AorusLang(rawValue: raw) {
            return lang
        }
        return resolve(Locale.preferredLanguages.first)
    }
}

public struct AorusL10n {
    public let lang: AorusLang

    public init(_ code: String?) {
        self.lang = AorusLang.resolve(code)
    }

    public init(lang: AorusLang) {
        self.lang = lang
    }

    public static var current: AorusL10n {
        return AorusL10n(lang: AorusLang.current)
    }

    private func t(_ ru: String, _ en: String) -> String {
        return self.lang == .ru ? ru : en
    }

    // MARK: Settings — section headers
    public var privacyHeader: String { t("ПРИВАТНОСТЬ", "PRIVACY") }
    public var aiHeader: String { t("AI ФУНКЦИИ", "AI FEATURES") }
    public var perfHeader: String { t("ПРОИЗВОДИТЕЛЬНОСТЬ", "PERFORMANCE") }
    public var uiHeader: String { t("ИНТЕРФЕЙС", "INTERFACE") }
    public var antiSpoofHeader: String { t("АНТИ-СПУФ", "ANTI-SPOOF") }
    public var accountBackupHeader: String { t("АККАУНТ", "ACCOUNT") }
    public var aorusCodeHeader: String { t("AORUS CODE", "AORUS CODE") }

    // MARK: Settings — subscription
    public var subscription: String { t("Подписка", "Subscription") }
    public var subscriptionSubtitle: String { t("Управление и продление", "Manage & renew") }

    // MARK: Settings — rows
    public var ghostMode: String { t("Режим призрака", "Ghost Mode") }
    public var deletedMessages: String { t("Удалённые сообщения", "Deleted Messages") }
    public var editedMessages: String { t("Изменённые сообщения", "Edited Messages") }
    public var clearDeletedCache: String { t("Очистить кэш", "Clear Cache") }
    public var antiScreenshot: String { t("Скрытие экрана при записи", "Hide Screen While Recording") }
    public var callRecording: String { t("Запись звонков", "Call Recording") }
    public var voiceTranscription: String { t("Расшифровка голосовых", "Voice to Text") }
    public var chatSummary: String { t("Сводка чата", "Chat Summary") }
    public var translator: String { t("Переводчик", "Translator") }
    public var shareButton: String { t("Кнопка «Поделиться»", "Share Button") }
    public var quickTranslateButton: String { t("Кнопка быстрого перевода", "Quick Translate Button") }
    public var quickTranscribeButton: String { t("Кнопка быстрой транскрипции", "Quick Transcription Button") }
    public var quickShareButton: String { t("Кнопка быстро поделиться", "Quick Share Button") }
    public var videoMessagesHeader: String { t("ВИДЕОСООБЩЕНИЯ", "VIDEO MESSAGES") }
    public var videoMessagesRearCamera: String { t("Запись сразу с задней камеры", "Start Recording With Rear Camera") }
    public var callsHeader: String { t("ЗВОНКИ", "CALLS") }
    public var autoReply: String { t("Автоответчик", "Auto-Reply") }
    public var downloadAccel: String { t("Ускоритель загрузок", "Download Accelerator") }
    public var maxMediaQuality: String { t("Макс качество медиа", "Max Media Quality") }
    public var antiSpam: String { t("Анти-спам", "Anti-Spam") }
    public var performanceStats: String { t("Показывать статистику", "Show Statistics") }
    public var performanceUptime: String { t("Uptime", "Uptime") }
    public var performanceRAM: String { t("RAM", "RAM") }
    public var performanceCPU: String { t("CPU", "CPU") }
    public var performanceFPS: String { t("FPS", "FPS") }
    public var performanceBattery: String { t("Батарея", "Battery") }
    public var performanceNetwork: String { t("Сеть", "Network") }
    public var performanceDisk: String { t("Диск", "Disk") }
    public var performanceThermal: String { t("Нагрев", "Thermal") }
    public var performanceGraph: String { t("График", "Graph") }
    public var ramAutoClean: String { t("Автоочистка RAM", "Auto-Clean RAM") }
    public var ramCleanInterval: String { t("Интервал очистки RAM", "RAM Cleanup Interval") }
    public var thermalNominal: String { t("Норма", "Normal") }
    public var thermalFair: String { t("Тепло", "Warm") }
    public var thermalSerious: String { t("ГОРЯЧО", "HOT") }
    public var thermalCritical: String { t("Критично", "Critical") }
    public var cacheAutoClean: String { t("Автоочистка кэша", "Auto-Clean Cache") }
    public var cacheInterval: String { t("Интервал очистки", "Cleanup Interval") }
    // Interval value in hours: "6 ч"/"6 h", or whole days as "7 дн"/"7 d".
    public func cacheIntervalValue(_ hours: Int) -> String {
        if hours % 24 == 0 { let d = hours / 24; return t("\(d) дн", "\(d) d") }
        return t("\(hours) ч", "\(hours) h")
    }
    public func ramIntervalValue(_ seconds: Int) -> String {
        if seconds < 60 { return t("\(seconds) сек", "\(seconds) sec") }
        let minutes = seconds / 60
        return t("\(minutes) мин", "\(minutes) min")
    }
    public var glassUI: String { t("Glass UI", "Glass UI") }
    public var siriShortcuts: String { t("Siri Shortcuts", "Siri Shortcuts") }
    public var amoledMode: String { t("AMOLED интерфейс", "AMOLED Interface") }
    public var profileReportButton: String { t("Кнопка «Пожаловаться»", "Report Button") }

    // Badge picker (notch-area logo): AorusGram / ATunnel / off.
    public var appBadge: String { t("Бейдж", "Badge") }
    public var appBadgeAorus: String { t("AorusGram", "AorusGram") }
    public var appBadgeATunnel: String { t("ATunnel", "ATunnel") }
    public var appBadgeOff: String { t("Отключен", "Disabled") }
    public var hideCallsTab: String { t("Вкладка «Звонки»", "Calls Tab") }
    public var hideContactsTab: String { t("Вкладка «Контакты»", "Contacts Tab") }
    public var antiSpoofDeleted: String { t("Анти-спуф удалёнок", "Anti-Spoof Deletions") }
    public var antiSpoofOnline: String { t("Анти-спуф онлайна", "Anti-Spoof Online") }
    public var accountBackup: String { t("Бэкап аккаунтов", "Account Backup") }
    public var misc: String { t("Прочее", "Other") }
    public var aorusCode: String { t("AorusCode", "AorusCode") }
    public var aorusCodeHint: String { t("Удерживайте кнопку прикрепления чтобы отправить скрытое сообщение", "Hold the attachment button to send a hidden message") }
    public var accountDetails: String { t("Подробнее", "Details") }
    public var officialChannel: String { t("Официальный канал", "Official channel") }
    public var proxyDiagnostics: String { t("Диагностика прокси", "Proxy diagnostics") } // AORUS-DIAG — opens ATunnelStatusViewController

    // MARK: Messages section (tap gestures + local editing)
    public var messagesHeader: String { t("СООБЩЕНИЯ", "MESSAGES") }
    public var doubleTapCopy: String { t("Двойное нажатие для копирования", "Double Tap to Copy") }
    public var tripleTapDelete: String { t("Тройное нажатие для удаления", "Triple Tap to Delete") }
    public var editLocally: String { t("Изменить локально", "Edit Locally") }
    public var userMessagesInGroup: String { t("Сообщения пользователя", "User's Messages") }
    public var editLocallyFooter: String { t("Изменяет текст сообщения только у вас на устройстве. Собеседник изменений не видит.", "Edits a message's text only on your device. Other people don't see the change.") }

    // MARK: Voice Twin
    public var voiceTwin: String { t("Голосовой двойник", "Voice Twin") }
    public var voiceTwinFooter: String { t("Изменяет ваш голос на выбранный в исходящих голосовых сообщениях, видеосообщениях (кружочках) и звонках.", "Changes your voice to the selected one in outgoing voice messages, video messages (round videos) and calls.") }
    public var voiceTwinVoiceHeader: String { t("ГОЛОС", "VOICE") }
    public var voiceAnonymous: String { t("Аноним", "Anonymous") }
    public var voiceMale: String { t("Мужской", "Male") }
    public var voiceFemale: String { t("Женский", "Female") }
    public var voiceRobot: String { t("Робот", "Robot") }
    public var voiceChild: String { t("Ребёнок", "Child") }

    // MARK: Device Spoof
    public var deviceSpoofHeader: String { t("ДЕВАЙС-СПУФ", "DEVICE SPOOF") }
    public var deviceSpoof: String { t("Устройство", "Device") }
    public var deviceSpoofOff: String { t("Выкл.", "Off") }
    public var deviceSpoofCancel: String { t("Отмена", "Cancel") }

    // MARK: Media bypass
    public var bypassHeader: String { t("ОБХОД ОГРАНИЧЕНИЙ", "BYPASS") }
    public var bypassSavePaid: String { t("Сохранение платных медиа", "Save Paid Media") }
    public var bypassSaveViewOnce: String { t("Сохранение одноразок", "Save View-Once Media") }
    public var bypassStoryDownload: String { t("Скачивание сторис", "Download Stories") }

    // MARK: Deleted / edited markers
    // Trailing space matches the original "Удалено " + relative-time layout.
    public var deletedPrefix: String { t("Удалено ", "Deleted ") }
}
