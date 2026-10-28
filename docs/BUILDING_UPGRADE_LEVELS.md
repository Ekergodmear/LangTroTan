# Building upgrades: max level 10, ten visual levels, upgrade transition

Status (2026-10-28): **implemented and validated for one building, Tàng Kinh Các (server type `scripture`). Stopped for approval before any other building.** Level visuals and the transition are gated behind `-visual-tiers`, so the production and preview scene is unchanged. Supersedes the four-tier proposal in `BUILDING_VISUAL_TIERS.md`.

**Art status, plainly:** only two of the ten levels have a model, and neither is approved art. L1 is the Blender kit pavilion (placeholder). L5 is the ForgeGUI export (candidate pending your approval; licence unverified). Levels 2–4 and 6–10 have **no model**. Per your instruction, none were invented. At runtime those levels show the nearest lower model with an orange `DEV` plaque, which is a development fallback and not production-complete.

## Phase A: upgrade systems audit and the server cap

### Systems that hold a building level

| System | Where the level lives | Cap before | Cap now | Cost and time (unchanged) | Level consumers |
|---|---|---|---|---|---|
| **Core Sơn Môn halls** (6): Chánh Điện, **Tàng Kinh Các**, Diễn Võ Trường, Luyện Đan Đường, Luyện Khí Điện, Linh Điền & Linh Mỏ | `state.sect.buildings[id].level`, mirrored into `sect.spatial.buildings[core:*]` (`server/unity/spatial.js:49`) | none | **10** | `round(baseCost × 1.5^level)` linh thạch; `round(baseTimeMin × 1.3^level)` minutes (`js/data/sectBuildings.js`) | Linh Điền: labour output `base × 1.2^(level−1)` (`sect.js:184`, `ngoaiMon.js:61`). Diễn Võ Trường: passive EXP (`sect.js:201`). Tàng Kinh Các: effect text "max 4 slots at level 8+" (no code consumer found). The others: text only |
| **Player-built spatial buildings** (21 catalog types, including a buildable `scripture`) | `sect.spatial.buildings[].level` | 3 | **10** | wood `max(20, wood) × level`, stone `max(10, stone) × level`, `5 × level` min (`spatial_upgrade`) | Housing `+4/level` (total capped at 40). Store `+300/level`. Work slots `+1/level`. HP `+hp/2` per level |
| Legacy offline Unity domain (`SectWorld`, cap 3) | local only, not server-authoritative | 3 | unchanged | — | Not used by the server-connected game. **Out of scope.** |

Balance impact to review: raising player buildings from 3 to 10 with unchanged formulas gives up to 9 extra work slots and +2,700 storage per building. Housing stays bounded by the existing cap of 40. `server/scripts/economyPhase16Experiment.js` uses its own `buildingCap=8` for simulations, which is left as is.

### Enforcement (authoritative server)

- `MAX_BUILDING_LEVEL = 10` is exported from `js/data/sectBuildings.js` and used everywhere below.
- `startUpgrade` (core halls) rejects at level ≥ 10 **before any cost is taken** and returns `{ok:false, maxLevel:true}`. `canAffordUpgrade` returns false at the cap.
- `spatial_upgrade` (player buildings) rejects with "Công trình đã đạt cấp tối đa 10." before any deduction.
- The spatial-state response carries `maxBuildingLevel`, so the client mirrors the server value instead of hard-coding it.
- The server-rendered Đại Điện panels show "Đã đạt cấp tối đa (Cấp 10)" in place of the upgrade button, and "Cấp X / 10".

### Migration for saves above 10 (no silent clamping)

- A saved level above 10 is **never changed**. It is displayed as is ("Bản lưu cũ vượt giới hạn mới"), cannot upgrade, and uses the level-10 visual.
- An upgrade already in flight when the building is at ≥ 10 (started before the cap existed) is **voided at completion with a full refund** of its cost. The level is not changed. It is logged as "đã ở cấp tối đa … hoàn … linh thạch" and emits a `building_upgrade_voided` event. Player buildings get the same treatment with a wood and stone refund.
- Protected saves (`cxi_demo_ui`, `game_mv3bzlj8`) were not loaded, ticked or touched. All tests ran in-memory or against the isolated QA database.

### Server regression tests

`Tools/QA/testBuildingMaxLevel.mjs` loads the backend's real rule modules and touches no database. Result: **17/17 PASS**.

- Core level 1 and 9: upgrade accepted, charged the unchanged formula cost, completes to 2 and 10.
- Core level 10 → 11: rejected as max level, nothing deducted, state unchanged.
- Legacy core level 12: not clamped, cannot upgrade, nothing deducted.
- Legacy in-flight upgrade at level 10: voided, level stays 10, full cost refunded and reported.
- Spatial level 1 and 9 accepted with the unchanged wood cost. Spatial 10 → 11 rejected with nothing deducted.

