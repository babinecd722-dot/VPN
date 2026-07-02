from __future__ import annotations

import html
import textwrap
from typing import Any


def esc(value: str) -> str:
    return html.escape(value or "")


def format_limits(osint_left: int | None, chat_left: int | None, is_admin: bool) -> str:
    if is_admin:
        return "♾ Без лимитов (админ)"
    return (
        f"Поиск: <b>{osint_left}</b> из 20 в час\n"
        f"Чат: <b>{chat_left}</b> из 20 в час"
    )


def format_osint_result(data: dict[str, Any], max_chars: int = 3800) -> list[str]:
    if not data.get("hasFields") and not data.get("data"):
        return ["<b>Ничего не найдено</b>\nПопробуйте другой запрос."]

    chunks: list[str] = []
    current = "<b>Результаты поиска</b>\n\n"
    total = int(data.get("totalFields") or 0)
    current += f"Полей: <b>{total}</b>\n\n"

    databases = data.get("data") or []
    for db in databases[:12]:
        name = esc(str(db.get("database") or "База"))
        fields = db.get("fields") or []
        if not fields:
            continue
        block = f"📁 <b>{name}</b>\n"
        for field in fields[:8]:
            label = esc(str(field.get("name") or field.get("key") or "Поле"))
            value = esc(str(field.get("value") or "—"))
            block += f"• {label}: <code>{value}</code>\n"
        if len(fields) > 8:
            block += f"<i>…ещё {len(fields) - 8} полей</i>\n"
        block += "\n"

        if len(current) + len(block) > max_chars:
            chunks.append(current.rstrip())
            current = block
        else:
            current += block

    persons = data.get("persons") or []
    if persons and len(current) < max_chars - 200:
        current += "<b>Сводка</b>\n"
        for person in persons[:5]:
            fields = person.get("fields") or []
            if not fields:
                continue
            line_parts = []
            for field in fields[:4]:
                label = esc(str(field.get("name") or ""))
                value = esc(str(field.get("value") or ""))
                if value:
                    line_parts.append(f"{label}: <code>{value}</code>")
            if line_parts:
                current += "— " + " · ".join(line_parts) + "\n"

    if current.strip():
        chunks.append(current.rstrip())

    if len(databases) > 12:
        chunks.append(f"<i>Показаны первые 12 из {len(databases)} баз.</i>")

    return chunks or ["<b>Ничего не найдено</b>"]


def format_vpn_card(server: dict[str, Any], vless_uri: str, speed_limit: int = 0) -> str:
    tier = "Премиум" if server["tier"] == "premium" else "Стандарт"
    speed = "без ограничений" if not speed_limit else f"~{speed_limit // 1024} KB/s"
    wrapped = textwrap.fill(vless_uri, width=48)
    return (
        f"<b>{server['flag']} {esc(server['title'])}</b>\n"
        f"Статус: {tier}\n"
        f"Скорость: {speed}\n"
        f"Хост: <code>{esc(server['host'])}</code>\n\n"
        f"<b>Ключ подключения</b>\n"
        f"<code>{esc(wrapped)}</code>\n\n"
        "Скопируйте ссылку в v2rayNG, Nekoray или Hiddify."
    )
