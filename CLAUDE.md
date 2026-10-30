# LangTroTan — Claude context

Unity 3D port of *Tứ Đại Bộ Châu — Tu Tiên Ký* (sect builder, xianxia). Server-authoritative client.
Unity **6000.4.3f1**, URP **17.4.0** (pipeline asset `Assets/VisualSlice/JadeMountainURP.asset`), Mono, Windows dev builds.
Workflow, task routing and QA commands: `docs/WORKFLOW_LOCAL_CLOUD.md`.

## Layout
- `Assets/Scripts/Domain` — legacy offline gameplay (World/LivingWorld/SectWorld). Still compiled and checked; don't delete.
- `Assets/Scripts/Infrastructure` — `AuthoritativeClient` (HTTP to the Node backend, requestId journal), `ServerJson`, `SaveRepository` (legacy local saves).
- `Assets/Scripts/Presentation` — `Game.Boot` → `FullPortView` (current full port: `FullPortSpatial`, `FullPortCommands`, `FullPortProjection`…), `Slice*` visual slice, `VisualSliceQA` (in-player QA).
- `Assets/Scripts/Presentation/SonMon` — 3D Sơn Môn art: visual tiers, building upgrade levels, upgrade FX, L6 spirit VFX (v3 `SonMonSpiritGathering`, v4 `SonMonSpiritGatheringV4`).
- `Assets/Editor` — builds, checks and capture harnesses (`ProjectBuild`, `*Checks`, `StageSiteCapture`, `SpiritV4Builder`…).
- `Assets/QA/` — isolated QA scenes (e.g. `L6SpiritQA`). `Assets/Scenes/Village.unity` is the only production scene (empty bootstrap; the world is built from code).
- `Tools/` — Blender, ForgeGUI, ShaderGraph (Python generators), QA (Windows-only scripts). `Reports/` — QA evidence (≈1.4 GB, large; read only the file you need). `docs/` — specs and policies.

## Key rules of the game code
- The client never resolves outcomes locally; every action goes to the server with a requestId. Don't add local gameplay logic.
- Visual work is gated: `-visual-tiers` (`SonMonTiers.Enabled`), `-no-ambient-fx`, `-reduced-effects`; world-scale stays off.
- Existing C# is dense; write new code readable and keep it in separate files.

## Safety (always)
- Do not modify: gameplay rules, the server/backend (`tu-tien-port/backend`, outside this repo), protected saves, production scenes, footprints, approved art, ForgeGUI GLBs or provenance (`ASSET_SOURCES.md`, `Tools/ForgeGUI/asset_manifest.json`).
- Protected player data: MongoDB on **27017** (`cxi_demo_ui`, `game_mv3bzlj8`). Never connect to it. QA uses only 27028/8899 (full port) and 27029/8901 (visual slice).
- The reference game (`C:/Users/Admin/Desktop/tu-tien-game`) is read-only. The Android release gate (`FullPortReleaseGate`) stays locked.
- **Level 6 Spirit VFX v4 is isolated QA only, not `approved_art`, not in production.** Don't touch it (or the L6 mesh/textures, Full/Reduced/Off modes, distance LOD, server-confirmed awakening) unless explicitly asked. Status: `Reports/ForgeGUI/scripture_L06/spirit_v4/REPORT.md`.
- `ProjectBuild.Build` rewrites `Village.unity`, build settings and `BUILD_RESULT.txt`; run it only when a build is the task.
- Never force-push `main`. Don't commit `Library/`, builds, QA players or frame sequences.

## Cloud sessions
No Unity, .NET, GPU or Unity download access. Don't try to install Unity. Do static review, docs, plans, Python tooling and small PRs; hand anything needing the Editor to Local.

## Detailed docs
Architecture `docs/APPROVED_ARCHITECTURE_SPEC.md` · project state and history `docs/CLAUDE_HANDOFF_AUDIT.md` · art `docs/ART_BIBLE.md`, `docs/XIANXIA_ART_DIRECTION.md` · building levels `docs/BUILDING_UPGRADE_LEVELS.md`, `docs/BUILDING_VISUAL_TIERS.md` · scale `docs/WORLD_SCALE_POLICY.md` · ForgeGUI `docs/FORGEGUI_INTEGRATION.md` · visual QA `docs/VISUAL_VERTICAL_SLICE_QA.md`.
