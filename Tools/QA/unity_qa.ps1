<#
Unity QA runner for the Windows self-hosted GitHub Actions runner (docs/UNITY_QA_RUNNER.md).
Runs only allowlisted, read-only QA editor methods in batchmode and writes a short summary plus selected outputs to
-OutDir. Never builds a player, starts a server or touches a database. Windows PowerShell 5.1 compatible.

  powershell -File Tools\QA\unity_qa.ps1 -Mode smoke|compile-shader|l6-visual [-OutDir <dir>] [-ProjectPath <dir>] [-ResetPackageCache]

-ResetPackageCache deletes only Library/PackageCache, Library/ScriptAssemblies and Library/Bee (package sources and
compile outputs, regenerated from the Editor install and registry on the next open). Imported assets
(Library/Artifacts) are kept. Use it once when package compile errors suggest a stale or corrupt package copy.

Modes:
  smoke           ProjectBuild.RunChecks (script compile + offline domain/save checks; -nographics)
  compile-shader  smoke + SpiritShaderGraphCheck.Run (Level 6 Shader Graph import/compile report)
  l6-visual       compile-shader + StageSiteCapture.RunL6SpiritV4 (needs a real GPU session)
Exit code: 0 all run steps passed, 1 a step failed, 2 setup error (Unity, license, lock).
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('smoke', 'compile-shader', 'l6-visual')][string]$Mode,
    [string]$OutDir = (Join-Path $env:TEMP 'unity-qa'),
    [string]$ProjectPath = $(if ($env:GITHUB_WORKSPACE) { $env:GITHUB_WORKSPACE } else { (Resolve-Path "$PSScriptRoot\..\..").Path }),
    [switch]$ResetPackageCache
)
$ErrorActionPreference = 'Stop'
$UnityVersion = '6000.4.3f1'
$StepTimeoutMin = @{ checks = 30; shader = 20; visual = 45 }
# The only editor methods this runner may execute.
$Allowed = @('ProjectBuild.RunChecks', 'SpiritShaderGraphCheck.Run', 'StageSiteCapture.RunL6SpiritV4')
$V4Out = 'Reports/ForgeGUI/scripture_L06/spirit_v4/unity'

$ProjectPath = (Resolve-Path $ProjectPath).Path
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path (Join-Path $OutDir 'logs') | Out-Null
# Full Unity logs stay outside the artifact folder: Unity's helper processes can keep them open after it exits.
$FullLogDir = "$OutDir-full-logs"
New-Item -ItemType Directory -Force -Path $FullLogDir | Out-Null
Get-ChildItem $FullLogDir -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
$RunStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$sha = (git -C $ProjectPath rev-parse HEAD).Trim()
$result = [ordered]@{
    commit = $sha; mode = $Mode; unity = $UnityVersion; editor = ''; cacheReset = [bool]$ResetPackageCache; renderer = ''
    compile = 'NOT RUN'; checks = ''; shaderGraph = 'NOT RUN'; visual = 'NOT RUN'
    screenshots = @(); gifs = @(); previews = 'none'; benchmarks = 'NOT RUN (player benchmark is not automated)'; errors = @(); warnings = @()
}

function Write-Summary {
    $md = @(
        "## Unity QA: $($result.mode)", '',
        "| | |", "|---|---|",
        "| Commit | ``$($result.commit)`` |", "| Mode | $($result.mode) |", "| Unity | $($result.unity) |",
        "| Editor | $($result.editor) |", "| Package cache reset | $($result.cacheReset) |",
        "| GPU renderer | $($result.renderer) |",
        "| Compile + checks | $($result.compile) $($result.checks) |", "| Shader Graph | $($result.shaderGraph) |",
        "| Visual QA | $($result.visual) |", "| Benchmark | $($result.benchmarks) |", '',
        "**Screenshots:** $(if ($result.screenshots.Count) { ($result.screenshots -join ', ') } else { 'none' })", '',
        "**GIF:** $(if ($result.gifs.Count) { ($result.gifs -join ', ') } else { 'none' })", '',
        "**Previews:** $($result.previews)", '',
        '**Errors:**'
    )
    if ($result.errors.Count) { $md += ($result.errors | Select-Object -First 30 | ForEach-Object { "- $_" }) } else { $md += '- none' }
    if ($result.warnings.Count) { $md += @('', '**Warnings:**') + ($result.warnings | Select-Object -First 10 | ForEach-Object { "- $_" }) }
    $text = $md -join "`n"
    Set-Content -Path (Join-Path $OutDir 'qa-summary.md') -Value $text -Encoding UTF8
    $result | ConvertTo-Json -Depth 4 | Set-Content -Path (Join-Path $OutDir 'qa-summary.json') -Encoding UTF8
    if ($env:GITHUB_STEP_SUMMARY) { Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $text -Encoding UTF8 }
    Write-Host $text
}

