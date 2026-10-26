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

Conclusion: the previously reported `COMPLETE` regression (14:22) does **not** reproduce on the final build. Leading hypothesis: the selector matches both the status chip and the action button, which share the caption "Tu luyện". Not yet confirmed.

### Performance, Sơn Môn, 1920×1080, run 2
CPU median 4.82 ms (p95 5.84 ms). GPU median 0.75 ms (p95 0.77 ms). 1,012,771 triangles. 315 renderers. About 100 MB allocated. Draw calls not captured by the existing profiler.

### Content observations
- HUD numbers are formatted in vi-VN (`4.320`, `26,7`). PASS.
- Duplicate disciple name "Dương Bất Quần". Likely caused by the constant-RNG QA fixture. Re-check in a normal new game.
- The same portrait is reused for every disciple of a school. Real issue, planned for Milestone D.
- Earlier GPT captures showed "Ngày 0 · Xuân · Đêm" next to "Nắng" and daylight lighting, and Lương 0. On the fresh fixture these read "Ban ngày" and 118–119, so they came from fixture ageing. The night label versus day lighting still needs a check once time-of-day lighting is implemented.

## Functional QA matrix still to run (after the art pass)
Server connection · world sync · resource changes · building selection · placement · upgrade · harvesting · worker movement · disciple actions · decision forms · Grand Hall data · save/load (Mongo restart) · UI navigation · camera. For each item, record separately: renders / correct content / action works / state persists.
