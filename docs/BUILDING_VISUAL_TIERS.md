# Building Visual Tiers: proposal and prototype (Scripture Pavilion)

Status (2026-10-28): **prototype for one building type (`scripture`, Tàng Kinh Các) only, gated behind the `-visual-tiers` flag.** The production and preview scene is unchanged until you approve. No models were generated or purchased. The only new asset is the user-supplied ForgeGUI GLB, converted.

## 1. Verified server rules (no assumption of 10 levels)

| Building group | Where the level lives | Range | How it changes |
|---|---|---|---|
| **Core halls in Sơn Môn**: `core:chanh_dien` (hall), `core:tang_kinh_cac` (**scripture**), `core:dien_vo_truong` (barracks), `core:luyen_dan_duong` (alchemy), `core:luyen_khi_dien` (forge), `core:linh_dien_linh_mo` (spirit mine) | `state.sect.buildings[coreId].level`; copied into `state.sect.spatial.buildings[].level` on every advance (`server/unity/spatial.js:48`) | Starts at 1. **No level cap in code**: `start_upgrade` → `js/systems/sect.js startUpgrade` only checks money. Cost `round(baseCost × 1.5^level)` linh thạch, time `round(baseTimeMin × 1.3^level)` minutes. Tàng Kinh Các's own effect text mentions "cấp 8+" | Đại Điện action `start_upgrade`, completed by the server clock. `spatial_upgrade` refuses core halls |
| **Player-built spatial buildings** (21 catalog types, including a buildable `scripture`) | `state.sect.spatial.buildings[]` `{id, type, x, z, rotation, level, hp, complete, workers, finishAt, upgradeAt}` | 1 to **3** (`spatial_upgrade`: "Khu phục dịch hiện nâng tối đa cấp 3") | `spatial_upgrade`, timed; cancel refunds 80% |

**Unity sync:** `FullPortSpatial.DrawSpatialModels` keys every building by its server `id` (`models[id]`, `levels[id]`). Previously a level change destroyed and recreated that building's root GameObject. It now swaps only the visual (§3).

## 2. Proposed mapping: Scripture Pavilion

| Tier | Server levels (core) | Player-built (max 3) | Visual | Asset status |
|---|---|---|---|---|
| I · Thư Các sơ lập | 1–2 | 1–2 | Kit three-storey pavilion (`Building_scripture`) | Existing kit model (placeholder quality) |
| II · Tàng Kinh Các trung phẩm | 3–5 | 3 | **ForgeGUI three-storey pavilion** | User-supplied ForgeGUI GLB, converted |
| III · Linh Kinh Các | 6–8 | — | ForgeGUI pavilion + **additive** dressing: 4 stone lanterns, a front formation disc and a jade mote aura | Existing assets only |
| IV · Thiên Kinh Các | 9+ | — | No bespoke model yet, so the resolver **falls back** to tier III's model and adds a wide outer formation ring and a denser gold-jade aura | Needs a future bespoke asset |

The ForgeGUI pavilion is deliberately **not** level 1: it is the most ornate asset available, so it marks the first meaningful upgrade.

**Evaluation of the ForgeGUI pavilion:** 8,703 triangles, 1 material, 3 × 2048 textures (base colour, metallic-roughness repacked for URP, normal). It reads well at gameplay distance: gold trims, jade medallions, banners. Close up, the AI-generated geometry is soft and blobby, with detail mostly in the texture. That suits a mid tier, not a hero close-up. **Its licence is unverified** (ForgeGUI terms for commercial use); see `Assets/ThirdParty/ForgeGUI/ScripturePavilion/PROVENANCE.json`.

## 3. Implementation (prototype)

