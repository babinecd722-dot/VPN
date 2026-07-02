from __future__ import annotations

from aiogram import Bot, F, Router
from aiogram.filters import Command, CommandStart
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from bot.doxgram_client import DoxgramClient, DoxgramError, server_by_key
from bot.formatters import esc, format_limits, format_osint_result, format_vpn_card
from bot.keyboards import (
    back_to_menu_keyboard,
    chat_keyboard,
    main_menu_keyboard,
    subscription_keyboard,
    vpn_connect_keyboard,
    vpn_servers_keyboard,
)
from bot.rate_limit import RateLimiter
from bot.states import ChatStates, OsintStates
from bot.subscription import has_access

router = Router()


WELCOME_LOCKED = (
    "<b>Доступ к сервису</b>\n\n"
    "Бот бесплатный, но нужна подписка на оба канала.\n"
    "После подписки нажмите «Проверить подписку»."
)

WELCOME_OPEN = (
    "<b>Главное меню</b>\n\n"
    "🔍 <b>Поиск</b> — проверка по открытым базам\n"
    "💬 <b>Чат</b> — текстовый помощник\n"
    "🌐 <b>VPN</b> — ключи для v2rayNG / Nekoray\n\n"
    "Лимит: 20 запросов в час на поиск и чат."
)


async def ensure_access(message_or_call: Message | CallbackQuery, bot: Bot, settings, limiter: RateLimiter) -> bool:
    user = message_or_call.from_user
    if not user:
        return False
    if limiter.is_admin(user.id):
        return True
    ok, missing = await has_access(bot, settings, user.id)
    if ok:
        return True

    text = (
        "<b>Нужна подписка</b>\n\n"
        f"Не подписаны: <b>{', '.join(missing)}</b>\n"
        "Подпишитесь и нажмите «Проверить подписку»."
    )
    kb = subscription_keyboard(settings.channel_1_link, settings.channel_2_link)
    if isinstance(message_or_call, CallbackQuery):
        await message_or_call.message.edit_text(text, reply_markup=kb)
        await message_or_call.answer()
    else:
        await message_or_call.answer(text, reply_markup=kb)
    return False


