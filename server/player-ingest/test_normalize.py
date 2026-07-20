"""Local regression: normalize, scrape_lag ceilings, session timing semantics."""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

import app as ingest


def test_is_scrape_lag_thresholds():
    assert ingest._is_scrape_lag(10, 10) is False  # small board
    assert ingest._is_scrape_lag(100, 34, max_age=100) is False  # under 35%
    assert ingest._is_scrape_lag(100, 35, max_age=100) is True
    assert ingest._is_scrape_lag(600, 600, max_age=120) is True  # mid-Find


def test_scrape_lag_hard_ceiling_dead_scraper():
    # Mass stale BUT older than max hold → scraper dead, do not protect ghosts.
    assert ingest._is_scrape_lag(600, 600, max_age=ingest._SCRAPE_LAG_MAX_AGE_SEC + 1) is False
    assert ingest._is_scrape_lag(600, 600, max_age=ingest._SCRAPE_LAG_MAX_AGE_SEC) is True


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


def test_stale_during_scrape_lag_stays_online_with_session():
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


def test_fresh_online_session_advances():
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


def test_offline_uses_last_seen_not_status_changed():
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


def test_loading_counts_online():
    now = datetime.now(timezone.utc)
    st, online, sess, off = ingest._normalize_presence(
        "LOADING",
        now - timedelta(seconds=5),
        now - timedelta(seconds=5),
        scrape_lag=False,
    )
    assert online is True
    assert st == "LOADING"
    assert sess is not None


def test_defaults_aligned():
    assert ingest._GHOST_TTL_SEC == 90
    assert ingest._SCRAPE_LAG_MAX_AGE_SEC == 180
    # Lease default 35 * 2 = 70 < 90 TTL margin
    assert 35 * 2 < ingest._GHOST_TTL_SEC
    # Mid-Find peak ~130 covered by scrape_lag max 180
    assert 130 < ingest._SCRAPE_LAG_MAX_AGE_SEC


if __name__ == "__main__":
    test_is_scrape_lag_thresholds()
    test_scrape_lag_hard_ceiling_dead_scraper()
    test_stale_alone_goes_offline()
    test_stale_during_scrape_lag_stays_online_with_session()
    test_fresh_online_session_advances()
    test_offline_uses_last_seen_not_status_changed()
    test_loading_counts_online()
    test_defaults_aligned()
    print("OK: all normalize/timing tests passed")
