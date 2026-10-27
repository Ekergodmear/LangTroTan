# Visual Vertical Slice QA

This QA log starts with the baseline re-verification on 2026-10-26. No new visual implementation has been tested yet, because none exists.

## Baseline (previous GPT build `Builds/VisualSlice`, compiled 2026-10-26 14:29)

Environment: Windows 11, AMD RX 6600M 8 GB, D3D11, development player, isolated server `127.0.0.1:8901` with game `visual-slice` on Mongo 27029 (`tu_tien_visual_slice_qa`). The player was copied to `work/claude-audit/a/b/Build` so the previous evidence was not overwritten.

| Check | Run 1 (aged fixture) | Run 2 (fresh fixture) | Category |
|---|---|---|---|
| Server snapshot received | PASS | PASS | Data received |
| Core building opens upgrade decision (1600 and 1920) | PASS | PASS | Renders |
| Disciple contextual controls present (1600 and 1920) | PASS | PASS | Renders |
| Bảo Khố projection available (1600 and 1920) | PASS | PASS | Renders + content |
| Pointer raycast reaches the visible "Tu luyện" button | **FAIL** | **FAIL** | Action |
| Training assignment, pill use, placement, cancel refund, harvest, camera | Not reached (aborted) | Not reached | Actions / persistence |

Conclusion: the previously reported `COMPLETE` regression (14:22) does **not** reproduce on the final build. (My first hypothesis, that the selector also matched the status chip, was wrong: the chip is not a Button. See the root cause below.)

## Regression fix (commit `9de5af6`)

**Root cause (verified with diagnostics):** the click point was inside the button, the only raycaster was on, and the display mapping was correct, yet `EventSystem.RaycastAll` returned no hits at all. `GraphicRaycaster` skips graphics whose depth is still -1, meaning they have never been rendered. The regression rebuilt each screen with `Draw()` and clicked in the same frame. Players are not affected: they can only click a button after it has been drawn.

**Fix:** `ClickVisualButton` is now a coroutine. It waits up to 30 frames until the button has been rendered, then runs the **unchanged** strict assertion (the first raycast hit must belong to that exact button). On failure it logs point, screen, displays, canvas, raycasters and the hit path.

**Result:** `-visual-regression` gives **33/33 PASS** on a fresh fixture. That covers training assignment through a real card button, the exact pill consumed and +40 EXP to the exact disciple id, placement debiting exactly 40 wood, cancel refunding 80%, selectable 3D roof bounds, harvest worker spawn and release, and resolved request journals. Logs: `work/claude-audit/fix1/`.

## "Tu luyện" card button: real product bug, fixed (2026-10-27)

**Root cause (verified in the reference rules):** the Đệ Tử card's primary button sent `assign_job {status: "training"}`. In the reference game, `canAssignJob` (`js/systems/workerAssignment.js`) **never** allows `training` as a target. Players may only recall a disciple to `idle`, or move an idle disciple to `labor`, `alchemy` (Đạo/Phật) or `forge` (Kiếm/Võ). `training` is set by the Ngoại Môn policy (`js/systems/ngoaiMon.js`). The server therefore always rejected the button ("Đệ tử không đáp ứng điều kiện phân công hoặc thu hồi."). The earlier 33/33 PASS (fix1) was a **false pass**: the disciple it picked happened to be in `training` already, so the rejected command left the status that the assertion expected.

**Fix (client presentation only; server and rules unchanged):**
- `SliceUI.CanAssignJob` mirrors the reference rule. Each card now shows only the command the server can accept, using the web UI's own wording: **Thu hồi** (→ idle), **Xếp luyện đan** (→ alchemy) or **Xếp rèn khí** (→ forge). When no assignment is valid (Chưởng môn, on a mission, etc.), the card shows only a full-width **Hồ sơ** button.
- The status tag uses the reference labels: `training` displays as "Luyện võ" instead of "Tu luyện", and "Luyện đan", "Luyện khí", "Thám hiểm", "Du hành" and "Bế quan" were added. State and command no longer share a caption.
- The regression now picks a disciple the reference rules allow to be assigned (it prefers the luyện đan/rèn round trip), finds the button by its id-based name (`AssignJob:<id>:<status>`) once it is alive and drawn, asserts the exact authoritative result, and restores the previous status only when the rules permit.

**Result:** two consecutive runs on fresh fixtures (`work/claude-audit/assign1`, `assign2`): **33/33 PASS each**. Both exercised `training → idle` (Thu hồi). **Not yet exercised:** the luyện đan/rèn path, because the fixture had no idle Đạo/Phật/Kiếm/Võ disciple at the time.

Observation, not fixed: the card shows "Tu vi 223 / 88". The requirement `80 × realm × (1 + 0.1 × subLevel)` is a client-side formula from the previous session, not taken from the reference game. It needs checking against `realms.js` in Milestone D.

## Full-port smoke (`-full-port-smoke`, isolated 8899 / Mongo 27028)

