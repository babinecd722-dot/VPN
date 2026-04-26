# Windscribe VPN — Windows installer (official)

The Windows x64 installer is vendored here as `Windscribe_2.21.7_amd64.exe` (from the official
`windscribe.com` redirect). You can re-fetch with the script if the version changes.

## Official entry points (HTTPS on windscribe.com)

- Windows desktop download page: https://windscribe.com/download?os=windows&platform=desktop
- Direct installer redirect (302 → CDN): https://windscribe.com/install/desktop/windows

## Resolved CDN URL (as of 2026-04-26)

After following the redirect from `windscribe.com`, the file was:

- https://deploy.totallyacdn.com/desktop-apps/2.21.7/Windscribe_2.21.7_amd64.exe

**SHA-256:** `f42e3110682d957af701bea6ea79b4eb2a9097dcd41acac9fd0e39ca1276c3ba`  
**Size:** 39904216 bytes

Re-download and verify:

```bash
./scripts/download-windscribe-windows.sh
```

Windscribe is third-party software; use only installers obtained from **windscribe.com** or URLs it redirects to.
