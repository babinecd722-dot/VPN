"""Thin ingest API: mod → HTTP → PostgreSQL. DB password never leaves the server."""

from __future__ import annotations

import logging
import re
from contextlib import asynccontextmanager
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


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    postgres_dsn: str
    ingest_api_key: str
    host: str = "0.0.0.0"
    port: int = 8787


settings = Settings()
pool: ConnectionPool | None = None


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
    # Fail fast if DSN is wrong
    with pool.connection() as conn:
        conn.execute("SELECT 1")
    log.info("postgres pool ready")
    try:
        yield
    finally:
        pool.close()
        pool = None


app = FastAPI(title="Player Ingest", version="1.0.0", lifespan=lifespan)


def require_key(authorization: str | None = Header(default=None)) -> None:
    if not authorization or not authorization.startswith("Bearer "):
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "missing bearer token")
    token = authorization[7:].strip()
    if not token or token != settings.ingest_api_key:
        raise HTTPException(status.HTTP_401_UNAUTHORIZED, "invalid token")


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


@app.get("/health")
def health() -> dict[str, Any]:
    """Public liveness + DB ping. Used by the mod on boot."""
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
def ingest(body: IngestBody, _: None = Depends(require_key)) -> dict[str, Any]:
    """Insert new players by pid. Existing pids are skipped (writer has no UPDATE)."""
    assert pool is not None

    # Dedup inside the batch (last name wins)
    by_pid: dict[str, str] = {}
    for p in body.players:
        by_pid[p.pid] = p.name

    pids = list(by_pid.keys())
    inserted = 0
    skipped = 0

    try:
        with pool.connection() as conn:
            with conn.transaction():
                existing: set[str] = set()
                with conn.cursor() as cur:
                    cur.execute(
                        "SELECT pid FROM client_data WHERE pid = ANY(%s)",
                        (pids,),
                    )
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
