#!/usr/bin/env python3
"""Пекём атлас шрифта для софтверного UI телефона.

Выход:
  font.png  — атлас глифов (альфа в канале A, RGB белый)
  font.bin  — метрики:  magic "LFN1", int16 lineHeight, int16 baseline,
                        int32 glyphCount, далее по глифу:
                        uint16 codepoint, int16 x,y,w,h, int16 bearingX, bearingY,
                        int16 advance
Латиница + кириллица + цифры + пунктуация — хватает на весь интерфейс.
"""
import struct
import numpy as np
from PIL import Image, ImageFont, ImageDraw

FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
SIZE = 48                       # кегль запекания (масштабируем при отрисовке)
PAD = 2
ATLAS_W = 1024

CHARS = (
    " !\"#$%&'()*+,-./0123456789:;<=>?@"
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`"
    "abcdefghijklmnopqrstuvwxyz{|}~"
    "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ"
    "абвгдеёжзийклмнопрстуфхцчшщъыьэюя"
    "→←↑↓•·—–…°×✓✕"
)


def bake():
    font = ImageFont.truetype(FONT, SIZE)
    asc, desc = font.getmetrics()
    line_h = asc + desc

    glyphs = []
    x = y = PAD
    row_h = 0
    for ch in CHARS:
        mask = font.getmask(ch, mode="L")
        w, h = mask.size
        bbox = font.getbbox(ch)
        adv = int(round(font.getlength(ch)))

        if x + w + PAD > ATLAS_W:
            x = PAD
            y += row_h + PAD
            row_h = 0
        glyphs.append(dict(ch=ch, x=x, y=y, w=w, h=h,
                           bx=bbox[0], by=bbox[1], adv=adv, mask=mask))
        x += w + PAD
        row_h = max(row_h, h)

    atlas_h = 1
    while atlas_h < y + row_h + PAD:
        atlas_h *= 2

    img = Image.new("RGBA", (ATLAS_W, atlas_h), (255, 255, 255, 0))
    for g in glyphs:
        if g["w"] == 0 or g["h"] == 0:
            continue
        gi = Image.frombytes("L", g["mask"].size, bytes(g["mask"]))
        rgba = Image.merge("RGBA", (
            Image.new("L", gi.size, 255), Image.new("L", gi.size, 255),
            Image.new("L", gi.size, 255), gi))
        img.paste(rgba, (g["x"], g["y"]))
    img.save("font.png")

    buf = bytearray(b"LFN1")
    buf += struct.pack("<hh", line_h, asc)
    buf += struct.pack("<i", len(glyphs))
    for g in glyphs:
        buf += struct.pack("<H hhhh hh h",
                           ord(g["ch"]), g["x"], g["y"], g["w"], g["h"],
                           g["bx"], g["by"], g["adv"])
    open("font.bin", "wb").write(bytes(buf))

    print(f"  font.png  {img.size}  глифов: {len(glyphs)}")
    print(f"  font.bin  {len(buf)} байт, lineHeight={line_h}, baseline={asc}")


if __name__ == "__main__":
    print("запекание шрифта:")
    bake()
