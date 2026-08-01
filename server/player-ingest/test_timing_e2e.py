#!/usr/bin/env python3
"""End-to-end local simulation of presence timing via VPS Postgres (client_writer).

Uses INSERT/UPDATE only (no DELETE privilege). Leaves probe rows OFFLINE.
"""

from __future__ import annotations

import os
import sys
import time
import uuid
from datetime import datetime, timedelta, timezone

import psycopg

sys.path.insert(0, os.path.dirname(__file__))
import app as ingest  # noqa: E402

DSN = os.environ.get("POSTGRES_DSN") or open("/tmp/lang-farm/state/postgres_dsn.env").read().strip()
if DSN.startswith("POSTGRES_DSN="):
    DSN = DSN.split("=", 1)[1].strip()

TAG = "cursor_local_probe_" + uuid.uuid4().hex[:10]
PID_A = TAG + "_a"
PID_B = TAG + "_b"


def fail(msg: str) -> None:
    print("FAIL:", msg)
    sys.exit(1)


def ok(msg: str) -> None:
    print("OK:", msg)


def main() -> None:
    assert ingest._GHOST_TTL_SEC == 90
    assert ingest._SCRAPE_LAG_MAX_AGE_SEC == 180
    assert 35 * 2 < 90

    with psycopg.connect(DSN) as conn:
        # --- 1) Session start + lease-style last_seen touch ---
        conn.execute(
            """
            INSERT INTO client_data (name, pid, status, server, lobby_code, last_seen_at, status_changed_at)
            VALUES (%s, %s, 'IN GAME', 'srv', 'ABC12345', NOW(), NOW() - interval '20 minutes')
            """,
            ("probeA", PID_A),
        )
        conn.commit()
        sc0, ls0 = conn.execute(
            "SELECT status_changed_at, last_seen_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_A,),
        ).fetchone()
        time.sleep(1.05)
        conn.execute(
            "UPDATE client_data SET last_seen_at = NOW() WHERE pid=%s",
            (PID_A,),
        )
        conn.commit()
        sc1, ls1 = conn.execute(
            "SELECT status_changed_at, last_seen_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_A,),
        ).fetchone()
        if sc1 != sc0:
            fail(f"lease touch reset status_changed_at {sc0} → {sc1}")
        if ls1 <= ls0:
            fail("lease touch did not advance last_seen")
        ok("lease last_seen refresh keeps session start")

        st, online, sess, off = ingest._normalize_presence("IN GAME", ls1, sc1, scrape_lag=False)
        if not online or sess is None or sess < 19 * 60:
            fail(f"session_sec too small: {sess}")
        ok(f"session_sec≈{sess}s from status_changed_at")

        # --- 2) Brief OFFLINE then resume <5min ---
        conn.execute(
            "UPDATE client_data SET status='OFFLINE', server=NULL, lobby_code=NULL WHERE pid=%s",
            (PID_A,),
        )
        conn.commit()
        sc_off = conn.execute(
            "SELECT status_changed_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_A,),
        ).fetchone()[0]
        if sc_off != sc0:
            fail(f"offline blip reset status_changed_at {sc0} → {sc_off}")
        ok("OFFLINE keeps status_changed_at (session anchor)")

        time.sleep(0.4)
        conn.execute(
            """
            UPDATE client_data
            SET status='IN GAME', server='srv', lobby_code='ABC12345', last_seen_at=NOW()
            WHERE pid=%s
            """,
            (PID_A,),
        )
        conn.commit()
        sc_back = conn.execute(
            "SELECT status_changed_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_A,),
        ).fetchone()[0]
        if sc_back != sc0:
            fail(f"resume within 5min reset session {sc0} → {sc_back}")
        ok("IN GAME resume <5min keeps same session timer")

        # --- 3) Long offline (>5min via last_seen) starts new session ---
        conn.execute(
            """
            UPDATE client_data
            SET status='OFFLINE', server=NULL, lobby_code=NULL,
                last_seen_at = NOW() - interval '10 minutes'
            WHERE pid=%s
            """,
            (PID_A,),
        )
        conn.commit()
        conn.execute(
            """
            UPDATE client_data
            SET status='IN GAME', server='srv', lobby_code='ABC12345', last_seen_at=NOW()
            WHERE pid=%s
            """,
            (PID_A,),
        )
        conn.commit()
        sc_new = conn.execute(
            "SELECT status_changed_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_A,),
        ).fetchone()[0]
        age_new = (datetime.now(timezone.utc) - sc_new.astimezone(timezone.utc)).total_seconds()
        if age_new > 5:
            fail(f"expected fresh session start, age={age_new}")
        ok("resume after >5min offline starts NEW session")

        # --- 4) scrape_lag mid-Find vs dead-scraper ceiling ---
        assert ingest._is_scrape_lag(600, 600, max_age=120) is True
        assert ingest._is_scrape_lag(600, 600, max_age=181) is False
        now = datetime.now(timezone.utc)
        st, online, sess, off = ingest._normalize_presence(
            "IN GAME", now - timedelta(seconds=110), now - timedelta(minutes=40), scrape_lag=True
        )
        assert online and sess and sess >= 2300
        st, online, sess, off = ingest._normalize_presence(
            "IN GAME", now - timedelta(seconds=110), now - timedelta(minutes=40), scrape_lag=False
        )
        assert not online and off is not None
        ok("scrape_lag mid-Find online; without lag → offline ghost")

        assert ingest._is_scrape_lag(600, 10, max_age=100) is False
        ok("lone ghosts not protected by scrape_lag")

        # --- 5) offline_sec from last_seen ---
        conn.execute(
            """
            INSERT INTO client_data (name, pid, status, last_seen_at, status_changed_at)
            VALUES (%s, %s, 'OFFLINE', NOW() - interval '45 seconds', NOW() - interval '2 hours')
            """,
            ("probeB", PID_B),
        )
        conn.commit()
        row = conn.execute(
            "SELECT status, last_seen_at, status_changed_at FROM client_data WHERE pid=%s ORDER BY id DESC LIMIT 1",
            (PID_B,),
        ).fetchone()
        st, online, sess, off = ingest._normalize_presence(row[0], row[1], row[2])
        if online or sess is not None or off is None or not (40 <= off <= 55):
            fail(f"offline_sec wrong: online={online} sess={sess} off={off}")
        ok(f"offline_sec≈{off}s from last_seen")

        # park probes offline (no DELETE privilege)
        conn.execute(
            """
            UPDATE client_data
            SET status='OFFLINE', server=NULL, server_map=NULL, lobby_code=NULL,
                name = name || ' (probe done)'
            WHERE pid LIKE %s
            """,
            (TAG + "%",),
        )
        conn.commit()
        ok("parked probe rows OFFLINE")

    # --- 6) Pure timing math: lease keeps age under TTL across 130s Find ---
    ttl, hb = 90, 35
    age = 0
    for t in range(0, 131):
        if t > 0 and t % hb == 0:
            age = 0
        else:
            age += 1
        if age >= ttl:
            fail(f"lease sim age hit TTL at t={t} age={age}")
    ok(f"lease every {hb}s keeps age<{ttl} across 130s Find (final_age={age})")

    print("ALL LOCAL TIMING CHECKS PASSED")


if __name__ == "__main__":
    main()
