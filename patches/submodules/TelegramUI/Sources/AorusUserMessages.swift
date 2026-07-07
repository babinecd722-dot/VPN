import Foundation
import UIKit
import SwiftSignalKit
import Postbox
import TelegramCore
import AccountContext
import Display

// AorusGram: a chat-like screen that shows all of one user's messages inside a group.
//
// Modeled on HashtagSearchGlobalChatContents — it feeds `searchMessages(fromId:)`
// results into a customChatContents chat, so every message (text / photo / video /
// voice / round video / file) renders with the real, native chat UI.
//
// It REUSES the existing `.hashTagSearch` kind on purpose: adding a new
// ChatCustomContentsKind case would have to be handled in dozens of exhaustive
// switches across the codebase. Instead the screen is recognised by its concrete
// type (`is AorusUserMessagesChatContents`) where we need to customise it — the
// title (the user's name) and the single "Go to message" long-press action.
public final class AorusUserMessagesChatContents: ChatCustomContentsProtocol {
    private final class Impl {
        let queue: Queue
        let context: AccountContext
        let groupPeerId: EnginePeer.Id
        let fromPeerId: EnginePeer.Id

        private var currentSearchState: SearchMessagesState?
        private(set) var mergedHistoryView: EngineRawMessageHistoryView?
        private var sourceHistoryView: EngineRawMessageHistoryView?
        private var historyViewDisposable: Disposable?
        let historyViewStream = ValuePipe<(EngineRawMessageHistoryView, EngineViewUpdateType)>()
        let isSearchingPromise = ValuePromise<Bool>(true)

        init(queue: Queue, context: AccountContext, groupPeerId: EnginePeer.Id, fromPeerId: EnginePeer.Id) {
            self.queue = queue
            self.context = context
            self.groupPeerId = groupPeerId
            self.fromPeerId = fromPeerId

            self.updateHistoryViewRequest(reload: false)
        }

        deinit {
            self.historyViewDisposable?.dispose()
        }

        private func searchSignal(state: SearchMessagesState?) -> Signal<(SearchMessagesResult, SearchMessagesState), NoError> {
            return self.context.engine.messages.searchMessages(location: .peer(peerId: self.groupPeerId, fromId: self.fromPeerId, tags: nil, reactions: nil, threadId: nil, minDate: nil, maxDate: nil), query: "", state: state)
        }

        private func makeHistoryView(_ result: SearchMessagesResult) -> EngineRawMessageHistoryView {
            return EngineRawMessageHistoryView(tag: nil, namespaces: .just(Set([Namespaces.Message.Cloud])), entries: result.messages.reversed().map { EngineRawMessageHistoryEntry(message: $0, isRead: false, location: nil, monthLocation: nil, attributes: EngineRawMutableMessageHistoryEntryAttributes(authorIsContact: false)) }, holeEarlier: !result.completed, holeLater: false, isLoading: false)
        }

        private func updateHistoryViewRequest(reload: Bool) {
            guard self.historyViewDisposable == nil || reload else {
                return
            }
            self.historyViewDisposable?.dispose()

            self.isSearchingPromise.set(true)
            self.historyViewDisposable = (self.searchSignal(state: nil)
            |> deliverOn(self.queue)).start(next: { [weak self] result in
                guard let self else {
                    return
                }
                self.sourceHistoryView = self.makeHistoryView(result.0)
                self.updateHistoryView(updateType: .Initial)

                Queue.mainQueue().async {
                    self.currentSearchState = result.1
                }

                self.historyViewDisposable?.dispose()
                self.historyViewDisposable = nil
                self.isSearchingPromise.set(false)
            })
        }

        private func updateHistoryView(updateType: EngineViewUpdateType) {
            var entries = self.sourceHistoryView?.entries ?? []
            entries.sort(by: { $0.message.index < $1.message.index })

            let mergedHistoryView = EngineRawMessageHistoryView(tag: nil, namespaces: .just(Set([Namespaces.Message.Cloud])), entries: entries, holeEarlier: self.sourceHistoryView?.holeEarlier ?? false, holeLater: false, isLoading: false)
            self.mergedHistoryView = mergedHistoryView

            self.historyViewStream.putNext((mergedHistoryView, updateType))
        }

