param(
    [Parameter(Mandatory=$true)][string]$Lab,
    [Parameter(Mandatory=$true)][string]$Tag,
    [Parameter(Mandatory=$true)][ValidateSet('knight','mage','ranger','ironclad')][string]$Class,
    [ValidateRange(0,2)][int]$Tier=0,
    [ValidateSet('attack','defense','tactical','planned')][string]$Policy='defense',
    [string]$Seed='SPIRELEGACY-CAMPAIGN-1',
    [ValidateRange(0,10)][int]$Ascension=0,
    [string]$Traits='',
    [string]$Python='python',
    [string]$ReleaseModDirectory='',
    [switch]$Checks,
    [switch]$DelayedChecks,
    [switch]$RewardChecks,
    [switch]$PayoffChecks,
    [switch]$ReleaseChecks,
    [switch]$CandidateChecks,
    [switch]$LifecycleChecks,
    [switch]$LifecycleUiChecks,
    [switch]$TraitAuditChecks,
    [switch]$OrobasChecks,
    [switch]$CardResolutionChecks,
    [switch]$PowerAuditChecks,
    [switch]$CrownChecks,
    [switch]$PoolAuditChecks,
    [switch]$ItemAuditChecks,
    [switch]$AncientAuditChecks,
    [switch]$ManorAuditChecks,
    [switch]$PresentationAuditChecks,
    [switch]$BalanceAuditChecks,
    [switch]$PackageChecks,
    [switch]$PacingChecks,
    [switch]$SphereUiChecks,
    [string]$ResumePacingReport='',
    [string]$ContinuePacingReport='',
    [switch]$ForkCompletedProfile,
    [ValidateRange(1,200)][int]$EndRun=12,
    [switch]$StopAtFullManor,
    [string]$ForcedReward='',
    [int]$ForcedRewardFloor=0,
    [ValidateSet('standalone','deck','defensive')][string]$Rewards='standalone'
)
$ErrorActionPreference='Stop'
if(([int]$Checks.IsPresent+[int]$DelayedChecks.IsPresent+[int]$RewardChecks.IsPresent+[int]$PayoffChecks.IsPresent+[int]$ReleaseChecks.IsPresent+[int]$CandidateChecks.IsPresent+[int]$LifecycleChecks.IsPresent+[int]$LifecycleUiChecks.IsPresent+[int]$TraitAuditChecks.IsPresent+[int]$OrobasChecks.IsPresent+[int]$CardResolutionChecks.IsPresent+[int]$PowerAuditChecks.IsPresent+[int]$CrownChecks.IsPresent+[int]$PoolAuditChecks.IsPresent+[int]$ItemAuditChecks.IsPresent+[int]$AncientAuditChecks.IsPresent+[int]$ManorAuditChecks.IsPresent+[int]$PresentationAuditChecks.IsPresent+[int]$BalanceAuditChecks.IsPresent+[int]$PackageChecks.IsPresent+[int]$PacingChecks.IsPresent+[int]$SphereUiChecks.IsPresent) -gt 1){throw 'Select one fixture driver.'}
if($ForcedReward -and ($ForcedRewardFloor -le 0 -or $Rewards -eq 'standalone')){throw 'Interventions require deck rewards and a positive floor.'}
$repoPath=Split-Path $PSScriptRoot -Parent
$privatePath=(Resolve-Path (Join-Path $repoPath 'private')).Path
$labPath=(Resolve-Path (Join-Path $repoPath $Lab)).Path
if (-not $labPath.StartsWith($privatePath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) { throw 'Lab must be private.' }
if ($Tag -notmatch '^[A-Za-z0-9_-]+$') { throw 'Use a plain evidence tag.' }
$provenance=Get-Content (Join-Path $labPath 'lab-provenance.json') -Raw | ConvertFrom-Json
if (-not $provenance.independentCopies) { throw 'Independent copies required.' }
$exePath=Join-Path $labPath 'SlayTheSpire2.exe'
$buildSource=([Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $repoPath 'sts2/bin/Release/net9.0/SpireLegacy.dll')).ProductVersion -split '\+')[-1]
if($buildSource -notmatch '^[a-f0-9]{40}$'){throw 'Audit DLL must carry its committed build source.'}
if (Get-CimInstance Win32_Process | Where-Object ExecutablePath -EQ $exePath) { throw 'The selected lab already has a client.' }
$profilePath=Join-Path $privatePath "campaign-$Tag-profile"
if($ResumePacingReport -and $ContinuePacingReport){throw 'Choose checkpoint recovery or completed-trajectory continuation.'}
if($ForkCompletedProfile -and !$ContinuePacingReport){throw 'Only completed trajectory receipts can seed a new private fork.'}
if($ResumePacingReport -or $ContinuePacingReport){
    if(!$PacingChecks){throw 'Only pacing trajectories may continue.'}
    $prior=(Resolve-Path $(if($ResumePacingReport){$ResumePacingReport}else{$ContinuePacingReport})).Path
    if(!$prior.StartsWith($privatePath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Prior report must be private.'}
    $priorData=Get-Content -LiteralPath $prior -Raw|ConvertFrom-Json
    $requiredStatus=if($ResumePacingReport){'error'}else{'finished'}
    if($priorData.status -ne $requiredStatus -or !$priorData.runs.Count){throw 'Prior report has the wrong completion state.'}
    # A completed chain can include a separately labelled durable-receipt fork.
    # Continue its latest profile, not the abandoned ancestor at the prefix.
    $priorProfile=(Resolve-Path $priorData.evidence[-1].response.metadata.profile).Path
    if(!$priorProfile.StartsWith($privatePath+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Recorded private profile required.'}
    if($ForkCompletedProfile){
        if(Test-Path -LiteralPath $profilePath){throw 'Fork requires a new private profile.'}
        New-Item -ItemType Directory -Path $profilePath | Out-Null
        & $Python (Join-Path $PSScriptRoot 'pacing_state.py') $prior $Class (Join-Path $profilePath 'family.json')
        if($LASTEXITCODE -ne 0){throw 'Durable receipt could not seed fork.'}
    }else{$profilePath=$priorProfile}
}else{
    if (Test-Path -LiteralPath $profilePath) { throw 'A new disposable profile is required.' }
    New-Item -ItemType Directory -Path $profilePath | Out-Null
}
Copy-Item -LiteralPath (Join-Path $repoPath 'sts2/bin/Release/net9.0/SpireLegacy.dll') -Destination (Join-Path $labPath 'mods/SpireLegacy/SpireLegacy.dll')
Copy-Item -LiteralPath (Join-Path $repoPath 'sts2/SpireLegacy.json') -Destination (Join-Path $labPath 'mods/SpireLegacy/SpireLegacy.json')
if($PresentationAuditChecks){Copy-Item -Path (Join-Path $repoPath 'src/main/resources/heir/presentation/*') -Destination (Join-Path $labPath 'mods/SpireLegacy/heir/presentation') -Force}
$client=$null
$packageInstall=$null
. (Join-Path $PSScriptRoot 'package-lab-install.ps1')
$names='HEIR_TEST_MODE','HEIR_TEST_ROOT','HEIR_DATA_DIR','HEIR_TEST_SOURCE_SHA'
$previous=@{}
foreach ($name in $names) { $previous[$name]=[Environment]::GetEnvironmentVariable($name,'Process') }
try {
    if($ReleaseModDirectory){$packageInstall=Install-PackageLabMod $privatePath $labPath $ReleaseModDirectory $Tag}
    $env:HEIR_TEST_MODE='1';$env:HEIR_TEST_ROOT=$privatePath;$env:HEIR_DATA_DIR=$profilePath
    $env:HEIR_TEST_SOURCE_SHA=$buildSource
    if($ResumePacingReport -or $ContinuePacingReport){
        foreach($logName in @('launch.log','errors.log')){
            $oldLog=Join-Path $profilePath $logName
            if(Test-Path -LiteralPath $oldLog){Copy-Item -LiteralPath $oldLog -Destination (Join-Path $profilePath "$Tag-previous-$logName")}
        }
    }
    $client=Start-Process $exePath -WorkingDirectory $labPath -ArgumentList '--force-steam=off --rendering-method gl_compatibility --rendering-driver opengl3 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $profilePath 'launch.log') -RedirectStandardError (Join-Path $profilePath 'errors.log')
    $driver=if($SphereUiChecks){'sts2-sphere-ui-check.py'}elseif($PacingChecks){'sts2-pacing-campaign.py'}elseif($Checks){'sts2-tactical-check.py'}elseif($DelayedChecks){'sts2-delayed-reward-check.py'}elseif($RewardChecks){'sts2-reward-intervention-check.py'}elseif($PayoffChecks){'sts2-finisher-payoff-check.py'}elseif($ReleaseChecks){'sts2-release-check.py'}elseif($CandidateChecks){'sts2-six-candidates-check.py'}elseif($LifecycleChecks){'sts2-lifecycle-check.py'}elseif($LifecycleUiChecks){'sts2-lifecycle-ui-check.py'}elseif($TraitAuditChecks){'sts2-trait-audit.py'}elseif($OrobasChecks){'sts2-orobas-check.py'}elseif($CardResolutionChecks){'sts2-card-resolution-check.py'}elseif($PowerAuditChecks){'sts2-power-audit.py'}elseif($CrownChecks){'sts2-crown-check.py'}elseif($PoolAuditChecks){'sts2-pool-audit.py'}elseif($ItemAuditChecks){'sts2-item-audit.py'}elseif($AncientAuditChecks){'sts2-ancient-audit.py'}elseif($ManorAuditChecks){'sts2-manor-audit.py'}elseif($PresentationAuditChecks){'sts2-presentation-audit.py'}elseif($BalanceAuditChecks){'sts2-adversarial-balance.py'}elseif($PackageChecks){'sts2-package-native.py'}else{'sts2-balance-campaign.py'}
    $traitArgument=if($Traits){$Traits}else{'-'}
    $rewardArgument=if($ForcedReward){$ForcedReward}else{'-'}
    [string[]]$pacingTail=@(if($StopAtFullManor){'--stop-when-full'})
    if($ContinuePacingReport){
        if($ForkCompletedProfile){& $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class --continue-report $prior --end-run $EndRun --forked-profile @pacingTail}
        else{& $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class --continue-report $prior --end-run $EndRun @pacingTail}
    }
    elseif($ResumePacingReport){& $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class --resume-report $prior --end-run $EndRun @pacingTail}
    elseif($PacingChecks){& $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class --end-run $EndRun @pacingTail}
    else{& $Python (Join-Path $PSScriptRoot $driver) $profilePath $Tag $Class $Tier $Policy $Seed $Ascension $traitArgument $Rewards $rewardArgument $ForcedRewardFloor}
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
    Restore-PackageLabMod $packageInstall
}
