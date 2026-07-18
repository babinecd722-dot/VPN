#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")" && pwd)"
cd "$ROOT"

# Load .env without clobbering vars already set in the environment.
if [[ -f "$ROOT/.env" ]]; then
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    key="${line%%=*}"
    key="${key// /}"
    [[ -z "$key" ]] && continue
    if [[ -z "${!key+x}" ]]; then
      export "$line"
    fi
  done < "$ROOT/.env"
fi

: "${POSTGRES_DSN:?Set POSTGRES_DSN or put it in .env}"

dotnet build -c Release -v q
exec env \
  POSTGRES_DSN="$POSTGRES_DSN" \
  SCRAPE_INTERVAL_SEC="${SCRAPE_INTERVAL_SEC:-15}" \
  SCRAPE_ONCE="${SCRAPE_ONCE:-0}" \
  FUSION_GAME_NAME="${FUSION_GAME_NAME:-BONELAB}" \
  CODE_PROBE_BUDGET="${CODE_PROBE_BUDGET:-25}" \
  LOADING_SEC="${LOADING_SEC:-3}" \
  LOBBY_CODES="${LOBBY_CODES:-}" \
  dotnet "$ROOT/bin/Release/net8.0/EosLobbyScraper.dll"
