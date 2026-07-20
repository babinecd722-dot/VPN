"""Player ingest + tracking API: mod → HTTP → PostgreSQL."""

from __future__ import annotations

import logging
import os
import re
import threading
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
# Keep in sync with scraper GHOST_TTL_SEC (default 180).
# Must exceed a full EOS Find cycle (~100s+) or Tracking flickers online↔offline mid-session.
_GHOST_TTL = timedelta(seconds=int(os.environ.get("GHOST_TTL_SEC", "180") or "180"))
_GHOST_TTL_SEC = int(_GHOST_TTL.total_seconds())
_GHOST_SWEEP_SEC = 10.0
# If a single sweep would wipe more than this fraction of active rows, skip —
# almost always a scraper blip/restart, not a real mass logout (prevents "0 online").
_GHOST_SWEEP_COLLAPSE_RATIO = 0.35


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    postgres_dsn: str
    ingest_api_key: str
    host: str = "0.0.0.0"
    port: int = 8787


settings = Settings()
pool: ConnectionPool | None = None
_track_last: dict[str, float] = {}
_sweep_stop = threading.Event()
_sweep_thread: threading.Thread | None = None
_last_sweep: dict[str, Any] = {"at": None, "offline": 0}


def _ghost_sweep_once() -> int:
    """Force-OFFLINE stale active rows. Skip mass-wipe when scrape blip would zero the board."""
    assert pool is not None
    with pool.connection() as conn:
        with conn.transaction():
            with conn.cursor() as cur:
                cur.execute(
                    """
                    SELECT
                      count(*) FILTER (
                        WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')
                      ) AS active,
                      count(*) FILTER (
                        WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')
                          AND (
                                last_seen_at IS NULL
                                OR last_seen_at < NOW() - make_interval(secs => %s)
                              )
                      ) AS stale
                    FROM client_data
                    """,
                    (_GHOST_TTL_SEC,),
                )
                row = cur.fetchone() or {}
                active = int(row.get("active") or 0)
                stale = int(row.get("stale") or 0)
                if active > 20 and stale >= max(1, int(active * _GHOST_SWEEP_COLLAPSE_RATIO)):
                    log.warning(
                        "ghost sweep skipped collapse active=%s stale=%s ratio>=%.2f",
                        active,
                        stale,
                        _GHOST_SWEEP_COLLAPSE_RATIO,
                    )
                    return 0
                cur.execute(
                    """
                    UPDATE client_data
                    SET status = 'OFFLINE',
                        server = NULL,
                        server_map = NULL,
                        lobby_code = NULL
                    WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')
                      AND (
                            last_seen_at IS NULL
                            OR last_seen_at < NOW() - make_interval(secs => %s)
                          )
                    """,
                    (_GHOST_TTL_SEC,),
                )
                return cur.rowcount


def _ghost_sweep_loop() -> None:
    # Hard backstop so DB presence cannot rot longer than ~TTL + sweep interval.
    while not _sweep_stop.wait(_GHOST_SWEEP_SEC):
        if pool is None:
            continue
        try:
            n = _ghost_sweep_once()
            _last_sweep["at"] = datetime.now(timezone.utc).isoformat()
            _last_sweep["offline"] = n
            if n > 0:
                log.info("ghost sweep → offline=%s", n)
        except Exception as e:
            log.warning("ghost sweep failed: %s", e)


@asynccontextmanager
async def lifespan(_app: FastAPI):
    global pool, _sweep_thread
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
    _sweep_stop.clear()
    _sweep_thread = threading.Thread(target=_ghost_sweep_loop, name="ghost-sweep", daemon=True)
    _sweep_thread.start()
    log.info("ghost sweep loop started ttl=%ss every=%ss", _GHOST_TTL_SEC, int(_GHOST_SWEEP_SEC))
    try:
        yield
    finally:
        _sweep_stop.set()
        if _sweep_thread is not None:
            _sweep_thread.join(timeout=5)
            _sweep_thread = None
        pool.close()
        pool = None


app = FastAPI(title="Player Ingest", version="1.2.0", lifespan=lifespan)


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
        # session_sec from status_changed_at (session start; survives brief OFFLINE via DB trigger)
        return st, True, _elapsed_sec(changed), None

    # Offline duration from last_seen (status_changed_at is session start, not offline-at).
    return st, False, None, _elapsed_sec(ls if isinstance(ls, datetime) else changed)


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


@app.get("/v1/presence-stats")
def presence_stats(_: str = Depends(require_key)) -> dict[str, Any]:
    """Live vs ghost counts (ghost = active status but last_seen older than TTL)."""
    assert pool is not None
    try:
        with pool.connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    """
                    SELECT
                      count(*) AS total,
                      count(*) FILTER (WHERE status = 'IN GAME') AS ingame,
                      count(*) FILTER (WHERE status = 'LOADING') AS loading,
                      count(*) FILTER (
                        WHERE status = 'IN GAME'
                          AND last_seen_at >= NOW() - make_interval(secs => %s)
                      ) AS live,
                      count(*) FILTER (
                        WHERE status IN ('ONLINE', 'IN GAME', 'LOADING')
                          AND (
                                last_seen_at IS NULL
                                OR last_seen_at < NOW() - make_interval(secs => %s)
                              )
                      ) AS ghost
                    FROM client_data
                    """,
                    (_GHOST_TTL_SEC, _GHOST_TTL_SEC),
                )
                row = cur.fetchone() or {}
    except psycopg.Error as e:
        log.exception("presence-stats failed")
        raise HTTPException(status.HTTP_500_INTERNAL_SERVER_ERROR, "db read failed") from e
    return {
        "ok": True,
        "ttl_sec": _GHOST_TTL_SEC,
        "total": row.get("total", 0),
        "ingame": row.get("ingame", 0),
        "loading": row.get("loading", 0),
        "live": row.get("live", 0),
        "ghost": row.get("ghost", 0),
        # Aliases for old clients/scripts
        "live4": row.get("live", 0),
        "ghost4": row.get("ghost", 0),
        "ttl_min": max(1, (_GHOST_TTL_SEC + 59) // 60),
        "last_sweep": _last_sweep,
    }


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
