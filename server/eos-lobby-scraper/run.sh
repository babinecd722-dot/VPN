#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

if [[ -f "$ROOT/.env" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "$ROOT/.env"
  set +a
fi

: "${POSTGRES_DSN:?Set POSTGRES_DSN or put it in .env}"

dotnet build -c Release -v q
exec env \
  POSTGRES_DSN="$POSTGRES_DSN" \
  SCRAPE_INTERVAL_SEC="${SCRAPE_INTERVAL_SEC:-60}" \
  SCRAPE_ONCE="${SCRAPE_ONCE:-0}" \
  FUSION_GAME_NAME="${FUSION_GAME_NAME:-BONELAB}" \
  dotnet "$ROOT/bin/Release/net8.0/EosLobbyScraper.dll"
