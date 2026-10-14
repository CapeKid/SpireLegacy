# Used only by guarded disposable labs; restores the original mod tree after exit.
function Assert-PackageLabTree([string]$Path) {
    foreach($item in @((Get-Item -LiteralPath $Path)) + @(Get-ChildItem -LiteralPath $Path -Recurse -Force)){
        if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Directory/file links are not permitted in package lab mod trees.'}
    }
}
function Install-PackageLabMod([string]$PrivateRoot,[string]$LabRoot,[string]$Source,[string]$Tag) {
    $sourcePath=(Resolve-Path -LiteralPath $Source).Path
    $privatePrefix=$PrivateRoot+[IO.Path]::DirectorySeparatorChar
    if(-not $sourcePath.StartsWith($privatePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Package source must be private.'}
    $repoRoot=Split-Path $PSScriptRoot -Parent
    $built=Join-Path $repoRoot 'sts2/bin/Release/net9.0/SpireLegacy.dll'
    if((Get-FileHash -LiteralPath (Join-Path $sourcePath 'SpireLegacy.dll')).Hash -ne (Get-FileHash -LiteralPath $built).Hash){throw 'Package lab DLL differs from checked build.'}
    $old=Join-Path $LabRoot 'mods/SpireLegacy'
    $installed=Join-Path $LabRoot 'mods/Spire Legacy é'
    $backup=Join-Path $PrivateRoot "package-mod-backup-$Tag"
    foreach($path in @($old,$installed,$backup)){
        if(-not [IO.Path]::GetFullPath($path).StartsWith($privatePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Package test path escapes private labs.'}
    }
    if((Test-Path -LiteralPath $installed) -or (Test-Path -LiteralPath $backup)){throw 'Use a fresh package install tag.'}
    Assert-PackageLabTree $sourcePath
    Assert-PackageLabTree $old
    Move-Item -LiteralPath $old -Destination $backup
    try {Copy-Item -LiteralPath $sourcePath -Destination $installed -Recurse}
    catch {
        if(Test-Path -LiteralPath $installed){Remove-Item -LiteralPath $installed -Recurse -Force}
        Move-Item -LiteralPath $backup -Destination $old
        throw
    }
    return @{private=$PrivateRoot;old=$old;installed=$installed;backup=$backup}
}
function Restore-PackageLabMod($State) {
    if($null -eq $State){return}
    $privatePrefix=$State.private+[IO.Path]::DirectorySeparatorChar
    foreach($path in @($State.old,$State.installed,$State.backup)){
        if(-not [IO.Path]::GetFullPath($path).StartsWith($privatePrefix,[StringComparison]::OrdinalIgnoreCase)){throw 'Restore path escapes private labs.'}
    }
    if(Test-Path -LiteralPath $State.old){throw 'Original mod destination unexpectedly occupied; preserve package backup.'}
    Assert-PackageLabTree $State.installed
    Assert-PackageLabTree $State.backup
    Remove-Item -LiteralPath $State.installed -Recurse -Force
    Move-Item -LiteralPath $State.backup -Destination $State.old
}
