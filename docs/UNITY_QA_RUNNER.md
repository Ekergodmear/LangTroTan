# Unity QA on the Windows self-hosted runner

Loop: Claude Cloud (or you) dispatches **Unity Windows QA** on `main` → the Windows runner runs `Tools/QA/unity_qa.ps1` → the summary is shown in the job's step summary and log, and the outputs are uploaded as a 7-day artifact.

Workflow: `.github/workflows/unity-windows-qa.yml`. Manual (`workflow_dispatch`) only, `main` only, one job at a time, 90 min timeout, `contents: read`.

| Mode | Runs | Needs GPU |
|---|---|---|
| `smoke` | `ProjectBuild.RunChecks`: script compile + offline domain/save checks (writes only `Reports/*Checks.txt` and test saves under `Reports/`, restored after the job) | no (`-nographics`) |
| `compile-shader` | smoke + `SpiritShaderGraphCheck.Run`: PASS only if every L6 graph reports `errors=False supported=True` with no error messages | yes |
| `l6-visual` | compile-shader + `StageSiteCapture.RunL6SpiritV4`: 17 key screenshots, `measurements.csv`, 101 cycle frames → previews (contact sheet, cycle phases, 10 s GIF, key JPEGs) published to the `qa-results` branch | yes |

Not automated: `ProjectBuild.Build` (rewrites `Village.unity`, build settings, `BUILD_RESULT.txt`), `BuildL6SpiritQAPlayer` and the player benchmark (rewrites `Assets/QA` assets, long build), and anything server- or Mongo-backed.

Artifact: `qa-summary.md` / `.json`, `logs/*.tail.log` (last 300 lines) and `logs/*.key.log` (errors, `SG_CHECK`, renderer), `Checks.txt`, `shadergraph.txt`, `screenshots/*.png`, `measurements.csv`, `previews/`. Nothing is committed to `main`.

**Previews on `qa-results`** (for Claude Cloud, which cannot download artifact zips): after an l6-visual run, a GitHub-hosted job copies only `contact_sheet.jpg`, `cycle_phases.jpg`, `cycle_10s.gif`, `cycle_stats.txt`, `measurements.csv` and `key/v4_*.jpg` (about 3 MB, capped at 15 MB) into `latest/` on the orphan branch `qa-results` and commits. Read them with `git fetch origin qa-results` and `git show origin/qa-results:latest/contact_sheet.jpg`. Never merge `qa-results`. If its history grows too large, delete the branch on GitHub; the next run recreates it.

## Setup (once)
1. **Dedicated Windows user** (e.g. `unityqa`, standard user, not admin). Log in as that user for the steps below.
2. Install Unity Hub and **Unity 6000.4.3f1** (Windows Build Support not needed). Sign in to Hub and activate a licence for this user (Personal is fine). If Unity is not in `C:\Program Files\Unity\Hub\Editor\6000.4.3f1`, set a user environment variable `UNITY_EDITOR_PATH` to the full `Unity.exe` path.
3. Install Git for Windows and Python 3.10–3.14 with the `py` launcher (python.org installer). The QA script creates `Tools/QA/.venv` in the runner workspace and installs only `Tools/QA/requirements-qa.txt` (numpy, Pillow; pinned, hash-checked, binary wheels only). Nothing is installed globally.
4. GitHub → repository **Settings → Actions → Runners → New self-hosted runner → Windows x64**. Follow the shown download/config commands in a folder such as `C:\actions-runner` (not inside your dev project). During `config.cmd`:
   - runner name: e.g. `rx6600m-unity`
   - additional labels: `unity-qa`
   - work folder: default `_work`
   - **"Run as service?" → N** (see Limitations).
5. Repository **Settings → Actions → General**: keep "Fork pull request workflows" off (private repo default) and workflow permissions at "Read repository contents".

## Start the runner (each session)
Log in to Windows as `unityqa` (an interactive desktop session, so Unity gets the GPU), then:

