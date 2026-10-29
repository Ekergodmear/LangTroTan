"""Front-view target envelopes for the ten Tàng Kinh Các levels at door 0.52 (three-stage policy), with the 0.85
character and the stage lots, so the stage jumps can be judged at one scale. Planning aid only.
Writes Reports/ForgeGUI/scripture_stage_policy/stage_envelopes.png
"""
import json
import os

from PIL import Image, ImageDraw, ImageFont

OUT = "Reports/ForgeGUI/scripture_stage_policy/stage_envelopes.png"
os.makedirs(os.path.dirname(OUT), exist_ok=True)
m = json.load(open("Tools/ForgeGUI/asset_manifest.json", encoding="utf-8"))
levels = sorted((a for a in m["assets"] if a["serverBuildingId"] == "tang_kinh_cac"), key=lambda a: a["level"])
S, GAP, GY = 34, 0.8, 560
total = sum(a["targetSizeAtDoor0_52"]["width"][1] for a in levels) + GAP * 11
img = Image.new("RGB", (int(total * S) + 80, 680), (248, 248, 246))
d = ImageDraw.Draw(img)
f = ImageFont.truetype("arial.ttf", 15)
fb = ImageFont.truetype("arial.ttf", 19)
COL = {"—": (110, 110, 110), "I": (47, 111, 106), "II": (184, 145, 63), "III": (168, 56, 44)}
d.text((20, 12), "Tàng Kinh Các target envelopes, front view, door 0.52 (solid = minimum, outline = maximum). Orange bar = 0.85 disciple", fill=(0, 0, 0), font=fb)
x = 40 + GAP * S
d.line([(20, GY), (img.width - 20, GY)], fill=(120, 120, 120), width=2)
for a in levels:
    t = a["targetSizeAtDoor0_52"]
    wmin, wmax = t["width"]
    hmin, hmax = t["height"]
    col = COL[a["sizeStage"]]
    cx = x + wmax * S / 2
    d.rectangle([cx - wmax * S / 2, GY - hmax * S, cx + wmax * S / 2, GY], outline=col, width=2)
    d.rectangle([cx - wmin * S / 2, GY - hmin * S, cx + wmin * S / 2, GY], fill=col + (0,) if False else tuple(min(255, c + 120) for c in col), outline=col, width=2)
    d.rectangle([cx - 0.22 * S, GY - 0.52 * S, cx + 0.22 * S, GY], fill=(90, 30, 20))  # door
    d.rectangle([cx - wmin * S / 2 - 10, GY - 0.85 * S, cx - wmin * S / 2 - 4, GY], fill=(230, 150, 60))
    d.text((cx - 18, GY + 8), f"L{a['level']}", fill=(0, 0, 0), font=fb)
    d.text((cx - 34, GY + 32), f"{wmin:g}-{wmax:g} w", fill=col, font=f)
    d.text((cx - 34, GY + 50), f"{hmin:g}-{hmax:g} h", fill=col, font=f)
    d.text((cx - 34, GY + 68), "stage " + a["sizeStage"], fill=col, font=f)
    x += (wmax + GAP) * S
img.save(OUT)
print(OUT)
