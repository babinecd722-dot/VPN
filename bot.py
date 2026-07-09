"""
Telegram-бот: отправь стикер — получи файл .tgs (для анимированных).
"""

from __future__ import annotations

import logging
import os
from pathlib import Path

from dotenv import load_dotenv
from telegram import Update
from telegram.constants import ChatAction
from telegram.ext import (
    Application,
    CommandHandler,
    ContextTypes,
    MessageHandler,
    filters,
)

load_dotenv()

logging.basicConfig(
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    level=logging.INFO,
)
logger = logging.getLogger(__name__)

DOWNLOADS = Path("downloads")
DOWNLOADS.mkdir(exist_ok=True)


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.message.reply_text(
        "Пришли анимированный стикер — верну файл .tgs.\n\n"
        "Статические стикеры (.webp) и видео-стикеры (.webm) тоже скачаю, "
        "но .tgs бывает только у анимированных."
    )


async def help_cmd(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.message.reply_text(
        "Как пользоваться:\n"
        "1. Отправь боту стикер из любого стикерпака\n"
        "2. Бот скачает файл и пришлёт его документом\n\n"
        "• Анимированный → .tgs\n"
        "• Обычный → .webp\n"
        "• Видео-стикер → .webm"
    )


def _sticker_meta(sticker) -> tuple[str, str]:
    """Возвращает (расширение, подпись) для стикера."""
    if sticker.is_animated:
        return "tgs", "Анимированный стикер (.tgs)"
    if sticker.is_video:
        return "webm", "Видео-стикер (.webm)"
    return "webp", "Статический стикер (.webp)"


async def handle_sticker(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.message
    if not message or not message.sticker:
        return

    sticker = message.sticker
    ext, caption = _sticker_meta(sticker)
    emoji = sticker.emoji or ""

    await message.chat.send_action(ChatAction.UPLOAD_DOCUMENT)

    filename = f"sticker_{sticker.file_unique_id}.{ext}"
    local_path = DOWNLOADS / filename

    try:
        tg_file = await context.bot.get_file(sticker.file_id)
        await tg_file.download_to_drive(custom_path=str(local_path))

        with local_path.open("rb") as f:
            await message.reply_document(
                document=f,
                filename=filename,
                caption=f"{caption} {emoji}".strip(),
            )
    except Exception:
        logger.exception("Не удалось скачать стикер")
        await message.reply_text("Не получилось скачать стикер. Попробуй ещё раз.")
    finally:
        local_path.unlink(missing_ok=True)


async def handle_other(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.message.reply_text("Пришли стикер — верну файл.")


def main() -> None:
    token = os.getenv("BOT_TOKEN", "").strip()
    if not token:
        raise SystemExit(
            "Не задан BOT_TOKEN. Скопируй .env.example → .env и вставь токен от @BotFather."
        )

    app = Application.builder().token(token).build()
    app.add_handler(CommandHandler("start", start))
    app.add_handler(CommandHandler("help", help_cmd))
    app.add_handler(MessageHandler(filters.Sticker.ALL, handle_sticker))
    app.add_handler(MessageHandler(filters.ALL, handle_other))

    logger.info("Бот запущен")
    app.run_polling(allowed_updates=Update.ALL_TYPES)


if __name__ == "__main__":
    main()
