param(
    [Parameter(Mandatory=$true)][ValidateSet('regular','beta')][string]$Branch,
    [Parameter(Mandatory=$true)][string]$Tag,
    [ValidateRange(1024,65530)][int]$Port=25001,
    [ValidateSet('all','party')][string]$Scope='all',
    [switch]$Pair,
    [switch]$AncientChecks,
    [switch]$ManorChecks,
    [switch]$PresentationChecks,
    [switch]$BalanceChecks,
    [string]$Python='python',
    [string]$ReleaseModDirectory=''
)
$ErrorActionPreference='Stop'
if(($AncientChecks -or $ManorChecks -or $PresentationChecks -or $BalanceChecks) -and !$Pair){throw 'Specialized checks require the pair launcher.'}
if(([int]$AncientChecks.IsPresent+[int]$ManorChecks.IsPresent+[int]$PresentationChecks.IsPresent+[int]$BalanceChecks.IsPresent) -gt 1){throw 'Select one pair driver.'}
if($Tag -notmatch '^[A-Za-z0-9_-]+$'){throw 'Plain evidence tag required'}
$repo=Split-Path $PSScriptRoot -Parent
$private=(Resolve-Path (Join-Path $repo 'private')).Path
$buildSource=([Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $repo 'sts2/bin/Release/net9.0/SpireLegacy.dll')).ProductVersion -split '\+')[-1]
if($buildSource -notmatch '^[a-f0-9]{40}$'){throw 'Audit DLL must carry its committed build source.'}
$clients=@();$profiles=@();$images=@();$old=@{};$packageInstalls=@()
. (Join-Path $PSScriptRoot 'package-lab-install.ps1')
$names='HEIR_TEST_MODE','HEIR_TEST_ROOT','HEIR_DATA_DIR','HEIR_TEST_SOURCE_SHA'
foreach($name in $names){$old[$name]=[Environment]::GetEnvironmentVariable($name,'Process')}
try {
    foreach($index in $(if($Pair){3,4}else{1,2,3,4})) {
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
        if($PresentationChecks){Copy-Item -Path (Join-Path $repo 'src/main/resources/heir/presentation/*') -Destination (Join-Path $lab 'mods/SpireLegacy/heir/presentation') -Force}
        if($ReleaseModDirectory){$packageInstalls+=Install-PackageLabMod $private $lab $ReleaseModDirectory "$Tag-$index"}
        $env:HEIR_TEST_MODE='1';$env:HEIR_TEST_ROOT=$private;$env:HEIR_DATA_DIR=$profile;$env:HEIR_TEST_SOURCE_SHA=$buildSource
        $clients+=Start-Process $image -WorkingDirectory $lab -ArgumentList '--force-steam=off --rendering-method gl_compatibility --rendering-driver opengl3 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profile 'launch.log') -RedirectStandardError (Join-Path $profile 'errors.log')
        $profiles+=$profile;$images+=$image
    }
    if($Pair){$pairDriver=if($AncientChecks){'sts2-ancient-coop.py'}elseif($ManorChecks){'sts2-manor-coop.py'}elseif($PresentationChecks){'sts2-presentation-coop.py'}elseif($BalanceChecks){'sts2-balance-coop.py'}else{'sts2-harness-native-check.py'};& $Python (Join-Path $PSScriptRoot $pairDriver) coop $profiles[0] $profiles[1] $Tag $Port}
    else {& $Python (Join-Path $PSScriptRoot 'sts2-coop-audit.py') $Tag $Port @profiles $Scope}
    if($LASTEXITCODE -ne 0){throw 'Co-op audit driver failed'}
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$old[$name],'Process')}
    for($index=0;$index -lt $clients.Count;$index++){
        $running=Get-CimInstance Win32_Process -Filter "ProcessId=$($clients[$index].Id)"
        if($running){if($running.ExecutablePath -ne $images[$index]){throw 'Refusing unexpected PID image'};Stop-Process -Id $clients[$index].Id}
    }
    foreach($installation in $packageInstalls){Restore-PackageLabMod $installation}
}
