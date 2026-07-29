#!/usr/bin/env python3
"""Софтверный превью-рендер модели (без Blender): 3 ракурса + затенение по нормали."""
import numpy as np
import trimesh
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection

scene = trimesh.load("iPhone17ProMax_Orange.glb")


def base_color(geom):
    try:
        c = geom.visual.material.baseColorFactor
        c = np.asarray(c, dtype=float)
        if c.max() > 1.5:
            c = c / 255.0
        return c[:3]
    except Exception:
        return np.array([0.7, 0.7, 0.7])


def face_colors(geom):
    """Цвет на грань: если у материала есть текстура — сэмплим её по UV центроида."""
    n_f = len(geom.faces)
    flat = np.tile(base_color(geom), (n_f, 1))
    try:
        tex = geom.visual.material.baseColorTexture
        uv = geom.visual.uv
        if tex is None or uv is None:
            return flat
        img = (np.asarray(tex, dtype=float) / 255.0) ** 2.2   # sRGB -> линейно
        h, w = img.shape[:2]
        c_uv = uv[geom.faces].mean(axis=1)              # UV центра грани
        px = np.clip((c_uv[:, 0] * (w - 1)).astype(int), 0, w - 1)
        py = np.clip(((1 - c_uv[:, 1]) * (h - 1)).astype(int), 0, h - 1)
        return img[py, px, :3]
    except Exception:
        return flat


def render(ax, azim_deg, elev_deg, title):
    ca, sa = np.cos(np.radians(azim_deg)), np.sin(np.radians(azim_deg))
    ce, se = np.cos(np.radians(elev_deg)), np.sin(np.radians(elev_deg))
    Ry = np.array([[ca, 0, sa], [0, 1, 0], [-sa, 0, ca]])
    Rx = np.array([[1, 0, 0], [0, ce, -se], [0, se, ce]])
    M = Rx @ Ry

    light = M @ np.array([0.35, 0.55, 0.75])
    light /= np.linalg.norm(light)

    polys, cols, depth = [], [], []
    for name, geom in scene.geometry.items():
        # экран дробим мельче — только для превью, чтобы обои не шли гранями
        if name.startswith("Screen"):
            geom = geom.copy()
            for _ in range(4):
                geom = geom.subdivide()
        V = np.asarray(geom.vertices) @ M.T
        F = np.asarray(geom.faces)
        col = face_colors(geom)                      # (n_faces, 3)
        tri = V[F]                                   # (n,3,3)
        n = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
        ln = np.linalg.norm(n, axis=1, keepdims=True)
        n = n / np.where(ln == 0, 1, ln)
        vis = n[:, 2] > 0                            # backface cull
        if not vis.any():
            continue
        tri, n, col = tri[vis], n[vis], col[vis]
        lam = np.clip(n @ light, 0, 1)
        spec = np.clip(n[:, 2], 0, 1) ** 40
        is_screen = name.startswith("Screen")
        emissive = 0.80 if is_screen else 0.0        # экран светится сам
        spec_k = 0.0 if is_screen else 0.16          # без блика, иначе обои блёкнут
        shade = ((0.20 + 0.80 * lam)[:, None] * (1 - emissive) + emissive) * col \
            + spec_k * spec[:, None]
        shade = np.clip(shade, 0, 1) ** (1 / 2.2)    # линейно -> sRGB для показа
        polys.extend(tri[:, :, :2])
        cols.extend(shade)
        depth.extend(tri[:, :, 2].mean(axis=1))

    order = np.argsort(depth)                        # painter's algorithm
    pc = PolyCollection([polys[i] for i in order],
                        facecolors=[cols[i] for i in order],
                        edgecolors=[cols[i] for i in order], linewidths=0.35, antialiased=True)
    ax.add_collection(pc)
    P = np.concatenate(polys)
    cx, cy = P[:, 0].mean(), P[:, 1].mean()
    rad = max(np.ptp(P[:, 0]), np.ptp(P[:, 1])) * 0.60
    ax.set_xlim(cx - rad, cx + rad)
    ax.set_ylim(cy - rad, cy + rad)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title(title, color="#e8e8e8", fontsize=11, pad=8)


fig, axes = plt.subplots(1, 3, figsize=(12, 7.2))
fig.patch.set_facecolor("#16181d")
render(axes[0], 0,   0,  "Фронт (экран)")
render(axes[1], 180, 0,  "Спинка (плато камер)")
render(axes[2], 208, 16, "3/4 сзади")
plt.tight_layout()
plt.savefig("preview.png", dpi=125, facecolor="#16181d")
print("ok -> preview.png")
