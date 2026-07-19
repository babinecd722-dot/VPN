#!/usr/bin/env python3
"""Upsert farm bot into client_data (name, pid). Always refreshes last_seen_at."""
from __future__ import annotations

import os
import sys

import psycopg2


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: register_bot.py <pid> <name> [status]", file=sys.stderr)
        return 2
    pid, name = sys.argv[1], sys.argv[2]
    # Default OFFLINE — bots must not create IN GAME ghosts in presence.
    status = sys.argv[3] if len(sys.argv) > 3 else "OFFLINE"
    dsn = os.environ.get("POSTGRES_DSN", "").strip()
    if not dsn:
        print("POSTGRES_DSN missing", file=sys.stderr)
        return 3
    conn = psycopg2.connect(dsn)
    try:
        cur = conn.cursor()
        cur.execute("SELECT pid FROM client_data WHERE pid=%s", (pid,))
        # OFFLINE must wipe lobby fields — otherwise UI shows "offline but still in server".
        clear_lobby = status.upper() == "OFFLINE"
        if cur.fetchone():
            if clear_lobby:
                cur.execute(
                    """UPDATE client_data
                       SET name=%s, status=%s, last_seen_at=NOW(),
                           server=NULL, server_map=NULL, lobby_code=NULL
                       WHERE pid=%s""",
                    (name, status, pid),
                )
            else:
                cur.execute(
                    "UPDATE client_data SET name=%s, status=%s, last_seen_at=NOW() WHERE pid=%s",
                    (name, status, pid),
                )
            action = "update"
        else:
            cur.execute(
                "INSERT INTO client_data (name, pid, status, last_seen_at) VALUES (%s, %s, %s, NOW())",
                (name, pid, status),
            )
            action = "insert"
        conn.commit()
        print(f"{action} ok pid={pid} name={name} status={status}")
        return 0
    finally:
        conn.close()


if __name__ == "__main__":
    raise SystemExit(main())
