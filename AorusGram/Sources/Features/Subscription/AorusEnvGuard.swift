import Foundation
import Darwin
import MachO

// Hardened runtime guard for the license layer.
//
// AUDIENCE: AorusGram ships via E-sign / AltStore sideloading — legitimate users are
// NOT jailbroken. So a jailbreak / injection signal is, in practice, an attacker. That
// lets us crash HARD on detection with negligible false-positive risk.
//
// HONEST LIMITS: no client-side check is unbypassable (Shadow / Liberty / A-Bypass /
// KernBypass hook detectors). This design instead maximizes cost to bypass:
//   • low-level syscalls (stat/access/open/lstat/getenv), not Foundation, which is the
//     first thing high-level bypass tweaks hook;
//   • cross-API consistency: if FileManager says "absent" but a raw syscall says
//     "present", a tweak is selectively lying → that itself is detection;
//   • multiple independent enforcement sites + multiple fatal paths, so NOP-ing one
//     call site or one crash does not disable the system;
//   • re-checks on launch, at the gate, and before every signed license request.
//
// FALSE-POSITIVE SAFETY: every hard-crash trigger (JB artifacts, sandbox escape,
// injected dylibs, DYLD_INSERT_LIBRARIES, JB symlinks) is ABSENT on a clean,
// non-jailbroken device — including E-sign / AltStore installs.
enum AorusEnvGuard {

    // The simulator runs on macOS, where /bin/bash etc. exist and /private is
    // writable — those would false-trigger. Never enforce/report there.
    #if targetEnvironment(simulator)
    private static let isSimulator = true
    #else
    private static let isSimulator = false
    #endif

    // MARK: - Public enforcement (crashes hard if compromised)

    @inline(__always)
    static func enforceAtLaunch() {
        guard !isSimulator else { return }
        if jailbrokenOrInjected(full: true) { selfDestruct(0xA1) }
    }

    @inline(__always)
    static func enforceAtGate() {
        guard !isSimulator else { return }
        // Independent site with its own crash code — patching the launch check is not enough.
        if jailbrokenOrInjected(full: true) { selfDestruct(0xB2) }
    }

    @inline(__always)
    static func enforceBeforeRequest() {
        guard !isSimulator else { return }
        // Fast subset run on the license request path itself.
        if hasInjection() || hasSuspiciousPaths() || canEscapeSandbox() { selfDestruct(0xC3) }
    }

    // MARK: - Anti-debug

    // PT_DENY_ATTACH: refuse debugger / Frida-via-ptrace attach for this process.
    // Does NOT crash and does NOT affect a normal launch (AltStore JIT is unused here).
    static func denyDebuggerAttach() {
        typealias PtraceFn = @convention(c) (Int32, pid_t, UnsafeMutableRawPointer?, Int32) -> Int32
        let ptDenyAttach: Int32 = 31
        guard let handle = dlopen(nil, RTLD_NOW), let sym = dlsym(handle, "ptrace") else { return }
        let ptrace = unsafeBitCast(sym, to: PtraceFn.self)
        _ = ptrace(ptDenyAttach, 0, nil, 0)
    }

    static func isDebuggerAttached() -> Bool {
        var info = kinfo_proc()
        var size = MemoryLayout<kinfo_proc>.stride
        var mib: [Int32] = [CTL_KERN, KERN_PROC, KERN_PROC_PID, getpid()]
        let result = mib.withUnsafeMutableBufferPointer { sysctl($0.baseAddress, 4, &info, &size, nil, 0) }
        guard result == 0 else { return false }
        let pTraced: Int32 = 0x00000800 // P_TRACED
        return (info.kp_proc.p_flag & pTraced) != 0
    }

    // MARK: - Combined signal for the server (signed request body)

    static func flags() -> [String] {
        guard !isSimulator else { return [] }
        var flags: [String] = []
        if hasSuspiciousPaths() || canEscapeSandbox() || hasJailbreakSymlinks() { flags.append("jb") }
        if hasInjection() { flags.append("hook") }
        if isDebuggerAttached() { flags.append("dbg") }
        return flags
    }

    // MARK: - Detection core

