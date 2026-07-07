#!/usr/bin/env python3
"""Inject AorusFix.dylib into the main app binary and every extension binary.

Usage: inject_dylib.py <App.app dir> <AorusFix.dylib path>

- copies AorusFix.dylib next to each Mach-O executable (app root + each .appex)
  so the "@executable_path/AorusFix.dylib" load command resolves in every process;
- adds an LC_LOAD_DYLIB load command to the main binary and each extension binary.

The IPA is unsigned at this point (rules_apple disable_legacy_signing); the user
re-signs the whole bundle afterwards (ESign), which also signs the injected dylib.
"""
import glob
import os
import plistlib
import shutil
import sys

import lief

LOAD_PATH = "@executable_path/AorusFix.dylib"


def executable_path(bundle_dir):
    with open(os.path.join(bundle_dir, "Info.plist"), "rb") as f:
        info = plistlib.load(f)
    return os.path.join(bundle_dir, info["CFBundleExecutable"])


def inject(bundle_dir, dylib_src):
    shutil.copy(dylib_src, os.path.join(bundle_dir, "AorusFix.dylib"))
    binary_path = executable_path(bundle_dir)
    fat = lief.MachO.parse(binary_path)
    changed = False
    for slice_ in fat:
        existing = [lib.name for lib in slice_.libraries]
        if LOAD_PATH not in existing:
            slice_.add_library(LOAD_PATH)
            changed = True
    if changed:
        fat.write(binary_path)
    # verify
    verify = lief.MachO.parse(binary_path)
    ok = all(LOAD_PATH in [lib.name for lib in s.libraries] for s in verify)
    print("  injected {} -> {}".format("OK" if ok else "FAILED", binary_path))
    if not ok:
        sys.exit("ERROR: load command not present after write: " + binary_path)


def main():
    app_dir, dylib_src = sys.argv[1], sys.argv[2]
    print("Main app:", app_dir)
    inject(app_dir, dylib_src)
    appexes = sorted(glob.glob(os.path.join(app_dir, "PlugIns", "*.appex")))
    print("Extensions found: {}".format(len(appexes)))
    for appex in appexes:
        print("Extension:", os.path.basename(appex))
        inject(appex, dylib_src)


if __name__ == "__main__":
    main()
