# Xianxia Creature & Character Direction

Status: **only the White Jade Spirit Deer and one disciple prototype are implemented, and both await your visual approval.** No combat, taming, gameplay or server mechanics are planned in this scope. Creatures are visual replacements and scenery.

## Shared rules

- **Style:** stylized anime-fantasy. Clean silhouettes readable at gameplay zoom, soft painted materials, restrained glow (jade / ivory / gold palette from `XIANXIA_ART_DIRECTION.md`).
- **Build method (proven on the deer):** named skeleton graph → Blender Skin modifier → Subdivision → automatic weights on a real armature. Accessories are separate meshes weighted to bones. Glowing details (antlers, markings) are separate emissive meshes; surface markings are shrink-wrapped ribbons. Generator: `Tools/Blender/sonmon_creatures.py`.
- **Motion:** bone-driven procedural idle in Unity (`SonMonPuppet`), applied about the creature's own axes. Clips can be authored later if a creature needs complex actions.
- **Budgets per creature:** ≤ 10k triangles at LOD0, ≤ 25 bones, ≤ 30 aura particles, one emissive material.
- **Gameplay:** presentation only. A creature may replace an existing server node's visual (as the deer does) or be scenery. It never adds server state.

## 1. White Jade Spirit Deer (Bạch Ngọc Linh Lộc): implemented, awaiting approval

- **Role:** replaces the server's `deer` resource node visual. Hunting, regrowth and sync are unchanged.
- **Look:** ivory coat; long elegant legs; branching jade antlers that glow; flowing jade spirit markings on the flanks, shoulders and a crescent on the brow; jade hooves; dark glossy eyes with highlights.
- **Motion:** breathing (chest), slow curious look-around and dips (neck/head), ear twitches, tail flicks, and a gentle jade particle aura.
- **Spec:** 7,676 triangles, 21 bones.

## 2. Nine-Tailed Spirit Fox (Cửu Vĩ Linh Hồ): direction only

- Slender fox with oversized fluffy tails fanning in an arc. Ivory to pale-gold coat with crimson tips; a fox-fire wisp at each tail tip (additive sprites).
- Tails are separate skinned chains (5–6 bones each, 9 chains), swaying in out-of-phase sine waves. That is the main motion cost, so the budget is ~9k triangles and about 60 bones. It may need a lower tail-bone count at distance.
- Placement idea: a rare scenic sighting near the waterfall crag at dusk and night.

## 3. Celestial Crane (Tiên Hạc): direction only

- White crane with a red crown, black wing tips and long legs. Wing feathers are layered card meshes with alpha-cut feather texture.
- Motion: idle standing (head preen, one-leg rest), and a looping glide circuit high over the plaza (path follower plus wing flap and glide cycle).
- Placement idea: two or three cranes circling the Chánh Điện; perching on roof ridges at dawn.

## 4. Spirit Wolf (Linh Lang): direction only

- Lean wolf with smoky blue-grey fur tufts (shell cards), star-like glowing eyes, and spectral mist trailing from the paws (particles).
- Motion: idle sniff and look, and a prowl loop along the cliff rim at night.
- Placement idea: night-only scenery on the rim; never in the build zone.

## Disciple prototype: implemented, awaiting approval

- Anime proportions (head ≈ 1/6.5 of body height, slender limbs, about 1.39 units tall to sit right beside the halls).
- Traditional robe: white inner robe with a jade *jiaoling* crossed collar (left over right), a jade skirt flaring to the ankles, wide white sleeves with jade cuffs, and a dark sash with trailing ribbons.
- Hair: high bun with a gold hairpin tipped in glowing jade, side locks, a swept fringe and a long ponytail. Face: large eyes with jade irises and highlights, small mouth, soft blush.
- Rig: 21 bones, automatic weights. Motion: idle (breathing, head sway, hands resting near the sash) and walk (legs, arm swing, torso counter-rotation, bob), driven by `SonMonPuppet`.
- Spec: 11,064 triangles. Two scenery instances (one idle, one walking the sacred axis). Server workers still use the old placeholder until the prototype is approved.
