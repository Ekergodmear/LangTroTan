# Tàng Kinh Các: three-stage size policy, targets, site migration and Level 3 proposal

Status: the policy (§1) was **approved by the user on 2026-10-28**. The targets (§2), site strategy (§3) and Level 3 design and prompt (§4) are **proposals awaiting approval**. Nothing has been generated, no credits spent, no gameplay data changed.

This policy supersedes the single-lot target table in `docs/SCALE_PLAN_SCRIPTURE.md` §2 for this family. The door rule (0.52) and character display height (0.85) are unchanged.

## 1. Policy (approved)

Size increases in **three distinct architectural stages**. Within a stage, the horizontal footprint stays roughly constant and progression comes from height, floors, roofs, terraces, balconies and detail. At Levels 6 and 9 the jump must be visually dramatic.

| Stage | Levels | Width (Unity units at door 0.52) | Growth within the stage |
|---|---|---|---|
| — | 1 | 2.18 (measured, approved with notes) | — |
| I | 2–5 | about 5.0–5.5 | L3 two-storey hall → L4 corridors and ornate podium → L5 three-storey jade pavilion |
| II | 6–8 | about 7.0–8.0 | L6 three-storey complex → L7 four storeys with two side towers → L8 bridges and guardian statues |
| III | 9–10 | about 10–12 | L9 five-storey immortal palace → L10 twin towers, white-jade foundation, central spirit crystal |

Fixed rules:
- Same identity at every level: jade-teal curved roofs, dark red timber, ivory stone, gold ornaments.
- Never shrink the door or distort proportions to fit a lot.
- No changes to server footprints, placement, pathing, protected saves or production scenes without separate approval.
- No automatic multi-level generation and no credit spending without approval.

Decisions recorded with the policy:
- **Level 2 attempt 2 is the approved architectural direction.** Its missing railings and signboard are noted. The signboard can be the planned Unity prop, as for Level 1. It is not integrated into Unity yet; that waits for the site review (§3).
- **The old Level 5 model is superseded.** It's 2.97 wide, narrower than Level 2. It stays on disk for provenance and stays wired to the gated `-visual-tiers` prototype until a new Level 5 is approved.

## 2. Proposed size targets (door 0.52, Unity units = server cells)

| Level | Stage | Width | Depth | Height | Storeys and main additions |
|---|---|---|---|---|---|
| 1 | — | **2.18** | **1.55** | **1.72** | measured |
| 2 | I | **5.01** | **2.80** | **2.32** | measured (attempt 2): 1 storey, two side rooms, six-pillar porch |
| 3 | I | **4.65** | **3.63** | **3.63** | measured; **approved with notes 2026-10-29** (narrower than L2, outside the original 5.0–5.5 × 3.0–3.4 × 3.1–3.5 targets) |
| 4 | I | **5.01** | **4.41** | **3.05** | measured; **approved with notes 2026-10-29** (broader two-storey compound with perimeter corridors; shorter than L3) |
| 5 | I | **4.57** | **4.93** | **4.16** | measured; **approved_art 2026-10-29** (attempt 2; accepted exceptions; annexes omitted; separate jade crystal prop + gated Unity glow) |
| 6 | II | 7.0–7.6 | 5.0–5.8 | 4.6–5.4 | proposed 2026-10-29: three-storey hall + two attached two-storey wings, two-tier terrace, branching stair, four stone corner pillars (`Reports/ForgeGUI/scripture_L06/L6_PLAN.md`) |
| 7 | II | 7.3–7.7 | 5.2–5.8 | 6.0–6.6 | 4-storey main pavilion and two side towers, connecting corridors |
| 8 | II | 7.6–8.0 | 5.6–6.4 | 6.6–7.2 | 4 storeys, taller towers, elevated bridges, guardian statues, three-tier terrace |
| 9 | III | 10–11 | 7.0–8.0 | 7.6–8.4 | 5-storey palace, side wings, auxiliary pavilions, ceremonial gateway |
| 10 | III | 11–12 | 8.0–9.0 | 8.6–9.6 | 5 storeys, twin towers, massive white-jade foundation, central spirit crystal (static mesh). The floating celestial scripture is VFX, outside these bounds |

Review rules per level, applied after door normalisation:
- **Within a stage:** width may hold steady (±5%). Height and front-silhouette area must each be at least the previous level's.
- **Stage jumps:** Level 6 must be at least 1.25× Level 5 in width and at least 1.5× in front-silhouette area. Level 9 must be at least 1.25× Level 8 in width and at least 1.6× in area.
- **Out-of-range result:** a result outside its range is flagged for your review. It's never rescaled to fit.

