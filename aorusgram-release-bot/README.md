# AorusGram Release Announcement Bot

A minimal Telegram bot that replies to `/start` with the AorusGram release
announcement (RU + EN), formatted as a single blockquote per language and an
inline "Download IPA" button.

## Setup

```bash
pip install -r requirements.txt
cp .env.example .env   # then fill in BOT_TOKEN
```

## Run

```bash
python3 main.py
# or, with auto-restart:
./run.sh
```

## Content

- `content.py` holds the announcement text and link targets:
  - `📥 Источники` (Russian) and `📥 Sources` (English) link to
    `https://download.aorusgram.com/apps.json`
  - `💬 Чат поддержки / Support chat` links to `@aorusgram_support`
  - The inline "Download IPA" button links to
    `https://download.aorusgram.com/AorusGram-12.8.ipa`
- All bullet lines are wrapped in a single `<blockquote>` per language so
  Telegram renders them as one continuous quote block instead of several
  separate ones.

## Channel publication

To post the announcement from the bot to `@aorusgram`:

```bash
python3 publish.py
```
