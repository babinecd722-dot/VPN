import UIKit

// Shared layout skeleton for subscription screens: a vertically-centred, scrollable
// content area + a button stack pinned to the bottom safe area. Adapts cleanly from
// iPhone SE to Pro Max (content scrolls if it doesn't fit) and forces dark/AMOLED.
class SubscriptionBaseController: UIViewController {
    let scrollView = UIScrollView()
    let contentStack = UIStackView()
    let buttonStack = UIStackView()

    override var preferredStatusBarStyle: UIStatusBarStyle { .lightContent }

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = SubscriptionStyle.background
        overrideUserInterfaceStyle = .dark

        buttonStack.axis = .vertical
        buttonStack.spacing = 10
        buttonStack.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(buttonStack)

        scrollView.translatesAutoresizingMaskIntoConstraints = false
        scrollView.alwaysBounceVertical = true
        scrollView.showsVerticalScrollIndicator = false
        scrollView.keyboardDismissMode = .interactive
        view.addSubview(scrollView)

        contentStack.axis = .vertical
        contentStack.alignment = .fill
        contentStack.spacing = 14
        contentStack.translatesAutoresizingMaskIntoConstraints = false
        scrollView.addSubview(contentStack)

        let guide = view.safeAreaLayoutGuide
        let frame = scrollView.frameLayoutGuide
        let content = scrollView.contentLayoutGuide

        NSLayoutConstraint.activate([
            buttonStack.leadingAnchor.constraint(equalTo: guide.leadingAnchor, constant: 24),
            buttonStack.trailingAnchor.constraint(equalTo: guide.trailingAnchor, constant: -24),
            buttonStack.bottomAnchor.constraint(equalTo: guide.bottomAnchor, constant: -16),

            scrollView.topAnchor.constraint(equalTo: guide.topAnchor),
            scrollView.leadingAnchor.constraint(equalTo: view.leadingAnchor),
            scrollView.trailingAnchor.constraint(equalTo: view.trailingAnchor),
            scrollView.bottomAnchor.constraint(equalTo: buttonStack.topAnchor, constant: -12),

            // Top-aligned, full-width content; scrolls when it exceeds the viewport.
            contentStack.topAnchor.constraint(equalTo: content.topAnchor, constant: 28),
            contentStack.bottomAnchor.constraint(equalTo: content.bottomAnchor, constant: -24),
            contentStack.leadingAnchor.constraint(equalTo: frame.leadingAnchor, constant: 28),
            contentStack.trailingAnchor.constraint(equalTo: frame.trailingAnchor, constant: -28),
        ])
    }

    func addContent(_ subview: UIView) { contentStack.addArrangedSubview(subview) }

    func addSpacing(_ height: CGFloat) {
        let spacer = UIView()
        spacer.translatesAutoresizingMaskIntoConstraints = false
        spacer.heightAnchor.constraint(equalToConstant: height).isActive = true
        contentStack.addArrangedSubview(spacer)
    }

    func addBottomButton(_ button: UIButton) { buttonStack.addArrangedSubview(button) }
}
