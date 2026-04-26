$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$modRoot = Join-Path $repoRoot "mods\VakuuRoomInjection"
$projectPath = Join-Path $modRoot "project.godot"
$manifestPath = Join-Path $modRoot "VakuuRoomInjection.json"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

Assert-True (Test-Path $projectPath) "project.godot should exist."
Assert-True (Test-Path $manifestPath) "VakuuRoomInjection.json should exist."

$project = Get-Content -Raw -Path $projectPath
Assert-True ($project.Contains('config/name="VakuuRoomInjection"')) "Godot project display name should be VakuuRoomInjection."
Assert-True ($project.Contains('project/assembly_name="VakuuRoomInjection"')) "Godot project assembly name should be VakuuRoomInjection."

$manifest = Get-Content -Raw -Path $manifestPath
Assert-True ($manifest.Contains('"id": "VakuuRoomInjection"')) "Mod manifest id should be VakuuRoomInjection."
Assert-True ($manifest.Contains('"name": "VakuuRoomInjection"')) "Mod manifest display name should be VakuuRoomInjection."

$sourceFiles = Get-ChildItem -Path $modRoot -Recurse -File -Include *.cs,*.csproj,*.sln,*.godot,*.json |
    Where-Object { $_.FullName -notmatch '\\.godot\\|\\bin\\|\\obj\\|\\dist\\' }

$offenders = foreach ($file in $sourceFiles) {
    $content = Get-Content -Raw -Path $file.FullName
    if ($content.Contains("MapNodeChanger")) {
        $file.FullName.Substring($repoRoot.Length + 1)
    }
}

Assert-True (($offenders | Measure-Object).Count -eq 0) ("Published mod metadata and source should not contain MapNodeChanger. Offenders: " + ($offenders -join ", "))
