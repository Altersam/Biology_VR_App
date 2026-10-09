$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath "$PSScriptRoot\..").Path
$parent = [System.IO.Path]::GetPathRoot($source)
$alias = Join-Path $parent 'BiologyVR_bio_v02_build'
if (-not (Test-Path -LiteralPath $parent)) { throw "Alias parent is missing: $parent" }
if (-not (Test-Path -LiteralPath $source)) { throw "Project missing: $source" }
if (-not (Test-Path -LiteralPath $alias)) {
    & cmd.exe /c mklink /J $alias $source
    if ($LASTEXITCODE -ne 0) { throw 'Creating the ASCII junction failed.' }
}
"ASCII build alias: $alias"
'In Unity: Biology VR > Reopen At ASCII Build Alias, then Build Pico 4 Enterprise APK.'
