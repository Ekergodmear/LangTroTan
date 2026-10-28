# Character & Creature Asset Pipeline: proposal for approval

Status (2026-10-28): **research only. Nothing downloaded, installed or imported.** The script-built deer and disciple (`Tools/Blender/sonmon_creatures.py`) were visually rejected. They remain only as temporary functional placeholders, and no further work goes into them. Candidate previews are in `Reports/Pipeline/`; they are the authors' own preview images, not our renders.

## 1. Human characters: VRoid Studio → VRM → UniVRM → Unity URP

| Step | Tool | Licence / cost | Notes |
|---|---|---|---|
| Create | **VRoid Studio** (pixiv) | Free | Anime character editor: face, hair (strand groups), body proportions, outfits, textures. Its [guidelines](https://vroid.com/en/studio/guidelines) let individuals and corporations use and sell models built from VRoid's preset items commercially, *unless an item carries special clauses*. Per-model permissions are also written into the VRM metadata ([conditions of use](https://developer.vroid.com/en/guidelines/conditions_of_use.html)). |
| Import | **UniVRM** (`com.vrmc.gltf` + `com.vrmc.vrm`, [GitHub](https://github.com/vrm-c/UniVRM)) | MIT, free | Builds a Unity **Humanoid Avatar**, blend-shape expressions, LookAt, and **SpringBone** physics (hair, sleeves, skirt). Under **URP**, VRM 1.0 MToon maps to `VRM10/Universal Render Pipeline/MToon10`. VRM 0.x MToon is not supported under URP and falls back to unlit, so import as **VRM 1.0** ([material docs](https://vrm.dev/en/univrm1/material)). Requires Unity 2021.3+; Unity 6 is not formally stated, so I test it first in an isolated branch. |
| Animate | Unity Humanoid retargeting | — | Any humanoid clip retargets onto VRM avatars. Free source: **Quaternius Universal Animation Library** (CC0, 120+ clips including idle, walk, sit and gestures; already identified). Mixamo works too but needs an Adobe account, which I can't create; you could. |
| Cloth | VRM SpringBone (included) | Free | VRoid exports hair, skirt and sleeve bones with spring physics, so robes sway when walking. Full cloth simulation (Unity Cloth, or the paid Magica Cloth 2) is optional and not needed for gameplay camera distances. |

**Reference quality:** `Reports/Pipeline/char-vroid-cc0-samples.png` shows old VRoid sample avatars that pixiv released under **CC0** ([OpenGameArt mirror](https://opengameart.org/content/vroid-studio-cc0-models)). They are true anime styling, but modern outfits with no robes.

### Chinese-style robes and hair ornaments

- **Free VRoid outfits by yingyang2013 on BOOTH**, ¥0, `.vroidcustomitem`:
  - [チャイナ服007](https://booth.pm/ja/items/6972424): crossed-collar dress with flared sleeves, embroidered plum branches, sash and tassels. Preview: `robe-yingyang2013-china-clothes-007.jpg`.
  - [民族衣装002](https://booth.pm/ja/items/8674521): long robe with side slits, gold lotus embroidery, knotted cords. Preview: `robe-yingyang2013-ethnic-002.jpg`.
  - **Licence: not stated on the item pages.** Commercial and game use must be confirmed with the author before shipping.
- Alternatives with explicit terms: [fox-corp](https://fox-corp.booth.pm/items/5370599) China dresses (commercial use allowed with credit; free versions are watermarked). Most hanfu-style items are paid, ¥100–¥1,000+. No free hanfu with confirmed commercial rights was found.
- Hair ornaments (hairpins, crowns, ribbons) are usually separate VRoid accessories or BOOTH items, each with its own licence. VRoid's own hair editor can build buns and long styles natively.

### Performance for our use

- A VRoid export is typically 20–70k triangles with 8–15 materials. Its export dialog can **reduce polygons and atlas materials** (down to about 2–4 materials). Target per disciple: ≤ 25k triangles, ≤ 4 materials, plus a simplified LOD for gameplay distance (disciples are about 20 px tall there).
- Skinned meshes are not GPU-instanced, so budget about 15–20 animated characters on screen at full LOD. Distant ones use an LOD1 mesh and a reduced bone update rate.

## 2. Creatures

Previews: `creatures-sketchfab-candidates.png`, `creatures-quaternius-ultimate-animated-animals.jpg`.

| Creature | Best candidate found | Licence | Mesh / animation | Fit |
|---|---|---|---|---|
| White Jade Spirit Deer | **[Elk Animated Stylized](https://sketchfab.com/3d-models/26e54fa5c1d74edba61f5c5a97c95798)** (WildMesh_3D) | CC-BY 4.0 (credit required) | 7,886 tris, **53 animations**, hand-painted texture | **Best.** Correct anatomy and a full animation set. Repaint the coat ivory, give the antlers an emissive jade material, add spirit markings in the texture and an aura. Sketchfab downloads need an account login, which you would have to do. |
| (fallback) | Quaternius [Ultimate Animated Animals](https://quaternius.com/packs/ultimateanimatedanimals.html): stag, deer, fox, wolf, husky … | CC0 | 12+ animations each | Correct anatomy and animation, but a faceted low-poly look. Only acceptable if smoothed and repainted. |
| Spirit Wolf | [Stylized Wolf](https://sketchfab.com/3d-models/2b369cacbd3a4d19b39353f6cf60b8a9) (ESOOOO) | CC-BY | 7,928 tris, 2 animations | Cartoon-aggressive. Needs recolour and more animations (or Quaternius wolf clips). |
| Nine-Tailed Fox | none suitable | — | — | **Gap.** The free "fox"/"kitsune" results are anthropomorphic characters (rejected). A quality nine-tailed fox needs a paid asset or a commissioned model. |
| Celestial Crane | none suitable | — | — | **Gap.** No free rigged, animated stylized crane found. |

## 3. Decisions needed from you

1. **Install VRoid Studio** (free, official site or Steam) and **UniVRM** (MIT, GitHub release). Each needs your OK.
2. **Who designs the disciple in VRoid?** You (or an artist) control the look in the editor; or I drive VRoid Studio through computer-use screenshots, which is slower and less precise. I recommend you or an artist do it, and I handle import, Unity URP setup, animation, SpringBone and performance.
3. **Robe licences:** contact yingyang2013 to confirm commercial game use, or choose items with explicit commercial terms (likely paid).
4. **Spirit deer base:** download WildMesh_3D's Elk Animated Stylized from Sketchfab while logged in (CC-BY, credit in the game). I then restyle it into the Bạch Ngọc Linh Lộc and show it in Unity before anything else.
