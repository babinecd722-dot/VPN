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

## Experimental host (`--host`)

Headless EOS CreateLobby + P2P handshake (ReallyWorld / ADMIN). Load is tiny
(~0.1–1% CPU, ~80 MB RSS). Not a Unity game simulation.

```bash
EOS_FORCE_NEW_ACCOUNT=0 BOT_NICK=ADMIN \
EOS_DATA_DIR=/tmp/lang-farm/state/host-reallyworld \
HOST_LOBBY_NAME=ReallyWorld HOST_LEVEL_TITLE='Halfway Park' \
HOST_LOBBY_DESC='Официальный сервер от www.bonelab.fun' \
HOST_HOLD_SEC=7200 \
dotnet eos-join-probe/bin/Release/net8.0/EosJoinProbe.dll --host
```

What works today:
- Public Find / browser listing (LobbyInfo, code, Halfway Park)
- EOS JoinLobby + P2P Accept (ForceRelays)
- Fusion ConnectionResponse + SceneLoad + empty DynamicsAssignment

What does **not** work without a real BONELAB+Fusion (Unity) host:
- Playable world / avatars / props / voice as a game session
- Clients time out after handshake (no pose/entity sync)

Requires EOS SDK 1.15.5 ApiVersion patches: CreateLobby=8, AddAttribute=1 (see `patches/`).
Join-test pin: `FORCE_LOBBY_CODE=<code> dotnet …/EosJoinProbe.dll`.
