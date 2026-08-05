#!/usr/bin/env bash
# Install lang-farm bots → /opt/lang-farm + systemd 24/7 (lang-farm-bots.service).
# Expects eos-join-probe already built (Release) under $ROOT/eos-join-probe or $BUILD_ROOT.
#
# Required: POSTGRES_DSN
# Optional: BOTS=10 START_DETECT=0|1 BOT_NICK=bonelab·fun
set -euo pipefail

ROOT_SRC="$(cd "$(dirname "$0")" && pwd)"
PROBE="${BUILD_ROOT:-$ROOT_SRC/eos-join-probe}"
DEST="${FARM_DEST:-/opt/lang-farm}"
SERVICE_NAME="${BOTS_SERVICE_NAME:-lang-farm-bots}"
HOST_CODE_FILE="${HOST_LOBBY_CODE_FILE:-$DEST/state/host_lobby_code.txt}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root: sudo POSTGRES_DSN='...' $0" >&2
  exit 1
fi

: "${POSTGRES_DSN:?POSTGRES_DSN required (postgresql://...)}"

if [[ ! -f "$PROBE/bin/Release/net8.0/EosJoinProbe.dll" ]]; then
  echo "Missing built DLL: $PROBE/bin/Release/net8.0/EosJoinProbe.dll — build first" >&2
  exit 1
fi

export PATH="/usr/local/bin:/usr/bin:$PATH"
need_pkg() { command -v "$1" >/dev/null 2>&1; }

echo "[install-bots] ensuring packages..."
if need_pkg dnf; then
  dnf install -y rsync python3 python3-pip curl >/dev/null || true
elif need_pkg apt-get; then
  apt-get update -y >/dev/null
  apt-get install -y rsync python3 python3-pip python3-venv curl >/dev/null
fi

if ! need_pkg dotnet; then
  echo "[install-bots] installing .NET 8..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
fi
DOTNET_BIN="$(command -v dotnet)"
if [[ -x /usr/share/dotnet/dotnet ]]; then
  DOTNET_BIN=/usr/share/dotnet/dotnet
fi

echo "[install-bots] syncing to $DEST..."
mkdir -p "$DEST"/{bin,orchestrator,langdetect,state,homes,results,logs}
# Preserve identities / claims / dsn across reinstalls.
rsync -a --delete \
  --exclude state/ --exclude homes/ --exclude results/ --exclude logs/ --exclude .env \
  "$PROBE/bin/Release/net8.0/" "$DEST/bin/"
rsync -a "$ROOT_SRC/orchestrator/" "$DEST/orchestrator/"
if [[ -d "$ROOT_SRC/langdetect" ]]; then
  rsync -a "$ROOT_SRC/langdetect/" "$DEST/langdetect/"
fi
chmod +x "$DEST/orchestrator/"*.sh "$DEST/orchestrator/"*.py 2>/dev/null || true

# Python deps for register_bot / update_language (+ optional detect)
VENV="$DEST/venv"
if [[ ! -x "$VENV/bin/python" ]]; then
  python3 -m venv "$VENV" || true
fi
if [[ -x "$VENV/bin/pip" ]]; then
  "$VENV/bin/pip" -q install --upgrade pip
  "$VENV/bin/pip" -q install 'psycopg2-binary>=2.9'
  if [[ "${START_DETECT:-0}" == "1" ]]; then
    echo "[install-bots] installing detect stack (heavy — first boot downloads models)..."
    "$VENV/bin/pip" -q install flask numpy torch torchaudio --index-url https://download.pytorch.org/whl/cpu || \
      "$VENV/bin/pip" -q install flask numpy
    "$VENV/bin/pip" -q install faster-whisper speechbrain transformers || true
  fi
fi

DEFAULT_NICK=$'bonelab\u00b7fun'
BOT_NICK="${BOT_NICK:-$DEFAULT_NICK}"
BOTS_N="${BOTS:-10}"
START_DETECT="${START_DETECT:-0}"

printf '%s\n' "$POSTGRES_DSN" >"$DEST/state/postgres_dsn.env"
chmod 600 "$DEST/state/postgres_dsn.env"

cat >"$DEST/.env" <<EOF
FARM_ROOT=${DEST}
FUSION_HOST_MODE=0
FUSION_GAME_NAME=BONELAB
BOT_NICK=${BOT_NICK}
BOTS=${BOTS_N}
LISTEN_SEC=${LISTEN_SEC:-55}
START_DETECT=${START_DETECT}
POSTGRES_DSN=${POSTGRES_DSN}
HOST_LOBBY_CODE_FILE=${HOST_CODE_FILE}
SKIP_LOBBY_NAMES=${SKIP_LOBBY_NAMES:-www·bonelab·fun,www.bonelab.fun}
EOS_DEPLOYMENT_ID=${EOS_DEPLOYMENT_ID:-f3fdf691aa6c4004abdb1e19665c1429}
EOS_CLIENT_SECRET=${EOS_CLIENT_SECRET:-SWDxYlWWsEgvmD0o3qAm2RMZoSZzOfYo5yvX/uikH94}
DOTNET_BIN=${DOTNET_BIN}
DOTNET_ROOT=$(dirname "$(dirname "$DOTNET_BIN")")
DOTNET_gcServer=0
FARM_PYTHONPATH=${VENV}/lib/python3.12/site-packages:${VENV}/lib/python3.11/site-packages:${VENV}/lib/python3.10/site-packages
PATH=${VENV}/bin:/usr/local/bin:/usr/bin
EOF
chmod 600 "$DEST/.env"

# Wrapper so EnvironmentFile vars are exported into run_parallel.
cat >"$DEST/run_bots.sh" <<EOF
#!/usr/bin/env bash
set -euo pipefail
set -a
# shellcheck disable=SC1091
source ${DEST}/.env
set +a
export FARM_ROOT=${DEST}
export BOT_DLL=${DEST}/bin/EosJoinProbe.dll
export HOST_LOBBY_CODE_FILE=${HOST_CODE_FILE}
exec bash ${DEST}/orchestrator/run_parallel.sh
EOF
chmod +x "$DEST/run_bots.sh"

cat >"/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=BONELAB lang-farm bots (join foreign lobbies)
After=network-online.target fusion-lobby-host.service
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=${DEST}
EnvironmentFile=${DEST}/.env
ExecStart=${DEST}/run_bots.sh
Restart=always
RestartSec=15
KillSignal=SIGINT
TimeoutStopSec=40
# Soft caps — detect (if enabled) is heavier; bump MemoryMax if START_DETECT=1.
MemoryMax=${BOTS_MEMORY_MAX:-2G}
CPUQuota=${BOTS_CPU_QUOTA:-200%}

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
systemctl restart "${SERVICE_NAME}.service"
sleep 2
systemctl --no-pager --full status "${SERVICE_NAME}.service" || true

echo
echo "[install-bots] OK — 24/7 ${SERVICE_NAME}"
echo "  status: systemctl status ${SERVICE_NAME}"
echo "  logs:   journalctl -u ${SERVICE_NAME} -f"
echo "  env:    ${DEST}/.env"
echo "  state:  ${DEST}/state  (identities — keep across updates)"
echo "  detect: START_DETECT=${START_DETECT} (0=join-only, 1=LID stack)"
