import Foundation
import UIKit
import Display
import Postbox
import TelegramCore
import SwiftSignalKit
import AccountContext

// AorusGram: export a chat's history to a clean, native-looking PDF and hand it to
// the system share sheet (Save to Files, send to a chat, AirDrop, …).
//
// Messages are read straight from the LOCAL postbox (no network round-trip), so the
// export contains whatever history is already on this device. Copy-protection is
// intentionally NOT honoured here — the messages are already stored locally and the
// user is exporting their own conversation (the client bypasses copy-protection
// globally anyway).
//
// Media: photos and video/GIF thumbnails that are already downloaded are embedded
// inline; everything else (voice, files, stickers, …) is shown as a labelled marker.
public enum AorusConversationExport {
    private static var isRu: Bool {
        return UserDefaults.standard.string(forKey: "aorusgram_lang") == "ru"
    }

    // Entry point invoked from the profile "…" menu item.
    public static func start(context: AccountContext, peer: EnginePeer, parentController: ViewController?) {
        let isRu = self.isRu
        let chatTitle = self.peerName(peer._asPeer()) ?? (isRu ? "Чат" : "Chat")
        let myName = isRu ? "Вы" : "You"
        let mediaBox = context.account.postbox.mediaBox

        let signal = context.account.postbox.aroundMessageHistoryViewForLocation(
            .peer(peerId: peer.id, threadId: nil),
            anchor: .upperBound,
            ignoreMessagesInTimestampRange: nil,
            ignoreMessageIds: Set(),
            count: 20000,
            fixedCombinedReadStates: nil,
            topTaggedMessageIdNamespaces: Set(),
            tag: nil,
            appendMessagesFromTheSameGroup: false,
            namespaces: .all,
            orderStatistics: []
        )
        |> take(1)
        |> deliverOnMainQueue

        let _ = signal.start(next: { viewData in
            let messages = viewData.0.entries.map { $0.message }.sorted(by: { lhs, rhs in
                if lhs.timestamp != rhs.timestamp {
                    return lhs.timestamp < rhs.timestamp
                }
                return lhs.id.id < rhs.id.id
            })
            self.render(messages: messages, mediaBox: mediaBox, chatTitle: chatTitle, myName: myName, isRu: isRu, parentController: parentController)
        })
    }

    // MARK: - Orchestration

    private static func render(messages: [Message], mediaBox: MediaBox, chatTitle: String, myName: String, isRu: Bool, parentController: ViewController?) {
        if messages.isEmpty {
            self.presentAlert(text: isRu ? "В этом чате нет сообщений для экспорта." : "There are no messages to export.", parentController: parentController, isRu: isRu)
            return
        }
        DispatchQueue.global(qos: .userInitiated).async {
            let data = self.makePDF(messages: messages, mediaBox: mediaBox, chatTitle: chatTitle, myName: myName, isRu: isRu)
            let url = URL(fileURLWithPath: NSTemporaryDirectory()).appendingPathComponent(self.fileName(chatTitle: chatTitle))
            do {
                try data.write(to: url, options: .atomic)
            } catch {
                DispatchQueue.main.async {
                    self.presentAlert(text: isRu ? "Не удалось создать файл." : "Could not create the file.", parentController: parentController, isRu: isRu)
                }
                return
            }
            DispatchQueue.main.async {
                self.share(url: url, parentController: parentController)
            }
        }
    }

    private static func share(url: URL, parentController: ViewController?) {
        guard let parentController = parentController else {
            return
        }
        let activity = UIActivityViewController(activityItems: [url], applicationActivities: nil)
        if let popover = activity.popoverPresentationController {
            popover.sourceView = parentController.view
            popover.sourceRect = CGRect(x: parentController.view.bounds.midX, y: parentController.view.bounds.midY, width: 1.0, height: 1.0)
            popover.permittedArrowDirections = []
        }
        parentController.present(activity, animated: true, completion: nil)
    }

    private static func presentAlert(text: String, parentController: ViewController?, isRu: Bool) {
        guard let parentController = parentController else {
            return
        }
        let alert = UIAlertController(title: nil, message: text, preferredStyle: .alert)
        alert.addAction(UIAlertAction(title: isRu ? "Готово" : "OK", style: .default, handler: nil))
        parentController.present(alert, animated: true, completion: nil)
    }

    // MARK: - Helpers

