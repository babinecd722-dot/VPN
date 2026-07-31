#!/usr/bin/env python3
"""UPDATE client_data.language = 'English'|'Russian'|... WHERE pid=..."""
from __future__ import annotations

import os
import sys

import psycopg2

ISO_TO_NAME = {
    "ru": "Russian",
    "en": "English",
    "es": "Spanish",
    "fr": "French",
    "de": "German",
    "pt": "Portuguese",
    "pl": "Polish",
    "uk": "Ukrainian",
    "it": "Italian",
    "tr": "Turkish",
    "ja": "Japanese",
    "zh": "Chinese",
    "ko": "Korean",
    "ar": "Arabic",
    "nl": "Dutch",
    "sv": "Swedish",
    "cs": "Czech",
    "ro": "Romanian",
    "hi": "Hindi",
    "vi": "Vietnamese",
    "no": "Norwegian",
    "fi": "Finnish",
    "da": "Danish",
    "el": "Greek",
    "he": "Hebrew",
    "th": "Thai",
    "id": "Indonesian",
}


def to_name(code_or_name: str) -> str | None:
    s = (code_or_name or "").strip()
    if not s or s.lower() in ("unknown", "und"):
        return None
    # already a display name?
    for name in ISO_TO_NAME.values():
        if s.lower() == name.lower():
            return name
    return ISO_TO_NAME.get(s.lower())


def main() -> int:
    if len(sys.argv) < 3:
        print("usage: update_language.py <pid> <lang_code_or_name> [confidence]", file=sys.stderr)
        return 2
    pid = sys.argv[1].strip()
    lang = to_name(sys.argv[2])
    conf = float(sys.argv[3]) if len(sys.argv) > 3 else 1.0
    if not lang:
        print(f"skip unknown lang={sys.argv[2]!r}")
        return 0
    dsn = os.environ.get("POSTGRES_DSN", "").strip()
    if not dsn:
        print("POSTGRES_DSN missing", file=sys.stderr)
        return 3

    conn = psycopg2.connect(dsn)
    try:
        cur = conn.cursor()
        cur.execute("SELECT language FROM client_data WHERE pid=%s", (pid,))
        row = cur.fetchone()
        if not row:
            # ensure row exists then set language
            cur.execute(
                "INSERT INTO client_data (name, pid, status, language) VALUES (%s, %s, 'OFFLINE', %s)",
                ("?", pid, lang),
            )
            action = "insert+set"
        else:
            prev = row[0]
            # overwrite null / empty; keep existing unless new conf is solid
            if prev and str(prev).strip() and conf < 0.70:
                print(f"keep pid={pid} language={prev} (new {lang} conf={conf:.3f} too low to overwrite)")
                return 0
            cur.execute("UPDATE client_data SET language=%s WHERE pid=%s", (lang, pid))
            action = f"update:{prev!r}->{lang}"
        conn.commit()
        print(f"{action} ok pid={pid} language={lang} conf={conf:.3f}")
        return 0
    finally:
        conn.close()


if __name__ == "__main__":
    raise SystemExit(main())
