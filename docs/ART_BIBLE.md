# Thanh Vân Tông: Art Bible (sect architecture)

Status: v3, 2026-10-28 (three-stage size policy for Tàng Kinh Các). This bible governs all 60 architectural models (6 building families × 10 levels) and their effects. It extends `XIANXIA_ART_DIRECTION.md`, and where the two conflict this file wins for buildings.

**Approved level-by-level architecture:** `docs/APPROVED_ARCHITECTURE_SPEC.md` (verbatim, approved 2026-10-28). The bible sets style; the spec sets structure. A prompt's style block may add materials and palette only, never structure (`asset_manifest.json → promptTemplate.rule`).

## 1. Identity

One sect, one hand. Every building must read as part of the same premium stylized **anime Chinese fantasy (Xianxia)** compound.

| Element | Rule |
|---|---|
| Roofs | **Jade-teal glazed tiles**, curved, with deep upswept eaves; ridge ornaments in restrained antique gold |
| Timber | **Deep red lacquered** round pillars and beams; visible carved dougong brackets under every eave |
| Foundations | **Ivory-white stone** plinths and terraces with steps; no building touches grass directly |
| Metal | **Restrained antique gold**: trims, ridge finials, door studs. Never whole surfaces |
| Accents | **Turquoise jade**: medallions, inlays, plaques |
| Motifs | Chinese clouds, dragons, immortals, used sparingly and in relief, never as floating props |
| Openings | Detailed lattice windows and doors, balconies with carved railings, believable stairs |
| Geometry | Full 360° game-ready model; back and sides are finished, not flat |

### Palette (target values)

| Role | Hex |
|---|---|
| Jade-teal roof | `#2F6F6A` → `#4E9C92` |
| Red lacquer | `#8E2B23` → `#A8382C` |
| Ivory stone | `#E9E2D0` → `#F4EFE3` |
| Antique gold | `#B8913F` (low saturation, never chrome-yellow) |
| Turquoise jade | `#3FC1B0` |

### Forbidden

Primitive cube architecture; generic low-poly placeholder look; style changes between levels; roof colours other than jade-teal; unrealistic proportions (doors must fit a disciple, steps must be climbable); floating or disconnected geometry; decoration blocking the entrance; **particles, light trails or magic baked into the static mesh** (all magic is VFX, §5).

## 2. Family silhouettes (server-verified)

| Family | Server building id | Unity / spatial type | Server footprint | Silhouette rule |
|---|---|---|---|---|
| Chánh Điện, Main Hall | `chanh_dien` | `hall` | **4 × 4** | Widest, tallest, strictly symmetrical hall on a stepped terrace. Always the tallest roofline in the sect |
| Tàng Kinh Các, Scripture Pavilion | `tang_kinh_cac` | `scripture` | 3 × 3 | Scholarly hall growing into a pagoda palace; grows in three size stages (§2, three-stage policy); the more floors, the more scholarly |
| Diễn Võ Trường, Martial Training Ground | `dien_vo_truong` | `barracks` (catalog name "Doanh Trại Hộ Vệ") | 3 × 3 | Open arena or platform with a gate pavilion and weapon racks; low and wide, never a closed box |
| Luyện Đan Đường, Alchemy Hall | `luyen_dan_duong` | `alchemy` | 3 × 3 | Hall built around a central cauldron courtyard, with chimney or vent roofs |
| Luyện Khí Điện, Artifact Forging Hall | `luyen_khi_dien` | `forge` | 3 × 3 | Heavy hall with a forge hearth, stone chimney and open anvil front |
| Linh Điền & Linh Mỏ, Spirit Fields & Mines | `linh_dien_linh_mo` | `spiritmine` (catalog name "Linh Mỏ") | **2 × 2** | Terraced field plots beside a carved mine entrance; lowest silhouette of the six |

Notes from the server check:

- **Mapping by identity (approved rule):** a family belongs to the authoritative core building (`visualKey = core:<serverBuildingId>`). Today's Unity code still resolves visuals by spatial **type**, so a player-built `barracks` ("Doanh Trại Hộ Vệ") would wrongly show the Diễn Võ Trường arena. The same applies to player-built `scripture`, `alchemy`, `forge` and `spiritmine`. The fix is presentation-only (resolve by `coreId` first) and is proposed, not yet implemented.
- **Spirit Fields & Mines is one server building with one level**, so it is one asset family on 2 × 2. It will not be split, and its footprint will not change, without approval.
- **Pivot and facing:** every model is pivoted at ground level at the footprint centre, with the entrance facing front (Unity −Z). Validator renders use the same five cameras for every level of a family.

