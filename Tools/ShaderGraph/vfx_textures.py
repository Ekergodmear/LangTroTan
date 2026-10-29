"""Procedural textures for the Tàng Kinh Các Level 6 v4 spirit VFX (free, reproducible; numpy + Pillow).

Writes to Assets/Art/SonMon/VFX/L6/Textures/:
  T_SpiritEnergy_Flipbook.png  512x512, 4x4 flipbook of soft curling energy clouds (white RGB, alpha = density); every
                               cell fades to zero well inside its border, so no texture squares can show
  T_SpiritMote.png             64x64 soft round mote with a brighter core
  T_SpiritPulseRing.png        256x256 soft ring for the release pulse
  T_RuneStrip.png              1024x128 tileable strip of eight different ancient-style characters (alpha), no border lines
  T_ScriptureGlyphs.png        512x256 4x2 atlas of scripture characters (alpha), no frames
Usage: python Tools/ShaderGraph/vfx_textures.py
"""
import math
import os

import numpy as np
from PIL import Image

OUT = os.path.join(os.getcwd(), "Assets", "Art", "SonMon", "VFX", "L6", "Textures")
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(6006)


def value_noise(shape, cells, seed):
    r = np.random.default_rng(seed)
    g = r.random((cells + 1, cells + 1))
    g[-1, :] = g[0, :]
    g[:, -1] = g[:, 0]
    h, w = shape
    y = np.linspace(0, cells, h, endpoint=False)
    x = np.linspace(0, cells, w, endpoint=False)
    yi, xi = np.floor(y).astype(int), np.floor(x).astype(int)
    yf, xf = y - yi, x - xi
    yf, xf = yf * yf * (3 - 2 * yf), xf * xf * (3 - 2 * xf)
    a = g[yi][:, xi]
    b = g[yi][:, xi + 1]
    c = g[yi + 1][:, xi]
    d = g[yi + 1][:, xi + 1]
    top = a + (b - a) * xf[None, :]
    bot = c + (d - c) * xf[None, :]
    return top + (bot - top) * yf[:, None]


def fbm(shape, seed, octaves=5, base=3):
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


# ---------------------------------------------------------------- energy flipbook (4 x 4, 128 px cells)
cell, n = 128, 4
book = np.zeros((cell * n, cell * n))
yy, xx = np.mgrid[0:cell, 0:cell] / cell - .5
r = np.sqrt(xx ** 2 + yy ** 2)
ang = np.arctan2(yy, xx)
for k in range(n * n):
    t = k / (n * n)  # frames evolve: the cloud slowly curls and breathes
    swirl = ang + (1.2 + .6 * math.sin(t * 2 * math.pi)) * (0.5 - r) * 3
    sx = (np.cos(swirl) * r + .5) * cell
    sy = (np.sin(swirl) * r + .5) * cell
    big = fbm((cell * 2, cell * 2), 100 + int(t * 8), octaves=5, base=3)
    ix = np.clip((sx * 2 + 40 * math.sin(t * 6.28)).astype(int) % (cell * 2), 0, cell * 2 - 1)
    iy = np.clip((sy * 2 + 40 * math.cos(t * 6.28)).astype(int) % (cell * 2), 0, cell * 2 - 1)
    density = big[iy, ix]
    density = np.clip((density - .28) * 2.6, 0, 1) ** 1.05
    # irregular, lobed outline (no disc silhouettes), still zero well before the cell border
    lobes = fbm((cell, cell), 300 + k, octaves=3, base=2)
    reff = r * (.75 + .5 * lobes)
    falloff = np.clip(1 - (np.minimum(reff, r * 1.2) / .45) ** 2, 0, 1) ** 1.1 * (r < .47)
    a = density * falloff * (.75 + .25 * math.sin(t * 6.28 + 1))
    book[(k // n) * cell:(k // n + 1) * cell, (k % n) * cell:(k % n + 1) * cell] = a
save("T_SpiritEnergy_Flipbook.png", book)

# ---------------------------------------------------------------- mote
m = 64
yy, xx = np.mgrid[0:m, 0:m] / m - .5
r = np.sqrt(xx ** 2 + yy ** 2) * 2
save("T_SpiritMote.png", np.clip(1 - r, 0, 1) ** 2.2 + np.exp(-(r / .18) ** 2) * .6)

# ---------------------------------------------------------------- pulse ring
m = 256
yy, xx = np.mgrid[0:m, 0:m] / m - .5
r = np.sqrt(xx ** 2 + yy ** 2) * 2
ring = np.exp(-((r - .86) / .06) ** 2) * .9 + np.exp(-((r - .74) / .03) ** 2) * .35
ring *= (.75 + .25 * fbm((m, m), 7, octaves=4, base=6))
save("T_SpiritPulseRing.png", ring * (r < .99))


# ---------------------------------------------------------------- characters
def character(seed):
    r = np.random.default_rng(seed * 7919)
    G = lambda k: .18 + k * .16
    segs = []
    for _ in range(8 + r.integers(4)):
        kind, a, b, ln = r.integers(10), r.integers(4), r.integers(4), 1 + r.integers(3)
        if kind < 4:
            segs.append(((G(a), G(b)), (G(min(a + ln, 4)), G(b))))
        elif kind < 7:
            segs.append(((G(a), G(b)), (G(a), G(min(b + ln, 4)))))
        elif kind < 9:
            segs.append(((G(a), G(min(b + 1, 4))), (G(a) - .14, G(b) - .05)))
        else:
            segs.append(((G(a) + .03, G(b) + .03), (G(a) + .07, G(b) - .02)))
    segs.append(((G(0), G(2)), (G(4), G(2))))
    return segs


def seg_dist(px, py, a, b):
    ax, ay = a
    bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / max(dx * dx + dy * dy, 1e-6), 0, 1)
    return np.sqrt((px - ax - t * dx) ** 2 + (py - ay - t * dy) ** 2)


def glyph_alpha(seed, size, core=.05, halo=.15, halo_gain=.3):
    yy, xx = np.mgrid[0:size, 0:size]
    px, py = (xx + .5) / size, 1 - (yy + .5) / size
    d = np.ones((size, size))
    for a, b in character(seed):
        d = np.minimum(d, seg_dist(px, py, a, b))
    # brush taper: slightly uneven stroke weight
    wobble = .85 + .3 * value_noise((size, size), 4, seed)
    return np.maximum(np.clip(1 - d / (core * wobble), 0, 1), np.clip(1 - d / halo, 0, 1) * halo_gain)


# rune strip: eight different characters per repeat (no visible repetition along the band), faint carved hairlines only
strip = np.zeros((128, 1024))
for i, seed in enumerate((11, 23, 31, 47, 59, 67, 79, 97)):
    g = glyph_alpha(seed, 100, core=.05, halo=.16, halo_gain=.3)
    strip[14:114, i * 128 + 14:i * 128 + 114] = np.maximum(strip[14:114, i * 128 + 14:i * 128 + 114], g)
for y in (6, 121):
    strip[y - 1:y + 2, :] = np.maximum(strip[y - 1:y + 2, :], .22)
save("T_RuneStrip.png", strip)

# scripture glyph atlas
atlas = np.zeros((256, 512))
for gidx in range(8):
    g = glyph_alpha(gidx + 3, 112, core=.05, halo=.14, halo_gain=.35)
    oy, ox = (gidx // 4) * 128 + 8, (gidx % 4) * 128 + 8
    atlas[oy:oy + 112, ox:ox + 112] = g
save("T_ScriptureGlyphs.png", atlas)
