"""Expected Level-2 (attempt 2) silhouette schematic at the approved door-0.52 scale, overlaid on Level 1's measured
envelope and the 3x3 lot. Planning aid only (not art). Writes Reports/ForgeGUI/scripture_L02/attempt2_plan/expected_silhouette.png
"""
import os

from PIL import Image, ImageDraw, ImageFont

OUT = "Reports/ForgeGUI/scripture_L02/attempt2_plan/expected_silhouette.png"
os.makedirs(os.path.dirname(OUT), exist_ok=True)
S = 210  # pixels per Unity unit
f = ImageFont.truetype("arial.ttf", 22)
fs = ImageFont.truetype("arial.ttf", 17)
img = Image.new("RGB", (2000, 1050), (248, 248, 246))
d = ImageDraw.Draw(img)
RED, ROOF, STONE, GOLD, GREY, L1C = (150, 40, 34), (47, 111, 106), (225, 218, 200), (184, 145, 63), (120, 120, 120), (200, 120, 60)


def dashed_rect(x0, y0, x1, y1, color, dash=10):
    for x in range(int(x0), int(x1), dash * 2):
        d.line([(x, y0), (min(x + dash, x1), y0)], fill=color, width=3)
        d.line([(x, y1), (min(x + dash, x1), y1)], fill=color, width=3)
    for y in range(int(y0), int(y1), dash * 2):
        d.line([(x0, y), (x0, min(y + dash, y1))], fill=color, width=3)
        d.line([(x1, y), (x1, min(y + dash, y1))], fill=color, width=3)


# ---------------- front elevation (left)
gx, gy = 60, 900  # ground line origin
cx = gx + 1.8 * S  # building centre
d.text((gx, 20), "FRONT ELEVATION (door 0.52 scale)", fill=(20, 20, 20), font=f)
d.line([(gx - 20, gy), (gx + 3.7 * S, gy)], fill=GREY, width=2)
d.text((gx, gy + 10), "3x3 lot = 3.0 (eaves up to 3.6)", fill=GREY, font=fs)
d.line([(cx - 1.5 * S, gy + 40), (cx + 1.5 * S, gy + 40)], fill=GREY, width=4)
W, H = 2.7, 1.9  # target envelope
plinth_h = 0.22
d.rectangle([cx - 1.35 * S, gy - plinth_h * S, cx + 1.35 * S, gy], fill=STONE, outline=GREY)
# central hall
d.rectangle([cx - 0.78 * S, gy - (plinth_h + 0.95) * S, cx + 0.78 * S, gy - plinth_h * S], fill=RED)
d.polygon([(cx - 0.92 * S, gy - (plinth_h + 0.95) * S), (cx - 0.5 * S, gy - H * S), (cx + 0.5 * S, gy - H * S), (cx + 0.92 * S, gy - (plinth_h + 0.95) * S)], fill=ROOF)
for side in (-1, 1):  # upturned main roof corners
    d.line([(cx + side * 0.92 * S, gy - (plinth_h + 0.95) * S), (cx + side * 1.02 * S, gy - (plinth_h + 1.1) * S)], fill=ROOF, width=8)
# side storage rooms: lower wings, each with its own separate side roof (stepped three-part roofline)
WING = (64, 132, 124)
for side in (-1, 1):
    x_in, x_out = cx + side * 0.78 * S, cx + side * 1.3 * S
    d.rectangle([min(x_in, x_out), gy - (plinth_h + 0.6) * S, max(x_in, x_out), gy - plinth_h * S], fill=RED)
    d.polygon([(x_in, gy - (plinth_h + 0.86) * S), (x_out + side * 0.1 * S, gy - (plinth_h + 0.6) * S), (x_in, gy - (plinth_h + 0.6) * S)], fill=WING, outline=(30, 80, 75))
    d.line([(x_out + side * 0.1 * S, gy - (plinth_h + 0.6) * S), (x_out + side * 0.16 * S, gy - (plinth_h + 0.7) * S)], fill=WING, width=6)
    d.text((min(x_in, x_out) + 8, gy - (plinth_h + 0.55) * S), "side room", fill=(255, 235, 200), font=fs)
d.rectangle([cx - 0.3 * S, gy - (plinth_h + 1.08) * S, cx + 0.3 * S, gy - (plinth_h + 0.98) * S], fill=(120, 70, 30))  # signboard
d.text((cx - 0.27 * S, gy - (plinth_h + 1.08) * S - 2), "blank sign", fill=(240, 220, 170), font=fs)
# six porch pillars
for i in range(6):
    x = cx - 1.1 * S + i * 0.44 * S
    d.rectangle([x - 7, gy - (plinth_h + 0.95) * S, x + 7, gy - plinth_h * S], fill=(110, 20, 18))
