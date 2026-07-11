"""
Telegram-бот: отправь анимированный стикер — получи .tgs (в zip).
"""

from __future__ import annotations

import io
import logging
import os
import zipfile
from pathlib import Path

from dotenv import load_dotenv
from telegram import InputFile, Update
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
        "Пришли анимированный стикер (Lottie) — верну файл .tgs.\n\n"
        "Обычные (.webp) и видео-стикеры (.webm) не принимаю: "
        "у них нет формата .tgs."
    )


async def help_cmd(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    await update.message.reply_text(
        "Только анимированные стикеры → файл .tgs.\n\n"
        "Как отличить: анимированный стикер крутится как векторная "
        "анимация (не видео). Видео-стикеры и статичные не подойдут."
    )


async def handle_sticker(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    message = update.message
    if not message or not message.sticker:
        return

    sticker = message.sticker

    if sticker.is_video:
        await message.reply_text(
            "Это видео-стикер (.webm), не .tgs.\n"
            "Нужен анимированный Lottie-стикер."
        )
        return

    if not sticker.is_animated:
        await message.reply_text(
            "Это обычный стикер (.webp), не .tgs.\n"
            "Нужен анимированный Lottie-стикер."
        )
        return

    await message.chat.send_action(ChatAction.UPLOAD_DOCUMENT)

    tgs_name = f"sticker_{sticker.file_unique_id}.tgs"
    local_path = DOWNLOADS / tgs_name

    try:
        tg_file = await context.bot.get_file(sticker.file_id)
        await tg_file.download_to_drive(custom_path=str(local_path))

        # Telegram часто показывает «голый» .tgs как стикер, а не как файл.
        # Кладём .tgs в zip — тогда это однозначно скачиваемый документ.
        zip_buf = io.BytesIO()
        with zipfile.ZipFile(zip_buf, "w", compression=zipfile.ZIP_STORED) as zf:
            zf.write(local_path, arcname=tgs_name)
        zip_buf.seek(0)

        zip_name = f"sticker_{sticker.file_unique_id}.zip"
        emoji = sticker.emoji or ""
        await message.reply_document(
            document=InputFile(zip_buf, filename=zip_name),
            caption=f"Внутри архива: {tgs_name} {emoji}".strip(),
        )
        logger.info("Отправлен .tgs в zip: %s", tgs_name)
    except Exception:
        logger.exception("Не удалось скачать стикер")
        await message.reply_text("Не получилось скачать стикер. Попробуй ещё раз.")
    finally:
        local_path.unlink(missing_ok=True)


async def handle_other(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    if not update.message:
        return
    await update.message.reply_text(
        "Пришли анимированный стикер — верну .tgs."
    )


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
