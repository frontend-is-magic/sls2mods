param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$Godot = "godot",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$modRoot = Join-Path $repoRoot "VakuuRoomInjection"
$dist = Join-Path $modRoot "dist"
$target = Join-Path $GameDir "mods\MapNodeChanger"

if (-not $SkipBuild) {
    & powershell -ExecutionPolicy Bypass -File (Join-Path $modRoot "build.ps1") -GameDir $GameDir -Godot $Godot
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
}

$requiredFiles = @(
    "MapNodeChanger.dll",
    "MapNodeChanger.json",
    "MapNodeChanger.pck",
    "MapNodeChangerConfig.json"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $dist $file
    if (-not (Test-Path $path)) {
        throw "Required built mod file missing: $path"
    }
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
foreach ($file in $requiredFiles) {
    Copy-Item (Join-Path $dist $file) -Destination (Join-Path $target $file) -Force
}

Write-Host "Enabled Vakuu Room Injection at $target"
