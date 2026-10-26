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

**Open issue (not fixed):** a later run on the final build (which also contains the smoke fix) passed the raycast and then **failed** `real disciple card button applies authoritative training assignment`. While that run was going, the server's world engine grew the roster from 4 to 9 disciples. The server rejects `assign_job` when `canAssignJob` fails (for example, a disciple on a mission). Root cause not confirmed. A diagnostic log line (`VISUAL_QA_TRAINING`) has been added, uncommitted, and the diagnostic runs were stopped before they finished. So the regression is **intermittent**: 1 PASS (fix1) and 1 FAIL (final).

## Full-port smoke (`-full-port-smoke`, isolated 8899 / Mongo 27028)

| Run | Result | Cause |
|---|---|---|
| Before fix | FAIL `upgrade or retry: before=581 after=131` | The check hard-coded a 300 linh thạch cost (the level-1 price). The shared `unity-port-qa` hall was already level 2, and the server correctly charged `round(200 × 1.5²) = 450`, exactly once |
| After fix (commit after `27febbd`) | **PASS**: upgrade received, exact cost debited once, journal resolved, 69 forms render, 14 hall sections project, `FULL_PORT_RUNTIME_OK` | The expected cost is now computed from the reference formula and the hall's current level. A new precondition reports FAIL if the hall is already upgrading |

The run used a freshly reseeded `unity-port-qa` game (`work/claude-audit/tools/resetUnityPortQa.mjs`, which asserts port 27028 and database `tu_tien_unity_port_qa`). The previous state of that QA game is gone, including the Mongo-restart evidence fixture. No player database was touched.

"69 forms render" and "14 sections project" still prove **rendering only**. Correct content and working actions for those screens remain unverified, apart from Bảo Khố and the actions listed above.

## Known issues found during QA
- `VisualSliceBuild.Build` appends duplicate entries to `m_AlwaysIncludedShaders` in `ProjectSettings/GraphicsSettings.asset` on every build. The URP shader dedupe check never matches under `-nographics`. It is harmless but grows the file, so I revert it after each build. To fix later.
- The status chip ("Tu luyện" = current state) and the action button ("Tu luyện" = command) share a caption on the Đệ Tử card. This is a UX problem, to fix in Milestone D.

### Performance, Sơn Môn, 1920×1080, run 2
CPU median 4.82 ms (p95 5.84 ms). GPU median 0.75 ms (p95 0.77 ms). 1,012,771 triangles. 315 renderers. About 100 MB allocated. Draw calls not captured by the existing profiler.

### Content observations
- HUD numbers are formatted in vi-VN (`4.320`, `26,7`). PASS.
- Duplicate disciple name "Dương Bất Quần". Likely caused by the constant-RNG QA fixture. Re-check in a normal new game.
- The same portrait is reused for every disciple of a school. Real issue, planned for Milestone D.
- Earlier GPT captures showed "Ngày 0 · Xuân · Đêm" next to "Nắng" and daylight lighting, and Lương 0. On the fresh fixture these read "Ban ngày" and 118–119, so they came from fixture ageing. The night label versus day lighting still needs a check once time-of-day lighting is implemented.

## Functional QA matrix still to run (after the art pass)
Server connection · world sync · resource changes · building selection · placement · upgrade · harvesting · worker movement · disciple actions · decision forms · Grand Hall data · save/load (Mongo restart) · UI navigation · camera. For each item, record separately: renders / correct content / action works / state persists.
