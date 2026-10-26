# Claude Handoff Audit — Thanh Vân Tông (Unity port of Tứ Đại Bộ Châu — Tu Tiên Ký)

Audit date: 2026-10-26. Auditor: Claude (Lead Unity Engineer / TA / UX / QA). Scope: Milestone A only. **No project source files were modified.** The only new files in the project are under `docs/`.

Legend: **VERIFIED** = I checked it directly today. **REPORTED** = a claim from the previous GPT session that I have not re-run. **FAILED** = re-run today and did not pass.

---

## 1. Verified project state

| Item | Finding | Status |
|---|---|---|
| Project path | `C:/Users/Admin/Documents/Codex/2026-10-25/referenced-chatgpt-conversation-this-is-an/outputs/LangTroTan` | VERIFIED |
| Version control | **Not a git repository** (a `.gitignore` exists, but there is no `.git`). There is no history and no way to diff the previous session's changes | VERIFIED |
| Baseline snapshot | Taken before any work: `work/claude-audit/baseline-source-2026-10-26.zip` (Assets, Packages, ProjectSettings and root docs; 297 files; SHA-256 `b23c0022…639a`). The previous session also left `work/visual-before-source.zip` (its pre-slice state) | VERIFIED |
| Unity | 6000.4.3f1 (`ProjectSettings/ProjectVersion.txt`), installed at `C:/Program Files/Unity/Hub/Editor/6000.4.3f1`. The Editor is **not** open on this project (only `unity.exe serve` from the Hub is running) | VERIFIED |
| Render pipeline | URP 17.4.0 (`Packages/manifest.json`), pipeline asset `Assets/VisualSlice/JadeMountainURP.asset`. The previous session switched the project from Built-in to URP | VERIFIED |
| Scenes | One scene, `Assets/Scenes/Village.unity` (3.5 KB, an empty bootstrap). All world and UI is spawned from code at runtime | VERIFIED |
| Code size | 30 C# files, about 364 KB. Written in a very dense code-golf style (many statements per line), which makes review and diffs hard | VERIFIED |
| Tools | Node and MongoDB 8.2 present. Blender was not installed at audit time (4.5.10 LTS installed later with approval; headless FBX export verified). No 3D assets on disk (see `UNITY_ASSET_INVENTORY.md`) | VERIFIED |
| Builds | `Builds/VisualSlice` (latest, 14:29), `Builds/VisualBaseline`, `Builds/Windows_FullPort_Dev`, and older 0.x builds. About 1.1 GB in total | VERIFIED |

### Running processes found at audit start (none started by me)

| Port | Process | Purpose | Action taken |
|---|---|---|---|
| 27017 | Windows MongoDB **service** | Probably the original game's database, which may hold the protected saves `cxi_demo_ui` and `game_mv3bzlj8` | **Not touched** |
| 27028 | mongod, dbpath `tu-tien-port/mongo-qa` | Full-port QA DB `tu_tien_unity_port_qa` | Not touched |
| 8899 | node `unityMongoQa.mjs` | Full-port QA server, game `unity-port-qa` | Not touched |
| 27029 | mongod, dbpath `work/visual-qa/mongo` | Visual-slice QA DB `tu_tien_visual_slice_qa` | Used for the re-run below |
| 8901 | node `visualQaServer.mjs` | Visual-slice QA server, game `visual-slice` | Restarted by me after the fixture reset (now running from this session) |

No protected save was loaded, ticked or modified. The only data I touched was the isolated `visual-slice` QA game, through the project's own `resetVisualQaFixture.mjs`, which asserts it is on port 27029 and database `tu_tien_visual_slice_qa`.

## 2. Existing architecture

```
Unity client (uGUI, runtime-built scene)            Node backend (tu-tien-port/backend, copy of reference rules)
 Game.Boot → FullPortView ─ AuthoritativeClient ──HTTP──► /api/games/:id/state | action | presentation | spatial
   ├ FullPortSpatial / SliceArt*    (3D Sơn Môn)            ├ server/ World Engine (reference game rules, 69 actions)
   ├ SliceUI / UISkin               (HUD, Đệ Tử)            ├ MongoDB persistence, requestId receipts (no double debit)
   ├ FullPortCommands               (69 decision forms)     └ "presentation" endpoint: runs the original web renderer
   ├ FullPortProjection             (Đại Điện sections)          on a state copy and returns semantic nodes
   └ FullPortRecords / Navigation   (records, fallback lists)
 Legacy offline modes: World/LivingWorld/SectWorld (Domain), SaveRepository (local JSON save, 0.1–0.4)
```

