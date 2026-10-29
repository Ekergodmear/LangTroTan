# Tàng Kinh Các Level 4 "Huyền Văn Kinh Các": review (awaiting your decision)

## Provenance

| Item | Value |
|---|---|
| Route | forgegui.com Text to 3D Assets via Claude in Chrome: 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1/1, **References empty** (all verified in the page before Create Now) |
| Prompt | Revision 1, 2,280 characters, page-computed SHA-256 matched `7a41c5ea…` |
| Job | conversation `de46799c-af7d-46a4-976a-f34b9dd37497`, artifact `796b94ca-fb6d-49e9-985f-3115739b6fb6` |
| **Credits** | 338,368 → 333,621 = **4,747** |
| Original GLB | `work/forgegui/downloads/scripture_L04_forge_de46799c.glb`: read-only, 14,979,888 bytes, sha256 `7958502f…` |
| Model | 9,142 triangles, 1 material, 3 × 2048 textures (base colour, metallic-roughness, normal), no rig or animation |

## Validation

- **Integrity: pass.** Textures decode, the material is textured, 0 degenerate faces.
- **Stage proportions: pass.** Frontage/depth is 1.14 (allowed 1.11–1.59) and height/frontage is 0.61 (allowed 0.61–0.87).
- **Door:** the central closed double door measures 0.1966 source units, so the scale is ×2.645.
- **Size at door 0.52:** **5.01 w × 4.41 d × 3.05 h**. Unity measured the same.

## Level 3 vs Level 4 (same door scale, lighting and cameras)

Blender: `L3_vs_L4_door052_{front,side,45,top}.png` and `_sheet.png`. Unity: `unity/L3_vs_L4_unity_{views,gameplay}.png`, gameplay zoom 10, 21 and 30.

| | L3 | L4 | Ratio | Preferred |
|---|---|---|---|---|
| Width | 4.65 | 5.01 | 1.08× | 5.2–5.5 (≥ 1.12×) |
| Depth | 3.63 | 4.41 | 1.21× | 3.8–4.2 |
| Height | 3.63 | **3.05** | **0.83×** | 3.7–4.1 |
| Plan area | 16.9 | 22.1 | 1.31× | — |
| Front outline | — | — | area 0.94×, overlap 0.62 | area ≥ 1.25× |

## Checklist

| # | Requirement | Result |
|---|---|---|
| 1 | Exactly two storeys, L3 identity | ✅ two stacked central roofs, upper balcony with wooden balustrade, no third floor; room left for L5's third storey |
| 2 | Perimeter corridors are real architecture | ✅ a closed ring of covered corridors with columns, floors and their own roofs, around an open courtyard (45° and top views), not roof extensions |
| 3 | Clearly wider than L3 | ◐ 1.08× wider (5.01). The plan is 1.31× larger because of the corridor ring |
| 4 | Size in range | ⚠ depth 4.41 (slightly over); **height 3.05, lower than L3's 3.63**; inside the Stage I 7 × 5 lot |
| 5 | High terrace, symmetrical staircase, carved stone balustrades | ✅ |
| 6 | Three-bay entrance with closed doors | ◐ three-bay front with **closed** doors in the centre bay (Level 3's miss fixed); the side bays are lattice windows, not doors |
| 7 | Lattice windows on both floors | ✅ |
| 8 | Ornament | ◐ gold ridge and eave trim, jade medallions. Cloud and scripture carvings aren't legible at this texture detail. A jade medallion replaces the wooden signboard |
| 9 | Two stone lantern towers | ❌ none. The front corners have hexagonal corner pavilions, and gold-capped stone posts flank the stair |
| 10 | Richer than L3 | ◐ clearly more complex plan and detail, but front outline area is 0.94× (lower roofline) |
| 11 | Same palette | ✅ roof tiles slightly more turquoise than L3 |
| 12 | Clean geometry | ◐ validator integrity passes, roofs symmetric, door measurable; **white/red texture smears under the rear and side corridors** (visible close up, not at gameplay zoom) |

## Isolated Unity QA

Run in a temporary, unsaved scene (`StageSiteCapture.RunL4`, fresh-world layout, 0.85 disciples):
- Both models sit **inside the Stage I 7 × 5 lot**.
- At the default gameplay zoom, Level 4 reads as a broader, more complex compound than Level 3, but lower.
- No production scene, server data, footprint or save was touched. Nothing was assigned to the kit.

## Points for your decision

1. **Height regression.** At the 0.52 door, Level 4 is 0.58 lower than Level 3. The Art Bible says a level is never shorter than the one below; you can accept it as an exception. The model looks architecturally balanced (wide corridor compound, lower hall), so it is flagged rather than rejected.
2. **Missing lantern towers and wooden signboard.** The signboard can be the planned Unity prop, as for Levels 1 and 2.
3. **Texture smears** under the rear corridors.
4. **Width** gain is modest (1.08×), though the plan is much larger.

No retries were made.
