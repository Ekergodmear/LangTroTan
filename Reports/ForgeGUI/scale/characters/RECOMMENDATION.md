# Character display height at architectural door 0.52 (isolated visual QA)

Status (2026-10-28): **comparison only. Nothing applied globally.** No ForgeGUI generation, no credits; production scene, saves, server data, footprints, movement, colliders, physics, animation timing and camera controls are unchanged.

## Setup

- `Assets/Editor/ScaleCompareCapture.RunCharacterScale`: a temporary, unsaved editor scene.
- Real Tàng Kinh Các L1 (1.72 tall) and L5 (3.63 tall) at door 0.52, on 3 × 3 lots with a cell grid and gold footprint outlines.
- The same existing `Character_disciple` placed directly in front of each entrance, plus a **group of three** standing together.
- One scene, one set of lights and identical cameras for all four heights. Only the disciple's **visual child** is scaled. The root transform (position, rotation, scale 1) is untouched, so the authoritative world position is preserved.
- Cameras:
  - front (eye level) and 45°;
  - the **real gameplay camera** (perspective FOV 42 along (31, 29, −38), distance = zoom × 2.7) at zoom 10 (closest), **21 (default)** and 30.

Unity's reported values (`character_heights.csv`):

| Display height | Door ratio (÷ 0.52) | Rendered mesh | Root scale / position | px @ zoom 10 | **px @ zoom 21 (default)** | px @ zoom 30 |
|---|---|---|---|---|---|---|
| 1.41 baseline | 2.71 | 1.408 | 1.0 / unchanged | 67 | **31** | 21 |
| 0.75 | 1.44 | 0.749 | 1.0 / unchanged | 35 | **16** | 11 |
| 0.85 | 1.63 | 0.849 | 1.0 / unchanged | 40 | **19** | 13 |
| 0.95 | 1.83 | 0.949 | 1.0 / unchanged | 45 | **21** | 14 |

Sheets:
- `sheet_front.png`
- `sheet_45.png`
- `sheet_gameplay_zoom10.png`
- `sheet_gameplay_zoom21_NATIVE.png` (1:1 pixels)
- `sheet_gameplay_zoom30_NATIVE.png` (1:1 pixels)

## Observations from the screenshots

- **1.41:** disciples reach L1's eaves and dominate both entrances. The three-person group is as large as L1, and the buildings read as miniatures. It is the most readable at every zoom (31 px at default).
- **0.75:** proportions look closest to architecture: the head sits around the door lintel and the L5 entrance reads grand. But at default zoom each disciple is only 16 px, and the group of three starts to merge into one white-teal cluster. At zoom 30 (11 px) individuals are hard to tell apart.
- **0.85:** the head sits just under L1's eave line, so the door reads believable for a stylized game, about 1.6 × door. L5's staircase and entrance stay clearly larger than a person. At default zoom (19 px) the three disciples remain separable by their white sleeves and dark hair, with the teal robe contrasting against the grass.
- **0.95:** very close to 0.85 visually (+2 px at default zoom). The person starts to crowd L1's doorway, and the step from 0.85 brings little extra readability.
- **Stairs:** L1 risers 0.081 and L5 risers 0.065 look climbable at 0.75–0.95; at 1.41 each step is ankle-height for the disciple.

## Recommendation

**0.85**, your provisional preference, is supported by the screenshots. It is the smallest height that keeps individuals and a group of three readable at the default zoom, while removing the miniature-village effect of 1.41.

- 0.95 is the fallback if playtests show disciples are hard to pick out at zoom 30 or beyond.
- Before enlarging characters, prefer readability aids that don't distort scale: selection ring, outline on hover, crowd counters.
- 0.75 is too small for gameplay readability at default zoom.

## Limitation before any global rollout

- The disciple prefab supports independent visual scaling safely: its root has exactly one visual child (the model), and the puppet animates bones under it with local rotations.
- **The gameplay worker actors (`SliceArt.Character`) are built from primitives directly on their root**, with no separate visual child. A global display-height setting would first need those parts moved under a "Visual" child. That's a presentation-only change, but it touches the actor construction code, so it is not done here.