function Stop-Setup([string]$message) {
    $result.errors += $message
    Write-Summary
    exit 2
}

# ---------------------------------------------------------------- setup checks
$pv = Get-Content (Join-Path $ProjectPath 'ProjectSettings/ProjectVersion.txt') -Raw
if ($pv -notmatch "m_EditorVersion: $([regex]::Escape($UnityVersion))") { Stop-Setup "Project is not on Unity $UnityVersion" }
$Revision = if ($pv -match 'm_EditorVersionWithRevision: \S+ \((\w+)\)') { $Matches[1] } else { '' }

$unity = $env:UNITY_EDITOR_PATH
if (-not $unity) {
    $candidates = @("$env:ProgramFiles\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe", "$env:ProgramFiles\Unity $UnityVersion\Editor\Unity.exe")
    $unity = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $unity -or -not (Test-Path $unity)) { Stop-Setup "Unity $UnityVersion Editor not found (set UNITY_EDITOR_PATH on the runner)" }
$ver = (Get-Item $unity).VersionInfo.ProductVersion
$result.editor = "$unity ($(if ($ver) { $ver } else { 'no file version' }))"
if ($ver -and $ver -notlike "$UnityVersion*") { Stop-Setup "Unity at $unity reports version $ver, expected $UnityVersion" }
if ($ver -and $Revision -and $ver -notlike "*$Revision*") { Stop-Setup "Unity at $unity is $ver, expected revision $Revision" }

