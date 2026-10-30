"""Procedural textures for the Tàng Kinh Các Level 6 spirit VFX v5 candidate (original, reproducible; numpy + Pillow).

Writes to Assets/Art/SonMon/VFX/L6v5/Textures/:
  T_ReleaseWaveV5.png       512x512 soft gold-jade release wave: a broad brushed band with a faint inner wash (RGB carries
                            the colour: jade inside, warm gold on the band), feathered to zero before the edge
  T_ReleaseGlowV5.png       256x256 soft warm disc for the release bloom over the court
  T_SealBrushV5.png         512x512 seal: two open brush-stroke arcs and a cloud-scroll spiral (no ticks, no crosshair)
  T_RuneStripV5.png         1024x128 tileable strip of eight curved, seal-script-like characters (no edge lines)
  T_ScriptureGlyphsV5.png   512x256 4x2 atlas of bolder curved characters for the floating scripture
  T_OrnamentHaloV5.png      128x128 soft halo (brighter rim, dim centre) instead of a solid ball
  T_SpiritMistV5_Flipbook.png 512x512 4x4 flipbook of elongated, eroded mist wisps (no round discs)
Usage: python Tools/ShaderGraph/vfx_textures_v5.py
"""
import math
import os

import numpy as np
from PIL import Image

OUT = os.path.join(os.getcwd(), "Assets", "Art", "SonMon", "VFX", "L6v5", "Textures")
os.makedirs(OUT, exist_ok=True)

JADE = np.array([.42, .93, .76])
GOLD = np.array([1.0, .74, .30])


def value_noise(shape, cells, seed):
    r = np.random.default_rng(seed)
    cy, cx = (cells, cells) if np.isscalar(cells) else cells
    g = r.random((cy + 1, cx + 1))
    g[-1, :] = g[0, :]
    g[:, -1] = g[:, 0]
    h, w = shape
    y = np.linspace(0, cy, h, endpoint=False)
    x = np.linspace(0, cx, w, endpoint=False)
    yi, xi = np.floor(y).astype(int), np.floor(x).astype(int)
    yf, xf = y - yi, x - xi
    yf, xf = yf * yf * (3 - 2 * yf), xf * xf * (3 - 2 * xf)
    a, b = g[yi][:, xi], g[yi][:, xi + 1]
    c, d = g[yi + 1][:, xi], g[yi + 1][:, xi + 1]
    top = a + (b - a) * xf[None, :]
    bot = c + (d - c) * xf[None, :]
    return top + (bot - top) * yf[:, None]


def fbm(shape, seed, octaves=4, base=3):
    total, amp, norm = np.zeros(shape), 1.0, 0.0
    for o in range(octaves):
        total += amp * value_noise(shape, base * 2 ** o, seed + o * 17)
        norm += amp
        amp *= .5
    return total / norm


def save(name, alpha, rgb=None):
    a = np.clip(alpha, 0, 1)
    h, w = a.shape
    img = np.zeros((h, w, 4), np.uint8)
    col = np.ones((h, w, 3)) if rgb is None else rgb
    img[..., :3] = np.clip(col * 255, 0, 255).astype(np.uint8)
    img[..., 3] = (a * 255).astype(np.uint8)
    Image.fromarray(img, "RGBA").save(os.path.join(OUT, name))
    print("wrote", name)


def polar(size):
    yy, xx = np.mgrid[0:size, 0:size]
    x, y = (xx + .5) / size * 2 - 1, (yy + .5) / size * 2 - 1
    return np.sqrt(x * x + y * y), np.arctan2(y, x)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- release wave
m = 512
r, ang = polar(m)
streak = fbm((m, m), 53, octaves=3, base=5)  # uneven brushed density along the band
band = np.exp(-((r - .80) / .085) ** 2)
wash = .32 * smooth(.15, .78, r) * (r < .80) + .32 * (r >= .80) * np.exp(-((r - .80) / .085) ** 2)
alpha = np.maximum(band * (.65 + .35 * streak), wash) * smooth(.99, .90, r)
mix = smooth(.55, .80, r)[..., None]
save("T_ReleaseWaveV5.png", alpha, JADE * (1 - mix) + GOLD * mix)

# ---------------------------------------------------------------- release glow
m = 256
r, _ = polar(m)
save("T_ReleaseGlowV5.png", np.exp(-(r / .55) ** 2) * smooth(1.0, .85, r),
     np.broadcast_to(np.array([1.0, .86, .55]), (m, m, 3)))


# ---------------------------------------------------------------- brush strokes
def brush_stroke(size, pts, width, seed, dry=.35):
    """Alpha of a tapered brush stroke along a polyline pts (Nx2 in 0..1 texture space, y up)."""
    yy, xx = np.mgrid[0:size, 0:size]
    px, py = (xx + .5) / size, 1 - (yy + .5) / size
    best = np.full((size, size), 9.0)
    tpar = np.zeros((size, size))
    n = len(pts)
    for i in range(n - 1):
        (ax, ay), (bx, by) = pts[i], pts[i + 1]
        dx, dy = bx - ax, by - ay
        t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(dx * dx + dy * dy, 1e-9), 0, 1)
        d = np.sqrt((px - ax - t * dx) ** 2 + (py - ay - t * dy) ** 2)
        closer = d < best
        best = np.where(closer, d, best)
        tpar = np.where(closer, (i + t) / (n - 1), tpar)
    taper = np.sin(np.clip(tpar, 0, 1) * math.pi) ** .6 * .85 + .15  # thin entry and exit, full belly
    w = width * taper
    core = np.clip(1 - best / w, 0, 1) ** .7
    # dry-brush: streaks along the stroke direction (noise sampled by stroke parameter and offset)
    hair = value_noise((size, size), 18, seed)
    dryk = 1 - dry * np.clip(hair - .45, 0, 1) * 2 * smooth(.4, 1.0, best / np.maximum(w, 1e-6))
    return core * dryk


