#!/usr/bin/env python3
"""Ассеты LPhone: обои под аспект экрана + иконки приложений (iOS-squircle)."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

SCR_W, SCR_H = 742, 1600          # аспект экрана телефона
ICON = 256

# ─────────────────────────── ОБОИ ───────────────────────────
def wallpaper(src, out="wallpaper.png"):
    im = Image.open(src).convert("RGB")
    w, h = im.size
    target = SCR_H / SCR_W
    if h / w < target:                      # обрезаем по ширине
        nw = int(round(h / target))
        x = (w - nw) // 2
        im = im.crop((x, 0, x + nw, h))
    else:                                   # обрезаем по высоте
        nh = int(round(w * target))
        y = (h - nh) // 2
        im = im.crop((0, y, w, y + nh))
    im = im.resize((SCR_W, SCR_H), Image.LANCZOS)
    im.save(out)
    print(f"  обои      -> {out}  {im.size}")
    return im


# ─────────────────────────── ИКОНКИ ───────────────────────────
def squircle_mask(size, n=5.0, pad=0.02):
    """iOS-squircle: |x|^n + |y|^n = 1 — мягче обычного скругления."""
    y, x = np.mgrid[0:size, 0:size]
    u = (x + 0.5) / size * 2 - 1
    v = (y + 0.5) / size * 2 - 1
    s = 1.0 - pad
    d = (np.abs(u / s) ** n + np.abs(v / s) ** n)
    m = np.clip((1.05 - d) * size * 0.35, 0, 1)      # мягкий край = антиалиасинг
    return Image.fromarray((m * 255).astype(np.uint8), "L")


def grad(size, top, bottom, diagonal=True):
    y, x = np.mgrid[0:size, 0:size]
    t = ((x + y) / (2 * size)) if diagonal else (y / size)
    t = t[..., None]
    a, b = np.array(top) / 255.0, np.array(bottom) / 255.0
    img = a * (1 - t) + b * t
    return Image.fromarray((img * 255).astype(np.uint8), "RGB")


def gloss(img):
    """Лёгкий верхний блик, как на iOS-иконках."""
    size = img.size[0]
    y, x = np.mgrid[0:size, 0:size]
    g = np.clip(1.0 - ((y / size) / 0.55) ** 1.6, 0, 1) * 0.13
    arr = np.asarray(img, dtype=float) / 255.0
    arr = np.clip(arr + g[..., None], 0, 1)
    return Image.fromarray((arr * 255).astype(np.uint8), "RGB")


def finish(base, name):
    base = gloss(base)
    out = Image.new("RGBA", base.size, (0, 0, 0, 0))
    out.paste(base, (0, 0), squircle_mask(base.size[0]))
    out.save(name)
    print(f"  иконка    -> {name}")


def icon_camera():
    s = ICON
    img = grad(s, (72, 76, 86), (26, 28, 34))
    d = ImageDraw.Draw(img)
    c, r = s / 2, s * 0.29
    d.ellipse([c - r, c - r, c + r, c + r], fill=(18, 19, 23))          # корпус объектива
    r2 = r * 0.80
    d.ellipse([c - r2, c - r2, c + r2, c + r2], fill=(38, 42, 52))      # оправа
    r3 = r * 0.60
    d.ellipse([c - r3, c - r3, c + r3, c + r3], fill=(12, 16, 30))      # стекло
    r4 = r * 0.33
    d.ellipse([c - r4, c - r4, c + r4, c + r4], fill=(46, 96, 168))     # блик оптики
    d.ellipse([c - r4 * 0.45 - r4 * 0.35, c - r4 * 0.95,
               c + r4 * 0.10 - r4 * 0.35, c - r4 * 0.30], fill=(190, 220, 255))
    fr = s * 0.045                                                      # вспышка
    fx, fy = s * 0.775, s * 0.225
    d.ellipse([fx - fr, fy - fr, fx + fr, fy + fr], fill=(226, 232, 240))
    finish(img, "icon_camera.png")


def icon_gallery():
    s = ICON
    img = grad(s, (255, 158, 66), (206, 62, 148))
    d = ImageDraw.Draw(img)
    sx, sy, sr = s * 0.30, s * 0.31, s * 0.085                          # солнце
    d.ellipse([sx - sr, sy - sr, sx + sr, sy + sr], fill=(255, 238, 170))
    d.polygon([(s*0.08, s*0.80), (s*0.40, s*0.44), (s*0.66, s*0.80)],   # дальняя гора
              fill=(126, 58, 150))
    d.polygon([(s*0.34, s*0.82), (s*0.66, s*0.40), (s*0.96, s*0.82)],   # ближняя гора
              fill=(84, 36, 116))
    d.rectangle([0, s * 0.80, s, s], fill=(58, 26, 88))                 # земля
    finish(img, "icon_gallery.png")


def icon_store():
    s = ICON
    img = grad(s, (64, 156, 255), (24, 74, 214))
    d = ImageDraw.Draw(img)
    # стилизованная «L» из двух брусков со скруглением
    t = s * 0.115
    x0, y0, y1 = s * 0.335, s * 0.235, s * 0.735
    d.rounded_rectangle([x0, y0, x0 + t, y1], radius=t * 0.45, fill=(255, 255, 255))
    d.rounded_rectangle([x0, y1 - t, x0 + s * 0.335, y1], radius=t * 0.45,
                        fill=(255, 255, 255))
    # стрелка загрузки
    ax, ay = s * 0.735, s * 0.335
    d.rounded_rectangle([ax - s*0.028, ay - s*0.10, ax + s*0.028, ay + s*0.06],
                        radius=s*0.028, fill=(255, 255, 255))
    d.polygon([(ax - s*0.075, ay + s*0.045), (ax + s*0.075, ay + s*0.045),
               (ax, ay + s*0.155)], fill=(255, 255, 255))
    finish(img, "icon_store.png")


def icon_messenger():
    s = ICON
    img = grad(s, (86, 176, 255), (18, 88, 232))
    d = ImageDraw.Draw(img)
    bolt = [(0.585, 0.130), (0.300, 0.545), (0.470, 0.545),
            (0.395, 0.880), (0.700, 0.435), (0.520, 0.435), (0.585, 0.130)]
    d.polygon([(x * s, y * s) for x, y in bolt], fill=(255, 255, 255))
    finish(img, "icon_messenger.png")


if __name__ == "__main__":
    print("ассеты LPhone:")
    wallpaper("/root/.claude/uploads/d0ba4f30-32cf-58cd-a162-b2490447fed2/48075c2e-IMG_6143.JPG")
    icon_camera()
    icon_gallery()
    icon_store()
    icon_messenger()
    # контактный лист превью иконок
    sheet = Image.new("RGB", (ICON * 4 + 50 * 5, ICON + 100), (22, 24, 29))
    for i, n in enumerate(["icon_camera", "icon_gallery", "icon_store", "icon_messenger"]):
        ic = Image.open(n + ".png")
        sheet.paste(ic, (50 + i * (ICON + 50), 50), ic)
    sheet.save("icons_sheet.png")
    print("  превью    -> icons_sheet.png")