        func loadMore() {
            guard self.historyViewDisposable == nil, let currentSearchState = self.currentSearchState, let sourceHistoryView = self.sourceHistoryView, sourceHistoryView.holeEarlier else {
                return
            }

            self.historyViewDisposable?.dispose()
            self.historyViewDisposable = (self.searchSignal(state: currentSearchState)
            |> deliverOn(self.queue)).startStrict(next: { [weak self] result in
                guard let self else {
                    return
                }
                self.sourceHistoryView = self.makeHistoryView(result.0)
                self.updateHistoryView(updateType: .FillHole)

                Queue.mainQueue().async {
                    self.currentSearchState = result.1
                }

                self.historyViewDisposable?.dispose()
                self.historyViewDisposable = nil
            })
        }
    }

    public var kind: ChatCustomContentsKind
    public let peerName: String

    public var historyView: Signal<(EngineRawMessageHistoryView, EngineViewUpdateType), NoError> {
        return self.impl.signalWith({ impl, subscriber in
            if let mergedHistoryView = impl.mergedHistoryView {
                subscriber.putNext((mergedHistoryView, .Initial))
            }
            return impl.historyViewStream.signal().start(next: subscriber.putNext)
        })
    }

    public var searching: Signal<Bool, NoError> {
        return self.impl.signalWith({ impl, subscriber in
            return impl.isSearchingPromise.get().start(next: subscriber.putNext)
        })
    }

    public var messageLimit: Int? {
        return nil
    }

    private let queue: Queue
    private let impl: QueueLocalObject<Impl>

    public init(context: AccountContext, groupPeerId: EnginePeer.Id, fromPeerId: EnginePeer.Id, peerName: String) {
        self.kind = .hashTagSearch(publicPosts: false)
        self.peerName = peerName

        let queue = Queue()
        self.queue = queue
        self.impl = QueueLocalObject(queue: queue, generate: {
            return Impl(queue: queue, context: context, groupPeerId: groupPeerId, fromPeerId: fromPeerId)
        })
    }

    public func enqueueMessages(messages: [EnqueueMessage]) {
    }

    public func deleteMessages(ids: [EngineMessage.Id]) {
    }

    public func editMessage(id: EngineMessage.Id, text: String, media: RequestEditMessageMedia, entities: TextEntitiesMessageAttribute?, webpagePreviewAttribute: WebpagePreviewMessageAttribute?, disableUrlPreview: Bool) {
    }

    public func quickReplyUpdateShortcut(value: String) {
    }

    public func businessLinkUpdate(message: String, entities: [MessageTextEntity], title: String?) {
    }

    public func loadMore() {
        self.impl.with { impl in
            impl.loadMore()
        }
    }

    public var hashtagSearchResultsUpdate: ((SearchMessagesResult, SearchMessagesState)) -> Void = { _ in }

    public func hashtagSearchUpdate(query: String) {
    }
}

// Opens the "user's messages in this group" screen, pushing a native chat controller
// whose contents are the user's filtered messages.
public func openAorusUserMessages(context: AccountContext, groupPeerId: EnginePeer.Id, author: Peer, navigationController: NavigationController) {
    let contents = AorusUserMessagesChatContents(context: context, groupPeerId: groupPeerId, fromPeerId: author.id, peerName: aorusUserMessagesPeerName(author))
    let controller = context.sharedContext.makeChatController(context: context, chatLocation: .customChatContents, subject: .customChatContents(contents: contents), botStart: nil, mode: .standard(.default), params: nil)
    navigationController.pushViewController(controller)
}

private func aorusUserMessagesPeerName(_ peer: Peer) -> String {
    if let user = peer as? TelegramUser {
        let name = [user.firstName, user.lastName].compactMap({ $0 }).joined(separator: " ").trimmingCharacters(in: .whitespacesAndNewlines)
        return name.isEmpty ? "User" : name
    } else if let channel = peer as? TelegramChannel {
        return channel.title
    } else if let group = peer as? TelegramGroup {
        return group.title
    }
    return "User"
}