Full backend suite, run per test: 49 pass. 10 fail, all 10 also failing on the untouched baseline backup (missing docs and assets for phase playtests). **No new failures.**

Backend backup before the change: `work/claude-audit/backend-before-maxlevel-2026-10-28.zip`. Patch scripts: `work/claude-audit/tools/patch_server_maxlevel.py`, `patch_server_maxlevel_log.py`.

## Phase B: ten visual levels (data-driven)

- **Data** (`SonMonKit.visualLevels`): one `BuildingVisualLevelSet` per server type, `maxLevel = 10`, with one `BuildingVisualLevel {level, prefab, decorations, status, targetHeight}` per level. Status is `approved`, `candidate`, `placeholder` or `missing`.
- **Generated by** `SonMonKitBuilder.BuildVisualLevels`. Each supplied model becomes its own prefab (`Level_scripture_L01_kit`, `Level_scripture_L05_forgegui`), scaled uniformly to a **height ladder**: `3.6 + 0.15 × (level − 1)`, from 3.60 at L1 to 4.95 at L10.
- **Build-time guard** (`ValidateLadder`): the build fails if a level's model is shorter or narrower than the level below, or spills more than 20% past its server footprint. Result: L1 is 2.02 × 3.60 × 2.71 and L5 is 3.44 × 4.20 × 3.49 (3 × 3 footprint).
- **Resolve** (`SonMonTiers.Resolve`): the exact level's model, otherwise the **nearest lower** model. A fallback is therefore never larger than the real model it stands in for. Saves above 10 resolve to level 10.
- **Fallback is visible:** while `-visual-tiers` is on, every non-approved visual carries a `DEV` plaque, either "mô hình tạm" / "ứng viên chờ duyệt" or, for a fallback, "Cấp N: chưa có mô hình (tạm hiển thị mô hình cấp M)".
- **Identity preserved:** a level change replaces only the `SonMonVisual` child. The root GameObject, `FullPortBuilding` (server id), position, rotation, footprint, selection and click handling stay the same. The collider is refitted, and the plaque is excluded from it. The level always comes from the server snapshot.

### Plugging in your ForgeGUI exports

1. Convert the GLB with `Tools/Blender/import_glb_asset.py` into `Assets/ThirdParty/ForgeGUI/<Name>/` (FBX plus PNGs plus `PROVENANCE.json`).
2. Add one line in `BuildVisualLevels`, for example `Assign(7, <prefab>, "Level_scripture_L07_<name>", "candidate")`.
3. Run `SonMonKitBuilder.BuildAll`. The ladder guard rejects a model that would read smaller than the level below.
4. Once you approve a level, change its status to `approved`, which removes its plaque.

## Phase C: upgrade transition (`SonMonUpgradeFx`)

Triggered only when a **server snapshot** reports a higher level for a building that is already drawn. A level loaded from a save builds the right model directly, with no animation.

| Time (full, 3.6 s) | Beat |
|---|---|
| 0–1.3 s | The building glows (temporary emissive copies of its own materials). Spirit motes gather inward from a sphere. |
| 0.45–2.2 s | Two rune formation discs grow and spin faster. A light pillar starts to rise. |
| 1.9–2.2 s | Controlled change: the old model settles slightly at peak glow. At 2.2 s it is swapped for the new level's model, which rises back with a small overshoot. |
| 2.2–3.0 s | Burst of 160 light particles and a pillar flash. Glow and formation fade. |
| 3.6 s | Materials restored, effect destroyed. The UI announces "<name> đã đạt Cấp N" (the last step). The panel showed "Đang thăng cấp A → B" during the transition. |

- **Non-blocking:** it runs from `Update`, with no input lock or coroutine, so camera, selection and other UI keep working during it (tested).
- **Repeated updates:** a newer level that arrives before the change only retargets. One that arrives after the change swaps again at once and replays the burst.
- **Cancellation:** a lower level (restore, correction) cancels at once and snaps. A building destroyed mid-way cleans up its material copies in `OnDestroy`.
- **Reduced effects:** a saved player preference (`sonmon.reducedEffects`) or the `-reduced-effects` flag gives a 1.2 s glow-and-change with no particles, formation or pillar. There is no settings screen in the port yet; the toggle UI needs one.
- **Asset:** `Fx_building_upgrade` prefab, built from the existing rune and mote materials. It is shared by all buildings and scaled to the footprint.

## Phase D: validation (Tàng Kinh Các)

