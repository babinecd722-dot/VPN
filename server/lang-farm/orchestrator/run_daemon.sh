#!/usr/bin/env bash
# Background lang-farm daemon:
#  - reuses bot0..bot9 identities (EOS_FORCE_NEW_ACCOUNT=0)
#  - nick = primex-host.online
#  - writes language to client_data
#  - between bot cycles runs eos-lobby-scraper (player search)
set -euo pipefail
ROOT=/tmp/lang-farm
BOT_DLL="$ROOT/eos-join-probe/bin/Release/net8.0/EosJoinProbe.dll"
RESULTS="$ROOT/results/languages.txt"
LOGS="$ROOT/logs"
N="${BOTS:-10}"
LISTEN="${LISTEN_SEC:-30}"
JOINS="${MAX_JOIN_TRIES:-4}"
SCRAPER_SEC="${SCRAPER_SEC:-90}"
# ZWSP (U+200B) after '.' — same censor bypass as host lobby www.​bonelab.​fun
NICK="${BOT_NICK:-bonelab.$'\u200b'fun}"

mkdir -p "$LOGS" "$ROOT/results" "$ROOT/state"
[[ -f "$RESULTS" ]] || printf '%s\n' '# utc	pid	user	lang	conf	ok	sid	lobby	wav	text' > "$RESULTS"

if [[ -f /tmp/lang-farm/state/postgres_dsn.env ]]; then
  export POSTGRES_DSN="$(cat /tmp/lang-farm/state/postgres_dsn.env)"
elif [[ -f /tmp/eos-run/.env ]]; then
  set -a; # shellcheck disable=SC1091
  source /tmp/eos-run/.env; set +a
fi
: "${POSTGRES_DSN:?POSTGRES_DSN required}"
printf 'POSTGRES_DSN=%s\n' "$POSTGRES_DSN" > /tmp/eos-run/.env
chmod 600 /tmp/eos-run/.env

# ensure LID
if ! curl -sf http://127.0.0.1:8091/health >/dev/null; then
  echo "[daemon] starting LID…"
  nohup env WHISPER_MODEL=base MIN_CONFIDENCE=0.55 MIN_SPEECH_SEC=1.0 \
    python3 "$ROOT/langdetect/detect_service.py" --host 127.0.0.1 --port 8091 --preload \
    >>"$LOGS/detect_service.log" 2>&1 &
  for i in $(seq 1 60); do curl -sf http://127.0.0.1:8091/health >/dev/null && break; sleep 2; done
fi

stop_scraper() {
  pkill -f 'EosLobbyScraper.dll' 2>/dev/null || true
  sleep 1
}

start_scraper() {
  if pgrep -f 'EosLobbyScraper.dll' >/dev/null; then
    echo "[daemon] scraper already running"
    return
  fi
  echo "[daemon] starting scraper (reuse account, FORCE_NEW=0)"
  nohup env EOS_FORCE_NEW_ACCOUNT=0 POSTGRES_DSN="$POSTGRES_DSN" \
    dotnet /tmp/eos-run/EosLobbyScraper.dll \
    >>/tmp/eos-scraper-loop.log 2>&1 &
  echo "[daemon] scraper pid=$!"
}

cycle=0
echo "[daemon] start N=$N nick=$NICK reuse=1 listen=${LISTEN}s scraper_gap=${SCRAPER_SEC}s"
while true; do
  cycle=$((cycle+1))
  echo "[daemon] ===== farm cycle $cycle ====="
  stop_scraper

  for i in $(seq 0 $((N-1))); do
    DATA="$ROOT/state/bot$i"
    mkdir -p "$DATA"
    # DO NOT wipe identity — reuse
    LOG="$LOGS/daemon_bot${i}.log"
    echo "[daemon] bot$i reuse data=$DATA"
    set +e
    LISTEN_SEC="$LISTEN" MAX_JOIN_TRIES="$JOINS" \
    EOS_FORCE_NEW_ACCOUNT=0 EOS_DATA_DIR="$DATA" \
    BOT_NICK="$NICK" POSTGRES_DSN="$POSTGRES_DSN" \
    RESULTS_TXT="$RESULTS" SESSION_DIR="$ROOT/results/session" \
    DETECT_URL=http://127.0.0.1:8091/detect MIN_CONFIDENCE=0.55 \
    MIN_VOICE_SAMPLES=96000 \
    dotnet "$BOT_DLL" >>"$LOG" 2>&1
    RC=$?
    set -e
    PUID=$(grep -oP 'logged in as \K[0-9a-f]+' "$LOG" | tail -1 || true)
    echo "[daemon] bot$i rc=$RC puid=${PUID:-?}"
    if [[ -n "${PUID:-}" ]]; then
      python3 "$ROOT/orchestrator/register_bot.py" "$PUID" "$NICK" || true
    fi
    sleep 1
  done

  echo "[daemon] farm cycle $cycle done — running scraper ${SCRAPER_SEC}s"
  start_scraper
  sleep "$SCRAPER_SEC"
done
