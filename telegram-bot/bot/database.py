import sqlite3
import time
from pathlib import Path


class Database:
    def __init__(self, path: str) -> None:
        self.path = path
        Path(path).parent.mkdir(parents=True, exist_ok=True)
        self._init_schema()

    def _connect(self) -> sqlite3.Connection:
        conn = sqlite3.connect(self.path)
        conn.row_factory = sqlite3.Row
        return conn

    def _init_schema(self) -> None:
        with self._connect() as conn:
            conn.executescript(
                """
                CREATE TABLE IF NOT EXISTS usage_log (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id INTEGER NOT NULL,
                    action TEXT NOT NULL,
                    created_at INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_usage_user_action_time
                    ON usage_log(user_id, action, created_at);

                CREATE TABLE IF NOT EXISTS chat_sessions (
                    user_id INTEGER PRIMARY KEY,
                    conversation_id INTEGER NOT NULL,
                    updated_at INTEGER NOT NULL
                );
                """
            )

    def count_usage(self, user_id: int, action: str, since_ts: int) -> int:
        with self._connect() as conn:
            row = conn.execute(
                """
                SELECT COUNT(*) AS cnt FROM usage_log
                WHERE user_id = ? AND action = ? AND created_at >= ?
                """,
                (user_id, action, since_ts),
            ).fetchone()
            return int(row["cnt"])

    def log_usage(self, user_id: int, action: str) -> None:
        now = int(time.time())
        with self._connect() as conn:
            conn.execute(
                "INSERT INTO usage_log(user_id, action, created_at) VALUES (?, ?, ?)",
                (user_id, action, now),
            )

    def get_conversation_id(self, user_id: int) -> int | None:
        with self._connect() as conn:
            row = conn.execute(
                "SELECT conversation_id FROM chat_sessions WHERE user_id = ?",
                (user_id,),
            ).fetchone()
            return int(row["conversation_id"]) if row else None

    def set_conversation_id(self, user_id: int, conversation_id: int) -> None:
        now = int(time.time())
        with self._connect() as conn:
            conn.execute(
                """
                INSERT INTO chat_sessions(user_id, conversation_id, updated_at)
                VALUES (?, ?, ?)
                ON CONFLICT(user_id) DO UPDATE SET
                    conversation_id = excluded.conversation_id,
                    updated_at = excluded.updated_at
                """,
                (user_id, conversation_id, now),
            )

    def clear_conversation(self, user_id: int) -> None:
        with self._connect() as conn:
            conn.execute("DELETE FROM chat_sessions WHERE user_id = ?", (user_id,))
