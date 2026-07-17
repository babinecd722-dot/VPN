# Player Ingest API

Mod → this service → PostgreSQL `client_data`. Postgres password stays on the VPS only.

## Run on VPS

```bash
cd server/player-ingest
cp .env.example .env
# edit POSTGRES_DSN + INGEST_API_KEY
python3 -m venv .venv && . .venv/bin/activate
pip install -r requirements.txt
uvicorn app:app --host 0.0.0.0 --port 8787
```

Or Docker:

```bash
docker build -t player-ingest .
docker run -d --name player-ingest --env-file .env -p 8787:8787 player-ingest
```

Open firewall for `8787` (or put nginx/caddy in front with TLS).

## GitHub Secrets (optional deploy)

- `POSTGRES_DSN`
- `INGEST_API_KEY`

## Endpoints

- `GET /health` → `{ "ok": true, "database": "connected" }`
- `POST /v1/players` + `Authorization: Bearer <INGEST_API_KEY>`
  ```json
  { "players": [ { "name": "Nick", "pid": "eos-product-user-id" } ] }
  ```

## Optional DB hardening (run as superuser)

```sql
CREATE UNIQUE INDEX IF NOT EXISTS client_data_pid_uidx ON client_data (pid);
```
