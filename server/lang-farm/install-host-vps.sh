#!/usr/bin/env bash
# Alma / RHEL / Rocky: build Fusion lobby host → /opt/fusion-lobby-host + systemd 24/7.
# Isolated from /opt/eos-lobby-scraper and /opt/player-ingest (own tree, ports, identity).
#
# Prefer bootstrap-host-alma.sh. Direct use (as root, after third_party/native are present):
#   cd /opt/src/fusion-lobby-host-src/repo/server/lang-farm && ./install-host-vps.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
PROBE="$ROOT/eos-join-probe"
DEST="${DEST:-/opt/fusion-lobby-host}"
SERVICE_NAME="${SERVICE_NAME:-fusion-lobby-host}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

if [[ ! -d "$PROBE" ]]; then
  echo "Missing probe sources: $PROBE" >&2
  exit 1
fi
if [[ ! -f "$PROBE/native/libEOSSDK-Linux-Shipping.so" ]]; then
  echo "Missing native EOS SDK under $PROBE/native — run bootstrap-host-alma.sh" >&2
  exit 1
fi
if [[ ! -d "$PROBE/third_party/Epic.OnlineServices" ]]; then
  echo "Missing third_party EOS bindings — run bootstrap-host-alma.sh" >&2
  exit 1
fi

export PATH="/usr/local/bin:/usr/bin:$PATH"

need_pkg() { command -v "$1" >/dev/null 2>&1; }

echo "[install-host] ensuring packages (rsync dotnet)..."
if need_pkg dnf; then
  dnf install -y rsync >/dev/null
  if ! need_pkg dotnet; then
    dnf install -y dotnet-sdk-8.0 || dnf install -y dotnet-sdk-9.0
  fi
elif need_pkg apt-get; then
  apt-get update -y
  apt-get install -y rsync
  if ! need_pkg dotnet; then
    apt-get install -y dotnet-sdk-8.0 || true
  fi
fi

if ! need_pkg dotnet; then
  echo "[install-host] installing .NET 8 via Microsoft script..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
  export DOTNET_ROOT=/usr/share/dotnet
  export PATH="$DOTNET_ROOT:$PATH"
fi

DOTNET_BIN="$(command -v dotnet)"
echo "[install-host] using $DOTNET_BIN ($("$DOTNET_BIN" --version))"

# Re-apply patches if present (safe on reinstall).
if [[ -d "$PROBE/patches" ]]; then
  cp -f "$PROBE/patches/CreateLobbyOptionsInternal.cs" \
    "$PROBE/third_party/Epic.OnlineServices.Lobby/CreateLobbyOptionsInternal.cs"
  cp -f "$PROBE/patches/LobbyModificationAddAttributeOptionsInternal.cs" \
    "$PROBE/third_party/Epic.OnlineServices.Lobby/LobbyModificationAddAttributeOptionsInternal.cs"
fi

echo "[install-host] building Release..."
cd "$PROBE"
"$DOTNET_BIN" build -c Release -v q -p:UseAppHost=false

echo "[install-host] syncing to $DEST..."
mkdir -p "$DEST"
# Preserve EOS DeviceId identity across updates.
if [[ -d "$DEST/data" ]]; then
  rm -rf /tmp/fusion-host-data-preserve
  cp -a "$DEST/data" /tmp/fusion-host-data-preserve
fi
if need_pkg rsync; then
  rsync -a --delete --exclude data/ --exclude .env "$PROBE/bin/Release/net8.0/" "$DEST/"
else
  find "$DEST" -mindepth 1 -maxdepth 1 ! -name data ! -name .env -exec rm -rf {} +
  cp -a "$PROBE/bin/Release/net8.0/." "$DEST/"
fi
if [[ -d /tmp/fusion-host-data-preserve ]]; then
  rm -rf "$DEST/data"
  mv /tmp/fusion-host-data-preserve "$DEST/data"
  echo "[install-host] preserved $DEST/data (EOS identity)"
fi
mkdir -p "$DEST/data"

