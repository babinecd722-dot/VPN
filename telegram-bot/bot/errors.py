from __future__ import annotations

import json


def humanize_doxgram_error(status: int | None, raw: str) -> str:
    text = (raw or "").strip()
    parsed = _extract_message(text)

    if status == 412 or parsed.lower() == "limited":
        return "Лимит поиска на сервере исчерпан. Подождите немного."
    if status == 417:
        return "Сессия не активирована. Откройте Doxgram и подтвердите через бота."
    if status == 402:
        return "Нужна активная подписка Doxgram на backend."
    if status == 401:
        return "Сессия backend истекла. Админу нужно обновить токен."
    if status == 502:
        return "Чат на техобслуживании. Сервер ответов временно недоступен."
    if status == 400:
        return parsed or "Некорректный запрос."
    if status and status >= 500:
        return "Сервис временно недоступен. Попробуйте позже."
    if parsed:
        return parsed
    return "Не удалось выполнить запрос."


def _extract_message(text: str) -> str:
    if not text:
        return ""
    try:
        data = json.loads(text)
        if isinstance(data, dict):
            for key in ("message", "error", "detail", "status"):
                value = data.get(key)
                if isinstance(value, str) and value.strip():
                    return value.strip()
    except json.JSONDecodeError:
        pass
    return text[:300]
