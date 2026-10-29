# Tàng Kinh Các Level 3 "Song Tầng Kinh Các": review (awaiting your decision)

## Provenance

| Item | Value |
|---|---|
| Route | forgegui.com Text to 3D Assets via Claude in Chrome: 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation, **References empty** |
| Prompt | Revision 1, 2,095 characters. The page-computed SHA-256 matched the approved text (`abd7db40…`) before Create Now. The box has no length limit, and ForgeGUI's official connector documents 4,000 characters, so nothing was shortened |
| Job | conversation `56f7f2dd-0624-4ef8-88a3-f974b96107c8`, artifact `16b2cc34-5897-4385-9629-851c056ad213` |
| **Credits** | 342,835 → 338,368 = **4,467** |
| Original GLB | `work/forgegui/downloads/scripture_L03_forge_56f7f2dd.glb`: read-only, 14,292,348 bytes, sha256 `1564cee7…` |

## Validation (updated validator, stage-specific)

- **Integrity: pass.** 9,488 triangles, 1 material, 3 × 2048 textures that all decode, textured material, 0 degenerate faces, no rig or animation.
- **Stage I proportions: fail.**
  - Plan frontage/depth is 1.28 (allowed 1.32–2.02).
  - Height/frontage is 0.78 (allowed 0.51–0.77).
- **Size after door normalisation (strict): fail.** At door 0.52 it measures **4.65 w × 3.63 d × 3.63 h**, against targets of 5.0–5.5 × 3.0–3.4 × 3.1–3.5. Unity measured the same.
- **Door:** the ground-floor centre bay is an **open hall with no doors** (bookshelves and a jade screen inside). So the door used for scale is the side rooms' double door, 0.2125 source units.

## Level 2 vs Level 3 at the same scale and cameras

Comparisons: `L2_vs_L3_door052_{front,45,top}.png` (Blender) and `unity/L2_vs_L3_unity_{views,gameplay,site}.png` (Unity).

| | L2 (approved direction) | L3 |
|---|---|---|
| Size at door 0.52 | 5.01 × 2.80 × 2.32 | 4.65 × 3.63 × 3.63 |
| Front outline | — | height 1.56×, width 0.93×, area 1.12×, overlap 0.52 |

## Checklist

| # | Requirement | Result |
|---|---|---|
| 1 | Two storeys in the centre | ✅ |
| 2 | Two distinct stacked central roofs | ✅ |
| 3 | Level 2's side wings kept | ◐ two side rooms with their own roofs, but small one-bay rooms instead of L2's long wings |
| 4 | Upper balcony with carved wooden balustrades | ✅ |
| 5 | Raised platform, about 7-step central staircase | ✅ |
| 6 | Porch carried over: six pillars, double doors, four lattice windows | ❌ four front pillars; open bay with no doors; no lattice windows on the ground-floor front (lattice only upstairs and on the side doors) |
| 7 | Wooden porch railings and blank signboard | ◐ the platform edge has **stone** balustrades (wood only on the balcony); small blank jade-framed board on the central lintel |
| 8 | Stage I size | ❌ 4.65 wide (7% narrower than L2), 3.63 deep, 3.63 tall. The height gain over L2 (1.56×) passes |
| 9 | Not a copy of L2 | ◐ overlap 0.52 passes (≤ 0.75); area 1.12× is short of the required 1.25× |
| 10 | Same materials | ✅ |
| 11 | Clean geometry | ✅ symmetric roofs, finished rear, no broken textures; thin strands on the top ridge ornament (minor) |
| 12 | Door measurable | ◐ no main door, so the side doors were used (scale depends on this choice) |

## Isolated Unity QA: site envelope

`Assets/Editor/StageSiteCapture.cs` built a temporary, unsaved scene. No kit, production scene, server or save was used.

- **Scene contents:** the fresh-world server layout, every core lot, all trees, the proposed stage lots, 0.85 disciples, and today's kit Chánh Điện.
- **Placement:** Level 2 and Level 3 were placed in turn on the Tàng Kinh Các front edge (z = 4), at door 0.52, with identical lights and cameras.
- **Views:** front, 45°, top, site, and gameplay at zoom 10, 21 and 30.

Results:
- **Fit:** both models sit **inside the Stage I 7 × 5 lot**. No building or tree is under them.
- **Mixed scale:** the kit Chánh Điện, which isn't door-normalised, still looks larger than L3. This is the known mixed-scale limitation; the world-scale flag stays off.
- **Gameplay views:** at the default zoom (21), L3 reads clearly as a taller, two-storey upgrade of L2.
- **Not tested:** server pathing and collision for the larger footprint. That needs the approved server-reservation phase.

## Assessment

**Strengths:**
- A clear Level 3 silhouette: two-storey hall, stacked roofs, balcony, raised platform.
- The height progression over L2 is strong, it stays inside the Stage I lot, and the geometry and textures are clean.

**Gaps:**
- It's narrower than L2 and outside the approved size ranges.
- L2's ground-floor porch features didn't carry over: six pillars, doors, lattice windows, wooden railings.
- The wings shrank.

**Not done:** nothing was integrated into the kit or game, and no further generation was made.
