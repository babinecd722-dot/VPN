# Lang-farm bots (BONELAB Fusion)

Headless EOS join bots (`bonelab.fun`) that collect voice → language into Postgres.

Runtime lives on the agent host under `/tmp/lang-farm`. This folder mirrors the
production bot sources for version control.

## Key hardening (2026-07-19 / 2026-07-20)

- Skip `Connect.Logout` **and** `Platform.Release` on Linux — EOSSDK atexit SIGSEGVs (exit 139)
- Clean cycle end via libc `_exit` (`SafeExit`) so shutdown handlers never run
- Keep one EOS login across multiple lobbies per process (no early teardown after first hit)
- Dead-lobby abort only when **no P2P**; stay full `LISTEN_SEC` if packets flow
- Confident language DB writes (`ok=1`) count as success
- Atomic lobby claims (`FileMode.CreateNew`)
- No fake SmallID from LobbyInfo index (pending pid map)
- `register_bot.py` clears `server`/`lobby_code` on OFFLINE

## Run bots (agent host only)

```bash
cd /tmp/lang-farm
POSTGRES_DSN=... BOT_NICK=bonelab.fun bash orchestrator/run_parallel.sh
```

Do **not** run farm bots on the Tracking VPS (DeviceId / port fights).

## VPS lobby host 24/7 (`www.bonelab.fun`)

Isolated systemd unit — separate from scraper + player-ingest:

| | path |
|---|---|
| Install | `/opt/fusion-lobby-host` |
| Identity | `/opt/fusion-lobby-host/data` |
| Env | `/opt/fusion-lobby-host/.env` |
| Unit | `fusion-lobby-host.service` |
| UDP | `17877+` (not 7777) |

### Deploy (Maze / root)

```bash
cd /
curl -fsSL 'https://raw.githubusercontent.com/babinecd722-dot/VPN/cursor/monsterpanel-tracking-eaa4/server/lang-farm/bootstrap-host-alma.sh' | sudo bash
```

```bash
systemctl status fusion-lobby-host
journalctl -u fusion-lobby-host -f
```

Defaults: lobby name/desc `www·bonelab·fun` (U+00B7 middle-dot — LinkFilter bypass; ZWSP-after-dot now shows "?" on Quest), nick `coolguy`, map Halfway Park, `HOST_HOLD_SEC=0` (forever), soft caps `MemoryMax=256M` / `CPUQuota=50%`.

`HOST_DISPLAY_PLAYERS=7` pads **LobbyInfo only** (browser count looks full-ish, `Full=False` so Find still returns it). Zero extra processes/CPU — not real bots.

### What it does / does not

Works: public Find listing, JoinLobby, P2P handshake (ConnectionResponse + SceneLoad).  
Does not: playable Unity Fusion world (needs a real BONELAB+Fusion host).

Requires EOS SDK 1.15.5 ApiVersion patches: CreateLobby=8, AddAttribute=1 (`eos-join-probe/patches/`).

## Fusion 0.1.0+ / 0.2.x credentials

Checkerb0ard release DLLs rotate EOS `DeploymentId` + `ClientSecret` (trust the DLL,
not older git tags). Defaults in `EosIdentity` / host / bots / scraper track **0.2.1**.

- **0.2.0+** P2P socket is `Fusion` (not `FusionSocket`). **0.2.1** rotates DeploymentId/ClientSecret again.
- P2P wire still prefixes every datagram with `KindSingle=0` / `KindFragment=1`
  (`FusionP2PWire`). Without this, clients mis-parse `ConnectionRequest` (tag=1)
  as a fragment and joins die.