All runs used a fresh Windows player build (`Builds/VisualSlice`, D3D11, RX 6600M, 1920×1080) against the **isolated** visual-slice QA server (port 8901, MongoDB 27029, `tu_tien_visual_slice_qa`), with the fixture reset before each run. Screenshots are real Unity camera renders. Evidence is in `Reports/BuildingLevels/`.

| Run | Fixture | Result |
|---|---|---|
| **Full regression** | Fresh fixture (Tàng Kinh Các level 1), `-visual-tiers -visual-regression` (all earlier checks plus the level suite) | **110/110 PASS** (`regression-full.txt`) |
| **Saved at max** | Isolated fixture seeded at **level 10**, server restarted | **79/79 PASS** (`regression-saved-L10.txt`): loads at 10 with no animation, MAX panel, server rejects 10 → 11 and deducts nothing |
| **Live, server-confirmed** | Seeded at **level 9 with a real upgrade in flight** (finishes 75 s after seeding), `-tier-live` | **11/11 PASS** (`regression-live.txt`): loads at 9 with no animation, panel shows the upgrade in progress, the server clock completes it, the poll delivers 10, the 9 → 10 transition plays, the UI announces "Cấp 10 · CẤP TỐI ĐA", the MAX panel shows, and 10 → 11 is rejected with nothing deducted |

What the level suite checks (simulated levels go through the normal draw path with server polling paused, and the real snapshot is restored and re-fetched at the end):

- **Data:** the server reports `MAX_BUILDING_LEVEL` 10, and exactly one visual entry exists per level 1–10.
- **Every level 10 → 1:** same root GameObject, exactly one `FullPortBuilding` (no duplicates), the expected model, fallback flagged with a DEV plaque, a fitted collider, one clean visual, and no animation for a lower level.
- **Size ladder** measured in the running game: L1–L4 3.60 h / 2.71 w, L5–L10 4.20 h / 3.49 w. No level is smaller than the one below.
- **Full transition 4 → 5:** non-blocking (the building was selected mid-transition and the panel showed "Đang thăng cấp 4 → 5"). Ends on the L5 model with shared materials restored. The UI announces the new level last.
- **Performance** (`performance-upgrade-fx.json`, 5 → 6, no captures): median frame 3.21 ms during the transition vs 3.02 ms idle; p95 4.78 vs 4.66 ms; max 6.7 ms.
- **Reduced effects:** no particles or formation, finished in 1.05 s.
- **Repeated updates:** a newer level before the change retargets (6 → 7 becomes 8). One after the change swaps again at once (8 → 9 becomes 10). Both end with one visual.
- **Cancellation:** a lower level mid-transition snaps instantly and leaves no effect object or glow material behind.
- **Authority:** after restoring the snapshot, the level fetched from the server is unchanged.

Contact sheets: `ladder-contact-sheet.png` (all 10 levels), `transition-L4-L5-contact-sheet.png`, `live-transition-contact-sheet.png` (the server-confirmed 9 → 10, where both levels show the L5 fallback model, so only the effect plays).

### Problems found and fixed while validating

- **Upgrade animation on load (real bug):** the spatial copy of a core hall's level is only re-synced when the server advances, so a fresh snapshot could carry a stale level (1 instead of the saved 9), and the corrected value then looked like an upgrade. Core halls now read the authoritative `sect.buildings[coreId].level`. The live run caught this.
- **Pre-existing, from Milestone B:** `SonMonSpin` (formation rotation) and `SonMonWalker` (walking disciple) lived in files with other names. Unity cannot bind serialized MonoBehaviours that way, so in builds the formation discs did not spin and the walking disciple did not walk ("referenced script is missing", 13 warnings). Each now has its own file, plus `SonMonBillboard`. 0 warnings.
- The cost shown in the client now uses JavaScript rounding (halves away from zero), so it matches what the server charges at .5 values.

### Known limitations

- 8 of 10 levels have no model (fallbacks with DEV plaques). The two that exist are a placeholder and an unapproved candidate.
- The ForgeGUI model at its L5 rung is 3.49 wide on a 3 × 3 footprint, overhanging about 0.25 per side.
- When two levels resolve to the same fallback model (for example 9 → 10), the transition still plays, but the model does not visibly change.
- The DEV plaque text is faint against bright sky.
- Capture-only artifact (unchanged): additive formation discs draw over the bottom nav in screenshots. In play the canvas is an overlay and stays on top.
- QA-only: `spatialSimulation` holds off server writes while simulated levels are on screen.

## Next steps (after your approval)

1. Approve or reject the L5 ForgeGUI candidate and the height ladder (3.60 → 4.95).
2. Supply approved ForgeGUI exports for the levels you want (any subset). Each one slots in with a single line.
3. Then remove the `-visual-tiers` gate for `scripture` and expand to the other halls and player buildings.
4. Add a reduced-effects toggle when a settings screen exists.
