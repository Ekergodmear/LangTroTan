# Visual world-scale system: implementation report

Status (2026-10-28): **implemented behind a feature flag that is off by default.** It's validated in the isolated QA build only. Not deployed to production; no ForgeGUI generation, no credits; production saves, server data, footprints, movement, NavMesh, physics, colliders used by gameplay, animation timing and camera controls are unchanged.

Approved policy: architectural main door **0.52**, disciple and worker display height **0.85**.

## 1. Code changes

| File | Change |
|---|---|
| `Assets/Scripts/Presentation/SonMon/SonMonWorldScale.cs` (new) | Central configuration: a ScriptableObject `Resources/SonMon/SonMonWorldScale.asset` with `enabled` (default **false**), `doorHeight` 0.52 and `characterHeight` 0.85. Launch flags `-world-scale` / `-no-world-scale` override it, so rollback is a flag flip. Helpers: `BuildingFactor`, `ApplyCharacter` (fits a visual child to the display height from its rendered bounds), `VisualChild` |
| `Assets/Scripts/Presentation/SonMon/SonMonCharacterVisual.cs` (new) | On spawn, applies the display height to the character's single visual child. The root is never scaled |
| `Assets/Editor/SonMonKitBuilder.cs` | Adds `SonMonCharacterVisual` to `Character_disciple` (visual = its model child); creates the settings asset; reads `MEASUREMENTS.json` per ForgeGUI model and stores each level's **door height in prefab units**; logs the door-normalised sizes; L1 status `approved_with_notes` |
| `Assets/Scripts/Presentation/SonMon/SonMonVisualTiers.cs` | `BuildingVisualLevel.doorHeight` |
| `Assets/Scripts/Presentation/SonMon/SonMonKit.cs` | `TryBuilding` scales the level **model** inside the swappable visual by `doorHeight → 0.52` when the flag is on. The root, its footprint position and the server state are untouched; the collider, dev label and upgrade FX all fit the result. Approved art no longer gets a DEV label |
| `Assets/Scripts/Presentation/FullPortSpatial.cs` | Worker actors: the generated geometry (and the `Combine` pass) now lives under a dedicated **`Visual`** child; then the display height is applied. The root keeps `ActorMotion` and is moved by gameplay exactly as before |
| `Assets/ThirdParty/ForgeGUI/ScripturePavilion{,_L01}/MEASUREMENTS.json` (new) | Measured door and source height plus provenance (the original GLB/FBX files are unchanged) |
| `Assets/Scripts/Presentation/VisualSliceQA.cs` | New checks (below) and zoom 10/21/30 captures |

Door normalisation from the kit build log: **L1 → 2.18 × 1.55 × 1.72, L5 → 2.97 × 3.02 × 3.63**, exactly the approved targets.

## 2. Tests

| Run | Result |
|---|---|
| Server max-level tests | 17/17 (`BUILDING_MAX_LEVEL_OK`) |
| Full visual regression, **flag off** (default) | **114 PASS**, 1 REVIEW (the known un-normalised L1-wider-than-L5 item). The 110 earlier checks plus 4 new structure checks: worker geometry under `Visual` with root unscaled; legs inside `Visual` and found by `ActorMotion`; both disciples' display scale targets their visual child with root scale 1 |
| Full visual regression, **flag on** (`-world-scale`) | **126 PASS**, no REVIEW. The level ladder is now monotonic (L1 1.72 h → L5 3.63 h) |

Flag-on checks, with the values Unity reported:

- **Idle disciple:** renders at **0.85**; feet grounded (−0.02 vs root 0.00).
- **Walking disciple:** renders at **0.85**; still moves along its route (movement unchanged); grounded through the walk cycle (−0.02).
- **Worker actor during a real harvest job:** renders at **0.85**; legs swing 25° (animation intact); feet grounded.
- **Tàng Kinh Các at server level 1:** normalised to 2.18 × 1.55 × 1.72; the selection collider refits to it.
- All earlier checks pass with the flag on as well: upgrade transitions, cancellation, MAX UI, identity mapping, raycast selection of buildings, placement, harvest.

Screenshots (real Unity camera renders) are in `Reports/WorldScale/`: `on/worldscale-{scripture,disciples,worker}-zoom{10,21,30}`, `worldscale_zoom_sheet.png`, `worker-interaction` (flag on and off), and both regression files.

## 3. Regressions and findings

1. **Mixed architectural scale on a partial rollout (visual, flag on only).** Only Tàng Kinh Các is normalised. The other five families still use the kit buildings, authored with doors of about 0.72, and the environment props (stone lanterns, gate, paving) are at kit scale too. So the 0.52-door Tàng Kinh Các L1 looks like a dollhouse beside Chánh Điện and next to its own stone lantern (`worldscale-scripture-zoom10`). At 0.85, the characters look right next to the kit buildings (≈ 1.2 × a 0.72 door) but oversized against the normalised pavilion's door (1.6 ×, as approved).
   **Not a code fault.** It's a consequence of rolling the scale out to one family only. Keep the flag off until the families are consistent.
2. Selection markers and labels: building selection colliders refit; the DEV plaque and upgrade FX follow the resized model. Character actors have no selection markers or name labels in the current build, so nothing else needed repositioning.
3. No gameplay changes were observed: movement, harvesting, placement, assignments, pill use and upgrade decisions all pass with the flag on.

## 4. Options for the mixed-scale finding (decision needed before any rollout)

- **(a) Keep the flag off** until every family has door-measured ForgeGUI models (as planned in the production order). Safest; nothing visible changes in the meantime.
- **(b) Add a configured kit-scale factor** (0.52 / 0.72 ≈ 0.72) for the remaining kit buildings and environment props, so the whole sect shares the 0.52 door now. This is presentation only, but it shrinks every existing building and prop and needs its own visual QA pass.
- **(c) Raise the standard door** toward the kit's 0.72. This is not compatible with the approved L5 on a 3 × 3 lot (shown earlier).

Recommendation: **(a)**, keeping the infrastructure in place, with (b) prototyped in isolated QA if you want the sect to look consistent before the other families are generated.
