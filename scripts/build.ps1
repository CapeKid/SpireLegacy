$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
& "$taskRoot/tools/python/Scripts/python.exe" "$PSScriptRoot/generate.py"
if ($LASTEXITCODE -ne 0) { throw 'Sheet preflight/generation failed.' }
$taskJdk = (Get-ChildItem "$taskRoot/tools/jdk" -Directory | Select-Object -First 1).FullName
$taskGame = 'C:\Program Files (x86)\Steam\steamapps\common\SlayTheSpire'
$taskCp = "$taskGame/desktop-1.0.jar;$taskRoot/tools/ModTheSpire.jar;$taskRoot/tools/BaseMod.jar"
New-Item -ItemType Directory -Force -Path "$taskRoot/build/classes" | Out-Null
$taskSources = Get-ChildItem "$taskRoot/src/main/java" -Recurse -Filter '*.java' | ForEach-Object { '"' + $_.FullName.Replace('\','/') + '"' }
Set-Content -LiteralPath "$taskRoot/build/sources.txt" -Value $taskSources -Encoding ascii
& "$taskJdk/bin/javac.exe" -encoding UTF-8 -source 8 -target 8 -cp $taskCp -d "$taskRoot/build/classes" "@$taskRoot/build/sources.txt"
if ($LASTEXITCODE -ne 0) { throw 'Java compilation failed.' }
Copy-Item "$taskRoot/src/main/resources/*" "$taskRoot/build/classes" -Recurse -Force
& "$taskJdk/bin/jar.exe" cf "$taskRoot/dist/HeirOfTheSpire.jar" -C "$taskRoot/build/classes" .
if ($LASTEXITCODE -ne 0) { throw 'Jar packaging failed.' }
Write-Output 'Built dist/HeirOfTheSpire.jar'
