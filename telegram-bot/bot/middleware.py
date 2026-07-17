from typing import Any, Awaitable, Callable

from aiogram import BaseMiddleware
from aiogram.types import TelegramObject


class InjectMiddleware(BaseMiddleware):
    def __init__(self, **deps: Any) -> None:
        self.deps = deps

    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        data.update(self.deps)
        return await handler(event, data)
