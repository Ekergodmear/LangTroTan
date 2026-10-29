# Tàng Kinh Các: human-scale and size progression plan (Levels 1–10)

> **Superseded in part (2026-10-28):** the single-lot target envelopes in §2 and the lot conflicts in §3 are replaced by the approved three-stage size policy, `docs/SCRIPTURE_THREE_STAGE_POLICY.md`. The door rule D = 0.52 still applies.

Status (2026-10-28): **proposal for approval. No generation, no credits spent, no Unity change applied.** Server footprint stays 3 × 3.

Visual comparison: `Reports/ForgeGUI/scale/scale_comparison_L1_L5.png` (rows A, B, C below; 3 × 3 cell grid, orange bar = current disciple height).

## 1. Measured dimensions (L1 approved with notes, L5 candidate)

Door height, riser height and door width were measured on orthographic front renders (precision about ±5%). 1 Unity unit = 1 server cell.

| | Source bounds w × d × h | Door height (source) | Riser (source) | Frontage in door widths |
|---|---|---|---|---|
| L1 | 1.884 × 1.334 × 1.487 | 0.449 | ≈ 0.07 | ≈ 4.2 |
| L5 | 1.556 × 1.581 × 1.901 | 0.272 | ≈ 0.034 | ≈ 6.6 |

**ForgeGUI did not generate the two at the same human scale.** L1's door is 1.65× L5's in source units, and its steps are about twice as tall. My earlier "same scale" estimate (from small thumbnails) was wrong; corrected here.

| Current Unity preview (row A) | World size w × d × h | Door |
|---|---|---|
| L1 (fitted to 3.6 wide) | 3.60 × 2.55 × 2.84 | **0.86** |
| L5 (fitted to 4.2 tall) | 3.44 × 3.49 × 4.20 | **0.60** |

So today L5's door is 30% smaller than L1's, and L1 looks wider than L5. That is exactly the "oversized L1 forces later levels smaller" problem.

## 2. Proposal: one architectural scale for the whole family

**Every level is scaled by the same rule: its measured main-door height becomes a constant D.** No model is scaled to fit a height rung or the lot. Doors, stairs and railings therefore stay the same real size at every level, and each building is as large as its approved architecture makes it.

| Option | D (door, Unity units) | L1 world size | L5 world size | Effect |
|---|---|---|---|---|
| B | 0.62 | 2.60 × 1.84 × 2.05 | 3.55 × 3.60 × 4.33 | L5 already fills the lot (3.6 = the 20% eave limit), so L6–L10 cannot grow in plan, conflicting with "broader roofs", "two-tier terrace", "side towers" and "side wings" |
| **C (recommended)** | **0.52** | **2.18 × 1.55 × 1.72** | **2.97 × 3.02 × 3.63** | L5 uses about 83% of the lot; L6–L10 get about 20% more frontage and depth plus height. L1 becomes a modest pavilion in the middle of its lot, so its porch no longer constrains anything |

How D was chosen: the largest door height at which the approved L5 still leaves room for five more levels of plan growth inside the 3 × 3 lot, eaves at most 3.6.

### Target envelopes at D = 0.52 (eaves at most 3.6 × 3.6; plinth inside 3.0 × 3.0 where possible)

| Level | Storeys (spec) | Frontage w | Depth d | Height h | Note |
|---|---|---|---|---|---|
| 1 | 1 | **2.18** | **1.55** | **1.72** | measured, approved with notes |
| 2 | 1 + porch | 2.3 – 2.6 | 1.7 – 2.0 | ≥ 1.8 | wider porch, larger roof overhang |
| 3 | 2 | 2.4 – 2.7 | 1.9 – 2.3 | 2.4 – 2.8 | first two-storey |
| 4 | 2 + corridors | 2.6 – 2.9 | 2.3 – 2.7 | 2.8 – 3.2 | high terrace, corridors |
| 5 | 3 | **2.97** | **3.02** | **3.63** | measured candidate |
| 6 | 3, broader | 3.0 – 3.2 | 3.0 – 3.2 | 3.7 – 4.0 | broader roofs, two-tier terrace |
| 7 | 4 + 2 mini towers | 3.1 – 3.3 | 3.1 – 3.3 | 4.2 – 4.6 | towers flush to the sides |
| 8 | 4 + taller towers + bridges | 3.2 – 3.4 | 3.2 – 3.4 | 4.5 – 4.9 | three-tier terrace |
| 9 | 5 + side wings + pavilions | 3.3 – 3.5 | 3.3 – 3.5 | 4.9 – 5.3 | **highest plan risk** |
| 10 | 5 + tall side towers + rooftop altar | 3.4 – 3.6 | 3.4 – 3.6 | 5.2 – 5.8 | at the eave limit |

