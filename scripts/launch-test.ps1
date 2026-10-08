$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskLab = Join-Path $taskRoot 'private/lab'
$env:HEIR_DATA_DIR = Join-Path $taskRoot 'private/runtime'
$env:HEIR_TEST_MODE = '1'
Copy-Item -LiteralPath "$taskRoot/dist/HeirOfTheSpire.jar" -Destination "$taskLab/mods/HeirOfTheSpire.jar" -Force
$taskProcess = Start-Process -FilePath "$taskLab/jre/bin/java.exe" -ArgumentList @('-Xmx1G','-jar','ModTheSpire.jar','--skip-launcher','--skip-intro','--mods','basemod,heir') -WorkingDirectory $taskLab -RedirectStandardOutput "$taskLab/launch.log" -RedirectStandardError "$taskLab/error.log" -WindowStyle Hidden -PassThru
Set-Content -LiteralPath "$taskRoot/private/test-pid.txt" -Value $taskProcess.Id
Write-Output ('Game test PID: ' + $taskProcess.Id)
