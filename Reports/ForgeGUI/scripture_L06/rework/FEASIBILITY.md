# Tàng Kinh Các Level 6: non-destructive rework feasibility (for your review)

**Original kept:** `work/forgegui/downloads/scripture_L06_forge_d31673e5.glb` stays read-only, with its sha256 `0d31ded5…` re-verified after every run. Its textures and provenance are unchanged.

**Credits and production:** no credits were spent and nothing was integrated.

**Prototype:** produced by `Tools/ForgeGUI/l6_rework_prototype.py`. The result exists only in `Reports/` and in an isolated `_Candidates` folder used by the unsaved QA scene.

## What the mesh allows

- **Separation:** the model is one mesh made of about 5,600 disconnected fragments (typical of generated meshes), so it can't be separated by parts. Plane cuts work, because the hall and each wing are joined only by a short gallery about 0.2 wide (at x ≈ −1.53 / +1.52). Cutting there leaves the hall, roofs, the round jade ornament and the wing geometry untouched.
- **UVs and textures:** the cuts and moves keep the original UVs and texture. Nothing is stretched.

## What the prototype does (door stays 0.52, no scaling)

1. **Width:** each wing moves 0.30 outward. The gaps are filled with duplicated slices of the original gallery, so the buildings become 7.15 wide (was 6.55).
2. **Height:** the whole complex is raised 0.94 onto a new **two-tier terrace**. Each tier is 0.47, exactly the model's own plinth and first-flight height, so the step size is unchanged.
3. **Kitbashed from the model's own geometry:**
   - the tier fronts are a carved plinth band;
   - a **central flight recessed into tier 1** that **branches into two flights** to tier 2, built from copies of the original stair flight.
4. **New simple meshes:** **four jade-capped corner pillars** with gold rings, and the tier bodies. They use texels from the model's atlas, so everything stays one material.

## Result

| | Width | Depth | Height | Storeys / roof tiers |
|---|---|---|---|---|
| L5 (approved art) | 4.57 | 4.93 | 4.16 | 3 / 3 |
| L6 before | 6.55 | 4.55 | 3.75 | hall 3/3, wings 2/2 |
| **L6 after (prototype)** | **7.45** | **5.81** | **4.69** | hall 3/3, wings 2/2 (unchanged) |
| Target | 7.0–7.6 | 5.0–5.8 | 4.6–5.4 | |

All targets are met; depth is 0.01 over the 5.8 limit. It now stands taller than L5 and wider than 6.9, and it fits the proposed Stage II 9 × 7 lot. The central entrance, staircase and round jade ornament remain readable, with no towers, extra floors or new bridges added.

**Renders:**
- `L6_rework_before_after_sheet.png` (Blender front, 45°, top, side and close-up: L5 | before | after);
- `unity/L6_rework_unity_views.png` and `unity/L6_rework_unity_gameplay.png` (isolated Unity, gameplay zoom 10, 21 and 30).

## Limitations

1. **The new terrace surfaces aren't convincing yet.**
   - **What's wrong:** the tier tops, sides and pillars are flat atlas colour without carved detail. Next to the richly textured generated model they read as a **pale stone slab**, most visibly in the Unity front, 45° and top views.
   - **What works:** the kitbashed fronts and stairs match well.
   - **Fixing it:** a proper stone-tile / trim texture pass, hand-authored or atlas-sampled, plus bevels and balustrade rails along the sides and back. That is a manual art task of perhaps half a day, not a parameter tweak.
2. **Galleries:** each gallery is 0.30 longer, so it reads slightly more like a bridge. Moving the wings less (for example 0.15) would reduce this, at the cost of width (about 6.9).
3. **Height source:** the gain comes from the base, not from taller architecture. The hall and wing proportions are the original's. This is honest to the brief (no stretching), but the buildings themselves stay as they were.
4. **The method is model-specific:** it worked because this model has clean gaps between hall and wings. It isn't a general fix for other generated models.

## Recommendation (your decision)

- **A. Continue the rework:** a texture/detail pass on the new terrace (tiles, trims, side balustrades, pillar carving), then a final review. No credits.
- **B. Use the findings for a revised ForgeGUI prompt instead:** ask for a raised two-tier terrace and wings set further out from the start. This costs credits and needs your approval.
- **C. Keep L6 as a candidate only** for now and defer.
