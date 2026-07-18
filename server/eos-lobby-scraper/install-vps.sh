#!/usr/bin/env bash
# Install Release build to /opt/eos-lobby-scraper and enable systemd.
# Run as root on the VPS after copying this folder.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
DEST="${DEST:-/opt/eos-lobby-scraper}"

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)" >&2
  exit 1
fi

command -v dotnet >/dev/null || {
  echo "dotnet SDK/runtime required" >&2
  exit 1
}

dotnet build -c Release -v q -p:UseAppHost=false
mkdir -p "$DEST"
rsync -a --delete "$ROOT/bin/Release/net8.0/" "$DEST/"
if [[ -f "$ROOT/.env" ]]; then
  install -m 600 "$ROOT/.env" "$DEST/.env"
elif [[ ! -f "$DEST/.env" ]]; then
  cat >"$DEST/.env" <<'EOF'
POSTGRES_DSN=postgresql://client_writer:CHANGE_ME@127.0.0.1:5432/clientdb
SCRAPE_INTERVAL_SEC=60
SCRAPE_ONCE=0
FUSION_GAME_NAME=BONELAB
EOF
  chmod 600 "$DEST/.env"
  echo "Edit $DEST/.env and set POSTGRES_DSN" >&2
fi

sed "s|/opt/eos-lobby-scraper|$DEST|g" "$ROOT/eos-lobby-scraper.service" \
  >/etc/systemd/system/eos-lobby-scraper.service
systemctl daemon-reload
systemctl enable --now eos-lobby-scraper.service
systemctl --no-pager status eos-lobby-scraper.service || true
echo "Installed. Logs: journalctl -u eos-lobby-scraper -f"