Prompt wording: the size hints in door widths were not followed. Level 2 asked for about 6 door widths and produced about 11. Prompts therefore describe size relative to the previous level ("same overall width as level 2, growing in height"), and the after-generation measurement is authoritative.

## 3. Site layout and footprint migration (proposal)

### What the server does today (read from `backend/server/unity/spatial.js`)

- **Core lots:** the core buildings are fixed lots. Tàng Kinh Các is 3 × 3 at cells x −10..−7, z 4..7. Chánh Điện is 4 × 4 at x −2..2, z −2..2, and the gate is at (0, −4).
- **Pathing:** worker pathing is the server grid. A cell is blocked only inside a building's lot. There is **no Unity NavMesh** (no NavMesh use in `Assets/Scripts`).
- **Placement:** player buildings may be placed **directly next to** a core lot, with no margin, anywhere within radius 24. A two-cell-wide exit corridor from the gate must survive every placement.

So once the visual exceeds 3 × 3, existing saves may have player buildings, trees and worker paths **under** the new model. That's the collision problem to solve before any stage-I model goes into production.

### Proposed lots

The lots are centred on the current lot (x = −8.5). The front edge stays at z = 4, facing the courtyard. Growth is backwards (+z) and sideways only.

| Lot | Cells | Contains the model with eave margin |
|---|---|---|
| Current, L1 | 3 × 3 | 2.18 × 1.55 |
| Stage I, L2–L5 | **7 × 5** (x −12..−5, z 4..9) | up to 5.5 × 4.4 |
| Stage II, L6–L8 | **9 × 7** (x −13..−4, z 4..11) | up to 8.0 × 6.4 |
| Stage III, L9–L10 | **13 × 10** (x −15..−2, z 4..14) | up to 12 × 9.6 |

Fresh-world check (`Reports/WorldScale/site_plan/scripture_stage_lots.png`, `.json`):
- **How it was made:** the server's own `ensureSpatial` was run in memory on a blank state. No database was used and no save was loaded.
- **Stages I and II:** clear (no buildings, trees, stone or deer).
- **Stage III:** overlaps 4 trees and no buildings.
- **Chánh Điện:** every stage keeps a 2-cell gap to it, and the gate corridor is untouched.
- **Limit:** player-built structures in real saves are unknown. Checking them needs a save audit, and the protected saves are never loaded for that.

### Migration phases (each needs your approval before it starts)

| Phase | What | Gameplay change |
|---|---|---|
| 0 (now) | Docs, manifest, targets. The Unity production scene keeps the current Tàng Kinh Các; the world-scale flag stays off | none |
| 1 | **Isolated QA only** (ports 8901/27029): place Stage I/II/III models on their lots; screenshot neighbours, zoom levels 10/21/30, worker paths that cross the model, selection collider | none |
| 2 | **Server reservation (data only):** core lots get a level-dependent size `coreLot(id, level)` used by `blocked()` (pathing) and placement. For **new saves**, placement is refused inside the Stage III envelope from day one, while pathing blocks only the current stage. The unbuilt part renders as a stone courtyard or garden | yes: new rule |
| 3 | **Existing saves (tested on QA copies only):** conflicting player buildings are **not moved silently**. Upgrades that cross a stage (L1→2, L5→6, L8→9) are refused with "clear the site around Tàng Kinh Các first" until the lot is clear, and the player demolishes with the normal refund. Trees inside the lot are removed and respawn elsewhere. A save already above a stage boundary with a blocked lot keeps showing the highest level whose lot is clear, with a notice | yes |
| 4 | Production rollout of phases 2–3 after QA, with a migration report | yes |

Alternative to phase 3: automatic relocation of conflicting player buildings with a full refund. It's simpler for players but moves their property. Not recommended without your approval.

### Open issue that blocks Stage III: Chánh Điện dominance

A 12-wide scripture palace would dwarf the 4 × 4 Chánh Điện (4.8 maximum at the eaves). The approved rule says Chánh Điện stays the dominant landmark. Before Stage III is produced, Chánh Điện needs its own staged policy, larger than this one, and a **sect master plan**. Chánh Điện can't grow towards the gate (only 2 cells in front of it). Growing backwards and sideways would meet the Stage III scripture lot at x = −2, so the master plan may require new core positions for new saves. This is a separate decision; Stages I and II are not affected.

## 4. Level 3 proposal: "Song Tầng Kinh Các"

### Design (Stage I, same footprint as Level 2, grows upward)

