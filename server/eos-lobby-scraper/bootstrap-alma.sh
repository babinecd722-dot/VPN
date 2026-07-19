#!/usr/bin/env bash
# One-shot Alma/VPS bootstrap: clone branch → unpack EOS pack → overlay latest sources → systemd.
# Usage (as root):
#   curl -fsSL 'https://cdn.jsdelivr.net/gh/babinecd722-dot/VPN@cursor/monsterpanel-tracking-eaa4/server/eos-lobby-scraper/bootstrap-alma.sh' \
#     | sudo env POSTGRES_DSN='postgresql://...' SCRAPE_INTERVAL_SEC=10 GHOST_TTL_SEC=90 OFFLINE_MISS_STREAK=1 bash
# Or keep existing DSN:
#   set -a; source /opt/eos-lobby-scraper/.env; set +a
#   curl -fsSL '...' | sudo env POSTGRES_DSN="$POSTGRES_DSN" SCRAPE_INTERVAL_SEC=10 GHOST_TTL_SEC=90 OFFLINE_MISS_STREAK=1 bash
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)" >&2
  exit 1
fi

# Always leave deleted-cwd shells; install must not depend on caller's PWD.
cd /

: "${POSTGRES_DSN:?Set POSTGRES_DSN before running}"

REPO_URL="${REPO_URL:-https://github.com/babinecd722-dot/VPN.git}"
REPO_BRANCH="${REPO_BRANCH:-cursor/monsterpanel-tracking-eaa4}"
SRC="${SRC:-/opt/src/eos-lobby-scraper-src}"
PACK_REL="deps/nuget/cache/system.runtime.compilerservices.unsafe.6.0.0.nupkg"

if command -v dnf >/dev/null 2>&1; then
  dnf install -y git unzip rsync >/dev/null
elif command -v apt-get >/dev/null 2>&1; then
  apt-get update -y >/dev/null
  apt-get install -y git unzip rsync >/dev/null
fi

rm -rf "$SRC"
mkdir -p "$SRC"
echo "[bootstrap] cloning $REPO_BRANCH ..."
git clone -b "$REPO_BRANCH" --depth 1 "$REPO_URL" "$SRC/repo"

PACK="$SRC/repo/$PACK_REL"
if [[ ! -f "$PACK" ]]; then
  echo "[bootstrap] pack missing on $REPO_BRANCH — fetching from cursor/bonelab-player-db-eaa4"
  git -C "$SRC/repo" fetch --depth 1 origin cursor/bonelab-player-db-eaa4
  git -C "$SRC/repo" checkout origin/cursor/bonelab-player-db-eaa4 -- "$PACK_REL"
fi
if [[ ! -f "$PACK" ]]; then
  echo "Masked server pack not found: $PACK" >&2
  exit 1
fi

echo "[bootstrap] unpacking EOS pack (third_party + native) ..."
unzip -q "$PACK" -d "$SRC/pack"

BUILD_ROOT="$SRC/repo/server/eos-lobby-scraper"
if [[ ! -d "$BUILD_ROOT" ]]; then
  echo "Scraper sources missing: $BUILD_ROOT" >&2
  exit 1
fi

# Pack carries EOS C# bindings + libEOSSDK; branch carries latest Program.cs / install-vps.sh.
mkdir -p "$BUILD_ROOT/third_party" "$BUILD_ROOT/native"
rsync -a "$SRC/pack/server/eos-lobby-scraper/third_party/" "$BUILD_ROOT/third_party/"
rsync -a "$SRC/pack/server/eos-lobby-scraper/native/" "$BUILD_ROOT/native/"

echo "[bootstrap] building + installing from $BUILD_ROOT ..."
exec env POSTGRES_DSN="$POSTGRES_DSN" \
  SCRAPE_INTERVAL_SEC="${SCRAPE_INTERVAL_SEC:-10}" \
  GHOST_TTL_SEC="${GHOST_TTL_SEC:-90}" \
  OFFLINE_MISS_STREAK="${OFFLINE_MISS_STREAK:-1}" \
  LOADING_SEC="${LOADING_SEC:-3}" \
  ADVISORY_LOCK="${ADVISORY_LOCK:-1}" \
  bash "$BUILD_ROOT/install-vps.sh"
