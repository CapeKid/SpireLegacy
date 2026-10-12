param(
    [Parameter(Mandatory=$true)][ValidateSet('regular','beta')][string]$Branch,
    [Parameter(Mandatory=$true)][string]$Tag,
    [ValidateRange(1024,65530)][int]$Port=25001,
    [ValidateSet('all','party')][string]$Scope='all',
    [string]$Python='python'
)
$ErrorActionPreference='Stop'
if($Tag -notmatch '^[A-Za-z0-9_-]+$'){throw 'Plain evidence tag required'}
$repo=Split-Path $PSScriptRoot -Parent
$private=(Resolve-Path (Join-Path $repo 'private')).Path
$clients=@();$profiles=@();$images=@();$old=@{}
$names='HEIR_TEST_MODE','HEIR_TEST_ROOT','HEIR_DATA_DIR','HEIR_TEST_SOURCE_SHA'
foreach($name in $names){$old[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    for($index=1;$index -le 4;$index++) {
        $folder=if($index -eq 1){"sts2-harness-$Branch-lab"}elseif($index -eq 2){"sts2-harness-${Branch}2-lab"}else{"sts2-coop-$Branch$index-lab"}
        $lab=(Resolve-Path (Join-Path $private $folder)).Path
        if(-not $lab.StartsWith($private+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Private lab required'}
        if(-not (Get-Content (Join-Path $lab 'lab-provenance.json') -Raw|ConvertFrom-Json).independentCopies){throw 'Independent copied lab required'}
        $profile=Join-Path $private "coop-audit-$Tag-$index-profile"
        if(Test-Path -LiteralPath $profile){throw 'Fresh profile required'}
        New-Item -ItemType Directory -Path $profile|Out-Null
        $image=Join-Path $lab 'SlayTheSpire2.exe'
        if(Get-CimInstance Win32_Process|Where-Object ExecutablePath -EQ $image){throw 'Lab already running'}
        Copy-Item -LiteralPath (Join-Path $repo 'sts2/bin/Release/net9.0/SpireLegacy.dll') -Destination (Join-Path $lab 'mods/SpireLegacy/SpireLegacy.dll')
        Copy-Item -LiteralPath (Join-Path $repo 'sts2/SpireLegacy.json') -Destination (Join-Path $lab 'mods/SpireLegacy/SpireLegacy.json')
        $env:HEIR_TEST_MODE='1';$env:HEIR_TEST_ROOT=$private;$env:HEIR_DATA_DIR=$profile;$env:HEIR_TEST_SOURCE_SHA=git -C $repo rev-parse HEAD
        $clients+=Start-Process $image -WorkingDirectory $lab -ArgumentList '--force-steam=off --rendering-method gl_compatibility --rendering-driver opengl3 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profile 'launch.log') -RedirectStandardError (Join-Path $profile 'errors.log')
        $profiles+=$profile;$images+=$image
    }
    & $Python (Join-Path $PSScriptRoot 'sts2-coop-audit.py') $Tag $Port @profiles $Scope
    if($LASTEXITCODE -ne 0){throw 'Co-op audit driver failed'}
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$old[$name],'Process')}
    for($index=0;$index -lt $clients.Count;$index++){
        $running=Get-CimInstance Win32_Process -Filter "ProcessId=$($clients[$index].Id)"
        if($running){if($running.ExecutablePath -ne $images[$index]){throw 'Refusing unexpected PID image'};Stop-Process -Id $clients[$index].Id}
    }
}
