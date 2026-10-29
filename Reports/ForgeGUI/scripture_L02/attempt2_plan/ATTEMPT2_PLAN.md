# Tàng Kinh Các Level 2, attempt 2: plan for approval (nothing generated)

## Why attempt 1 was rejected (measured)

Evidence: `../rejected_attempt1/L1_vs_L2_rejected_comparison.png` (front, 45° and top views at the same door-0.52 scale and camera).

| | L1 (approved with notes) | L2 attempt 1 (rejected) |
|---|---|---|
| World size at door 0.52 (w × d × h) | 2.18 × 1.55 × 1.72 | 2.37 × 1.65 × **1.67 (lower than L1)** |
| Front pillars / steps / lattice windows | 4 / 3 / 2 | 4 / 3 / 2 |
| Side storage rooms | — | none |
| Roofline | single roof | single roof, same front-right hook |
| Front-silhouette overlap with L1 (IoU) | — | **0.72** (near-duplicate) |
| Front-silhouette area vs L1 | — | **0.93×** (smaller) |

The image in the References slot evidently drove the geometry. Attempt 2 uses **no reference image**.

## Proposed prompt: revision 3 (1,818 characters, sha256 `458e92d6…`)

The Art Bible style block is unchanged, followed by:

> Tàng Kinh Các — Scripture Pavilion, level 2 of 10, "Tàng Thư Các": a wide rectangular one-story scripture hall, clearly wider and deeper than a small one-room pavilion. Silhouette first: a central main hall under a broad curved jade-teal roof, flanked left and right by two distinct lower side storage rooms, each with its own smaller curved side roof joined to the main hall, giving a stepped three-part roofline when seen from the front. A full-width front porch with six prominent red lacquered pillars in one row. Five wide ivory stone steps at the centre of the front. Four lattice windows across the front, two on each side of the double wooden doors. Carved wooden railings along the porch edge. A blank carved wooden signboard above the main doors (no lettering). The main roof has four matching upturned corners, symmetrical, all parts connected. Single storey only, no upper floor, no tower. About 6 main-door widths wide and 4.5 deep, on a low ivory stone platform, for a 3x3 square game footprint. Same materials as the earlier level: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments.

The unchanged technical suffix follows (one self-contained structure, entrance front, static, no particles, floating objects, characters, terrain or text).

Changes from revision 2:
- **Silhouette is described first.** Adds the two side roof extensions and stepped roofline, six pillars, and the wider, deeper plan in door widths.
- **Drops the reference-image continuity line,** because there is no reference this time.
- **Drops the family theme phrase "layered pagoda roofs",** so it can't push toward multiple tiers.

Settings: Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, Automatically Rig off, 1 generation, **References empty, no style reference**. Estimated cost 2,568–4,585 credits (the two measured jobs).

## Expected silhouette

`expected_silhouette.png` shows the front elevation and plan at door-0.52 scale, over L1's envelope (dashed) and the 3 × 3 lot:
- tall central hall roof with two lower side roofs (stepped three-part roofline);
- six pillars across a full-width porch;
- five central steps, four lattice windows, wooden rails, blank signboard.

**Target envelope:** 2.6–2.8 w × 1.9–2.1 d × 1.8–2.0 h, plinth inside the 3 × 3 lot. Width is capped by the L5 candidate (2.97 wide): L2 must leave room for L3 and L4 to grow before L5.

## Comparison checklist (applied after generation, against L1 at identical scale and camera)

| # | Requirement | Pass condition |
|---|---|---|
| 1 | Exactly one storey | No upper floor, gallery or second roof tier above the main hall |
| 2 | Substantially wider and deeper | At door 0.52: width ≥ 2.6 (≥ 1.19× L1), depth ≥ 1.9 (≥ 1.23× L1), height ≥ 1.8 (≥ 1.05× L1); width ≤ 2.9 (room before L5); eaves within 3.6 |
| 3 | Six front porch pillars | 6 pillars counted in the front view |
| 4 | Wider roof with two side roof extensions | Two separate lower side roofs visible in the front and top views; stepped roofline |
| 5 | Five front steps | 5 risers counted |
| 6 | Four lattice windows | 4 on the front facade |
| 7 | Two side storage rooms | Two enclosed wings, symmetric, attached to the main hall |
| 8 | Carved wooden railings | Railing reads as wood (red or brown timber), not the L1 stone balustrade |
| 9 | Blank wooden signboard | Board above the doors, no lettering |
| 10 | Same materials as L1 | Jade-teal tiles, dark red lacquer, ivory stone, restrained gold |
| 11 | Not a near-duplicate | Front-silhouette IoU vs L1 ≤ 0.60 **and** silhouette area ≥ 1.30× L1 (attempt 1: 0.72 / 0.93×) — `Tools/ForgeGUI/silhouette_similarity.py` |
| 12 | Clean geometry | Four symmetric main-roof corners; no holes or white fragments; finished rear; validator passes (no rig, under 60k triangles) |

A result failing #2, #4, #7 or #11 is marked `rejected_visual_similarity` or a candidate requiring review. It is never regenerated automatically.
