#!/usr/bin/env python3
"""Static gold→champagne→cyan TMP shimmer for HOST_LOBBY_NAME (not animated)."""
import sys
plain = sys.argv[1] if len(sys.argv) > 1 else "www\u00b7bonelab\u00b7fun"
phase = float(sys.argv[2]) if len(sys.argv) > 2 else 0.18
palette = [
    (1.00, 0.88, 0.40), (1.00, 0.82, 0.12), (1.00, 0.72, 0.22),
    (1.00, 0.94, 0.72), (0.92, 0.95, 1.00), (0.55, 0.88, 1.00),
    (0.40, 0.72, 1.00), (0.85, 0.90, 1.00), (1.00, 0.90, 0.50),
]
def sample(t01):
    n = len(palette); x = t01 * n; i0 = int(x) % n; i1 = (i0 + 1) % n
    f = x - int(x); f = f * f * (3 - 2 * f)
    c0, c1 = palette[i0], palette[i1]
    return tuple(c0[i] + (c1[i] - c0[i]) * f for i in range(3))
colored = [c for c in plain if c != " "]
out, vi = [], 0
for c in plain:
    if c == " ":
        out.append(" "); continue
    t = phase + vi * (0.7 / max(1, len(colored))); t -= int(t)
    r, g, b = sample(t)
    out.append(f"<color=#{int(round(r*255)):02x}{int(round(g*255)):02x}{int(round(b*255)):02x}>{c}")
    vi += 1
sys.stdout.write("".join(out))
