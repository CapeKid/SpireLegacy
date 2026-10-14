throw 'Retired StS1 entry point. Use sts2/SpireLegacy.csproj and scripts/sts2-package.py; see docs/historical-sts1-audit.md.'
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskLab = Join-Path $taskRoot 'private/lab'
$taskRuntime = Join-Path $taskLab 'mods/SpireLegacyRuntime'
$taskSource = Join-Path $taskRoot 'build/workshop/content/SpireLegacyRuntime'
if (-not (Test-Path -LiteralPath $taskRuntime)) { New-Item -ItemType Junction -Path $taskRuntime -Target $taskSource | Out-Null }
Copy-Item -LiteralPath "$taskRoot/build/workshop/content/HeirOfTheSpire.jar" -Destination "$taskLab/mods/HeirOfTheSpire.jar" -Force
$env:HEIR_DATA_DIR = Join-Path $taskRoot 'private/workshop-game-data'
$env:HEIR_TEST_MODE = '1'
# Deliberately call only the native mod loader. No content-reader launcher runs first.
$taskProcess = Start-Process -FilePath "$taskLab/jre/bin/java.exe" -ArgumentList @('-Xmx1G','-jar','ModTheSpire.jar','--skip-launcher','--skip-intro','--mods','basemod,heir') -WorkingDirectory $taskLab -RedirectStandardOutput "$taskLab/workshop-launch.log" -RedirectStandardError "$taskLab/workshop-error.log" -WindowStyle Hidden -PassThru
Set-Content -LiteralPath "$taskRoot/private/workshop-test-pid.txt" -Value $taskProcess.Id
Write-Output ('Workshop-layout game test PID: ' + $taskProcess.Id)
