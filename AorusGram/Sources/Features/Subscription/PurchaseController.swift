import UIKit
import Display

// Subscription management screen (opened from the entry banner for an ACTIVE user).
//
// Not a second "buy" wall: it shows the current subscription state — plan, status and
// how long it stays active — with a single primary action to extend it. The duck stays.
final class PurchaseController: SubscriptionBaseController {
    var onBuy: (() -> Void)?        // extend / renew (opens the bot)
    var onHaveKey: (() -> Void)?    // enter another key

    // Entrance animation runs once, on first appearance.
    private var didPlayEntrance = false

    override func viewDidLoad() {
        super.viewDidLoad()

        addContent(SubscriptionStyle.centered(SubscriptionDuckView(duck: .purchase), size: 150))
        addSpacing(2)
        addContent(SubscriptionStyle.title(SubL10n.premiumTitle))

        let snap = LicenseStore.shared.snapshot
        let status = LicenseStatus.parse(snap?.statusRaw ?? "")
        let isTrial = (status == .trialActive)

        // Status pill.
        let statusLabel = UILabel()
        statusLabel.text = isTrial ? SubL10n.statusTrial : SubL10n.statusActive
        statusLabel.font = Font.semibold(16)
        statusLabel.textColor = isTrial ? SubscriptionStyle.accent : SubscriptionStyle.success
        statusLabel.textAlignment = .center
        addContent(statusLabel)

        addSpacing(8)
        addContent(SubscriptionStyle.card(buildInfoStack(snap: snap, isTrial: isTrial)))

        let renew = SubscriptionStyle.primaryButton(SubL10n.renew)
        let key = SubscriptionStyle.secondaryButton(SubL10n.enterAnotherKey)
        renew.addTarget(self, action: #selector(buyTapped), for: .touchUpInside)
        key.addTarget(self, action: #selector(haveKeyTapped), for: .touchUpInside)
        addBottomButton(renew)
        addBottomButton(key)
    }

    // MARK: - Entrance animation
    //
    // Animates ONLY this controller's own content (hero duck, info rows, buttons).
    // It never touches the navigation bar, the window, or the root view, so it can't
    // disturb safe-area layout — the bug that previously pushed the close button into
    // the status bar. Initial hidden state is set in viewWillAppear (before the screen
    // is on-screen) so there is no first-frame flash.

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        guard !didPlayEntrance else { return }
        for v in contentStack.arrangedSubviews + buttonStack.arrangedSubviews {
            v.alpha = 0
            v.transform = CGAffineTransform(translationX: 0, y: 16)
        }
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        guard !didPlayEntrance else { return }
        didPlayEntrance = true

        // Hero duck: a soft spring scale-pop.
        if let hero = contentStack.arrangedSubviews.first {
            hero.transform = CGAffineTransform(scaleX: 0.7, y: 0.7)
            UIView.animate(withDuration: 0.6, delay: 0,
                           usingSpringWithDamping: 0.6, initialSpringVelocity: 0.6,
                           options: [.allowUserInteraction]) {
                hero.alpha = 1
                hero.transform = .identity
            }
        }

        // Title → status → card → buttons cascade up with a gentle fade, one after another.
        let cascade = Array(contentStack.arrangedSubviews.dropFirst()) + buttonStack.arrangedSubviews
        for (i, v) in cascade.enumerated() {
            UIView.animate(withDuration: 0.5, delay: 0.08 + Double(i) * 0.05,
                           usingSpringWithDamping: 0.85, initialSpringVelocity: 0.3,
                           options: [.allowUserInteraction]) {
                v.alpha = 1
                v.transform = .identity
            }
        }
    }

    private func buildInfoStack(snap: LicenseStore.Snapshot?, isTrial: Bool) -> UIView {
        var rows: [UIView] = []

        // Big "N days left" headline.
        if let days = snap?.daysLeft {
            let big = UILabel()
            big.text = SubL10n.remaining(days: max(days, 0))
            big.font = Font.bold(22)
            big.textColor = SubscriptionStyle.primaryText
            big.textAlignment = .center
            big.numberOfLines = 0
            rows.append(big)
        }

        // Active-until date row.
        if let until = snap?.activeUntil, until > 0 {
            let dateStr = PurchaseController.formatDate(until)
            let line = UILabel()
            line.text = isTrial ? SubL10n.trialUntil(dateStr) : SubL10n.activeUntil(dateStr)
            line.font = Font.regular(15)
            line.textColor = SubscriptionStyle.secondaryText
            line.textAlignment = .center
            line.numberOfLines = 0
            rows.append(line)
        }

        // Price hint.
        let price = UILabel()
        price.text = SubL10n.price
        price.font = Font.regular(14)
        price.textColor = SubscriptionStyle.secondaryText
        price.textAlignment = .center
        rows.append(price)

        let stack = UIStackView(arrangedSubviews: rows)
        stack.axis = .vertical
        stack.alignment = .fill
        stack.spacing = 8
        return stack
    }

    private static func formatDate(_ unix: Int64) -> String {
        let df = DateFormatter()
        df.locale = Locale(identifier: SubL10n.isRU ? "ru_RU" : "en_US")
        df.dateFormat = "dd.MM.yyyy"
        return df.string(from: Date(timeIntervalSince1970: TimeInterval(unix)))
    }

    @objc private func buyTapped() { onBuy?() }
    @objc private func haveKeyTapped() { onHaveKey?() }
}
