# Sect architecture: ForgeGUI → Unity production plan (60 models)

Status (2026-10-28): **approved architecture loaded; no-credit dry-run passed. Nothing generated, no credits spent, Unity scene and gameplay untouched.** Stopped before the first paid generation.

| Source | File |
|---|---|
| Approved architecture (verbatim) | `docs/APPROVED_ARCHITECTURE_SPEC.md` |
| Art Bible (style, ladders, effects, review) | `docs/ART_BIBLE.md` |
| Manifest (60 entries, single source of truth) | `Tools/ForgeGUI/asset_manifest.json` |
| Scaffold / spec import | `Tools/ForgeGUI/build_manifest.py`, `Tools/ForgeGUI/apply_spec.py` |
| Validator and five-view previews | `Tools/ForgeGUI/validate_glb.py` |
| Integration findings | `docs/FORGEGUI_INTEGRATION.md` |

## 1. Manifest: all 60 entries populated

- 6 families × 10 levels. Every entry has `approvedArchitectureDescription` copied verbatim from the spec (the script asserts each string appears exactly in the spec), plus `levelName`, `architectureTheme`, `animationTheme` and `specStatus: approved_2026-10-28`.
- Every entry has a composed `generationPrompt` (969–1,210 characters):
  - `style` adds materials and palette only;
  - then the family theme, `level N of 10`, the level name and the approved description verbatim;
  - then the footprint line and technical suffix (static, entrance front, ground base, no particles, floating objects, characters, terrain or text).
- One production remark ("The existing ForgeGUI model is a candidate for this level.") stays in the description but is excluded from the prompt.
- Production fields are present and empty until used: `forgeJobId`, `creditsUsed`, `sourceGLB`, `sha256`, `convertedFBX`, `textureFiles`, `triangleCount`, `bounds`, `footprintValidation`, `visualReviewStatus`, `unityPrefabPath`, `ambientVfxProfile`, `upgradeVfxProfile`, `visualKey`.
- Statuses: 59 × `planned`, and `scripture_L05` is `candidate` (existing ForgeGUI model, with its original ForgeGUI prompt recorded under `existingCandidate`).

## 2. Contradictions with server data

| # | Finding | Impact | Proposal |
|---|---|---|---|
| 1 | Server IDs and footprints match the spec for all six families (hall 4×4, spiritmine 2×2, others 3×3). | none | — |
| 2 | Diễn Võ Trường's core building uses spatial type `barracks`, which is also the player-buildable "Doanh Trại Hộ Vệ". Unity resolves level visuals **by type**. | The arena art would also appear on player-built guard barracks, which the spec forbids. | Presentation-only change: resolve the visual family by `coreId` first (`visualKey = core:<id>`), so player-built types keep their kit art. No gameplay or save change. **Needs your OK to implement.** |
| 3 | The same type-sharing applies to player-built `scripture`, `alchemy`, `forge` and `spiritmine`. | Player-built copies would show the core family's art at their own level. | Same fix. Should player-built copies use the core family or their own kit art? Default proposal: their own kit art. |
| 4 | Display names differ: the spatial catalog calls `barracks` "Doanh Trại Hộ Vệ" and `spiritmine` "Linh Mỏ"; the server's core names are "Diễn Võ Trường" and "Linh Điền & Linh Mỏ". | The upgrade-complete toast (added in the last milestone) uses the catalog name, so the core Diễn Võ Trường would announce "Doanh Trại Hộ Vệ đã đạt Cấp N". | Use `sectBuildings.BUILDINGS[coreId].name` for core buildings. Small presentation fix, bundled with #2. |
| 5 | The existing L5 vs approved L5: three storeys, three roof layers, columns, white terrace, grand staircase, gold ridges, jade medallion and lanterns all match. | **The top finial is a gold spike with a jade gem, not a spirit-beast finial.** Its original prompt asked for "glowing cyan spiritual crystals" (glow belongs in VFX). One side lantern looks close to floating. | Keep as candidate. Decide later whether to regenerate L5 from the approved prompt and compare. |

## 3. Footprint risks and proposed adaptations (for approval)

1. **Linh Điền & Linh Mỏ, 2 × 2, ten levels.** Fixed zoning on every level, so continuity stays readable:
   - The **mine gate** is carved into a small rock outcrop along the back edge, mouth facing front.
   - **Herb terraces** are stepped in the front-left quadrant.
   - The **spring or well** sits front-right.
   - Paths run along the centre line.
   - Growth is vertical: from L5, terraces stack up the outcrop's flank; L6–L10 towers and altars sit on top of the outcrop (behind the terraces), never in front of the entrance.
   - Height ladder 1.3 → 3.2; eave or terrace overhang at most 10%.
2. **Diễn Võ Trường, 3 × 3.** The central arena holds about 60% of the plot. L3–L10 additions go on the edges: banner pillars and sword towers on the four corners (inside the footprint), spectator tiers along left and right, the instruction hall along the back edge, the gate front-centre. Height comes from towers and the hall (1.4 → 4.6), keeping the arena open from the camera.
3. **Tàng Kinh Các L7–L10 side towers and bridges, 3 × 3.** The main pagoda stays in the centre third; side towers are attached flush to the left and right walls; bridges stay within the eave line. Total width is at most 3.6 (20% eave rule); growth goes into height (4.65 → 4.95).
4. **Luyện Đan Đường L8/L10 nine fire positions or pillars, 3 × 3.** A ring of nine slender pillars on the terrace edge, inside the plinth, around the central cauldron platform.
5. **Chánh Điện dominance.** The hall ladder of 5.2 → 7.6 makes hall L1 taller than any other family's L10, and the 4 × 4 footprint makes it the widest. Proposed in Art Bible §2; please approve or set different rungs.

