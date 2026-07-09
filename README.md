# Sticker → .tgs бот

Telegram-бот: отправляешь стикер — получаешь файл.

| Тип стикера | Формат |
|-------------|--------|
| Анимированный | `.tgs` |
| Обычный | `.webp` |
| Видео | `.webm` |

## Быстрый старт

1. Создай бота у [@BotFather](https://t.me/BotFather) → `/newbot` → скопируй токен.
2. Установи зависимости:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
```

3. Настрой токен:

```bash
cp .env.example .env
# открой .env и вставь BOT_TOKEN=...
```

4. Запусти:

```bash
python bot.py
```

5. В Telegram напиши боту `/start` и пришли стикер.

## Заметки

- `.tgs` — это gzip-сжатый Lottie JSON. Открывается в редакторах вроде [lottie.github.io](https://lottie.github.io/lottie-docs/) или после `gunzip`.
- Бот работает через long polling — сервер с публичным IP не нужен.
- Для постоянной работы удобно держать процесс через `systemd`, `screen` или хостинг вроде Railway / Render.
