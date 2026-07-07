import Foundation
import UIKit
import Display
import SwiftSignalKit
import TelegramCore
import TelegramPresentationData
import ItemListUI
import PresentationDataUtils
import AccountContext

// MARK: - "Прочее" (Other) screen
//
// A spillover screen reachable from the AorusGram settings, holding optional /
// future features so the main settings list stays focused. Currently hosts the
// Local Premium toggle and the Fake Gifts manager.

private enum MiscSection: Int32 {
    case premium
    case fakeGifts
    case fakeStars
    case calls
    case autoReplyS
    case chatSummaryS
    case antiSearch
    case security
}

private struct MiscState: Equatable {
    var localPremium: Bool
    var fakeStars: Bool
    var fakeStarsAmount: String
    var autoReply: Bool
    var chatSummary: Bool
    var antiSearch: Bool
    var anonymousStickers: Bool
    var profileLink: Bool
    var profileLinkTargetPeerId: Int64
    var profileLinkTargetName: String
    var phoneSpoof: Bool
    var phoneSpoofNumber: String
    var mediaMetadata: Bool
    var linkProtection: Bool
    var linkProtectionRedirects: Bool
    var linkProtectionBlockFiles: Bool
}

private final class MiscArguments {
    let setLocalPremium: (Bool) -> Void
    let openFakeGifts: () -> Void
    let setFakeStars: (Bool) -> Void
    let setFakeStarsAmount: (String) -> Void
    let openVoiceTwin: () -> Void
    let openQuickReplies: () -> Void
    let setAutoReply: (Bool) -> Void
    let setChatSummary: (Bool) -> Void
    let setAntiSearch: (Bool) -> Void
    let setAnonymousStickers: (Bool) -> Void
    let setProfileLink: (Bool) -> Void
    let selectProfileLinkSelf: () -> Void
    let selectProfileLinkPeer: () -> Void
    let setPhoneSpoof: (Bool) -> Void
    let setPhoneSpoofNumber: (String) -> Void
    let randomizePhoneSpoof: () -> Void
    let setMediaMetadata: (Bool) -> Void
    let setLinkProtection: (Bool) -> Void
    let setLinkProtectionRedirects: (Bool) -> Void
    let setLinkProtectionBlockFiles: (Bool) -> Void

    init(setLocalPremium: @escaping (Bool) -> Void, openFakeGifts: @escaping () -> Void, setFakeStars: @escaping (Bool) -> Void, setFakeStarsAmount: @escaping (String) -> Void, openVoiceTwin: @escaping () -> Void, openQuickReplies: @escaping () -> Void, setAutoReply: @escaping (Bool) -> Void, setChatSummary: @escaping (Bool) -> Void, setAntiSearch: @escaping (Bool) -> Void, setAnonymousStickers: @escaping (Bool) -> Void, setProfileLink: @escaping (Bool) -> Void, selectProfileLinkSelf: @escaping () -> Void, selectProfileLinkPeer: @escaping () -> Void, setPhoneSpoof: @escaping (Bool) -> Void, setPhoneSpoofNumber: @escaping (String) -> Void, randomizePhoneSpoof: @escaping () -> Void, setMediaMetadata: @escaping (Bool) -> Void, setLinkProtection: @escaping (Bool) -> Void, setLinkProtectionRedirects: @escaping (Bool) -> Void, setLinkProtectionBlockFiles: @escaping (Bool) -> Void) {
        self.setLocalPremium = setLocalPremium
        self.openFakeGifts = openFakeGifts
        self.setFakeStars = setFakeStars
        self.setFakeStarsAmount = setFakeStarsAmount
        self.openVoiceTwin = openVoiceTwin
        self.openQuickReplies = openQuickReplies
        self.setAutoReply = setAutoReply
        self.setChatSummary = setChatSummary
        self.setAntiSearch = setAntiSearch
        self.setAnonymousStickers = setAnonymousStickers
        self.setProfileLink = setProfileLink
        self.selectProfileLinkSelf = selectProfileLinkSelf
        self.selectProfileLinkPeer = selectProfileLinkPeer
        self.setPhoneSpoof = setPhoneSpoof
        self.setPhoneSpoofNumber = setPhoneSpoofNumber
        self.randomizePhoneSpoof = randomizePhoneSpoof
        self.setMediaMetadata = setMediaMetadata
        self.setLinkProtection = setLinkProtection
        self.setLinkProtectionRedirects = setLinkProtectionRedirects
        self.setLinkProtectionBlockFiles = setLinkProtectionBlockFiles
    }
}

