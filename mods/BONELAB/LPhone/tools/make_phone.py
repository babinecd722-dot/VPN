#!/usr/bin/env python3
"""
Параметрический генератор модели смартфона (iPhone 17 Pro Max -стайл, Cosmic Orange)
под BONELAB / Marrow SDK / Unity.

Особенности:
  - точные реальные размеры в МЕТРАХ (Unity-масштаб 1:1)
  - hard-surface: ровные рёбра + машинная фаска по периметру
  - экран = ОТДЕЛЬНЫЙ плоский меш с UV 0..1 (якорь для World Space Canvas)
  - раздельные объекты и материалы: корпус / экран / плато камер / линзы / кнопки
  - низкий полигонаж под Quest

Запуск:  python3 make_phone.py
Выход:   iPhone17ProMax_Orange.glb
"""

import numpy as np
import trimesh
from trimesh.visual.material import PBRMaterial

# ────────────────────────── ПАРАМЕТРЫ (метры) ──────────────────────────
W  = 0.0776      # ширина корпуса        (77.6 мм)
H  = 0.1634      # высота корпуса       (163.4 мм)
T  = 0.00875     # толщина корпуса        (8.75 мм)
R  = 0.0110      # радиус скругления углов (11 мм)
CH = 0.00080     # фаска по периметру      (0.8 мм)

BEZEL   = 0.0017   # рамка вокруг экрана   (1.7 мм — тонкая, как в референсе)
SCR_R   = R - BEZEL  # радиус углов экрана концентричен корпусу
SCR_OFF = 0.00012  # вынос экрана над корпусом (анти z-fighting)

# Dynamic Island — «таблетка» поверх экрана
DI_W, DI_H = 0.0205, 0.0066     # 20.5 x 6.6 мм
DI_TOP     = 0.0098             # центр острова от верхнего торца корпуса
DI_CAM_R   = 0.0013             # фронтальная камера внутри острова

# плато камер (широкая полоса сверху сзади, как у 17 Pro)
PL_W, PL_H, PL_T, PL_R = 0.0750, 0.0420, 0.0025, 0.0110
PL_TOP_GAP = 0.0045                       # отступ от верхнего торца

LENS_OUT_R, LENS_GLASS_R = 0.00835, 0.0062  # оправа / стекло (16.7 / 12.4 мм)
LENS_RING_H, LENS_GLASS_H = 0.0016, 0.0011
LENS_PAIR_X, LENS_PAIR_Y = 0.0155, 0.00975  # смещение пары от левого края плато
LENS_THIRD_DX = 0.0191                      # третья линза правее пары

# задняя вставная панель (стеклянная зона MagSafe на нижних 2/3 спинки)
BP_W, BP_H, BP_T, BP_R = 0.0670, 0.1010, 0.0007, 0.0130
BP_CY = -0.0225                             # центр панели ниже центра корпуса

SEG_CORNER = 8    # сегментов на угол скругления
SEG_CIRCLE = 28   # сегментов на окружность линзы

# ────────────────────────── МАТЕРИАЛЫ ──────────────────────────
MAT = {
    # ВНИМАНИЕ: baseColorFactor в glTF — ЛИНЕЙНОЕ пространство.
    # #E8763A (sRGB) -> линейно [0.807, 0.181, 0.042] — это и есть Cosmic Orange.
    "alu_orange": PBRMaterial(name="M_Alu_CosmicOrange",
                              baseColorFactor=[0.807, 0.181, 0.042, 1.0],
                              metallicFactor=0.85, roughnessFactor=0.40),
    "alu_dark":   PBRMaterial(name="M_Alu_Dark",
                              baseColorFactor=[0.660, 0.135, 0.030, 1.0],
                              metallicFactor=0.85, roughnessFactor=0.34),
    "screen":     PBRMaterial(name="M_Screen",
                              baseColorFactor=[1.0, 1.0, 1.0, 1.0],
                              metallicFactor=0.0, roughnessFactor=0.06),
    "island":     PBRMaterial(name="M_DynamicIsland",
                              baseColorFactor=[0.008, 0.008, 0.010, 1.0],
                              metallicFactor=0.0, roughnessFactor=0.10),
    "backpanel":  PBRMaterial(name="M_BackPanel_Glass",
                              baseColorFactor=[0.855, 0.215, 0.055, 1.0],
                              metallicFactor=0.30, roughnessFactor=0.28),
    "glass":      PBRMaterial(name="M_LensGlass",
                              baseColorFactor=[0.015, 0.025, 0.055, 1.0],
                              metallicFactor=0.35, roughnessFactor=0.05),
    "ring":       PBRMaterial(name="M_LensRing",
                              baseColorFactor=[0.78, 0.78, 0.80, 1.0],
                              metallicFactor=1.0, roughnessFactor=0.22),
    "black":      PBRMaterial(name="M_Black",
                              baseColorFactor=[0.03, 0.03, 0.03, 1.0],
                              metallicFactor=0.0, roughnessFactor=0.35),
}


