param([Parameter(Mandatory=$true)][ValidateSet('regular','beta')][string]$Branch)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$lab=Join-Path $repo "private/sts2-startup-negative-$Branch-lab"
if(-not(Test-Path -LiteralPath (Join-Path $lab 'lab-provenance.json'))){throw 'Create a fresh negative lab with sts2-lab.py first.'}
$suffix=if($Branch -eq 'regular'){'Regular'}else{'Beta'}
$userdata=Join-Path $env:APPDATA "SpireLegacy2PortLabNegative$suffix"
$manifest=Join-Path $lab 'mods/BaseLib/BaseLib.json'
$originalManifest=Get-Content -LiteralPath $manifest -Raw
$settingsFiles=@(Get-ChildItem -LiteralPath $userdata -Filter settings.save -Recurse)
$settingsCopies=@{};foreach($file in $settingsFiles){$settingsCopies[$file.FullName]=Get-Content -LiteralPath $file.FullName -Raw}
$evidence=@()
foreach($case in @('missing','disabled','version','patch')){
    $profile=Join-Path $repo ("private/startup-negative-$Branch-$case-"+[guid]::NewGuid().ToString('N'))
    [IO.Directory]::CreateDirectory($profile)|Out-Null
    $family=Join-Path $profile 'family.json';[IO.File]::WriteAllText($family,'{"family":"Untouched fixture","crowns":1234}')
    $before=(Get-FileHash -LiteralPath $family -Algorithm SHA256).Hash
    $exe=Join-Path $lab 'SlayTheSpire2.exe';$log=Join-Path $profile 'stdout.log';$errors=Join-Path $profile 'stderr.log';$process=$null;$processPath=$null
    try{
        if($case -eq 'missing'){Rename-Item -LiteralPath $manifest -NewName 'BaseLib.json.audit-disabled'}
        if($case -eq 'version'){$data=$originalManifest|ConvertFrom-Json;$data.version='1.0.0';[IO.File]::WriteAllText($manifest,($data|ConvertTo-Json -Depth 20))}
        if($case -eq 'disabled'){foreach($file in $settingsFiles){$data=$settingsCopies[$file.FullName]|ConvertFrom-Json;foreach($mod in $data.mod_settings.mod_list){if($mod.id -eq 'BaseLib'){$mod.is_enabled=$false}};[IO.File]::WriteAllText($file.FullName,($data|ConvertTo-Json -Depth 20))}}
        $env:HEIR_TEST_MODE='1';$env:HEIR_TEST_ROOT=Join-Path $repo 'private';$env:HEIR_DATA_DIR=$profile;$env:HEIR_TEST_SOURCE_SHA=(git -C $repo rev-parse HEAD)
        $env:HEIR_TEST_FAIL_PATCH=if($case -eq 'patch'){'BindNewPlayerBloodline'}else{$null}
        $process=Start-Process -FilePath $exe -WorkingDirectory $lab -ArgumentList '--rendering-method gl_compatibility --rendering-driver opengl3 --position -2000,-2000' -WindowStyle Hidden -PassThru -RedirectStandardOutput $log -RedirectStandardError $errors
        $processPath=$process.Path; $processStart=$process.StartTime.ToUniversalTime().Ticks
        $physicalPrivate=(Get-Item -LiteralPath (Join-Path $repo 'private')).Target
        $physicalExe=if($physicalPrivate){$exe.Replace((Join-Path $repo 'private'),$physicalPrivate)}else{$exe}
        if($processPath -ne $exe -and $processPath -ne $physicalExe){throw 'Unexpected child process path.'}
        $deadline=[DateTime]::UtcNow.AddSeconds(45);$text='';$matched=$false
        while([DateTime]::UtcNow -lt $deadline){
            $text=(Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue)+(Get-Content -LiteralPath $errors -Raw -ErrorAction SilentlyContinue)
            $matched=if($case -eq 'patch'){$text -match 'patch installation failed at SpireLegacy.BindNewPlayerBloodline'}else{$text -match '(?im)\[ERROR\].*SpireLegacy.*BaseLib'}
            if($matched){break};Start-Sleep -Milliseconds 250
        }
        if(-not $matched){throw "Expected startup failure missing: $Branch/$case"}
        Start-Sleep -Milliseconds 500
    }finally{
        try { if($process -and -not $process.HasExited){$actual=Get-Process -Id $process.Id;if(($actual.Path -ne $exe -and $actual.Path -ne $physicalExe) -or $actual.StartTime.ToUniversalTime().Ticks -ne $processStart){throw 'PID identity changed; refusing to stop it.'};$actual.Kill();if(-not $actual.WaitForExit(10000)){throw 'Test client failed to exit.'}} } finally {
        if(Test-Path -LiteralPath "$manifest.audit-disabled"){Rename-Item -LiteralPath "$manifest.audit-disabled" -NewName 'BaseLib.json'}
        [IO.File]::WriteAllText($manifest,$originalManifest)
        foreach($file in $settingsFiles){[IO.File]::WriteAllText($file.FullName,$settingsCopies[$file.FullName])}
        $env:HEIR_TEST_FAIL_PATCH=$null
        }
    }
    $after=(Get-FileHash -LiteralPath $family -Algorithm SHA256).Hash
    if($before -ne $after -or $text -match 'Spire Legacy StS2 port initialized'){throw "Failed initialization touched family or exposed bridge: $Branch/$case"}
    $evidence+=@{case=$case;branch=$Branch;familyUnchanged=$true;profile=$profile;dllSha256=(Get-FileHash -LiteralPath (Join-Path $lab 'mods/SpireLegacy/SpireLegacy.dll') -Algorithm SHA256).Hash;sourceSha=$env:HEIR_TEST_SOURCE_SHA;game=(Get-Content -LiteralPath (Join-Path $lab 'release_info.json') -Raw|ConvertFrom-Json);diagnostic=$text}
    $evidence|ConvertTo-Json -Depth 20|Set-Content (Join-Path $repo "private/sts2-evidence/startup-negative-$Branch.json")
    Write-Output "$Branch/${case}: expected startup failure; family unchanged."
}