- **Server-authoritative.** The client sends intents with a requestId and keeps a pending journal (`tu-tien-server-pending.json`). It never resolves outcomes locally. Stale snapshots are rejected by a sequence check. (Verified by reading `AuthoritativeClient.cs`.)
- **Two generations of gameplay code coexist.** The offline prototype domain (`Domain/*.cs`, local saves, 105 editor checks) and the server-authoritative full port (`FullPort*.cs`). The README says the full port is current; the offline domain is legacy, but it is still compiled and its checks still run in `ProjectBuild`.
- **Đại Điện is a projection.** The server renders the original HTML UI, which is converted to generic nodes that Unity lays out. Only Bảo Khố has a specialised native layout. The other 13 sections show generic flattened content.
- **The original reference game** (`C:/Users/Admin/Desktop/tu-tien-game`) is read-only. The previous session recorded a 556-file integrity hash (`../VisualSlice/SourceIntegrity.json`). I only listed its `assets/` folder.

## 3. Completed functionality

| Feature | Evidence | Status |
|---|---|---|
| Server snapshot received by client | Re-run today: PASS | VERIFIED (rendering + data received) |
| Core building opens upgrade decision dialog | Re-run today: PASS (dialog opens; the action itself was not exercised) | VERIFIED (render only) |
| Đệ Tử screen renders cards with controls | Re-run today: PASS | VERIFIED (render only) |
| Bảo Khố projection loads | Re-run today: PASS | VERIFIED (render + content) |
| Training assignment through a real card button | **Re-run today: FAIL** (see §5) | **FAILED** |
| Bảo Khố pill use → exact disciple receives +40 EXP | Not reached, because the run aborted at the failure above | REPORTED only |
| Placement → one house, exact wood debit; cancel → 80% refund | Not reached | REPORTED only |
| Harvest → animated worker; cancel releases worker | Not reached | REPORTED only |
| Camera pan/zoom | Not reached | REPORTED only |
| 69 decision forms render; 14 Đại Điện sections project | `Reports/FullPort/NativeRuntime.txt` (`FULL_PORT_RUNTIME_OK`) | REPORTED. This proves rendering only, not correct content or working actions |
| Mongo cold restart keeps state and request receipts | `Reports/FullPort/MongoRestart.json` | REPORTED |
| 105 offline domain checks + JSON/command coverage checks | `BUILD_RESULT.txt` | REPORTED (editor checks; not re-run) |

## 4. Incomplete, temporary or broken

1. **The last visual-slice build was never verified.** `VisualSlice/After/regression.txt` (the "COMPLETE" evidence) is timestamped 14:22. `VisualSliceQA.cs` and `FullPortSpatial.cs` were edited at 14:28 and `LangTroTan.Presentation.dll` was built at 14:29. The PASS evidence therefore belongs to an earlier build.
2. **That shipped build fails its own interaction regression** today, on both an aged fixture and a freshly reset one (§5).
3. **Ambiguous "Tu luyện" caption.** Each disciple card shows a status chip "Tu luyện" (state) next to an action button "Tu luyện" (command). Players cannot tell state from action, and the QA selector `text=="Tu luyện"` matches both. This is my leading hypothesis for the failure; I have not confirmed it in a debugger.
4. **Portraits repeat by school.** Every Đạo Giáo disciple shows the same face. There are only 5 school portraits plus about 22 avatars.
5. **Duplicate disciple names in QA screenshots** ("Dương Bất Quần" ×2). The QA fixture calls `createNewGame` with a constant RNG (`()=>.42`), so this is very likely a fixture artifact rather than a game bug. A real-game check is still needed.
6. **13 of 14 Đại Điện sections** are generic projections (Tông Phong, Thiên Cơ Báo and the others are flattened). The previous audit admits this.
7. **No authored scene, prefabs or materials.** Designers cannot work in the Editor; every art change is a C# change plus a rebuild.
8. **Characters** are primitive meshes with procedural leg swing. There is no rig, no animation and no anime proportion.
9. **Legacy medieval art** (`TitleVillage.png`, `MedievalIcons.png`, `VillageArt.cs` wolves and soldiers) is still in the project.
10. Military/war on the 3D map, phone performance and migration of old Unity saves are listed as not done in the README.
11. The Android/APK gate is intentionally locked (`FullPortReleaseGate.cs`). Keep it locked.

## 5. Re-verification run (today)

Method: I copied `Builds/VisualSlice` to `work/claude-audit/a/b/Build`, so the player's relative output path writes to `work/claude-audit/VisualSlice/After` and does not overwrite the previous evidence. I ran `LangTroTan.exe -visual-qa -visual-regression -force-d3d11` against the isolated server on 8901.

| Run | Fixture | Result |
|---|---|---|
| 1 | As left by GPT (aged) | 7 PASS, then `FAIL pointer raycast reaches visible button: Tu luyện`, abort |
| 2 | Freshly reset `visual-slice` fixture | Same 7 PASS, then the same FAIL |

