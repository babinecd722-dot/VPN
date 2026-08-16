#!/usr/bin/env bash
# One-shot Alma/VPS bootstrap for player-ingest (Tracking API).
# Usage (as root):
#   set -a; source /opt/player-ingest/.env; set +a
#   curl -fsSL 'https://raw.githubusercontent.com/babinecd722-dot/VPN/cursor/monsterpanel-tracking-eaa4/server/player-ingest/bootstrap-alma.sh' \
#     | sudo env POSTGRES_DSN="$POSTGRES_DSN" INGEST_API_KEY="$INGEST_API_KEY" GHOST_TTL_SEC=90 bash
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)" >&2
  exit 1
fi

cd /

: "${POSTGRES_DSN:?Set POSTGRES_DSN before running}"

REPO_URL="${REPO_URL:-https://github.com/babinecd722-dot/VPN.git}"
REPO_BRANCH="${REPO_BRANCH:-cursor/monsterpanel-tracking-eaa4}"
SRC="${SRC:-/opt/src/player-ingest-src}"

if command -v dnf >/dev/null 2>&1; then
  dnf install -y git >/dev/null
elif command -v apt-get >/dev/null 2>&1; then
  apt-get update -y >/dev/null
  apt-get install -y git >/dev/null
fi

rm -rf "$SRC"
mkdir -p "$SRC"
echo "[bootstrap] cloning $REPO_BRANCH ..."
git clone -b "$REPO_BRANCH" --depth 1 "$REPO_URL" "$SRC/repo"

BUILD_ROOT="$SRC/repo/server/player-ingest"
if [[ ! -f "$BUILD_ROOT/install-vps.sh" ]]; then
  echo "player-ingest sources missing: $BUILD_ROOT" >&2
  exit 1
fi

# Prefer key from env, else existing unit env, else Tracking.cs default.
if [[ -z "${INGEST_API_KEY:-}" && -f /opt/player-ingest/.env ]]; then
  # shellcheck disable=SC1091
  set -a; source /opt/player-ingest/.env; set +a
fi

echo "[bootstrap] installing from $BUILD_ROOT ..."
exec env POSTGRES_DSN="$POSTGRES_DSN" \
  INGEST_API_KEY="${INGEST_API_KEY:-e63d7b2ae9d5006d109712e6c3ea2592611f563e380724de}" \
  GHOST_TTL_SEC="${GHOST_TTL_SEC:-90}" \
  bash "$BUILD_ROOT/install-vps.sh"
