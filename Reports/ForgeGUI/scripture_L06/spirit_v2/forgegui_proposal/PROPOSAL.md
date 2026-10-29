# Level 6 spirit effects: ForgeGUI proposal (nothing generated, no credits spent)

## What ForgeGUI can actually provide (checked with the free `asset_capabilities` call)

**Available:**
- Text-to-3D **static** GLB models (one model per generation, rig off for props);
- image generation;
- GLB / texture preparation routes.

**Not available or not useful here:**
- No VFX export of any kind (particles, shaders, Shader Graph / VFX Graph).
- No non-humanoid animation intake: `prepare.animation.*` and `upload.roblox_animation` are unavailable.
- Mesh-UV texture validation is unavailable.

**Conclusion:** everything animated (formation rotation and activation, streams, glyphs, mist, pulses) has to be Unity. It is now implemented with the `SonMon/SpiritFlow` URP shader and particle systems, at zero ForgeGUI cost.

## Candidate ForgeGUI objects (static meshes only)

| # | Object | Value at gameplay zoom | Recommendation |
|---|---|---|---|
| A | Four carved jade formation node stones (one per corner seal, ~0.25 m) | Low: 3–5 px at zoom 21; the procedural seals already read | **Not needed** |
| B | Small cluster of three floating spirit crystals per pillar (separate prop, animated by Unity) | Medium at zoom 10, invisible at 21+ | Optional, later |
| C | Ornate jade rune medallion for the formation seals | Low: flat ground decals do this better and cheaper | **Not needed** |

**Cost of any ForgeGUI prop:**
- ForgeGUI exports 3 × 2048 textures (about 16 MB GPU when compressed), which would undo much of this round's texture savings. Any approved prop would have to be downsized to 512 textures.
- Measured cost per generation so far: **4,467–5,501 credits**.

## Prepared prompt (only if you want option B; no reference attached by default)

> Premium stylized anime Xianxia game prop, a small cluster of three faceted spirit crystals in translucent turquoise-jade, each a tall eight-sided bipyramid with sharp clean facets, the largest about twice the size of the others, held together by a delicate restrained antique-gold filigree ring at their waist, no base, no stand, no particles, no glow effects baked in, no text, symmetrical, clean geometry, game-ready low-poly, single object centred.

**Settings:** Text to 3D Assets → 3D Model, Mesh Model Auto, Anime, rig off, polygons Auto, 1 generation.

**Reference images prepared in this folder** (only if you want stronger style matching; in earlier levels, references tended to copy shapes):
- `ref_pillar_jade_cap.png`: the existing jade cap material and proportions;
- `ref_round_jade_ornament.png`: the hall's round jade screen (palette);
- `ref_formation_seal_current.png`: the current procedural seal.

**Recommendation:** don't spend credits on Level 6 effects. Revisit B for Level 7–8 if floating artifacts become part of those signatures.
