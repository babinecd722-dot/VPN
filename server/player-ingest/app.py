"""Player ingest + tracking API: mod → HTTP → PostgreSQL."""

from __future__ import annotations

import logging
import re
import time
from contextlib import asynccontextmanager
from datetime import datetime, timedelta, timezone
from typing import Any

import psycopg
from fastapi import Depends, FastAPI, Header, HTTPException, status
from psycopg.rows import dict_row
from psycopg_pool import ConnectionPool
from pydantic import BaseModel, Field, field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

log = logging.getLogger("player-ingest")

_PID_RE = re.compile(r"^[A-Za-z0-9._\-:]+$")
_NAME_MAX = 128
_PID_MAX = 128
_BATCH_MAX = 64
_TRACK_MIN_INTERVAL_SEC = 10.0
# Presence older than this is treated as OFFLINE for clients (anti-ghost).
_GHOST_TTL = timedelta(minutes=4)


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    postgres_dsn: str
    ingest_api_key: str
    host: str = "0.0.0.0"
    port: int = 8787


settings = Settings()
pool: ConnectionPool | None = None
_track_last: dict[str, float] = {}


@asynccontextmanager
async def lifespan(_app: FastAPI):
    global pool
    pool = ConnectionPool(
        conninfo=settings.postgres_dsn,
        min_size=1,
        max_size=4,
        kwargs={"row_factory": dict_row, "connect_timeout": 5},
        open=True,
    )
    with pool.connection() as conn:
        conn.execute("SELECT 1")
    log.info("postgres pool ready")
    try:
        yield
    finally:
        pool.close()
        pool = None


app = FastAPI(title="Player Ingest", version="1.1.0", lifespan=lifespan)


def require_key(authorization: str | None = Header(default=None)) -> str:
    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "missing bearer token")
    token = authorization[7:].strip()
    if not token or token != settings.ingest_api_key:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "invalid token")
    return token


def _enforce_track_cd(token: str) -> None:
    now = time.monotonic()
    last = _track_last.get(token, 0.0)
    wait = _TRACK_MIN_INTERVAL_SEC - (now - last)
    if wait > 0.05:
        raise HTTPException(
            status.HTTP_429_TOO_MANY_REQUESTS,
            f"track cooldown {wait:.1f}s",
            headers={"Retry-After": str(int(wait) + 1)},
        )
    _track_last[token] = now


class PlayerIn(BaseModel):
    name: str = Field(min_length=1, max_length=_NAME_MAX)
    pid: str = Field(min_length=1, max_length=_PID_MAX)

    @field_validator("name")
    @classmethod
    def clean_name(cls, v: str) -> str:
        v = " ".join(v.strip().split())
        if not v:
            raise ValueError("empty name")
        return v[:_NAME_MAX]

    @field_validator("pid")
    @classmethod
    def clean_pid(cls, v: str) -> str:
        v = v.strip()
        if not _PID_RE.match(v):
            raise ValueError("invalid pid")
        return v[:_PID_MAX]


class IngestBody(BaseModel):
    players: list[PlayerIn] = Field(min_length=1, max_length=_BATCH_MAX)


class TrackBody(BaseModel):
    pids: list[str] = Field(min_length=1, max_length=_BATCH_MAX)

    @field_validator("pids")
    @classmethod
    def clean_pids(cls, v: list[str]) -> list[str]:
        out: list[str] = []
        seen: set[str] = set()
        for raw in v:
            pid = (raw or "").strip()
            if not pid or not _PID_RE.match(pid):
                continue
            pid = pid[:_PID_MAX]
            if pid in seen:
                continue
            seen.add(pid)
            out.append(pid)
        if not out:
            raise ValueError("no valid pids")
        return out


def _iso(dt: Any) -> str | None:
    if dt is None:
        return None
    if isinstance(dt, datetime):
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=timezone.utc)
        return dt.isoformat()
    return str(dt)


def _elapsed_sec(anchor: Any) -> int | None:
    if not isinstance(anchor, datetime):
        return None
    if anchor.tzinfo is None:
        anchor = anchor.replace(tzinfo=timezone.utc)
    return max(0, int((datetime.now(timezone.utc) - anchor.astimezone(timezone.utc)).total_seconds()))


