#!/usr/bin/env bash
set -u

cd "$(dirname "$0")"
mkdir -p data logs

echo "[$(date -Iseconds)] watchdog started" >> logs/watchdog.log

while true; do
  echo "[$(date -Iseconds)] starting bot" >> logs/watchdog.log
  python3 main.py
  code=$?
  echo "[$(date -Iseconds)] bot exited code=$code, restart in 5s" >> logs/watchdog.log
  sleep 5
done
