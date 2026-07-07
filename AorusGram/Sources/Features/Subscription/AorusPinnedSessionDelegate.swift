import Foundation
import CryptoKit
import Security

// TLS public-key (SPKI) pinning for the license URLSession.
//
// Pins the SHA256 of the server certificate's SubjectPublicKeyInfo. A normal MITM
// (a custom root CA trusted on the device, Charles/mitmproxy, etc.) presents a
// different public key → the pin does not match → the connection is dropped before
// any license traffic flows.
//
// ROLLOUT SAFETY: when SubscriptionConfig.pinnedSPKIHashesBase64 is empty, pinning
// is DISABLED and the delegate falls back to default system trust — zero behavior
// change. Fill the pin set only after you have verified the server's real SPKI
// hash, otherwise legitimate connections would be refused.
//
// To compute a pin (base64 of SHA256 over the DER SPKI):
//   openssl s_client -connect license.aorusgram.com:443 </dev/null 2>/dev/null \
//     | openssl x509 -pubkey -noout \
//     | openssl pkey -pubin -outform der \
//     | openssl dgst -sha256 -binary | openssl enc -base64
final class AorusPinnedSessionDelegate: NSObject, URLSessionDelegate {
    static let shared = AorusPinnedSessionDelegate()

    func urlSession(_ session: URLSession,
                    didReceive challenge: URLAuthenticationChallenge,
                    completionHandler: @escaping (URLSession.AuthChallengeDisposition, URLCredential?) -> Void) {
        guard challenge.protectionSpace.authenticationMethod == NSURLAuthenticationMethodServerTrust,
              let trust = challenge.protectionSpace.serverTrust else {
            completionHandler(.performDefaultHandling, nil); return
        }

        let pins = SubscriptionConfig.pinnedSPKIHashesBase64
        guard !pins.isEmpty else {
            completionHandler(.performDefaultHandling, nil); return   // pinning disabled
        }

        // 1) Standard chain validation must pass first.
        var secError: CFError?
        guard SecTrustEvaluateWithError(trust, &secError) else {
            completionHandler(.cancelAuthenticationChallenge, nil); return
        }

        // 2) At least one cert in the chain must match a pinned SPKI hash.
        for cert in certificateChain(trust) {
            if let spki = spkiSHA256Base64(cert), pins.contains(spki) {
                completionHandler(.useCredential, URLCredential(trust: trust)); return
            }
        }
        completionHandler(.cancelAuthenticationChallenge, nil)
    }

    private func certificateChain(_ trust: SecTrust) -> [SecCertificate] {
        if #available(iOS 15.0, *) {
            return (SecTrustCopyCertificateChain(trust) as? [SecCertificate]) ?? []
        } else {
            var out: [SecCertificate] = []
            let count = SecTrustGetCertificateCount(trust)
            for i in 0..<count {
                if let c = SecTrustGetCertificateAtIndex(trust, i) { out.append(c) }
            }
            return out
        }
    }

    private func spkiSHA256Base64(_ cert: SecCertificate) -> String? {
        guard let key = SecCertificateCopyKey(cert),
              let keyData = SecKeyCopyExternalRepresentation(key, nil) as Data?,
              let header = Self.spkiHeader(for: key) else { return nil }
        var spki = Data(header)
        spki.append(keyData)
        return Data(SHA256.hash(data: spki)).base64EncodedString()
    }

    // ASN.1 SubjectPublicKeyInfo DER headers for the common key types. The raw key
    // bytes from SecKeyCopyExternalRepresentation lack this prefix; we prepend it so
    // the hash matches a standard SPKI pin.
    private static func spkiHeader(for key: SecKey) -> [UInt8]? {
        guard let attrs = SecKeyCopyAttributes(key) as? [CFString: Any],
              let type  = attrs[kSecAttrKeyType] as? String,
              let bits  = attrs[kSecAttrKeySizeInBits] as? Int else { return nil }

        if type == (kSecAttrKeyTypeRSA as String) {
            switch bits {
            case 2048: return rsa2048SPKIHeader
            case 4096: return rsa4096SPKIHeader
            default:   return nil
            }
        } else if type == (kSecAttrKeyTypeECSECPrimeRandom as String) {
            switch bits {
            case 256: return ec256SPKIHeader
            case 384: return ec384SPKIHeader
            default:  return nil
            }
        }
        return nil
    }

    private static let rsa2048SPKIHeader: [UInt8] = [
        0x30,0x82,0x01,0x22,0x30,0x0d,0x06,0x09,0x2a,0x86,0x48,0x86,0xf7,0x0d,0x01,0x01,
        0x01,0x05,0x00,0x03,0x82,0x01,0x0f,0x00
    ]
    private static let rsa4096SPKIHeader: [UInt8] = [
        0x30,0x82,0x02,0x22,0x30,0x0d,0x06,0x09,0x2a,0x86,0x48,0x86,0xf7,0x0d,0x01,0x01,
        0x01,0x05,0x00,0x03,0x82,0x02,0x0f,0x00
    ]
    private static let ec256SPKIHeader: [UInt8] = [
        0x30,0x59,0x30,0x13,0x06,0x07,0x2a,0x86,0x48,0xce,0x3d,0x02,0x01,0x06,0x08,0x2a,
        0x86,0x48,0xce,0x3d,0x03,0x01,0x07,0x03,0x42,0x00
    ]
    private static let ec384SPKIHeader: [UInt8] = [
        0x30,0x76,0x30,0x10,0x06,0x07,0x2a,0x86,0x48,0xce,0x3d,0x02,0x01,0x06,0x05,0x2b,
        0x81,0x04,0x00,0x22,0x03,0x62,0x00
    ]
}