# ────────────────────────── ОБОИ (процедурная текстура) ──────────────────────────
def make_wallpaper(path="wallpaper.png", w=720, h=1560):
    """Абстрактные обои сине-розовым градиентом (в духе стоковых Apple)."""
    from PIL import Image

    yy, xx = np.mgrid[0:h, 0:w]
    u = xx / (w - 1.0)
    v = yy / (h - 1.0)

    deep   = np.array([0.09, 0.16, 0.62])   # глубокий синий
    cyan   = np.array([0.30, 0.62, 0.95])   # голубой
    magenta= np.array([0.93, 0.28, 0.45])   # розово-красный
    violet = np.array([0.52, 0.26, 0.78])   # фиолетовый

    # базовый диагональный градиент: низ-лево розовый -> верх-право синий
    d = np.clip(0.5 * (u + (1.0 - v)), 0, 1)[..., None]
    img = magenta * (1 - d) + deep * d

    # мягкая «волна» — крупный светлый лепесток сверху
    cx, cy, rx, ry = 0.62, 0.30, 0.52, 0.34
    e = (((u - cx) / rx) ** 2 + ((v - cy) / ry) ** 2)
    petal = np.exp(-2.6 * e)[..., None]
    img = img * (1 - 0.72 * petal) + cyan * (0.72 * petal)

    # диагональные полосы-складки
    band = np.sin((u * 3.1 + v * 5.4) * np.pi) * 0.5 + 0.5
    band = (band ** 3)[..., None]
    img = img * (1 - 0.22 * band) + violet * (0.22 * band)

    # нижний розовый луч
    ray = np.exp(-6.0 * (((u - 0.18) / 0.55) ** 2 + ((v - 0.86) / 0.40) ** 2))[..., None]
    img = img * (1 - 0.55 * ray) + magenta * (0.55 * ray)

    # лёгкое виньетирование
    vig = 1.0 - 0.22 * (((u - 0.5) * 2) ** 2 + ((v - 0.5) * 2) ** 2)[..., None]
    img = np.clip(img * vig, 0, 1)

    out = Image.fromarray((img * 255).astype(np.uint8), mode="RGB")
    out.save(path)
    return out


# ────────────────────────── ГЕОМЕТРИЯ ──────────────────────────
def rounded_rect(w, h, r, seg=SEG_CORNER):
    """Профиль скруглённого прямоугольника (CCW, XY)."""
    r = min(r, w / 2, h / 2)
    pts = []
    for cx, cy, a0 in ((w/2-r,  h/2-r,   0.0),
                       (-w/2+r, h/2-r,  90.0),
                       (-w/2+r, -h/2+r, 180.0),
                       (w/2-r,  -h/2+r, 270.0)):
        for i in range(seg + 1):
            a = np.radians(a0 + 90.0 * i / seg)
            pts.append((cx + r*np.cos(a), cy + r*np.sin(a)))
    return np.asarray(pts, dtype=np.float64)


def _bridge(v, a0, b0, n, faces):
    """Кольцо квадов между двумя кольцами вершин (по n вершин)."""
    for i in range(n):
        j = (i + 1) % n
        faces.append((a0+i, b0+i, b0+j))
        faces.append((a0+i, b0+j, a0+j))


