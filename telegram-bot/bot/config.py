import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


def _parse_ids(raw: str) -> set[int]:
    items: set[int] = set()
    for part in raw.split(","):
        part = part.strip()
        if part.isdigit():
            items.add(int(part))
    return items


@dataclass(frozen=True)
class Settings:
    bot_token: str
    doxgram_token: str
    channel_1_id: int
    channel_1_link: str
    channel_2_id: int
    channel_2_link: str
    admin_ids: set[int]
    rate_limit_per_hour: int
    db_path: str


def load_settings() -> Settings:
    bot_token = os.getenv("BOT_TOKEN", "").strip()
    doxgram_token = os.getenv("DOXGRAM_TOKEN", "").strip()
    if not bot_token:
        raise RuntimeError("BOT_TOKEN is not set")
    if not doxgram_token:
        raise RuntimeError("DOXGRAM_TOKEN is not set")

    return Settings(
        bot_token=bot_token,
        doxgram_token=doxgram_token,
        channel_1_id=int(os.getenv("CHANNEL_1_ID", "-1003485510052")),
        channel_1_link=os.getenv("CHANNEL_1_LINK", "https://t.me/ted_resolution").strip(),
        channel_2_id=int(os.getenv("CHANNEL_2_ID", "-1003956524111")),
        channel_2_link=os.getenv("CHANNEL_2_LINK", "https://t.me/c/3956524111").strip(),
        admin_ids=_parse_ids(os.getenv("ADMIN_IDS", "8123825459,6297603868")),
        rate_limit_per_hour=int(os.getenv("RATE_LIMIT_PER_HOUR", "20")),
        db_path=os.getenv("DB_PATH", "data/bot.db"),
    )
