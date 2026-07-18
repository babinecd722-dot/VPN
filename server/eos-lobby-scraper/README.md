# EOS Lobby Scraper (Quest Fusion matchmaking)

Headless bot: EOS DeviceId login → `FindLobbies` → parse `LobbyInfo.playerList` → Postgres `client_data`.

Tested live: **163 lobbies / 490 players** in one pass.

## Setup

```bash
cd server/eos-lobby-scraper
cp .env.example .env   # set POSTGRES_DSN
chmod +x run.sh
./run.sh               # loop every 60s
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
