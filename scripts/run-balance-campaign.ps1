param(
    [Parameter(Mandatory=$true)][string]$Lab,
    [Parameter(Mandatory=$true)][string]$Tag,
    [Parameter(Mandatory=$true)][ValidateSet('knight','mage','ranger','ironclad')][string]$Class,
    [ValidateRange(0,2)][int]$Tier=0,
    [ValidateSet('attack','defense','tactical')][string]$Policy='defense',
    [string]$Seed='SPIRELEGACY-CAMPAIGN-1',
    [ValidateRange(0,10)][int]$Ascension=0,
    [string]$Traits='',
    [string]$Python='python',
    [switch]$Checks,
    [switch]$DelayedChecks,
    [ValidateSet('standalone','deck')][string]$Rewards='standalone'
)
$ErrorActionPreference='Stop'
if($Checks -and $DelayedChecks){throw 'Select one fixture driver.'}
$repoPath=Split-Path $PSScriptRoot -Parent
$privatePath=(Resolve-Path (Join-Path $repoPath 'private')).Path
$labPath=(Resolve-Path (Join-Path $repoPath $Lab)).Path
if (-not $labPath.StartsWith($privatePath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Lab must be private.' }
if ($Tag -notmatch '^[A-Za-z0-9_-]+$') { throw 'Use a plain evidence tag.' }
$provenance=Get-Content (Join-Path $labPath 'lab-provenance.json') -Raw | ConvertFrom-Json
if (-not $provenance.independentCopies) { throw 'Independent copies required.' }
$exePath=Join-Path $labPath 'SlayTheSpire2.exe'
if (Get-CimInstance Win32_Process | Where-Object ExecutablePath -EQ $exePath) { throw 'The selected lab already has a client.' }
$profilePath=Join-Path $privatePath "campaign-$Tag-profile"
if (Test-Path -LiteralPath $profilePath) { throw 'A new disposable profile is required.' }
New-Item -ItemType Directory -Path $profilePath | Out-Null
Copy-Item -LiteralPath (Join-Path $repoPath 'sts2/bin/Release/net9.0/SpireLegacy.dll') -Destination (Join-Path $labPath 'mods/SpireLegacy/SpireLegacy.dll')
$client=$null
$names='HEIR_TEST_MODE','HEIR_TEST_ROOT','HEIR_DATA_DIR','HEIR_TEST_SOURCE_SHA'
$previous=@{}
foreach ($name in $names) { $previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process') }
try {
    $env:HEIR_TEST_MODE='1';$env:HEIR_TEST_ROOT=$privatePath;$env:HEIR_DATA_DIR=$profilePath
    $env:HEIR_TEST_SOURCE_SHA=git -C $repoPath rev-parse HEAD
    $client=Start-Process $exePath -WorkingDirectory $labPath -ArgumentList '--force-steam=off --rendering-method gl_compatibility --rendering-driver opengl3 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profilePath 'launch.log') -RedirectStandardError (Join-Path $profilePath 'errors.log')
    $driver=if($Checks){'sts2-tactical-check.py'}elseif($DelayedChecks){'sts2-delayed-reward-check.py'}else{'sts2-balance-campaign.py'}
    $traitArgument=if($Traits){$Traits}else{'-'}
    & $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class $Tier $Policy $Seed $Ascension $traitArgument $Rewards
    if ($LASTEXITCODE -ne 0) { throw "Campaign driver failed; inspect $profilePath" }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$previous[$name],'Process') }
    if ($null -ne $client) {
        $running=Get-CimInstance Win32_Process -Filter "ProcessId=$($client.Id)"
        if ($null -ne $running) {
            if ($running.ExecutablePath -ne $exePath) { throw 'PID executable changed; refusing to stop it.' }
            Stop-Process -Id $client.Id
        }
    }
}
