# APPROVED BUILDING ARCHITECTURE SPECIFICATION
## Thanh Vân Tông — 6 Building Families × 10 Levels

> Verbatim copy of the specification approved by the user on 2026-10-28. It is the authoritative architecture source for `Tools/ForgeGUI/asset_manifest.json` (the `approvedArchitectureDescription` fields are parsed from the tables below). Do not edit the level text; record deviations in the manifest's review notes instead. Approved amendments are appended per family (Tàng Kinh Các: Amendment 1, three-stage size policy).

**Status:** Architectural design approved. Generated 3D assets require separate visual approval.

**Production order:** Scripture Pavilion → Main Hall → Alchemy Hall → Forging Hall → Martial Training Ground → Spirit Fields & Mines.

### Global architectural rules

- Premium stylized anime Xianxia architecture, inspired by traditional Chinese immortal sects.
- Jade-teal curved roofs, deep-red lacquered timber, ivory stone foundations, antique gold details, and turquoise jade accents.
- Preserve the same architectural identity throughout all ten levels of a building family.
- Every level requires a genuinely different architectural structure, not just a scaled or recolored model.
- Keep realistic human-scale doors, windows, stairs, balconies, and columns.
- Keep entrances facing the same direction and use a consistent ground-level pivot.
- Preserve server-defined footprints and existing save data.
- Do not generate ambient particles, magical trails, or animated objects as part of the static building mesh.
- Chánh Điện must remain the dominant architectural landmark of the sect.
- The Azure Dragon belongs exclusively to Chánh Điện level 10.

---

# 1. Tàng Kinh Các — Scripture Pavilion

**Server ID:** `tang_kinh_cac`  
**Footprint:** 3×3  
**Architecture theme:** Ancient scripture library, layered pagoda roofs, elegant scholarly architecture.  
**Animation theme:** Floating scriptures, ancient runes, celestial books.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Tàng Thư Tiểu Các | One-story library pavilion, one curved jade-teal roof, four main red timber pillars, low ivory stone platform, three front steps, double wooden doors, two lattice windows, wooden signboard and two small lanterns. |
| 2 | Tàng Thư Các | Expanded one-story pavilion with a wider entrance porch, two additional porch pillars, larger roof overhang, five front steps, four windows, carved wooden railings and two small side storage rooms integrated into the main structure. |
| 3 | Song Tầng Kinh Các | Two-story scripture pavilion with two distinct curved roof layers, lower reading hall, upper scripture chamber, upper-floor balcony, carved wooden balustrades, elevated stone platform and central staircase. |
| 4 | Huyền Văn Kinh Các | Enlarged two-story pavilion with surrounding covered corridors, high stone terrace, symmetrical staircase, three-bay main entrance, carved columns, cloud motifs, restrained gold roof trim and two small stone lantern towers. |
| 5 | Linh Ngọc Tàng Kinh Các | Prestigious three-story pagoda with three jade-teal roof layers, red lacquered columns, white stone terrace, grand front staircase, ornate golden roof ridges, decorative spirit-beast roof finial, turquoise jade medallion and hanging lanterns. The existing ForgeGUI model is a candidate for this level. |
| 6 | Tụ Linh Kinh Các | Enlarged three-story pagoda with broader roofs, continuous second-floor balconies, two-tier stone terrace, symmetrical staircase branches, intricately carved roof brackets, jade balustrades, dragon-shaped eave ornaments and four spirit-stone pillars at the terrace corners. |
| 7 | Thiên Thư Bảo Các | Majestic four-story scripture pavilion with four curved jade roof layers, grand central staircase, multi-floor balconies, richly carved red timberwork, two miniature side towers integrated with the main building and covered connecting corridors. |
| 8 | Cửu Thiên Kinh Các | Refined four-story celestial library with layered roof crowns, taller side towers, three-tier ceremonial stone terrace, split staircases, ornate entrance arch, spirit-beast statues, jade scripture tablets, celestial-book reliefs and elevated bridges connecting the central pavilion to the side towers. |
| 9 | Vạn Pháp Tiên Các | Monumental five-story immortal scripture palace with cascading jade roofs, symmetrical side wings, two attached auxiliary pavilions, three-bay ceremonial gateway, grand white stone staircase, golden dragon roof finials, scripture reliefs and richly detailed red timber latticework. |
| 10 | Thái Thượng Tàng Kinh Tiên Cung | Ultimate five-story celestial scripture palace with two tall integrated side towers, multiple curved jade roof tiers, monumental ivory-jade terrace, grand ceremonial entrance, dragon-carved columns, ornate connecting galleries, magnificent golden roof crowns, a rooftop scripture altar and four ceremonial artifact pillars. |

