import asyncio
import logging

from aiogram import Bot, Dispatcher
from aiogram.client.default import DefaultBotProperties
from aiogram.enums import ParseMode
from aiogram.fsm.storage.memory import MemoryStorage

from bot.config import load_settings
from bot.database import Database
from bot.doxgram_client import DoxgramClient
from bot.gate_middleware import SubscriptionGateMiddleware
from bot.handlers import router
from bot.middleware import InjectMiddleware
from bot.rate_limit import RateLimiter


async def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
    settings = load_settings()

    bot = Bot(
        token=settings.bot_token,
        default=DefaultBotProperties(parse_mode=ParseMode.HTML),
    )
    dp = Dispatcher(storage=MemoryStorage())

    db = Database(settings.db_path)
    doxgram = DoxgramClient(settings)
    limiter = RateLimiter(db, settings)

    dp.update.middleware(
        InjectMiddleware(
            settings=settings,
            db=db,
            doxgram=doxgram,
            limiter=limiter,
        )
    )
    dp.update.middleware(SubscriptionGateMiddleware())

    dp.include_router(router)

    await dp.start_polling(bot)


if __name__ == "__main__":
    asyncio.run(main())
