#!/usr/bin/env bash
# Install / update player-ingest on VPS (Tracking API for Monster Panel).
# Usage (as root):
#   POSTGRES_DSN='postgresql://client_writer:PASS@127.0.0.1:5432/clientdb' \
#   INGEST_API_KEY='e63d7b2ae9d5006d109712e6c3ea2592611f563e380724de' \
#   ./install-vps.sh
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
DEST="${DEST:-/opt/player-ingest}"
SERVICE_NAME="player-ingest"
KEY="${INGEST_API_KEY:-e63d7b2ae9d5006d109712e6c3ea2592611f563e380724de}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root: sudo POSTGRES_DSN='...' $0" >&2
  exit 1
fi

: "${POSTGRES_DSN:?Set POSTGRES_DSN}"

command -v python3 >/dev/null || { echo "python3 required" >&2; exit 1; }

mkdir -p "$DEST"
cp -f "$ROOT/app.py" "$ROOT/requirements.txt" "$DEST/"
python3 -m venv "$DEST/.venv"
"$DEST/.venv/bin/pip" install -q -U pip
"$DEST/.venv/bin/pip" install -q -r "$DEST/requirements.txt"

cat >"$DEST/.env" <<EOF
POSTGRES_DSN=${POSTGRES_DSN}
INGEST_API_KEY=${KEY}
HOST=0.0.0.0
PORT=8787
GHOST_TTL_SEC=${GHOST_TTL_SEC:-90}
EOF
chmod 600 "$DEST/.env"

cat >"/etc/systemd/system/${SERVICE_NAME}.service" <<EOF
[Unit]
Description=BONELAB player-ingest (Tracking API)
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
WorkingDirectory=${DEST}
EnvironmentFile=${DEST}/.env
ExecStart=${DEST}/.venv/bin/uvicorn app:app --host 0.0.0.0 --port 8787
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
systemctl restart "${SERVICE_NAME}.service"
sleep 1
systemctl --no-pager --full status "${SERVICE_NAME}.service" || true
curl -sf http://127.0.0.1:8787/health && echo || echo "health check failed"
echo "[install] OK — journalctl -u ${SERVICE_NAME} -f"
