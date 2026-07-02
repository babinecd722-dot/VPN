# Telegram Access Bot

Бот с проверкой подписки на два канала, поиском (OSINT), чатом и VPN через backend Doxgram.

## Возможности

- Проверка подписки на канал 1 (через @ted_resolution) и канал 2
- Поиск по базам — 20 запросов/час
- Чат — 20 сообщений/час
- VPN — 3 локации (Польша, Франция, Нидерланды)
- Админы без лимитов

## Запуск

```bash
cd telegram-bot
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env
# заполните BOT_TOKEN и DOXGRAM_TOKEN
python main.py
```

## Переменные окружения

| Переменная | Описание |
|------------|----------|
| `BOT_TOKEN` | Токен Telegram-бота |
| `DOXGRAM_TOKEN` | `UserAuthToken` из Doxgram |
| `CHANNEL_1_ID` | ID канала 1 (`-1003485510052`) |
| `CHANNEL_1_LINK` | Ссылка на переходник (`https://t.me/ted_resolution`) |
| `CHANNEL_2_ID` | ID канала 2 (`-1003956524111`) |
| `CHANNEL_2_LINK` | Ссылка на открытый канал 2 |
| `ADMIN_IDS` | ID админов через запятую |

Бот должен быть добавлен администратором в оба канала, иначе проверка подписки не сработает.
