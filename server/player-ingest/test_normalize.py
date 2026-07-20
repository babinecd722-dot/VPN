"""Local regression tests for presence normalize / scrape-lag (no DB required)."""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

import app as ingest


def test_is_scrape_lag_thresholds():
    assert ingest._is_scrape_lag(10, 10) is False  # small board
    assert ingest._is_scrape_lag(100, 34) is False  # under 35%
    assert ingest._is_scrape_lag(100, 35) is True
    assert ingest._is_scrape_lag(600, 600) is True


def test_stale_alone_goes_offline():
    now = datetime.now(timezone.utc)
    st, online, sess, off = ingest._normalize_presence(
        "IN GAME",
        now - timedelta(seconds=95),
        now - timedelta(minutes=30),
        scrape_lag=False,
    )
    assert online is False
    assert st == "OFFLINE"
    assert sess is None
    assert off is not None and off >= 90


def test_stale_during_scrape_lag_stays_online():
    now = datetime.now(timezone.utc)
    changed = now - timedelta(minutes=30)
    st, online, sess, off = ingest._normalize_presence(
        "IN GAME",
        now - timedelta(seconds=120),
        changed,
        scrape_lag=True,
    )
    assert online is True
    assert st == "IN GAME"
    assert off is None
    assert sess is not None and sess >= 1790


def test_fresh_online_session():
    now = datetime.now(timezone.utc)
    st, online, sess, off = ingest._normalize_presence(
        "IN GAME",
        now - timedelta(seconds=20),
        now - timedelta(seconds=400),
        scrape_lag=False,
    )
    assert online is True
    assert st == "IN GAME"
    assert off is None
    assert 399 <= sess <= 401


def test_offline_uses_last_seen():
    now = datetime.now(timezone.utc)
    st, online, sess, off = ingest._normalize_presence(
        "OFFLINE",
        now - timedelta(seconds=50),
        now - timedelta(hours=2),
        scrape_lag=True,  # must not revive offline rows
    )
    assert online is False
    assert st == "OFFLINE"
    assert sess is None
    assert 49 <= off <= 51


if __name__ == "__main__":
    # Avoid lifespan / settings requiring real DSN for import side effects beyond module load.
    test_is_scrape_lag_thresholds()
    test_stale_alone_goes_offline()
    test_stale_during_scrape_lag_stays_online()
    test_fresh_online_session()
    test_offline_uses_last_seen()
    print("OK: all normalize tests passed")
