from aiogram.types import InlineKeyboardButton, InlineKeyboardMarkup

from bot.doxgram_client import VPN_SERVERS


def subscription_keyboard(channel_1_link: str, channel_2_link: str) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="📢 Канал 1", url=channel_1_link)],
            [InlineKeyboardButton(text="📢 Канал 2", url=channel_2_link)],
            [InlineKeyboardButton(text="✅ Проверить подписку", callback_data="check_sub")],
        ]
    )


def main_menu_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="🔍 Поиск", callback_data="menu_osint"),
                InlineKeyboardButton(text="💬 Чат", callback_data="menu_chat"),
            ],
            [InlineKeyboardButton(text="🌐 VPN", callback_data="menu_vpn")],
            [
                InlineKeyboardButton(text="📊 Лимиты", callback_data="menu_limits"),
                InlineKeyboardButton(text="✅ Проверить доступ", callback_data="check_sub"),
            ],
        ]
    )


def back_to_menu_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[[InlineKeyboardButton(text="← Меню", callback_data="menu_home")]]
    )


def vpn_servers_keyboard() -> InlineKeyboardMarkup:
    rows = []
    for server in VPN_SERVERS:
        rows.append(
            [
                InlineKeyboardButton(
                    text=f"{server['flag']} {server['title']}",
                    callback_data=f"vpn:{server['key']}",
                )
            ]
        )
    rows.append([InlineKeyboardButton(text="← Меню", callback_data="menu_home")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def vpn_connect_keyboard(server_key: str) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="Подключиться", callback_data=f"vpn_connect:{server_key}")],
            [InlineKeyboardButton(text="← К серверам", callback_data="menu_vpn")],
        ]
    )


def chat_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="🗑 Новый диалог", callback_data="chat_reset")],
            [InlineKeyboardButton(text="← Меню", callback_data="menu_home")],
        ]
    )
