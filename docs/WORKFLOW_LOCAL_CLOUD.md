# Local vs Cloud workflow

## Routing
| Claude Code **Local** (Windows, Unity installed) | Claude **Cloud** session (no Unity) |
|---|---|
| Unity Editor, Shader Graph, Particle System | C# static review |
| Models, materials, animation, VFX | Architecture / refactor plans |
| Play Mode / Edit Mode tests, editor checks | Documentation |
| Windows builds, QA players | Unit tests that don't need Unity |
| Screenshots, GPU benchmarks | Data structures, configuration, Python tooling |
| Visual approval (incl. L6 Tàng Kinh Các) | Reviewing reports and committed screenshots |
| Anything touching the local backend/Mongo | Small, isolated PRs |

A Cloud change to C# that Unity hasn't compiled is **unverified**: mark the PR as such and run the relevant check locally before merging.

## Every task (token budget)
1. Read the task and `CLAUDE.md`; don't re-explore the repo.
2. Open only the affected files (grep for symbols, read ranges). Don't scan `Reports/` or re-read long reports.
3. Make the smallest safe change.
4. Run only the relevant check first; widen only if it fails or dependencies require it.
5. Summarize results (counts, PASS/FAIL, failing lines). Store full logs as files; don't paste them.
6. Keep replies short; no plan documents for simple fixes.

## Git
- Before editing: `git status` (clean), `git log -1 --oneline`, note the affected files.
- Cloud work goes on a short-lived feature branch; one focused commit or PR, only when requested.
- Never force-push `main`; don't merge Unity changes that haven't been verified locally.
- Don't commit `Library/`, `Temp/`, builds, QA players, logs or capture frame folders. Keep contact sheets, GIFs and CSVs only when they are the review evidence.

## QA commands (Local)
`U` = `"C:/Program Files/Unity/Hub/Editor/6000.4.3f1/Editor/Unity.exe"`, `P` = project path. Add `-logFile <file>` and read it with `grep`, not in full. Capture runs need graphics (no `-nographics`). Close the Editor on the project before batchmode.

| Purpose | Command |
|---|---|
| Domain + save checks (fast, no build) | `U -batchmode -projectPath P -executeMethod ProjectBuild.RunChecks -quit` |
| Full checks + Windows dev build (rewrites `Village.unity`, `BUILD_RESULT.txt`) | `U -batchmode -projectPath P -executeMethod ProjectBuild.Build -quit` |
| Visual-slice build (URP, kit, then build) | `U -batchmode -projectPath P -executeMethod VisualSliceBuild.Build -quit -visual-slice-build` |
| Visual-slice interaction regression (needs isolated server 8901) | `bash Tools/QA/run_visual_qa.sh <label>` (env: `TIER_LEVEL`, `TIER_UPGRADE_IN`, `PLAYER_ARGS`) |
| Full-port smoke against 8899 | `Builds/Windows_FullPort_Dev/LangTroTan.exe -full-port-smoke` (also `-spatial-smoke`) |
| Tier / level checks in player | `LangTroTan.exe -visual-qa -visual-tiers -tier-only` or `-tier-live` |
| Shader Graph compile check (L6) | `U -batchmode -projectPath P -executeMethod SpiritShaderGraphCheck.Run -quit` → `grep SG_CHECK` |
| Regenerate L6 v4 particle prefabs | `U -batchmode -projectPath P -executeMethod SpiritV4Builder.Build -quit` (only when asked) |
| L6 v4 editor captures | `U -batchmode -projectPath P -executeMethod StageSiteCapture.RunL6SpiritV4 -quit` |
| L6 isolated QA player | `U -batchmode -projectPath P -executeMethod StageSiteCapture.BuildL6SpiritQAPlayer -quit -qabuild <dir outside repo>` |
| L6 benchmark / motion capture | `<qa player>.exe -qaout <csv> [-fxver 3\|4] [-qacapture <dir>]` |
| Loop smoothness of capture frames (Cloud OK) | `python Tools/ShaderGraph/cycle_report.py <frames dir> <gif out> <fps> [seam]` |
| Other stage captures | `StageSiteCapture.Run`, `.RunL4/L5/L6…`, `ScaleCompareCapture.Run` (see each method's `// Run:` comment) |

## QA summary (paste this, not logs)
```
Task:
Commit:
Files changed:
Tests: PASS / FAIL / NOT RUN (which, counts)
Visual QA: (screenshots: paths only)
Performance: (mean / p95 ms, GPU ms, SetPass, particles)
Known issues:
Next action:
```

## Automation
No self-hosted runners, cloud infrastructure, paid services or new dependencies without approval. Prefer the existing scripts above; propose any new automation with its setup and maintenance cost first.
