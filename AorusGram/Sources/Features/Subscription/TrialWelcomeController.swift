import UIKit

// First-launch trial welcome (status == not_started).
//
// IMPORTANT: the trial is NOT started automatically — only when the user taps
// "Активировать 3 дня". The coordinator (Phase 3) wires the closures to the
// license API and navigation.
final class TrialWelcomeController: SubscriptionBaseController {
    var onActivateTrial: (() -> Void)?
    var onHaveKey: (() -> Void)?

    private let primary = SubscriptionStyle.primaryButton(SubL10n.startTrial)
    private let secondary = SubscriptionStyle.secondaryButton(SubL10n.haveKey)
    private let spinner = UIActivityIndicatorView(style: .medium)

    override func viewDidLoad() {
        super.viewDidLoad()

        addContent(SubscriptionStyle.centered(SubscriptionDuckView(duck: .trial), size: 170))
        addSpacing(6)
        addContent(SubscriptionStyle.title(SubL10n.welcomeTitle))
        addContent(SubscriptionStyle.body(SubL10n.welcomeBody1))
        addContent(SubscriptionStyle.body(SubL10n.welcomeBody2,
                                          color: SubscriptionStyle.secondaryText, size: 14))

        primary.addTarget(self, action: #selector(activateTapped), for: .touchUpInside)
        secondary.addTarget(self, action: #selector(haveKeyTapped), for: .touchUpInside)
        addBottomButton(primary)
        addBottomButton(secondary)

        spinner.hidesWhenStopped = true
        spinner.color = .white
        spinner.translatesAutoresizingMaskIntoConstraints = false
        primary.addSubview(spinner)
        NSLayoutConstraint.activate([
            spinner.centerXAnchor.constraint(equalTo: primary.centerXAnchor),
            spinner.centerYAnchor.constraint(equalTo: primary.centerYAnchor),
        ])
    }

    func setLoading(_ loading: Bool) {
        if loading {
            spinner.startAnimating()
            primary.setTitle("", for: .normal)
            primary.isEnabled = false
            secondary.isEnabled = false
        } else {
            spinner.stopAnimating()
            primary.setTitle(SubL10n.startTrial, for: .normal)
            primary.isEnabled = true
            secondary.isEnabled = true
        }
    }

    @objc private func activateTapped() { onActivateTrial?() }
    @objc private func haveKeyTapped() { onHaveKey?() }
}
