# Tàng Kinh Các Level 5: final art acceptance report (for your review)

**Candidate:** Level 5 attempt 2, `work/forgegui/downloads/scripture_L05_attempt2_forge_fbb9a937.glb`.
- **Hash:** sha256 `7af91efd…`, re-verified unchanged and read-only.
- **Generation:** 5,012 credits, ForgeGUI asset `c1ed8ce1-c693-4cd9-89a3-ee480fe048d3`.
- **Manifest status:** `conditionally_approved` (preferred Level 5 candidate).
- **Attempt 1:** stays rejected and kept as a possible Level 7/8 reference.

## 1. Architecture: kept as generated

Exactly three occupied storeys and three main roof tiers, readable at gameplay zoom 21. Other features:
- ivory terrace and grand staircase;
- closed doors and lattice windows on every floor;
- same palette as Levels 1–4.

No uniform rescale was applied; the door stays 0.52.

## 2. Pending art exceptions (recorded in `asset_manifest.json`)

| Item | Measured (door 0.52) | Target | Deviation |
|---|---|---|---|
| Width | 4.57 | 4.8–5.5 | −0.23 (−4.8%); within the ±5% door-measurement error |
| Height | 4.16 | 4.4–5.0 | −0.24 (−5.5%); still 1.15× L3 and 1.36× L4 |
| Side annexes | missing in the GLB | two, one bay each | optional props prototyped (§3) |
| Second-floor jade jewel | modest jade banner | large jewel | separate prop prototyped (§3) |

## 3. Optional props: the investigation

Prototyped with `Tools/ForgeGUI/l5_prop_prototype.py`. It reads the source GLBs only and writes to `Reports/ForgeGUI/scripture_L05/attempt2/props/`, not to `Assets/` or the kit.

**Annexes: feasible without new credits.**
- **Method:** each annex is a cut copy of an approved Level 3 side room. The Level 3 GLB is untouched.
- **Fit:** the side rooms share the same texture and palette, and at Level 3's door scale their doors are exactly 0.52.
- **Size:** 1.12 wide, 2.58 deep, 1.62 tall each. Main-roof eave tips that overhung the cut were removed.
- **Effect on width:** the overall width becomes **6.72** with annexes (4.57 without). That's over the Stage I range and close to Level 6's planned 7.0–8.0, which weakens the dramatic L6 jump. Placing the annexes partly behind the front line, or omitting one, would reduce this.
- **Clean-up before production:** a small Level 3 bracket stub on the left annex; final placement against the terrace edge, to be tuned in Unity.

**Jade ornament: feasible as a separate prop.**
- **Placeholder:** a faceted jade gem in a gold frame, 0.81 × 1.01, mounted on the second-floor balcony front at 1.79. It sits 0.35 forward of the wall so the first roof doesn't hide it from above.
- **Recommended production version:** a properly modelled prop plus a subtle Unity glow or pulse effect that respects the reduced-effects setting. Both are separate from the GLB.

**Renders (door 0.52, same cameras):**
- `props/L4_L5_L5props_{front,45,top}.png`: Level 4, then Level 5, then Level 5 with props;
- `props/L5_props_closeup_45.png`.

## 4. Level 4 vs Level 5 (same camera, door scale and gameplay zoom)

`unity/L4_vs_L5a2_unity.png` shows front, 45°, and gameplay zoom 10, 21 and 30, from the isolated unsaved Unity scene. Full Level 3/4/5 sheets are in `unity/L3_L4_L5a2_unity_*`.

| | Width | Depth | Height |
|---|---|---|---|
| L4 | 5.01 | 4.41 | 3.05 |
| L5 attempt 2 | 4.57 | 4.93 | 4.16 |

At every zoom, Level 5 reads as the taller, more vertical successor of Level 4's broad compound. It is slightly narrower and does not have Level 4's corridor ring. Both sit inside the proposed Stage I 7 × 5 lot.

## 5. Decisions for you

1. **Accept the width and height exceptions** as final, or keep them open.
2. **Annexes:** use them as optional props (overall width 6.72), place them set back, or leave them out.
3. **Jewel:** use a modelled prop, a Unity effect, or both.

Not done:
- no production integration, kit assignment or server/footprint/save change;
- no credits spent since attempt 2;
- nothing committed.

## 6. Decision (user, 2026-10-29): APPROVED as `approved_art`

**Final art exceptions:** 4.57 w × 4.93 d × 4.16 h at door 0.52. The original GLB, hashes, provenance and attempt 1 are preserved, with no rescale and no regeneration.

**Side annexes:** not added. The larger expansion is reserved for Level 6, and the annex prototypes stay in `Reports/` only.

**Jade crystal prop** (separate, isolated):
- **Mesh:** `Assets/Art/SonMon/Props/JadeCrystal_L05/SonMon_JadeCrystal_L05.fbx`, with a faceted jade crystal, a carved gold frame (eight lobes, ruyi crest, knot) and a red-lacquer backplate; 0.80 × 1.13 × 0.28, 2,020 faces.
- **Mount:** 1.79 above ground, 0.35 in front of the second-floor wall.
- **Effects:** `SonMonJadeAura`:
  - full: a 4.5 s jade emission pulse, a soft additive halo and up to 18 rising motes;
  - reduced effects: a steady dim glow only;
  - nothing at all unless `-visual-tiers` is on.
- **Isolated QA:** `StageSiteCapture.RunL5Jade` writes to `unity_jade/L5_jade_sheet.png` (no prop / full / reduced; close-ups and gameplay zoom 10/21).

**Not done:** no kit assignment, production scene, server, footprint or save change.
