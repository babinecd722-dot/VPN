import Foundation
import UIKit
import Display
import SwiftSignalKit
import TelegramCore
import TelegramPresentationData
import ItemListUI
import PresentationDataUtils
import AccountContext

// MARK: - Fake gifts manager screen
//
// Lists the local-only gifts the user pinned to their own profile (stored in
// TelegramCore's AorusFakeGiftsStore). Nothing here is sent to the server. Gifts are
// added from another user's gift detail "..." menu ("Добавить в профиль"); tapping a
// gift opens its management screen (sender / date / comment / visibility / remove).

private func aorusGiftTitleAndNumber(_ gift: StarGift) -> (String, Int32?) {
    switch gift {
    case let .unique(uniqueGift):
        return (uniqueGift.title, uniqueGift.number)
    case let .generic(genericGift):
        return (genericGift.title ?? "Gift", nil)
    }
}

private enum FakeGiftsSection: Int32 {
    case toggle
    case list
}

private struct FakeGiftsState: Equatable {
    var enabled: Bool
    var revision: Int
}

private final class FakeGiftsArguments {
    let setEnabled: (Bool) -> Void
    let openGift: (AorusStoredGift) -> Void

    init(setEnabled: @escaping (Bool) -> Void, openGift: @escaping (AorusStoredGift) -> Void) {
        self.setEnabled = setEnabled
        self.openGift = openGift
    }
}

private enum FakeGiftsEntry: ItemListNodeEntry {
    case enable(PresentationTheme, String, Bool)
    case enableInfo(PresentationTheme, String)
    case listHeader(PresentationTheme, String)
    case empty(PresentationTheme, String)
    case gift(PresentationTheme, Int32, AorusStoredGift, String, String)

    var section: ItemListSectionId {
        switch self {
        case .enable, .enableInfo:
            return FakeGiftsSection.toggle.rawValue
        case .listHeader, .empty, .gift:
            return FakeGiftsSection.list.rawValue
        }
    }

    var stableId: Int32 {
        switch self {
        case .enable:     return 0
        case .enableInfo: return 1
        case .listHeader: return 2
        case .empty:      return 3
        case let .gift(_, index, _, _, _): return 100 + index
        }
    }

    static func < (lhs: FakeGiftsEntry, rhs: FakeGiftsEntry) -> Bool {
        return lhs.stableId < rhs.stableId
    }

