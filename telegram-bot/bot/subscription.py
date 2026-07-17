from aiogram import Bot
from aiogram.enums import ChatMemberStatus

from bot.config import Settings


async def is_member(bot: Bot, channel_id: int, user_id: int) -> bool:
    try:
        member = await bot.get_chat_member(channel_id, user_id)
    except Exception:
        return False
    return member.status in {
        ChatMemberStatus.CREATOR,
        ChatMemberStatus.ADMINISTRATOR,
        ChatMemberStatus.MEMBER,
    }


async def has_access(bot: Bot, settings: Settings, user_id: int) -> tuple[bool, list[str]]:
    missing: list[str] = []
    if not await is_member(bot, settings.channel_1_id, user_id):
        missing.append("канал 1")
    if not await is_member(bot, settings.channel_2_id, user_id):
        missing.append("канал 2")
    return len(missing) == 0, missing
