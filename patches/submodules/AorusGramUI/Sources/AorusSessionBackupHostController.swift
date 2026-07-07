import Foundation
import UIKit
import SwiftUI
import Display
import AccountContext
import TelegramPresentationData

// Hosts the SwiftUI AorusSessionBackupView inside a Telegram Display navigation
// controller so it can be pushed from settings like any other screen. The Display
// navigation bar provides the title + back button; the SwiftUI List renders below
// it. The theme is applied by forcing the matching userInterfaceStyle and tint on
// the hosting controller, so SwiftUI's Color.accentColor / .secondary follow the
// AorusGram theme.
@available(iOS 13.0, *)
final class AorusSessionBackupHostController: ViewController {
    private let context: AccountContext
    private let presentationData: PresentationData
    private var hostingController: UIViewController?

    init(context: AccountContext) {
        self.context = context
        self.presentationData = context.sharedContext.currentPresentationData.with { $0 }
        super.init(navigationBarPresentationData: NavigationBarPresentationData(presentationData: self.presentationData))

        self.statusBar.statusBarStyle = self.presentationData.theme.rootController.statusBarStyle.style
        let isRu = AorusLang.resolve(self.presentationData.strings.baseLanguageCode) == .ru
        self.title = isRu ? "Бэкап аккаунтов" : "Account Backup"
        self.navigationItem.backBarButtonItem = UIBarButtonItem(title: self.presentationData.strings.Common_Back, style: .plain, target: nil, action: nil)
    }

    required init(coder aDecoder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    override func loadDisplayNode() {
        self.displayNode = ViewControllerTracingNode()
        self.displayNode.backgroundColor = self.presentationData.theme.list.blocksBackgroundColor

        let isRu = AorusLang.resolve(self.presentationData.strings.baseLanguageCode) == .ru
        let root = AorusSessionBackupView(context: self.context, isRu: isRu)
        let host = UIHostingController(rootView: root)
        host.overrideUserInterfaceStyle = self.presentationData.theme.overallDarkAppearance ? .dark : .light
        host.view.tintColor = self.presentationData.theme.list.itemAccentColor

        self.addChild(host)
        self.displayNode.view.addSubview(host.view)
        host.didMove(toParent: self)
        self.hostingController = host

        self.displayNodeDidLoad()
    }

    override func containerLayoutUpdated(_ layout: ContainerViewLayout, transition: ContainedViewLayoutTransition) {
        super.containerLayoutUpdated(layout, transition: transition)
        let navigationHeight = self.navigationLayout(layout: layout).navigationFrame.maxY
        self.hostingController?.view.frame = CGRect(x: 0.0, y: navigationHeight, width: layout.size.width, height: max(0.0, layout.size.height - navigationHeight))
    }
}
