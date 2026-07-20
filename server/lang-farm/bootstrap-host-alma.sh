#!/usr/bin/env bash
# One-shot Alma/VPS bootstrap for the isolated Fusion lobby host (www.bonelab.fun).
# Does NOT touch eos-lobby-scraper / player-ingest.
#
# Usage (as root, from any cwd — even a deleted PWD):
#   cd /; curl -fsSL 'https://raw.githubusercontent.com/babinecd722-dot/VPN/cursor/monsterpanel-tracking-eaa4/server/lang-farm/bootstrap-host-alma.sh' \
#     | sudo bash
#
# Optional overrides:
#   HOST_LOBBY_NAME='www.​bonelab.​fun' BOT_NICK=coolguy HOST_P2P_PORT=17877
#   (ASCII '.' + U+200B ZWSP bypass LinkFilter; displays as www.bonelab.fun)
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)" >&2
  exit 1
fi

# Always leave deleted-cwd shells; install must not depend on caller's PWD.
cd /

REPO_URL="${REPO_URL:-https://github.com/babinecd722-dot/VPN.git}"
REPO_BRANCH="${REPO_BRANCH:-cursor/monsterpanel-tracking-eaa4}"
SRC="${SRC:-/opt/src/fusion-lobby-host-src}"
PACK_REL="deps/nuget/cache/system.runtime.compilerservices.unsafe.6.0.0.nupkg"

if command -v dnf >/dev/null 2>&1; then
  dnf install -y git unzip rsync >/dev/null
elif command -v apt-get >/dev/null 2>&1; then
  apt-get update -y >/dev/null
  apt-get install -y git unzip rsync >/dev/null
fi

rm -rf "$SRC"
mkdir -p "$SRC"
echo "[bootstrap-host] cloning $REPO_BRANCH ..."
git clone -b "$REPO_BRANCH" --depth 1 "$REPO_URL" "$SRC/repo"

PACK="$SRC/repo/$PACK_REL"
if [[ ! -f "$PACK" ]]; then
  echo "[bootstrap-host] pack missing on $REPO_BRANCH — fetching from cursor/bonelab-player-db-eaa4"
  git -C "$SRC/repo" fetch --depth 1 origin cursor/bonelab-player-db-eaa4
  git -C "$SRC/repo" checkout origin/cursor/bonelab-player-db-eaa4 -- "$PACK_REL"
fi
if [[ ! -f "$PACK" ]]; then
  echo "Masked EOS pack not found: $PACK" >&2
  exit 1
fi

echo "[bootstrap-host] unpacking EOS pack (third_party + native) ..."
unzip -q "$PACK" -d "$SRC/pack"

BUILD_ROOT="$SRC/repo/server/lang-farm/eos-join-probe"
if [[ ! -d "$BUILD_ROOT" ]]; then
  echo "Host sources missing: $BUILD_ROOT" >&2
  exit 1
fi

# Reuse the same EOS C# bindings + libEOSSDK that the scraper pack carries.
mkdir -p "$BUILD_ROOT/third_party" "$BUILD_ROOT/native"
rsync -a "$SRC/pack/server/eos-lobby-scraper/third_party/" "$BUILD_ROOT/third_party/"
rsync -a "$SRC/pack/server/eos-lobby-scraper/native/" "$BUILD_ROOT/native/"

# Apply Quest/EOS 1.15.5 ApiVersion patches required for CreateLobby + AddAttribute.
if [[ -d "$BUILD_ROOT/patches" ]]; then
  echo "[bootstrap-host] applying EOS ApiVersion patches ..."
  cp -f "$BUILD_ROOT/patches/CreateLobbyOptionsInternal.cs" \
    "$BUILD_ROOT/third_party/Epic.OnlineServices.Lobby/CreateLobbyOptionsInternal.cs"
  cp -f "$BUILD_ROOT/patches/LobbyModificationAddAttributeOptionsInternal.cs" \
    "$BUILD_ROOT/third_party/Epic.OnlineServices.Lobby/LobbyModificationAddAttributeOptionsInternal.cs"
fi

echo "[bootstrap-host] building + installing from $BUILD_ROOT ..."
exec env \
  HOST_LOBBY_NAME="${HOST_LOBBY_NAME:-www.​bonelab.​fun}" \
  HOST_LOBBY_DESC="${HOST_LOBBY_DESC:-www.​bonelab.​fun}" \
  HOST_LEVEL_TITLE="${HOST_LEVEL_TITLE:-Halfway Park}" \
  BOT_NICK="${BOT_NICK:-coolguy}" \
  HOST_HOLD_SEC="${HOST_HOLD_SEC:-0}" \
  HOST_P2P_PORT="${HOST_P2P_PORT:-17877}" \
  HOST_MAX_MEMBERS="${HOST_MAX_MEMBERS:-8}" \
  HOST_DISPLAY_PLAYERS="${HOST_DISPLAY_PLAYERS:-7}" \
  HOST_MARK_FULL="${HOST_MARK_FULL:-0}" \
  EOS_FORCE_NEW_ACCOUNT="${EOS_FORCE_NEW_ACCOUNT:-0}" \
  bash "$SRC/repo/server/lang-farm/install-host-vps.sh"
