# Proposal: V6 candidate names and an isolated player benchmark (not implemented)

Status: for review. Nothing here changes the workflow or runner until approved; each part is a separate,
security-reviewed change.

## 1. V6-specific file names in the qa/* overlay

Today the trusted overlay (`.github/workflows/unity-windows-qa.yml`, step "Overlay allowlisted v5 files") only accepts
`*V5*` candidate paths, so the V6 release concept lives in the v5 files (`ReleaseStyle.Plume`). Minimal change:

| Allowlist entry | Now | Proposed |
|---|---|---|
| runtime scripts | `^Assets/Scripts/Presentation/SonMon/[A-Za-z0-9_]*V5[A-Za-z0-9_]*\.cs(\.meta)?$` | `...[A-Za-z0-9_]*V[56][A-Za-z0-9_]*\.cs(\.meta)?$` |
| editor scripts | `^Assets/Editor/[A-Za-z0-9_]*V5[A-Za-z0-9_]*\.cs(\.meta)?$` | `...V[56]...` |
| assets | `^Assets/(Art|Resources)/SonMon/VFX/L6v5(...)` | `L6v[56]`, same extension list (png/mat/prefab/asset/meta) |
| texture tools | `^Tools/ShaderGraph/[A-Za-z0-9_]*v5[A-Za-z0-9_]*\.py$` | `v[56]` |
| reports | `^Reports/ForgeGUI/scripture_L06/spirit_v5(...)` | `spirit_v[56]` |

Plus, in `Tools/QA/unity_qa.ps1`: a mode `l6-visual-v6` (`$Fx = 'v6'`) whose allowlisted methods are
`SpiritV6Builder.Build` and `StageSiteCapture.RunL6SpiritV6`, shots `v6_*`, previews to `qa-results` `v6/run-<n>/`.

Security review:
- Same rules, one more candidate generation: code still only as `*V6*.cs` script files; assets still limited to data
  types Unity never executes; no deletions, renames, symlinks, submodules; case-sensitive; exact 40-hex commit;
  non-admin runner; read-only token; hook/fsmonitor cleanup. No path outside `L6v6` / `spirit_v6` / `*V6*` opens up.
- Not weakened: the method allowlist grows by two named methods, nothing generic (no wildcard execute).
- Migration: the validated candidate stays as is (V6 inside v5 files, `qa/l6-v6`). Renaming to `*V6*` files is
  optional and only worth doing together with production integration, so the validated commit is not put at risk.

## 2. Player benchmark without touching protected QA assets

The existing `StageSiteCapture.BuildL6SpiritQAPlayer` (not on the runner allowlist) writes the QA scene and
materials into tracked `Assets/QA/L6SpiritQA/`, and builds to `../../work/qa_builds` (outside the workspace).
`Assets/QA/L6SpiritQA/L6SpiritBenchmark.cs` only knows v3/v4 (`-fxver 3|4`).

Proposed isolated mechanism (one reviewed PR to main, then a `benchmark-v5` runner mode):
1. New editor method `StageSiteCapture.BuildL6SpiritQAPlayerIsolated(outDir)`: same scene contents, but scene and
   generated materials go to a git-ignored temp folder `Assets/QA/_isolated_build/` (added to `.gitignore`), which
   is deleted after the build; `EditorBuildSettings` and every tracked asset untouched; `outDir` must be under
   `$RUNNER_TEMP`.
2. `L6SpiritBenchmark.cs`: add `-fxver 5` (SonMonSpiritGatheringV5, style from `-fxstyle waves|plume`).
3. `unity_qa.ps1` mode `benchmark-v5`: build the isolated player, run it windowed with
   `-qaout $OutDir\benchmark.csv -fxver 5 -fxstyle plume` for the existing 30 / 60 / uncapped matrix (timeout), then
   delete the player folder; the CSV and a short summary go to the artifact and the qa-results previews.
4. The workflow's `Restore working tree` step already removes untracked, non-ignored files; the temp folder is
   ignored, so the method deletes it itself and the restore step checks it is gone.

Security review: the player is built from main + the overlaid candidate files only (same trust as the editor
capture); output stays in `$RUNNER_TEMP`; no tracked QA scene or production scene is written; the runner account
stays non-admin; no network or server is involved (the benchmark scene does not boot the game).

Until this is approved and merged, the V6 player benchmark is **not tested**; the v4/v3 player numbers in
`Reports/ForgeGUI/scripture_L06/spirit_v4/perf/` remain the only player measurements.