    private static func peerName(_ peer: Peer?) -> String? {
        guard let peer = peer else {
            return nil
        }
        if let user = peer as? TelegramUser {
            let name = [user.firstName, user.lastName].compactMap({ $0 }).joined(separator: " ").trimmingCharacters(in: .whitespacesAndNewlines)
            return name.isEmpty ? nil : name
        } else if let group = peer as? TelegramGroup {
            return group.title
        } else if let channel = peer as? TelegramChannel {
            return channel.title
        }
        return nil
    }

    private static func fileName(chatTitle: String) -> String {
        let df = DateFormatter()
        df.dateFormat = "yyyy-MM-dd"
        let dateStr = df.string(from: Date())
        let illegal = CharacterSet(charactersIn: "/\\:?%*|\"<>")
        let safe = chatTitle.components(separatedBy: illegal).joined(separator: " ").trimmingCharacters(in: .whitespacesAndNewlines)
        let base = safe.isEmpty ? "Chat" : safe
        return "AorusGram — \(base) \(dateStr).pdf"
    }

    // Returns an embeddable (already-downloaded) image and/or a short label for the
    // message's first media attachment. Both nil → no displayable media.
    private static func mediaInfo(_ message: Message, mediaBox: MediaBox, isRu: Bool) -> (image: UIImage?, label: String?) {
        for media in message.media {
            if let image = media as? TelegramMediaImage {
                if let img = self.cachedImage(image.representations, mediaBox: mediaBox) {
                    return (img, nil)
                }
                return (nil, isRu ? "🖼 Фото" : "🖼 Photo")
            } else if let file = media as? TelegramMediaFile {
                if file.isSticker || file.isAnimatedSticker || file.isVideoSticker {
                    return (nil, isRu ? "Стикер" : "Sticker")
                }
                if file.isVoice {
                    return (nil, isRu ? "🎤 Голосовое сообщение" : "🎤 Voice message")
                }
                if file.isInstantVideo {
                    return (self.cachedImage(file.previewRepresentations, mediaBox: mediaBox), isRu ? "🎥 Видеосообщение" : "🎥 Video message")
                }
                if file.isAnimated {
                    return (self.cachedImage(file.previewRepresentations, mediaBox: mediaBox), "GIF")
                }
                if file.isVideo {
                    return (self.cachedImage(file.previewRepresentations, mediaBox: mediaBox), isRu ? "🎬 Видео" : "🎬 Video")
                }
                if file.isMusic {
                    return (nil, "🎵 " + (file.fileName ?? (isRu ? "Аудио" : "Audio")))
                }
                return (nil, "📄 " + (file.fileName ?? (isRu ? "Файл" : "File")))
            }
        }
        return (nil, nil)
    }

    private static func cachedImage(_ representations: [TelegramMediaImageRepresentation], mediaBox: MediaBox) -> UIImage? {
        guard let representation = largestImageRepresentation(representations) else {
            return nil
        }
        guard let path = mediaBox.completedResourcePath(representation.resource) else {
            return nil
        }
        return UIImage(contentsOfFile: path)
    }

    // MARK: - PDF rendering

