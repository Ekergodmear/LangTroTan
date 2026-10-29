# Tàng Kinh Các Level 6: spirit VFX v4 (Particle System + Shader Graph), for visual review

**Status:** isolated QA only. Not `approved_art`, not integrated into production. Awaiting your visual approval.

**Tools used:**
- Unity 6000.4.3f1 with URP 17.4;
- the built-in Particle System (Shuriken);
- Shader Graph 17.4;
- C# orchestration;
- Python (numpy/Pillow) for procedural textures and contact sheets.

No VFX Graph, paid assets, ForgeGUI credits or external services were used. Production scenes, saves, the server, gameplay data, footprints, the L6 mesh, the GLB and provenance are all untouched. The world-scale flag stays off.

## Deliverables

| What | Where |
|---|---|
| **Shader Graph assets** (real `.shadergraph` files, compile with 0 errors and 0 warnings) | `Assets/Art/SonMon/VFX/L6/Shaders/` |
| Shader Graph generator and import check | `Tools/ShaderGraph/sg_build.py`, `Assets/Editor/SpiritShaderGraphCheck.cs` |
| Procedural textures (5, BC7, 1.55 MB in total) | `Assets/Art/SonMon/VFX/L6/Textures/`, made by `Tools/ShaderGraph/vfx_textures.py` |
| **Particle System prefabs** (7) and materials (7) | `Assets/Resources/SonMon/VFX/L6/`, made by `Assets/Editor/SpiritV4Builder.cs` |
| **C# orchestration** | `Assets/Scripts/Presentation/SonMon/SonMonSpiritGatheringV4.cs` |
| QA harness | `StageSiteCapture.RunL6SpiritV4` (editor), `Assets/QA/L6SpiritQA/` (isolated player scene and benchmark) |
| Captures | `unity/`, `motion_gif/` (player motion GIFs; frame folders are not in git) |
| Performance | `perf/benchmark_v4.csv`, `perf/benchmark_v3.csv` |

### Shader Graphs
- **SG_SpiritMist** (alpha blend):
  - soft alpha with UV distortion from scrolling gradient noise;
  - jade emission boost;
  - circular edge fade per flipbook cell, so no texture squares show;
  - 4×4 flipbook with a random start frame.
- **SG_SpiritFormation** (additive):
  - rune mask;
  - `_Activation` sweeps rune by rune: the threshold comes from the rune position along the band or the seal order, via vertex colour;
  - `_BaseLevel` / `_LitLevel`;
  - noise dissolve;
  - no rectangular outline (faint hairlines only).
- **SG_ScriptureGlyph** (additive):
  - gold-to-jade characters with a glow;
  - fades in, then dissolves by particle age with an ember edge (custom vertex streams: age and stable random);
  - no frames or circles.

### Particle systems (they replace the v3 ribbons and wisps completely)

| Prefab | Content |
|---|---|
| `PS_SpiritMist` | soft jade-cyan energy clouds with Noise, Size and Color over Lifetime, and slow rotation. Two kinds: ambient clouds over the open terrace, and gathering clouds that drift from a front arc towards the jade ornament |
| `PS_JadeMotes` | motes rising by the pillars; during the gathering phase they curve in towards the ornament and fade before reaching it |
| `PS_GoldenMotes` | sparse golden motes, plus an outward dispersal at release |
| `PS_PillarGlow`, `PS_OrnamentGlow` | static soft glows driven each frame; their pivot is shifted towards the camera so the opaque crystal doesn't hide them |
| `PS_ReleasePulse` | one flat jade-gold ring per cycle |
| `PS_ScriptureGlyphs` | golden characters rising from the hall facade |

No trails or line renderers are used.

## The 10 s cycle (seamless)

| Time | Phase | What happens |
|---|---|---|
| 0–3 s | Ambient | mist circulates over the terrace, motes rise by the pillars, the formation rests at its base level |
| 3–6 s | Gathering | the formation activates rune by rune and the four corner seals light in sequence; mist and jade motes drift to the ornament |
| 6–8 s | Convergence | the ornament glow swells softly; the peak is low-alpha, with no flash |
| 8–10 s | Release | one soft ring spreads over the terrace (2 s life, ends at 10 s), golden motes disperse, everything settles back to ambient |

- **No discontinuities:** every driven value is a continuous periodic function of the phase. The formation activation resets while the lit level is 0, so the reset can't be seen.
- **Fades on every particle:** each one fades in and out over its life, so nothing pops.
- **Measured smoothness** (`unity/cycle/`, 101 frames at 10 FPS, hero camera):
  - median frame-to-frame difference 0.10, largest step 2.6× the median, so there are no harsh transitions;
  - frame 100 (t + 10 s) differs from frame 0 by 0.95, about the same as mid-cycle versus frame 0 (0.83).

  That difference is the free-flowing particles being in new random positions. In game, time runs on continuously and nothing resets. In the looping GIF, however, the wrap point shows the particles reposition.