```bat
cd C:\actions-runner
run.cmd
```

Leave the window open while QA is wanted; close it (Ctrl+C) when done. The runner shows as **Idle** under Settings → Actions → Runners.

## Smoke test
1. With the runner idle: GitHub → **Actions → Unity Windows QA → Run workflow**, branch `main`, mode `smoke`. (From the GitHub CLI: `gh workflow run unity-windows-qa.yml --ref main -f mode=smoke`.)
2. The first run imports the project into `_work\...\Library` (can take 20–40 min); later runs reuse it.
3. Expect: job green, step summary `Compile + checks | PASS (N PASS lines)`, artifact `unity-qa-smoke-<n>`.
4. Then run `compile-shader`, then `l6-visual`. Check that the summary's **GPU renderer** names the RX 6600M; a "Microsoft Basic Render Driver" means the session has no GPU and captures are invalid.
5. Failure triage: open the artifact's `logs/*.key.log` first; exit code 2 = setup (Unity path, licence, lock), 1 = a QA step failed.
   - `logs/checks.first-error.log` holds the 40 lines before the first compiler error; `logs/packagecache.txt` lists the compiled package copies; the summary's **Editor** row shows which `Unity.exe` ran.
   - Compile errors inside `Library/PackageCache` (e.g. `CS0246 'GUID'` in `com.unity.shadergraph`) on an unchanged project point at the Editor install or a stale package copy, not the game code. Check the Editor row and the log's `Unity Editor version` line match 6000.4.3f1 (39d1a88d4dd1), then re-run once with **reset_package_cache** ticked. It deletes only `Library/PackageCache`, `Library/ScriptAssemblies` and `Library/Bee`; imported assets are kept. If it still fails, repair or reinstall 6000.4.3f1 in Unity Hub.
   - `GPU renderer: Null Device` is expected in `smoke` (it runs with `-nographics`); only `compile-shader` and `l6-visual` must show the RX 6600M.

## Limitations
- Only `main` can be tested (deliberate for the first rollout). Feature branches need a reviewed change to the job's `if:`.
- `smoke` is also the compile check: if scripts don't compile, `RunChecks` cannot run and the job fails with the `error CS…` lines in the summary.
- The capture path has not been tested as a Windows service (session 0 has no interactive desktop; Unity may fall back to a software renderer or fail). Run the runner interactively until a service run is proven with `l6-visual`.
- Visual PASS means "captured and complete", not "approved": visual approval stays manual.
- Claude Cloud can dispatch and read job logs/step summaries; whether it can download artifact zips depends on its GitHub tools. The summary is printed in the log for that reason.
- The player benchmark (`perf/benchmark_v4.csv`) is still manual on the Local machine.

## Security
- **Anyone who can push to this repository can run code on this PC.** Editor scripts are repository code, and a branch can change the workflow itself; the `main`-only `if:` limits normal use, not a malicious writer. This includes Claude, which can push branches. Only keep collaborators (and apps) you trust with write access.
- Contain the blast radius: the dedicated `unityqa` user must not be able to read your dev project, `tu-tien-port/backend`, MongoDB data folders, protected saves or browser profiles; keep it non-admin; don't store other credentials in that profile.
- MongoDB on 27017 (protected saves) is reachable by any local process. The QA script never connects to it, but for real isolation enable MongoDB authentication on that instance.
- The workflow checks out with `persist-credentials: false`, never interpolates inputs into commands (inputs pass through validated environment variables), pins actions to commit SHAs and has read-only permissions. The only job with `contents: write` is `publish-previews`: it runs on a GitHub-hosted runner, executes no repository code (only the workflow's own shell), copies a fixed list of image/text files and pushes only to `qa-results`. Its `GITHUB_TOKEN` push cannot trigger workflows, so there is no trigger loop. The GitHub-hosted job uses a few Actions minutes from the account's free allowance per l6-visual run.
- Run the runner only while you want QA, and keep the repository private.
