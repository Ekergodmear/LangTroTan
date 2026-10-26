# Xianxia Art Direction — "Thanh Vân Sơn Môn"

Status: **proposal, needs your approval.** No implementation exists yet. The images in `outputs/VisualSlice/` are real Unity captures of the *previous* prototype, not of this direction.

## 1. Pillar statement

**Detailed stylized anime Xianxia.** Think hand-painted stylized 3D in the vein of Genshin-like environment art: clean readable shapes, painted texture detail, soft cel-influenced lighting and saturated but harmonious colour. It must not look like generic low-poly, and it must not look photoreal.

Three words for every review: **Elevated · Serene · Luminous**
- *Elevated*: the sect sits *on* a mountain. Terraces, stairs and cliffs give vertical hierarchy.
- *Serene*: mist, water, pine, negative space, slow motion.
- *Luminous*: spiritual energy shown as restrained light (jade glow, gold accents, drifting motes), never neon noise.

## 2. Composition of the Sơn Môn (vertical slice)

```
              distant layered peaks (3 depth planes, atmospheric falloff)
                  waterfall from cliff ─┐          floating spirit rock w/ pavilion
        pine cliffs ─┐                  │                     │
                     ▼                  ▼                     ▼
   [Upper terrace]  CHÁNH ĐIỆN — 2-tier hip-and-gable roof, 9-bay front, on 3-step stone plinth
                         ▲ grand central stair (axis)
   [Middle terrace] courtyard: formation circle (Tụ Linh Trận) · bronze cauldron · lantern rows
         side halls (Tàng Kinh Các pagoda left, Luyện Đan Đường right) frame the axis
                         ▲ second stair
   [Lower terrace]  service ring: Dân Xá, Kho, fields, workshops — playable build zone
                         ▲
   SƠN MÔN gate (paifang, 3 bays) ─ stone bridge over stream ─ path into bamboo forest
```

- Use one strong **central axis** (gate → bridge → stairs → hall). Asymmetric nature frames it.
- The camera's default framing at gameplay zoom shows the gate in the lower third and the hall in the upper third.
- The **build zone** stays on the lower and middle terraces. Its grid must stay compatible with the server's `spatial` coordinates (no gameplay contract change). Terraces are visual layers. Server cells map to terrace heights by a lookup on the client only.

## 3. Shape language

| Element | Rule |
|---|---|
| Roofs | Concave curve with upturned eaves (翘角). Visible tile rows, ridge caps, ridge-end ornaments (鸱吻 / small beasts). The main hall gets a double eave |
| Structure | Red-lacquer round pillars, visible dougong bracket bands under eaves, lattice doors and windows, carved railings |
| Base | Every building sits on a stone plinth with steps. Nothing touches grass directly |
| Hierarchy | Chánh Điện ≥ 2.5× the height of a Dân Xá. Pagoda/tower for scripture. Production buildings are lower, wider and simpler |
| Silhouettes | Each building type must be identifiable by silhouette alone at max zoom-out |
| Nature | Pines are asymmetric, wind-swept and layered. Bamboo grows in dense clumps. Spirit trees (pink/white blossom) are rare focal accents. Rocks are tall vertical karst shapes |

## 4. Colour script

| Role | Hex | Use |
|---|---|---|
| Jade tile | `#2F6F6A` → `#4E9C92` | Roofs (main), UI primary |
| Lacquer red | `#9E2B25` | Pillars, gates, accent only |
| Ivory plaster | `#EDE3CC` | Walls, UI parchment base |
| Warm stone | `#B8B0A0` | Plinths, stairs, paths |
| Bronze / gold | `#A97B3B` / `#D9B45A` | Trim, ornaments, UI borders |
| Pine / foliage | `#3E6B48`, `#6E9A5A` | Vegetation, kept *less* saturated than the jade roofs |
| Spirit glow | `#9FF3E2` (cool), `#FFD98A` (warm) | Formations, crystals, lanterns |
| Mist | `#CFE3E2` | Fog, at a distance only |

Value rule: the sect (ivory, red, gold) is the brightest, highest-contrast area of the frame. Vegetation sits mid-value, and the distance is light and low-contrast. The current build inverts this, because fog flattens the whole frame.

## 5. Lighting and atmosphere (URP)

- One warm directional key (around 35° elevation, `#FFE7C2`) and a cool sky ambient (gradient). Shadows: 2 cascades, around 60 m distance.
- **Height fog plus distance fog**, light near the camera and heavy only beyond the playable area. Never fog the courtyard.
- Post-processing: ACES or Neutral tonemapping, mild bloom (threshold ≥ 1.1, intensity ≤ 0.4) applied to emissive spirit elements only, slight warm white balance, and SSAO at low intensity.
- Time of day: dawn (hero shot), day (default) and night (lanterns plus formation glow). Night must stay readable.
- Motion: vertex wind on foliage, waterfall and stream UV scroll, drifting cloud cards, around 200–400 spirit motes in total, slow lantern sway.

## 6. Characters

- Anime proportions of roughly 6.5–7 heads, slender, with readable silhouettes at gameplay zoom: flowing robes, a hair crown or bun, and a sword on the back for Kiếm Tu.
- Robes are coloured by school: Kiếm Tu teal and white, Đạo Giáo orange and gold, Phật Giáo saffron and white, and so on, matching the existing 2D portraits.
- Minimum animation set: idle, walk, carry, chop/mine, meditate (seated, with a glow).
- In-world characters are small, so silhouette and colour matter more than facial detail. Portraits carry identity in the UI.

## 7. What lighting cannot fix (an explicit non-goal)

The previous pass changed palette, fog and post-processing on top of primitive geometry. That made a prettier prototype, not a different class of art. This direction requires **new modelled and textured assets**: architecture kit, vegetation, terrain and cliffs, and characters. Shaders and post-processing are the last 20%.

## 8. UI direction (Milestones C and D)

- Materials: parchment panel (subtle paper grain), jade lacquer buttons, bronze/gold hairline borders with cloud-scroll (祥云) corner ornaments, and a red seal stamp for primary confirms.
- Hierarchy: a thinner top resource bar (about 64 px at 1080p) with icons instead of uppercase labels. The contextual panel slides from the right and covers at most 30% of the width. The bottom nav uses icon plus label.
- Typography: Noto Serif Bold for titles (Vietnamese-safe). Consider adding a calligraphic display face with Vietnamese diacritics *for headings only* (it must pass a diacritic render test).
- Numbers: vi-VN formatting with no raw floats (`4.320`, `26,7`). This is already done for the HUD and should be kept.
- Disciple identity: unique portrait per disciple, rank frame colour (N/R/SR/SSR), realm progress as a jade bar, and status as a non-button tag in a different shape and colour from actions.

## 9. Acceptance

Real Unity Game View captures at 1600×900 and 1920×1080 for: sect overview, medium architecture, hall close-up, Đệ Tử, a Grand Hall sample, and selection + placement. Before/after pairs are compared side by side, with performance JSON for each.
