#!/usr/bin/env python3
"""Запекаем модель телефона в компактный бинарь для загрузки в рантайме мода.

Формат (little-endian):
  magic  "LPH1"
  int32  partCount
  для каждой части:
    uint8  len(name), utf8 name
    uint8  materialId
    int32  vertCount
    float32[3] * vertCount     позиции
    uint8  hasUV (0/1)
    float32[2] * vertCount     UV (если hasUV)
    int32  indexCount
    uint16 * indexCount        треугольники
"""
import struct
import numpy as np
import trimesh

MAT_ID = {
    "M_Alu_CosmicOrange": 0,
    "M_Alu_Dark":         1,
    "M_Screen":           2,
    "M_LensGlass":        3,
    "M_LensRing":         4,
    "M_Black":            5,
    "M_DynamicIsland":    6,
    "M_BackPanel_Glass":  7,
}


def mat_of(geom):
    try:
        return MAT_ID.get(geom.visual.material.name, 0)
    except Exception:
        return 0


def bake(src="iPhone17ProMax_Orange.glb", out="phone.bin"):
    scene = trimesh.load(src)
    buf = bytearray()
    buf += b"LPH1"
    buf += struct.pack("<i", len(scene.geometry))

    total_v = total_t = 0
    for name, g in scene.geometry.items():
        nb = name.encode("utf-8")
        buf += struct.pack("<B", len(nb)) + nb
        buf += struct.pack("<B", mat_of(g))

        V = np.asarray(g.vertices, dtype=np.float32)
        F = np.asarray(g.faces, dtype=np.int64)
        assert len(V) < 65535, f"{name}: слишком много вершин для uint16"

        buf += struct.pack("<i", len(V))
        buf += V.astype("<f4").tobytes()

        uv = None
        try:
            if g.visual.uv is not None and len(g.visual.uv) == len(V):
                uv = np.asarray(g.visual.uv, dtype=np.float32)
        except Exception:
            pass
        buf += struct.pack("<B", 1 if uv is not None else 0)
        if uv is not None:
            buf += uv.astype("<f4").tobytes()

        # Unity: левосторонняя система -> инвертируем X и порядок обхода
        idx = F[:, [0, 2, 1]].reshape(-1).astype("<u2")
        buf += struct.pack("<i", len(idx))
        buf += idx.tobytes()

        total_v += len(V)
        total_t += len(F)

    open(out, "wb").write(bytes(buf))
    print(f"  {out}: {len(scene.geometry)} частей, {total_v} вершин, "
          f"{total_t} треугольников, {len(buf)/1024:.1f} КБ")


def bake_mirrored_x(src="iPhone17ProMax_Orange.glb", out="phone.bin"):
    """glTF (правосторонняя, +X вправо) -> Unity (левосторонняя): зеркалим X."""
    scene = trimesh.load(src)
    for g in scene.geometry.values():
        v = np.asarray(g.vertices)
        v[:, 0] *= -1.0
        g.vertices = v
    scene.export("_unity_tmp.glb")
    bake("_unity_tmp.glb", out)


if __name__ == "__main__":
    print("запекание модели:")
    bake_mirrored_x()