def _cap(v, ring0, n, faces, center, flip=False):
    """Крышка веером от центра (профиль выпуклый — корректно)."""
    ci = len(v)
    v.append(center)
    for i in range(n):
        j = (i + 1) % n
        f = (ci, ring0+j, ring0+i) if flip else (ci, ring0+i, ring0+j)
        faces.append(f)
    return ci


def chamfered_slab(w, h, t, r, ch, seg=SEG_CORNER):
    """Плита со скруглёнными углами и машинной фаской сверху/снизу."""
    outer = rounded_rect(w, h, r, seg)
    inner = rounded_rect(w - 2*ch, h - 2*ch, max(r - ch, 1e-5), seg)
    n = len(outer)
    z_f, z_b = t/2, -t/2

    rings = [(inner, z_f), (outer, z_f - ch), (outer, z_b + ch), (inner, z_b)]
    v, faces = [], []
    starts = []
    for prof, z in rings:
        starts.append(len(v))
        v.extend([(p[0], p[1], z) for p in prof])

    for k in range(3):
        _bridge(v, starts[k], starts[k+1], n, faces)
    _cap(v, starts[0], n, faces, (0.0, 0.0, z_f), flip=False)
    _cap(v, starts[3], n, faces, (0.0, 0.0, z_b), flip=True)

    return trimesh.Trimesh(vertices=np.asarray(v), faces=np.asarray(faces),
                           process=True)


def flat_rounded_panel(w, h, r, seg=SEG_CORNER):
    """Плоская панель (экран). UV растянуты 0..1 по bbox — под Canvas/текстуру."""
    prof = rounded_rect(w, h, r, seg)
    n = len(prof)
    v = [(p[0], p[1], 0.0) for p in prof]
    faces = []
    _cap(v, 0, n, faces, (0.0, 0.0, 0.0), flip=False)

    V = np.asarray(v)
    uv = np.column_stack(((V[:, 0] + w/2) / w, (V[:, 1] + h/2) / h))
    m = trimesh.Trimesh(vertices=V, faces=np.asarray(faces), process=False)
    m.visual = trimesh.visual.TextureVisuals(uv=uv)
    return m


def ring_cylinder(r_out, r_in, height, seg=SEG_CIRCLE):
    """Кольцо (оправа линзы) — труба со стенками."""
    a = np.linspace(0, 2*np.pi, seg, endpoint=False)
    co, si = np.cos(a), np.sin(a)
    v, faces, starts = [], [], []
    for rad, z in ((r_out, height/2), (r_out, -height/2),
                   (r_in, -height/2), (r_in, height/2)):
        starts.append(len(v))
        v.extend([(rad*c, rad*s, z) for c, s in zip(co, si)])
    for k in range(3):
        _bridge(v, starts[k], starts[k+1], seg, faces)
    _bridge(v, starts[3], starts[0], seg, faces)
    return trimesh.Trimesh(vertices=np.asarray(v), faces=np.asarray(faces),
                           process=True)


def disc(radius, height, seg=SEG_CIRCLE):
    return trimesh.creation.cylinder(radius=radius, height=height, sections=seg)


def box(w, h, d):
    return trimesh.creation.box(extents=(w, h, d))


def put(mesh, pos, rot=None, mat=None):
    if rot is not None:
        mesh.apply_transform(rot)
    mesh.apply_translation(pos)
    if mat is not None:
        if isinstance(mesh.visual, trimesh.visual.TextureVisuals):
            mesh.visual.material = mat
        else:
            mesh.visual = trimesh.visual.TextureVisuals(material=mat)
    return mesh


