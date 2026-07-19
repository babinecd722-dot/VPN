# Lang-farm bots (BONELAB Fusion)

Headless EOS join bots (`bonelab.fun`) that collect voice → language into Postgres.

Runtime lives on the agent host under `/tmp/lang-farm`. This folder mirrors the
production bot sources for version control.

## Key hardening (2026-07-19)

- Skip `Connect.Logout` on Linux EOSSDK (prevents SIGSEGV / exit 139 after success)
- Dead-lobby abort only when **no P2P**; stay full `LISTEN_SEC` if packets flow
- SUCCESS exit only on confident language DB writes (`ok=1`)
- Atomic lobby claims (`FileMode.CreateNew`)
- No fake SmallID from LobbyInfo index (pending pid map)
- `register_bot.py` clears `server`/`lobby_code` on OFFLINE

## Run

```bash
cd /tmp/lang-farm
POSTGRES_DSN=... BOT_NICK=bonelab.fun bash orchestrator/run_parallel.sh
```