@router.message(CommandStart())
async def cmd_start(message: Message, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    await state.clear()
    user = message.from_user
    if not user:
        return

    if limiter.is_admin(user.id):
        await message.answer(WELCOME_OPEN, reply_markup=main_menu_keyboard())
        return

    ok, _ = await has_access(bot, settings, user.id)
    if ok:
        await message.answer(WELCOME_OPEN, reply_markup=main_menu_keyboard())
    else:
        await message.answer(WELCOME_LOCKED, reply_markup=subscription_keyboard(settings.channel_1_link, settings.channel_2_link))


@router.callback_query(F.data == "check_sub")
async def cb_check_sub(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    await state.clear()
    user = call.from_user
    if limiter.is_admin(user.id):
        await call.message.edit_text(WELCOME_OPEN, reply_markup=main_menu_keyboard())
        await call.answer("Доступ открыт")
        return

    ok, missing = await has_access(bot, settings, user.id)
    if ok:
        await call.message.edit_text(WELCOME_OPEN, reply_markup=main_menu_keyboard())
        await call.answer("Доступ открыт")
    else:
        await call.answer(f"Нужна подписка: {', '.join(missing)}", show_alert=True)


@router.callback_query(F.data == "menu_home")
async def cb_menu_home(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    await state.clear()
    if not await ensure_access(call, bot, settings, limiter):
        return
    await call.message.edit_text(WELCOME_OPEN, reply_markup=main_menu_keyboard())
    await call.answer()


@router.callback_query(F.data == "menu_limits")
async def cb_limits(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter) -> None:
    if not await ensure_access(call, bot, settings, limiter):
        return
    user = call.from_user
    text = (
        "<b>Ваши лимиты</b>\n\n"
        + format_limits(
            limiter.remaining(user.id, "osint"),
            limiter.remaining(user.id, "chat"),
            limiter.is_admin(user.id),
        )
    )
    await call.message.edit_text(text, reply_markup=back_to_menu_keyboard())
    await call.answer()


@router.callback_query(F.data == "menu_osint")
async def cb_osint(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    if not await ensure_access(call, bot, settings, limiter):
        return
    await state.set_state(OsintStates.waiting_query)
    left = limiter.remaining(call.from_user.id, "osint")
    extra = "♾ без лимита" if left is None else f"осталось {left} запросов в час"
    await call.message.edit_text(
        f"<b>Поиск</b>\n\nОтправьте запрос одним сообщением.\n<i>{extra}</i>",
        reply_markup=back_to_menu_keyboard(),
    )
    await call.answer()


@router.message(OsintStates.waiting_query)
async def osint_query(
    message: Message,
    bot: Bot,
    settings,
    limiter: RateLimiter,
    doxgram: DoxgramClient,
    state: FSMContext,
) -> None:
    if not await ensure_access(message, bot, settings, limiter):
        await state.clear()
        return

    query = (message.text or "").strip()
    if len(query) < 2:
        await message.answer("Запрос слишком короткий.", reply_markup=back_to_menu_keyboard())
        return

    allowed, remaining = limiter.check(message.from_user.id, "osint")
    if not allowed:
        await message.answer(
            f"Лимит поиска исчерпан. Подождите час.\nОсталось: <b>0</b>",
            reply_markup=main_menu_keyboard(),
        )
        await state.clear()
        return

    wait = await message.answer("⏳ Ищу…")
    try:
        data = await doxgram.osint_search(query)
        limiter.consume(message.from_user.id, "osint")
        parts = format_osint_result(data)
        for i, part in enumerate(parts):
            markup = main_menu_keyboard() if i == len(parts) - 1 else None
            await message.answer(part, reply_markup=markup)
        await wait.delete()
    except DoxgramError as exc:
        await wait.edit_text(f"Ошибка поиска ({exc.status or '—'}).\nПопробуйте позже.", reply_markup=main_menu_keyboard())
    except Exception:
        await wait.edit_text("Сервис временно недоступен.", reply_markup=main_menu_keyboard())
    finally:
        await state.clear()


@router.callback_query(F.data == "menu_chat")
async def cb_chat(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    if not await ensure_access(call, bot, settings, limiter):
        return
    await state.set_state(ChatStates.waiting_message)
    left = limiter.remaining(call.from_user.id, "chat")
    extra = "♾ без лимита" if left is None else f"осталось {left} сообщений в час"
    await call.message.edit_text(
        f"<b>Чат</b>\n\nНапишите сообщение — получите ответ.\n<i>{extra}</i>",
        reply_markup=chat_keyboard(),
    )
    await call.answer()


@router.callback_query(F.data == "chat_reset")
async def cb_chat_reset(call: CallbackQuery, db, state: FSMContext) -> None:
    db.clear_conversation(call.from_user.id)
    await state.set_state(ChatStates.waiting_message)
    await call.message.edit_text(
        "<b>Новый диалог</b>\n\nНапишите первое сообщение.",
        reply_markup=chat_keyboard(),
    )
    await call.answer("Диалог сброшен")


@router.message(ChatStates.waiting_message)
async def chat_message(
    message: Message,
    bot: Bot,
    settings,
    limiter: RateLimiter,
    doxgram: DoxgramClient,
    db,
    state: FSMContext,
) -> None:
    if not await ensure_access(message, bot, settings, limiter):
        await state.clear()
        return

    text = (message.text or "").strip()
    if not text:
        await message.answer("Отправьте текстовое сообщение.", reply_markup=chat_keyboard())
        return
    if len(text) > 4000:
        await message.answer("Слишком длинное сообщение (макс. 4000).", reply_markup=chat_keyboard())
        return

    allowed, _ = limiter.check(message.from_user.id, "chat")
    if not allowed:
        await message.answer("Лимит чата исчерпан. Подождите час.", reply_markup=main_menu_keyboard())
        await state.clear()
        return

    wait = await message.answer("⏳ Думаю…")
    try:
        conversation_id = db.get_conversation_id(message.from_user.id)
        if not conversation_id:
            created = await doxgram.create_conversation(title="Telegram")
            conversation_id = int(created["id"])
            db.set_conversation_id(message.from_user.id, conversation_id)

        result = await doxgram.send_chat_message(conversation_id, text)
        limiter.consume(message.from_user.id, "chat")

        reply = (result or {}).get("reply") or {}
        answer = (reply.get("content") or "").strip()
        if not answer:
            answer = "Пустой ответ. Попробуйте переформулировать."

        title = result.get("conversationTitle") or "Чат"
        await wait.delete()
        await message.answer(
            f"<b>{esc(title)}</b>\n\n{esc(answer)}",
            reply_markup=chat_keyboard(),
        )
    except DoxgramError as exc:
        if exc.status == 502:
            msg = "Сервис чата временно недоступен на стороне провайдера."
        elif exc.status in (401, 403, 417):
            msg = "Ошибка авторизации backend. Сообщите администратору."
        else:
            msg = f"Ошибка чата ({exc.status or '—'})."
        await wait.edit_text(msg, reply_markup=chat_keyboard())
    except Exception:
        await wait.edit_text("Сервис временно недоступен.", reply_markup=chat_keyboard())


@router.callback_query(F.data == "menu_vpn")
async def cb_vpn(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    await state.clear()
    if not await ensure_access(call, bot, settings, limiter):
        return
    await call.message.edit_text(
        "<b>VPN</b>\n\nВыберите локацию сервера:",
        reply_markup=vpn_servers_keyboard(),
    )
    await call.answer()


@router.callback_query(F.data.startswith("vpn:"))
async def cb_vpn_server(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter) -> None:
    if not await ensure_access(call, bot, settings, limiter):
        return
    key = call.data.split(":", 1)[1]
    server = server_by_key(key)
    if not server:
        await call.answer("Сервер не найден", show_alert=True)
        return
    tier = "Премиум · без лимита скорости" if server["tier"] == "premium" else "Стандарт · ограничение скорости"
    await call.message.edit_text(
        f"<b>{server['flag']} {esc(server['title'])}</b>\n\n"
        f"Хост: <code>{esc(server['host'])}</code>\n"
        f"Тариф: {tier}",
        reply_markup=vpn_connect_keyboard(key),
    )
    await call.answer()


@router.callback_query(F.data.startswith("vpn_connect:"))
async def cb_vpn_connect(call: CallbackQuery, bot: Bot, settings, limiter: RateLimiter, doxgram: DoxgramClient) -> None:
    if not await ensure_access(call, bot, settings, limiter):
        return
    key = call.data.split(":", 1)[1]
    server = server_by_key(key)
    if not server:
        await call.answer("Сервер не найден", show_alert=True)
        return

    await call.answer("Получаю ключ…")
    try:
        if server["tier"] == "premium":
            payload = await doxgram.get_vpn_key(node_id=server["node_id"])
        else:
            payload = await doxgram.get_free_vpn_key()
        vless = payload.get("vlessUri") or ""
        if not vless:
            raise DoxgramError("empty vless")
        speed = int(payload.get("speedLimitBytesPerSecond") or 0)
        text = format_vpn_card(server, vless, speed)
        await call.message.edit_text(text, reply_markup=vpn_servers_keyboard())
    except DoxgramError:
        await call.message.edit_text(
            "Не удалось получить ключ. Попробуйте другой сервер.",
            reply_markup=vpn_servers_keyboard(),
        )
    except Exception:
        await call.message.edit_text(
            "Сервис VPN временно недоступен.",
            reply_markup=vpn_servers_keyboard(),
        )


@router.message(Command("menu"))
async def cmd_menu(message: Message, bot: Bot, settings, limiter: RateLimiter, state: FSMContext) -> None:
    await state.clear()
    if not await ensure_access(message, bot, settings, limiter):
        return
    await message.answer(WELCOME_OPEN, reply_markup=main_menu_keyboard())
