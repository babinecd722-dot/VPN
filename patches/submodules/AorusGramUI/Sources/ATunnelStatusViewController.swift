import Foundation
import UIKit
import TelegramPresentationData
import AsyncDisplayKit
import SwiftSignalKit
import AnimatedStickerNode
import MediaResources
import Display

// MARK: - Data Model

private struct ATunnelServerEntry: Codable {
    let region: String
    let available: Bool
    let active: Bool
    let latencyMs: Double?
    let jitterMs: Double?
    let lossCount: Int?
}

private struct ATunnelDiag: Codable {
    let servers: [ATunnelServerEntry]
    let callTunnel: Bool
    let updatedAt: Double
}

// MARK: - Duck animation (TGS)
// Same pattern as SubscriptionDuckView — materialize base64 to tmp, feed LocalTgsSource.

private final class ATunnelLocalTgsSource: AnimatedStickerNodeSource {
    let fitzModifier: EmojiFitzModifier? = nil
    let isVideo: Bool = false
    private let path: String
    init(path: String) { self.path = path }
    func cachedDataPath(width: Int, height: Int) -> Signal<(String, Bool), NoError> { .never() }
    func directDataPath(attemptSynchronously: Bool) -> Signal<String?, NoError> { .single(path) }
}

private final class ATunnelDuckView: UIView {
    private let node = DefaultAnimatedStickerNodeImpl()

    init(renderSizePx: Int = 280) {
        super.init(frame: .zero)
        addSubview(node.view)
        if let path = ATunnelDuckView.materialize() {
            node.setup(
                source: ATunnelLocalTgsSource(path: path),
                width: renderSizePx, height: renderSizePx,
                playbackMode: .loop,
                mode: .direct(cachePathPrefix: nil)
            )
            node.visibility = true
        }
    }
    required init?(coder: NSCoder) { fatalError() }

    override func layoutSubviews() {
        super.layoutSubviews()
        node.frame = bounds
        node.updateLayout(size: bounds.size)
    }

    private static func materialize() -> String? {
        let dir = NSTemporaryDirectory() + "aorus_atunnel_anim"
        try? FileManager.default.createDirectory(atPath: dir, withIntermediateDirectories: true)
        let path = dir + "/duck.tgs"
        if FileManager.default.fileExists(atPath: path) { return path }
        guard let data = Data(base64Encoded: ATunnelDuckAsset.b64), !data.isEmpty else { return nil }
        try? data.write(to: URL(fileURLWithPath: path))
        return path
    }
}

// MARK: - Embedded duck asset
private enum ATunnelDuckAsset {
    static let b64 = "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"
}

// MARK: - Flag view (drawn stripes, no emoji)

private final class FlagView: UIView {
    init(regionCode: String) {
        super.init(frame: .zero)
        layer.cornerRadius = 3
        layer.masksToBounds = true
        setupStripes(for: regionCode.uppercased())
    }
    required init?(coder: NSCoder) { fatalError() }

    private func setupStripes(for code: String) {
        switch code {
        case "DE", "GERMANY", "ГЕРМАНИЯ":
            // Black / Red / Gold horizontal
            addStripe(UIColor(white: 0.07, alpha: 1), at: 0)
            addStripe(UIColor(red: 0.84, green: 0.00, blue: 0.12, alpha: 1), at: 1)
            addStripe(UIColor(red: 1.00, green: 0.80, blue: 0.00, alpha: 1), at: 2)
        case "FI", "FINLAND", "ФИНЛЯНДИЯ":
            // White + blue cross
            backgroundColor = .white
            let hBar = UIView()
            hBar.backgroundColor = UIColor(red: 0.00, green: 0.20, blue: 0.60, alpha: 1)
            hBar.translatesAutoresizingMaskIntoConstraints = false
            addSubview(hBar)
            let vBar = UIView()
            vBar.backgroundColor = UIColor(red: 0.00, green: 0.20, blue: 0.60, alpha: 1)
            vBar.translatesAutoresizingMaskIntoConstraints = false
            addSubview(vBar)
            NSLayoutConstraint.activate([
                hBar.leadingAnchor.constraint(equalTo: leadingAnchor),
                hBar.trailingAnchor.constraint(equalTo: trailingAnchor),
                hBar.centerYAnchor.constraint(equalTo: centerYAnchor),
                hBar.heightAnchor.constraint(equalTo: heightAnchor, multiplier: 0.33),
                vBar.topAnchor.constraint(equalTo: topAnchor),
                vBar.bottomAnchor.constraint(equalTo: bottomAnchor),
                vBar.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 6),
                vBar.widthAnchor.constraint(equalTo: heightAnchor, multiplier: 0.33),
            ])
        default:
            backgroundColor = UIColor(white: 0.3, alpha: 1)
        }
    }

    private func addStripe(_ color: UIColor, at index: Int) {
        let stripe = UIView()
        stripe.backgroundColor = color
        stripe.translatesAutoresizingMaskIntoConstraints = false
        addSubview(stripe)
        NSLayoutConstraint.activate([
            stripe.leadingAnchor.constraint(equalTo: leadingAnchor),
            stripe.trailingAnchor.constraint(equalTo: trailingAnchor),
            stripe.topAnchor.constraint(equalTo: topAnchor,
                constant: CGFloat(index) * (bounds.height > 0 ? bounds.height / 3 : 10)),
            stripe.heightAnchor.constraint(equalTo: heightAnchor, multiplier: 1.0 / 3.0),
        ])
    }

    // Stripes need height at layout time — redraw after bounds are set
    override func layoutSubviews() {
        super.layoutSubviews()
        // For DE stripes: reframe them by thirds
        let stripes = subviews.filter { $0.backgroundColor != .white }
        if stripes.count == 3 {
            let h = bounds.height / 3
            for (i, s) in stripes.enumerated() {
                s.frame = CGRect(x: 0, y: CGFloat(i) * h, width: bounds.width, height: h)
            }
        }
    }
}

// MARK: - Flow View (Phone → ATunnel → Telegram)

private enum ATunnelFlowState { case active, down, unknown }

private final class ATunnelFlowView: UIView {
    private let purple = UIColor(red: 0.48, green: 0.40, blue: 0.97, alpha: 1.0)

    private let leftIcon = UIImageView()
    private let centerBadge = UIView()
    private let centerLabel = UILabel()
    private let rightIcon = UIImageView()

    private let lineLeft  = CAShapeLayer()
    private let lineRight = CAShapeLayer()
    private let arrowLeft  = CAShapeLayer()
    private let arrowRight = CAShapeLayer()
    private let cross1 = CAShapeLayer()
    private let cross2 = CAShapeLayer()

    var state: ATunnelFlowState = .unknown { didSet { applyState() } }

    override init(frame: CGRect) { super.init(frame: frame); setup() }
    required init?(coder: NSCoder) { super.init(coder: coder); setup() }

