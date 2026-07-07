import UIKit
import Display

// MARK: - AorusCode first-run hint overlay
//
// Shown exactly once — the first time the user opens a chat (where the attachment
// button is visible) while AorusCode is enabled. It dims the screen with a soft
// blur, spotlights the attachment button with a pulsing accent ring, and floats a
// glass callout above it explaining how to open the AorusCode composer. Tapping
// anywhere (or a 10s timeout) dismisses it.
//
// Hosted inside the chat controller's own view (passed in as `hostView`) so it is
// removed automatically when the user leaves the chat — it never lingers over the
// chat list. The one-time gating lives in the caller (ChatTextInputPanelNode) via a
// UserDefaults flag.

public final class AorusCodeHintOverlay: UIView {

    private let accent = UIColor(red: 0.49, green: 0.36, blue: 0.96, alpha: 1.0)
    private let accentDark = UIColor(red: 0.36, green: 0.24, blue: 0.84, alpha: 1.0)
    private let ringLayer = CAShapeLayer()
    private let glowLayer = CAShapeLayer()
    private let bubble = UIView()
    private let blurView = UIVisualEffectView(effect: nil)
    private var dismissed = false

    public static func present(in hostView: UIView, targetRect: CGRect, russian: Bool) {
        let overlay = AorusCodeHintOverlay(frame: hostView.bounds, targetRect: targetRect, russian: russian)
        overlay.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        hostView.addSubview(overlay)
        overlay.animateIn()
    }

    private init(frame: CGRect, targetRect: CGRect, russian: Bool) {
        super.init(frame: frame)
        self.backgroundColor = .clear
        self.isUserInteractionEnabled = true

        let holeRadius = max(targetRect.width, targetRect.height) * 0.92
        let holeRect = CGRect(
            x: targetRect.midX - holeRadius,
            y: targetRect.midY - holeRadius,
            width: holeRadius * 2.0,
            height: holeRadius * 2.0)

        // ── Soft blur backdrop with a circular spotlight punched out around the button ──
        self.blurView.frame = self.bounds
        self.blurView.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        self.blurView.isUserInteractionEnabled = false
        self.addSubview(self.blurView)

        let dimmer = UIView(frame: self.bounds)
        dimmer.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        dimmer.backgroundColor = UIColor.black.withAlphaComponent(0.30)
        dimmer.isUserInteractionEnabled = false
        self.addSubview(dimmer)

        let cutout = UIBezierPath(rect: self.bounds)
        cutout.append(UIBezierPath(ovalIn: holeRect))
        let mask = CAShapeLayer()
        mask.path = cutout.cgPath
        mask.fillRule = .evenOdd
        self.blurView.layer.mask = mask
        let dimMask = CAShapeLayer()
        dimMask.path = cutout.cgPath
        dimMask.fillRule = .evenOdd
        dimmer.layer.mask = dimMask

        // ── Outer breathing glow + pulsing accent ring centred on the button ──
        let ringFrame = holeRect.insetBy(dx: -2.0, dy: -2.0)
        self.glowLayer.frame = ringFrame
        self.glowLayer.path = UIBezierPath(ovalIn: CGRect(origin: .zero, size: ringFrame.size)).cgPath
        self.glowLayer.fillColor = UIColor.clear.cgColor
        self.glowLayer.strokeColor = self.accent.withAlphaComponent(0.5).cgColor
        self.glowLayer.lineWidth = 8.0
        self.glowLayer.shadowColor = self.accent.cgColor
        self.glowLayer.shadowRadius = 10.0
        self.glowLayer.shadowOpacity = 0.9
        self.glowLayer.shadowOffset = .zero
        self.layer.addSublayer(self.glowLayer)

        self.ringLayer.frame = ringFrame
        self.ringLayer.path = UIBezierPath(ovalIn: CGRect(origin: .zero, size: ringFrame.size)).cgPath
        self.ringLayer.fillColor = UIColor.clear.cgColor
        self.ringLayer.strokeColor = UIColor.white.cgColor
        self.ringLayer.lineWidth = 2.5
        self.layer.addSublayer(self.ringLayer)

        // ── Glass callout above the button ──
        let lead = russian ? "Зажми скрепку, чтобы открыть " : "Hold the paperclip to open "
        let attr = NSMutableAttributedString(string: lead, attributes: [
            .foregroundColor: UIColor.white.withAlphaComponent(0.95),
            .font: Font.medium(15.5)
        ])
        attr.append(NSAttributedString(string: "AorusCode", attributes: [
            .foregroundColor: UIColor.white,
            .font: Font.heavy(15.5)
        ]))

        let label = UILabel()
        label.numberOfLines = 0
        label.attributedText = attr

        // Paperclip glyph badge on the left of the callout.
        let iconSize: CGFloat = 30.0
        let iconView = UIView()
        iconView.backgroundColor = UIColor.white.withAlphaComponent(0.18)
        iconView.layer.cornerRadius = iconSize / 2.0
        let icon = UIImageView()
        icon.contentMode = .center
        icon.tintColor = .white
        if #available(iOS 13.0, *) {
            let cfg = UIImage.SymbolConfiguration(pointSize: 15.0, weight: .semibold)
            icon.image = UIImage(systemName: "paperclip", withConfiguration: cfg)
        }

        let maxBubbleWidth = min(self.bounds.width - 32.0, 330.0)
        let inset = UIEdgeInsets(top: 13.0, left: 13.0, bottom: 13.0, right: 15.0)
        let gap: CGFloat = 10.0
        let textAvail = maxBubbleWidth - inset.left - inset.right - iconSize - gap
        let textSize = label.sizeThatFits(CGSize(width: textAvail, height: .greatestFiniteMagnitude))
        let contentHeight = max(iconSize, textSize.height)
        let bubbleSize = CGSize(
            width: inset.left + iconSize + gap + textSize.width + inset.right,
            height: contentHeight + inset.top + inset.bottom)

