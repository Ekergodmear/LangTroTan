# Tàng Kinh Các Level 6: spirit VFX prototype v3 (free tools only), for visual review

**Status:** isolated QA only. Not `approved_art`, not integrated, nothing committed.

**Tools used:**
- Unity URP;
- the built-in Particle System;
- three hand-written URP shaders;
- Python/PIL for contact sheets and GIFs.

No ForgeGUI credits, paid assets or external services were used, and no production scene, save, server or footprint was touched.

**Reused:** the Level 6 v2 model and its texture optimisation (31.5 MB in the editor, down from 55.9 MB), the effects component's Full / Reduced / Off modes, camera-distance LOD, awakening, and the zero-allocation update.

## What changed

| Before (v2) | Now (v3) |
|---|---|
| 4 rigid cyan arcs | **8 thin wisps** (2 per pillar), `SonMonSpiritWisp` shader: vertices displaced by smooth periodic noise so each ribbon sways and curls; streaks flow towards the hall; one soft pulse per loop per wisp; slender, faded ends |
| Neon formation border (bright lines, ticks) | **Ancient jade rune band**: procedurally composed scripture characters with a soft glow and faint carved hairlines, no hard borders. A wide **activation wave** lights it rune by rune; the four corner seals (two counter-rotating rune rings each) light in sequence. Slow breathing swell |
| UI-like glyph tiles with frames | **Small golden scripture characters** (0.13–0.19, max 16) rise from the hall facade between the first and second roofs. They fade in, float with gentle noise sway, then **dissolve into specks** with a warm ember edge (`SonMonSpiritGlyph` shader, driven by particle age) |
| Mist rising at the terrace edges | **Gathering**: jade wisps and motes drift **inwards around the hall** and rise; motes curve in from a ring over the terrace and fade before the walls |

**Seamless loop:** every shader motion is a whole number of repetitions per **10 s** of a shared clock (`_SpiritTime`), and the pillar pulses use 5 s with quarter-period staggers. Particles flow continuously with no resets.

**Measured** on the 100-frame capture (mean pixel difference):

| Frame pair | Difference |
|---|---|
| Adjacent frames 0.1 s apart | 0.35 |
| Frame 0 vs frame 100 (t + 10 s) | **0.29** |
| Frame 0 vs mid-cycle | 1.03 |

The loop point is as smooth as any ordinary frame step; the remaining difference is the free-flowing particles.

## Captures (`unity/`)

| File | Content |
|---|---|
| `cycle_10s_loop.gif` | **one complete 10 s cycle**, 100 frames at 10 fps, 720 × 405 (full-size PNG frames in `cycle/`) |
| `crop_gameplay_zoom10_off_vs_full.png`, `crop_gameplay_zoom21_default_off_vs_full.png` | zoom 10 and 21, off vs full, native size |
| `sheet_modes_gameplay.png` | off / full / reduced at zoom 10, 21, 30 |
| `sheet_modes_closeups.png` | wisps, pillar and formation, seal, characters in each mode |
| `sheet_L5_vs_L6.png` | L5 vs L6 (effects off and full), same camera and lighting |

## Performance (isolated QA player)

**How it was measured:**
- `perf/benchmark.csv` comes from a development Windows build of the isolated QA scene `Assets/QA/L6SpiritQA/L6SpiritQA.unity`. The build lives outside the project in `work/qa_builds/L6SpiritQA`.
- **Hardware:** RX 6600M, Ryzen 7 5800H, 1600 × 900 windowed, D3D12, vSync off with a target frame rate.
- **Method:** 8 s samples after 2 s warm-up for every combination of target, mode and zoom.

| Target | Mode | Zoom | Mean frame (ms) | p99 (ms) | Max (ms) | Std dev (ms) | Frames > 1.5× | GPU (ms) | SetPass | Particles |
|---|---|---|---|---|---|---|---|---|---|---|
| 60 | Off | 10 | 16.67 | 16.67 | 17.07 | 0.02 | 0 | 5.45 | 74 | 0 |
| 60 | Reduced | 10 | 16.67 | 16.68 | 16.84 | 0.01 | 0 | 5.76 | 82 | 8 |
| 60 | **Full** | 10 | 16.67 | 16.68 | 16.72 | 0.00 | 0 | 5.90 | 88 | 28 |
| 60 | Full | 21 | 16.67 | 16.68 | 17.23 | 0.03 | 0 | 5.75 | 89 | 8 |
| 30 | Off | 21 | 33.33 | 33.34 | 33.37 | 0.00 | 0 | 4.82 | 77 | 0 |
| 30 | **Full** | 10 | 33.33 | 33.34 | 33.36 | 0.00 | 0 | 5.11 | 91 | 66 |
| 30 | Full | 21 | 33.33 | 33.34 | 33.37 | 0.00 | 0 | 4.86 | 88 | 8 |

The full table, including memory, is in the CSV.

**Results:**
- **Frame pacing** is perfectly steady at both 30 and 60 FPS in every mode: no frame over 1.5× target, and standard deviation ≤ 0.03 ms.
- **GPU** cost of the full effects is about +0.0–0.5 ms per frame, within run-to-run noise. The frame is 5–6 ms of a 16.7 ms budget.
- **SetPass calls:** +8 (Reduced) to +14–17 (Full) over Off.
- **Memory:**
  - managed memory 4.1–5.0 MB in every phase;
  - **0 GC collections** in every phase;
  - total allocated ≈ 97 MB and graphics driver ≈ 231 MB for the whole development build;
  - Off → Full differs by ≤ 1 MB.
- **Editor checks:** 0 bytes allocated per update (near and far LOD), 0 particles inside the terrace solids, 0 colliders, Ignore Raycast layer.

**Measurement notes:**
- The very first phase (30 FPS / Off / zoom 10) ran at 60 FPS because the frame-rate cap only took effect after it started. Treat that row as a startup artefact.
- The player's "CPU frame time" includes the frame-rate wait, so it isn't an effects cost. The isolated effects update itself measured about 0.015 ms in the editor.
- Draw-call and batch counters read 0 in this D3D12 development player (counter not reported), so SetPass calls are given instead.

**Footprint:** all strips, wisps, mist, motes and characters stay inside the proposed Stage II lot. Only the soft glow billboards on the two front pillar crystals reach ≤ 0.11 past the front lot line, at crystal height.

## For your review
- **Restrained by design:** at zoom 21 the wisps are faint; the pillar glow, formation corner seals and band carry the signature. If you want the wisps to read more at zoom 21, the next step is a higher mid-distance intensity or slightly wider wisps.
- **Character glyphs** are procedural stroke compositions (character-like, not real hanzi).
- **New files** (all isolated):
  - `Assets/QA/L6SpiritQA/` (QA scene, benchmark script, saved QA materials);
  - three shaders in `Assets/Resources/SonMon/Shaders/` (Resources, so they ship if this is integrated later).
- **Pre-existing change:** `ProjectSettings/GraphicsSettings.asset` was already modified before this work; it was not changed for this task.
