#!/usr/bin/env bash
# Launch farm bots without inheriting visual-host env (BOT_NICK=coolguy etc.).
set -euo pipefail
cd /tmp/lang-farm
unset HOST_LOBBY_NAME HOST_LOBBY_DESC HOST_P2P_PORT HOST_HOLD_SEC HOST_DISPLAY_PLAYERS
unset HOST_MAX_MEMBERS HOST_LEVEL_TITLE HOST_LEVEL_BARCODE
export FUSION_HOST_MODE=0
export BOT_NICK=$'bonelab\u00b7fun'
export SKIP_LOBBY_NAMES=$'www\u00b7bonelab\u00b7fun,www.bonelab.fun'
exec bash orchestrator/run_parallel.sh