## Readability by zoom and mode (`unity/sheet_modes_*.png`, `sheet_phases_*.png`)

**Distance LOD:** mid starts at 42 units (zoom 21), far at 70 (zoom 30).

| | Full | Reduced | Off |
|---|---|---|---|
| **Zoom 10** | everything: mist, gathering, motes, glyphs, formation, glows, release | faint static formation, steady pillar glow, a little mist | nothing is built |
| **Zoom 21** | fewer, larger and softer mist shapes, stronger formation and ornament/pillar glow, release ring; no glyphs or motes | as at zoom 10 | nothing |
| **Zoom 30** | formation and glows only; every particle system stops emitting | formation and pillar glow | nothing |

- **Zoom 21, Full vs Off:** the difference is noticeable: pillar glows, ornament glow, corner seals and soft mist. Brightness isn't pushed.
- **Disabled systems stop simulating:** switching LOD stops emission, live particles fade out over their lifetime, and Off builds nothing.

**Particle counts** (editor and player):

| | Zoom 10 | Zoom 21 | Zoom 30 |
|---|---|---|---|
| Full | 50–69 | 21–29 | 14 |
| Reduced | 7–8 | 7–8 | 4 |

**Estimated particle overdraw** (summed transparent quad area divided by screen area):

| Zoom 10 | Zoom 21 | Zoom 30 |
|---|---|---|
| 0.018 | 0.004 | 0.001 |

The largest single quad covers 0.2% of the screen.

## Technical checks (editor, `unity/measurements.csv`)
- **Placement:** effects are separate child objects of the building root.
- **Colliders:** 0.
- **Layer:** Ignore Raycast on every object.
- **Formation properties:** set through a MaterialPropertyBlock.
- **Materials:** 7 shared; 3 shaders.
- **Allocations:** 0 bytes in the orchestrator update across 600 steps (two full cycles, including releases) at near, mid and far LOD.
- **Script cost:** 0.003 ms per update in the editor.
- **Clipping:** 6 of 760 particle samples had their centre inside the terrace or hall solids over a full cycle. Before the fixes in this pass it was 311.
- **Footprint:** everything stays inside the proposed Stage II lot except:
  - the pillar-glow halos, which reach 0.13 past the front lot line at crystal height (the front pillars stand on the line; the same exception as v3);
  - mist quads, which touch the left lot edge by 0.03 at their full quad corner (the texture is already transparent there).
- **Gating:** `-visual-tiers` (`SonMonTiers.Enabled`); `-no-ambient-fx` gives Off; Reduced follows `SonMonUpgradeFx.ReducedEffects`.
- **Upgrade animation:**
  - the L5 → L6 awakening plays only right after a server-confirmed upgrade swap (`SonMonUpgradeFx` with `ToLevel == 6 && Swapped`), never on load;
  - a plain build reports `awakenOnPlainBuild = False`;
  - the awakening frames are in `unity/sheet_awakening.png`.

## Captures
| File | Content |
|---|---|
| `unity/sheet_v3_vs_v4.png` | v3 vs v4 from identical cameras (hero, 45°, formation, ornament) |
| `unity/sheet_modes_gameplay_zoom10/21/30.png` | Off / Reduced / Full / v3 at each gameplay zoom |
| `unity/sheet_closeups_v4.png` | mist, formation, glyphs, ornament, pillar with formation, reduced formation |
| `unity/sheet_phases_hero.png`, `sheet_phases_gameplay_zoom10.png`, `sheet_phases_gameplay_zoom21.png` | ambient, gathering, convergence, release |
| `unity/cycle_10s_hero.gif`, `unity/cycle_10s_zoom21.gif` | one complete 10 s cycle (the frame folders are not in git) |
| `motion_gif/v4_{zoom10,hero}_{30,60}fps.gif` | 3 s player captures at a fixed 30 and 60 FPS step (`Time.captureFramerate`) |
| `unity/v3_*.png`, `unity/v4_{off,reduced,full}_*.png` | full-resolution single shots |

**Player motion captures:** median step 0.001–0.03 and largest step under 4× the median, except one isolated step at zoom 10 / 30 FPS (0.012 absolute, a glyph spawning). No flicker, popping, texture squares or wire-like trails were found.

## Performance (isolated Windows QA player)

**How it was measured:**
- **Build:** development build of `Assets/QA/L6SpiritQA/L6SpiritQA.unity`, stored outside the project in `work/qa_builds/L6SpiritQA_v4`.
- **Hardware:** RX 6600M, Ryzen 7 5800H, 1600 × 900 windowed, D3D12, vSync off.
- **Method:** 2 s warm-up and 8 s sample per row. v3 and v4 ran in the same build, one after the other.