**Ambient animation progression:** Lanterns → floating pages → ancient runes → glowing jade → rotating scripture rings → floating books → celestial book projection.

**Level-10 signature:** A colossal celestial scripture opens above the pavilion, with golden ancient characters orbiting the building. Implement separately in Unity.

### Amendment 1 (approved by the user 2026-10-28): three-stage size progression

The level rows above are unchanged. This amendment sets how large each level is and adds Level 10 elements. Details, targets and site strategy: `docs/SCRIPTURE_THREE_STAGE_POLICY.md`.

| Level | Stage | Width at door 0.52 | Stage design emphasis (user policy) | Footprint | Notes |
|---|---|---|---|---|---|
| 1 | — | 2.18 | Existing approved small pavilion | 3×3 | approved with notes |
| 2 | I | 5.0–5.5 | Level 2 attempt 2 is the approved architectural direction | Stage I lot (proposed 7×5) | railings and signboard still required (signboard may be a Unity prop) |
| 3 | I | 5.0–5.5 | Two-story central hall | Stage I lot | — |
| 4 | I | 5.0–5.5 | Expanded two-story hall with corridors and ornate stone podium | Stage I lot | — |
| 5 | I | 5.0–5.5 | Magnificent three-story jade-roof scripture pavilion | Stage I lot | replaces the old Level 5 candidate (superseded) |
| 6 | II | 7.0–8.0 | Expanded three-story complex; extended wings | Stage II lot (proposed 9×7) | dramatic jump from Level 5 |
| 7 | II | 7.0–8.0 | Four-story main pavilion with two side towers | Stage II lot | — |
| 8 | II | 7.0–8.0 | Grand four-story complex with elevated bridges and guardian statues | Stage II lot | — |
| 9 | III | 10–12 | Monumental five-story immortal scripture palace | Stage III lot (proposed 13×10) | dramatic jump from Level 8 |
| 10 | III | 10–12 | Supreme five-story celestial scripture palace with twin towers, massive white-jade foundation, central spirit crystal | Stage III lot | floating celestial scripture is VFX, not mesh |

Within a stage, growth comes from height, complexity and silhouette, not width. Door height 0.52 and character height 0.85 are fixed. Never shrink the door or distort proportions to fit a lot. The stage lots are proposals: server footprints, placement and saves are unchanged until separately approved.

---

# 2. Chánh Điện — Main Hall

