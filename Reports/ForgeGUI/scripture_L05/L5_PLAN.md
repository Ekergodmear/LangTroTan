# Tàng Kinh Các Level 5 "Linh Ngọc Tàng Kinh Các": plan for approval (nothing generated)

**Context:** Level 3 is a compact two-storey pavilion and Level 4 a broader two-storey compound; both are approved with notes. Level 5 closes Stage I by growing **upward**. It is a compact three-storey scripture tower, the tallest and most imposing level so far, with no corridor ring, courtyard, large wings or extra towers.
- **Approved spec row (verbatim, unchanged):** a three-storey pagoda with three jade-teal roof layers, a white stone terrace, a grand staircase, golden ridges, a spirit-beast finial, a jade medallion and hanging lanterns.
- **Your additions:** a prominent jade jewel on the second storey, a compact footprint, and no static effects.
- **Old Level 5 model:** the old candidate (2.97 wide) stays superseded.

## Design

| Element | Level 4 (measured) | Level 5 (proposed) |
|---|---|---|
| Storeys | 2 | **3** central tower |
| Roofs | 2 stacked central roofs + corridor ring roofs | **3 distinct stacked curved roofs**, each smaller than the one below (stepped pagoda profile) |
| Plan | corridor ring, courtyard, corner pavilions | **compact**: tower plus two small one-bay, one-storey side annexes tucked against its base (keeps the side-room identity of L2–L4) |
| Front | closed centre doors, lattice side bays | closed carved double doors at the centre, lattice windows; **large turquoise jade jewel in a gold frame on storey 2** |
| Balconies | upper balcony | a narrow carved wooden balcony on each upper storey |
| Base | terrace, symmetrical stair | **white ivory stone terrace**, carved stone balustrades, **grand wide central staircase** |
| Ornament | gold trim, jade medallions | ornate restrained gold ridges and eaves, **spirit-beast finial** on the top ridge, small hanging lanterns attached under the eaves (static mesh) |
| Effects | — | none in the GLB. Floating scriptures, glow and particles are Unity effects, added later |

## Preferred dimensions (door 0.52, Unity units; preferred ranges, not automatic rejection)

| | Width | Depth | Height | Front outline |
|---|---|---|---|---|
| L3 (measured) | 4.65 | 3.63 | 3.63 | — |
| L4 (measured) | 5.01 | 4.41 | 3.05 | — |
| **L5 preferred** | **4.8–5.5** | **3.8–4.6** | **4.4–5.0** | height ≥ 1.4× L4 and ≥ 1.2× L3; area ≥ 1.0× L4 |
| Stage I lot | 7 × 5 | | | |

- **Width:** it holds within Stage I, so L5 doesn't have to be wider than L4. Its growth is vertical.
- **Depth:** capped at 4.6, inside the 5-deep lot.
- **Validator ranges** (automatic from these targets): frontage/depth 0.94–1.59, height/frontage 0.72–1.15, plus the strict door-normalised check. A result outside the ranges is flagged for your review, not rejected.

## Visual progression criteria (L3 → L4 → L5)

1. **Vertical growth:** the tallest Tàng Kinh Các so far. It's visibly taller than both L3 and L4 at the same door scale, in the front, side and gameplay views.
2. **Three tiers:** three roofs read as separate tiers at default gameplay zoom (21), not a single roof mass.
3. **Identity:** continuity with L1–L4, with the same jade-teal curved roofs, red timber, ivory stone, gold trim and side rooms. It should read as the same building family, not a different temple.
4. **Compactness:** the plan is no larger than L4's (≤ 22 square units at door scale). No ring, courtyard or extra towers.
5. **Focal point:** the jade jewel on storey 2 is visible from the front and the gameplay camera.
6. **Stage I closure:** L5 should look like the peak of Stage I, leaving the dramatic width jump to L6 (7.0–8.0 wide).

## Prompt (revision 1, 2,291 characters, sha256 `bb24a8e4…`, text only, no reference image)