| Version | Target | Mode | Zoom | Mean (ms) | p95 (ms) | p99 (ms) | Max (ms) | Std dev (ms) | Frames > 1.5× | GPU (ms) | SetPass | Particles |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| v4 | 60 | Off | 10 | 16.67 | 16.67 | 16.68 | 16.82 | 0.01 | 0 | 1.13 | 10 | 0 |
| v4 | 60 | Reduced | 10 | 16.67 | 16.67 | 16.68 | 16.68 | 0.00 | 0 | 1.13 | 14 | 8 |
| v4 | 60 | **Full** | 10 | 16.67 | 16.67 | 16.67 | 16.82 | 0.01 | 0 | 1.14 | 18.5 | 68 |
| v4 | 60 | Full | 21 | 16.67 | 16.67 | 16.68 | 16.91 | 0.01 | 0 | 0.93 | 14 | 21 |
| v4 | 30 | **Full** | 10 | 33.33 | 33.34 | 33.34 | 33.41 | 0.01 | 0 | 1.14 | 18 | 69 |
| v4 | 30 | Full | 21 | 33.33 | 33.34 | 33.34 | 33.36 | 0.00 | 0 | 0.93 | 14 | 21 |
| v4 | uncapped | Off | 10 | 1.91 | 3.24 | 3.51 | 6.28 | 0.98 | — | 0.71 | 10 | 0 |
| v4 | uncapped | Reduced | 10 | 2.25 | 3.74 | 4.56 | 43.35 | 1.41 | — | 1.02 | 14 | 8 |
| v4 | uncapped | **Full** | 10 | 2.05 | 3.31 | 3.64 | 6.51 | 1.06 | — | 0.94 | 18.3 | 67 |
| v4 | uncapped | Full | 21 | 1.99 | 3.17 | 3.55 | 6.27 | 1.06 | — | 0.86 | 14 | 21 |
| v3 | 60 | Full | 10 | 16.67 | 16.67 | 16.69 | 17.07 | 0.02 | 0 | 1.15 | 17 | 27 |
| v3 | uncapped | Full | 10 | 2.16 | 3.64 | 4.43 | 5.29 | 1.13 | — | 0.94 | 16.9 | 26 |

The full tables (all modes and zooms, CPU, GC, memory) are in the two CSVs.

**Results:**
- **Frame pacing:** perfectly steady at 30 and 60 FPS in every mode and zoom: no frame over 1.5× target, standard deviation ≤ 0.03 ms.
- **Uncapped:** about 450–520 FPS in every mode. Full costs no measurable time over Off (mean 2.05 vs 1.91 ms, inside run-to-run noise).
  - The single 43 ms spike (Reduced, zoom 10) was the first frames after the mode rebuild.
  - "Frames > 1.5×" is not meaningful at a 2 ms frame time, so it is shown as "—".
- **GPU:** Full adds about 0.01–0.2 ms over Off. v4 costs the same as v3 while drawing about 2.5× more particles.
- **SetPass:** Off 10 → Reduced 14 → Full 18–18.5 at zoom 10 and 14 at zoom 21 (v3: 17 / 14.8).
- **Texture memory:** 61.8 MB for the whole QA scene, including the L6 model. The VFX textures are 1.55 MB (BC7).
- **Managed memory:** 2–2.6 MB.

**GC allocations:** the harness, not the effects, allocates about 1.8 KB per frame in every mode, Off included. That causes 1–3 collections per 8 s at a capped frame rate. The effects update itself allocates 0 bytes (editor measurement). v4 rows show about 300 B per frame more than v3 in every mode, Off included, because the benchmark looks up the version flag every frame.

**Measurement notes:**
- The player's CPU time includes the frame-rate wait when capped.
- Draw-call and batch counters read 0 in this D3D12 development player, so SetPass is given instead.
- **Correction to the v3 report:** the earlier v3 player numbers (`spirit_v3/perf`) were measured while the game's own UI was also booting in the QA player (`Game.Boot` runs after any scene loads). The QA scene now contains a disabled `FullPortView` placeholder that stops the game from booting there; no production code was changed. That boot couldn't connect to a server, and no save was loaded. The v3 rows in this report were re-measured in the corrected player.

## Known issues
1. **Release ring:** it is subtle on the pale stone. The release mostly reads through the ornament glow fading and the golden motes dispersing. A warmer, more opaque ring is easy to try if you want it stronger.
2. **No soft-particle depth fade** in the Shader Graphs. Mist is emitted above the surfaces to avoid hard intersections, but a cloud that drifts into a wall can show a soft edge line up close.
3. **Mist flipbook cells** are still somewhat round swirls in close-ups. At gameplay zooms they read as soft clouds.
4. **The scripture glyphs and runes** are procedural stroke compositions (character-like, not real hanzi).
5. **The Shader Graphs are generated by a script** from Unity's own graph templates. They open and edit normally in the Shader Graph editor; regenerating them overwrites manual edits.
6. **Footprint and clipping exceptions** are listed under Technical checks.

**Stopped here for your visual approval.**
