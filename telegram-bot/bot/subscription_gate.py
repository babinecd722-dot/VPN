from __future__ import annotations

from aiogram import Bot
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from bot.config import Settings
from bot.keyboards import subscription_keyboard
from bot.subscription import has_access

LOCKED_TEXT = (
    "<b>Доступ закрыт</b>\n\n"
    "Подпишитесь на оба канала, затем нажмите «Проверить подписку».\n"
    "До подписки меню и функции недоступны."
)


async def send_locked_screen(
    target: Message | CallbackQuery,
    settings: Settings,
    missing: list[str] | None = None,
) -> None:
    text = LOCKED_TEXT
    if missing:
        text += f"\n\nНе хватает: <b>{', '.join(missing)}</b>"
    keyboard = subscription_keyboard(settings.channel_1_link, settings.channel_2_link)

    if isinstance(target, CallbackQuery):
        try:
            await target.message.edit_text(text, reply_markup=keyboard)
        except Exception:
            await target.message.answer(text, reply_markup=keyboard)
        await target.answer()
    else:
        await target.answer(text, reply_markup=keyboard)


async def user_has_menu_access(bot: Bot, settings: Settings, user_id: int) -> tuple[bool, list[str]]:
    return await has_access(bot, settings, user_id)
