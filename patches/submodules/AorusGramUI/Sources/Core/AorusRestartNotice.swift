import Foundation
import UIKit
import Display
import TelegramPresentationData
import AccountContext
import UndoUI
import UserNotifications

// Native "restart required" notification, matching Telegram's own bottom info pill
// (the ⓘ pill with an action button). Used by toggles whose effect only takes hold
// when the app's low-level layer re-initialises — local premium and device spoof are
// read once at launch / first network init, so the change is only fully applied after
// a restart.
//
// Tapping "Перезапустить сейчас" performs a real close-and-reopen: it schedules a
// one-shot local notification a moment in the future and terminates the process. The
// app fully closes (forcing a clean re-init); a moment later the banner appears and a
// single tap relaunches the app. iOS has no public API to relaunch a process in place,
// so this is the closest thing to an automatic restart.
public func aorusPresentRestartNotice(context: AccountContext, controller: ViewController?) {
    guard let controller = controller else { return }
    let presentationData = context.sharedContext.currentPresentationData.with { $0 }
    let isRu = AorusLang.resolve(presentationData.strings.baseLanguageCode) == .ru
    let text = isRu ? "Необходим перезапуск" : "Restart required"
    let actionText = isRu ? "Перезапустить сейчас" : "Restart now"
    controller.present(
        UndoOverlayController(
            presentationData: presentationData,
            content: .info(title: nil, text: text, timeout: nil, customUndoText: actionText),
            elevatedLayout: false,
            position: .bottom,
            animateInAsReplacement: false,
            action: { action in
                if case .undo = action {
                    aorusRestartApp(isRu: isRu)
                }
                return true
            }
        ),
        in: .window(.root)
    )
}

// Close the app and arm a tap-to-reopen banner. The notification is scheduled slightly
// in the future and the process is terminated first, so the banner surfaces on the
// home/lock screen (foreground banners are suppressed by default) where one tap brings
// the freshly-launched app back.
private func aorusRestartApp(isRu: Bool) {
    let center = UNUserNotificationCenter.current()
    let content = UNMutableNotificationContent()
    content.title = "AorusGram"
    content.body = isRu ? "Нажмите, чтобы снова открыть AorusGram" : "Tap to reopen AorusGram"
    content.sound = .default
    let trigger = UNTimeIntervalNotificationTrigger(timeInterval: 1.0, repeats: false)
    let request = UNNotificationRequest(identifier: "aorusgram_restart_relaunch", content: content, trigger: trigger)
    // Request permission up-front; whether it is granted or not we still terminate so
    // the restart actually happens. If the user previously allowed notifications the
    // reopen banner appears; if not, the app simply closes and is reopened by hand.
    center.requestAuthorization(options: [.alert, .sound]) { _, _ in
        center.add(request) { _ in
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) {
                exit(0)
            }
        }
    }
}