        let arrowHeight: CGFloat = 10.0
        var bubbleX = targetRect.midX - bubbleSize.width * 0.30
        bubbleX = max(12.0, min(bubbleX, self.bounds.width - 12.0 - bubbleSize.width))
        var bubbleY = targetRect.minY - arrowHeight - 14.0 - bubbleSize.height
        bubbleY = max(60.0, bubbleY)

        self.bubble.frame = CGRect(x: bubbleX, y: bubbleY, width: bubbleSize.width, height: bubbleSize.height)
        self.bubble.layer.cornerRadius = 18.0
        self.bubble.layer.cornerCurve = .continuous
        self.bubble.clipsToBounds = false

        // Accent gradient fill.
        let gradient = CAGradientLayer()
        gradient.frame = CGRect(origin: .zero, size: bubbleSize)
        gradient.colors = [self.accent.cgColor, self.accentDark.cgColor]
        gradient.startPoint = CGPoint(x: 0.0, y: 0.0)
        gradient.endPoint = CGPoint(x: 1.0, y: 1.0)
        gradient.cornerRadius = 18.0
        let gradientHost = UIView(frame: CGRect(origin: .zero, size: bubbleSize))
        gradientHost.layer.cornerRadius = 18.0
        gradientHost.layer.cornerCurve = .continuous
        gradientHost.clipsToBounds = true
        gradientHost.layer.addSublayer(gradient)
        gradientHost.autoresizingMask = [.flexibleWidth, .flexibleHeight]
        self.bubble.addSubview(gradientHost)

        self.bubble.layer.shadowColor = self.accentDark.cgColor
        self.bubble.layer.shadowOpacity = 0.45
        self.bubble.layer.shadowRadius = 16.0
        self.bubble.layer.shadowOffset = CGSize(width: 0.0, height: 8.0)
        self.addSubview(self.bubble)

        iconView.frame = CGRect(x: inset.left, y: (bubbleSize.height - iconSize) / 2.0, width: iconSize, height: iconSize)
        icon.frame = iconView.bounds
        iconView.addSubview(icon)
        self.bubble.addSubview(iconView)

        label.frame = CGRect(
            x: inset.left + iconSize + gap,
            y: (bubbleSize.height - textSize.height) / 2.0,
            width: textSize.width,
            height: textSize.height)
        self.bubble.addSubview(label)

        // ── Tail pointing from the callout toward the button ──
        let tailX = max(self.bubble.frame.minX + 22.0, min(targetRect.midX, self.bubble.frame.maxX - 22.0))
        let tailTop = self.bubble.frame.maxY - 1.0
        let tp = UIBezierPath()
        tp.move(to: CGPoint(x: tailX - 11.0, y: tailTop))
        tp.addLine(to: CGPoint(x: tailX + 11.0, y: tailTop))
        tp.addLine(to: CGPoint(x: tailX, y: tailTop + arrowHeight))
        tp.close()
        let tailLayer = CAShapeLayer()
        tailLayer.path = tp.cgPath
        tailLayer.fillColor = self.accentDark.cgColor
        self.layer.addSublayer(tailLayer)

        let tap = UITapGestureRecognizer(target: self, action: #selector(self.handleTap))
        self.addGestureRecognizer(tap)
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }

    private func animateIn() {
        self.alpha = 0.0
        self.bubble.transform = CGAffineTransform(scaleX: 0.8, y: 0.8).translatedBy(x: 0.0, y: 8.0)

        UIView.animate(withDuration: 0.32, delay: 0.0, options: [.curveEaseOut], animations: {
            self.alpha = 1.0
            self.blurView.effect = UIBlurEffect(style: .systemUltraThinMaterialDark)
        })
        UIView.animate(withDuration: 0.55, delay: 0.06, usingSpringWithDamping: 0.66, initialSpringVelocity: 0.5, options: [], animations: {
            self.bubble.transform = .identity
        })

        // Pulsing ring (scale + fade), repeating.
        let scale = CABasicAnimation(keyPath: "transform.scale")
        scale.fromValue = 1.0
        scale.toValue = 1.22
        let fade = CABasicAnimation(keyPath: "opacity")
        fade.fromValue = 0.95
        fade.toValue = 0.0
        let group = CAAnimationGroup()
        group.animations = [scale, fade]
        group.duration = 1.4
        group.repeatCount = .infinity
        group.timingFunction = CAMediaTimingFunction(name: .easeOut)
        self.ringLayer.add(group, forKey: "aorusPulse")

        // Gentle breathing glow.
        let breathe = CABasicAnimation(keyPath: "shadowOpacity")
        breathe.fromValue = 0.4
        breathe.toValue = 1.0
        breathe.duration = 1.1
        breathe.autoreverses = true
        breathe.repeatCount = .infinity
        breathe.timingFunction = CAMediaTimingFunction(name: .easeInEaseOut)
        self.glowLayer.add(breathe, forKey: "aorusGlow")

        DispatchQueue.main.asyncAfter(deadline: .now() + 10.0) { [weak self] in
            self?.dismiss()
        }
    }

    @objc private func handleTap() {
        self.dismiss()
    }

    private func dismiss() {
        if self.dismissed { return }
        self.dismissed = true
        UIView.animate(withDuration: 0.26, animations: {
            self.alpha = 0.0
            self.bubble.transform = CGAffineTransform(scaleX: 0.92, y: 0.92)
        }, completion: { _ in
            self.removeFromSuperview()
        })
    }
}
