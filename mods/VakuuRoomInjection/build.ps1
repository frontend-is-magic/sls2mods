param(
    [string]$GameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
    [string]$Godot = "godot"
)

$ErrorActionPreference = "Stop"

. (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "utils\Build\ModBuild.ps1")

Build-Sls2Mod `
    -ModRoot $PSScriptRoot `
    -ModId "VakuuRoomInjection" `
    -ProjectFile "VakuuRoomInjection.csproj" `
    -GameDir $GameDir `
    -CleanDistNames @("MapNodeChanger")
