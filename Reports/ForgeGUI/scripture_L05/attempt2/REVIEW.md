# Tàng Kinh Các Level 5, attempt 2: review (awaiting your decision)

## Provenance

| Item | Value |
|---|---|
| Route | forgegui.com Text to 3D Assets via Claude in Chrome: 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1/1, **References empty** (all verified in the page) |
| Prompt | Revision 3 = revision 2 plus your "broad rectangular architectural composition …" sentence. Your sentence's final period was merged into the existing "…, entrance facing the front" clause. 2,762 characters; page-computed SHA-256 matched `97c51463…` |
| Job | conversation `fbb9a937-13c6-4a5e-8186-335e060bd637`, artifact `c1ed8ce1-c693-4cd9-89a3-ee480fe048d3` |
| **Credits** | 328,120 → 323,108 = **5,012** |
| Original GLB | `work/forgegui/downloads/scripture_L05_attempt2_forge_fbb9a937.glb`: read-only, 13,852,788 bytes, sha256 `7af91efd…` |
| Model | 9,252 triangles, 1 material, 3 × 2048 textures, no rig or animation; integrity checks pass |

## Actual dimensions (door 0.52; ground-floor door 0.200 source units, scale ×2.60)

| | Width | Depth | Height |
|---|---|---|---|
| L3 | 4.65 | 3.63 | 3.63 |
| L4 | 5.01 | 4.41 | 3.05 |
| L5 attempt 1 (rejected) | 3.18 | 3.45 | 4.73 |
| **L5 attempt 2** | **4.57** | **4.93** | **4.16** |
| Target | 4.8–5.5 (M) | 3.8–4.6 | 4.4–5.0 (M) |

- **Door uncertainty:** the door measurement is ±5%. At the favourable end, width reaches 4.81 and height 4.38.
- **Depth:** the 4.93 includes the projecting front staircase. The model still sits inside the Stage I 7 × 5 lot.

## Comparisons

- **Blender:** `L3_L4_L5a2_door052_{front,side,45,top}.png` and `_sheet.png`, at the same door scale, lighting and cameras.
- **Unity:** `../unity/L3_L4_L5a2_unity_{views,gameplay}.png`, in an isolated unsaved scene at gameplay zoom 10, 21 and 30.

The individual `../unity/L5_*.png` frames now show attempt 2. Attempt 1's composed sheets `L3_L4_L5_unity_*.png` are unchanged.

## Checklist (M = mandatory)

| # | Requirement | Result |
|---|---|---|
| 1 M | Exactly three occupied storeys | ✅ ground, second and third floors, each with walls and windows |
| 2 M | Exactly three main roof tiers | ✅ three tiers, readable at zoom 21; no fourth roof, attic, top pavilion or spire |
| 3 M | Width 4.8–5.5 | ⚠ **4.57**, about 5% below; reaches 4.81 within the measurement uncertainty. 0.91× L4 |
| 4 M | Height 4.4–5.0 | ⚠ **4.16**, about 5% below (4.38 within uncertainty). Still 1.15× L3 and 1.36× L4: the tallest approved-direction level |
| 5 | Two side annexes | ❌ not generated |
| 6 | Floors clearly separated | ✅ |
| 7 | Large jade jewel on storey 2 | ◐ a gold-framed jade banner centred on storey 2, smaller than a "large jewel" (flanked by smaller jade banners) |
| 8 | Ivory terrace, grand staircase, closed doors | ✅ door measurable; jade crystal finials on the terrace posts |
| 9 | Palette and ornament like L1–L4 | ✅ low gold ridge ornament with a jade gem; restrained gold trim |
| 10 | No static effects, clean geometry | ✅ symmetric roofs, finished rear, no smears or floating parts |

## Assessment

**What revision 3 fixed:**
- the tier count: exactly three storeys and three roofs, after attempt 1's four;
- the "slim tower" problem: it's a broad hall again, 4.57 wide against attempt 1's 3.18.

At gameplay zoom it reads as a clear three-storey upgrade of L3 and L4.

**Open points:**
- width and height are both about 5% under the mandatory ranges (borderline within measurement error);
- no side annexes;
- the jewel is modest.

No retries were made. Nothing was integrated, committed, or changed in server data, footprints or saves.