def _normalize_presence(status: str | None, last_seen: Any, changed: Any) -> tuple[str, bool, int | None, int | None]:
    """Return (status, online, session_sec, offline_sec). Stale last_seen → force offline."""
    st = (status or "OFFLINE").strip()
    st_u = st.upper()
    online = st_u in ("IN GAME", "LOADING", "ONLINE")
    now = datetime.now(timezone.utc)

    ls = last_seen
    if isinstance(ls, datetime) and ls.tzinfo is None:
        ls = ls.replace(tzinfo=timezone.utc)

    if online:
        stale = ls is None or (isinstance(ls, datetime) and (now - ls.astimezone(timezone.utc)) > _GHOST_TTL)
        if stale:
            # Ghost / scraper lag — do not report as online.
            online = False
            st = "OFFLINE"
            offline_sec = _elapsed_sec(ls if isinstance(ls, datetime) else changed)
            return st, online, None, offline_sec
        return st, True, _elapsed_sec(changed), None

    # Truly offline: time since status flipped to OFFLINE (fallback last_seen).
    return st, False, None, _elapsed_sec(changed if changed is not None else ls)


@app.get("/health")
def health() -> dict[str, Any]:
    if pool is None:
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "pool not ready")
    try:
        with pool.connection() as conn:
            conn.execute("SELECT 1")
        return {"ok": True, "database": "connected"}
    except Exception as e:
        log.warning("health db fail: %s", e)
        raise HTTPException(status.HTTP_503_SERVICE_UNAVAILABLE, "database not connected") from e


@app.post("/v1/players")
def ingest(body: IngestBody, _: str = Depends(require_key)) -> dict[str, Any]:
    assert pool is not None
    by_pid: dict[str, str] = {p.pid: p.name for p in body.players}
    pids = list(by_pid.keys())
    inserted = 0
    skipped = 0
    try:
        with pool.connection() as conn:
            with conn.transaction():
                existing: set[str] = set()
                with conn.cursor() as cur:
                    cur.execute("SELECT pid FROM client_data WHERE pid = ANY(%s)", (pids,))
                    for row in cur.fetchall():
                        existing.add(row["pid"])
                    for pid, name in by_pid.items():
                        if pid in existing:
                            skipped += 1
                            continue
                        cur.execute(
                            "INSERT INTO client_data (name, pid) VALUES (%s, %s)",
                            (name, pid),
                        )
                        inserted += 1
                        existing.add(pid)
    except psycopg.Error as e:
        log.exception("ingest failed")
        raise HTTPException(status.HTTP_500_INTERNAL_SERVER_ERROR, "db write failed") from e
    return {"ok": True, "inserted": inserted, "skipped": skipped, "received": len(by_pid)}


@app.post("/v1/track")
def track(body: TrackBody, token: str = Depends(require_key)) -> dict[str, Any]:
    """Batch presence lookup for Monster Panel tracking (10s cooldown per key)."""
    assert pool is not None
    _enforce_track_cd(token)

    try:
        with pool.connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    """
                    SELECT pid, name, status, server, server_map, language,
                           lobby_code, last_seen_at, status_changed_at
                    FROM client_data
                    WHERE pid = ANY(%s)
                    """,
                    (body.pids,),
                )
                rows = {r["pid"]: r for r in cur.fetchall()}
    except psycopg.Error as e:
        log.exception("track failed")
        raise HTTPException(status.HTTP_500_INTERNAL_SERVER_ERROR, "db read failed") from e

    players: list[dict[str, Any]] = []
    for pid in body.pids:
        row = rows.get(pid)
        if not row:
            players.append(
                {
                    "pid": pid,
                    "found": False,
                    "name": None,
                    "status": "UNKNOWN",
                    "server": None,
                    "server_map": None,
                    "language": None,
                    "lobby_code": None,
                    "last_seen_at": None,
                    "status_changed_at": None,
                    "session_sec": None,
                    "offline_sec": None,
                    "online": False,
                }
            )
            continue
        st, online, session_sec, offline_sec = _normalize_presence(
            row.get("status"),
            row.get("last_seen_at"),
            row.get("status_changed_at"),
        )
        players.append(
            {
                "pid": pid,
                "found": True,
                "name": row.get("name"),
                "status": st,
                "server": None if not online else row.get("server"),
                "server_map": None if not online else row.get("server_map"),
                "language": row.get("language"),
                "lobby_code": None if not online else row.get("lobby_code"),
                "last_seen_at": _iso(row.get("last_seen_at")),
                "status_changed_at": _iso(row.get("status_changed_at")),
                "session_sec": session_sec,
                "offline_sec": offline_sec,
                "online": online,
            }
        )

    return {"ok": True, "count": len(players), "players": players}
