# EOS Lobby Scraper (Quest Fusion matchmaking)

Headless bot: EOS DeviceId login → bucketed `FindLobbies` (public/friends + private + locked + LobbyCode cache) → sync Postgres `client_data` presence:

| `status` | meaning |
|---|---|
| `OFFLINE` | not in any Fusion lobby `playerList` |
| `LOADING` | just appeared in a lobby list (~`LOADING_SEC`, default 3s) |
| `IN GAME` | still in a lobby after loading window |

| column | active (`LOADING` / `IN GAME`) | `OFFLINE` |
|---|---|---|
| `server` | `lobbyName` | `NULL` |
| `server_map` | `levelTitle` | `NULL` |

Private notes: Fusion hides `Privacy=PRIVATE/LOCKED` from the public browser, but those lobbies stay searchable by attribute / by `LobbyCode`. Codes are 8×`[A-Z0-9]` — not brute-forceable; we harvest codes from seen lobbies and re-probe them (`CODE_PROBE_BUDGET`).

## Setup

```bash
cd server/eos-lobby-scraper
cp .env.example .env   # set POSTGRES_DSN
chmod +x run.sh
./run.sh               # loop every 15s (pause between cycles)
```

One-shot:

```bash
SCRAPE_ONCE=1 ./run.sh
```

### VPS (systemd)

```bash
# copy this folder to the VPS, then:
sudo ./install-vps.sh
# logs: journalctl -u eos-lobby-scraper -f
```

`install-vps.sh` installs Release build to `/opt/eos-lobby-scraper` and enables `eos-lobby-scraper.service`.

## Env

| Var | Default |
|---|---|
| `POSTGRES_DSN` | **required** — `postgresql://` URI or Npgsql key=value |
| `SCRAPE_INTERVAL_SEC` | `60` |
| `SCRAPE_ONCE` | `0` (`1` = single pass) |
| `FUSION_GAME_NAME` | `BONELAB` |
| `EOS_*` | Fusion Quest credentials (defaults match LabFusion 1.14.2) |

## Notes

- Does **not** join lobbies; reads matchmaking metadata only.
- Uses Connect **DeviceId** (separate ProductUserId), not Oculus.
- Managed EOS bindings are under `third_party/` (from LabFusion.dll); ApiVersions patched for Linux SDK 1.15.5.
