# Tàng Kinh Các Level 6: refined terrace detail pass, QA for final art acceptance

**Status:** awaiting your decision. Not `approved_art` and not integrated.

**Source:** `Tools/ForgeGUI/l6_terrace_detail.py` reads the original GLB only. Its sha256 `0d31ded5…` was re-verified after every run. The original UVs and texture are copied unchanged into the left half of a derived atlas.

**Isolation:** no credits were spent; production scenes, server, footprints and saves are untouched.

## What changed (terrace only)

The building is unchanged from the approved composition: the three-storey hall, two-storey wings, roofs, round jade ornament and the 0.52 door scale.

| Element | Detail |
|---|---|
| Stone material | Procedural ivory stone baked to texture, toned below the brightest atlas ivory so it supports the hall. Brick-coursed tile seams (0.45 m on tiers, 0.30 m on trims), mortar lines and subtle noise variation. A darker trim stone, a jade-stone inlay tone, antique gold and jade |
| Tier fronts | The model's own carved plinth (one panel of the main hall's rear plinth, 0.86 wide), tiled outward from the central stair; each tile ends on its own carved divider. The ends are closed with trim-stone fills |
| Tier sides and rear | Bevelled bodies, carved cornice and base trim bands, raised panel frames |
| Balustrades | Sides and rear of both tiers: posts with caps, top and bottom rails, jade-stone inlay panels. The entrance and stairs stay clear |
| Stairs | Central flight recessed into tier 1 and two symmetrical branch flights to tier 2, all copies of the model's own stair flight (identical steps); stair cheek walls and notch walls in trim stone |
| Corner pillars | Stepped base, framed shaft with jade-stone inlays on four faces, two antique-gold rings, capital, gold collar, faceted jade cap, small gold finial |

**Dimensions at door 0.52:**
- **7.51 × 5.85 × 4.68**. The approved composition was 7.45 × 5.81 × 4.69; the 6 cm of extra width is cornice overhang. The terrace height is unchanged.
- The buildings alone are 7.15 wide.

**Triangles:** 44,495 in total (29,400 of them terrace), under the 60k budget.

## Renders

| Content | File |
|---|---|
| L5 / L6 original / L6 refined, same cameras: front, 45°, top, side | `L6_refined_comparison_sheet.png` |
| Close-ups: terrace, central stair and branches, side balustrade and trims, material / texture quality | `L6_refined_closeups_sheet.png` |
| Corner pillar close-up | `L6_refined_pillar_closeup.png` |
| Unity (isolated unsaved scene): front, 45°, side, top | `unity/L6_refined_unity_views.png` |
| Unity gameplay zoom 10, 21, 30 | `unity/L6_refined_unity_gameplay.png` |
| Unity terrace close-up (material in engine) | `unity/L6_refined_unity_terrace_closeup.png` |

## QA checks

| Check | Result |
|---|---|
| Stretched textures | ✅ terrace texel density uniform: 5th–95th percentile within 0.96–1.08 of the median. Kitbashed pieces keep the original UVs |
| UV seams / bleed | ✅ fixed. Thin UV islands bled the black unbaked background into dark slivers; the background is now filled with the stone tone and the bake margin is 16 px |
| Z-fighting | ✅ none observed. The building sits 1 cm into tier 2 (no coplanar join), and cornices sit 6 mm above the tier tops |
| Floating geometry | ✅ 0 unattached terrace parts (contact test against the terrace, building and kit pieces) |
| Joins: original mesh ↔ terrace | ✅ the base is embedded, plinth tiles meet on carved dividers, the notch meets the central flight, ends are filled |
| Back-face culling | ✅ fixed. The generated mesh has some inversely oriented faces; on the turned plinth copies they vanished in Unity. Only those copies were made double-sided (+406 triangles) |

## Open issues for your judgement

1. **Branch flights are the weakest part.** They are rotated copies of the model's own stair, so their carved central carpet ramp runs sideways inside plain cheek walls. They read as cluttered rather than elegant. Hand-modelled branch flights would fix this.
2. **The terrace is still visually strong at gameplay zoom 21–30.** It's textured now, but it remains a large pale base. Another tone step down or a warmer stone would reduce this further.
3. **Macro softness.** The baked stone is about 260 px per metre and soft in extreme close-up, and the cut ends of kitbashed pieces show some faceting. Neither is visible at gameplay zoom.
4. **Texture memory:** the derived atlas is 4096 × 2048, double the memory of a single original texture set.

## Decisions for you

- **Accept as final art** (`approved_art`) with these notes; integration stays a separate step.
- **One more refinement pass:** hand-modelled branch flights and a darker or warmer terrace tone, without spending credits.