# Write / merge .env — never touch scraper/ingest env files.
# ASCII dots + U+200B ZWSP bypass Fusion LinkFilter (normal display on Quest).
HOST_LOBBY_NAME="${HOST_LOBBY_NAME:-www.​bonelab.​fun}"
HOST_LOBBY_DESC="${HOST_LOBBY_DESC:-www.​bonelab.​fun}"
HOST_LEVEL_TITLE="${HOST_LEVEL_TITLE:-Halfway Park}"
HOST_LEVEL_BARCODE="${HOST_LEVEL_BARCODE:-fa534c5a83ee4ec6bd641fec424c4142.Level.LevelHalfwayPark}"
BOT_NICK="${BOT_NICK:-coolguy}"
HOST_HOLD_SEC="${HOST_HOLD_SEC:-0}"
HOST_P2P_PORT="${HOST_P2P_PORT:-17877}"
HOST_MAX_MEMBERS="${HOST_MAX_MEMBERS:-8}"
# Cosmetic LobbyInfo count (default 7/8). No bots / no extra CPU. Keep < max so Full=False.
HOST_DISPLAY_PLAYERS="${HOST_DISPLAY_PLAYERS:-7}"
HOST_MARK_FULL="${HOST_MARK_FULL:-0}"
EOS_FORCE_NEW_ACCOUNT="${EOS_FORCE_NEW_ACCOUNT:-0}"

cat >"$DEST/.env" <<EOF
# Isolated Fusion lobby host — do not share with eos-lobby-scraper / player-ingest.
FUSION_HOST_MODE=1
FUSION_GAME_NAME=BONELAB
HOST_LOBBY_NAME=${HOST_LOBBY_NAME}
HOST_LOBBY_DESC=${HOST_LOBBY_DESC}
HOST_LEVEL_TITLE=${HOST_LEVEL_TITLE}
HOST_LEVEL_BARCODE=${HOST_LEVEL_BARCODE}
BOT_NICK=${BOT_NICK}
HOST_HOLD_SEC=${HOST_HOLD_SEC}
HOST_P2P_PORT=${HOST_P2P_PORT}
HOST_MAX_MEMBERS=${HOST_MAX_MEMBERS}
HOST_DISPLAY_PLAYERS=${HOST_DISPLAY_PLAYERS}
HOST_MARK_FULL=${HOST_MARK_FULL}
EOS_DATA_DIR=${DEST}/data
EOS_FORCE_NEW_ACCOUNT=${EOS_FORCE_NEW_ACCOUNT}
# Fusion 0.2.0 LabFusion.dll credentials + P2P socket "Fusion" (baked into binary too)
EOS_DEPLOYMENT_ID=${EOS_DEPLOYMENT_ID:-951363bef61a4b7cbd04902e570f80f1}
EOS_CLIENT_SECRET=${EOS_CLIENT_SECRET:-4HJqOC7+zzdzWw8AsA4yvLe0Ea9CBco8PS+yzW/rhBE}
DOTNET_gcServer=0
EOF
chmod 600 "$DEST/.env"

DOTNET_ROOT_DIR="$(dirname "$(dirname "$DOTNET_BIN")")"
# Prefer /usr/share/dotnet layout when present.
if [[ -x /usr/share/dotnet/dotnet ]]; then
  DOTNET_ROOT_DIR=/usr/share/dotnet
  DOTNET_BIN=/usr/share/dotnet/dotnet
fi

cat >"/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=BONELAB Fusion lobby host (www.bonelab.fun listing)
After=network-online.target
Wants=network-online.target
# Explicitly independent of scraper/ingest — no Requires= on those units.

[Service]
Type=simple
WorkingDirectory=${DEST}
EnvironmentFile=${DEST}/.env
Environment=DOTNET_ROOT=${DOTNET_ROOT_DIR}
Environment=FUSION_HOST_MODE=1
ExecStart=${DOTNET_BIN} ${DEST}/EosJoinProbe.dll --host
Restart=always
RestartSec=12
KillSignal=SIGINT
TimeoutStopSec=25
# Soft caps so a wedged host cannot starve scraper/Postgres.
MemoryMax=256M
CPUQuota=50%

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
systemctl restart "${SERVICE_NAME}.service"
sleep 2
systemctl --no-pager --full status "${SERVICE_NAME}.service" || true

echo
echo "[install-host] OK — 24/7 ${SERVICE_NAME} enabled"
echo "  status: systemctl status ${SERVICE_NAME}"
echo "  logs:   journalctl -u ${SERVICE_NAME} -f"
echo "  env:    ${DEST}/.env"
echo "  data:   ${DEST}/data  (EOS identity — keep across updates)"
echo "  ports:  UDP ${HOST_P2P_PORT}+ (isolated from farm bots on 7777)"
echo "  note:   listing+handshake only; not a playable Unity Fusion host"