**"At least as substantial" rule (checked per level after human-scale fitting):** height, frontage and front-silhouette area must each be at least the previous level's. Frontage may hold steady once it reaches the 3.6 limit. A failing level is flagged for your review; it is never fixed by shrinking doors.

## 3. Footprint conflicts and how each is handled

1. **L5 at D = 0.62 fills the lot.** That's why D = 0.52 is recommended. Larger D values push L9–L10 past the lot.
2. **L9 (side wings + two auxiliary pavilions + three-bay gateway) and L10 (two tall side towers + galleries)** need about 6.5 door widths of frontage at D = 0.52, which is about the full 3.6. Prompts will ask for a compact square plan, towers and wings attached flush, nothing projecting forward. If a generated model still exceeds 3.6 after human-scale fitting, the options are: regenerate with tighter wording, or approve an eave overhang up to the 20% limit. Doors are never shrunk and the footprint is never changed.
3. **L8 bridges to side towers** must stay inside the eave line. They're checked in the top view.
4. **L1 already approved:** at D = 0.52 it sits at 2.18 wide in the centre of its lot. Optionally, the signboard prop and lanterns can make it read less sparse.

## 4. What changes in the pipeline after approval

- **Validator:** add a semi-automatic door measurement. It writes a front orthographic render with a ruler and records `doorHeightSource` in the manifest; the measurement is confirmed visually, as here.
- **Kit builder:** fit each level by `scale = D / doorHeightSource` instead of the provisional height rungs. Keep the 3.6 eave check and the "at least as substantial" comparison as review warnings.
- **Prompts for L2–L10:** append "compact square plan sized for a small 3x3 lot, about N door-widths wide, same door and step size as the previous level", with N following the table (L2 ≈ 4.5 → L10 ≈ 6.5). The previous level's front render goes in as a reference image. Generators only partly obey this, so §3's after-generation check stays authoritative.
- **Signboard:** a separate Unity prop (carved wooden plaque with gold border) attached at a per-level anchor above the main door, so L1 gets the approved signboard without regenerating.

## 5. Separate issue to decide: character scale

The current disciple prototype is **1.41** units tall. At D = 0.52 (or 0.62), a door is only about 37–44% of a disciple's height, and a disciple stands taller than L1's eaves. Any architectural scale that fits the approved buildings on 3 × 3 lots has this conflict. It's a characters-versus-lot problem, not a building problem. Options:
- (a) Display disciples near buildings at about 0.42 units, so the door is about 1.25 × a person. This is presentation only, and the character models still await approval.
- (b) Accept a stylized "strategy-game" scale, where characters are oversized.

Not changed now; recorded so building scale isn't distorted to compensate.

## 6. Roof-corner investigation (L1)

The front-right roof tip (+X−Y) reaches 0.17 less far out than its mirror twin, which causes the curled look. A prototype local correction on a copy (vertices pulled toward the mirrored healthy corner with a smooth falloff; UVs and topology unchanged) moved at most 0.12 and **did not visibly remove the curl**. Pushing it harder risks folding the tile surface.

Conclusion: no safe automatic fix. A safe manual fix is possible: grab or proportional-edit in Blender, moving vertices only, which keeps UVs, with slight texture stretch at that corner. It belongs in a later polish pass. The prototype is kept outside the project: `work/forgegui/roof_probe_L01/` (before/after renders, report, `corner_fix_prototype.glb`).
