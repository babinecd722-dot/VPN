# EOS Lobby Scraper (Quest Fusion matchmaking)

Headless bot: EOS DeviceId login → sharded `FindLobbies` (privacy × Full, auto VersionMinor if a shard hits EOS max 200, + LobbyCode cache) → sync Postgres `client_data` presence:

| `status` | meaning |
|---|---|
| `OFFLINE` | not in any Fusion lobby `playerList` |
| `LOADING` | just appeared in a lobby list (~`LOADING_SEC`, default 3s) |
| `IN GAME` | still in a lobby after loading window |

| column | active (`LOADING` / `IN GAME`) | `OFFLINE` |
|---|---|---|
| `server` | `lobbyName` | `NULL` |
| `server_map` | `levelTitle` | `NULL` |
| `lobby_code` | current lobby `LobbyCode`, or `NULL` if the lobby has none | `NULL` |

Coverage: privacy buckets `PUBLIC/FRIENDS/PRIVATE/LOCKED` × `Full` true/false (+ catch-all without `Full`). If any Find returns `200` (EOS cap), that shard expands over `VersionMajor=1` + `VersionMinor=0..SHARD_VERSION_MINOR_MAX`. Private notes: Fusion hides PRIVATE/LOCKED from the browser; we still Find them. Codes are 8×`[A-Z0-9]` — not brute-forceable; harvested codes are re-probed (`CODE_PROBE_BUDGET`).

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
| `EOS_*` | Fusion Quest credentials (defaults match LabFusion **0.2.1** DLL — DeploymentId/ClientSecret rotated vs 0.2.0) |

## Notes

- Does **not** join lobbies; reads matchmaking metadata only.
- Uses Connect **DeviceId** (separate ProductUserId), not Oculus.
- **Per-server account:** `data/eos-identity.json` + isolated `CacheDirectory`. New host (`/etc/machine-id`) → new account. `InvalidAuth` → `DeleteDeviceId` + mint identity + `CreateUser` (not a re-login of the dead session).
- `EOS_FORCE_NEW_ACCOUNT=1` forces a fresh ProductUserId on next start.
- Managed EOS bindings are under `third_party/` (from LabFusion.dll); ApiVersions patched for Linux SDK 1.15.5.