    private func setup() {
        backgroundColor = .clear

        let cfgPhone = UIImage.SymbolConfiguration(pointSize: 22, weight: .medium)
        leftIcon.image = UIImage(systemName: "iphone", withConfiguration: cfgPhone)
        leftIcon.contentMode = .scaleAspectFit

        // Centre badge: purple circle with "A"
        centerBadge.layer.cornerRadius = 18
        centerBadge.backgroundColor = UIColor(red: 0.48, green: 0.40, blue: 0.97, alpha: 1)
        centerLabel.text = "A"
        centerLabel.font = Font.bold(14.0)
        centerLabel.textColor = .white
        centerLabel.textAlignment = .center
        centerBadge.addSubview(centerLabel)

        // Right: clean Telegram plane logo (single silhouette, no fold crease)
        rightIcon.image = ATunnelFlowView.telegramPlaneImage(box: 30)
        rightIcon.contentMode = .scaleAspectFit

        [leftIcon, centerBadge, rightIcon].forEach { addSubview($0) }

        for l in [lineLeft, lineRight] {
            l.fillColor = UIColor.clear.cgColor
            l.lineWidth = 1.5
            l.lineDashPattern = [6, 4]
            layer.addSublayer(l)
        }
        for l in [arrowLeft, arrowRight] {
            l.fillColor = UIColor.clear.cgColor
            l.lineWidth = 1.5
            l.lineCap = .round
            layer.addSublayer(l)
        }
        for l in [cross1, cross2] {
            l.fillColor = UIColor.clear.cgColor
            l.strokeColor = UIColor.systemRed.cgColor
            l.lineWidth = 2.5
            l.lineCap = .round
            l.isHidden = true
            layer.addSublayer(l)
        }
        applyState()
    }

    override func layoutSubviews() {
        super.layoutSubviews()
        let h = bounds.height
        let w = bounds.width
        let iconW: CGFloat = 32
        let badgeW: CGFloat = 36
        let cx = w / 2
        let midY = h / 2

        leftIcon.frame  = CGRect(x: 8, y: (h - iconW) / 2, width: iconW, height: iconW)
        centerBadge.frame = CGRect(x: cx - badgeW / 2, y: (h - badgeW) / 2, width: badgeW, height: badgeW)
        centerBadge.layer.cornerRadius = badgeW / 2
        centerLabel.frame = centerBadge.bounds
        rightIcon.frame = CGRect(x: w - 8 - iconW, y: (h - iconW) / 2, width: iconW, height: iconW)

        let lE = leftIcon.frame.maxX + 6
        let cL = centerBadge.frame.minX - 6
        let cR = centerBadge.frame.maxX + 6
        let rE = rightIcon.frame.minX - 6

        drawLine(lineLeft,  from: CGPoint(x: lE, y: midY), to: CGPoint(x: cL, y: midY))
        drawLine(lineRight, from: CGPoint(x: cR, y: midY), to: CGPoint(x: rE, y: midY))
        drawArrow(arrowLeft,  tip: CGPoint(x: cL, y: midY))
        drawArrow(arrowRight, tip: CGPoint(x: rE, y: midY))

        let r = centerBadge.frame
        let inset: CGFloat = 8
        let p = UIBezierPath(); p.move(to: CGPoint(x: r.minX + inset, y: r.minY + inset))
        p.addLine(to: CGPoint(x: r.maxX - inset, y: r.maxY - inset))
        cross1.path = p.cgPath
        let p2 = UIBezierPath(); p2.move(to: CGPoint(x: r.maxX - inset, y: r.minY + inset))
        p2.addLine(to: CGPoint(x: r.minX + inset, y: r.maxY - inset))
        cross2.path = p2.cgPath
    }

    private func drawLine(_ l: CAShapeLayer, from: CGPoint, to: CGPoint) {
        let p = UIBezierPath(); p.move(to: from); p.addLine(to: to); l.path = p.cgPath
    }

    private func drawArrow(_ l: CAShapeLayer, tip: CGPoint) {
        let s: CGFloat = 6
        let p = UIBezierPath()
        p.move(to: CGPoint(x: tip.x - s, y: tip.y - s / 2))
        p.addLine(to: tip)
        p.addLine(to: CGPoint(x: tip.x - s, y: tip.y + s / 2))
        l.path = p.cgPath
    }

    private func applyState() {
        lineLeft.removeAllAnimations(); lineRight.removeAllAnimations()
        switch state {
        case .active:
            let c = purple.cgColor
            [lineLeft, lineRight].forEach { $0.strokeColor = c; $0.lineDashPattern = [6, 4] }
            [arrowLeft, arrowRight].forEach { $0.strokeColor = c }
            leftIcon.tintColor = purple
            centerBadge.backgroundColor = purple
            rightIcon.tintColor = purple
            cross1.isHidden = true; cross2.isHidden = true
            [lineLeft, lineRight].forEach { addFlow($0) }
        case .down:
            let c = UIColor.systemRed.cgColor
            [lineLeft, lineRight].forEach { $0.strokeColor = c; $0.lineDashPattern = [6, 4] }
            [arrowLeft, arrowRight].forEach { $0.strokeColor = c }
            leftIcon.tintColor = .systemRed
            centerBadge.backgroundColor = .systemRed
            rightIcon.tintColor = .systemRed
            cross1.isHidden = false; cross2.isHidden = false
        case .unknown:
            let c = UIColor(white: 0.35, alpha: 1).cgColor
            [lineLeft, lineRight].forEach { $0.strokeColor = c; $0.lineDashPattern = [6, 4] }
            [arrowLeft, arrowRight].forEach { $0.strokeColor = c }
            leftIcon.tintColor = UIColor(white: 0.35, alpha: 1)
            centerBadge.backgroundColor = UIColor(white: 0.25, alpha: 1)
            rightIcon.tintColor = UIColor(white: 0.35, alpha: 1)
            cross1.isHidden = true; cross2.isHidden = true
        }
    }

    private func addFlow(_ l: CAShapeLayer) {
        let a = CABasicAnimation(keyPath: "lineDashPhase")
        a.fromValue = 0; a.toValue = -10
        a.duration = 0.6; a.repeatCount = .infinity; a.isRemovedOnCompletion = false
        l.add(a, forKey: "flow")
    }