- **Kept from Level 2:** the plan, two one-storey side storage rooms with their own curved roofs, six-pillar full-width porch, double doors, four lattice windows.
- **Added:**
  - a **second storey over the central hall** (upper scripture chamber) with its own smaller curved roof, giving **two distinct roof layers** in the centre;
  - an **upper-floor front balcony** with carved wooden balustrades;
  - carved wooden railings on the porch (missed in Level 2);
  - a blank signboard between the two central roofs;
  - an **elevated ivory stone platform** with a **wide central staircase** of about seven steps.
- **Silhouette:** tallest in the centre, stepping down to the wings. Front proportion about 3 wide : 2 tall (Level 2 is about 2 : 1).
- **Target:** 5.0–5.5 w × 3.0–3.4 d × 3.1–3.5 h.

### Proposed prompt (revision 1, text only, no reference image)

> Premium stylized anime Xianxia Chinese fantasy architecture inspired by traditional Chinese immortal sects, game-ready 3D model with full 360-degree finished geometry. Materials and palette only: any roofs are jade-teal glazed curved tiles, deep-red lacquered timber, ivory-white stone foundations, restrained antique gold details, turquoise jade accents, realistic human-scale doors, windows, stairs and columns. Tàng Kinh Các — Scripture Pavilion, level 3 of 10, "Song Tầng Kinh Các": the same wide scripture hall as level 2, now raised to two storeys in the centre. Silhouette first: a two-story central hall rising clearly above two lower one-story side storage rooms; the central hall has two distinct stacked curved jade-teal roofs, a broad lower roof over the ground-floor reading hall and a smaller upper roof over the upper scripture chamber; each side room keeps its own smaller curved roof, so the roofline steps up to its highest point in the centre. Ground floor: a full-width front porch with six red lacquered pillars, double wooden doors and four lattice windows, with carved wooden railings along the porch edge. Upper floor: a scripture chamber with lattice windows and a front balcony with carved wooden balustrades across its width. A blank carved wooden signboard between the two central roofs (no lettering). An elevated ivory stone platform, higher than level 2, with a wide central staircase of seven steps. All roof corners upturned and symmetrical, all parts connected. Exactly two storeys in the centre: no third floor, no tower, no spire. Same overall width as level 2; grows in height, not in width; front view about 3 wide by 2 tall. Same door and step size as level 2. Same materials as the earlier levels: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments. One self-contained structure centred on its square footprint, entrance facing the front and unobstructed, flat ground-level base, static architecture only, no particles, no light trails, no floating objects, no characters, no surrounding terrain, no text.

Length 2,095 characters, sha256 `abd7db40…` (full text and hash in `asset_manifest.json` → `scripture_L03.promptHistory[0]`). It's longer than Level 2's 1,818. The page's input limit will be checked in the page before Create Now, and if it is shorter the prompt comes back to you for trimming.

Settings: Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, Automatically Rig off, 1 generation, References empty.

**Estimated cost: 2,568–4,903 credits** (the range of the three measured jobs).

**Why no reference image:** in attempt 1 the reference made the model copy Level 1. Attaching the Level 2 front render could lock the plan, but risks a one-storey copy. This is an option for you to choose; it isn't the default.

### Level 3 checklist (after generation, against Level 2 at the same door scale and camera)

| # | Requirement | Pass condition |
|---|---|---|
| 1 | Two storeys in the centre | Upper chamber with its own floor and windows; no third floor |
| 2 | Two distinct central roof layers | Two stacked curved roofs visible in front and side views |
| 3 | Side rooms kept | Two one-storey wings, each with its own roof; stepped roofline |
| 4 | Upper balcony with balustrades | Front balcony on the upper floor |
| 5 | Elevated platform and central staircase | Platform visibly higher than L2; about 7 steps |
| 6 | Porch carried over | Six pillars, four lattice windows, double doors |
| 7 | Railings and signboard | Wooden porch railings; blank board above the doors |
| 8 | Stage I size | 5.0–5.5 w, 3.0–3.4 d, 3.1–3.5 h at door 0.52; height ≥ 1.3× L2 |
| 9 | Not a copy of L2 | Front-silhouette IoU vs L2 ≤ 0.75 (same width expected) and area ≥ 1.25× L2 |
| 10 | Same materials | Jade-teal, red lacquer, ivory, restrained gold |
| 11 | Clean geometry | Symmetric roof corners, no holes or white fragments, finished rear; validator passes rig, animation and triangle checks |
| 12 | Door measurable | Main door visible in the front orthographic view for normalisation |

The validator's "square footprint" and "taller than wide" checks don't apply to Stage I proportions. They will be replaced by the stage ranges above before Level 3 is validated. That's a tooling change only.