private enum MiscEntry: ItemListNodeEntry {
    case premiumHeader(PresentationTheme, String)
    case localPremium(PresentationTheme, String, Bool)
    case premiumInfo(PresentationTheme, String)
    case fakeGifts(PresentationTheme, String)
    case fakeStarsHeader(PresentationTheme, String)
    case fakeStars(PresentationTheme, String, Bool)
    case fakeStarsCount(PresentationTheme, String, String)
    case fakeStarsInfo(PresentationTheme, String)
    case callsHeader(PresentationTheme, String)
    case voiceTwin(PresentationTheme, String)
    case autoReply(PresentationTheme, String, Bool)
    case chatSummary(PresentationTheme, String, Bool)
    case antiSearchHeader(PresentationTheme, String)
    case antiSearch(PresentationTheme, String, Bool)
    case antiSearchInfo(PresentationTheme, String)
    case anonymousStickers(PresentationTheme, String, Bool)
    case anonymousStickersInfo(PresentationTheme, String)
    case profileLink(PresentationTheme, String, Bool)
    case profileLinkSelf(PresentationTheme, String, String)
    case profileLinkPeer(PresentationTheme, String, String)
    case profileLinkInfo(PresentationTheme, String)
    case phoneSpoof(PresentationTheme, String, Bool)
    case phoneSpoofNumber(PresentationTheme, String, String)
    case phoneSpoofRandomize(PresentationTheme, String)
    case phoneSpoofInfo(PresentationTheme, String)
    case mediaMetadata(PresentationTheme, String, Bool)
    case mediaMetadataInfo(PresentationTheme, String)
    case securityHeader(PresentationTheme, String)
    case linkProtection(PresentationTheme, String, Bool)
    case linkProtectionRedirects(PresentationTheme, String, Bool)
    case linkProtectionBlockFiles(PresentationTheme, String, Bool)
    case linkProtectionInfo(PresentationTheme, String)

    var section: ItemListSectionId {
        switch self {
        case .premiumHeader, .localPremium, .premiumInfo:
            return MiscSection.premium.rawValue
        case .fakeGifts:
            return MiscSection.fakeGifts.rawValue
        case .fakeStarsHeader, .fakeStars, .fakeStarsCount, .fakeStarsInfo:
            return MiscSection.fakeStars.rawValue
        case .callsHeader, .voiceTwin:
            return MiscSection.calls.rawValue
        case .autoReply:
            return MiscSection.autoReplyS.rawValue
        case .chatSummary:
            return MiscSection.chatSummaryS.rawValue
        case .antiSearchHeader, .antiSearch, .antiSearchInfo, .anonymousStickers, .anonymousStickersInfo,
             .profileLink, .profileLinkSelf, .profileLinkPeer, .profileLinkInfo, .phoneSpoof, .phoneSpoofNumber,
             .phoneSpoofRandomize, .phoneSpoofInfo, .mediaMetadata, .mediaMetadataInfo:
            return MiscSection.antiSearch.rawValue
        case .securityHeader, .linkProtection, .linkProtectionRedirects, .linkProtectionBlockFiles, .linkProtectionInfo:
            return MiscSection.security.rawValue
        }
    }