> Premium stylized anime Xianxia Chinese fantasy architecture inspired by traditional Chinese immortal sects, game-ready 3D model with full 360-degree finished geometry. Materials and palette only: any roofs are jade-teal glazed curved tiles, deep-red lacquered timber, ivory-white stone foundations, restrained antique gold details, turquoise jade accents, realistic human-scale doors, windows, stairs and columns. Tàng Kinh Các — Scripture Pavilion, level 5 of 10, "Linh Ngọc Tàng Kinh Các": the scripture pavilion of the earlier levels grown upward into a prestigious three-story pagoda tower, the tallest and most imposing level so far. Silhouette first: a compact three-story central scripture tower with three distinct stacked curved jade-teal roofs, each roof smaller than the one below, rising in a tall stepped pagoda profile; front view roughly as tall as it is wide, with the central tower clearly dominating the silhouette. Each storey has deep-red carved wooden columns, lattice windows and a narrow balcony with carved wooden balustrades. A large prominent turquoise jade jewel medallion set in a gold frame on the front of the second storey. Ground floor: closed carved wooden double doors at the centre with lattice windows on either side. Two small, low one-story side annexes, each only one bay wide, tucked against the base of the tower with their own small curved roofs. The tower stands on a white ivory stone terrace with carved stone balustrades and a grand wide central staircase at the front. Ornate restrained antique gold roof ridges and eave trim, a decorative spirit-beast finial on the top roof ridge, and small hanging lanterns attached under the eaves. Compact footprint: no corridor ring, no courtyard, no large side wings, no extra towers or pavilions. All roof corners upturned and symmetrical, all parts connected. Same door and step size as the earlier levels. Same materials as the earlier levels: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments. One self-contained structure centred on its square footprint, entrance facing the front and unobstructed, flat ground-level base, static architecture only, no particles, no light trails, no floating objects, no characters, no surrounding terrain, no text.

Settings: Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation, References empty. The length and hash are verified in the page before Create Now; the limit is 4,000 characters.

**Estimated cost: 2,568–4,903 credits** (Levels 3 and 4 cost 4,467 and 4,747).

Notes on the wording:
- **"Closed carved wooden double doors at the centre":** keeps the main door measurable. Level 3's open bay made scaling ambiguous.
- **"Only one bay wide" annexes and the explicit exclusions:** guard against the generator widening the plan again.
- **No spirit crystal or floating elements:** those wait for Level 10, or for Unity effects.

## QA checklist (after generation; against L4 and L3 at the same door scale, lighting and cameras)

| # | Requirement | Pass condition |
|---|---|---|
| 1 | Three storeys | Three floors with their own walls or windows in the front and side views; no fourth floor |
| 2 | Three distinct stacked roofs | Three separate curved roof tiers, decreasing upward, readable at zoom 21 |
| 3 | Taller than L4 and L3 | Door-normalised height 4.4–5.0 (≥ 1.4× L4, ≥ 1.2× L3) |
| 4 | Compact Stage I footprint | Width 4.8–5.5, depth 3.8–4.6, plan ≤ L4's; inside the 7 × 5 lot |
| 5 | No oversized wings or extra structures | At most two small one-bay annexes; no corridor ring, courtyard or extra towers |
| 6 | Foundation and staircase | White ivory terrace, carved stone balustrades, grand central staircase |
| 7 | Doors and windows | Closed centre double doors; lattice windows; main door measurable |
| 8 | Jade jewel on storey 2 | Prominent gold-framed jade jewel on the second-storey front, visible from the gameplay camera |
| 9 | Ornament | Carved red columns, restrained gold ridges and trim, spirit-beast finial, attached hanging lanterns |
| 10 | Identity with L1–L4 | Same palette and roof language; reads as the same family |
| 11 | No static effects | No floating objects, particles, glow sprites or detached parts |
| 12 | Clean geometry | Validator integrity passes; symmetric roof corners; finished rear; no holes or texture smears (the L4 issue) |

After generation, the procedure is the same as for Level 4:
- five views;
- L4 vs L5 and L3 vs L5 at door 0.52 (front, side, 45° and top);
- isolated Unity `StageSiteCapture` at gameplay zoom 10, 21 and 30;
- one generation, no retries, then stop for your review.