    // Clean single-silhouette Telegram plane (Font Awesome "telegram-plane", viewBox
    // 448×512). One closed path — no interior fold line — rendered as a template image
    // so the surrounding tintColor drives its colour across flow states.
    static func telegramPlaneImage(box: CGFloat) -> UIImage {
        // Absolute control points derived from the canonical telegram-plane path.
        let path = UIBezierPath()
        path.move(to: CGPoint(x: 446.7, y: 98.6))
        path.addLine(to: CGPoint(x: 379.1, y: 417.4))
        path.addCurve(to: CGPoint(x: 341.8, y: 434.9),
                      controlPoint1: CGPoint(x: 374.0, y: 439.9), controlPoint2: CGPoint(x: 360.7, y: 445.5))
        path.addLine(to: CGPoint(x: 238.8, y: 359.0))
        path.addLine(to: CGPoint(x: 189.1, y: 406.8))
        path.addCurve(to: CGPoint(x: 168.4, y: 416.9),
                      controlPoint1: CGPoint(x: 183.6, y: 412.3), controlPoint2: CGPoint(x: 179.0, y: 416.9))
        path.addLine(to: CGPoint(x: 175.8, y: 312.0))
        path.addLine(to: CGPoint(x: 366.7, y: 139.5))
        path.addCurve(to: CGPoint(x: 353.8, y: 135.4),
                      controlPoint1: CGPoint(x: 375.0, y: 132.1), controlPoint2: CGPoint(x: 364.9, y: 128.0))
        path.addLine(to: CGPoint(x: 117.8, y: 284.0))
        path.addLine(to: CGPoint(x: 16.2, y: 252.2))
        path.addCurve(to: CGPoint(x: 20.8, y: 219.5),
                      controlPoint1: CGPoint(x: -5.9, y: 245.3), controlPoint2: CGPoint(x: -6.3, y: 230.1))
        path.addLine(to: CGPoint(x: 418.2, y: 66.4))
        path.addCurve(to: CGPoint(x: 446.7, y: 98.6),
                      controlPoint1: CGPoint(x: 436.6, y: 59.6), controlPoint2: CGPoint(x: 452.7, y: 70.8))
        path.close()

        // Fit the glyph's tight bounds into a square box (with a hair of padding).
        // boundingBoxOfPath is curve-accurate and excludes control points (some of
        // which sit at negative x), so the plane stays correctly centred.
        let bounds = path.cgPath.boundingBoxOfPath
        let inset: CGFloat = box * 0.07
        let avail = box - inset * 2
        let scale = min(avail / bounds.width, avail / bounds.height)
        let dx = (box - bounds.width * scale) / 2 - bounds.minX * scale
        let dy = (box - bounds.height * scale) / 2 - bounds.minY * scale
        var transform = CGAffineTransform(translationX: dx, y: dy)
        transform = transform.scaledBy(x: scale, y: scale)
        path.apply(transform)

        let renderer = UIGraphicsImageRenderer(size: CGSize(width: box, height: box), format: {
            let f = UIGraphicsImageRendererFormat.preferred()
            f.opaque = false
            return f
        }())
        let img = renderer.image { ctx in
            UIColor.black.setFill()
            path.fill()
        }
        return img.withRenderingMode(.alwaysTemplate)
    }
}

// MARK: - ATunnelStatusViewController

final class ATunnelStatusViewController: UIViewController {

    private let theme: PresentationTheme
    private let isRu: Bool
    private let purple = UIColor(red: 0.48, green: 0.40, blue: 0.97, alpha: 1.0)
    private let cardBg = UIColor(white: 0.10, alpha: 1.0)

    private let scrollView  = UIScrollView()
    private let contentView = UIView()

    private let duckView       = ATunnelDuckView(renderSizePx: 280)
    private let titleLabel     = UILabel()
    private let subtitleLabel  = UILabel()

    private let serversSectionLabel = UILabel()
    private let serverCardsStack    = UIStackView()

    private let callsSectionLabel = UILabel()
    private let callCard          = UIView()
    private let callPhoneIcon     = UIImageView()
    private let callTitleLabel    = UILabel()
    private let callStatusIcon    = UIImageView()

    private let updatedLabel = UILabel()
    private let diagButton   = UIButton(type: .system)

    private var diag: ATunnelDiag?
    private var refreshTimer: Foundation.Timer?
    private var serverCardViews: [UIView] = []
    private var didPlayEntrance = false

    init(theme: PresentationTheme, isRu: Bool) {
        self.theme = theme
        self.isRu  = isRu
        super.init(nibName: nil, bundle: nil)
        title = "ATunnel"
    }
    required init?(coder: NSCoder) { fatalError() }

    // MARK: - Lifecycle

    override func viewDidLoad() {
        super.viewDidLoad()
        overrideUserInterfaceStyle = .dark
        view.backgroundColor = .black
        setupNavigationBar()
        setupScrollView()
        setupHeader()
        setupServerSection()
        setupCallSection()
        setupFooter()
        loadData()
        buildServerCards()
        applyData()
    }

    override func viewWillAppear(_ animated: Bool) {
        super.viewWillAppear(animated)
        guard !didPlayEntrance else { return }
        let animatable: [UIView] = [duckView, titleLabel, subtitleLabel, serversSectionLabel,
                                    serverCardsStack, callsSectionLabel, callCard, updatedLabel]
        animatable.forEach {
            $0.alpha = 0
            $0.transform = CGAffineTransform(translationX: 0, y: 20)
        }
        // diagButton starts hidden; entrance only reveals it if it was already shown
        if !diagButton.isHidden {
            diagButton.alpha = 0
            diagButton.transform = CGAffineTransform(translationX: 0, y: 20)
        }
    }

    override func viewDidAppear(_ animated: Bool) {
        super.viewDidAppear(animated)
        startRefreshTimer()
        guard !didPlayEntrance else { return }
        didPlayEntrance = true

        // Duck pops in with spring
        UIView.animate(withDuration: 0.55, delay: 0,
                       usingSpringWithDamping: 0.65, initialSpringVelocity: 0.5,
                       options: .allowUserInteraction) {
            self.duckView.alpha = 1
            self.duckView.transform = .identity
        }
        // Content cascades up
        let rest: [UIView] = [titleLabel, subtitleLabel, serversSectionLabel,
                              serverCardsStack, callsSectionLabel, callCard, updatedLabel]
        for (i, v) in rest.enumerated() {
            UIView.animate(withDuration: 0.45, delay: 0.08 + Double(i) * 0.045,
                           usingSpringWithDamping: 0.82, initialSpringVelocity: 0.4,
                           options: .allowUserInteraction) {
                v.alpha = 1; v.transform = .identity
            }
        }
        // diagButton fades in last only if visible
        if !diagButton.isHidden {
            UIView.animate(withDuration: 0.4, delay: 0.08 + Double(rest.count) * 0.045,
                           options: .allowUserInteraction) {
                self.diagButton.alpha = 1; self.diagButton.transform = .identity
            }
        }
    }

    override func viewWillDisappear(_ animated: Bool) {
        super.viewWillDisappear(animated)
        stopRefreshTimer()
    }

    // MARK: - Nav bar

