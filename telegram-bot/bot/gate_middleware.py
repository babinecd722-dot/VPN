from __future__ import annotations

from typing import Any, Awaitable, Callable

from aiogram import BaseMiddleware
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message, TelegramObject

from bot.config import Settings
from bot.subscription_gate import send_locked_screen, user_has_menu_access


class SubscriptionGateMiddleware(BaseMiddleware):
    """Блокирует любые действия, кроме /start и проверки подписки."""

    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        settings: Settings = data["settings"]
        bot = data["bot"]

        user_id: int | None = None
        if isinstance(event, Message) and event.from_user:
            user_id = event.from_user.id
            if event.text and event.text.startswith("/start"):
                return await handler(event, data)
        elif isinstance(event, CallbackQuery) and event.from_user:
            user_id = event.from_user.id
            if event.data == "check_sub":
                return await handler(event, data)

        if user_id is None:
            return await handler(event, data)

        ok, missing = await user_has_menu_access(bot, settings, user_id)
        if ok:
            return await handler(event, data)

        state: FSMContext | None = data.get("state")
        if state is not None:
            await state.clear()

        if isinstance(event, CallbackQuery):
            await send_locked_screen(event, settings, missing)
        elif isinstance(event, Message):
            await send_locked_screen(event, settings, missing)
        return None