Logs: `work/claude-audit/player-reverify.log`, `player-reverify2.log`. Screenshots: `work/claude-audit/VisualSlice/After-run1/` and `After/`.

Measured performance on the run 2 Sơn Môn view (dev player, D3D11, RX 6600M, 1920×1080, vsync off): CPU frame median 4.82 ms (p95 5.84 ms), GPU frame median 0.75 ms (p95 0.77 ms), **1,012,771 triangles**, 315 visible renderers, about 100 MB Unity allocated. Frame time is CPU-bound, and the GPU is mostly idle. The triangle count is far too high for what is on screen (§7).

## 6. Current visual quality (assessed from real Unity captures)

Unity Game View captures taken today (`work/claude-audit/VisualSlice/After/son-mon-1600.png`, `son-mon-1920.png`) and the previous `architecture-detail-1920.png` show the following:

- **Terrain:** a single flat green plane, with fog standing in for depth. There is no elevation, terraces, cliffs, paths or water in the playable area. The mountains and waterfall are distant flat grey extrusions.
- **Vegetation:** about 150 copies of one lollipop tree (a faceted blob canopy on a thin trunk), plus thin bamboo sticks. They are evenly scattered with no clustering or species mix, and they cover the courtyard.
- **Architecture:** the roofs read as "East Asian" and are the strongest element. But walls are flat untextured boxes, and there are no brackets, ridge beasts, railings, carved panels or bevels. Scale hierarchy is weak: the Chánh Điện is barely bigger than the houses. The courtyard is a flat tiled square sitting on the grass.
- **Lighting/colour:** heavy uniform fog flattens everything to green-grey. There is no key-light warmth, no rim light, no AO, and no contrast between the sect and its surroundings.
- **Magic identity:** a single small cyan crystal. No formation, glowing array, floating rock or spiritual particles are visible at gameplay zoom.
- **UI:** the cream/jade HUD is clean and readable, and the Vietnamese typography works. But the panels are generic rounded rectangles, the top bar is very tall, and the right info card duplicates HUD data. There is no bronze/gold ornament or parchment texture.

**Verdict:** I agree with the user. This is a primitive prototype, and lighting or post-processing will not lift it to "detailed stylized anime Xianxia". The missing piece is real modelled and textured assets (`UNITY_ASSET_INVENTORY.md`).

## 7. Technical risks

| Risk | Severity | Note |
|---|---|---|
| No version control | High | Any edit is unrecoverable except via zip snapshots. Resolved: local git repo initialised with approval, baseline commit `e9d7424` |
| Unverified final build / failing regression | High | Fix before building more on top |
| Runtime-generated, unshared meshes (1.0M tris, no LOD/instancing/batching) | High for scale and mobile | Replacing them with imported, LOD'd, instanced prefabs is necessary anyway |
| Dense code-golf C# | Medium | Slows safe changes. New presentation code should be written readable and kept separate |
| Two gameplay generations in one assembly | Medium | Keep the legacy offline domain compiling. Do not delete it during the art pass |
| Protected player DB on 27017 on the same machine | Medium | All QA scripts hard-assert ports 27028/27029. Keep that discipline |
| Asset licensing of user-supplied 2D art | Medium | No licence is inferred. You need to confirm rights before release |
| Art pipeline gap (no models) | **Blocking for Milestone B** | Route D approved: Blender installed, CC0 Poly Haven textures and HDRIs imported, commercial kit pending your purchase |

## 8. What GPT Work was doing most recently

From file timestamps (13:25–14:29 on 2026-10-26): it switched the project to URP, wrote `SliceArchitecture`, `SliceEnvironment`, `SliceAtmosphere` and `SliceUI` (the cream/jade "visual vertical slice"), added two custom HLSL shaders, ran a "night pass" on lighting, and captured Before/After screenshots with performance JSON in `outputs/VisualSlice/`. Its last action was a code edit plus a rebuild at 14:28–14:29, made **after** its last QA capture.

## 9. Recommended continuation order

1. ✅ Done: `git init` and commit the baseline, so every later change is reversible and reviewable.
2. ✅ Done: real root cause was clicking before buttons were rendered (see the QA doc). Original suggestion, kept for the record: make the status chip non-interactive or caption it differently, and select buttons by role rather than caption. Do not weaken the test. Re-run the full `-visual-regression`, then also run `-full-port-smoke` against 8899.
3. ✅ Route D approved. Was: decide on the asset sourcing route (`VISUAL_VERTICAL_SLICE_PLAN.md` §2).
4. Milestone B: build an authored `SonMon_Slice` scene and prefabs from the real assets. Move from runtime-only generation to prefab lookup by building `type`, keeping the server contract unchanged.
5. Milestones C, D and E as planned, each with real screenshots, performance JSON and functional QA.