    private func setupNavigationBar() {
        navigationItem.leftBarButtonItem = UIBarButtonItem(
            barButtonSystemItem: .close, target: self, action: #selector(closePage))
        let appearance = UINavigationBarAppearance()
        appearance.configureWithTransparentBackground()
        appearance.titleTextAttributes = [.foregroundColor: UIColor.white]
        navigationItem.standardAppearance = appearance
        navigationItem.scrollEdgeAppearance = appearance
        navigationController?.navigationBar.tintColor = purple
    }

    @objc private func closePage() { dismiss(animated: true) }

    // MARK: - Layout

    private func setupScrollView() {
        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.alwaysBounceVertical = true
        scrollView.showsVerticalScrollIndicator = false
        view.addSubview(scrollView)
        NSLayoutConstraint.activate([
            scrollView.topAnchor.constraint(equalTo: view.topAnchor),
            scrollView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            scrollView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            scrollView.bottomAnchor.constraint(equalTo: view.bottomAnchor),
        ])
        contentView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.addSubview(contentView)
        NSLayoutConstraint.activate([
            contentView.topAnchor.constraint(equalTo: scrollView.topAnchor),
            contentView.leadingAnchor.constraint(equalTo: scrollView.leadingAnchor),
            contentView.trailingAnchor.constraint(equalTo: scrollView.trailingAnchor),
            contentView.bottomAnchor.constraint(equalTo: scrollView.bottomAnchor),
            contentView.widthAnchor.constraint(equalTo: scrollView.widthAnchor),
        ])
    }

    private func setupHeader() {
        // Duck
        duckView.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(duckView)

        // Title
        titleLabel.text = "ATunnel"
        titleLabel.font = Font.bold(26.0)
        titleLabel.textColor = .white
        titleLabel.textAlignment = .center
        titleLabel.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(titleLabel)

        // Subtitle
        subtitleLabel.text = isRu ? "Скоростная безопасная маршрутизация" : "Fast secure routing"
        subtitleLabel.font = Font.regular(15.0)
        subtitleLabel.textColor = UIColor(white: 0.6, alpha: 1)
        subtitleLabel.textAlignment = .center
        subtitleLabel.numberOfLines = 2
        subtitleLabel.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(subtitleLabel)

        NSLayoutConstraint.activate([
            duckView.topAnchor.constraint(equalTo: contentView.topAnchor, constant: 16),
            duckView.centerXAnchor.constraint(equalTo: contentView.centerXAnchor),
            duckView.widthAnchor.constraint(equalToConstant: 140),
            duckView.heightAnchor.constraint(equalToConstant: 140),

            titleLabel.topAnchor.constraint(equalTo: duckView.bottomAnchor, constant: 12),
            titleLabel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 24),
            titleLabel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -24),

            subtitleLabel.topAnchor.constraint(equalTo: titleLabel.bottomAnchor, constant: 6),
            subtitleLabel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 24),
            subtitleLabel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -24),
        ])
    }

    private func setupServerSection() {
        serversSectionLabel.text = isRu ? "СЕРВЕРЫ" : "SERVERS"
        serversSectionLabel.font = Font.semibold(12.0)
        serversSectionLabel.textColor = UIColor(white: 0.45, alpha: 1)
        serversSectionLabel.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(serversSectionLabel)

        serverCardsStack.axis = .vertical
        serverCardsStack.spacing = 10
        serverCardsStack.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(serverCardsStack)

        NSLayoutConstraint.activate([
            serversSectionLabel.topAnchor.constraint(equalTo: subtitleLabel.bottomAnchor, constant: 28),
            serversSectionLabel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 20),
            serversSectionLabel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -20),

            serverCardsStack.topAnchor.constraint(equalTo: serversSectionLabel.bottomAnchor, constant: 8),
            serverCardsStack.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 16),
            serverCardsStack.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -16),
        ])
    }

    private func setupCallSection() {
        callsSectionLabel.text = isRu ? "ЗВОНКИ" : "CALLS"
        callsSectionLabel.font = Font.semibold(12.0)
        callsSectionLabel.textColor = UIColor(white: 0.45, alpha: 1)
        callsSectionLabel.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(callsSectionLabel)

        callCard.backgroundColor = cardBg
        callCard.layer.cornerRadius = 14
        callCard.layer.cornerCurve = .continuous
        callCard.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(callCard)

        let cfgPhone = UIImage.SymbolConfiguration(pointSize: 20, weight: .medium)
        callPhoneIcon.image = UIImage(systemName: "phone.fill", withConfiguration: cfgPhone)
        callPhoneIcon.tintColor = purple
        callPhoneIcon.contentMode = .scaleAspectFit
        callPhoneIcon.translatesAutoresizingMaskIntoConstraints = false
        callCard.addSubview(callPhoneIcon)

        callTitleLabel.text = isRu ? "Маршрутизация звонков" : "Call routing"
        callTitleLabel.font = Font.medium(15.0)
        callTitleLabel.textColor = .white
        callTitleLabel.translatesAutoresizingMaskIntoConstraints = false
        callCard.addSubview(callTitleLabel)

        let cfgStatus = UIImage.SymbolConfiguration(pointSize: 22, weight: .medium)
        callStatusIcon.image = UIImage(systemName: "checkmark.circle.fill", withConfiguration: cfgStatus)
        callStatusIcon.tintColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
        callStatusIcon.contentMode = .scaleAspectFit
        callStatusIcon.translatesAutoresizingMaskIntoConstraints = false
        callCard.addSubview(callStatusIcon)

        NSLayoutConstraint.activate([
            callsSectionLabel.topAnchor.constraint(equalTo: serverCardsStack.bottomAnchor, constant: 28),
            callsSectionLabel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 20),
            callsSectionLabel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -20),

            callCard.topAnchor.constraint(equalTo: callsSectionLabel.bottomAnchor, constant: 8),
            callCard.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 16),
            callCard.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -16),
            callCard.heightAnchor.constraint(equalToConstant: 56),

            callPhoneIcon.leadingAnchor.constraint(equalTo: callCard.leadingAnchor, constant: 16),
            callPhoneIcon.centerYAnchor.constraint(equalTo: callCard.centerYAnchor),
            callPhoneIcon.widthAnchor.constraint(equalToConstant: 26),
            callPhoneIcon.heightAnchor.constraint(equalToConstant: 26),

            callTitleLabel.leadingAnchor.constraint(equalTo: callPhoneIcon.trailingAnchor, constant: 12),
            callTitleLabel.centerYAnchor.constraint(equalTo: callCard.centerYAnchor),
            callTitleLabel.trailingAnchor.constraint(equalTo: callStatusIcon.leadingAnchor, constant: -8),

            callStatusIcon.trailingAnchor.constraint(equalTo: callCard.trailingAnchor, constant: -16),
            callStatusIcon.centerYAnchor.constraint(equalTo: callCard.centerYAnchor),
            callStatusIcon.widthAnchor.constraint(equalToConstant: 26),
            callStatusIcon.heightAnchor.constraint(equalToConstant: 26),
        ])
    }

    private func setupFooter() {
        updatedLabel.font = Font.regular(13.0)
        updatedLabel.textColor = UIColor(white: 0.4, alpha: 1)
        updatedLabel.textAlignment = .center
        updatedLabel.translatesAutoresizingMaskIntoConstraints = false
        contentView.addSubview(updatedLabel)

        diagButton.setTitle(isRu ? "Запустить диагностику" : "Run diagnostics", for: .normal)
        diagButton.titleLabel?.font = Font.semibold(16.0)
        diagButton.setTitleColor(purple, for: .normal)
        diagButton.backgroundColor = purple.withAlphaComponent(0.15)
        diagButton.layer.cornerRadius = 14
        diagButton.layer.cornerCurve = .continuous
        diagButton.translatesAutoresizingMaskIntoConstraints = false
        diagButton.addTarget(self, action: #selector(diagButtonTapped), for: .touchUpInside)
        contentView.addSubview(diagButton)

        NSLayoutConstraint.activate([
            updatedLabel.topAnchor.constraint(equalTo: callCard.bottomAnchor, constant: 20),
            updatedLabel.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 24),
            updatedLabel.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -24),

            diagButton.topAnchor.constraint(equalTo: updatedLabel.bottomAnchor, constant: 12),
            diagButton.leadingAnchor.constraint(equalTo: contentView.leadingAnchor, constant: 16),
            diagButton.trailingAnchor.constraint(equalTo: contentView.trailingAnchor, constant: -16),
            diagButton.heightAnchor.constraint(equalToConstant: 50),
            diagButton.bottomAnchor.constraint(equalTo: contentView.bottomAnchor, constant: -40),
        ])
    }

    // MARK: - Data

    private func loadData() {
        guard let s = UserDefaults.standard.string(forKey: "aorusgram_atunnel_status"),
              let d = s.data(using: .utf8) else { diag = nil; return }
        diag = try? JSONDecoder().decode(ATunnelDiag.self, from: d)
    }

    private func buildServerCards() {
        serverCardsStack.arrangedSubviews.forEach { $0.removeFromSuperview() }
        serverCardViews.removeAll()
        guard let servers = diag?.servers else { return }
        for server in servers {
            let card = makeServerCard(for: server)
            serverCardsStack.addArrangedSubview(card)
            serverCardViews.append(card)
        }
    }

    private func applyData() {
        applyCallCard()
        applyUpdatedLabel()
        applyDiagButton()
    }

    private func applyCallCard() {
        let on = diag?.callTunnel ?? false
        let cfg = UIImage.SymbolConfiguration(pointSize: 22, weight: .medium)
        if on {
            callPhoneIcon.tintColor = purple
            callStatusIcon.image = UIImage(systemName: "checkmark.circle.fill", withConfiguration: cfg)
            callStatusIcon.tintColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
        } else {
            callPhoneIcon.tintColor = UIColor(white: 0.35, alpha: 1)
            callStatusIcon.image = UIImage(systemName: "minus.circle", withConfiguration: cfg)
            callStatusIcon.tintColor = UIColor(white: 0.35, alpha: 1)
        }
    }

    private func applyUpdatedLabel() {
        guard let d = diag else { updatedLabel.text = isRu ? "Обновлено: —" : "Updated: —"; return }
        let e = Date().timeIntervalSince1970 - d.updatedAt
        let t: String
        if e < 10       { t = isRu ? "только что" : "just now" }
        else if e < 60  { t = isRu ? "\(Int(e))с назад" : "\(Int(e))s ago" }
        else            { t = isRu ? "\(Int(e/60))м назад" : "\(Int(e/60))m ago" }
        updatedLabel.text = (isRu ? "Обновлено: " : "Updated: ") + t
    }

    private func applyDiagButton() {
        let anyDown = diag?.servers.contains(where: { !$0.available }) ?? false
        let wasHidden = diagButton.isHidden
        diagButton.isHidden = !anyDown
        if wasHidden && anyDown {
            diagButton.alpha = 0
            UIView.animate(withDuration: 0.35) { self.diagButton.alpha = 1 }
        }
    }

    // MARK: - Server card

    private func makeServerCard(for server: ATunnelServerEntry) -> UIView {
        let card = UIView()
        card.backgroundColor = cardBg
        card.layer.cornerRadius = 16
        card.layer.cornerCurve = .continuous

        // Flag
        let flag = FlagView(regionCode: server.region)
        flag.translatesAutoresizingMaskIntoConstraints = false

        // Region full name
        let regionLabel = UILabel()
        regionLabel.text = localizedRegion(server.region)
        regionLabel.font = Font.semibold(16.0)
        regionLabel.textColor = .white
        regionLabel.translatesAutoresizingMaskIntoConstraints = false

        // Status badge
        let badge = makeBadge(for: server)
        badge.translatesAutoresizingMaskIntoConstraints = false

        // Flow
        let flow = ATunnelFlowView()
        flow.state = server.available ? .active : .down
        flow.translatesAutoresizingMaskIntoConstraints = false

        // Detail
        let detail = UILabel()
        detail.font = Font.regular(13.0)
        detail.translatesAutoresizingMaskIntoConstraints = false
        if server.available, let lat = server.latencyMs, let jit = server.jitterMs {
            detail.textColor = UIColor(white: 0.55, alpha: 1)
            let ms  = isRu ? "мс" : "ms"
            let jw  = isRu ? "джиттер" : "jitter"
            detail.text = "⚡ \(Int(lat)) \(ms) · \(jw) \(Int(jit)) \(ms)"
        } else {
            detail.textColor = .systemRed
            detail.text = isRu ? "Сервер недоступен" : "Server unavailable"
        }

        [flag, regionLabel, badge, flow, detail].forEach { card.addSubview($0) }

        NSLayoutConstraint.activate([
            card.heightAnchor.constraint(equalToConstant: 148),

            flag.leadingAnchor.constraint(equalTo: card.leadingAnchor, constant: 16),
            flag.topAnchor.constraint(equalTo: card.topAnchor, constant: 16),
            flag.widthAnchor.constraint(equalToConstant: 28),
            flag.heightAnchor.constraint(equalToConstant: 20),

            regionLabel.centerYAnchor.constraint(equalTo: flag.centerYAnchor),
            regionLabel.leadingAnchor.constraint(equalTo: flag.trailingAnchor, constant: 10),
            regionLabel.trailingAnchor.constraint(lessThanOrEqualTo: badge.leadingAnchor, constant: -8),

            badge.centerYAnchor.constraint(equalTo: flag.centerYAnchor),
            badge.trailingAnchor.constraint(equalTo: card.trailingAnchor, constant: -16),

            flow.topAnchor.constraint(equalTo: flag.bottomAnchor, constant: 8),
            flow.leadingAnchor.constraint(equalTo: card.leadingAnchor, constant: 16),
            flow.trailingAnchor.constraint(equalTo: card.trailingAnchor, constant: -16),
            flow.heightAnchor.constraint(equalToConstant: 52),

            detail.topAnchor.constraint(equalTo: flow.bottomAnchor, constant: 6),
            detail.leadingAnchor.constraint(equalTo: card.leadingAnchor, constant: 16),
            detail.trailingAnchor.constraint(equalTo: card.trailingAnchor, constant: -16),
        ])

        return card
    }

    private func makeBadge(for server: ATunnelServerEntry) -> UIView {
        let dot = UIView()
        dot.layer.cornerRadius = 4
        dot.translatesAutoresizingMaskIntoConstraints = false

        let label = UILabel()
        label.font = Font.semibold(11.0)
        label.translatesAutoresizingMaskIntoConstraints = false

        if server.active {
            dot.backgroundColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
            label.text = isRu ? "АКТИВЕН" : "ACTIVE"
            label.textColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
        } else if !server.available {
            dot.backgroundColor = .systemRed
            label.text = isRu ? "НЕДОСТУПЕН" : "DOWN"
            label.textColor = .systemRed
        } else {
            // available but not primary — no badge
            return UIView()
        }

        let container = UIView()
        container.addSubview(dot); container.addSubview(label)
        NSLayoutConstraint.activate([
            dot.widthAnchor.constraint(equalToConstant: 8),
            dot.heightAnchor.constraint(equalToConstant: 8),
            dot.leadingAnchor.constraint(equalTo: container.leadingAnchor),
            dot.centerYAnchor.constraint(equalTo: container.centerYAnchor),
            label.leadingAnchor.constraint(equalTo: dot.trailingAnchor, constant: 5),
            label.trailingAnchor.constraint(equalTo: container.trailingAnchor),
            label.topAnchor.constraint(equalTo: container.topAnchor),
            label.bottomAnchor.constraint(equalTo: container.bottomAnchor),
        ])
        return container
    }

    // Map server region code → localized display name
    private func localizedRegion(_ region: String) -> String {
        let r = region.lowercased()
        if r == "de" || r.contains("germ") || r.contains("герм") || r.contains("frankfurt") || r.contains("франкф") {
            return isRu ? "Германия" : "Germany"
        }
        if r == "fi" || r.contains("finl") || r.contains("финл") || r.contains("helsinki") || r.contains("хелс") {
            return isRu ? "Финляндия" : "Finland"
        }
        if r == "nl" || r.contains("neth") || r.contains("нидерл") { return isRu ? "Нидерланды" : "Netherlands" }
        if r == "us" || r.contains("usa")  || r.contains("сша")    { return isRu ? "США" : "USA" }
        // Capitalise unknown codes as-is
        return region
    }

    // MARK: - Timer

    private func startRefreshTimer() {
        stopRefreshTimer()
        refreshTimer = Foundation.Timer.scheduledTimer(withTimeInterval: 3.0, repeats: true) { [weak self] _ in
            self?.loadData()
            self?.applyUpdatedLabel()
            self?.applyDiagButton()
        }
    }

    private func stopRefreshTimer() { refreshTimer?.invalidate(); refreshTimer = nil }

    // MARK: - Actions

    @objc private func diagButtonTapped() {
        diagButton.isEnabled = false
        let vc = ATunnelDiagnosticsViewController(theme: theme, isRu: isRu)
        vc.onDismiss = { [weak self] in
            self?.loadData()
            self?.buildServerCards()
            self?.applyData()
            self?.diagButton.isEnabled = true
        }
        if #available(iOS 15.0, *) {
            if let sheet = vc.sheetPresentationController {
                sheet.detents = [.medium(), .large()]
                sheet.prefersGrabberVisible = true
                sheet.preferredCornerRadius = 24
            }
        }
        present(vc, animated: true)
    }
}

