import Foundation

// All subscription / license configuration in one isolated place.
//
// This whole layer is deliberately self-contained: it talks ONLY to the license
// API over a private URLSession and never touches MTProto, getProxy, the proxy
// secret logic or any Telegram networking.
enum SubscriptionConfig {
    // MARK: License API
    static let baseURLString = "https://license.aorusgram.com"
    static let keyVersion = "1"                       // X-Aorus-Kv
    static let userAgent = "AorusGram/1.0.0 (iOS)"

    // MARK: Purchase bot
    static let purchaseBotUsername = "AorusGram_bot"
    // The app's own deep-link scheme (aorusgram://). The build sets
    // app_specific_url_scheme = "aorusgram" to match Info.plist, so this resolves the
    // bot chat INSIDE AorusGram (no Safari, no real-Telegram hop).
    static let purchaseBotScheme = "aorusgram://resolve?domain=AorusGram_bot&start=buy"
    // Resolved by Telegram's own internal resolver (openExternalUrl, forceExternal:
    // false) for the over-lock flow and as an in-app fallback. Never the browser.
    static let purchaseBotLink = "https://t.me/AorusGram_bot?start=buy"

    // MARK: Networking
    static let requestTimeout: TimeInterval = 15

    // Re-check throttle: on foreground only re-hit the API if the last successful
    // check is older than this. active_until still governs real validity.
    static let recheckInterval: TimeInterval = 60 * 30   // 30 minutes

    // MARK: - Hardening (anti fake-server / MITM)
    //
    // SAFE DEFAULTS: everything below is inert until provisioned, so the current
    // build behaves exactly as before. Activate ONLY in lock-step with the server.

    // Ed25519 PUBLIC key (raw 32 bytes, lowercase hex = 64 chars) the server uses to
    // sign every license response. Empty → response verification is inert. Its leak
    // is harmless: the matching PRIVATE key lives only on the server.
    static let responseSigningPublicKeyHex = "ae648741a012a36c991fdf4680742c68bdf1722c1693b3781c4daa08a8c49196"

    // When true, a response that is unsigned OR badly-signed is rejected (treated as
    // a network failure → no access granted). Keep FALSE until the server signs every
    // response, otherwise every user would be locked out.
    static let requireSignedResponse = true

    // Max clock skew (seconds) tolerated for the signed response timestamp.
    static let responseMaxSkew: TimeInterval = 300

    // TLS SPKI pins (base64 of SHA256 over the DER SubjectPublicKeyInfo) for the
    // license endpoint. Empty → pinning disabled (default system trust). Fill ONLY
    // after verifying the real server SPKI hash.
    static let pinnedSPKIHashesBase64: [String] = []
}
