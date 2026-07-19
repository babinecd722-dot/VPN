# Player Ingest + Tracking API

Mod → this service → PostgreSQL `client_data`.

## Endpoints

- `GET /health` — liveness + DB ping
- `POST /v1/players` — insert-only player rows (Bearer)
- `POST /v1/track` — batch presence by pid list (Bearer, **10s cooldown**)

Track body:

```json
{ "pids": ["0002…", "0002…"] }
```

Response fields per player: `status`, `server`, `server_map`, `language`, `lobby_code`,
`last_seen_at`, `session_sec`, `online`, `found`.

## Run on VPS

```bash
cd server/player-ingest
cp .env.example .env   # set POSTGRES_DSN + INGEST_API_KEY
python3 -m venv .venv && . .venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8787
```