// MARK: - ATunnelDiagnosticsViewController

private final class ATunnelDiagnosticsViewController: UIViewController {

    var onDismiss: (() -> Void)?

    private let theme: PresentationTheme
    private let isRu:  Bool
    private let purple = UIColor(red: 0.48, green: 0.40, blue: 0.97, alpha: 1.0)
    private let cardBg = UIColor(white: 0.10, alpha: 1.0)

    private let handleBar    = UIView()
    private let titleLabel   = UILabel()
    private let subtitleLabel = UILabel()
    private let stepsStack   = UIStackView()
    private let resultCard   = UIView()
    private let resultIcon   = UIImageView()
    private let resultTitle  = UILabel()
    private let resultBody   = UILabel()
    private let closeButton  = UIButton(type: .system)

    private var stepRows: [DiagStepRow] = []

    private var pollTimer: Foundation.Timer?
    private var snapshotActiveRegion: String?
    private var snapshotUpdatedAt: Double = 0
    private var pollElapsed: TimeInterval = 0
    private let pollInterval: TimeInterval = 0.4
    private let pollTimeout:  TimeInterval = 12.0

    private let kStepAnalyze = 0
    private let kStepScan    = 1
    private let kStepSelect  = 2
    private let kStepApply   = 3

    init(theme: PresentationTheme, isRu: Bool) {
        self.theme = theme; self.isRu = isRu
        super.init(nibName: nil, bundle: nil)
        modalPresentationStyle = .pageSheet
    }
    required init?(coder: NSCoder) { fatalError() }