    var stableId: Int32 {
        switch self {
        case .premiumHeader:    return 0
        case .localPremium:     return 1
        case .premiumInfo:      return 2
        case .fakeGifts:        return 10
        case .fakeStarsHeader:  return 20
        case .fakeStars:        return 21
        case .fakeStarsCount:   return 22
        case .fakeStarsInfo:    return 23
        case .callsHeader:      return 24
        case .voiceTwin:        return 25
        case .autoReply:        return 26
        case .chatSummary:      return 27
        case .antiSearchHeader: return 30
        case .antiSearch:       return 31
        case .antiSearchInfo:   return 32
        case .anonymousStickers:return 33
        case .anonymousStickersInfo: return 34
        case .profileLink:      return 35
        case .profileLinkSelf:  return 36
        case .profileLinkPeer:  return 37
        case .profileLinkInfo:  return 38
        case .phoneSpoof:       return 39
        case .phoneSpoofNumber: return 40
        case .phoneSpoofRandomize: return 41
        case .phoneSpoofInfo:   return 42
        case .mediaMetadata:    return 43
        case .mediaMetadataInfo:return 44
        case .securityHeader:   return 50
        case .linkProtection:   return 51
        case .linkProtectionRedirects: return 52
        case .linkProtectionBlockFiles: return 53
        case .linkProtectionInfo: return 54
        }
    }

    static func < (lhs: MiscEntry, rhs: MiscEntry) -> Bool {
        return lhs.stableId < rhs.stableId
    }