# ────────────────────────── СБОРКА ──────────────────────────
def build():
    parts = {}
    # (вращения не нужны: все цилиндры уже ориентированы по Z)

    # 1) Корпус — Z+ = лицевая сторона (экран)
    parts["Body"] = put(chamfered_slab(W, H, T, R, CH), (0, 0, 0),
                        mat=MAT["alu_orange"])

    # 2) Экран — отдельный меш, плоский, UV 0..1  → якорь для Canvas
    scr_mat = MAT["screen"]
    try:
        scr_mat.baseColorTexture = make_wallpaper()
    except Exception as e:
        print("  (обои пропущены:", e, ")")
    parts["Screen_UI_Anchor"] = put(
        flat_rounded_panel(W - 2*BEZEL, H - 2*BEZEL, SCR_R),
        (0, 0, T/2 + SCR_OFF), mat=scr_mat)

    # 2b) Dynamic Island — «таблетка» поверх экрана + фронтальная камера
    di_y = H/2 - DI_TOP
    parts["DynamicIsland"] = put(
        flat_rounded_panel(DI_W, DI_H, DI_H/2),
        (0, di_y, T/2 + SCR_OFF + 0.00008), mat=MAT["island"])
    parts["FrontCamera"] = put(
        disc(DI_CAM_R, 0.00004),
        (DI_W/2 - 0.0042, di_y, T/2 + SCR_OFF + 0.00014), mat=MAT["glass"])

    # 3) Плато камер (сзади, Z-)
    pl_y = H/2 - PL_TOP_GAP - PL_H/2
    pl_z = -T/2 - PL_T/2 + 0.0002
    parts["CameraPlateau"] = put(
        chamfered_slab(PL_W, PL_H, PL_T, PL_R, 0.0005),
        (0, pl_y, pl_z), mat=MAT["alu_dark"])

    # 4) Три линзы — треугольником на левой части плато
    lz = pl_z - PL_T/2
    lx0 = -PL_W/2 + LENS_PAIR_X
    lens_xy = [(lx0,                  LENS_PAIR_Y),   # верх-лево
               (lx0,                 -LENS_PAIR_Y),   # низ-лево
               (lx0 + LENS_THIRD_DX,  0.0000)]        # право
    # ось цилиндров по умолчанию = Z, а это и есть направление "из спинки" — не вращаем
    for i, (lx, ly) in enumerate(lens_xy, start=1):
        parts[f"LensRing_{i}"] = put(
            ring_cylinder(LENS_OUT_R, LENS_GLASS_R, LENS_RING_H),
            (lx, pl_y + ly, lz - LENS_RING_H/2), mat=MAT["ring"])
        # стекло утоплено в оправу на 0.25 мм — иначе грани совпадают
        parts[f"LensGlass_{i}"] = put(
            disc(LENS_GLASS_R, LENS_GLASS_H),
            (lx, pl_y + ly, lz - 0.00025 - LENS_GLASS_H/2), mat=MAT["glass"])

    # 5) Вспышка + LiDAR на правой части плато (разнесены по вертикали, как в референсе)
    fx = PL_W/2 - 0.0115
    parts["Flash"] = put(disc(0.0046, 0.0008), (fx, pl_y + 0.0125, lz - 0.0004),
                         mat=MAT["ring"])
    parts["Mic"]   = put(disc(0.0007, 0.0006), (fx, pl_y + 0.0008, lz - 0.0003),
                         mat=MAT["black"])
    parts["LiDAR"] = put(disc(0.0038, 0.0008), (fx, pl_y - 0.0105, lz - 0.0004),
                         mat=MAT["black"])

    # 6) Кнопки. Левый торец: Action + громкость. Правый: питание + Camera Control
    bx = W/2
    parts["Btn_Action"]   = put(box(0.0016, 0.0072, 0.0042), (-bx, 0.0512, 0), mat=MAT["alu_dark"])
    parts["Btn_VolUp"]    = put(box(0.0016, 0.0150, 0.0042), (-bx, 0.0300, 0), mat=MAT["alu_dark"])
    parts["Btn_VolDown"]  = put(box(0.0016, 0.0150, 0.0042), (-bx, 0.0122, 0), mat=MAT["alu_dark"])
    parts["Btn_Power"]    = put(box(0.0016, 0.0212, 0.0042), (bx, 0.0330, 0), mat=MAT["alu_dark"])
    parts["Btn_Camera"]   = put(box(0.0016, 0.0110, 0.0038), (bx, -0.0060, 0), mat=MAT["black"])

    # 6b) Задняя вставная панель (стеклянная зона MagSafe, нижние 2/3 спинки)
    parts["BackPanel"] = put(
        chamfered_slab(BP_W, BP_H, BP_T, BP_R, 0.0003),
        (0, BP_CY, -T/2 - BP_T/2 + 0.00015), mat=MAT["backpanel"])

    # 7) Динамик/порт снизу
    parts["Port_USBC"]    = put(box(0.0092, 0.0018, 0.0030), (0, -H/2 + 0.0009, 0), mat=MAT["black"])

    return trimesh.Scene(parts)


