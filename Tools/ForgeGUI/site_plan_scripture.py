"""Tàng Kinh Các three-stage site plan: draws the proposed visual lots for each stage on the fresh-world server grid
(from site_snapshot.mjs) and counts what each lot overlaps. Planning aid only; changes no game data.
Usage: python Tools/ForgeGUI/site_plan_scripture.py Reports/WorldScale/site_plan/fresh_world_snapshot.json
"""
import json
import sys

from PIL import Image, ImageDraw, ImageFont

snap = json.load(open(sys.argv[1]))
OUT = "Reports/WorldScale/site_plan/scripture_stage_lots.png"
# Proposed lots in server cells [x0, x1) x [z0, z1): centred on the current lot (x = -8.5), front edge kept at z = 4,
# growth backwards (+z, away from the courtyard) and sideways.
LOTS = {"Current 3x3 (L1)": (-10, -7, 4, 7), "Stage I 7x5 (L2-L5)": (-12, -5, 4, 9),
        "Stage II 9x7 (L6-L8)": (-13, -4, 4, 11), "Stage III 13x10 (L9-L10)": (-15, -2, 4, 14)}
COLORS = {"Current 3x3 (L1)": (40, 40, 40), "Stage I 7x5 (L2-L5)": (47, 111, 106), "Stage II 9x7 (L6-L8)": (184, 145, 63),
          "Stage III 13x10 (L9-L10)": (168, 56, 44)}


def overlaps(lot):
    x0, x1, z0, z1 = lot
    hits = {"buildings": [], "trees": 0, "stone": 0, "deer": 0}
    for b in snap["buildings"]:
        if b["id"] == "core:tang_kinh_cac":
            continue
        if b["x"] < x1 and b["x"] + b["w"] > x0 and b["z"] < z1 and b["z"] + b["d"] > z0:
            hits["buildings"].append(b["id"])
    for n in snap["nodes"]:
        if x0 <= n["x"] < x1 and z0 <= n["z"] < z1:
            hits["trees" if n["kind"] == "tree" else n["kind"]] += 1
    gap = min(abs(b["z"] + b["d"] - z0) if b["id"] == "core:chanh_dien" else 99 for b in snap["buildings"])
    hits["gapToChanhDienCells"] = gap
    return hits


report = {name: {"cells": f"x {lot[0]}..{lot[1]} , z {lot[2]}..{lot[3]}", **overlaps(lot)} for name, lot in LOTS.items()}
S, X0, Z1 = 22, -26, 26  # px per cell; view x -26..26, z -26..26 (north = +z at the top)
img = Image.new("RGB", (52 * S + 360, 52 * S + 60), (248, 248, 246))
d = ImageDraw.Draw(img)
f = ImageFont.truetype("arial.ttf", 15)
fb = ImageFont.truetype("arial.ttf", 18)
px = lambda x, z: (30 + (x - X0) * S, 30 + (Z1 - z) * S)
for i in range(-26, 27):
    d.line([px(i, -26), px(i, 26)], fill=(228, 228, 224))
    d.line([px(-26, i), px(26, i)], fill=(228, 228, 224))
cx, cz = px(0, 0)
d.ellipse([cx - 24 * S, cz - 24 * S, cx + 24 * S, cz + 24 * S], outline=(170, 170, 170), width=2)
for n in snap["nodes"]:
    x, y = px(n["x"], n["z"])
    col = {"tree": (90, 150, 80), "stone": (130, 130, 140), "deer": (190, 130, 70)}[n["kind"]]
    d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=col)
for b in snap["buildings"]:
    a, c = px(b["x"], b["z"] + b["d"]), px(b["x"] + b["w"], b["z"])
    d.rectangle([a, c], fill=(205, 200, 190), outline=(90, 90, 90), width=2)
    d.text((a[0] + 3, a[1] + 3), b["id"].replace("core:", "").replace("building:1", "store"), fill=(30, 30, 30), font=f)
for name, (x0, x1, z0, z1) in reversed(list(LOTS.items())):
    a, c = px(x0, z1), px(x1, z0)
    d.rectangle([a, c], outline=COLORS[name], width=4)
gx, gy = px(0, -4)
d.text((gx - 30, gy + 4), "gate (0,-4)", fill=(0, 0, 0), font=f)
d.text((30, 4), "Fresh-world server grid (1 cell = 1 Unity unit). Tàng Kinh Các proposed stage lots; front edge fixed at z = 4", fill=(0, 0, 0), font=fb)
lx = 52 * S + 50
y = 40
for name, r in report.items():
    d.rectangle([lx, y, lx + 18, y + 18], outline=COLORS[name], width=4)
    d.text((lx + 26, y), name, fill=(0, 0, 0), font=fb)
    d.text((lx + 26, y + 24), f"trees {r['trees']}  stone {r['stone']}  deer {r['deer']}", fill=(60, 60, 60), font=f)
    d.text((lx + 26, y + 44), "buildings: " + (", ".join(r["buildings"]) or "none"), fill=(60, 60, 60), font=f)
    y += 80
d.text((lx, y + 10), "grey circle: build radius 24", fill=(60, 60, 60), font=f)
d.text((lx, y + 30), "green dots: trees, grey: stone, brown: deer", fill=(60, 60, 60), font=f)
img.save(OUT)
json.dump(report, open(OUT.replace(".png", ".json"), "w"), indent=1)
print(json.dumps(report, indent=1))
