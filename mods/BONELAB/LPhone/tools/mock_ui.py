#!/usr/bin/env python3
"""Мокап экранов LPhone теми же координатами, что в PhoneOS.cs — визуальная проверка вёрстки."""
import datetime
from PIL import Image, ImageDraw, ImageFont

W, H = 742, 1600
DOCK_H = 210
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BAKE = 48                       # кегль запекания в make_font.py


def f(scale):
    return ImageFont.truetype(FONT, max(8, int(round(BAKE * scale))))


def wallpaper(dim=0.0):
    im = Image.open("wallpaper.png").convert("RGB").resize((W, H))
    if dim > 0:
        ov = Image.new("RGB", (W, H), (0, 0, 0))
        im = Image.blend(im, ov, dim)
    return im


def status_bar(d):
    now = datetime.datetime.now()
    d.text((92, 58), now.strftime("%H:%M"), font=f(0.44), fill=(255, 255, 255))
    bx, by = W - 128, 62
    d.rounded_rectangle([bx, by, bx + 56, by + 26], 8, outline=(255, 255, 255, 90), width=2)
    d.rounded_rectangle([bx + 3, by + 3, bx + 47, by + 23], 5, fill=(255, 255, 255))
    d.rounded_rectangle([bx + 60, by + 9, bx + 65, by + 19], 2, fill=(200, 200, 200))
    for i in range(4):
        x = W - 232 + i * 15
        d.rounded_rectangle([x, by + 22 - (i + 1) * 5, x + 10, by + 22], 3, fill=(255, 255, 255))


def lock():
    im = wallpaper(0.10)
    d = ImageDraw.Draw(im, "RGBA")
    status_bar(d)
    now = datetime.datetime.now()
    d.text((W // 2, 300), now.strftime("%A, %d %B"), font=f(0.52),
           fill=(235, 235, 245), anchor="ma")
    d.text((W // 2, 350), now.strftime("%H:%M"), font=f(2.55),
           fill=(255, 255, 255), anchor="ma")
    d.text((W // 2, H - 200), "Свайп вверх для разблокировки", font=f(0.40),
           fill=(240, 240, 250), anchor="ma")
    d.rounded_rectangle([W // 2 - 90, H - 60, W // 2 + 90, H - 51], 5, fill=(255, 255, 255))
    return im


def icon_rect(i, n):
    size = 132
    gap = (W - 80 - n * size) // (n + 1)
    x = 40 + gap + i * (size + gap)
    y = H - DOCK_H - 60 + 30
    return x, y, size


def home():
    im = wallpaper(0.05)
    d = ImageDraw.Draw(im, "RGBA")
    status_bar(d)
    dock_y = H - DOCK_H - 60
    d.rounded_rectangle([28, dock_y, W - 28, dock_y + DOCK_H], 56, fill=(255, 255, 255, 46))

    apps = [("icon_camera.png", "Camera"), ("icon_gallery.png", "Gallery"),
            ("icon_store.png", "L Store")]
    for i, (icon, title) in enumerate(apps):
        x, y, s = icon_rect(i, len(apps))
        ic = Image.open(icon).convert("RGBA").resize((s, s))
        im.paste(ic, (x, y), ic)
        d.text((x + s // 2, y + s + 12), title, font=f(0.32),
               fill=(255, 255, 255), anchor="ma")

    d.rounded_rectangle([W // 2 - 90, H - 46, W // 2 + 90, H - 37], 5, fill=(255, 255, 255))
    return im


if __name__ == "__main__":
    a, b = lock(), home()
    sheet = Image.new("RGB", (W * 2 + 90, H + 60), (22, 24, 29))
    sheet.paste(a, (30, 30))
    sheet.paste(b, (W + 60, 30))
    sheet = sheet.resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS)
    sheet.save("ui_mock.png")
    print("ok -> ui_mock.png  (слева: экран блокировки, справа: домашний)")
