from __future__ import annotations

import asyncio
import os
from pathlib import Path

from aiogram import Bot
from aiogram.client.default import DefaultBotProperties
from aiogram.enums import ParseMode
from dotenv import load_dotenv

from content import FULL_MESSAGE
from main import RELEASE_KEYBOARD

BASE_DIR = Path(__file__).resolve().parent
load_dotenv(BASE_DIR / ".env")

CHANNEL_ID = "@aorusgram"


async def main() -> None:
    bot = Bot(
        token=os.environ["BOT_TOKEN"],
        default=DefaultBotProperties(parse_mode=ParseMode.HTML),
    )
    try:
        result = await bot.send_message(
            CHANNEL_ID,
            FULL_MESSAGE,
            reply_markup=RELEASE_KEYBOARD,
            disable_web_page_preview=True,
        )
        print(f"Published channel post: {result.message_id}")
    finally:
        await bot.session.close()


if __name__ == "__main__":
    asyncio.run(main())
