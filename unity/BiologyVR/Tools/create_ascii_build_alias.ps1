$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath "$PSScriptRoot\..").Path
$parent = Join-Path $env:TEMP 'opencode'
$alias = Join-Path $parent 'BiologyVR_Bio_v_0_1'
if (-not (Test-Path -LiteralPath $parent)) { throw "Alias parent is missing: $parent" }
if (-not (Test-Path -LiteralPath $source)) { throw "Project missing: $source" }
if (-not (Test-Path -LiteralPath $alias)) {
    & cmd.exe /c mklink /J $alias $source
    if ($LASTEXITCODE -ne 0) { throw 'Creating the ASCII junction failed.' }
}
"ASCII build alias: $alias"
'In Unity: Biology VR > Reopen At ASCII Build Alias, then Build Pico 4 Enterprise APK.'