    static func == (lhs: FakeGiftsEntry, rhs: FakeGiftsEntry) -> Bool {
        switch lhs {
        case let .enable(lt, ls, lv):
            if case let .enable(rt, rs, rv) = rhs { return lt === rt && ls == rs && lv == rv }
        case let .enableInfo(lt, ls):
            if case let .enableInfo(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .listHeader(lt, ls):
            if case let .listHeader(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .empty(lt, ls):
            if case let .empty(rt, rs) = rhs { return lt === rt && ls == rs }
        case let .gift(lt, li, lg, ltitle, llabel):
            if case let .gift(rt, ri, rg, rtitle, rlabel) = rhs { return lt === rt && li == ri && lg == rg && ltitle == rtitle && llabel == rlabel }
        }
        return false
    }

    func item(presentationData: ItemListPresentationData, arguments: Any) -> ListViewItem {
        let args = arguments as! FakeGiftsArguments
        switch self {
        case let .enable(_, title, value):
            return ItemListSwitchItem(presentationData: presentationData, title: title, value: value, sectionId: section, style: .blocks, updated: { args.setEnabled($0) })
        case let .enableInfo(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .listHeader(_, text):
            return ItemListSectionHeaderItem(presentationData: presentationData, text: text, sectionId: section)
        case let .empty(_, text):
            return ItemListTextItem(presentationData: presentationData, text: .plain(text), sectionId: section)
        case let .gift(_, _, stored, title, label):
            return ItemListDisclosureItem(presentationData: presentationData, title: title, label: label, sectionId: section, style: .blocks, action: { args.openGift(stored) })
        }
    }
}

// Holds the store-change observer so its lifetime is bound to the controller: when the
// controller (and the signal pipeline that retains this holder) is released, deinit
// removes the observer. No retain cycle — NotificationCenter owns the block, the block
// only touches the refresh closure, and the holder is what ties it to the screen.
private final class FakeGiftsObserverHolder {
    var token: NSObjectProtocol?
    deinit {
        if let token = self.token {
            NotificationCenter.default.removeObserver(token)
        }
    }
}

private func fakeGiftsEntries(state: FakeGiftsState, theme: PresentationTheme) -> [FakeGiftsEntry] {
    let isRu = AorusLang.current == .ru
    var entries: [FakeGiftsEntry] = []

    entries.append(.enable(theme, isRu ? "Включить фейковые подарки" : "Enable fake gifts", state.enabled))
    entries.append(.enableInfo(theme, isRu
        ? "Эти подарки видны только вам, локально. Ничего не отправляется на сервер."
        : "These gifts are visible only to you, locally. Nothing is sent to the server."))

    entries.append(.listHeader(theme, isRu ? "ДОБАВЛЕННЫЕ ПОДАРКИ" : "ADDED GIFTS"))
    let gifts = AorusFakeGiftsStore.all()
    if gifts.isEmpty {
        entries.append(.empty(theme, isRu
            ? "Нет локальных подарков. Откройте подарок другого пользователя и нажмите «Добавить в профиль»."
            : "No local gifts. Open another user's gift and tap \"Add to Profile\"."))
    } else {
        for (i, stored) in gifts.enumerated() {
            let title: String
            let label: String
            if let gift = stored.gift {
                let (t, number) = aorusGiftTitleAndNumber(gift)
                title = t
                label = number.flatMap { "#\($0)" } ?? ""
            } else {
                title = isRu ? "Подарок" : "Gift"
                label = ""
            }
            entries.append(.gift(theme, Int32(i), stored, title, label))
        }
    }

    return entries
}

public func aorusFakeGiftsController(context: AccountContext) -> ViewController {
    let initialState = FakeGiftsState(enabled: AorusFakeGiftsStore.isEnabled, revision: 0)
    let statePromise = ValuePromise(initialState, ignoreRepeated: true)
    let stateValue = Atomic(value: initialState)
    let updateState: ((FakeGiftsState) -> FakeGiftsState) -> Void = { f in
        statePromise.set(stateValue.modify { f($0) })
    }

    let refresh: () -> Void = {
        updateState { current in
            var next = current
            next.revision += 1
            next.enabled = AorusFakeGiftsStore.isEnabled
            return next
        }
    }

    // Rebuild the list whenever the store changes (e.g. a gift is transferred away from
    // the profile, sold, or its visibility toggled), so this screen always mirrors the
    // store live — not only on the next time it is opened.
    let observerHolder = FakeGiftsObserverHolder()
    observerHolder.token = NotificationCenter.default.addObserver(forName: AorusFakeGiftsStore.changedNotification, object: nil, queue: .main) { _ in
        refresh()
    }

    weak var weakController: ItemListController?

    let arguments = FakeGiftsArguments(
        setEnabled: { value in
            AorusFakeGiftsStore.setEnabled(value)
            updateState { current in
                var next = current
                next.enabled = value
                return next
            }
        },
        openGift: { stored in
            guard let controller = weakController,
                  let navigationController = controller.navigationController as? NavigationController else {
                return
            }
            navigationController.pushViewController(aorusFakeGiftManageController(context: context, stored: stored, onChanged: {
                refresh()
            }))
        }
    )

    let signal = statePromise.get()
        |> deliverOnMainQueue
        |> map { state -> (ItemListControllerState, (ItemListNodeState, Any)) in
            // Keep the store observer alive for as long as the screen's signal lives.
            let _ = observerHolder
            let presentationData = context.sharedContext.currentPresentationData.with { $0 }
            let entries = fakeGiftsEntries(state: state, theme: presentationData.theme)
            let isRu = AorusLang.current == .ru
            let controllerState = ItemListControllerState(
                presentationData: ItemListPresentationData(presentationData),
                title: .text(isRu ? "Фейковые подарки" : "Fake Gifts"),
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

    let controller = ItemListController(context: context, state: signal)
    weakController = controller
    return controller
}
