import UIKit
import Display

// Shared Telegram-style dark / AMOLED look for every subscription screen.
enum SubscriptionStyle {
    static let background     = UIColor.black                          // AMOLED
    static let card           = UIColor(white: 0.11, alpha: 1.0)       // ~#1C1C1E
    static let cardElevated   = UIColor(white: 0.17, alpha: 1.0)
    static let primaryText    = UIColor.white
    static let secondaryText  = UIColor(white: 0.62, alpha: 1.0)
    static let accent         = UIColor(red: 0x9B/255.0, green: 0x4D/255.0, blue: 0xFF/255.0, alpha: 1.0) // AorusGram neon violet
    static let success        = UIColor(red: 0x34/255.0, green: 0xC7/255.0, blue: 0x59/255.0, alpha: 1.0)
    static let destructive    = UIColor(red: 0xFF/255.0, green: 0x3B/255.0, blue: 0x30/255.0, alpha: 1.0)

    static func title(_ text: String) -> UILabel {
        let label = UILabel()
        label.text = text
        label.font = Font.bold(24)
        label.textColor = primaryText
        label.textAlignment = .center
        label.numberOfLines = 0
        label.adjustsFontForContentSizeCategory = true
        return label
    }

    static func body(_ text: String, color: UIColor? = nil, size: CGFloat = 16) -> UILabel {
        let label = UILabel()
        label.text = text
        label.font = Font.regular(size)
        label.textColor = color ?? secondaryText
        label.textAlignment = .center
        label.numberOfLines = 0
        return label
    }

    static func primaryButton(_ title: String) -> UIButton {
        let button = UIButton(type: .system)
        button.setTitle(title, for: .normal)
        button.titleLabel?.font = Font.semibold(17)
        button.setTitleColor(.white, for: .normal)
        button.backgroundColor = accent
        button.layer.cornerRadius = 12
        button.layer.cornerCurve = .continuous
        button.translatesAutoresizingMaskIntoConstraints = false
        button.heightAnchor.constraint(equalToConstant: 50).isActive = true
        return button
    }

    static func secondaryButton(_ title: String) -> UIButton {
        let button = UIButton(type: .system)
        button.setTitle(title, for: .normal)
        button.titleLabel?.font = Font.regular(16)
        button.setTitleColor(accent, for: .normal)
        button.translatesAutoresizingMaskIntoConstraints = false
        button.heightAnchor.constraint(equalToConstant: 44).isActive = true
        return button
    }

    // Fixed-size view centred inside a full-width container (for .fill stacks).
    static func centered(_ view: UIView, size: CGFloat) -> UIView {
        let container = UIView()
        view.translatesAutoresizingMaskIntoConstraints = false
        container.addSubview(view)
        NSLayoutConstraint.activate([
            view.centerXAnchor.constraint(equalTo: container.centerXAnchor),
            view.topAnchor.constraint(equalTo: container.topAnchor),
            view.bottomAnchor.constraint(equalTo: container.bottomAnchor),
            view.widthAnchor.constraint(equalToConstant: size),
            view.heightAnchor.constraint(equalToConstant: size),
        ])
        return container
    }

    // A left-aligned "✓ feature" row.
    static func featureRow(_ text: String) -> UIView {
        let check = UILabel()
        check.text = "✓"
        check.font = Font.bold(16)
        check.textColor = success
        check.setContentHuggingPriority(.required, for: .horizontal)

        let label = UILabel()
        label.text = text
        label.font = Font.regular(16)
        label.textColor = primaryText
        label.numberOfLines = 0

        let row = UIStackView(arrangedSubviews: [check, label])
        row.axis = .horizontal
        row.alignment = .firstBaseline
        row.spacing = 10
        return row
    }

    // A rounded info / price card holding arbitrary content.
    static func card(_ content: UIView, padding: CGFloat = 16) -> UIView {
        let cardView = UIView()
        cardView.backgroundColor = card
        cardView.layer.cornerRadius = 14
        cardView.layer.cornerCurve = .continuous
        content.translatesAutoresizingMaskIntoConstraints = false
        cardView.addSubview(content)
        NSLayoutConstraint.activate([
            content.topAnchor.constraint(equalTo: cardView.topAnchor, constant: padding),
            content.bottomAnchor.constraint(equalTo: cardView.bottomAnchor, constant: -padding),
            content.leadingAnchor.constraint(equalTo: cardView.leadingAnchor, constant: padding),
            content.trailingAnchor.constraint(equalTo: cardView.trailingAnchor, constant: -padding),
        ])
        return cardView
    }
}