# One Unity QA job per machine, and never a second Editor on this project.
$mutex = New-Object System.Threading.Mutex($false, 'Global\LangTroTanUnityQA')
$locked = $false
try { $locked = $mutex.WaitOne(0) } catch [System.Threading.AbandonedMutexException] { $locked = $true }
if (-not $locked) { Stop-Setup 'Another Unity QA run holds the machine lock' }
$open = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine -like "*$ProjectPath*" }
if ($open) { Stop-Setup "A Unity Editor already has this project open (PID $($open.ProcessId -join ', '))" }
if (Test-Path (Join-Path $ProjectPath 'Temp/UnityLockfile')) {
    try { Remove-Item (Join-Path $ProjectPath 'Temp/UnityLockfile') -Force } catch { Stop-Setup 'Project lock file is held by another process' }
}
if ($ResetPackageCache) {
    # Only inside this project's Library, never through a junction or symlink (PowerShell 5.1 would delete the target).
    $library = Join-Path $ProjectPath 'Library'
    if ((Test-Path $library) -and ((Get-Item $library -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { Stop-Setup 'Library is a link; refusing to reset it' }
    foreach ($name in @('PackageCache', 'ScriptAssemblies', 'Bee')) {
        $dir = Join-Path $library $name
        if (-not (Test-Path $dir)) { continue }
        $item = Get-Item $dir -Force
        if (-not $item.FullName.StartsWith("$ProjectPath\Library\", [StringComparison]::OrdinalIgnoreCase)) { Stop-Setup "Refusing to delete $($item.FullName)" }
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { Stop-Setup "Library/$name is a link; refusing to delete it" }
        $links = @(Get-ChildItem $dir -Recurse -Force -Attributes ReparsePoint -ErrorAction SilentlyContinue)
        if ($links.Count) { Stop-Setup "Library/$name contains links ($($links[0].FullName)); refusing to delete it" }
        Write-Host "Removing Library/$name"
        Remove-Item $dir -Recurse -Force
    }
}

# ---------------------------------------------------------------- Unity runner
function Invoke-Unity([string]$method, [string]$name, [int]$timeoutMin, [bool]$graphics) {
    if ($Allowed -notcontains $method) { throw "Method $method is not allowlisted" }
    $log = Join-Path $FullLogDir "$name-$RunStamp.log"
    $unityArgs = @('-batchmode', '-quit', '-projectPath', "`"$ProjectPath`"", '-executeMethod', $method, '-logFile', "`"$log`"")
    if (-not $graphics) { $unityArgs += '-nographics' }
    Write-Host "Unity: $method ($name, timeout $timeoutMin min)"
    $p = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -NoNewWindow
    $null = $p.Handle  # keeps ExitCode readable after exit
    if (-not $p.WaitForExit($timeoutMin * 60000)) {
        try { $p.Kill() } catch {}
        $result.errors += "${name}: timed out after $timeoutMin min"
        return @{ exit = -1; log = $log; text = '' }
    }
    $text = if (Test-Path $log) { Get-Content $log -Raw } else { '' }
    # Keep a 300-line tail and the key lines for the artifact; the full log stays in $FullLogDir on the runner.
    if (Test-Path $log) {
        $lines = Get-Content $log
        $lines | Select-Object -Last 300 | Set-Content (Join-Path $OutDir "logs/$name.tail.log") -Encoding UTF8
        $lines | Select-String -Pattern 'Unity Editor version|Built from|Command line arguments|-nographics|Package Manager|Registered \d+ packages|PackageCache|BuiltInPackages|error CS\d+|Compilation failed|compiler errors|Exception|FAIL|SG_CHECK|STAGE_SITE|license|Licens|GfxDevice|Renderer:' |
            Select-Object -First 300 | ForEach-Object { $_.Line } | Set-Content (Join-Path $OutDir "logs/$name.key.log") -Encoding UTF8
        # The 40 lines before the first compiler error (package resolution and compile context).
        $first = $lines | Select-String -Pattern 'error CS\d+' | Select-Object -First 1
        if ($first) {
            $from = [Math]::Max(0, $first.LineNumber - 41)
            $lines[$from..($first.LineNumber - 1)] | Set-Content (Join-Path $OutDir "logs/$name.first-error.log") -Encoding UTF8
        }
    }
    if ($text -match 'No valid Unity Editor license|License is not active|Failed to activate|ULF license') {
        $result.errors += "${name}: Unity license problem (see logs/$name.key.log)"
    }
    # Compiler errors in project code always fail a step. Errors inside Library/PackageCache are logged by Unity 6.4
    # while a fresh Library is first imported (CS0246 'GUID' in com.unity.shadergraph) even though the Editor then
    # compiles, runs the method and exits 0; the step decides from its exit code and outputs, and they are listed.
    $cs = @([regex]::Matches($text, '(?m)^.*error CS\d+.*$') | ForEach-Object { $_.Value.Trim() } | Select-Object -Unique)
    $projectCs = @($cs | Where-Object { $_ -notmatch 'Library[\\/]PackageCache[\\/]' })
    $packageCs = @($cs | Where-Object { $_ -match 'Library[\\/]PackageCache[\\/]' })
    foreach ($e in ($projectCs | Select-Object -First 10)) { $result.errors += "${name}: $e" }
    foreach ($e in ($packageCs | Select-Object -First 5)) { $result.warnings += "${name} (package, logged during import): $e" }
    if ($text -match 'Renderer:\s*(.+)') { $result.renderer = $Matches[1].Trim() }
    if (-not $graphics -and $result.renderer -match 'Null') { $result.renderer += ' (expected with -nographics)' }
    if ($text -match 'Unity Editor version:\s+(\S+)\s*\((\w+)\)') {
        if ($Matches[1] -ne $UnityVersion -or ($Revision -and $Matches[2] -ne $Revision)) {
            $result.errors += "${name}: log reports Unity $($Matches[1]) ($($Matches[2])), project expects $UnityVersion ($Revision)"
        }
    }
    # Which package copies were compiled (folder names only).
    $pc = Join-Path $ProjectPath 'Library/PackageCache'
    if (Test-Path $pc) {
        Get-ChildItem $pc -Directory | Where-Object { $_.Name -match 'shadergraph|render-pipelines|searcher' } |
            ForEach-Object { "$($_.Name)  $($_.LastWriteTime.ToString('s'))" } | Set-Content (Join-Path $OutDir "logs/packagecache.txt") -Encoding UTF8
    }
    return @{ exit = $p.ExitCode; text = $text; projectCs = $projectCs.Count; packageCs = $packageCs.Count }
}

function Fresh([string]$rel, [datetime]$since) {
    $f = Join-Path $ProjectPath $rel
    return (Test-Path $f) -and ((Get-Item $f).LastWriteTime -ge $since)
}

$failed = $false

# ---------------------------------------------------------------- smoke: compile + offline checks
$t0 = Get-Date
$r = Invoke-Unity 'ProjectBuild.RunChecks' 'checks' $StepTimeoutMin.checks $false
$pass = ([regex]::Matches($r.text, '(?m)^PASS ')).Count
if ($r.exit -eq 0 -and (Fresh 'Reports/Checks.txt' $t0) -and $r.projectCs -eq 0) {
    $result.compile = 'PASS'; $result.checks = "($pass PASS lines)"
    Copy-Item (Join-Path $ProjectPath 'Reports/Checks.txt') (Join-Path $OutDir 'Checks.txt')
} else {
    $result.compile = 'FAIL'; $failed = $true
    $fail = [regex]::Match($r.text, 'FAIL: [^\r\n]+'); if ($fail.Success) { $result.errors += "checks: $($fail.Value)" }
    if ($r.exit -ne 0) { $result.errors += "checks: Unity exit code $($r.exit)" }
    if (-not (Fresh 'Reports/Checks.txt' $t0)) { $result.errors += 'checks: Reports/Checks.txt was not written' }
    if ($r.packageCs) { $result.errors += "checks: $($r.packageCs) package compiler error(s), see Warnings" }
}

# ---------------------------------------------------------------- Shader Graph compile report
if (-not $failed -and $Mode -ne 'smoke') {
    $r = Invoke-Unity 'SpiritShaderGraphCheck.Run' 'shadergraph' $StepTimeoutMin.shader $true
    $sg = [regex]::Matches($r.text, '(?m)^SG_CHECK (\S+) (.*)$')
    $expected = (Get-ChildItem (Join-Path $ProjectPath 'Assets/Art/SonMon/VFX/L6/Shaders') -Filter *.shadergraph).Count
    $bad = @($sg | Where-Object { $_.Groups[2].Value -match 'FAILED|errors=True|supported=False' })
    $sgErrors = @([regex]::Matches($r.text, '(?m)^SG_CHECK_MSG (\S+) Error .*$') | ForEach-Object { $_.Value.Trim() })
    Set-Content (Join-Path $OutDir 'shadergraph.txt') -Value (($sg | ForEach-Object { $_.Value.Trim() }) -join "`n") -Encoding UTF8
    if ($r.exit -eq 0 -and $sg.Count -eq $expected -and $expected -gt 0 -and $bad.Count -eq 0 -and $sgErrors.Count -eq 0) {
        $result.shaderGraph = "PASS ($expected graphs)"
    } else {
        $result.shaderGraph = 'FAIL'; $failed = $true
        $result.errors += "shadergraph: $($sg.Count)/$expected graphs reported, $($bad.Count) failing, exit $($r.exit)"
        foreach ($b in ($bad | Select-Object -First 5)) { $result.errors += "shadergraph: $($b.Value.Trim())" }
        foreach ($e in ($sgErrors | Select-Object -First 5)) { $result.errors += "shadergraph: $e" }
    }
}

# ---------------------------------------------------------------- Level 6 v4 visual capture
if (-not $failed -and $Mode -eq 'l6-visual') {
    $t0 = Get-Date
    $r = Invoke-Unity 'StageSiteCapture.RunL6SpiritV4' 'l6visual' $StepTimeoutMin.visual $true
    $shots = @('v4_full_hero', 'v4_full_45', 'v4_full_front',
        'v4_off_gameplay_zoom10', 'v4_reduced_gameplay_zoom10', 'v4_full_gameplay_zoom10',
        'v4_off_gameplay_zoom21_default', 'v4_reduced_gameplay_zoom21_default', 'v4_full_gameplay_zoom21_default',
        'v4_off_gameplay_zoom30', 'v4_reduced_gameplay_zoom30', 'v4_full_gameplay_zoom30',
        'v4_full_close_mist', 'v4_full_close_formation', 'v4_full_close_glyphs', 'v4_full_close_ornament', 'v4_full_close_pillar_formation')
    $shotDir = Join-Path $OutDir 'screenshots'; New-Item -ItemType Directory -Force -Path $shotDir | Out-Null
    $missing = @()
    foreach ($s in $shots) {
        if (Fresh "$V4Out/$s.png" $t0) { Copy-Item (Join-Path $ProjectPath "$V4Out/$s.png") $shotDir; $result.screenshots += "screenshots/$s.png" }
        else { $missing += $s }
    }
    $frames = @(Get-ChildItem (Join-Path $ProjectPath "$V4Out/cycle") -Filter 'frame_*.png' -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $t0 })
    if (Fresh "$V4Out/measurements.csv" $t0) { Copy-Item (Join-Path $ProjectPath "$V4Out/measurements.csv") (Join-Path $OutDir 'measurements.csv') }
    $done = $r.text -match 'STAGE_SITE_CUSTOM_DONE'
    if ($r.exit -eq 0 -and $done -and $missing.Count -eq 0 -and $frames.Count -ge 100 -and (Fresh "$V4Out/measurements.csv" $t0)) {
        $result.visual = 'PASS (captured; visual approval is manual)'
    } else {
        $result.visual = 'FAIL'; $failed = $true
        if (-not $done) { $result.errors += 'l6visual: STAGE_SITE_CUSTOM_DONE not logged' }
        if ($missing.Count) { $result.errors += "l6visual: missing screenshots: $($missing -join ', ')" }
        if ($frames.Count -lt 100) { $result.errors += "l6visual: $($frames.Count) cycle frames (expected 101)" }
        if ($r.exit -ne 0) { $result.errors += "l6visual: Unity exit code $($r.exit)" }
    }
    if ($result.renderer -match 'Basic Render|WARP|llvmpipe|Software') {
        $result.errors += "l6visual: software renderer ($($result.renderer)); captures are not representative"
        $result.visual += ' (software renderer)'
    }
    # Compact previews (contact sheet, cycle phases, 10 s GIF, key JPEGs) for review without the full artifact; the
    # workflow publishes them to the qa-results branch. numpy/Pillow come from a project-scoped venv
    # (Tools/QA/.venv, git-ignored) with hash-pinned binary wheels only. Optional: failures are warnings, and Python
    # writing to stderr must not abort the run (PowerShell 5.1 + ErrorActionPreference Stop).
    if ($frames.Count -ge 100) {
        $previewDir = Join-Path $OutDir 'previews'
        $previewLog = Join-Path $OutDir 'logs/previews.log'
        $venv = Join-Path $ProjectPath 'Tools/QA/.venv'
        $venvPy = Join-Path $venv 'Scripts/python.exe'
        $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try {
            if (-not (Test-Path $venvPy)) {
                $py = Get-Command py -ErrorAction SilentlyContinue
                $pyArgs = @('-3')
                if (-not $py) { $py = Get-Command python -ErrorAction SilentlyContinue; $pyArgs = @() }
                if ($py) { & $py.Source @pyArgs -m venv $venv 2>&1 | ForEach-Object { "$_" } | Add-Content $previewLog -Encoding UTF8 }
            }
            if (Test-Path $venvPy) {
                & $venvPy -m pip install --disable-pip-version-check --no-input --require-hashes --only-binary=:all: `
                    -r (Join-Path $ProjectPath 'Tools/QA/requirements-qa.txt') 2>&1 | ForEach-Object { "$_" } | Add-Content $previewLog -Encoding UTF8
                & $venvPy -I (Join-Path $ProjectPath 'Tools/QA/make_previews.py') $shotDir (Join-Path $ProjectPath "$V4Out/cycle") `
                    $previewDir (Join-Path $OutDir 'measurements.csv') 2>&1 | ForEach-Object { "$_" } | Add-Content $previewLog -Encoding UTF8
            } else { 'Python not found on the runner' | Add-Content $previewLog -Encoding UTF8 }
        } catch { "$_" | Add-Content $previewLog -Encoding UTF8 }
        finally { $ErrorActionPreference = $eap }
        if ((Test-Path (Join-Path $previewDir 'contact_sheet.jpg')) -and (Test-Path (Join-Path $previewDir 'cycle_10s.gif'))) {
            $result.gifs += 'previews/cycle_10s.gif'
            $result.previews = 'previews/ (contact_sheet.jpg, cycle_phases.jpg, cycle_10s.gif, key/*.jpg, cycle_stats.txt)'
        } else {
            $last = Get-Content $previewLog -ErrorAction SilentlyContinue | Where-Object { $_ -match '\S' } | Select-Object -Last 1
            $result.warnings += "previews: not generated ($last); see logs/previews.log"
        }
    }
}

Write-Summary
$mutex.ReleaseMutex()
if ($failed) { exit 1 } else { exit 0 }
