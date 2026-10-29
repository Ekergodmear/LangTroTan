# Tàng Kinh Các Level 6: spirit effects v2 and texture memory fix

**Status:** isolated QA only. Not `approved_art`, not integrated, nothing committed.

**Safeguards:**
- No credits spent.
- Original ForgeGUI GLB unchanged (sha256 `0d31ded5…`).
- No changes to server, saves, footprints or production scenes.

## Inspection first
- **Reused:** the existing `SonMonSpiritGathering` (modes, distance LOD, awakening, pillar glow, motes, ornament gathering, zero-allocation updates) and the isolated `StageSiteCapture` QA harness.
- **Replaced:** only the weak parts: the ground outline, the trails, and the old mist.
- **Packages:** URP 17.4 and Shader Graph are installed; **VFX Graph is not installed**. I wrote one small hand-written URP shader instead of adding a package.
- **ForgeGUI:** it can't export VFX or prop animation (see `forgegui_proposal/PROPOSAL.md`). Nothing was generated.

## What's new

| Effect | Implementation |
|---|---|
| Formation | Rune-inscribed band: border lines, ticks, and glyphs that scroll ("rotating symbols"). Sequential activation pulses travel along it. It embraces the terrace sides and back inside the proposed Stage II lot; the front stays open for the entrance. **Four corner seals**, each with three concentric counter-rotating rune rings |
| Energy streams | Four curved semi-transparent ribbons (crossed horizontal + vertical, slender ends) from the jade pillar caps arcing into the hall's front and back facades. Slow flowing streaks, an occasional brighter pulse per stream (out of phase), soft fade at both ends |
| Scripture glyphs | 12 max, 0.34–0.46 size, bold gold and cyan glyphs with a thin seal frame. They orbit in front of the second-floor facade and fade through the wall behind; the roof silhouette stays clear |
| Spirit mist | Sparse thin jade wisps (≤ 14) rising from the exposed terrace tops and the front landing; no ground fog |
| Kept | Pillar crystal cores and halos, rising motes, ornament gathering, awakening sequence, Full / Reduced / Off |

**Implementation:**
- **Shader:** strips use `Assets/Resources/SonMon/Shaders/SonMonSpiritFlow.shader` (URP unlit, additive, SRP-batcher compatible). All motion comes from `_Time` on the GPU, so the CPU writes nothing per frame; intensity changes only on mode or LOD changes.
- **Draw cost:** formation bands = 1 mesh, seals = 1 mesh, streams = 1 mesh.

**Readability by zoom:**
- **10:** everything.
- **21:** formation and seals driven harder, streams, mist, pillar glow; no glyphs or motes.
- **30:** formation, seals and pillar glow only.

## Texture memory

The building now keeps its **original 2048 textures untouched** (new provenance benefit), and the terrace gets its own small set. The 4096 × 2048 doubled atlas is gone.

| Texture | Before (4096 × 2048 atlas) | v2 |
|---|---|---|
| Albedo | 4096 × 2048 | 2048² **BC7** (original, full quality) |
| Normal | 4096 × 2048 uncompressed | 2048² **BC5** (original) |
| Metallic / smoothness | 4096 × 2048 uncompressed | 1024² BC7 |
| Terrace albedo | (in atlas) | 2048² DXT1 |
| Terrace mask | (in atlas) | 512² |
| **Total, Unity editor measurement** | **55.9 MB** | **31.5 MB** (−44%) |

The editor figure includes the CPU-side copy. The runtime GPU estimate is about half: **≈ 16 MB** (was ≈ 28 MB). The roof, carved stairs and entrance use the untouched full-resolution albedo and normal.

## QA (measured in the isolated scene unless marked *estimate*)

| Item | Result |
|---|---|
| Triangles | 48,022 (budget 60k); VFX strips add ≈ 1.9k (*estimate* from mesh sizes) |
| Particles, full | 43 at zoom 10 · 20 at zoom 21 · 8 at zoom 30 |
| Particles, reduced | 8 |
| Active VFX renderers (≈ draw calls, *estimate*) | full 9 / 6 / 4 · reduced 4 · off 0 (old v1: 10 / 8 / 3) |
| GC allocations per frame | 0 bytes (near and far LOD probes) |
| CPU per update | 0.27 ms in the editor (upper bound, includes manual particle simulation) |
| VFX texture memory | 1.3 MB, 6 small procedural textures (was 5.9 MB) |
| Colliders / selection | 0 colliders, Ignore Raycast layer |
| Particles inside terrace solids | 0 of 35 |
| Footprint | all strips, mist, motes and glyphs inside the proposed Stage II lot (x −12.98…−4.02, z 4.03…10.58 vs lot −13…−4, 4…11). **The pillar-glow billboards** on the two front crystals overhang the front lot line by up to 0.08 at 2.2 m height |
| Overdraw | thin strips; mist ≤ 14 small sprites at alpha 0.16 |
| Off / Reduced | off: nothing built; reduced: static faint formation and seals plus steady pillar glow only |

## Renders (`unity/`)

| File | Content |
|---|---|
| `sheet_L5_vs_L6.png` | L5, L6 effects off, L6 full: front, 45°, side, top |
| `sheet_modes_gameplay.png` | off / full / reduced at zoom 10, 21, 30 |
| `crop_gameplay_zoom21_default_off_vs_full.png` | zoom 21 at native size, off vs full |
| `sheet_modes_closeups.png` | streams, pillar + formation, formation seal, roof glyphs in each mode |
| `sheet_motion_frames.png`, `motion_streams.gif`, `motion_pillar.gif` | frames 0.75 s apart (measured frame-to-frame change, so the motion is real) |
| `sheet_awakening.png`, `awakening.gif` | L5 → L6 awakening (forced in QA) |

## Known issues
1. **Zoom 30:** only the pillar caps and a faint band read; this is intended ("low-cost signature").
2. **Pillar-glow billboards** overhang the front lot line by up to 0.08 (purely visual, at roof-line height). Shrinking the front halos would fix it at some loss of readability.
3. **Stream end points** enter the hall facade; the end fade hides the join, but from steep side angles a stream can pass close to a wing roof edge.
4. **Shader inclusion:** the shader is in `Resources`, so it's included in builds. Kit integration (assigning L6 and attaching the component) still needs your approval, so the awakening hasn't been tested live.
5. **Glyph shapes** are stylised procedural strokes, not real calligraphy.