**Server ID:** `chanh_dien`  
**Footprint:** 4×4  
**Architecture theme:** Sect authority, ceremonial palace, grand central hall.  
**Animation theme:** Sect banners, protective formations, spiritual energy, Azure Dragon.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Sơ Khai Chính Điện | Modest one-story three-bay ceremonial hall, broad curved teal roof, four main red pillars, simple stone foundation, central double doors and two sect banners. |
| 2 | Thanh Vân Chính Điện | Wider entrance porch, two additional pillars, enlarged main doors, carved stone railings, improved roof brackets and ceremonial stone steps. |
| 3 | Song Mái Chính Điện | Taller main hall with two-tier roof composition, expanded audience chamber, covered side corridors and more prominent central entrance. |
| 4 | Huyền Vân Đại Điện | Two-tier stone platform, grand ceremonial staircase, symmetrical side-roof extensions, cloud-pattern reliefs and a wider forecourt entrance. |
| 5 | Thanh Ngọc Đại Điện | Prestigious two-story main hall with jade-glazed roofs, gold-trimmed ridges, twin ceremonial lantern towers, carved red pillars and an elevated stone podium. |
| 6 | Long Văn Đại Điện | Expanded five-bay ceremonial facade, enlarged upper balconies, dragon-carved columns, elaborate wooden brackets and wider symmetrical roof wings. |
| 7 | Thiên Vân Chính Cung | Three-level architectural composition with cascading jade roofs, two attached side halls, monumental central staircase and a large ceremonial terrace. |
| 8 | Thanh Vân Tiên Cung | Grand three-story palace with integrated side halls, covered connecting galleries, an imposing ceremonial gate, layered roof crowns and finely carved stone balustrades. |
| 9 | Cửu Thiên Đại Điện | Monumental immortal sect palace with multiple jade roof tiers, ceremonial dragon statues, dedicated formation altar, ornate gold roof decorations and an elevated main throne hall. |
| 10 | Thanh Vân Tiên Điện | Ultimate five-bay, three-story celestial main palace with symmetrical side towers, grand dragon ceremonial platform, monumental stone staircase, intricately layered jade roofs, magnificent golden ridge ornaments and a dominant sect-leader throne hall. |

**Level-10 signature:** A separate rigged Azure Dragon circles the Main Hall. The dragon must not be embedded in the architectural model.

---

# 3. Luyện Đan Đường — Alchemy Hall

**Server ID:** `luyen_dan_duong`  
**Footprint:** 3×3  
**Architecture theme:** Alchemical laboratories, medicinal herbs, bronze cauldrons and sacred flames.  
**Animation theme:** Cauldron smoke, alchemy fire, floating pills, celestial furnace.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Sơ Cấp Đan Phòng | One-story alchemy workshop with a curved teal roof, small bronze cauldron, modest chimney, wooden herb shelves and low stone foundation. |
| 2 | Linh Thảo Đan Phòng | Enlarged workshop with extended porch, two auxiliary furnaces, improved ventilation chimney and additional herb storage. |
| 3 | Song Tầng Đan Các | Two-story alchemy building with central furnace chamber, medicinal-herb drying courtyard, layered roofs and side storage compartments. |
| 4 | Huyền Hỏa Đan Đường | Elevated alchemy terrace, two symmetrical medicinal annexes, decorated chimneys, carved stone steps and a more prominent furnace platform. |
| 5 | Linh Đỉnh Đan Đường | Grand alchemy hall with three roof sections, central ceremonial bronze cauldron, twin ventilation towers, red timber columns and jade-trimmed stone base. |
| 6 | Tụ Hỏa Đan Điện | Enlarged two-story alchemy complex with symmetrical side chambers, jade balustrades, dedicated fire pillars and expanded medicinal preparation areas. |
| 7 | Thiên Đan Bảo Điện | Three-story central alchemy hall with two attached medicinal towers, covered corridors, grand furnace chamber and richly ornamented roofs. |
| 8 | Cửu Hỏa Đan Điện | High ceremonial alchemy platform with nine designated fire-formation positions, elaborate jade roofs, central cauldron altar and elevated furnace terraces. |
| 9 | Tiên Đan Bảo Điện | Monumental alchemy palace with auxiliary pavilions, oversized bronze cauldron platform, spirit-fire pillars, extensive medicinal annexes and grand stone staircases. |
| 10 | Cửu Chuyển Tiên Đan Cung | Ultimate three-story alchemy palace with two attached alchemy towers, central celestial-cauldron platform, nine ceremonial fire pillars, jade connecting corridors and magnificently ornamented teal-gold roofs. |

**Level-10 signature:** A floating celestial cauldron surrounded by nine rotating spiritual-flame rings.

