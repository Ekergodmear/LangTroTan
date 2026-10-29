# Tàng Kinh Các Level 6: final refinement and spirit-gathering VFX

**Status:** awaiting your visual approval. Not `approved_art` and not integrated.

**Safeguards:**
- The original ForgeGUI GLB is unchanged (sha256 `0d31ded5…` re-verified).
- No credits spent.
- Server, levels, footprints and saves are untouched; no production scene was changed.
- All QA ran in an isolated, unsaved Unity scene, with `-visual-tiers` switched on for that scene only.

## 1. Architecture (`Tools/ForgeGUI/l6_terrace_detail.py`)

- **Staircase, hand-modelled:**
  - **Central flight:** recessed into tier 1, five steps (0.094 rise × 0.17 tread) either side of a carved jade-stone ramp with trim edges.
  - **Side flights:** two symmetrical flights from tier 1 to tier 2, with identical steps, a jade-stone inlay on every riser, and open-side balustrades with sloped rails and jade panels.
  - **Removed:** the rotated stair copies, crossing ramps and cheek walls.
  - The entrance is clear.
- **Terrace tone:** warmer, softer jade-white, slightly dimmer. Panels, trims, balustrades, tile seams and pillars are unchanged.
- **Measured:** 7.51 × 5.85 × 4.68 at door 0.52; **48,022 triangles** (budget 60k). There are no floating parts, and texel density is 0.90–1.08 of the median (5th–95th percentile).

## 2. Spirit-gathering VFX (`SonMonSpiritGathering.cs`, separate from the GLB)

| Effect | Implementation |
|---|---|
| 1 Formation 聚灵阵 | Thin jade-cyan inscribed band on the ground just outside the terrace: double line, rune ticks, four corner seals, open at the entrance; procedural texture. 6 s emission pulse, 16 rune segments lighting in turn, slow inward drift of motes |
| 2 Four spirit pillars | The existing pillar meshes are unchanged. An alpha jade core plus an additive halo on each crystal, pulsing asynchronously (4.6–6.7 s); rising motes; a cyan trail from each pillar towards the hall every 5–8 s, fading before the walls |
| 3 Scripture runes | 8 procedural glyphs in a 4 × 2 atlas, gold and jade; at most 10, orbiting slowly and rising around the second-roof level, fading in and out |
| 4 Spirit mist | Thin jade wisps (alpha 0.11) rising along the terrace edge, sparse motes above the stairs, an inward gathering at the round jade ornament every 6–8 s |

**Modes:**
- **Full:** all four effects.
- **Reduced:** steady dim pillar glow and a faint static formation; no mist, runes, trails or motes. It follows the project's existing reduced-effects setting.
- **Off:** nothing is built. This happens when `-visual-tiers` is off; `-no-ambient-fx` also forces it.

**Distance LOD:**
- **Near** (zoom 10): everything.
- **Mid** (zoom 21): formation (brighter band), pillar glow, mist at half rate; no runes or motes.
- **Far** (zoom 30): formation and pillar glow only. The other systems are stopped and cleared, so they cost no simulation.

**Engineering:**
- Shared materials and textures are created once; GPU-only textures.
- Property blocks; a single vertex-coloured mesh for the rune segments.
- Zero GC allocations per frame (measured).
- Ignore Raycast layer and no colliders, so effects can't interfere with selection.
- Local simulation space, so the effects follow the building root.

**Awakening (L5 → L6):** the steps run in this order:
1. The existing `SonMonUpgradeFx` gathers energy, lights its formation and masks the swap.
2. Once the L6 visual appears, `Build()` detects the in-progress upgrade (`ToLevel == 6 && Swapped`).
3. The ground formation flashes.
4. The four pillars light in sequence (0.25 s apart).
5. A jade ring pulse expands outward.
6. The effects settle into ambient over 2.4 s.

Loading a save never plays it, because no upgrade is in progress. **Not yet wired:** L6 is not assigned in the kit (integration needs your approval). The QA forced the awakening; the live server-confirmed path has not been tested.

## 3. QA results (measured in the isolated Unity scene unless marked)

| Item | Result |
|---|---|
| Particles, full mode | **65** at zoom 10 · **28** at zoom 21 · **8** at zoom 30 |
| Particles, reduced mode | **8** (static pillar glow) at every zoom |
| Active VFX renderers (≈ draw calls, *estimate*) | full: 10 / 8 / 3 · reduced: 3 · off: 0 |
| GC allocations per frame | **0 bytes** (300-frame probes, near and far LOD) |
| CPU per update | **0.28 ms** in the editor, including manual particle simulation (*upper bound*; a play-mode build should be lower) |
| Particles inside terrace solids | **0** of 55 |
| VFX colliders | 0; layer Ignore Raycast |
| Off / reduced behaviour | off: nothing built; reduced: only glow and a faint formation, as specified |
| VFX texture memory | 5.9 MB, 5 textures (*editor figure*; formation 768 × 600 with mips is the largest) |
| **Model texture memory** | albedo 11.2 MB, normal 22.4 MB, mask 22.4 MB = **≈ 56 MB** (*editor figure, 4096 × 2048 atlas*). **Too high:** see issues |
| Z-fighting, UV, missing faces | none seen in the renders; stairs and terrace pass the floating and texel-density checks |
| Bloom / clutter | restrained: no bloom post-effect is used, and the effects stay thin |

## 4. Renders (`unity/`)

| File | Content |
|---|---|
| `sheet_L5_vs_L6.png` | L5 / L6 off / L6 full: front, 45°, side, top, identical camera and lighting |
| `sheet_modes_gameplay.png` | off / full / reduced at zoom 10, 21, 30 |
| `sheet_modes_closeups.png` | stairs, pillar + formation, formation corner, roof runes in each mode |
| `sheet_motion_frames.png`, `motion_pillar.gif` | 6 frames 0.75 s apart: pulses, trails, runes moving |
| `sheet_awakening.png`, `awakening.gif` | L5 → L6 awakening, 0–2.4 s |

## 5. Known issues

1. **Texture memory (≈ 56 MB) is the main technical problem.** The derived 4096 × 2048 atlas is imported uncompressed for normal and mask. It needs a compression and import pass, or a separate 1024 terrace texture set instead of the doubled atlas, before integration.
2. **Subtle at gameplay zoom.** The spec asked for restraint, so the effects are subtle. At zoom 21 the pillar caps and a faint formation line remain, but the formation is hard to see against bright grass. Runes are small even at zoom 10. If you want stronger readability, the next step is to raise formation alpha at mid LOD and rune size.
3. **Formation placement.** The formation sits on the ground just outside the terrace (8.2 × 6.6), because the tier tops are mostly covered by buildings and stairs. It fits the proposed 9 × 7 Stage II lot.
4. **Awakening not live-tested,** because L6 isn't in the kit yet (see §2).
5. **No HDR bloom pass.** The glow comes from additive sprites only, which avoids bloom blowout but limits "light" intensity.
