#!/usr/bin/env bash
# ONE command: install Fusion visual host + lang-farm bots as systemd 24/7.
# Does NOT touch eos-lobby-scraper / player-ingest.
#
# Usage (as root, from any cwd):
#   curl -fsSL 'https://raw.githubusercontent.com/babinecd722-dot/VPN/cursor/farm-skip-own-host-eaa4/server/lang-farm/bootstrap-farm-all.sh' \
#     | sudo env POSTGRES_DSN='postgresql://USER:PASS@HOST:5432/DB' bash
#
# Optional env:
#   BOTS=10              farm workers (default 10)
#   START_DETECT=0       1 = install heavy LID stack (needs ~6GB RAM)
#   BOT_NICK=bonelab·fun farm display nick
#   HOST_P2P_PORT=17877
#   REPO_BRANCH=...      override git branch
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root via: curl ... | sudo env POSTGRES_DSN='...' bash" >&2
  exit 1
fi

if [[ -z "${POSTGRES_DSN:-}" ]]; then
  echo "POSTGRES_DSN is required." >&2
  echo "Example:" >&2
  echo "  curl -fsSL 'URL' | sudo env POSTGRES_DSN='postgresql://...' bash" >&2
  exit 2
fi

# Leave deleted-cwd shells.
cd /

REPO_URL="${REPO_URL:-https://github.com/babinecd722-dot/VPN.git}"
REPO_BRANCH="${REPO_BRANCH:-cursor/farm-skip-own-host-eaa4}"
SRC="${SRC:-/opt/src/lang-farm-src}"
PACK_REL="deps/nuget/cache/system.runtime.compilerservices.unsafe.6.0.0.nupkg"

echo "[farm-all] packages..."
if command -v dnf >/dev/null 2>&1; then
  dnf install -y git unzip rsync python3 python3-pip curl >/dev/null
elif command -v apt-get >/dev/null 2>&1; then
  apt-get update -y >/dev/null
  apt-get install -y git unzip rsync python3 python3-pip python3-venv curl >/dev/null
fi

rm -rf "$SRC"
mkdir -p "$SRC"
echo "[farm-all] cloning $REPO_BRANCH ..."
git clone -b "$REPO_BRANCH" --depth 1 "$REPO_URL" "$SRC/repo"

PACK="$SRC/repo/$PACK_REL"
if [[ ! -f "$PACK" ]]; then
  echo "[farm-all] pack missing on $REPO_BRANCH — fetching from cursor/bonelab-player-db-eaa4"
  git -C "$SRC/repo" fetch --depth 1 origin cursor/bonelab-player-db-eaa4
  git -C "$SRC/repo" checkout FETCH_HEAD -- "$PACK_REL" || \
    git -C "$SRC/repo" checkout origin/cursor/bonelab-player-db-eaa4 -- "$PACK_REL"
fi
if [[ ! -f "$PACK" ]]; then
  echo "Masked EOS pack not found: $PACK" >&2
  exit 1
fi

echo "[farm-all] unpacking EOS SDK pack..."
unzip -q "$PACK" -d "$SRC/pack"

BUILD_ROOT="$SRC/repo/server/lang-farm/eos-join-probe"
FARM_ROOT_SRC="$SRC/repo/server/lang-farm"
if [[ ! -d "$BUILD_ROOT" ]]; then
  echo "Sources missing: $BUILD_ROOT" >&2
  exit 1
fi

mkdir -p "$BUILD_ROOT/third_party" "$BUILD_ROOT/native"
rsync -a "$SRC/pack/server/eos-lobby-scraper/third_party/" "$BUILD_ROOT/third_party/"
rsync -a "$SRC/pack/server/eos-lobby-scraper/native/" "$BUILD_ROOT/native/"

if [[ -d "$BUILD_ROOT/patches" ]]; then
  echo "[farm-all] applying EOS ApiVersion patches..."
  cp -f "$BUILD_ROOT/patches/CreateLobbyOptionsInternal.cs" \
    "$BUILD_ROOT/third_party/Epic.OnlineServices.Lobby/CreateLobbyOptionsInternal.cs"
  cp -f "$BUILD_ROOT/patches/LobbyModificationAddAttributeOptionsInternal.cs" \
    "$BUILD_ROOT/third_party/Epic.OnlineServices.Lobby/LobbyModificationAddAttributeOptionsInternal.cs"
  if [[ -f "$BUILD_ROOT/patches/JoinLobbyOptionsInternal.cs" ]]; then
    cp -f "$BUILD_ROOT/patches/JoinLobbyOptionsInternal.cs" \
      "$BUILD_ROOT/third_party/Epic.OnlineServices.Lobby/JoinLobbyOptionsInternal.cs"
  fi
fi

# Ensure shared code-file dir exists before host starts.
mkdir -p /opt/lang-farm/state
HOST_CODE_FILE="/opt/lang-farm/state/host_lobby_code.txt"

DEFAULT_LOBBY_NAME=$'www\u00b7bonelab\u00b7fun'
DEFAULT_BOT_NICK=$'bonelab\u00b7fun'

echo "[farm-all] === 1/2 install visual host (systemd fusion-lobby-host) ==="
env \
  HOST_LOBBY_NAME="${HOST_LOBBY_NAME:-$DEFAULT_LOBBY_NAME}" \
  HOST_LOBBY_DESC="${HOST_LOBBY_DESC:-$DEFAULT_LOBBY_NAME}" \
  HOST_LEVEL_TITLE="${HOST_LEVEL_TITLE:-Halfway Park}" \
  BOT_NICK="${HOST_BOT_NICK:-coolguy}" \
  HOST_HOLD_SEC="${HOST_HOLD_SEC:-0}" \
  HOST_P2P_PORT="${HOST_P2P_PORT:-17877}" \
  HOST_MAX_MEMBERS="${HOST_MAX_MEMBERS:-8}" \
  HOST_DISPLAY_PLAYERS="${HOST_DISPLAY_PLAYERS:-7}" \
  HOST_MARK_FULL="${HOST_MARK_FULL:-0}" \
  EOS_FORCE_NEW_ACCOUNT="${EOS_FORCE_NEW_ACCOUNT:-0}" \
  HOST_LOBBY_CODE_FILE="$HOST_CODE_FILE" \
  bash "$FARM_ROOT_SRC/install-host-vps.sh"

echo "[farm-all] === 2/2 install farm bots (systemd lang-farm-bots) ==="
env \
  POSTGRES_DSN="$POSTGRES_DSN" \
  BUILD_ROOT="$BUILD_ROOT" \
  FARM_DEST=/opt/lang-farm \
  BOTS="${BOTS:-10}" \
  BOT_NICK="${BOT_NICK:-$DEFAULT_BOT_NICK}" \
  START_DETECT="${START_DETECT:-0}" \
  HOST_LOBBY_CODE_FILE="$HOST_CODE_FILE" \
  bash "$FARM_ROOT_SRC/install-bots-vps.sh"

echo
echo "[farm-all] DONE — both units enabled 24/7"
echo "  host:  systemctl status fusion-lobby-host"
echo "         journalctl -u fusion-lobby-host -f"
echo "  bots:  systemctl status lang-farm-bots"
echo "         journalctl -u lang-farm-bots -f"
echo "  code:  cat $HOST_CODE_FILE"
echo "  note:  START_DETECT=${START_DETECT:-0} (set 1 to enable language ID — heavy)"