---

# 4. Luyện Khí Điện — Artifact Forging Hall

**Server ID:** `luyen_khi_dien`  
**Footprint:** 3×3  
**Architecture theme:** Divine smithing, forging furnaces, weapon crafting and heavenly metal.  
**Animation theme:** Forge fire, sparks, animated tools, floating weapons and lightning.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Sơ Khai Luyện Khí Phường | Small one-story forge with teal roof, furnace, anvil, bellows, weapon rack and stone foundation. |
| 2 | Huyền Thiết Luyện Phường | Expanded smithing workshop with auxiliary furnace, additional crafting table, taller chimney and covered work area. |
| 3 | Song Lô Luyện Khí Phường | Two-section forge with dual furnaces, metal storage room, improved roof ventilation and extended entrance porch. |
| 4 | Linh Hỏa Luyện Khí Điện | Enlarged one-story forge hall with central crafting altar, stone forging terrace, decorative metal pillars and reinforced furnace chamber. |
| 5 | Thanh Ngọc Luyện Khí Điện | Prestigious two-story forging hall with monumental central furnace, twin smoke towers, jade roof tiles, ornate red timber and grand stone stairs. |
| 6 | Huyền Công Luyện Khí Điện | Expanded forging complex with two workshop wings, weapon-quenching platform, divine-weapon display racks and refined roof ornaments. |
| 7 | Thiên Công Bảo Điện | Three-story artifact hall with auxiliary forging towers, stone galleries, elevated spirit-fire platform and richly detailed roof brackets. |
| 8 | Thiên Công Khí Điện | Advanced celestial forge with multi-tier furnace architecture, sacred artifact altar, lightning-conducting columns and elaborate jade-gold roofs. |
| 9 | Thần Binh Bảo Điện | Monumental divine-forging palace with two attached forge towers, grand crafting platform, protective guardian statues and reinforced ceremonial foundations. |
| 10 | Thiên Công Thần Binh Cung | Ultimate three-story divine-forging palace with two high forging towers, sacred flame furnace, celestial weapon altar, four heavenly lightning columns and richly layered jade roofs. |

**Level-10 signature:** A divine weapon suspended over the main forge, surrounded by magical forging rings and controlled lightning effects.

---

# 5. Diễn Võ Trường — Martial Training Ground

**Server ID:** `dien_vo_truong`  
**Footprint:** 3×3  
**Architecture theme:** Open-air martial arenas, training courts, sword formations and combat pavilions.  
**Animation theme:** Sect banners, sword energy, flying swords and sword formations.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Sơ Cấp Võ Trường | Open stone training yard with wooden dummies, weapon racks, small sect banners and simple perimeter fencing. |
| 2 | Ngoại Môn Võ Trường | Enlarged training court with additional dummies, covered training shelter, improved wooden fences and wider weapon storage. |
| 3 | Thanh Thạch Võ Đài | Raised circular stone martial platform, four banner pillars, two side training areas and carved stone edging. |
| 4 | Huyền Võ Đài | Elevated arena with low spectator terraces, formal entrance gate, stone balustrades and symmetrical training shelters. |
| 5 | Thanh Vân Diễn Võ Trường | Grand training ground with two martial platforms, covered instruction pavilion, ornate stone railings and sect banners. |
| 6 | Truyền Công Võ Điện | Expanded martial ground with a two-story instruction hall, covered corridors, sword-testing pillars and larger central arena. |
| 7 | Thiên Võ Trường | Three-zone martial complex with multi-tier spectator platforms, four sword-formation columns, training pavilions and ceremonial entrance. |
| 8 | Thanh Vân Đại Võ Trường | Large central arena with symmetrical auxiliary halls, raised stone platforms, ceremonial staircases and refined jade-roofed galleries. |
| 9 | Thiên Kiếm Võ Đài | Grand sword-cultivation arena with high sword altar, stone sword pillars, layered training terraces and dedicated formation platform. |
| 10 | Vạn Kiếm Thiên Đài | Ultimate immortal martial complex with multi-tier central arena, four sword towers, instruction hall, celestial sword-formation altar and richly detailed jade-roofed pavilions. |

