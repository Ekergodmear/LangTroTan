# Visual Vertical Slice Plan

Status (corrected 2026-10-27): Milestone A complete. D1 done. **No paid purchase is approved, and none is required for Milestone B.** The earlier line "Milestone B is waiting only on the commercial kit" over-stated the dependency. Milestone B proceeds on the free route (§2a); a paid kit is an optional accelerator, to be judged only after free-route screenshots exist.

## 2a. Asset research, 2026-10-27 (verified sources)

| Option | Publisher / source | Price | Licence | Visual fit for "stylized anime Xianxia sect" | Verdict |
|---|---|---|---|---|---|
| [STYLIZED Far East Kingdom](https://assetstore.unity.com/packages/3d/environments/historic/stylized-far-east-269978) | PHIDEAS (Ramtin Rad) | $60, Single Entity | Unity Asset Store EULA | The closest paid match found. Stylized and hand-painted, palaces/shops/shrines/pagoda/temple, modular walls and gatehouses, trees/cliffs, about 8k polys per building. Mixed Chinese/Japanese "town" theme rather than a mountain sect. v1.0 (Jan 2024), 8 reviews, URP needs material conversion. I could not inspect its screenshots myself (browser pane hidden) | Optional later. **Not required** |
| [Ancient Dragon Temple](https://www.fab.com/listings/1ae6bd57-fea5-4409-b40c-9d042c946291) | DanStudios (Fab) | $79.99 exterior / $69.99 interior | Fab licence | Realistic AAA / dark fantasy, 4K textures, Unreal format listed | **Unsuitable** (style mismatch) |
| Sketchfab free models (12 best CC-BY/CC0 downloadable, contact sheet reviewed) | various authors | Free | CC-BY (attribution) / CC0 | Mostly realistic or photogrammetry, 180k–1.1M tris, inconsistent styles | Not usable as a kit. One or two might serve as decor or modelling reference |
| [Quaternius Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html) | Quaternius | Free (Standard, ~68 of 110+ models) | CC0 | Stylized pine, cherry, rocks, plants; FBX/glTF | **Good fit for vegetation.** Needs your OK to download |
| Poly Haven textures + HDRI | Poly Haven | Free | CC0 | Ground, stone, tile, wood, sky | Already imported (`27febbd`) |
| Blender-authored modular kit | this project | Free | Project-owned | Exactly matches the art direction and the server grid | **Primary route for architecture** |

**What can proceed with no purchase (all of Milestone B):** the Blender modular architecture kit (curved tiled roofs with ridge ornaments, dougong bands, lacquer pillars, lattice walls, railings, stairs, plinths, paifang gate, hall/pagoda/pavilion variants) with Poly Haven materials and a stylized URP shader; terraced terrain, cliffs, stream and waterfall; vegetation (Quaternius CC0 plus Blender bamboo); HDRI sky, lighting and post-processing; formation, crystal and spirit-particle VFX; the prefab factory hooked to server building types; QA and performance capture.

## 1. Decisions needed from you

| # | Decision | Why it blocks |
|---|---|---|
| D1 ✅ approved: done, repo at `e9d7424` | Allow `git init` plus a baseline commit in the project folder (existing `.gitignore`, local only, no push) | Every later change must be reversible and diffable |
| D2 ✅ approved: route D (hybrid) | Choose the 3D asset route (§2) | There are no 3D assets in the project, no Blender, and no Asset Store cache. Downloading or purchasing needs your permission |

## 2. Asset sourcing options

| Route | What it gives | Cost / effort | Quality ceiling | Risk |
|---|---|---|---|---|
| **A. Commercial stylized Chinese kit** (example: *STYLIZED Far East Kingdom*, Unity Asset Store, listed at $60 per seat. Its listing describes temples, pagodas, walls, gatehouses, nature and day/night skyboxes, with a URP material-conversion step) | Complete modular architecture plus nature in one consistent style | Paid, and **you buy it** (I cannot purchase). About 1–2 days to integrate | High, consistent | Must check URP conversion, poly counts and licence seats. The listing says "Unity 2017.1+", so test it in 6000.4 |
| **B. Free CC0 packs + project-authored architecture** (Quaternius nature/RTS, Kenney, Poly Haven CC0 textures and HDRIs) | Vegetation, rocks and textures that are CC0 and safe to ship | Free. Needs your OK for each download. Architecture still needs authoring | Medium. Free packs are mostly low-poly, which is the look you rejected | Style mismatch with Xianxia. Few free CC0 Chinese-architecture models exist (a search today found none confirmed CC0) |
| **C. Blender kit authored by me** (install Blender 4.x LTS from blender.org, then a scripted modular kit: roof, bracket, pillar, railing, stair and gate pieces, plus baked textures) | Exactly fitted to the server grid and to the art direction, with no licence issues | Free. Needs your OK to install Blender. Several days of iteration | Medium–high for architecture. Weaker for organic foliage and characters | My modelling is procedural/scripted. Hand-painted texture quality is limited |
| **D. Hybrid (recommended)** | A for architecture and nature, C for gameplay-specific pieces (formation circle, terraces, spirit landmarks), CC0 Poly Haven HDRI and textures for sky and ground, plus a commercial or CC0 rigged stylized character | A + C costs | Highest achievable in this setup | Needs both approvals |
| E. Keep code-generated meshes | Nothing new | Free | Already rejected by you | Would repeat the previous superficial pass. **Not recommended** |

Characters: none of the routes above solve anime characters well for free. Options are a commercial stylized anime character pack (you choose and buy), or a VRoid Studio export (free tool, anime style, needs install and licence check per model). VRoid would be my recommendation to evaluate. Until then, in-world characters stay placeholders and are labelled as such.

**2D AI images will not be presented as 3D assets.** The existing 2D portraits and paintings stay in the UI and concept roles only.

## 3. Milestones

### Milestone B — Sơn Môn environment (after D1 + D2)
1. Import the chosen kit into `Assets/ThirdParty/<vendor>/` (untouched) and record the licence and provenance in `ASSET_SOURCES.md`.
2. Author `Assets/Art/SonMon/` prefabs: one prefab per server building `type` (21 types in `SpatialCatalog.json`), each with LOD0–2, a collider and a pivot at the footprint centre.
3. Add `PrefabBuildingFactory`: look up the prefab by `type`, with fallback to the existing `SliceArt.Building` so no type is ever invisible. **The server contract, coordinates and footprints stay unchanged.**
4. Build an authored scene layer, `SonMon_Environment` (additive scene or prefab): terraces, cliffs, stream, bridge, gate, distant peaks and vegetation clusters. Vegetation uses GPU instancing, placed by hand and deliberately kept off the build grid.
5. Lighting volume per §5 of the art direction: three time-of-day presets, tied to the existing server day/night state.
6. Hero landmarks: Chánh Điện (hero detail), Tụ Linh Trận formation (emissive animated decal), waterfall, floating spirit rock.
7. Budgets: ≤ 300k visible triangles at gameplay zoom, ≤ 250 draw calls/SRP batches, ≤ 2 GB VRAM, 1080p frame ≤ 8.3 ms (120 fps) in a dev player on the RX 6600M. All are measured, not estimated.

### Milestone C — Main HUD
Thinner resource bar with icons, merged info card, ornamented bronze/parchment skin, contextual right panel ≤ 30% width. Keep every existing button and its binding.

### Milestone D — Đệ Tử screen
Unique portrait per disciple (from the expanded portrait pool, assigned deterministically by disciple id), rank frame, realm bar, status tag visually distinct from actions (this also fixes the "Tu luyện" ambiguity), and contextual actions driven by the real server actions.

### Milestone E — Grand Hall sample
Recommended: **Tông Phong**. It is the richest data (loyalty, influence, Phong Chủ, petitions, budget) and is currently flattened by the generic projection. Build one specialised native layout from real snapshot fields through a read-only contract, the same pattern as Bảo Khố. Keep the existing allocation action flow.

## 4. Pre-work that doesn't need D2 (can start right after D1)
- Fix the failing `-visual-regression` (status chip versus action button), without weakening the assertion.
- Re-run the full regression plus `-full-port-smoke` against the isolated servers, and record the results in `VISUAL_VERTICAL_SLICE_QA.md`.
- Add per-frame draw-call / SRP-batch capture to the profiler JSON (it currently records `drawCalls.samples: 0`).

## 5. Guardrails
- Isolated QA only: ports 8899/27028 and 8901/27029. Never 27017. Never load `cxi_demo_ui` or `game_mv3bzlj8`.
- Do not modify the original `tu-tien-game` repository.
- Keep the APK gate locked.
- New code is readable, not code-golf, and lives in new files where possible.
