# Tàng Kinh Các Level 4 "Huyền Văn Kinh Các": plan for approval (nothing generated)

**Starting direction:** Level 3 (approved with notes 2026-10-29). Keep its two-storey hall, stacked jade roofs and upper balcony, and grow the plan **sideways** with perimeter corridors and a high terrace. The approved spec row for Level 4 (verbatim, unchanged) asks for:
- an enlarged two-storey pavilion with surrounding covered corridors;
- a high stone terrace and a symmetrical staircase;
- a three-bay main entrance;
- carved columns, cloud motifs and restrained gold roof trim;
- two small stone lantern towers.

## Design

| Element | Level 3 (measured) | Level 4 (proposed) |
|---|---|---|
| Central hall | 2 storeys, open ground-floor bay | 2 storeys, **closed**: three-bay entrance with three pairs of carved doors, lattice windows on both floors |
| Roofs | 2 stacked central roofs + 2 small side roofs | 2 stacked central roofs + **long low corridor roofs on all four sides** + 2 front corner-pavilion roofs, all with gold ridge and eave trim |
| Plan | hall + two one-bay side rooms | hall inside a **perimeter corridor ring**; the front corridor runs out to two corner pavilions, so it's clearly wider |
| Base | raised platform, about 7 steps | **high ivory terrace** with carved stone balustrades, symmetrical central staircase |
| Ornament | jade medallions | carved red columns with cloud motifs, **scripture-scroll reliefs** on beams and terrace panels, blank wooden signboard |
| Extras | lanterns | **two stone lantern towers** on the terrace front corners, symmetrical, attached |

The design avoids Level 3's misses by asking explicitly for:
- closed doors (not an open hall);
- lattice windows on both floors;
- a building "clearly wider than level 3 and noticeably wider than tall".

As before, there are no door-width numbers, because the generator ignores them.

## Target size (door 0.52, Unity units)

| | Width | Depth | Height | Front outline area |
|---|---|---|---|---|
| L3 (measured) | 4.65 | 3.63 | 3.63 | — |
| **L4 target** | **5.2–5.5** (≥ 1.12× L3) | **3.8–4.2** | **3.7–4.1** (≥ L3) | ≥ 1.25× L3 |
| Stage I lot | 7 × 5 | | | |

This replaces the earlier L4 range of 5.2–5.5 × 3.6–4.0 × 3.5–3.9, which had a minimum height below L3's measured 3.63. Level 5 stays at 5.3–5.5 × 3.8–4.4 × 4.4–5.0, so there is still height to gain there.

Validator ranges (`validate_glb.py … asset=scripture_L04`, derived automatically from the manifest targets):
- frontage/depth 1.11–1.59;
- height/frontage 0.61–0.87;
- strict door-normalised size in the ranges above.

## Prompt (revision 1, 2,280 characters, sha256 `7a41c5ea…`, text only, no reference image)

> Premium stylized anime Xianxia Chinese fantasy architecture inspired by traditional Chinese immortal sects, game-ready 3D model with full 360-degree finished geometry. Materials and palette only: any roofs are jade-teal glazed curved tiles, deep-red lacquered timber, ivory-white stone foundations, restrained antique gold details, turquoise jade accents, realistic human-scale doors, windows, stairs and columns. Tàng Kinh Các — Scripture Pavilion, level 4 of 10, "Huyền Văn Kinh Các": the two-storey scripture pavilion of level 3, now enlarged into a wider and more elaborate complex. Silhouette first: a two-story central scripture hall with two stacked curved jade-teal roofs and an upper balcony with carved wooden balustrades, standing on a high ivory stone terrace; covered corridors with their own long, low curved roofs surround the hall on all four sides, and at the front they extend left and right well beyond the hall to two small corner pavilions, so the building is clearly wider than level 3 and noticeably wider than tall, front view about 4 wide by 3 tall. Front: a three-bay main entrance with three pairs of closed carved wooden doors (a closed building, not an open hall), lattice windows on both floors, carved red lacquered columns with cloud motifs, and carved scripture-scroll reliefs on the beams and terrace panels. A symmetrical stone staircase rises to the terrace at the centre front, with carved ivory stone balustrades along the terrace edge. A blank carved wooden signboard above the central doors (no lettering). Two small stone lantern towers stand on the terrace at the left and right front corners, attached to it. Restrained antique gold trim along all roof ridges and eaves. All roof corners upturned and symmetrical, all parts connected. Exactly two storeys: no third floor, no tower over the hall, no spire. Same door and step size as level 3. Same materials as the earlier levels: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments. One self-contained structure centred on its square footprint, entrance facing the front and unobstructed, flat ground-level base, static architecture only, no particles, no light trails, no floating objects, no characters, no surrounding terrain, no text.

Settings: Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation, References empty. At 2,280 characters it's under the documented 4,000 limit; the length and hash are checked in the page before Create Now.

**Estimated cost: 2,568–4,903 credits** (four measured jobs; Level 3 was 4,467).

**Reference image:** none by default. The Level 3 front render could lock in the two-storey identity, but in attempt 1 a reference produced a near-copy. Attaching it is your option.

## QA checklist (after generation; against Level 3 at the same door scale, lighting and cameras)

| # | Requirement | Pass condition |
|---|---|---|
| 1 | Two storeys, L3 identity | Two stacked central roofs and an upper balcony; no third floor or tower |
| 2 | Perimeter corridors | Covered corridors with their own roofs visible around the hall in the top and 45° views |
| 3 | Clearly wider than L3 | Door-normalised width 5.2–5.5 (≥ 1.12× L3) |
| 4 | Size in range | Depth 3.8–4.2, height 3.7–4.1, inside the Stage I 7 × 5 lot |
| 5 | High stone terrace, symmetrical staircase | Terrace visibly higher than L3's platform; central stair; carved stone balustrades |
| 6 | Three-bay entrance with doors | Three door bays on the front, closed doors (the L3 miss) |
| 7 | Lattice windows | On both floors of the front |
| 8 | Ornament | Cloud-motif columns, scripture reliefs, gold ridge/eave trim, blank signboard |
| 9 | Lantern towers | Two, symmetrical, standing on the terrace, attached |
| 10 | Richer than L3 | Front outline area ≥ 1.25× L3; overlap ≤ 0.75 |
| 11 | Same palette | Jade-teal roofs, red timber, ivory stone, restrained gold |
| 12 | Clean geometry | Validator integrity passes (no rig or animation, textures decode, < 60k triangles, < 1% degenerate faces); symmetric roof corners; no floating parts; finished rear; main door measurable in the front view |

Renders and QA will follow the Level 3 procedure:
- five views;
- Level 3 vs Level 4 at door 0.52 in Blender (front, 45° and top);
- isolated Unity `StageSiteCapture` (Level 3 and Level 4 on the site, gameplay zoom 10, 21 and 30);
- one generation, no retries, then stop for your review.
