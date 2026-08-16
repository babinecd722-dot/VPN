#!/usr/bin/env bash
# Alma / RHEL / Rocky: build, install to /opt, enable systemd 24/7.
# Usage (as root):
#   POSTGRES_DSN='postgresql://...' ./install-vps.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
DEST="${DEST:-/opt/eos-lobby-scraper}"
SERVICE_NAME="eos-lobby-scraper"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root: sudo POSTGRES_DSN='...' $0" >&2
  exit 1
fi

export PATH="/usr/local/bin:/usr/bin:$PATH"

need_pkg() {
  command -v "$1" >/dev/null 2>&1
}

echo "[install] ensuring packages (git rsync dotnet)..."
if need_pkg dnf; then
  dnf install -y git rsync >/dev/null
  if ! need_pkg dotnet; then
    dnf install -y dotnet-sdk-8.0 || dnf install -y dotnet-sdk-9.0
  fi
elif need_pkg apt-get; then
  apt-get update -y
  apt-get install -y git rsync
  if ! need_pkg dotnet; then
    apt-get install -y dotnet-sdk-8.0 || true
  fi
fi

if ! need_pkg dotnet; then
  echo "[install] installing .NET 8 via Microsoft script..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
  ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
  export DOTNET_ROOT=/usr/share/dotnet
  export PATH="$DOTNET_ROOT:$PATH"
fi

DOTNET_BIN="$(command -v dotnet)"
echo "[install] using $DOTNET_BIN ($("$DOTNET_BIN" --version))"

echo "[install] building Release..."
cd "$ROOT"
"$DOTNET_BIN" build -c Release -v q -p:UseAppHost=false

echo "[install] syncing to $DEST..."
mkdir -p "$DEST"
# Preserve per-server EOS identity + lobby code cache across updates.
if [[ -d "$DEST/data" ]]; then
  rm -rf /tmp/eos-scraper-data-preserve
  cp -a "$DEST/data" /tmp/eos-scraper-data-preserve
fi
if need_pkg rsync; then
  rsync -a --delete --exclude data/ "$ROOT/bin/Release/net8.0/" "$DEST/"
else
  # Keep data/ if present.
  find "$DEST" -mindepth 1 -maxdepth 1 ! -name data -exec rm -rf {} +
  cp -a "$ROOT/bin/Release/net8.0/." "$DEST/"
fi
if [[ -d /tmp/eos-scraper-data-preserve ]]; then
  rm -rf "$DEST/data"
  mv /tmp/eos-scraper-data-preserve "$DEST/data"
  echo "[install] preserved $DEST/data (EOS identity / code cache)"
fi

# Prefer caller-provided DSN; else keep existing; else create template.
if [[ -n "${POSTGRES_DSN:-}" ]]; then
  cat >"$DEST/.env" <<EOF
POSTGRES_DSN=${POSTGRES_DSN}
SCRAPE_INTERVAL_SEC=${SCRAPE_INTERVAL_SEC:-10}
SCRAPE_ONCE=0
FUSION_GAME_NAME=BONELAB
LOADING_SEC=${LOADING_SEC:-3}
CODE_PROBE_BUDGET=${CODE_PROBE_BUDGET:-25}
OFFLINE_MISS_STREAK=${OFFLINE_MISS_STREAK:-1}
COLLAPSE_RATIO=${COLLAPSE_RATIO:-0.35}
GHOST_TTL_SEC=${GHOST_TTL_SEC:-90}
LEASE_HEARTBEAT_SEC=${LEASE_HEARTBEAT_SEC:-35}
ADVISORY_LOCK=${ADVISORY_LOCK:-1}
# 1 = wipe DeviceId and mint a brand-new EOS ProductUserId on next start
EOS_FORCE_NEW_ACCOUNT=${EOS_FORCE_NEW_ACCOUNT:-0}
# Fusion 0.2.1 LabFusion.dll credentials (override any stale 0.2.0 values)
EOS_DEPLOYMENT_ID=${EOS_DEPLOYMENT_ID:-f3fdf691aa6c4004abdb1e19665c1429}
EOS_CLIENT_SECRET=${EOS_CLIENT_SECRET:-SWDxYlWWsEgvmD0o3qAm2RMZoSZzOfYo5yvX/uikH94}
EOF
  chmod 600 "$DEST/.env"
elif [[ -f "$ROOT/.env" ]]; then
  install -m 600 "$ROOT/.env" "$DEST/.env"
elif [[ ! -f "$DEST/.env" ]]; then
  cat >"$DEST/.env" <<'EOF'
POSTGRES_DSN=postgresql://client_writer:CHANGE_ME@127.0.0.1:5432/clientdb
SCRAPE_INTERVAL_SEC=15
SCRAPE_ONCE=0
FUSION_GAME_NAME=BONELAB
LOADING_SEC=3
CODE_PROBE_BUDGET=25
EOF
  chmod 600 "$DEST/.env"
  echo "[install] WARNING: edit $DEST/.env and set POSTGRES_DSN" >&2
fi

cat >"/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=BONELAB Fusion EOS lobby scraper → Postgres
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=${DEST}
EnvironmentFile=${DEST}/.env
Environment=DOTNET_ROOT=$(dirname "$(dirname "$DOTNET_BIN")")
ExecStart=${DOTNET_BIN} ${DEST}/EosLobbyScraper.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
TimeoutStopSec=20

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
# enable --now does NOT reload an already-running unit — force new dll into memory
systemctl restart "${SERVICE_NAME}.service"
sleep 1
systemctl --no-pager --full status "${SERVICE_NAME}.service" || true

echo
echo "[install] OK — 24/7 autostart enabled"
echo "  status: systemctl status ${SERVICE_NAME}"
echo "  logs:   journalctl -u ${SERVICE_NAME} -f"
echo "  env:    ${DEST}/.env"
