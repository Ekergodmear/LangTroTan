# Tàng Kinh Các Level 6 "Tụ Linh Kinh Các": review (awaiting your decision)

## Pre-generation confirmation

Checked in the page immediately before Create Now:

| Item | Value |
|---|---|
| Prompt | Complete revision 1, 2,627 characters; SHA-256 matched `7620cf13…` |
| Settings | 3D Model, Mesh Model Auto, Anime, Automatically Rig off, polygons Auto, 1/1 |
| References | empty |
| Balance and estimate | 323,108; estimated 4,467–5,501 credits |

## Provenance

| Item | Value |
|---|---|
| Job | conversation `d31673e5-314e-4303-adb5-0c1ca8c4b03c`, artifact `40872198-8e97-4731-b625-56b34edc2ae1` |
| **Credits** | 323,108 → 318,250 = **4,858** |
| Original GLB | `work/forgegui/downloads/scripture_L06_forge_d31673e5.glb`: read-only, 15,420,732 bytes, sha256 `0d31ded5…` |
| Model | 9,624 triangles, 1 material, 3 × 2048 textures, no rig or animation; integrity and Stage II proportion checks pass |

## Dimensions (door 0.52)

The door is the closed lattice double door in the main-hall side bays, 0.151 source units, scale ×3.44. The centre bay holds a round jade screen, not doors.

| | Width | Depth | Height |
|---|---|---|---|
| L4 | 5.01 | 4.41 | 3.05 |
| L5 (approved art) | 4.57 | 4.93 | 4.16 |
| **L6** | **6.55** | **4.55** | **3.75** |
| Preferred | 7.0–7.6 | 5.0–5.8 | 4.6–5.4 |

⚠ **Width 6.55 is below the 6.9 flag threshold.** Even at the favourable end of the ±5% door error it reaches only 6.89.

Further flags:
- **Height:** 3.75, lower than Level 5.
- **Depth:** 4.55, below range.
- **Against L5:** 1.43× wider, 0.90× the height, and front outline area only **1.05×** (the jump criterion was 1.5×).
- **Fit:** it still sits inside the proposed Stage II 9 × 7 lot.

## Comparisons

- **Blender:** `L4_L5_L6_door052_{front,side,45,top}.png` and `_sheet.png`, at the same door scale, lighting and cameras.
- **Unity:** `unity/L4_L5_L6_unity_{views,gameplay}.png`, in an isolated unsaved scene at gameplay zoom 10, 21 and 30.

## Checklist

| # | Requirement | Result |
|---|---|---|
| 1 | Dramatic Stage II jump | ❌ width 6.55 (< 6.9), front area 1.05× L5 |
| 2 | Central hall: three storeys, three roofs | ✅ |
| 3 | Two symmetrical two-storey wings | ◐ two roofs each, symmetrical; but they are **separate pavilions on their own terraces**, linked to the hall by covered upper-level galleries, with tops about level with the hall's second roof |
| 4 | Depth 5.0–5.8, height 4.6–5.4 | ❌ 4.55 deep, 3.75 tall (lower than L5) |
| 5 | Continuous second-floor balcony | ◐ balconies on each building plus covered galleries, not one continuous front balcony |
| 6 | Two-tier terrace, branching staircase | ◐ raised central terrace with twin flights beside a carved central ramp; wings have their own stairs |
| 7 | Four corner stone pillars | ◐ jade-capped stone posts at the stair heads and terrace front; not clearly four at the terrace corners |
| 8 | Ornament | ✅ carved brackets, gold ridges and finials (dragon eave ornaments not distinct) |
| 9 | Doors and windows | ◐ closed lattice doors in the side bays; centre bay is a round jade screen; lattice windows throughout |
| 10 | No reserved elements | ⚠ the covered upper-level galleries read like Level 8's elevated bridges |
| 11 | Identity | ✅ same jade-teal roofs, red timber, ivory stone, restrained gold |
| 12 | Clean geometry | ◐ integrity passes, symmetric roofs; one smeared texture panel in the right main-hall bay |

## Assessment

**Strengths:** visually the first multi-building complex: a central three-storey hall with symmetrical two-storey wing pavilions. It reads as a richer, broader compound at gameplay zoom.

**Problems:**
- It misses the dramatic Stage II jump: 6.55 wide, front area ≈ L5.
- It's lower than Level 5.
- Several details are only partial: balcony, corner pillars, door.
- The bridge-like galleries anticipate Level 8.

No retries were made. Nothing was integrated, committed, or changed in server data, footprints, saves or production scenes.
