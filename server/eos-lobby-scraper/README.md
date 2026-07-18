# EOS Lobby Scraper (Quest Fusion matchmaking)

Headless bot: EOS DeviceId login → `FindLobbies` → parse `LobbyInfo.playerList` → Postgres `client_data`.

Tested live: **163 lobbies / 490 players** in one pass.

## Setup (VPS)

```bash
cd server/eos-lobby-scraper
# native SDK (Linux x64)
# already in native/libEOSSDK-Linux-Shipping.so (EOS 1.15.5)

dotnet build -c Release
cd bin/Release/net8.0
SCRAPE_ONCE=1 \
POSTGRES_DSN='Host=127.0.0.1;Port=5432;Database=clientdb;Username=client_writer;Password=YOUR_PASSWORD;SSL Mode=Prefer' \
dotnet EosLobbyScraper.dll
```

Loop every 60s (default):

```bash
POSTGRES_DSN='...' SCRAPE_INTERVAL_SEC=60 dotnet EosLobbyScraper.dll
```

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
