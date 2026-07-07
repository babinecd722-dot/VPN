import Foundation

// Local Telegram Premium toggle. Stored in UserDefaults under a plain key so the
// low-level premium gate (PeerUtils.isPremium / AccountContextImpl, in the Telegram
// source) can read the same flag without importing this module.
//
// Default is ON: a fresh install (key absent) has premium enabled, matching the
// previous always-on behaviour. The toggle in the "Прочее" screen flips it.
public enum AorusLocalPremium {
    public static let key = "aorusgram_local_premium"

    public static var isEnabled: Bool {
        let defaults = UserDefaults.standard
        if defaults.object(forKey: key) == nil {
            return true
        }
        return defaults.bool(forKey: key)
    }

    public static func setEnabled(_ value: Bool) {
        UserDefaults.standard.set(value, forKey: key)
    }
}