| Run | Result | Cause |
|---|---|---|
| Before fix | FAIL `upgrade or retry: before=581 after=131` | The check hard-coded a 300 linh thạch cost (the level-1 price). The shared `unity-port-qa` hall was already level 2, and the server correctly charged `round(200 × 1.5²) = 450`, exactly once |
| After fix (commit after `27febbd`) | **PASS**: upgrade received, exact cost debited once, journal resolved, 69 forms render, 14 hall sections project, `FULL_PORT_RUNTIME_OK` | The expected cost is now computed from the reference formula and the hall's current level. A new precondition reports FAIL if the hall is already upgrading |

The run used a freshly reseeded `unity-port-qa` game (`work/claude-audit/tools/resetUnityPortQa.mjs`, which asserts port 27028 and database `tu_tien_unity_port_qa`). The previous state of that QA game is gone, including the Mongo-restart evidence fixture. No player database was touched.

"69 forms render" and "14 sections project" still prove **rendering only**. Correct content and working actions for those screens remain unverified, apart from Bảo Khố and the actions listed above.

## Known issues found during QA
- `VisualSliceBuild.Build` appends duplicate entries to `m_AlwaysIncludedShaders` in `ProjectSettings/GraphicsSettings.asset` on every build. The URP shader dedupe check never matches under `-nographics`. It is harmless but grows the file, so I revert it after each build. To fix later.
- ~~Status chip and action button shared the caption "Tu luyện"~~: fixed 2026-10-27 (see above).

### Performance, Sơn Môn, 1920×1080, run 2
CPU median 4.82 ms (p95 5.84 ms). GPU median 0.75 ms (p95 0.77 ms). 1,012,771 triangles. 315 renderers. About 100 MB allocated. Draw calls not captured by the existing profiler.

### Content observations
- HUD numbers are formatted in vi-VN (`4.320`, `26,7`). PASS.
- Duplicate disciple name "Dương Bất Quần". Likely caused by the constant-RNG QA fixture. Re-check in a normal new game.
- The same portrait is reused for every disciple of a school. Real issue, planned for Milestone D.
- Earlier GPT captures showed "Ngày 0 · Xuân · Đêm" next to "Nắng" and daylight lighting, and Lương 0. On the fresh fixture these read "Ban ngày" and 118–119, so they came from fixture ageing. The night label versus day lighting still needs a check once time-of-day lighting is implemented.

## Functional QA matrix still to run (after the art pass)
Server connection · world sync · resource changes · building selection · placement · upgrade · harvesting · worker movement · disciple actions · decision forms · Grand Hall data · save/load (Mongo restart) · UI navigation · camera. For each item, record separately: renders / correct content / action works / state persists.

## Milestone B, free route: first integrated pass (2026-10-27)

Build `Builds/VisualSlice` (VisualSliceBuild → SonMonKitBuilder → ProjectBuild), isolated fixture reset before the run. Evidence: `Reports/MilestoneB/` (real Unity camera renders; the GPT baseline images there are named `before-gpt-*`).

| Check | Result |
|---|---|
| Editor checks in ProjectBuild | 194 PASS, 0 FAIL |
| `-visual-regression` | **33/33 PASS** (placement debits exact wood, 80% cancel refund, harvest worker, assignment, pill, selectable 3D roof bounds) |
| Kit sanity (logged by builder) | 21 building prefabs, 9 resource-tree prefabs with LODGroups, environment prefab. LOD1 verified on all three axes against LOD0 |

**Performance, Sơn Môn 1920×1080** (dev player, D3D11, RX 6600M, vsync off): CPU median 5.44 ms (p95 8.84), GPU median 1.91 ms (p95 2.17), 1.13 M triangles counted by the profiler (includes the 2-cascade shadow passes), 346 visible renderers. The GPT baseline had 1.01 M triangles and 0.75 ms GPU. GPU cost rose about 1.2 ms for real modelled and textured content. Triangle count is still above my 300 k target, which needs instancing and LOD on the buildings and meadow props.

Defects found and fixed during this pass, each caught by a screenshot or a builder guard:
1. FBX came in Z-up (models lying on their backs). Fixed with `bake_space_transform` plus a guard that throws if the hall is not Y-up.
2. Slab faces were culled (inverted winding), so the plaza was invisible.
3. LOD1 imported 100× too large (unit mismatch). Now normalised and checked on 3 axes.
4. Roof trims used absolute sizes ("horns" on small roofs). Now scaled to the roof.
5. A detail-scale variable shadowed a loop index and made every roof 12× too large. This broke one regression check and was caught by the regression.
6. Photo-scanned leaf litter read as autumn mud. Replaced with a generated painted meadow.

**Still placeholder or not done (Milestone B is not complete):** worker characters and deer are the old primitive meshes; no spirit-particle or formation VFX pass yet; waterfall not visible from the default camera; no close-up hero shot of the hall or night capture yet; the meadow between resource trees is plain; HUD panels hide the sky and peaks (Milestone C).