### Height and scale guidelines (revised 2026-10-28)

The fixed cross-family ladder (Chánh Điện L1 taller than every other family's L10) was **not approved**. Scale follows these rules instead, in priority order:

1. **Believable human scale first.** Every model is scaled so its main door is the same real height in every family and level: about 0.55 Unity units (a disciple is about 0.45 tall; 1 unit = 1 server cell). Scale comes from the measured door of the generated model, not from a target building height. A building is as tall as its approved architecture makes it at that scale.
2. **Progression within each family.** Each level must read as more prestigious than the one below, through more storeys, wings, terraces, towers and ornament. It must never be visibly smaller in height or width. The kit build keeps the monotonic check (`ValidateLadder`) per family, comparing measured sizes after human-scale fitting. A level that fails is flagged for review, not silently rescaled.
3. **Architectural hierarchy across families** comes from footprint, massing and silhouette, not a forced height. Chánh Điện stays the sect's dominant landmark: 4 × 4 footprint, the broadest and most ceremonial roof mass, and the central axis position. At the **same level** it should be the most imposing building. The review checklist compares it with the other families at the same level. Conflicts go to the user rather than being solved by scaling.
4. **Footprint.** The plinth stays inside the server footprint; eaves may overhang by up to 20%. Uniform scaling only, never squashing. **Exception: Tàng Kinh Các** follows the three-stage policy below, whose models exceed the 3 × 3 lot by design. They need the stage lots and site review before production use.

Until door-height measurement is automated in the validator, the existing per-family rungs (Tàng Kinh Các 3.6 → 4.95) are provisional placeholders and will be replaced by human-scale fitting when the generated models are integrated.

### World scale (approved 2026-10-28)

| Standard | Value |
|---|---|
| Architectural main door | **0.52** Unity units, every family and level (fitted from each model's measured door; `MEASUREMENTS.json` beside the converted export) |
| Disciple and worker display height | **0.85** (visual child only; roots, movement and colliders untouched) |
| Server footprints | unchanged |

Implemented by `SonMonWorldScale` (`Resources/SonMon/SonMonWorldScale.asset`). It is a reversible, presentation-only feature flag, **OFF by default**; `-world-scale` / `-no-world-scale` override it.

**Mixed-scale limitation:** keep the flag off until all six families are in the system. Today only Tàng Kinh Các is door-normalised. The other five families still use kit buildings authored with doors of about 0.72, and the environment props (stone lanterns, gate, paving) share that kit scale. With the flag on, a normalised Tàng Kinh Các reads too small beside Chánh Điện and its own lantern (`Reports/WorldScale/on/worldscale-scripture-zoom10-1920.png`). Kit buildings and props are deliberately **not** rescaled (decision 2026-10-28, option A). The flag goes on only when all six core families have door-measured models. Regression baseline: 126 PASS with the flag on, 114 PASS with it off (production appearance unchanged).

### Three-stage size progression: Tàng Kinh Các (approved 2026-10-28)

Full policy, targets, site strategy: `docs/SCRIPTURE_THREE_STAGE_POLICY.md`.

| Stage | Levels | Width at door 0.52 | Within the stage |
|---|---|---|---|
| — | 1 | 2.18 (approved small pavilion) | — |
| I | 2–5 | about 5.0–5.5 | same footprint; grows by floors, roofs, terraces, balconies, detail (L3 two-storey hall, L4 corridors and podium, L5 three-storey jade pavilion) |
| II | 6–8 | about 7.0–8.0 | L6 three-storey complex, L7 four storeys with side towers, L8 bridges and guardian statues |
| III | 9–10 | about 10–12 | L9 five-storey immortal palace, L10 twin towers, white-jade foundation, spirit crystal, celestial scripture VFX |

Rules:
1. Identity stays constant: jade-teal curved roofs, dark red timber, ivory stone, golden ornaments.
2. Door 0.52 and character display 0.85 stay constant.
3. Inside a stage, progress through height, complexity and silhouette, not horizontal expansion.
4. The jumps at Levels 6 and 9 must be visually dramatic: at least 1.25× width and at least 1.5× / 1.6× front-silhouette area.
5. Never shrink the door or distort proportions to fit a lot.
6. Server footprints, placement, pathing, protected saves and production scenes change only with separate approval. The larger footprints need the Sơn Môn site-layout and collision review.
7. One level per approved generation, no automatic credit spending.

The other five families keep the single-lot rules until their own policies are decided. **Chánh Điện must remain dominant**, so its policy has to be settled before Tàng Kinh Các Stage III is produced.

## 3. Level progression rules

1. Every level 1–10 is its **own model**. Never scale the same mesh up, and never change only the materials.
2. Level N+1 keeps the identity of level N (plan, roof language, palette) and adds its approved structural improvement (floors, wings, terrace tiers, bridges, ornament density).
3. **Prestige rises monotonically:** a level is never shorter or narrower than the one below (enforced by `ValidateLadder` at kit build time). In a staged family, width holds roughly steady within a stage while height and silhouette area rise.
4. **Footprint:** the server footprint is fixed (Tàng Kinh Các: staged lots, pending approval). The model may overhang it by at most 20% at the eaves, and the plinth fits inside it. Height is the main axis of growth.
5. Human scale is constant across levels. Doors, steps and railings stay the same real size; growth comes from more and larger structure, not bigger doors.
6. Entrance and stairs face the front (Unity −Z after import) and stay unobstructed at every level.

## 4. Technical rules for generated models

| Item | Rule |
|---|---|
| Generator | ForgeGUI Text to 3D Assets: 3D Model, **Style Anime**, **Automatically Rig off**, 1 model per request |
| Source | The original GLB is kept unmodified (with sha256) in `Assets/ThirdParty/ForgeGUI/<Family>_L<nn>/source/` once approved; until then it stays in `work/forgegui/downloads/` |
| Budget | ≤ 60k triangles per level (target 10–30k); ≤ 3 × 2048 textures |
| Validation | `Tools/ForgeGUI/validate_glb.py`: geometry, no rig or animation, textures, square footprint, height ratio. Previews: front, rear, side, top, 45° |
| Conversion | `Tools/Blender/import_glb_asset.py` produces FBX plus URP textures; one prefab per level on the height ladder |
| Status | `planned → generating → generated → validation_failed / candidate → approved → integrated`. Only the user moves a level to approved |

## 5. Effects (separate from architecture)

Architecture is static. All motion is VFX: URP particles, shaders and spline paths, attached by profile per family and level, with quality and LOD tiers. Ambient effects grow subtly with level. Spectacle is reserved for the upgrade transition (`SonMonUpgradeFx`).

| Family | Ambient theme (subtle, grows L1 → L10) | Upgrade burst accent |
|---|---|---|
| Chánh Điện | Incense, banners, golden motes; **Azure Dragon guardian at level 10 only** | Dragon-roar shockwave, gold-azure pillar |
| Tàng Kinh Các | Floating scriptures, ancient runes, **rotating celestial book** (Thiên Thư) | Book opens, rune storm |
| Diễn Võ Trường | Sword energy, flying sword formations | Sword-rain ring |
| Luyện Đan Đường | Cauldron smoke, spiritual flames, floating pills | Flame column, pill burst |
| Luyện Khí Điện | Forge flames, animated tools, floating divine weapons, lightning | Lightning strike, weapon ring |
| Linh Điền & Linh Mỏ | Glowing herbs, water, crystal pulses, flowing spirit veins | Vein surge, crystal bloom |

Ambient intensity bands: L1–3 minimal (one cue), L4–6 moderate, L7–9 rich, L10 signature hero effect. Everything respects the reduced-effects setting.

## 6. Review checklist (per level)

- [ ] Matches the approved specification for this family and level
- [ ] Palette and roof language match this bible and the previous level
- [ ] Clearly more prestigious than the previous level; not smaller
- [ ] Entrance readable and unobstructed; human scale consistent
- [ ] No floating geometry, no baked magic, finished rear
- [ ] Validator passed; footprint within the 20% eave overhang
- [ ] Front, rear, side, top and 45° previews reviewed by the user
