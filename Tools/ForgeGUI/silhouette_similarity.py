"""Front-silhouette similarity of two levels rendered side by side by scale_compare.py (same door scale and camera).

Masks the building pixels in each half of <prefix>_front.png (background grey, orange human bar and ground line
removed), aligns the two masks at their bottom centre and reports IoU plus size ratios. A near-duplicate scores
high IoU; a genuine upgrade should differ clearly in silhouette (see the Level-2 checklist).
Usage: python Tools/ForgeGUI/silhouette_similarity.py <prefix>
"""
import json
import sys

import numpy as np
from PIL import Image

prefix = sys.argv[1]
img = np.asarray(Image.open(prefix + "_front.png").convert("RGB")).astype(int)
bg = np.median(img[:40].reshape(-1, 3), axis=0)
diff = np.abs(img - bg).sum(axis=2) > 40
r, g, b = img[..., 0], img[..., 1], img[..., 2]
orange = (r > 190) & (g > 130) & (g < 200) & (b > 110) & (b < 180) & (r - b > 50)
mask = diff & ~orange
h, w = mask.shape
results = {}
halves = []
for name, part in (("left", mask[:, : w // 2]), ("right", mask[:, w // 2:])):
    rows = np.where(part.sum(axis=1) > 6)[0]
    rows = rows[rows < rows.max() - 3]  # drop the lot's ground bars (the bottom few px span the full 3x3 lot)
    cols = np.where(part[rows.min():rows.max() + 1].any(axis=0))[0]
    crop = part[rows.min():rows.max() + 1, cols.min():cols.max() + 1]
    halves.append(crop)
    results[name] = {"width_px": int(crop.shape[1]), "height_px": int(crop.shape[0]), "area_px": int(crop.sum())}
H = max(c.shape[0] for c in halves)
W = max(c.shape[1] for c in halves)
canvases = []
for c in halves:
    canvas = np.zeros((H, W), bool)
    y0 = H - c.shape[0]
    x0 = (W - c.shape[1]) // 2
    canvas[y0:, x0:x0 + c.shape[1]] = c
    canvases.append(canvas)
inter = (canvases[0] & canvases[1]).sum()
union = (canvases[0] | canvases[1]).sum()
results["front_silhouette_iou"] = round(float(inter / union), 3)
results["area_ratio_right_over_left"] = round(results["right"]["area_px"] / results["left"]["area_px"], 3)
results["width_ratio"] = round(results["right"]["width_px"] / results["left"]["width_px"], 3)
results["height_ratio"] = round(results["right"]["height_px"] / results["left"]["height_px"], 3)
print(json.dumps(results, indent=2))
json.dump(results, open(prefix + "_silhouette.json", "w"), indent=2)
