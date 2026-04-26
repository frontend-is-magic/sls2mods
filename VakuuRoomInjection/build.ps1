param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$Godot = "godot"
)

$ErrorActionPreference = "Stop"

$previousLocation = Get-Location
Set-Location $PSScriptRoot
try {
$stsDll = Join-Path $GameDir "data_sts2_windows_x86_64\sts2.dll"
if (-not (Test-Path $stsDll)) {
    throw "sts2.dll not found at $stsDll"
}

Copy-Item $stsDll -Destination (Join-Path $PSScriptRoot "sts2.dll") -Force

& $Godot --build-solutions --quit --headless --verbose
if ($LASTEXITCODE -ne 0) {
    throw "Godot solution build failed."
}

$dll = Join-Path $PSScriptRoot ".godot\mono\temp\bin\Debug\MapNodeChanger.dll"
if (-not (Test-Path $dll)) {
    throw "Built DLL not found at $dll"
}

$dist = Join-Path $PSScriptRoot "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item $dll -Destination (Join-Path $dist "MapNodeChanger.dll") -Force
Copy-Item (Join-Path $PSScriptRoot "MapNodeChanger.json") -Destination (Join-Path $dist "MapNodeChanger.json") -Force
Copy-Item (Join-Path $PSScriptRoot "MapNodeChangerConfig.json.example") -Destination (Join-Path $dist "MapNodeChangerConfig.json") -Force

& $Godot --export-pack "Windows Desktop" (Join-Path $dist "MapNodeChanger.pck") --headless
if ($LASTEXITCODE -ne 0) {
    throw "Godot pck export failed. Open project.godot once in Godot and create a Windows Desktop export preset if needed."
}

Write-Host "Built mod files in $dist"
}
finally {
    Set-Location $previousLocation
}
