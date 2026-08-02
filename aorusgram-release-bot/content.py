from __future__ import annotations

DOWNLOAD_JSON_URL = "https://download.aorusgram.com/apps.json"
DOWNLOAD_IPA_URL = "https://download.aorusgram.com/AorusGram-12.8.ipa"
SUPPORT_URL = "https://t.me/aorusgram_support"
SUPPORT_HANDLE = "@aorusgram_support"

RU_QUOTE = """🇷🇺 Работа в России
— Встроенный обход блокировок и собственная система туннелирования ATunnel
— Прокси для звонков и MTProto с обфускацией
— Всё настраивается в пару касаний, без сторонних приложений

📰 Стена — ваша лента новостей
— Все посты из непрочитанных каналов в одном месте, читаются сверху вниз
— Умные рекомендации: подписки и близкие каналы всегда выше, лента подбирает лучшее и свежее
— Бесконечная прокрутка, реакции прямо из ленты, свайп чтобы скрыть канал

👁 То, что скрывают
— Чтение удалённых сообщений
— История изменений отредактированных сообщений
— Просмотр одноразовых фото и видео, сохранение без следов
— Скачивание историй и скрытный просмотр

🎁 Премиум без премиума
— Фейковый Telegram Premium
— Фейковые подарки и звёзды
— Безлимитные закреплённые чаты и недавние стикеры

🔒 Приватность и безопасность
— Защита чатов по Face ID с отдельным списком чатов
— Защита ссылок: предупреждение о подменённых доменах, редиректах и опасных файлах
— Подтверждение действий перед звонком и отправкой голосового
— Скрытие онлайна, набора текста и прочтения
— Подмена устройства и номера, антипоиск, антиспам

🎨 Внешний вид
— AMOLED-тема, кастомные шрифты, фиолетовые акценты
— Анимированные обои из GIF и анимированный баннер профиля
— Настраиваемый таббар: компактный режим, скрытие подписей

💬 Удобство
— Панель форматирования и история буфера обмена
— Быстрые ответы и автоответ
— Перевод сообщений и расшифровка голосовых
— Локальное редактирование чужих сообщений
— Маски и задняя камера для кружков, голосовой двойник
— Ускоритель загрузок, резервное копирование аккаунтов и вход по бэкапу"""

EN_QUOTE = """🌍 Works where Telegram doesn't
— Built-in censorship bypass and our own ATunnel tunneling system
— Obfuscated MTProto and call proxying
— Set up in a couple of taps, no third-party apps

📰 The Wall — your news feed
— Every post from your unread channels in one place, read top to bottom
— Smart recommendations: your subscriptions rank first, the feed picks what's best and current
— Endless scrolling, reactions straight from the feed, swipe to hide a channel

👁 See what's hidden
— Read deleted messages
— Full edit history of edited messages
— Open view-once photos and videos, save them silently
— Download stories and view them without being seen

🎁 Premium without Premium
— Fake Telegram Premium
— Fake gifts and stars
— Unlimited pinned chats and recent stickers

🔒 Privacy and security
— Chat Protection with Face ID and its own chat list
— Link Protection: warns about spoofed domains, redirects and dangerous files
— Action Confirmation before calls and before sending a voice message
— Hide online status, typing and read receipts
— Device and phone spoofing, anti-search, anti-spam

🎨 Looks
— AMOLED theme, custom fonts, purple accents
— Animated GIF wallpapers and an animated profile banner
— Configurable tab bar: compact mode, hidden labels

💬 Everyday
— Formatting panel and clipboard history
— Quick replies and auto-reply
— Message translation and voice transcription
— Edit anyone's message locally
— Masks and rear camera for video messages, voice twin
— Download accelerator, account backup and login by backup"""


def _bi(text: str) -> str:
    """Wrap text in bold+italic entities."""
    return f"<b><i>{text}</i></b>"


def _build(quote: str, header: str, intro: str, sources_label: str, support_label: str) -> str:
    download_line = f'📥 {sources_label}: <a href="{DOWNLOAD_JSON_URL}">{DOWNLOAD_JSON_URL}</a>'
    support_line = f'💬 {support_label}: <a href="{SUPPORT_URL}">{SUPPORT_HANDLE}</a>'
    return (
        f"{_bi(header)}\n\n"
        f"{_bi(intro)}\n\n"
        f"<blockquote>{_bi(quote)}</blockquote>\n\n"
        f"{_bi(download_line)}\n"
        f"{_bi(support_line)}"
    )


RU_MESSAGE = _build(
    RU_QUOTE,
    "🎉 ДОЛГОЖДАННЫЙ РЕЛИЗ AORUSGRAM!",
    "👨‍💻 Мы долго к этому шли — и наконец готовы представить наш лучший форк Telegram.",
    "Источники",
    "Чат поддержки",
)

EN_MESSAGE = _build(
    EN_QUOTE,
    "🇺🇸 AORUSGRAM IS OUT!",
    "👨‍💻 We've been building this for a long time — here is our best Telegram fork.",
    "Sources",
    "Support chat",
)

FULL_MESSAGE = f"{RU_MESSAGE}\n\n\n{EN_MESSAGE}"