    private static func makePDF(messages: [Message], mediaBox: MediaBox, chatTitle: String, myName: String, isRu: Bool) -> Data {
        let pageWidth: CGFloat = 595.0
        let pageHeight: CGFloat = 842.0
        let pageRect = CGRect(x: 0.0, y: 0.0, width: pageWidth, height: pageHeight)
        let marginX: CGFloat = 36.0
        let marginTop: CGFloat = 48.0
        let marginBottom: CGFloat = 48.0
        let contentWidth = pageWidth - marginX * 2.0
        let bubbleMaxWidth = floor(contentWidth * 0.74)
        let bubblePad: CGFloat = 10.0
        let bubbleSpacing: CGFloat = 8.0
        let innerGap: CGFloat = 4.0
        let imageMaxHeight: CGFloat = 280.0
        let textMaxWidth = bubbleMaxWidth - bubblePad * 2.0

        let bodyFont = Font.regular(12.0)
        let authorFont = Font.bold(11.0)
        let labelFont = Font.regular(11.0)
        let timeFont = Font.regular(9.0)
        let dateFont = Font.medium(10.0)
        let titleFont = Font.bold(18.0)
        let subtitleFont = Font.regular(11.0)

        let incomingColor = UIColor(red: 0.93, green: 0.93, blue: 0.94, alpha: 1.0)
        let outgoingColor = UIColor(red: 0.0, green: 0.48, blue: 1.0, alpha: 1.0)
        let incomingTextColor = UIColor(white: 0.05, alpha: 1.0)
        let outgoingTextColor = UIColor.white
        let mutedDark = UIColor(white: 0.45, alpha: 1.0)
        let mutedLight = UIColor(white: 1.0, alpha: 0.75)

        let dayFormatter = DateFormatter()
        dayFormatter.locale = Locale(identifier: isRu ? "ru_RU" : "en_US")
        dayFormatter.dateFormat = "d MMMM yyyy"
        let timeFormatter = DateFormatter()
        timeFormatter.locale = Locale(identifier: isRu ? "ru_RU" : "en_US")
        timeFormatter.dateFormat = "HH:mm"

        let measureOptions: NSStringDrawingOptions = [.usesLineFragmentOrigin, .usesFontLeading]
        let bodyParagraph = NSMutableParagraphStyle()
        bodyParagraph.lineBreakMode = .byWordWrapping
        let timeParagraph = NSMutableParagraphStyle()
        timeParagraph.alignment = .right

        let renderer = UIGraphicsPDFRenderer(bounds: pageRect)
        return renderer.pdfData { ctx in
            ctx.beginPage()
            var y: CGFloat = marginTop

            let titleAttr = NSAttributedString(string: chatTitle, attributes: [.font: titleFont, .foregroundColor: UIColor.black])
            let titleBound = titleAttr.boundingRect(with: CGSize(width: contentWidth, height: 1000.0), options: measureOptions, context: nil)
            titleAttr.draw(with: CGRect(x: marginX, y: y, width: contentWidth, height: ceil(titleBound.height)), options: measureOptions, context: nil)
            y += ceil(titleBound.height) + 4.0

            let subtitleText: String
            if isRu {
                subtitleText = "Экспортировано \(dayFormatter.string(from: Date())) · сообщений: \(messages.count)"
            } else {
                subtitleText = "Exported \(dayFormatter.string(from: Date())) · \(messages.count) messages"
            }
            let subtitleAttr = NSAttributedString(string: subtitleText, attributes: [.font: subtitleFont, .foregroundColor: mutedDark])
            subtitleAttr.draw(with: CGRect(x: marginX, y: y, width: contentWidth, height: 20.0), options: measureOptions, context: nil)
            y += 26.0

            var lastDay: String?

            for message in messages {
                let bodyText = message.text
                let media = self.mediaInfo(message, mediaBox: mediaBox, isRu: isRu)
                if bodyText.isEmpty && media.image == nil && media.label == nil {
                    continue
                }

                let date = Date(timeIntervalSince1970: Double(message.timestamp))
                let isIncoming = message.flags.contains(.Incoming)
                let authorText = isIncoming ? (self.peerName(message.author) ?? chatTitle) : myName
                let timeText = timeFormatter.string(from: date)

                // Date separator.
                let dayStr = dayFormatter.string(from: date)
                if dayStr != lastDay {
                    lastDay = dayStr
                    let dateAttr = NSAttributedString(string: dayStr, attributes: [.font: dateFont, .foregroundColor: mutedDark])
                    let dateBound = dateAttr.boundingRect(with: CGSize(width: contentWidth, height: 100.0), options: measureOptions, context: nil)
                    let dateBlockHeight = ceil(dateBound.height) + 12.0
                    if y + dateBlockHeight > pageHeight - marginBottom {
                        ctx.beginPage()
                        y = marginTop
                    }
                    let dateX = marginX + (contentWidth - ceil(dateBound.width)) / 2.0
                    dateAttr.draw(with: CGRect(x: dateX, y: y + 6.0, width: ceil(dateBound.width), height: ceil(dateBound.height)), options: measureOptions, context: nil)
                    y += dateBlockHeight
                }

                // Components.
                let bodyColor = isIncoming ? incomingTextColor : outgoingTextColor
                let mutedColor = isIncoming ? mutedDark : mutedLight

                let authorAttr = NSAttributedString(string: authorText, attributes: [.font: authorFont, .foregroundColor: isIncoming ? outgoingColor : mutedLight])
                let authorBound = authorAttr.boundingRect(with: CGSize(width: textMaxWidth, height: 40.0), options: measureOptions, context: nil)
                let authorH = ceil(authorBound.height)

                // Embedded image sizing.
                var imageDrawWidth: CGFloat = 0.0
                var imageDrawHeight: CGFloat = 0.0
                if let img = media.image, img.size.width > 1.0, img.size.height > 1.0 {
                    var w = img.size.width
                    var h = img.size.height
                    let sW = textMaxWidth / w
                    if sW < 1.0 {
                        w *= sW
                        h *= sW
                    }
                    let sH = imageMaxHeight / h
                    if sH < 1.0 {
                        w *= sH
                        h *= sH
                    }
                    imageDrawWidth = floor(w)
                    imageDrawHeight = floor(h)
                }

                var labelBound = CGRect.zero
                if let label = media.label {
                    let labelAttr = NSAttributedString(string: label, attributes: [.font: labelFont, .foregroundColor: mutedColor])
                    labelBound = labelAttr.boundingRect(with: CGSize(width: textMaxWidth, height: 1000.0), options: measureOptions, context: nil)
                }
                let labelH = media.label != nil ? ceil(labelBound.height) : 0.0

                var bodyBound = CGRect.zero
                if !bodyText.isEmpty {
                    let bodyAttr = NSAttributedString(string: bodyText, attributes: [.font: bodyFont, .foregroundColor: bodyColor, .paragraphStyle: bodyParagraph])
                    bodyBound = bodyAttr.boundingRect(with: CGSize(width: textMaxWidth, height: .greatestFiniteMagnitude), options: measureOptions, context: nil)
                }
                let bodyH = bodyText.isEmpty ? 0.0 : ceil(bodyBound.height)

                let timeMeasureAttr = NSAttributedString(string: timeText, attributes: [.font: timeFont])
                let timeBound = timeMeasureAttr.boundingRect(with: CGSize(width: textMaxWidth, height: 20.0), options: measureOptions, context: nil)
                let timeH = ceil(timeBound.height)
                let timeColor = isIncoming ? mutedDark : mutedLight

                // Bubble geometry.
                let innerWidth = max(ceil(authorBound.width), max(imageDrawWidth, max(ceil(labelBound.width), max(ceil(bodyBound.width), ceil(timeBound.width)))))
                let bubbleWidth = min(bubbleMaxWidth, max(innerWidth, 40.0) + bubblePad * 2.0)

                var bubbleHeight = bubblePad + authorH
                if imageDrawHeight > 0.0 {
                    bubbleHeight += innerGap + imageDrawHeight
                }
                if labelH > 0.0 {
                    bubbleHeight += innerGap + labelH
                }
                if bodyH > 0.0 {
                    bubbleHeight += innerGap + bodyH
                }
                bubbleHeight += innerGap + timeH + bubblePad

                if y + bubbleHeight > pageHeight - marginBottom {
                    ctx.beginPage()
                    y = marginTop
                }

                let bubbleX = isIncoming ? marginX : (pageWidth - marginX - bubbleWidth)
                let bubbleRect = CGRect(x: bubbleX, y: y, width: bubbleWidth, height: bubbleHeight)
                let path = UIBezierPath(roundedRect: bubbleRect, cornerRadius: 12.0)
                (isIncoming ? incomingColor : outgoingColor).setFill()
                path.fill()

                let textX = bubbleX + bubblePad
                let innerW = bubbleWidth - bubblePad * 2.0
                var ty = y + bubblePad

                authorAttr.draw(with: CGRect(x: textX, y: ty, width: innerW, height: authorH), options: measureOptions, context: nil)
                ty += authorH

                if let img = media.image, imageDrawWidth > 0.0, imageDrawHeight > 0.0 {
                    ty += innerGap
                    let imageRect = CGRect(x: textX, y: ty, width: imageDrawWidth, height: imageDrawHeight)
                    let clip = UIBezierPath(roundedRect: imageRect, cornerRadius: 8.0)
                    ctx.cgContext.saveGState()
                    clip.addClip()
                    img.draw(in: imageRect)
                    ctx.cgContext.restoreGState()
                    ty += imageDrawHeight
                }

                if let label = media.label {
                    ty += innerGap
                    let labelAttr = NSAttributedString(string: label, attributes: [.font: labelFont, .foregroundColor: mutedColor])
                    labelAttr.draw(with: CGRect(x: textX, y: ty, width: innerW, height: labelH), options: measureOptions, context: nil)
                    ty += labelH
                }

                if !bodyText.isEmpty {
                    ty += innerGap
                    let bodyAttr = NSAttributedString(string: bodyText, attributes: [.font: bodyFont, .foregroundColor: bodyColor, .paragraphStyle: bodyParagraph])
                    bodyAttr.draw(with: CGRect(x: textX, y: ty, width: innerW, height: bodyH), options: measureOptions, context: nil)
                    ty += bodyH
                }

                ty += innerGap
                let timeDrawAttr = NSAttributedString(string: timeText, attributes: [.font: timeFont, .foregroundColor: timeColor, .paragraphStyle: timeParagraph])
                timeDrawAttr.draw(with: CGRect(x: textX, y: ty, width: innerW, height: timeH), options: measureOptions, context: nil)

                y += bubbleHeight + bubbleSpacing
            }
        }
    }
}