## 4. Tàng Kinh Các prompts (from the approved spec)

The shared parts, identical for every level and family, are the materials-and-palette style block (which never adds structure) and the technical suffix; they are in `asset_manifest.json → promptTemplate`. Level-specific part:

- **L1 (1,084 characters):** `Tàng Kinh Các — Scripture Pavilion (ancient scripture library, layered pagoda roofs, elegant scholarly architecture), level 1 of 10, "Tàng Thư Tiểu Các": One-story library pavilion, one curved jade-teal roof, four main red timber pillars, low ivory stone platform, three front steps, double wooden doors, two lattice windows, wooden signboard and two small lanterns. Compact layout for a 3x3 square game footprint.`
- **L5:** the same pattern with "Linh Ngọc Tàng Kinh Các" and the approved L5 text (without the candidate remark).
- **L10 (longest, 1,210 characters):** the same pattern with "Thái Thượng Tàng Kinh Tiên Cung" and the approved L10 text.

## 5. Dry-run (no credits): full prompt retained

In your signed-in ForgeGUI, Text to 3D Assets:
- 3D Model, Mesh Model Auto, Style **Anime**, Automatically Rig **off**, 1 of 1 generations.
- The full approved L1 prompt was entered as keystrokes; **both the start and the end of the 1,084 characters are present in the box.**
- A programmatic fill did not stick (the page kept its previous text), so production entry must type the prompt and then verify both ends.
- ForgeGUI also accepted the original ~1,170-character L5 prompt earlier, so the 1,210-character L10 is within proven length.
- **Create Now was not clicked; the balance is unchanged at 354,891.**

Evidence is in `Reports/ForgeGUI/dry-run-spec-L01/`: prompt start, prompt end with rig off, the model picker, the asset card controls and an overview.

## 6. Exact ForgeGUI controls (Text to 3D Assets, `/studio?tab=3d`)

| Area | Control | Setting for production |
|---|---|---|
| Generation mode | **3D Model** / Asset Pack | 3D Model |
| Mesh Model | Picker: **Auto (ForgeGUI default), Meshy 7.1, Tripo P1, Tripo v2.5, Tripo v3.1**; filters by provider and feature. No costs shown | Auto (the L5 candidate used Auto). Switching models changes the look across a family, so pick one per family and keep it |
| References | Image upload (click or drag-and-drop) | Previous approved level's front and 45° render, plus the family reference pack |
| Style | Presets Cartoon / Realistic / **Anime**, plus "Add a style reference" | Anime |
| 3D Properties | **Polygons** slider (Auto), **Faces** slider (remesher target), **Automatically Rig** switch | Polygons Auto; rig **off** |
| Prompt | Text box plus suggestion chips (Low poly, Stylized, Treasure chest, Magic sword, Game-ready) | Composed prompt, typed and both ends verified; chips unused |
| Count | − / + generations ("Each request makes one 3D model") | **1** |
| Generate | **Create Now** (the expanded asset view has its own "Create" too) | Only after your approval |
| Results | Generated Assets list: per asset **Download**, **Expand** (shows the original prompt), **More actions (⋯)**; panel **Download All**; tile size slider | Download the single new asset only |
| Account | Pro Plan, credit balance in the user menu | Read before and after each generation |

Not inspected, to avoid side effects: the formats offered by Download (the docs say 3D downloads as `.glb`) and the ⋯ menu contents. Both get checked on the first approved download.

## 7. Credits

There is no published or displayed per-generation price. The plan is to measure it on the first approved generation (balance before minus balance after). The projection is 9–10 generations for Tàng Kinh Các, and about 59 for all families, ×1.5–2 for retries. **I stop after the first generation to report the measured cost before any second generation.**

## 8. First paid generation (awaiting approval)

`scripture_L01`, 1 model, settings as in §6, approved L1 prompt.

Then: download the GLB to `work/forgegui/downloads/` (sha256 recorded), validate, render five views, compare with the spec and Art Bible, record the result as `candidate`, report the credits used, and stop.

## 9. World-scale rollout (decision 2026-10-28: option A)

- Standards: door **0.52**, characters **0.85**, footprints unchanged. Infrastructure: `SonMonWorldScale` feature flag, default **OFF**.
- **Mixed-scale limitation:** only Tàng Kinh Các is door-normalised; the kit buildings of the other five families and the environment props remain at kit scale (door ≈ 0.72). The flag stays off until all six core families have door-measured models. Kit buildings and props are not rescaled.
- Baselines to preserve: **126 PASS with the flag on**, **114 PASS with it off** (current production appearance).
- Status: Tàng Kinh Các L1 **approved_with_notes**; L5 **candidate**.
- Every new level must ship with a `MEASUREMENTS.json` (measured door and source height) so the builder can normalise it to 0.52.

