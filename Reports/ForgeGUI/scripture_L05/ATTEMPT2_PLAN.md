# Tàng Kinh Các Level 5, attempt 2: revised prompt for approval (nothing generated)

## Attempt 1: rejected

- **Model:** `work/forgegui/downloads/scripture_L05_forge_cb782aa3.glb` (read-only, sha256 `ff96c122…`), 5,501 credits.
- **Decision:** rejected as the official Level 5 because it has **four storeys and four roof tiers**, and at the 0.52 door it is **3.18 wide** against Level 4's 5.01.
- **Kept as:** a potential reference candidate for Level 7 or 8, not assigned to any level (`asset_manifest.json → scripture_L05.newModelAttempts[0]`). Nothing in Unity production changed.

## Why attempt 1 went wrong (the prompt's part in it)

| Attempt-1 wording | Likely effect |
|---|---|
| "grown upward into a prestigious three-story **pagoda tower**", "a tall stepped **pagoda** profile" | "Pagoda" and "tower" pull towards slim, many-tiered towers. The generator added a fourth tier and shrank the plan |
| "the tallest and most imposing level so far", "each roof smaller than the one below" | Rewarded height and tapering, nothing held the width |
| "**compact** three-story central scripture tower", "compact footprint" | Read as "small plan", hence 3.18 wide |
| "front view roughly as tall as it is wide" | Ambiguous; the result came out 1.49 : 1 tall |
| "a decorative spirit-beast **finial**" | Became a tall gold spire above a small top floor, so the top read as an extra tier |
| No explicit floor count or "nothing above" rule | Nothing forbade a fourth roof or attic |

## Revised prompt (revision 2, 2,652 characters, sha256 `96cfb4fc…`, text only, no reference image)

> Premium stylized anime Xianxia Chinese fantasy architecture inspired by traditional Chinese immortal sects, game-ready 3D model with full 360-degree finished geometry. Materials and palette only: any roofs are jade-teal glazed curved tiles, deep-red lacquered timber, ivory-white stone foundations, restrained antique gold details, turquoise jade accents, realistic human-scale doors, windows, stairs and columns. Tàng Kinh Các — Scripture Pavilion, level 5 of 10, "Linh Ngọc Tàng Kinh Các": the broad scripture hall of the earlier levels raised to exactly three storeys. Silhouette first: a wide three-story scripture hall, slightly wider than it is tall (front view about 5 wide by 4.5 tall), as broad as the level 4 hall. Exactly three occupied floors, counted from the ground: ground floor, second floor, third floor. Exactly three main curved jade-teal roofs, one over each floor: a broad first roof over the ground floor, a second roof over the second floor, and the third-floor roof as the top roof. Each upper floor steps in only slightly, so the building stays wide and solid, not a slim tower. Every floor has solid deep-red timber walls, carved red columns and clearly visible lattice windows, and the three floors are clearly separated by the roofs. Ground floor: closed carved wooden double doors at the centre with lattice windows on either side. Second floor: a large turquoise jade jewel medallion in a gold frame on the front wall, centred above the main doors, and a carved wooden balcony. Two small one-story side annexes attached to the left and right of the ground floor, each one bay wide, with small curved roofs lower than the first main roof. An ivory-white stone terrace with carved stone balustrades and a grand wide central staircase at the front. Restrained antique gold ridge and eave trim, small hanging lanterns attached under the eaves, and only a low spirit-beast ornament on the top ridge. Nothing above the third-floor roof: no fourth roof, no attic, no small top pavilion or lantern room, no spire, no tall finial, no roof decoration that looks like another floor. No corridor ring, no courtyard, no extra towers. All roof corners upturned and symmetrical, all parts connected. Same door and step size as the earlier levels. Same materials as levels 1 to 4: jade-teal glazed roof tiles, dark red lacquered wood, ivory stone, restrained antique gold ornaments. One self-contained structure centred on its square footprint, entrance facing the front and unobstructed, flat ground-level base, static architecture only, no particles, no light trails, no floating objects, no characters, no surrounding terrain, no text.

## What changed from revision 1

1. **Building type:** "pagoda tower" is gone. It's now "a **wide three-story scripture hall** … as broad as the level 4 hall", and "not a slim tower".
2. **Explicit counting:** "Exactly three occupied floors, counted from the ground: ground floor, second floor, third floor. Exactly three main curved jade-teal roofs, one over each floor … the third-floor roof as the top roof."
3. **Nothing above the top roof:** it names everything that could read as a fourth tier: "no fourth roof, no attic, no small top pavilion or lantern room, no spire, no tall finial, no roof decoration that looks like another floor". The finial becomes "only a low spirit-beast ornament on the top ridge".
4. **Width held:**
   - "slightly wider than it is tall (front view about 5 wide by 4.5 tall)";
   - "each upper floor steps in only slightly";
   - the word "compact" is removed; the only footprint limit is "no corridor ring, no courtyard, no extra towers".
5. **Floors legible:** "every floor has solid deep-red timber walls, carved red columns and clearly visible lattice windows, and the three floors are clearly separated by the roofs".
6. **Jewel placed precisely:** "on the front wall of the second floor, centred above the main doors".
7. **Side annexes:** "attached to the left and right of the ground floor, each one bay wide, with small curved roofs **lower than the first main roof**". Their roofs sit clearly below tier 1, so they can't be counted as a main tier.
8. **Unchanged:** the style and technical blocks; closed doors (for door measurement); ivory terrace and grand staircase; gold trim; attached lanterns; same materials; no reference image.

The prompt is longer (2,652 vs 2,291 characters) but still under the 4,000 limit.

## Settings and cost

- **Settings:** Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation, References empty.
- **Estimated cost:** 4,467–5,501 credits (the last three jobs).

## Targets (door 0.52, Unity units)

| | Width | Depth | Height |
|---|---|---|---|
| L4 (approved with notes) | 5.01 | 4.41 | 3.05 |
| Attempt 1 (rejected) | 3.18 | 3.45 | 4.73 |
| **Attempt 2 target** | **4.8–5.5** (mandatory) | 3.8–4.6 (preferred) | **4.4–5.0** (mandatory) |

## Checklist for attempt 2 (mandatory items marked M)

| # | Requirement | Pass condition |
|---|---|---|
| 1 M | Exactly three occupied storeys | Three floors with walls and windows in the front and side views |
| 2 M | Exactly three main roof tiers | Three tiers at zoom 21 and in the front, side and 45° views. No fourth roof, attic, top pavilion or spire that reads as a floor |
| 3 M | Width 4.8–5.5 | Door-normalised |
| 4 M | Height 4.4–5.0 | Door-normalised; taller than L3 (3.63) and L4 (3.05) |
| 5 | Two side annexes | One-bay, one-storey annexes attached to the base, roofs below tier 1 |
| 6 | Floors clearly separated | Visible windows and solid walls on every floor |
| 7 | Jade jewel on storey 2 | Large, gold-framed, centred above the doors, visible from the gameplay camera |
| 8 | Ivory terrace, grand staircase, closed doors | Door measurable |
| 9 | Same palette and ornament as L1–L4 | Restrained gold, low ridge ornament only |
| 10 | No static effects, clean geometry | Validator integrity passes; symmetric roofs; finished rear; no smears or floating parts |

After generation, the procedure is the same as before:
- five views;
- L3/L4/L5 comparisons at door 0.52 (front, side, 45° and top);
- isolated Unity gameplay views at zoom 10, 21 and 30;
- one generation, no retries, then stop for your review.
