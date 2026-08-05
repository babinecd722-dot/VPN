#!/usr/bin/env bash
# 10 PARALLEL lang-farm bots.
# Isolation trick: each bot gets its own HOME so EOS DeviceId/keychain do not collide.
# Reuse identities; mint only when that bot cannot login.
set -euo pipefail
# VPS install uses FARM_ROOT=/opt/lang-farm; agent host defaults to /tmp/lang-farm.
ROOT="${FARM_ROOT:-/tmp/lang-farm}"
BOT_DLL="${BOT_DLL:-$ROOT/bin/EosJoinProbe.dll}"
# Dev/agent layout keeps the DLL under eos-join-probe/bin/Release/...
if [[ ! -f "$BOT_DLL" && -f "$ROOT/eos-join-probe/bin/Release/net8.0/EosJoinProbe.dll" ]]; then
  BOT_DLL="$ROOT/eos-join-probe/bin/Release/net8.0/EosJoinProbe.dll"
fi
RESULTS="${RESULTS_TXT:-$ROOT/results/languages.txt}"
LOGS="${LOGS:-$ROOT/logs}"
N="${BOTS:-10}"
LISTEN="${LISTEN_SEC:-55}"
JOINS="${MAX_JOIN_TRIES:-8}"
# U+00B7 MIDDLE DOT — same censor bypass as host lobby www·bonelab·fun
DEFAULT_NICK=$'bonelab\u00b7fun'
NICK="${BOT_NICK:-$DEFAULT_NICK}"
LOOP_SLEEP="${LOOP_SLEEP:-2}"
# Dead lobby only (no P2P). With P2P traffic bots now wait full LISTEN_SEC.
NO_VOICE_ABORT="${NO_VOICE_ABORT_SEC:-18}"

# Never inherit visual-host launcher env (BOT_NICK=coolguy / FUSION_HOST_MODE=1 leak).
unset FUSION_HOST_MODE HOST_LOBBY_NAME HOST_LOBBY_DESC HOST_P2P_PORT HOST_HOLD_SEC
unset HOST_DISPLAY_PLAYERS HOST_MAX_MEMBERS HOST_LEVEL_TITLE HOST_LEVEL_BARCODE
export FUSION_HOST_MODE=0
# Skip our own www·bonelab·fun host (comma-separated overrides via SKIP_LOBBY_NAMES / SKIP_LOBBY_CODES).
export SKIP_LOBBY_NAMES="${SKIP_LOBBY_NAMES:-www·bonelab·fun,www.bonelab.fun}"
export HOST_LOBBY_CODE_FILE="${HOST_LOBBY_CODE_FILE:-$ROOT/state/host_lobby_code.txt}"

mkdir -p "$LOGS" "$ROOT/results" "$ROOT/homes" "$ROOT/state"
[[ -f "$RESULTS" ]] || printf '%s\n' '# utc	pid	user	lang	conf	ok	sid	lobby	wav	text' > "$RESULTS"

if [[ -f "$ROOT/state/postgres_dsn.env" ]]; then
  export POSTGRES_DSN="$(cat "$ROOT/state/postgres_dsn.env")"
fi
: "${POSTGRES_DSN:?POSTGRES_DSN required}"
# psycopg2: prefer system / venv; bots isolate HOME for EOS DeviceId.
export PYTHONPATH="${FARM_PYTHONPATH:-${PYTHONPATH:-}}"

# LID (optional — set START_DETECT=0 to skip; join/P2P farm still works)
START_DETECT="${START_DETECT:-1}"
DETECT_PY="${DETECT_PY:-$ROOT/langdetect/detect_service.py}"
if [[ "$START_DETECT" == "1" ]] && [[ -f "$DETECT_PY" ]]; then
  if ! curl -sf http://127.0.0.1:8091/health >/dev/null; then
    nohup env WHISPER_MODEL="${WHISPER_MODEL:-base}" MIN_CONFIDENCE=0.55 \
      MIN_SPEECH_SEC=1.8 MIN_SPEECH_SEC_NON_EN=2.8 ECAPA_MARGIN=0.18 MMS_MARGIN=0.10 \
      python3 "$DETECT_PY" --host 127.0.0.1 --port 8091 --preload \
      >>"$LOGS/detect_service.log" 2>&1 &
    for _ in $(seq 1 90); do curl -sf http://127.0.0.1:8091/health >/dev/null && break; sleep 1; done
  fi