    deinit { pollTimer?.invalidate() }

    override func viewDidLoad() { super.viewDidLoad(); setupUI() }
    override func viewDidAppear(_ animated: Bool) { super.viewDidAppear(animated); runDiagnostics() }
    override func viewWillDisappear(_ animated: Bool) { super.viewWillDisappear(animated); pollTimer?.invalidate() }

    private func setupUI() {
        view.backgroundColor = .black
        overrideUserInterfaceStyle = .dark

        handleBar.backgroundColor = UIColor(white: 0.35, alpha: 1)
        handleBar.layer.cornerRadius = 2.5
        handleBar.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(handleBar)

        titleLabel.text = isRu ? "Диагностика" : "Diagnostics"
        titleLabel.font = Font.bold(20.0)
        titleLabel.textColor = .white
        titleLabel.textAlignment = .center
        titleLabel.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(titleLabel)

        subtitleLabel.text = isRu ? "Проверка и восстановление ATunnel" : "Checking and restoring ATunnel"
        subtitleLabel.font = Font.regular(14.0)
        subtitleLabel.textColor = UIColor(white: 0.55, alpha: 1)
        subtitleLabel.textAlignment = .center
        subtitleLabel.numberOfLines = 2
        subtitleLabel.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(subtitleLabel)

        stepsStack.axis = .vertical; stepsStack.spacing = 0
        stepsStack.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(stepsStack)

        let defs: [(String, String)] = [
            (isRu ? "Анализ текущего состояния" : "Analysing current state",    "magnifyingglass"),
            (isRu ? "Сканирование серверов"      : "Scanning servers",           "antenna.radiowaves.left.and.right"),
            (isRu ? "Выбор оптимального маршрута": "Selecting optimal route",    "arrow.triangle.branch"),
            (isRu ? "Применение настроек"         : "Applying configuration",    "checkmark.shield"),
        ]
        for d in defs {
            let row = DiagStepRow(title: d.0, sfSymbol: d.1, accentColor: purple)
            stepRows.append(row); stepsStack.addArrangedSubview(row)
        }

        resultCard.backgroundColor = cardBg
        resultCard.layer.cornerRadius = 16; resultCard.layer.cornerCurve = .continuous
        resultCard.alpha = 0
        resultCard.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(resultCard)

        resultIcon.contentMode = .scaleAspectFit
        resultIcon.translatesAutoresizingMaskIntoConstraints = false
        resultCard.addSubview(resultIcon)

        resultTitle.font = Font.semibold(16.0)
        resultTitle.textColor = .white; resultTitle.numberOfLines = 2
        resultTitle.translatesAutoresizingMaskIntoConstraints = false
        resultCard.addSubview(resultTitle)

        resultBody.font = Font.regular(13.0)
        resultBody.textColor = UIColor(white: 0.55, alpha: 1); resultBody.numberOfLines = 3
        resultBody.translatesAutoresizingMaskIntoConstraints = false
        resultCard.addSubview(resultBody)

        closeButton.setTitle(isRu ? "Закрыть" : "Close", for: .normal)
        closeButton.titleLabel?.font = Font.semibold(17.0)
        closeButton.setTitleColor(purple, for: .normal)
        closeButton.backgroundColor = purple.withAlphaComponent(0.15)
        closeButton.layer.cornerRadius = 14; closeButton.layer.cornerCurve = .continuous
        closeButton.alpha = 0
        closeButton.translatesAutoresizingMaskIntoConstraints = false
        closeButton.addTarget(self, action: #selector(closeTapped), for: .touchUpInside)
        view.addSubview(closeButton)

        NSLayoutConstraint.activate([
            handleBar.topAnchor.constraint(equalTo: view.topAnchor, constant: 8),
            handleBar.centerXAnchor.constraint(equalTo: view.centerXAnchor),
            handleBar.widthAnchor.constraint(equalToConstant: 40),
            handleBar.heightAnchor.constraint(equalToConstant: 5),

            titleLabel.topAnchor.constraint(equalTo: handleBar.bottomAnchor, constant: 20),
            titleLabel.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 24),
            titleLabel.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -24),

            subtitleLabel.topAnchor.constraint(equalTo: titleLabel.bottomAnchor, constant: 6),
            subtitleLabel.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 24),
            subtitleLabel.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -24),

