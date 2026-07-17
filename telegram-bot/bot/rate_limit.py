import time

from bot.config import Settings
from bot.database import Database


class RateLimiter:
    def __init__(self, db: Database, settings: Settings) -> None:
        self.db = db
        self.settings = settings

    def is_admin(self, user_id: int) -> bool:
        return user_id in self.settings.admin_ids

    def remaining(self, user_id: int, action: str) -> int | None:
        if self.is_admin(user_id):
            return None
        since = int(time.time()) - 3600
        used = self.db.count_usage(user_id, action, since)
        return max(0, self.settings.rate_limit_per_hour - used)

    def check(self, user_id: int, action: str) -> tuple[bool, int | None]:
        remaining = self.remaining(user_id, action)
        if remaining is None:
            return True, None
        return remaining > 0, remaining

    def consume(self, user_id: int, action: str) -> None:
        if self.is_admin(user_id):
            return
        self.db.log_usage(user_id, action)