def selftest():
    """Проверки геометрии — чтобы пересечения/вылеты ловились автоматически."""
    ok = True
    lx0 = -PL_W/2 + LENS_PAIR_X
    pts = [(lx0, LENS_PAIR_Y), (lx0, -LENS_PAIR_Y), (lx0 + LENS_THIRD_DX, 0.0)]
    for i in range(3):
        for j in range(i+1, 3):
            d = np.hypot(pts[i][0]-pts[j][0], pts[i][1]-pts[j][1])
            if d < 2*LENS_OUT_R:
                print(f"  [FAIL] линзы {i+1}-{j+1} пересекаются: {d*1000:.1f} < {2*LENS_OUT_R*1000:.1f} мм")
                ok = False
    span_y = (LENS_PAIR_Y + LENS_OUT_R) * 2
    if span_y > PL_H:
        print(f"  [FAIL] линзы вылезают за плато по Y: {span_y*1000:.1f} > {PL_H*1000:.1f} мм")
        ok = False
    if pts[2][0] + LENS_OUT_R > PL_W/2:
        print("  [FAIL] третья линза вылезает за плато по X")
        ok = False
    if W - 2*BEZEL <= 0 or H - 2*BEZEL <= 0:
        print("  [FAIL] рамка съедает экран")
        ok = False

    # плато не должно вылезать за корпус
    if PL_W > W or PL_H > H:
        print("  [FAIL] плато больше корпуса")
        ok = False

    # задняя панель не должна пересекать плато и вылезать за корпус
    pl_bottom = (H/2 - PL_TOP_GAP - PL_H/2) - PL_H/2
    bp_top    = BP_CY + BP_H/2
    if bp_top > pl_bottom:
        print(f"  [FAIL] задняя панель заходит на плато: {bp_top*1000:.1f} > {pl_bottom*1000:.1f} мм")
        ok = False
    if BP_W > W or abs(BP_CY) + BP_H/2 > H/2:
        print("  [FAIL] задняя панель вылезает за корпус")
        ok = False

    # Dynamic Island должен полностью лежать внутри экрана
    scr_top = (H - 2*BEZEL) / 2
    di_top_local = (H/2 - DI_TOP) + DI_H/2
    if di_top_local > scr_top:
        print(f"  [FAIL] Dynamic Island вылезает за экран: {di_top_local*1000:.1f} > {scr_top*1000:.1f} мм")
        ok = False
    if DI_W > W - 2*BEZEL:
        print("  [FAIL] Dynamic Island шире экрана")
        ok = False
    print("  самопроверка геометрии:", "OK" if ok else "ЕСТЬ ОШИБКИ")
    return ok


if __name__ == "__main__":
    selftest()
    scene = build()
    out = "iPhone17ProMax_Orange.glb"
    scene.export(out)

    tris = sum(len(g.faces) for g in scene.geometry.values())
    verts = sum(len(g.vertices) for g in scene.geometry.values())
    lo, hi = scene.bounds
    print(f"экспорт      : {out}")
    print(f"объектов     : {len(scene.geometry)}")
    print(f"треугольников: {tris}   вершин: {verts}")
    print(f"габариты (мм): {(hi[0]-lo[0])*1000:.1f} x {(hi[1]-lo[1])*1000:.1f} x {(hi[2]-lo[2])*1000:.1f}")
    for name in scene.geometry:
        print(f"  - {name}")
