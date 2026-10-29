# Tàng Kinh Các Level 2, attempt 2: review (awaiting your decision)

## Provenance

| Item | Value |
|---|---|
| Route | forgegui.com Text to 3D Assets via Claude in Chrome; 3D Model, Mesh Model Auto, Anime, rig off, 1 generation, **no reference image** (References verified empty) |
| Prompt | Revision 3 (1,818 characters), page-verified SHA-256 `458e92d6…` before Create Now |
| Job | ForgeGUI conversation `55cf1cd6-fa92-4360-beb6-3ef234d7de85`, artifact `1fb43fe6-78ca-4930-8336-9cb5a2110f6f` |
| **Credits** | 347,738 → 342,835 = **4,903** |
| Original GLB | `work/forgegui/downloads/scripture_L02_attempt2_forge_55cf1cd6.glb`, read-only, sha256 `22f5fa7f…` (fetched with ForgeMCP `personal_asset_details`, free) |

## Validation

- **Passes:** 1 mesh, 9,610 triangles, 1 material, 3 × 2048 textures, no rig, no animation.
- **Fails two shape checks:** the plan is 1.79 : 1 (not near-square) and the building is much wider than tall.
- Source bounds: 1.90 × 1.06 × 0.88.
- Five views: `scripture_L02_attempt2_{front,rear,side,top,45}.png` and `_sheet.png`.

## 12-point checklist (against L1, same door-0.52 scale and camera: `L1_vs_L2_attempt2_comparison.png`)

| # | Requirement | Result |
|---|---|---|
| 1 | One storey | ✅ |
| 2 | 2.6–2.8 × 1.9–2.1 × 1.8–2.0 at door 0.52 | ❌ **5.01 × 2.80 × 2.32**: about 1.4× the lot's 3.6 eave limit |
| 3 | Six front pillars | ✅ |
| 4 | Two side roof extensions, three-part roofline | ✅ clear stepped roofline |
| 5 | Five front steps | ✅ (about 5 risers) |
| 6 | Four lattice windows | ✅ 4 lattice bays on the main-hall front |
| 7 | Two side storage rooms | ✅ enclosed wings, symmetric |
| 8 | Carved wooden railings | ❌ none |
| 9 | Blank wooden signboard | ❌ none above the entrance |
| 10 | Same materials as L1 | ✅ |
| 11 | Not a near-duplicate | ✅ front-silhouette IoU **0.29** (attempt 1: 0.72); area about 2.8× L1 |
| 12 | Clean geometry | ✅ mostly: symmetric roof corners and a finished rear; one small white texture fleck in a main-hall bay |

## The blocking problem: scale

The main door measures 0.197 source units (±5%). Normalising it to 0.52 needs a factor of 2.64, which makes the building **5.0 wide and 2.8 deep**. That exceeds the 3 × 3 lot and is also wider than the L5 candidate (2.97).

The doors here are set deep in the porch. Even a generous reading (door = 75% of the pillar height) gives 3.9 wide, still over the limit. To fit 3.6, the door would shrink to about 0.37, which the approved scale rule forbids.

The architecture is a large ceremonial hall at human scale: the generator honoured "wide rectangular hall, six pillars, two side rooms" but at a scale that is too big for a 3 × 3 lot.

**Unity QA was not run.** Your instruction was to run it only if basic validation passes, and the asset fails both the footprint and the validator's shape checks.

## Options (your decision)

1. **Reject.** Keep everything for provenance. A future attempt would ask for a **compact** composition: "small hall about 5 bays wide in total including wings, wings only one bay each, porch shallow".
2. **Keep as reference art** for a later, larger level (its massing resembles the spec's L4–L7 compositions). It would not be used for L2.
3. **Accept with a smaller door for this level only.** This breaks the approved world-scale policy, so it's not recommended.

Nothing was integrated. No Unity, gameplay, server, save or production change.
