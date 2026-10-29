# World-scale policy (proposal for all six building families)

Status (2026-10-28): **proposal for approval.** Option C (main door 0.52) is provisionally approved for isolated visual QA only. Nothing in gameplay, characters, camera, footprints or saves was changed. No generation, no credits.

Evidence:
- `Reports/ForgeGUI/scale/unity/`: an isolated Unity capture built by `Assets/Editor/ScaleCompareCapture.cs`. It is a temporary unsaved editor scene with the real L1 and L5 models on 3 × 3 lots, the existing 1.41 disciple in front of each entrance, a cell grid and footprint outlines.
- `scale_options_unity.png` puts options A, B and C side by side in front, 45° and real gameplay-camera views (perspective FOV 42 along (31, 29, −38), closest zoom 10). `ALL_gameplay_zoom21_default.png` shows the default zoom. `measurements.csv` holds the numbers.

## 1. What the comparison shows

| Option | L1 world size w × d × h | L5 world size w × d × h | Door L1 / L5 | Disciple ÷ door | Disciple ÷ L1 height |
|---|---|---|---|---|---|
| A current fit | 3.60 × 2.55 × 2.84 | 3.44 × 3.49 × 4.20 | 0.86 / 0.60 | 1.6 / 2.4 (inconsistent) | 0.50 |
| B door 0.62 | 2.60 × 1.84 × 2.05 | 3.54 × 3.60 × 4.33 | 0.62 / 0.62 | 2.3 | 0.69 |
| **C door 0.52** | **2.18 × 1.54 × 1.72** | **2.97 × 3.02 × 3.63** | 0.52 / 0.52 | **2.7** | **0.82** |

Proportion checks (option C):
- **Doors:** identical across L1 and L5 (0.52).
- **Stairs:** risers 0.081 (L1) and 0.065 (L5). Close; L1's steps are a little steeper, as its approved model is.
- **Buildings:** L5 is clearly larger in every dimension and has room for L6–L10 to grow.
- **Character:** the 1.41 disciple stands 2.7 doors tall. In the gameplay view the buildings read as miniatures next to the disciples, and at the closest zoom the disciple almost reaches L1's eaves.

So the building side of the problem is solved by C. The remaining mismatch is purely between the character size and the 3 × 3 lot.

## 2. Realistic or exaggerated proportions?

- **Realistic is not achievable with the current server footprint.** A believable ratio (door ≈ 1.25 × a person) with the 1.41 disciple needs a 1.76 door. That is 3.4 × option C, so L5 would be about 10 units wide on a 3-unit lot. It would only work by changing footprints, which is not allowed.
- **The game already uses exaggerated strategy-game proportions.** The authored kit buildings used for the other five families today were modelled with doors of about 0.72. The gameplay worker actors are about 1.3 tall and the disciple 1.41, so characters are already about 1.8–2 × a door. That is the usual city-builder convention: oversized, readable units on compact lots.
- **What option C changes is the degree,** from about 2 × to about 2.7 × the door, because the approved ForgeGUI L5 is more detailed per unit of footprint than the kit. Keeping 1.41 characters makes the sect look like a model village. Shrinking characters a lot hurts readability at the default camera: at zoom 21 one unit is about 25 pixels on a 1080p screen, so 1.41 → about 35 px and 0.85 → about 21 px.

## 3. Recommended world-scale policy

**P1. One architectural scale for every family.** Main door **D = 0.52** for all six families and all levels, so the whole sect shares one human scale. Each model is fitted by `scale = D / measured source door` and is never fitted to a height rung or squeezed to its lot. Supporting constants at that scale:

| Element | Size |
|---|---|
| Step riser | ≈ 0.07 |
| Railing | ≈ 0.25 |
| "Architectural person" for human-sized props (training dummies, ore carts, herb-bed edges) | ≈ 0.42 |
| Storey | ≈ 0.9–1.1 |

**P2. Lots stay as the server defines them.** Eaves may overhang up to 20% (3 × 3 → 3.6; Chánh Điện 4 × 4 → 4.8; Linh Điền & Linh Mỏ 2 × 2 → 2.4). Plinths stay inside the lot. A model that exceeds its lot at D is regenerated or its overhang approved. Doors and stairs are never shrunk.

**P3. Hierarchy through lot and massing, not by rescaling.** At the same D:

| Family | Lot (frontage limit) | Door widths across | Note |
|---|---|---|---|
| Chánh Điện | 4 × 4 (4.8) | ≈ 10.7 | Naturally the broadest and most ceremonial building |
| 3 × 3 families | 3.6 | ≈ 8 | |
| Linh Điền & Linh Mỏ | 2.4 | ≈ 5.3 | Grows vertically, as the spec requires |

Within each family, every level is at least as tall, wide and visually massive as the previous one, until the frontage reaches the lot limit.

**P4. Families without a main door at low levels** (Diễn Võ Trường L1 open yard, Linh Điền L1 herb patch) are fitted by a different reference at the same scale: the mine entrance = a door (0.52), training dummies = an architectural person (0.42), fence ≈ 0.3.

**P5. Character readability scale, a separate decision.** Characters are presentation and keep their gameplay positions, movement, physics, animation and camera unchanged. Options, none applied:

| Option | Character height | ≈ door | Pros | Cons |
|---|---|---|---|---|
| **K1 (recommended)** | ≈ 0.85 (display multiplier 0.6) | 1.6 × | Buildings stop reading as miniatures; characters still visible at default zoom (≈ 21 px) | Needs a later visual QA with the real characters |
| K2 | unchanged 1.41 | 2.7 × | Maximum readability, no character work | Toy-village look; human props (dummies, carts) look tiny next to disciples |
| K3 | ≈ 0.42–0.55 | ≈ 1 × | Near-realistic | Characters ≈ 10–14 px at default zoom; too small for gameplay readability |

Whatever is chosen applies to all characters (workers, disciples, creatures) through one display multiplier, never per building.

## 4. Footprint conflicts at D = 0.52 (all families)

- **Tàng Kinh Các L9–L10:** need about 8 door widths of frontage, i.e. right at the 3.6 limit.
- **Chánh Điện L7–L10:** two attached side halls, side towers and a dragon platform need about 10 door widths; 4.8 is tight but feasible with flush wings.
- **Luyện Đan Đường / Luyện Khí Điện L7–L10:** two attached towers each; same constraint as Tàng Kinh Các.
- **Diễn Võ Trường L7–L10:** a multi-tier arena with four sword towers. The arena floor holds about 60% of the lot.
- **Linh Điền & Linh Mỏ L5–L10 (2.4 limit):** strictly vertical growth, as the spec already requires.

## 5. If approved, the implementation steps

1. Add semi-automatic door measurement to `validate_glb.py` (front orthographic ruler render, confirmed visually) and record `doorHeightSource` per model.
2. In `SonMonKitBuilder`: fit levels by `D / doorHeightSource` (D = 0.52). The provisional height rungs are removed; the lot and "at least as substantial" checks stay as review warnings.
3. Optionally prototype the K1 character display multiplier in isolated QA only, with screenshots, before any gameplay-scene change.