fi

# Presence scraper runs on the VPS (systemd eos-lobby-scraper).
# Do NOT start a second copy here — it fights for the same DB advisory lock.
start_scraper() {
  echo "[parallel] skip local scraper — use VPS journalctl -u eos-lobby-scraper"
}

bot_worker() {
  local i="$1"
  local HOME_DIR="$ROOT/homes/bot$i"
  local DATA="$ROOT/state/bot$i"
  local LOG="$LOGS/parallel_bot${i}.log"
  mkdir -p "$HOME_DIR/.cache" "$HOME_DIR/.local/share" "$DATA"
  # seed empty XDG layout
  mkdir -p "$HOME_DIR/.cache" "$HOME_DIR/.local/share"

  local force=0
  if [[ ! -f "$DATA/eos-identity.json" ]]; then
    force=1
    echo "[bot$i] no identity yet — first mint under isolated HOME" | tee -a "$LOG"
  fi

  while true; do
    echo "[bot$i] $(date -u +%H:%M:%S) start force=$force home=$HOME_DIR" >>"$LOG"
    set +e
    HOME="$HOME_DIR" \
    XDG_CACHE_HOME="$HOME_DIR/.cache" \
    XDG_DATA_HOME="$HOME_DIR/.local/share" \
    LISTEN_SEC="$LISTEN" MAX_JOIN_TRIES="$JOINS" NO_VOICE_ABORT_SEC="$NO_VOICE_ABORT" \
    EOS_FORCE_NEW_ACCOUNT="$force" EOS_DATA_DIR="$DATA" \
    BOT_NICK="$NICK" POSTGRES_DSN="$POSTGRES_DSN" \
    FUSION_HOST_MODE=0 \
    SKIP_LOBBY_NAMES="$SKIP_LOBBY_NAMES" \
    HOST_LOBBY_CODE_FILE="$HOST_LOBBY_CODE_FILE" \
    RESULTS_TXT="$RESULTS" SESSION_DIR="$ROOT/results/session" \
    LOBBY_CLAIM_DIR="$ROOT/state/lobby_claims" \
    DETECT_URL="${DETECT_URL:-http://127.0.0.1:8091/detect}" MIN_CONFIDENCE=0.55 \
    MIN_VOICE_SAMPLES=72000 \
    ${DOTNET_BIN:-dotnet} "$BOT_DLL" >>"$LOG" 2>&1
    local rc=$?
    set -e

    local puid
    puid=$(grep -oP 'logged in as \K[0-9a-f]+' "$LOG" | tail -1 || true)
    echo "[bot$i] exit=$rc puid=${puid:-?}" | tee -a "$LOG"
    if [[ -n "${puid:-}" ]]; then
      # Always force OFFLINE + clear lobby fields (never leave IN GAME ghosts).
      python3 "$ROOT/orchestrator/register_bot.py" "$puid" "$NICK" "OFFLINE" >>"$LOG" 2>&1 || true
      force=0  # after first successful login, always reuse
    else
      # login failed hard — allow remint next loop
      force=1
    fi
    # 0 = clean hard-exit after farm cycle. 4/5 = no/weak detections (normal).
    # 139 / >128 = native crash — longer backoff so EOS/DeviceId can settle.
    if [[ "$rc" -eq 139 || "$rc" -gt 128 ]]; then
      echo "[bot$i] native crash — backoff" | tee -a "$LOG"
      sleep $((LOOP_SLEEP + 8))
    elif [[ "$rc" -eq 0 ]]; then
      sleep "$LOOP_SLEEP"
    else
      sleep $((LOOP_SLEEP + 1))
    fi
  done
}

echo "[parallel] launching $N bots nick=$NICK listen=${LISTEN}s"
PIDS=()
for i in $(seq 0 $((N-1))); do
  bot_worker "$i" &
  PIDS+=($!)
  echo "[parallel] bot$i worker pid=${PIDS[-1]}"
  sleep 0.4
done

start_scraper

echo "[parallel] all workers up: ${PIDS[*]}"
echo "[parallel] pids file → $LOGS/parallel.pids"
printf '%s\n' "${PIDS[@]}" > "$LOGS/parallel.pids"

# wait forever (or until signal)
trap 'echo "[parallel] stopping…"; kill "${PIDS[@]}" 2>/dev/null || true; exit 0' INT TERM
wait