**Level-10 signature:** A rotating formation of flying spiritual swords above the main arena.

**Critical technical note:** The existing Unity visual mapping reportedly shares a type with player-built guard barracks. Do not reuse this arena art for barracks. Separate visual mapping by authoritative building identity without changing gameplay.

---

# 6. Linh Điền & Linh Mỏ — Spirit Fields and Mines

**Server ID:** `linh_dien_linh_mo`  
**Footprint:** 2×2  
**Architecture theme:** Spirit herb cultivation, sacred springs, mineral crystals and spiritual veins.  
**Animation theme:** Growing herbs, water flow, crystal pulses and flowing spiritual energy.

**Important:** This is one server-managed building family. Do not split it or modify its footprint without explicit approval. High-level designs must be adapted to the limited 2×2 space using compact vertical terraces and integrated structures.

| Level | Name | Approved architectural structure |
|---|---|---|
| 1 | Sơ Khai Linh Điền | Compact spirit-herb patch, simple irrigation well, small reinforced mine entrance and basic stone pathways. |
| 2 | Linh Thảo Điền | Additional herb beds, low wooden fencing, small ore cart and improved mine entrance supports. |
| 3 | Thanh Thạch Linh Viên | Compact stepped herb terraces, narrow stone pathways, reinforced stone mine gate and small mineral storage. |
| 4 | Tụ Linh Điền | Improved irrigation channels, small cultivation shelter, raised herb planters and decorative mine storage. |
| 5 | Linh Tuyền Điền Mỏ | Multi-level herb terraces, compact spirit spring, carved stone mine entrance and jade-accented retaining walls. |
| 6 | Huyền Mạch Linh Viên | Vertical herb cultivation racks, small medicinal pavilion, spirit-crystal columns and elevated irrigation channels. |
| 7 | Thiên Linh Điền Mỏ | Compact multi-tier cultivation terraces, narrow spiritual-energy regulation tower, jade mine gateway and integrated mineral display. |
| 8 | Linh Mạch Sơn Trang | Vertical spirit garden with central sacred spring, small stone bridge, raised mining altar and stacked cultivation platforms. |
| 9 | Thiên Địa Linh Viên | Highly refined tiered herb garden, crystalline spiritual vein, miniature jade cultivation tower and integrated sacred spring. |
| 10 | Cửu Thiên Linh Mạch | Ultimate compact celestial resource sanctuary with vertical spirit-herb terraces, sacred spring, luminous jade crystals, refined spiritual-energy regulation altar and monumental carved mine gate adapted to the 2×2 footprint. |

**Level-10 signature:** A flowing spiritual vein illuminates the herb terraces, sacred spring and jade crystals in a synchronized cycle.

---

# Implementation instructions

1. Copy these 60 architectural descriptions verbatim into the corresponding `approvedArchitectureDescription` fields of `asset_manifest.json`.
2. Keep the Art Bible as the shared global style specification.
3. Verify the server IDs, footprint values and visual mapping against the actual codebase.
4. Treat this approval as approval of architectural design only, not generated model quality or credit spending.
5. Do not modify authoritative gameplay rules, level caps, production scenes or protected saves.
6. Do not silently alter the approved architectural design to accommodate ForgeGUI limitations.
7. For a footprint conflict, provide a concrete proposed adaptation and request approval.
8. For each building family, use the same reference pack, camera angles, palette, material rules and proportions across all levels.
9. Use the previous level's render as a continuity reference when supported, but do not assume image references guarantee consistent geometry.
10. Keep ambient animations separate from static architecture assets.
11. Preserve the existing level-5 ForgeGUI Scripture Pavilion as a candidate and compare it against the approved level-5 specification.
12. Do not generate assets or spend credits yet.
