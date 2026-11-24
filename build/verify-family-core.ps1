# Runs build/family-core/native-check.py in the isolated regular or beta lab (build/verify-170/<channel>)
# against one Release DLL, e.g.: pwsh build/verify-family-core.ps1 -Channel beta -ReleaseDll sts2/bin/Release/net9.0/SpireLegacy.dll
param([Parameter(Mandatory)][ValidateSet('regular','beta')][string]$Channel,[Parameter(Mandatory)][string]$ReleaseDll)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$lab=(Resolve-Path (Join-Path $PSScriptRoot "verify-170/$Channel")).Path
$source=if($Channel -eq 'regular'){Join-Path $root 'private/sts2-111-chinese'}else{'C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2'}
$dl=(Resolve-Path -LiteralPath $ReleaseDll).Path
$profile=Join-Path $lab ('profile-family-core-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $profile | Out-Null
$pck=Join-Path $lab 'SlayTheSpire2.pck'
if(Test-Path $pck){throw 'Expected a fresh test copy of the game PCK'}
Copy-Item -LiteralPath (Join-Path $source 'SlayTheSpire2.pck') -Destination $pck
try {
    $modId=if($Channel -eq 'beta'){'SpireLegacyBeta'}else{'SpireLegacy'}
    $mod=(Resolve-Path (Join-Path $lab "mods/$modId")).Path
    foreach($old in Get-Item -LiteralPath (Join-Path $mod "$modId.dll"),(Join-Path $mod "$modId.json")){
        if($old.LinkType){throw 'Expected independent copies of test mod files'}
    }
    Copy-Item -LiteralPath $dl -Destination (Join-Path $mod "$modId.dll")
    $manifest=Get-Content (Join-Path $root 'sts2/SpireLegacy.json') -Raw | ConvertFrom-Json
    $manifest.id=$modId
    $manifest | ConvertTo-Json -Depth 12 | Set-Content -Encoding utf8 (Join-Path $mod "$modId.json")
    $env:HEIR_TEST_MODE='1'
    $env:HEIR_TEST_ROOT=$lab
    $env:HEIR_DATA_DIR=$profile
    $exe=Join-Path $lab 'SlayTheSpire2.exe'
    $child=Start-Process -FilePath $exe -WorkingDirectory $lab -ArgumentList '--force-steam=off --rendering-method gl_compatibility --rendering-driver opengl3 --windowed --resolution 1280x800 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profile 'launch.log') -RedirectStandardError (Join-Path $profile 'errors.log')
    try {
        $deadline=(Get-Date).AddSeconds(40)
        do {
            if($child.HasExited -or (Get-Date) -gt $deadline){throw 'Isolated game failed to reach the main menu'}
            Start-Sleep -Milliseconds 300
        } until (Select-String -LiteralPath (Join-Path $profile 'launch.log') -SimpleMatch -Quiet -Pattern 'main menu loaded (complete)')
        & (Join-Path $root 'tools/python/Scripts/python.exe') -u (Join-Path $PSScriptRoot 'family-core/native-check.py') $Channel $profile $dl
        if($LASTEXITCODE -ne 0){throw "$Channel gameplay checks failed"}
    } finally {
        if(!$child.HasExited){Stop-Process -Id $child.Id; $child.WaitForExit(10000) | Out-Null}
    }
} finally {
    $proved=Get-Item -LiteralPath $pck
    if($proved.DirectoryName -eq $lab -and !$proved.LinkType){Remove-Item -LiteralPath $pck}
}