            stepsStack.topAnchor.constraint(equalTo: subtitleLabel.bottomAnchor, constant: 28),
            stepsStack.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 20),
            stepsStack.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -20),

            resultCard.topAnchor.constraint(equalTo: stepsStack.bottomAnchor, constant: 20),
            resultCard.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 20),
            resultCard.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -20),
            resultCard.heightAnchor.constraint(equalToConstant: 90),

            resultIcon.leadingAnchor.constraint(equalTo: resultCard.leadingAnchor, constant: 16),
            resultIcon.centerYAnchor.constraint(equalTo: resultCard.centerYAnchor),
            resultIcon.widthAnchor.constraint(equalToConstant: 36),
            resultIcon.heightAnchor.constraint(equalToConstant: 36),

            resultTitle.topAnchor.constraint(equalTo: resultCard.topAnchor, constant: 16),
            resultTitle.leadingAnchor.constraint(equalTo: resultIcon.trailingAnchor, constant: 14),
            resultTitle.trailingAnchor.constraint(equalTo: resultCard.trailingAnchor, constant: -16),

            resultBody.topAnchor.constraint(equalTo: resultTitle.bottomAnchor, constant: 4),
            resultBody.leadingAnchor.constraint(equalTo: resultIcon.trailingAnchor, constant: 14),
            resultBody.trailingAnchor.constraint(equalTo: resultCard.trailingAnchor, constant: -16),

            closeButton.topAnchor.constraint(equalTo: resultCard.bottomAnchor, constant: 16),
            closeButton.leadingAnchor.constraint(equalTo: view.leadingAnchor, constant: 20),
            closeButton.trailingAnchor.constraint(equalTo: view.trailingAnchor, constant: -20),
            closeButton.heightAnchor.constraint(equalToConstant: 50),
        ])
    }

    private func runDiagnostics() {
        snapshotActiveRegion = currentActiveRegion()
        snapshotUpdatedAt    = currentUpdatedAt()
        stepRows[kStepAnalyze].setState(.running)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.6) { [weak self] in
            guard let self = self else { return }
            self.stepRows[self.kStepAnalyze].setState(self.snapshotUpdatedAt > 0 ? .ok : .warn)
            self.stepRows[self.kStepScan].setState(.running)
            NotificationCenter.default.post(name: NSNotification.Name("aorusgram_request_probe"), object: nil)
            self.startPolling()
        }
    }

    private func startPolling() {
        pollElapsed = 0
        pollTimer = Foundation.Timer.scheduledTimer(withTimeInterval: pollInterval, repeats: true) { [weak self] _ in
            self?.pollTick()
        }
    }

    private func pollTick() {
        pollElapsed += pollInterval
        let newAt = currentUpdatedAt()
        if newAt > snapshotUpdatedAt + 0.5 { pollTimer?.invalidate(); onProbeFinished(); return }
        if pollElapsed >= pollTimeout       { pollTimer?.invalidate(); onProbeFinished() }
    }

    private func onProbeFinished() {
        stepRows[kStepScan].setState(.ok)
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.4) { [weak self] in
            guard let self = self else { return }
            self.stepRows[self.kStepSelect].setState(.running)
            let newActive = self.currentActiveRegion()
            let allDown   = self.currentAllDown()
            DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { [weak self] in
                guard let self = self else { return }
                self.stepRows[self.kStepSelect].setState(allDown ? .fail : .ok)
                self.stepRows[self.kStepApply].setState(.running)
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.4) { [weak self] in
                    guard let self = self else { return }
                    self.stepRows[self.kStepApply].setState(allDown ? .fail : .ok)
                    self.showResult(prev: self.snapshotActiveRegion, new: newActive, allDown: allDown)
                }
            }
        }
    }

    private func showResult(prev: String?, new: String?, allDown: Bool) {
        let cfg = UIImage.SymbolConfiguration(pointSize: 28, weight: .semibold)
        if allDown {
            resultIcon.image = UIImage(systemName: "wifi.slash", withConfiguration: cfg)
            resultIcon.tintColor = .systemRed
            resultTitle.text = isRu ? "Серверы недоступны" : "Servers unreachable"
            resultBody.text  = isRu ? "Нет ответа ни от одного сервера. Проверьте интернет." : "No server responded. Check your internet."
        } else if let n = new, n != prev, let p = prev {
            resultIcon.image = UIImage(systemName: "arrow.triangle.2.circlepath", withConfiguration: cfg)
            resultIcon.tintColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
            resultTitle.text = isRu ? "Маршрут переключён" : "Route switched"
            resultBody.text  = "\(p) → \(n)\n" + (isRu ? "Активный сервер обновлён автоматически." : "Active server updated automatically.")
        } else if let active = new {
            resultIcon.image = UIImage(systemName: "checkmark.shield.fill", withConfiguration: cfg)
            resultIcon.tintColor = purple
            resultTitle.text = isRu ? "Соединение в норме" : "Connection is healthy"
            resultBody.text  = (isRu ? "Активен: " : "Active: ") + active + (isRu ? ". Маршрутизация оптимальна." : ". Routing is optimal.")
        } else {
            resultIcon.image = UIImage(systemName: "exclamationmark.triangle.fill", withConfiguration: cfg)
            resultIcon.tintColor = .systemOrange
            resultTitle.text = isRu ? "Нет данных" : "No data"
            resultBody.text  = isRu ? "Данные ещё не получены. Подождите немного." : "Data not yet available. Wait a moment."
        }
        UIView.animate(withDuration: 0.4) { self.resultCard.alpha = 1; self.closeButton.alpha = 1 }
    }

    private func parsedDiag() -> ATunnelDiag? {
        guard let s = UserDefaults.standard.string(forKey: "aorusgram_atunnel_status"),
              let d = s.data(using: .utf8) else { return nil }
        return try? JSONDecoder().decode(ATunnelDiag.self, from: d)
    }
    private func currentActiveRegion() -> String? { parsedDiag()?.servers.first(where: { $0.active })?.region }
    private func currentUpdatedAt() -> Double      { parsedDiag()?.updatedAt ?? 0 }
    private func currentAllDown() -> Bool          { parsedDiag()?.servers.allSatisfy { !$0.available } ?? true }

    @objc private func closeTapped() {
        closeButton.isEnabled = false
        dismiss(animated: true) { [weak self] in self?.onDismiss?() }
    }
}