    private static func jailbrokenOrInjected(full: Bool) -> Bool {
        if hasInjection() { return true }
        if hasSuspiciousPaths() { return true }
        if canEscapeSandbox() { return true }
        if full && hasJailbreakSymlinks() { return true }
        if full && hasInjectEnv() { return true }
        return false
    }

    // Artifacts of a jailbreak. Checked via multiple independent syscalls; a mismatch
    // between Foundation and raw stat/access means a tweak is selectively hiding them.
    private static func hasSuspiciousPaths() -> Bool {
        let paths = [
            "/Applications/Cydia.app", "/Applications/Sileo.app", "/Applications/Zebra.app",
            "/Applications/Filza.app", "/Applications/blackra1n.app",
            "/usr/sbin/sshd", "/usr/bin/ssh", "/bin/bash", "/bin/sh", "/etc/apt",
            "/private/var/lib/apt", "/private/var/lib/cydia", "/private/var/stash",
            "/private/var/tmp/cydia.log", "/usr/libexec/cydia", "/usr/libexec/sftp-server",
            "/usr/lib/libsubstrate.dylib", "/usr/lib/libhooker.dylib", "/usr/lib/substitute-loader.dylib",
            "/Library/MobileSubstrate/MobileSubstrate.dylib", "/Library/MobileSubstrate/DynamicLibraries",
            "/var/jb", "/var/binpack", "/var/checkra1n.dmg"
        ]
        for path in paths {
            let fm = FileManager.default.fileExists(atPath: path)
            let raw = rawExists(path)
            if fm || raw { return true }       // present (or a tweak hid it from one API)
        }
        return false
    }

    // stat() OR access() — both raw libc, independent of Foundation hooks.
    private static func rawExists(_ path: String) -> Bool {
        if access(path, F_OK) == 0 { return true }
        var st = stat()
        return stat(path, &st) == 0
    }

    // A non-jailbroken sandbox forbids writing outside the app container.
    private static func canEscapeSandbox() -> Bool {
        let targets = ["/private/aorus_\(UInt32.random(in: 0..<UInt32.max))",
                       "/aorus_\(UInt32.random(in: 0..<UInt32.max))"]
        for target in targets {
            if let file = fopen(target, "w") {   // low-level, non-variadic
                fclose(file)
                unlink(target)
                return true
            }
        }
        return false
    }

    // Injected tweak / instrumentation libraries loaded into our address space.
    private static func hasInjection() -> Bool {
        let markers = ["substrate", "substitute", "libhooker", "frida", "cynject",
                       "cycript", "rocketbootstrap", "shadow.dylib", "tweakinject",
                       "libellekit", "pspawn"]
        let count = _dyld_image_count()
        var i: UInt32 = 0
        while i < count {
            if let cName = _dyld_get_image_name(i) {
                let name = String(cString: cName).lowercased()
                if markers.contains(where: { name.contains($0) }) { return true }
            }
            i += 1
        }
        return false
    }

    // DYLD_INSERT_LIBRARIES is the classic injection vector.
    private static func hasInjectEnv() -> Bool {
        return getenv("DYLD_INSERT_LIBRARIES") != nil
    }

    // Classic jailbreaks symlink system dirs onto the data partition.
    private static func hasJailbreakSymlinks() -> Bool {
        let links = ["/Applications", "/Library/Ringtones", "/Library/Wallpaper",
                     "/usr/share", "/var/stash"]
        for path in links {
            var st = stat()
            if lstat(path, &st) == 0 && (UInt32(st.st_mode) & UInt32(S_IFMT)) == UInt32(S_IFLNK) { return true }
        }
        return false
    }

    // MARK: - Crash (multiple independent fatal paths)

    private static func selfDestruct(_ tag: UInt8) -> Never {
        // Spread fatal operations so NOP-ing any single one still ends the process.
        let a = UnsafeMutablePointer<UInt8>(bitPattern: UInt(0x1 + Int(tag)))
        a?.pointee = tag
        let b = UnsafeMutablePointer<Int>(bitPattern: 0xF00D)
        b?.pointee = Int(tag)
        abort()
    }
}