    static func == (lhs: MiscEntry, rhs: MiscEntry) -> Bool {
        switch lhs {
        case let .premiumHeader(lt, ls):
            if case let .premiumHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .localPremium(lt, ls, lv):
            if case let .localPremium(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .premiumInfo(lt, ls):
            if case let .premiumInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .fakeGifts(lt, ls):
            if case let .fakeGifts(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .fakeStarsHeader(lt, ls):
            if case let .fakeStarsHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .fakeStars(lt, ls, lv):
            if case let .fakeStars(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .fakeStarsCount(lt, ls, lv):
            if case let .fakeStarsCount(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .fakeStarsInfo(lt, ls):
            if case let .fakeStarsInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .callsHeader(lt, ls):
            if case let .callsHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .voiceTwin(lt, ls):
            if case let .voiceTwin(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .autoReply(lt, ls, lv):
            if case let .autoReply(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .chatSummary(lt, ls, lv):
            if case let .chatSummary(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .antiSearchHeader(lt, ls):
            if case let .antiSearchHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .antiSearch(lt, ls, lv):
            if case let .antiSearch(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .antiSearchInfo(lt, ls):
            if case let .antiSearchInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .anonymousStickers(lt, ls, lv):
            if case let .anonymousStickers(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .anonymousStickersInfo(lt, ls):
            if case let .anonymousStickersInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .profileLink(lt, ls, lv):
            if case let .profileLink(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .profileLinkSelf(lt, ls, lv):
            if case let .profileLinkSelf(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .profileLinkPeer(lt, ls, lv):
            if case let .profileLinkPeer(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .profileLinkInfo(lt, ls):
            if case let .profileLinkInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .phoneSpoof(lt, ls, lv):
            if case let .phoneSpoof(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .phoneSpoofNumber(lt, ls, lv):
            if case let .phoneSpoofNumber(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .phoneSpoofRandomize(lt, ls):
            if case let .phoneSpoofRandomize(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .phoneSpoofInfo(lt, ls):
            if case let .phoneSpoofInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .mediaMetadata(lt, ls, lv):
            if case let .mediaMetadata(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .mediaMetadataInfo(lt, ls):
            if case let .mediaMetadataInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .securityHeader(lt, ls):
            if case let .securityHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .linkProtection(lt, ls, lv):
            if case let .linkProtection(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .linkProtectionRedirects(lt, ls, lv):
            if case let .linkProtectionRedirects(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .linkProtectionBlockFiles(lt, ls, lv):
            if case let .linkProtectionBlockFiles(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .linkProtectionInfo(lt, ls):
            if case let .linkProtectionInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        }
        return false
    }

    func item(presentationData: ItemListPresentationData, arguments: Any) -> ListViewItem {
        let args = arguments as! MiscArguments
        switch self {
        case let .premiumHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .localPremium(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setLocalPremium($0) })
        case let .premiumInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .fakeGifts(_, title):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: "", sectionId: section, style: .blocks, action: args.openFakeGifts)
        case let .fakeStarsHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .fakeStars(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setFakeStars($0) })
        case let .fakeStarsCount(_, placeholder, value):
            return ItemListSingleLineInputItem(presentationData: presentationData, title: NSAttributedString(string: ""), text: value, placeholder: placeholder, type: .number, sectionId: section, textUpdated: { text in args.setFakeStarsAmount(text) }, action: {})
        case let .fakeStarsInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .callsHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .voiceTwin(_, title):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: "", sectionId: section, style: .blocks, action: args.openVoiceTwin)
        case let .autoReply(_, title, _):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: "", sectionId: section, style: .blocks, action: args.openQuickReplies)
        case let .chatSummary(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setChatSummary($0) })
        case let .antiSearchHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .antiSearch(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setAntiSearch($0) })
        case let .antiSearchInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .anonymousStickers(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setAnonymousStickers($0) })
        case let .anonymousStickersInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .profileLink(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setProfileLink($0) })
        case let .profileLinkSelf(_, title, value):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: value, sectionId: section, style: .blocks, disclosureStyle: .none, action: args.selectProfileLinkSelf)
        case let .profileLinkPeer(_, title, value):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: value, sectionId: section, style: .blocks, action: args.selectProfileLinkPeer)
        case let .profileLinkInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .phoneSpoof(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setPhoneSpoof($0) })
        case let .phoneSpoofNumber(_, placeholder, value):
            return ItemListSingleLineInputItem(presentationData: presentationData, title: NSAttributedString(string: ""), text: value, placeholder: placeholder, sectionId: section, textUpdated: { text in args.setPhoneSpoofNumber(text) }, action: {})
        case let .phoneSpoofRandomize(_, title):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: "", sectionId: section, style: .blocks, disclosureStyle: .none, action: args.randomizePhoneSpoof)
        case let .phoneSpoofInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .mediaMetadata(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setMediaMetadata($0) })
        case let .mediaMetadataInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .securityHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .linkProtection(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setLinkProtection($0) })
        case let .linkProtectionRedirects(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setLinkProtectionRedirects($0) })
        case let .linkProtectionBlockFiles(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setLinkProtectionBlockFiles($0) })
        case let .linkProtectionInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        }
    }
}

private func miscEntries(state: MiscState, theme: PresentationTheme) -> [MiscEntry] {
    let isRu = AorusLang.current == .ru
    var entries: [MiscEntry] = []

    entries.append(.premiumHeader(theme, isRu ? "ПРЕМИУМ" : "PREMIUM"))
    entries.append(.localPremium(theme, isRu ? "Локальный премиум" : "Local Premium", state.localPremium))
    entries.append(.premiumInfo(theme, isRu
        ? "Включает Telegram Premium локально на этом устройстве — функции работают только у вас. Изменение полностью применяется после перезапуска приложения."
        : "Enables Telegram Premium locally on this device — the features work only for you. The change fully applies after restarting the app."))

    entries.append(.fakeGifts(theme, isRu ? "Фейковые подарки" : "Fake Gifts"))

    entries.append(.fakeStarsHeader(theme, isRu ? "ЗВЁЗДЫ" : "STARS"))
    entries.append(.fakeStars(theme, isRu ? "Фейковые звёзды" : "Fake Stars", state.fakeStars))
    // The amount row animates in/out with the toggle (the list uses animateChanges).
    if state.fakeStars {
        entries.append(.fakeStarsCount(theme, isRu ? "Количество" : "Amount", state.fakeStarsAmount))
    }
    entries.append(.fakeStarsInfo(theme, isRu
        ? "Показывает указанный баланс звёзд локально, только у вас. Реальные звёзды не создаются и не тратятся."
        : "Shows the entered Stars balance locally, only for you. No real stars are created or spent."))

    // Voice Twin (Calls) moved to the main AorusGram screen. Auto-Reply and
    // Chat Summary stay here as separate headerless blocks.
    entries.append(.autoReply(theme, isRu ? "Быстрые ответы" : "Quick Replies", state.autoReply))
    entries.append(.chatSummary(theme, isRu ? "Сводка чата" : "Chat Summary", state.chatSummary))

    // ANTI-SEARCH header intentionally removed — the block stays, just untitled.
    entries.append(.antiSearch(theme, isRu ? "АнтиПоиск" : "AntiSearch", state.antiSearch))
    entries.append(.antiSearchInfo(theme, isRu
        ? "Заменяет только визуально идентичные буквы, чтобы текст выглядел так же, но хуже находился поиском."
        : "Swaps only visually identical letters so the text looks the same but is harder to match in search."))
    entries.append(.anonymousStickers(theme, isRu ? "Анонимные стикеры" : "Anonymous Stickers", state.anonymousStickers))
    entries.append(.anonymousStickersInfo(theme, isRu
        ? "Отправляет ваши стикеры без ссылки на набор: клиент переупаковывает файл и убирает привязку к стикерпаку."
        : "Sends your stickers without a sticker-pack link: the client repacks the file and removes pack attribution."))
    entries.append(.profileLink(theme, isRu ? "Гиперссылка на профиль" : "Profile Link", state.profileLink))
    if state.profileLink {
        entries.append(.profileLinkSelf(theme, isRu ? "Мой профиль" : "My Profile", state.profileLinkTargetPeerId == 0 ? (isRu ? "Выбрано" : "Selected") : ""))
        entries.append(.profileLinkPeer(theme, isRu ? "Чужой профиль" : "Other Profile", state.profileLinkTargetPeerId == 0 ? (isRu ? "Выбрать" : "Choose") : state.profileLinkTargetName))
    }
    entries.append(.profileLinkInfo(theme, isRu
        ? "Делает весь текст ваших исходящих сообщений кликабельным упоминанием выбранного профиля."
        : "Makes the whole text of your outgoing messages a clickable mention of the selected profile."))
    entries.append(.phoneSpoof(theme, isRu ? "Подмена номера" : "Phone Spoofing", state.phoneSpoof))
    if state.phoneSpoof {
        entries.append(.phoneSpoofNumber(theme, isRu ? "Номер" : "Number", state.phoneSpoofNumber))
        entries.append(.phoneSpoofRandomize(theme, isRu ? "Рандомизировать" : "Randomize"))
    }
    entries.append(.phoneSpoofInfo(theme, isRu
        ? "Когда бот просит номер телефона, клиент отправляет защищённый номер вместо настоящего."
        : "When a bot asks for your phone number, the client sends a protected number instead of your real one."))
    entries.append(.mediaMetadata(theme, isRu ? "Метаданные медиа" : "Media Metadata", state.mediaMetadata))
    entries.append(.mediaMetadataInfo(theme, isRu
        ? "Добавляет пункт «Метаданные» в меню фото, видео и GIF и показывает доступные EXIF, GPS, камеру, контейнер и файл."
        : "Adds a Metadata item to photos, videos and GIFs and shows available EXIF, GPS, camera, container and file data."))

    entries.append(.securityHeader(theme, isRu ? "БЕЗОПАСНОСТЬ" : "SECURITY"))
    entries.append(.linkProtection(theme, isRu ? "Защита ссылок" : "Link Protection", state.linkProtection))
    if state.linkProtection {
        entries.append(.linkProtectionRedirects(theme, isRu ? "Проверять редиректы" : "Check Redirects", state.linkProtectionRedirects))
        entries.append(.linkProtectionBlockFiles(theme, isRu ? "Блокировать опасные файлы" : "Block Dangerous Files", state.linkProtectionBlockFiles))
    }
    entries.append(.linkProtectionInfo(theme, isRu
        ? "Предупреждает о подмене доменов, Punycode, сокращателях, редиректах, опасных схемах и файлах до открытия ссылки."
        : "Warns about spoofed domains, Punycode, shorteners, redirects, dangerous schemes and files before opening a link."))

    return entries
}

public func aorusMiscController(context: AccountContext) -> ViewController {
    let initialFakeStars = AorusFakeStarsStore.isEnabled
    let initialFakeStarsAmount = AorusFakeStarsStore.amount > 0 ? "\(AorusFakeStarsStore.amount)" : ""
    let initialProfileLinkTargetPeerId = Int64(UserDefaults.standard.string(forKey: "aorusgram_profile_link_target_peer_id") ?? "") ?? 0
    let initialState = MiscState(
        localPremium: AorusLocalPremium.isEnabled,
        fakeStars: initialFakeStars,
        fakeStarsAmount: initialFakeStarsAmount,
        autoReply: AorusGramManager.shared.autoReply,
        chatSummary: AorusGramManager.shared.chatSummary,
        antiSearch: AorusAntiSearchStore.isEnabled,
        anonymousStickers: UserDefaults.standard.bool(forKey: "aorusgram_anonymous_stickers_enabled"),
        profileLink: UserDefaults.standard.bool(forKey: "aorusgram_profile_link_enabled"),
        profileLinkTargetPeerId: initialProfileLinkTargetPeerId,
        profileLinkTargetName: UserDefaults.standard.string(forKey: "aorusgram_profile_link_target_name") ?? "",
        phoneSpoof: AorusPhoneSpoofStore.isEnabled,
        phoneSpoofNumber: AorusPhoneSpoofStore.ensureNumber(),
        mediaMetadata: UserDefaults.standard.bool(forKey: "aorusgram_media_metadata_enabled"),
        linkProtection: AorusLinkProtection.isEnabled,
        linkProtectionRedirects: AorusLinkProtection.checksRedirects,
        linkProtectionBlockFiles: AorusLinkProtection.blocksDangerousFiles
    )
    let statePromise = ValuePromise(initialState, ignoreRepeated: true)
    let stateValue = Atomic(value: initialState)
    let updateState: ((MiscState) -> MiscState) -> Void = { f in
        statePromise.set(stateValue.modify { f($0) })
    }

    weak var weakController: ItemListController?
    let actionsDisposable = DisposableSet()

    let arguments = MiscArguments(
        setLocalPremium: { value in
            AorusLocalPremium.setEnabled(value)
            updateState { current in
                var next = current
                next.localPremium = value
                return next
            }
            // The premium flag is read at launch / account registration, so the
            // change only fully applies after a restart — offer a native prompt.
            aorusPresentRestartNotice(context: context, controller: weakController)
        },
        openFakeGifts: {
            guard let controller = weakController,
                  let navigationController = controller.navigationController as? NavigationController else {
                return
            }
            navigationController.pushViewController(aorusFakeGiftsController(context: context))
        },
        setFakeStars: { value in
            AorusFakeStarsStore.setEnabled(value)
            updateState { current in
                var next = current
                next.fakeStars = value
                return next
            }
        },
        setFakeStarsAmount: { text in
            // Keep digits only; an empty field means zero.
            let digits = text.filter { $0.isNumber }
            let amount = Int64(digits) ?? 0
            AorusFakeStarsStore.setAmount(amount)
            updateState { current in
                var next = current
                next.fakeStarsAmount = digits
                return next
            }
        },
        openVoiceTwin: {
            guard let controller = weakController,
                  let navigationController = controller.navigationController as? NavigationController else {
                return
            }
            navigationController.pushViewController(voiceTwinController(context: context))
        },
        openQuickReplies: {
            guard let controller = weakController,
                  let navigationController = controller.navigationController as? NavigationController else {
                return
            }
            navigationController.pushViewController(aorusQuickRepliesController(context: context))
        },
        setAutoReply: { value in
            AorusGramManager.shared.autoReply = value
            updateState { current in
                var next = current
                next.autoReply = value
                return next
            }
        },
        setChatSummary: { value in
            AorusGramManager.shared.chatSummary = value
            updateState { current in
                var next = current
                next.chatSummary = value
                return next
            }
        },
        setAntiSearch: { value in
            AorusAntiSearchStore.setEnabled(value)
            updateState { current in
                var next = current
                next.antiSearch = value
                return next
            }
        },
        setAnonymousStickers: { value in
            UserDefaults.standard.set(value, forKey: "aorusgram_anonymous_stickers_enabled")
            updateState { current in
                var next = current
                next.anonymousStickers = value
                return next
            }
        },
        setProfileLink: { value in
            UserDefaults.standard.set(value, forKey: "aorusgram_profile_link_enabled")
            updateState { current in
                var next = current
                next.profileLink = value
                return next
            }
        },
        selectProfileLinkSelf: {
            UserDefaults.standard.removeObject(forKey: "aorusgram_profile_link_target_peer_id")
            UserDefaults.standard.removeObject(forKey: "aorusgram_profile_link_target_name")
            updateState { current in
                var next = current
                next.profileLinkTargetPeerId = 0
                next.profileLinkTargetName = ""
                return next
            }
        },
        selectProfileLinkPeer: {
            let selectionController = context.sharedContext.makeContactSelectionController(ContactSelectionControllerParams(
                context: context,
                autoDismiss: false,
                title: { strings in return strings.Contacts_Title }
            ))
            actionsDisposable.add((selectionController.result
            |> deliverOnMainQueue).start(next: { [weak selectionController] result in
                selectionController?.dismiss()
                if let (peers, _, _, _, _, _) = result, let first = peers.first, case let .peer(peer, _, _) = first {
                    let peerId = peer.id.toInt64()
                    let peerName = peer.compactDisplayTitle
                    UserDefaults.standard.set("\(peerId)", forKey: "aorusgram_profile_link_target_peer_id")
                    UserDefaults.standard.set(peerName, forKey: "aorusgram_profile_link_target_name")
                    updateState { current in
                        var next = current
                        next.profileLinkTargetPeerId = peerId
                        next.profileLinkTargetName = peerName
                        return next
                    }
                }
            }))
            weakController?.push(selectionController)
        },
        setPhoneSpoof: { value in
            AorusPhoneSpoofStore.setEnabled(value)
            let number = AorusPhoneSpoofStore.ensureNumber()
            updateState { current in
                var next = current
                next.phoneSpoof = value
                next.phoneSpoofNumber = number
                return next
            }
        },
        setPhoneSpoofNumber: { text in
            let number = AorusPhoneSpoofStore.setNumber(text)
            updateState { current in
                var next = current
                next.phoneSpoofNumber = number
                return next
            }
        },
        randomizePhoneSpoof: {
            let number = AorusPhoneSpoofStore.randomize()
            updateState { current in
                var next = current
                next.phoneSpoofNumber = number
                return next
            }
        },
        setMediaMetadata: { value in
            UserDefaults.standard.set(value, forKey: "aorusgram_media_metadata_enabled")
            updateState { current in
                var next = current
                next.mediaMetadata = value
                return next
            }
        },
        setLinkProtection: { value in
            AorusLinkProtection.setEnabled(value)
            updateState { current in
                var next = current
                next.linkProtection = value
                return next
            }
        },
        setLinkProtectionRedirects: { value in
            AorusLinkProtection.setChecksRedirects(value)
            updateState { current in
                var next = current
                next.linkProtectionRedirects = value
                return next
            }
        },
        setLinkProtectionBlockFiles: { value in
            AorusLinkProtection.setBlocksDangerousFiles(value)
            updateState { current in
                var next = current
                next.linkProtectionBlockFiles = value
                return next
            }
        }
    )

    let signal = statePromise.get()
        |> deliverOnMainQueue
        |> map { state -> (ItemListControllerState, (ItemListNodeState, Any)) in
            let presentationData = context.sharedContext.currentPresentationData.with { $0 }
            let entries = miscEntries(state: state, theme: presentationData.theme)
            let isRu = AorusLang.current == .ru
            let controllerState = ItemListControllerState(
                presentationData: ItemListPresentationData(presentationData),
                title: .text(isRu ? "Прочее" : "Other"),
                leftNavigationButton: nil,
                rightNavigationButton: nil,
                backNavigationButton: ItemListBackButton(title: presentationData.strings.Common_Back)
            )
            let listState = ItemListNodeState(
                presentationData: ItemListPresentationData(presentationData),
                entries: entries,
                style: .blocks
            )
            return (controllerState, (listState, arguments))
        }
        |> afterDisposed {
            actionsDisposable.dispose()
        }

    let controller = ItemListController(context: context, state: signal)
    weakController = controller
    return controller
}
