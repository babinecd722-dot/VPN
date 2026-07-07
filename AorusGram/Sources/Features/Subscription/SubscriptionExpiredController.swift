import UIKit
import Display

// Hard-lock screen (status == expired / banned / trial finished). Also reused for
// the offline "no connection" state. Root-swapped in by the gate so no Telegram UI
// is reachable behind it.
final class SubscriptionExpiredController: SubscriptionBaseController {
    var onBuy: (() -> Void)?
    var onEnterKey: (() -> Void)?

    // Optional overrides (banned / connection-error variants).
    var titleTextOverride: String?
    var bodyTextOverride: String?
    var primaryTitleOverride: String?
    var secondaryTitleOverride: String?
    var hidePriceCard = false
    var hideFootnote = false

    override func viewDidLoad() {
        super.viewDidLoad()

        addContent(SubscriptionStyle.centered(SubscriptionDuckView(duck: .expired), size: 180))
        addSpacing(6)
        addContent(SubscriptionStyle.title(titleTextOverride ?? SubL10n.expiredTitle))
        addContent(SubscriptionStyle.body(bodyTextOverride ?? SubL10n.expiredBody))

        if !hidePriceCard {
            let price = UILabel()
            price.text = SubL10n.price
            price.font = Font.bold(20)
            price.textColor = SubscriptionStyle.primaryText
            price.textAlignment = .center
            addSpacing(4)
            addContent(SubscriptionStyle.card(price))
        }

        if !hideFootnote {
            addContent(SubscriptionStyle.body(SubL10n.keyHint,
                                              color: SubscriptionStyle.secondaryText, size: 13))
        }

        let buy = SubscriptionStyle.primaryButton(primaryTitleOverride ?? SubL10n.buyKey)
        let enter = SubscriptionStyle.secondaryButton(secondaryTitleOverride ?? SubL10n.enterKey)
        buy.addTarget(self, action: #selector(buyTapped), for: .touchUpInside)
        enter.addTarget(self, action: #selector(enterTapped), for: .touchUpInside)
        addBottomButton(buy)
        addBottomButton(enter)
    }

    @objc private func buyTapped() { onBuy?() }
    @objc private func enterTapped() { onEnterKey?() }
}