def arc(cx, cy, rad, a0, a1, n=96):
    t = np.linspace(a0, a1, n)
    return np.stack([cx + np.cos(t) * rad, cy + np.sin(t) * rad], 1)


def spiral(cx, cy, r0, r1, turns, a0, n=140):
    t = np.linspace(0, 1, n)
    a = a0 + t * turns * 2 * math.pi
    rr = r0 + (r1 - r0) * t
    return np.stack([cx + np.cos(a) * rr, cy + np.sin(a) * rr], 1)


# ---------------------------------------------------------------- seal
m = 512
outer = brush_stroke(m, arc(.5, .5, .40, math.radians(105), math.radians(105 + 318)), .045, 61)
inner = brush_stroke(m, arc(.5, .5, .27, math.radians(290), math.radians(290 + 250)), .026, 62)
scroll = brush_stroke(m, spiral(.5, .5, .015, .15, 1.35, math.radians(30)), .03, 63)
curl = brush_stroke(m, spiral(.585, .395, .01, .06, .9, math.radians(200)), .018, 64)
r, _ = polar(m)
wash = .10 * smooth(.95, .55, r)
save("T_SealBrushV5.png", np.maximum.reduce([outer, inner * .9, scroll, curl * .85, wash]))


# ---------------------------------------------------------------- curved characters
def curved_character(seed, size, width):
    """Seal-script-like character: 3-5 curved strokes (arcs, hooks, gentle S-curves), never straight crossings."""
    rnd = np.random.default_rng(seed * 104729)
    out = np.zeros((size, size))
    strokes = 3 + rnd.integers(3)
    for k in range(strokes):
        kind = rnd.integers(3)
        cx, cy = .3 + .4 * rnd.random(), .3 + .4 * rnd.random()
        if kind == 0:  # open arc
            rad = .14 + .14 * rnd.random()
            a0 = rnd.random() * 2 * math.pi
            pts = arc(cx, cy, rad, a0, a0 + math.radians(120 + 140 * rnd.random()), 48)
        elif kind == 1:  # hook: a sweep that curls at the end
            pts = spiral(cx, cy, .2 + .06 * rnd.random(), .05, .45 + .3 * rnd.random(), rnd.random() * 2 * math.pi, 56)
        else:  # gentle S curve
            t = np.linspace(0, 1, 48)
            ang = rnd.random() * math.pi
            ux, uy = math.cos(ang), math.sin(ang)
            off = .07 * np.sin(t * 2 * math.pi + rnd.random())
            pts = np.stack([cx + (t - .5) * .5 * ux - off * uy, cy + (t - .5) * .5 * uy + off * ux], 1)
        pts = np.clip(pts, .1, .9)
        out = np.maximum(out, brush_stroke(size, pts, width * (.8 + .4 * rnd.random()), seed * 31 + k, dry=.25))
    return out


strip = np.zeros((128, 1024))
for i, seed in enumerate((3, 5, 8, 13, 21, 34, 55, 89)):
    g = curved_character(seed, 112, .055)
    strip[8:120, i * 128 + 8:i * 128 + 120] = np.maximum(strip[8:120, i * 128 + 8:i * 128 + 120], g)
save("T_RuneStripV5.png", strip)

atlas = np.zeros((256, 512))
for gidx in range(8):
    g = curved_character(101 + gidx, 120, .065)
    oy, ox = (gidx // 4) * 128 + 4, (gidx % 4) * 128 + 4
    atlas[oy:oy + 120, ox:ox + 120] = g
save("T_ScriptureGlyphsV5.png", atlas)

# ---------------------------------------------------------------- ornament halo
m = 128
r, _ = polar(m)
save("T_OrnamentHaloV5.png", (.55 * np.exp(-((r - .45) / .2) ** 2) + .22 * np.exp(-(r / .28) ** 2)) * smooth(1.0, .82, r))

# ---------------------------------------------------------------- mist wisps (4 x 4, 128 px cells)
cell, n = 128, 4
book = np.zeros((cell * n, cell * n))
yy, xx = np.mgrid[0:cell, 0:cell] / cell - .5
for k in range(n * n):
    t = k / (n * n)
    # elongated (2:1) and eroded by noise so no disc silhouette reads; still zero before the cell border
    ex, ey = xx / .38, yy / .19
    rr = np.sqrt(ex * ex + ey * ey)
    big = fbm((cell, cell), 700 + k * 3, octaves=3, base=2)
    fine = fbm((cell, cell), 900 + k * 5, octaves=2, base=4)
    density = (.35 + .65 * smooth(.15, .8, big)) * (.8 + .2 * fine)  # soft, continuous density, never sparse
    shape = smooth(1.0, .3, rr + .55 * (fbm((cell, cell), 1100 + k, octaves=2, base=2) - .5))
    border = smooth(.49, .40, np.abs(xx)) * smooth(.49, .40, np.abs(yy))  # soft, never a clipped edge
    a = density * shape * border
    a = a / max(a.max(), 1e-6) * (.75 + .15 * math.sin(t * 6.28))  # every cell equally present
    book[(k // n) * cell:(k // n + 1) * cell, (k % n) * cell:(k % n + 1) * cell] = a
save("T_SpiritMistV5_Flipbook.png", book)