# door (0.52) and four lattice windows
d.rectangle([cx - 0.22 * S, gy - (plinth_h + 0.52) * S, cx + 0.22 * S, gy - plinth_h * S], fill=(90, 30, 20), outline=GOLD, width=3)
for x in (-0.66, -0.4, 0.4, 0.66):
    d.rectangle([cx + (x - 0.08) * S, gy - (plinth_h + 0.6) * S, cx + (x + 0.08) * S, gy - (plinth_h + 0.3) * S], outline=GOLD, width=3)
# five steps
for i in range(5):
    w = 0.5 + i * 0.04
    d.rectangle([cx - w / 2 * S, gy - (plinth_h - i * 0.044) * S, cx + w / 2 * S, gy - (plinth_h - (i + 1) * 0.044) * S], fill=STONE, outline=GREY)
# carved wooden railings
for side in (-1, 1):
    d.line([(cx + side * 0.3 * S, gy - (plinth_h + 0.12) * S), (cx + side * 1.32 * S, gy - (plinth_h + 0.12) * S)], fill=(120, 60, 25), width=5)
# level 1 envelope for comparison
dashed_rect(cx - 2.18 / 2 * S, gy - 1.72 * S, cx + 2.18 / 2 * S, gy, L1C)
d.text((cx - 1.09 * S, gy - 1.72 * S - 26), "Level 1 envelope 2.18 w x 1.72 h", fill=L1C, font=fs)
d.text((cx + 0.6 * S, gy - H * S - 30), f"L2 target {W} w x {H} h", fill=ROOF, font=fs)
# door height marker
d.line([(gx + 3.55 * S, gy), (gx + 3.55 * S, gy - 0.52 * S)], fill=(0, 0, 0), width=3)
d.text((gx + 3.58 * S, gy - 0.4 * S), "door 0.52", fill=(0, 0, 0), font=fs)

# ---------------- plan (right)
px, py = 1080, 120
d.text((px, 20), "PLAN / TOP (entrance at bottom)", fill=(20, 20, 20), font=f)
lot = 3.0 * S
d.rectangle([px, py, px + lot, py + lot], outline=GREY, width=3)
for i in (1, 2):
    d.line([(px + i * S, py), (px + i * S, py + lot)], fill=(200, 200, 200))
    d.line([(px, py + i * S), (px + lot, py + i * S)], fill=(200, 200, 200))
ox, oy = px + lot / 2, py + lot / 2
d.rectangle([ox - 1.35 * S, oy - 1.0 * S, ox + 1.35 * S, oy + 1.0 * S], fill=STONE, outline=GREY)  # platform 2.7 x 2.0
d.rectangle([ox - 0.78 * S, oy - 0.85 * S, ox + 0.78 * S, oy + 0.35 * S], fill=ROOF)  # main hall roof
for side in (-1, 1):
    d.rectangle([min(ox + side * 0.78 * S, ox + side * 1.3 * S), oy - 0.6 * S, max(ox + side * 0.78 * S, ox + side * 1.3 * S), oy + 0.3 * S], fill=(80, 140, 132))
for i in range(6):
    x = ox - 1.1 * S + i * 0.44 * S
    d.ellipse([x - 8, oy + 0.55 * S - 8, x + 8, oy + 0.55 * S + 8], fill=(110, 20, 18))
d.rectangle([ox - 0.3 * S, oy + 1.0 * S, ox + 0.3 * S, oy + 1.24 * S], fill=(210, 205, 190), outline=GREY)  # five steps
d.text((ox - 0.28 * S, oy + 1.04 * S), "5 steps", fill=(60, 60, 60), font=fs)
d.text((ox - 0.75 * S, oy + 0.62 * S), "porch: 6 pillars, wooden rails", fill=(60, 60, 60), font=fs)
d.text((ox - 1.3 * S, oy - 0.95 * S - 2), "side room", fill=(255, 255, 255), font=fs)
d.text((ox + 0.82 * S, oy - 0.95 * S - 2), "side room", fill=(255, 255, 255), font=fs)
dashed_rect(ox - 2.18 / 2 * S, oy - 1.55 / 2 * S, ox + 2.18 / 2 * S, oy + 1.55 / 2 * S, L1C)
d.text((px, py + lot + 12), "dashed = Level 1 footprint 2.18 x 1.55   |   target 2.7 x 2.0 (plinth inside the 3x3 lot)", fill=(60, 60, 60), font=fs)
img.save(OUT)
print(OUT)
