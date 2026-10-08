$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskLab = Join-Path $taskRoot 'private/subscribed-lab'
$taskOriginalLab = Join-Path $taskRoot 'private/lab'
$taskMts = 'C:/Program Files (x86)/Steam/steamapps/workshop/content/646570/1605060445/ModTheSpire.jar'
if (-not (Test-Path -LiteralPath $taskMts)) { throw 'Subscribe to and download ModTheSpire through Steam first.' }
New-Item -ItemType Directory -Path "$taskLab/mods" -Force | Out-Null
if (-not (Test-Path -LiteralPath "$taskLab/desktop-1.0.jar")) {
    Copy-Item -LiteralPath "$taskOriginalLab/desktop-1.0.jar" -Destination $taskLab
    Copy-Item -LiteralPath "$taskOriginalLab/jre" -Destination $taskLab -Recurse
    Copy-Item -LiteralPath "$taskOriginalLab/preferences" -Destination $taskLab -Recurse
}
if (Get-ChildItem -LiteralPath "$taskLab/mods" -File -Filter '*.jar') { throw 'This test requires an empty local mods directory.' }
Set-Content -LiteralPath "$taskLab/steam_appid.txt" -Value '646570' -Encoding ascii -NoNewline
$env:HEIR_DATA_DIR = Join-Path $taskRoot 'private/subscribed-proof-data'
$env:HEIR_TEST_MODE = '1'
$env:SteamAppId = '646570'
$taskProcess = Start-Process -FilePath "$taskLab/jre/bin/java.exe" -ArgumentList @('-Xmx1G','-jar',('"'+$taskMts+'"'),'--skip-launcher','--skip-intro','--mods','basemod,heir') -WorkingDirectory $taskLab -RedirectStandardOutput "$taskLab/launch.log" -RedirectStandardError "$taskLab/error.log" -WindowStyle Hidden -PassThru
Set-Content -LiteralPath "$taskRoot/private/subscribed-test-pid.txt" -Value $taskProcess.Id
Write-Output ('Subscribed-only game test PID: ' + $taskProcess.Id)