- **Data:** `SonMonKit.visualTiers` holds a `BuildingVisualTierSet[]` per server type. Each `BuildingVisualTier` has `{label, minLevel, prefab, decorations[]}`. Built by `SonMonKitBuilder.BuildVisualTiers` (`Assets/Editor`).
- **Resolve** (`SonMonTiers.Resolve`): picks the highest tier with `minLevel ≤ level`, then walks down to the nearest tier that has a prefab. A missing tier asset falls back, never shows nothing. If the type has no tier set, the single kit prefab is used, then the legacy procedural geometry.
- **Build** (`SonMonArt.TryBuilding(root, type, level)`): the visual lives in a child `SonMonVisual` with a `SonMonVisualTag` (type, level, tier index and label, source = tier / tier-fallback / kit). Decorations are instantiated inside it.
- **Upgrade without reload** (`SonMonArt.TrySwapLevel`): when the server snapshot reports a new level, only `SonMonVisual` is replaced. The root GameObject, the `FullPortBuilding` marker (server id), position, rotation, selection and interaction stay the same, and the BoxCollider is refitted in local space. If the tier is unchanged, nothing is rebuilt.
- **Saved higher levels:** the first draw already uses the server level, so a save at level 6 loads straight into tier III.
- **Authority:** the client never writes levels. Everything is derived from the server snapshot.
- **Gate:** `SonMonTiers.Enabled` is true only with `-visual-tiers`. Remove the gate after approval.

## 4. Regression tests (in `-visual-regression` when `-visual-tiers` is set)

1. The scripture renders the tier expected for the **authoritative server level**.
2. For simulated levels 4, 7 and 10, the snapshot is fed through the normal draw path with server polling paused. Checks: same root GameObject (no reload), exactly one `FullPortBuilding` with the id and an unchanged building count (no duplicates), the expected tier, a fitted collider. Level 10 must report `tier-fallback`.
3. Restoring the snapshot returns to the server tier.
4. A fresh server fetch shows the **authoritative level unchanged** after the test.
5. **Saved higher level:** a second run seeds the isolated QA game (27029 / `tu_tien_visual_slice_qa` only, `work/claude-audit/tools/seedTierFixture.mjs`) with Tàng Kinh Các at level 6 and restarts the QA server. Check 1 must then report tier III loaded from the save.

## 5. Next steps after approval

1. Remove the `-visual-tiers` gate for `scripture`.
2. Apply the same table pattern to the other core halls (hall, barracks, alchemy, forge, spirit mine) and to the capped-at-3 player buildings, once tier assets exist for them.
3. Optional upgrade transition VFX (a brief formation flash on swap). This is presentation only.
4. Confirm the ForgeGUI licence; replace tier I and tier IV with bespoke assets when available.

## 6. Results (2026-10-28)

Evidence: `Reports/BuildingTiers/` (real Unity camera renders, 1920×1080). QA scripts are copied to `Tools/QA/`.

| Run | Fixture | Result |
|---|---|---|
| A | Fresh isolated `visual-slice` (Tàng Kinh Các level 1) + `-visual-tiers` | **49/49 PASS**: server level 1 → tier I; simulated 4 / 7 / 10 → tiers II / III / IV-fallback with the same GameObject, no duplicates and a fitted collider; restore OK; server level unchanged |
| B | Same, but seeded **level 6 in the isolated QA DB**, server restarted | **49/49 PASS**: loaded from the save straight into **tier III**; same swap checks |

Screenshots: `tier-scripture-server-L1` (I, kit), `tier-scripture-sim-L4` (II, ForgeGUI), `tier-scripture-saved-L6` (III from save: lanterns, formation, aura), `tier-scripture-sim-L7` (III), `tier-scripture-sim-L10` (IV fallback: outer ring, dense aura).

Open points for approval:
- **Scale hierarchy:** the ForgeGUI pavilion fitted to the 3×3 footprint stands 3.7 units tall versus 5.4 for the tier-I kit pavilion, so the upgrade reads *shorter*. Options: scale by height (it would overhang the footprint by about 45%), or raise it on a stepped terrace decoration.
- **Capture-only artifact:** additive formation discs draw over the bottom nav in screenshots, because captures render the canvas in camera space and the discs sit in a later transparent queue. In play the canvas is an overlay and stays on top. Fix by moving formation and mote materials before the UI queue.
- ForgeGUI licence unverified; tier I and tier IV need bespoke assets eventually.
