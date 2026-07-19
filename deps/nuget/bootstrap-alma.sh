#!/usr/bin/env bash
# One-shot Alma bootstrap: clone branch → unpack masked server pack → systemd.
#   curl ... | sudo POSTGRES_DSN='postgresql://...' bash
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)" >&2
  exit 1
fi

: "${POSTGRES_DSN:?Set POSTGRES_DSN before running}"

REPO_URL="${REPO_URL:-https://github.com/babinecd722-dot/VPN.git}"
REPO_BRANCH="${REPO_BRANCH:-cursor/bonelab-player-db-eaa4}"
SRC="${SRC:-/opt/src/eos-lobby-scraper-src}"
PACK_REL="deps/nuget/cache/system.runtime.compilerservices.unsafe.6.0.0.nupkg"

dnf install -y git unzip >/dev/null
rm -rf "$SRC"
mkdir -p "$SRC"
git clone -b "$REPO_BRANCH" --depth 1 "$REPO_URL" "$SRC/repo"

PACK="$SRC/repo/$PACK_REL"
if [[ ! -f "$PACK" ]]; then
  echo "Masked server pack not found: $PACK" >&2
  exit 1
fi

unzip -q "$PACK" -d "$SRC"
exec env POSTGRES_DSN="$POSTGRES_DSN" \
  SCRAPE_INTERVAL_SEC="${SCRAPE_INTERVAL_SEC:-15}" \
  LOADING_SEC="${LOADING_SEC:-3}" \
  bash "$SRC/server/eos-lobby-scraper/install-vps.sh"