// MARK: - DiagStepRow

private final class DiagStepRow: UIView {

    enum State { case idle, running, ok, warn, fail }

    private let iconBg    = UIView()
    private let iconView  = UIImageView()
    private let titleLbl  = UILabel()
    private let indicator = UIActivityIndicatorView(style: .medium)
    private let stateIcon = UIImageView()

    init(title: String, sfSymbol: String, accentColor: UIColor) {
        super.init(frame: .zero)

        iconBg.backgroundColor = accentColor.withAlphaComponent(0.15)
        iconBg.layer.cornerRadius = 10
        iconBg.translatesAutoresizingMaskIntoConstraints = false
        addSubview(iconBg)

        let cfg = UIImage.SymbolConfiguration(pointSize: 15, weight: .medium)
        iconView.image = UIImage(systemName: sfSymbol, withConfiguration: cfg)
        iconView.tintColor = accentColor
        iconView.contentMode = .scaleAspectFit
        iconView.translatesAutoresizingMaskIntoConstraints = false
        iconBg.addSubview(iconView)

        titleLbl.text = title
        titleLbl.font = Font.medium(15.0)
        titleLbl.textColor = .white
        titleLbl.translatesAutoresizingMaskIntoConstraints = false
        addSubview(titleLbl)

        indicator.color = accentColor
        indicator.hidesWhenStopped = true
        indicator.translatesAutoresizingMaskIntoConstraints = false
        addSubview(indicator)

        stateIcon.contentMode = .scaleAspectFit
        stateIcon.alpha = 0
        stateIcon.translatesAutoresizingMaskIntoConstraints = false
        addSubview(stateIcon)

        let sep = UIView()
        sep.backgroundColor = UIColor(white: 0.18, alpha: 1)
        sep.translatesAutoresizingMaskIntoConstraints = false
        addSubview(sep)

        NSLayoutConstraint.activate([
            heightAnchor.constraint(equalToConstant: 60),

            iconBg.leadingAnchor.constraint(equalTo: leadingAnchor, constant: 4),
            iconBg.centerYAnchor.constraint(equalTo: centerYAnchor),
            iconBg.widthAnchor.constraint(equalToConstant: 40),
            iconBg.heightAnchor.constraint(equalToConstant: 40),

            iconView.centerXAnchor.constraint(equalTo: iconBg.centerXAnchor),
            iconView.centerYAnchor.constraint(equalTo: iconBg.centerYAnchor),
            iconView.widthAnchor.constraint(equalToConstant: 20),
            iconView.heightAnchor.constraint(equalToConstant: 20),

            titleLbl.leadingAnchor.constraint(equalTo: iconBg.trailingAnchor, constant: 14),
            titleLbl.centerYAnchor.constraint(equalTo: centerYAnchor),
            titleLbl.trailingAnchor.constraint(equalTo: indicator.leadingAnchor, constant: -8),

            indicator.trailingAnchor.constraint(equalTo: trailingAnchor, constant: -4),
            indicator.centerYAnchor.constraint(equalTo: centerYAnchor),
            indicator.widthAnchor.constraint(equalToConstant: 24),
            indicator.heightAnchor.constraint(equalToConstant: 24),

            stateIcon.trailingAnchor.constraint(equalTo: trailingAnchor, constant: -4),
            stateIcon.centerYAnchor.constraint(equalTo: centerYAnchor),
            stateIcon.widthAnchor.constraint(equalToConstant: 24),
            stateIcon.heightAnchor.constraint(equalToConstant: 24),

            sep.leadingAnchor.constraint(equalTo: iconBg.leadingAnchor),
            sep.trailingAnchor.constraint(equalTo: trailingAnchor),
            sep.bottomAnchor.constraint(equalTo: bottomAnchor),
            sep.heightAnchor.constraint(equalToConstant: 0.5),
        ])
        setState(.idle)
    }
    required init?(coder: NSCoder) { fatalError() }

    func setState(_ state: State) {
        let cfg = UIImage.SymbolConfiguration(pointSize: 18, weight: .semibold)
        switch state {
        case .idle:    indicator.stopAnimating(); stateIcon.alpha = 0
        case .running: indicator.startAnimating(); stateIcon.alpha = 0
        case .ok:
            indicator.stopAnimating()
            stateIcon.image = UIImage(systemName: "checkmark.circle.fill", withConfiguration: cfg)
            stateIcon.tintColor = UIColor(red: 0.20, green: 0.78, blue: 0.35, alpha: 1)
            UIView.animate(withDuration: 0.25) { self.stateIcon.alpha = 1 }
        case .warn:
            indicator.stopAnimating()
            stateIcon.image = UIImage(systemName: "exclamationmark.circle.fill", withConfiguration: cfg)
            stateIcon.tintColor = .systemOrange
            UIView.animate(withDuration: 0.25) { self.stateIcon.alpha = 1 }
        case .fail:
            indicator.stopAnimating()
            stateIcon.image = UIImage(systemName: "xmark.circle.fill", withConfiguration: cfg)
            stateIcon.tintColor = .systemRed
            UIView.animate(withDuration: 0.25) { self.stateIcon.alpha = 1 }
        }
    }
}
