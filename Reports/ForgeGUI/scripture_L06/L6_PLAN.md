# Tàng Kinh Các Level 6 "Tụ Linh Kinh Các": plan for approval (nothing generated)

**Stage II begins here.** Level 6 makes the dramatic jump in width, depth and complexity. It keeps Level 5's three-storey main hall as its centre and grows sideways into a symmetrical complex.

The approved spec row (verbatim, unchanged) asks for:
- an enlarged three-storey pagoda with broader roofs;
- continuous second-floor balconies;
- a two-tier stone terrace with symmetrical staircase branches;
- intricately carved roof brackets, jade balustrades and dragon-shaped eave ornaments;
- four spirit-stone pillars at the terrace corners.

## Design

| Element | Level 5 (approved art) | Level 6 (proposed) |
|---|---|---|
| Composition | single three-storey hall, 4.57 wide | **symmetrical complex**: three-storey main hall plus **two attached two-storey side wings** (the "extended wings" of Stage II) |
| Roofs | 3 tiers | main hall: exactly 3 broader, heavier tiers; wings: exactly 2 tiers each, kept below the hall's second roof, giving a stepped silhouette |
| Balconies | narrow balcony per floor | **continuous second-floor balcony** with jade-green balustrades across the hall and both wings |
| Base | ivory terrace, central stair | **two-tier ivory terrace**; a central stair that **branches into two symmetrical flights** to the upper tier |
| Ornament | gold trim, jade banners (+ jade crystal prop) | carved red and gold roof brackets under every eave; restrained dragon-shaped eave-corner ornaments; gold ridge trim |
| Corner pillars | — | **four carved stone pillars with jade caps** at the terrace corners: solid, attached, not glowing (any glow is a Unity effect) |
| Reserved for later | — | no 4th floor or side towers (Level 7), no bridges or statues (Level 8), no corridor ring or courtyard (that was Level 4) |

The L5 jade crystal prop is a separate Unity prop. Whether Level 6 reuses it is a later Unity decision; it is not in the prompt.

## Preferred dimensions (door 0.52, Unity units)

| | Width | Depth | Height |
|---|---|---|---|
| L4 | 5.01 | 4.41 | 3.05 |
| L5 (approved art) | 4.57 | 4.93 | 4.16 |
| **L6 preferred** | **7.0–7.6** | **5.0–5.8** | **4.6–5.4** |
| Stage II lot (proposed) | 9 × 7 | | |

**Stage-jump criteria:**
- width at least 1.5× Level 5 (≥ 6.9), against the policy's 1.25× minimum;
- front outline area at least 1.5× Level 5;
- taller than Level 5.

**Validator ranges** (automatic from the targets): frontage/depth 1.09–1.67, height/frontage 0.55–0.85, plus the strict door-normalised check. A result outside the ranges is flagged for your review.

**Site:** the proposed Stage II lot (x −13..−4, z 4..11) was clear in the fresh-world check. The footprint and server reservation are still unapproved, so testing stays in the isolated scene.

## Prompt (revision 1, 2,627 characters, sha256 `7620cf13…`, text only, no reference image)

> Premium stylized anime Xianxia Chinese fantasy architecture inspired by traditional Chinese immortal sects, game-ready 3D model with full 360-degree finished geometry. Materials and palette only: any roofs are jade-teal glazed curved tiles, deep-red lacquered timber, ivory-white stone foundations, restrained antique gold details, turquoise jade accents, realistic human-scale doors, windows, stairs and columns. Tàng Kinh Các — Scripture Pavilion, level 6 of 10, "Tụ Linh Kinh Các": the scripture hall of the earlier levels expanded into a much larger and wider symmetrical scripture complex, the first grand complex of the sect library. Silhouette first: a wide composition, front view about one and a half times as wide as it is tall, clearly much wider and deeper than a single hall. At the centre, a three-story main scripture hall with exactly three stacked curved jade-teal roofs, broader and heavier than before. On the left and right, two attached two-story side wings, each with exactly two curved roofs that stay lower than the main hall's second roof, joined to the main hall so the whole front reads as one stepped, symmetrical complex. A continuous second-floor balcony with carved jade-green balustrades runs across the full front of the main hall and both wings. Every floor has solid deep-red timber walls, carved red columns and clearly visible lattice windows; closed carved wooden double doors at the centre of the ground floor. Intricately carved red and gold roof brackets under every eave, and restrained dragon-shaped ornaments at the upturned eave corners. The complex stands on a two-tier ivory-white stone terrace with carved stone balustrades: a wide central staircase rises to the first tier and branches into two symmetrical flights to the upper tier. Four tall carved stone pillars with jade caps stand at the four corners of the terrace, solid and attached to it. Restrained antique gold ridge and eave trim and small hanging lanterns attached under the eaves. No fourth floor, no towers, no spires, no corridor ring, no courtyard, no glowing or floating crystals. All roof corners upturned and symmetrical, all parts connected. Same door and step size as the earlier levels. Same materials as levels 1 to 5: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments. One self-contained, broad rectangular architectural composition with a wide front facade, entrance facing the front and unobstructed, flat ground-level base, static architecture only, no particles, no light trails, no floating objects, no characters, no surrounding terrain, no text.

**How this applies the Level 5 lessons:**
- **No "pagoda" or "tower":** in L5 attempt 1 those words produced a slim, four-tier tower.
- **Exact tier counts:** "exactly three" for the hall and "exactly two" for each wing.
- **Exclusions:** "no fourth floor, no towers, no spires".
- **Width in proportion:** "about one and a half times as wide as it is tall", not door-width counts.
- **Footprint sentence:** your "broad rectangular architectural composition" wording from L5 replaces the old "centred on its square footprint" ending.
- **Spirit-stone pillars:** described as solid carved stone with jade caps and "no glowing or floating crystals", to avoid baked-in magic.

**Settings:** Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation, References empty.

**Estimated cost:** 4,467–5,501 credits (the last four jobs).

## QA checklist (after generation; against L4 and L5 at the same door scale, cameras and gameplay zoom)

| # | Requirement | Pass condition |
|---|---|---|
| 1 | Dramatic Stage II jump | Width 7.0–7.6 (≥ 1.5× L5) and front outline area ≥ 1.5× L5 |
| 2 | Main hall: exactly three storeys and three roofs | Readable at zoom 21; no fourth tier |
| 3 | Two attached two-storey wings | Two roofs each, lower than the hall's second roof, attached and symmetrical |
| 4 | Depth and height | Depth 5.0–5.8, height 4.6–5.4 (taller than L5) |
| 5 | Continuous second-floor balcony | Across the hall and both wings, with jade-green balustrades |
| 6 | Two-tier terrace, branching staircase | Two terrace levels; the central stair splits into two flights |
| 7 | Four corner stone pillars | Solid, jade-capped, attached to the terrace corners |
| 8 | Ornament | Carved brackets, dragon eave ornaments, restrained gold trim |
| 9 | Doors and windows | Closed central doors (measurable), lattice windows on every floor |
| 10 | No reserved or extra elements | No towers, spires, bridges, statues, corridor ring or courtyard |
| 11 | Identity | Same jade-teal roofs, dark-red timber, ivory stone, restrained gold as L1–L5 |
| 12 | Clean geometry | Validator integrity passes; symmetric roofs; finished rear; no smears, floating parts or baked effects |

After generation, the procedure is the same as for Level 5:
- five views;
- L4/L5/L6 at door 0.52 (front, side, 45° and top);
- isolated Unity gameplay views at zoom 10, 21 and 30, placed on the Stage II lot;
- one generation, no retries, then stop for your review.
