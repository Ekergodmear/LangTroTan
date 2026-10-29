# Tàng Kinh Các Level 1 "Tàng Thư Tiểu Các": generation review

Status: **candidate, awaiting your visual approval.** No other model was generated.

## Generation and provenance

| Item | Value |
|---|---|
| Authorisation | One generation, approved by the user 2026-10-28 |
| Tool / settings | ForgeGUI Text to 3D Assets · 3D Model · Mesh Model Auto · Style Anime · Automatically Rig **off** · Polygons Auto · 1 of 1 |
| Prompt | Manifest `scripture_L01.generationPrompt`, 1,084 characters (Art Bible style block + approved L1 text verbatim + technical suffix). Typed into ForgeGUI and checked line by line before Create Now; ForgeGUI shows it unchanged as the job's chat title |
| Job reference | ForgeGUI conversation `a64ad4bd-2630-495b-9015-a49170ca7a87` |
| **Credits** | 354,891 before → 350,306 after = **4,585 credits** for one model |
| Original GLB | Saved by you to `Downloads/premium-stylized-anime-xianxia-chinese-fantasy-architecture-.glb`; preserved read-only as `work/forgegui/downloads/scripture_L01_forge_a64ad4bd.glb` |
| sha256 | `6a925fbb5e8511fd2f7641e70308f9545b2f2b83d9b58313049544f1e1bbf79d` (7,161,424 bytes) |
| Conversion | `Assets/ThirdParty/ForgeGUI/ScripturePavilion_L01/` (FBX + albedo, normal, mask) via `Tools/Blender/import_glb_asset.py`; the original is not modified |

Cost projection at 4,585 per model: the remaining 8 Tàng Kinh Các levels (L2–L4, L6–L10) are about 36,700 credits without retries; all 59 new models are about 270,500.

## Blender validation (`scripture_L01_report.json`)

Passed. 1 mesh, **4,914 triangles**, 1 material, 3 × 2048 textures (base colour, metallic-roughness, normal), no armature, no animation. Source bounds 1.884 × 1.334 × 1.487 (a 1.41 : 1 rectangle, entrance on the long side, facing front). Previews: `scripture_L01_front / rear / side / top / 45.png` and `scripture_L01_sheet.png`.

## Against the approved Level-1 specification

| Spec element | Result |
|---|---|
| One-story library pavilion | ✅ |
| One curved jade-teal roof | ✅ (hip-and-gable with upswept ends) |
| Four main red timber pillars | ✅ four on the front |
| Low ivory stone platform | ✅, but deeper and more elaborate than "low" (large front porch) |
| Three front steps | ✅ three treads |
| Double wooden doors | ✅ with gold studs |
| Two lattice windows | ✅ with jade medallions |
| Wooden signboard | ⚠ only a vague panel above the door; not readable as a signboard |
| Two small lanterns | ✅ hanging at the front eave corners |

**Not in the spec:** a stone balustrade around the platform (the spec introduces carved railings at L2) and a second door at the rear.

## Against the Level-5 candidate and the Art Bible

- **Consistent:** palette, glazed tile language, gold ridge ornaments, jade medallions, red lacquer and ivory stone all match L5. L1 is clearly simpler and lower: a sound start for the progression.
- **Defect:** the back-right roof wing is warped into an asymmetric hook (45° and top views); the other three corners are regular.
- **Style drift:** the gold ornament is heavy for "restrained" at L1, which narrows the visual gap to L5. Geometry is soft and blobby close up, with detail mostly in the texture (same as L5).
- **Scale and progression:** both models were generated at almost the same human scale (door ≈ 0.6 source units), but L1 is wider than L5 (1.88 vs 1.58) because of its porch. At matching human scale L1 would overhang the 3 × 3 footprint by about 39%. For the preview it is fitted to 3.6 wide (20% eave rule), giving a door about 6% smaller than L5's. It is then still slightly wider than L5 (3.60 vs 3.49), flagged as a review item (`REVIEW` in the QA results), not hidden.

## Unity preview (isolated QA only)

Fresh visual-slice fixture (port 8901, MongoDB 27029), `-visual-tiers -tier-only`, real Unity camera renders, production scene unchanged. Result: **78 PASS, 1 REVIEW** (the width item above), COMPLETE. The run included:

- server level 1 renders the new L1 model;
- every level 10 → 1 keeps the same GameObject with no duplicates;
- the upgrade transitions, cancellation, reduced effects and MAX panel;
- **player-built buildings never resolve a core family (identity-based mapping)**.

The **full regression** (all earlier checks plus the level suite, `unity/regression-full.txt`) gives **110 PASS, 1 REVIEW**, COMPLETE. A first full run failed one check, "transition 5 -> 6 ends clean". It inspected the scene in the same frame the effect ended, before Unity's end-of-frame destroy. It now waits the same two frames as the other cleanliness checks; the effect itself had ended normally after 3.6 s.

Screenshots are in `unity/`: `level-scripture-server-L1`, `level-scripture-L01`, `level-scripture-L05`, `unity-L01-vs-L05.png`.

## Decision needed

Approve L1 as is, or ask for **one** regeneration aimed at:
1. a symmetric roof;
2. a readable signboard;
3. no balustrade;
4. a shallower platform (narrower than L5);
5. less gold.

A regeneration costs about 4,585 credits and needs your explicit approval.
