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

dotnet build (Join-Path $PSScriptRoot "VakuuRoomInjection.csproj")
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

$modId = "VakuuRoomInjection"
$dll = Join-Path $PSScriptRoot ".godot\mono\temp\bin\Debug\$modId.dll"
if (-not (Test-Path $dll)) {
    throw "Built DLL not found at $dll"
}

$dist = Join-Path $PSScriptRoot "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Remove-Item -Path (Join-Path $dist "MapNodeChanger.dll") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "MapNodeChanger.json") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "MapNodeChanger.pck") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "MapNodeChangerConfig.json") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "$modId.pck") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $dist "$($modId)Config.json") -Force -ErrorAction SilentlyContinue
Copy-Item $dll -Destination (Join-Path $dist "$modId.dll") -Force
Copy-Item (Join-Path $PSScriptRoot "$modId.json") -Destination (Join-Path $dist "$modId.json") -Force

Write-Host "Built mod files in $dist"
}
finally {
    Set-Location $previousLocation
}
