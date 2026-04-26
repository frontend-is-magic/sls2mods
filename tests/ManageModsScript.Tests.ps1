$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot "manage-mods-win11.bat"
$modRoot = Join-Path $repoRoot "mods\VakuuRoomInjection"
$readmePath = Join-Path $repoRoot "README.md"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

Assert-True (Test-Path $modRoot) "VakuuRoomInjection should live under mods\VakuuRoomInjection."
Assert-True (-not (Test-Path (Join-Path $repoRoot "VakuuRoomInjection"))) "Root VakuuRoomInjection directory should be removed."
Assert-True (-not (Test-Path (Join-Path $repoRoot "enable-vakuu-room-injection.ps1"))) "Old enable-vakuu-room-injection.ps1 should be removed."
Assert-True (Test-Path $scriptPath) "manage-mods-win11.bat should exist."
Assert-True (Test-Path $readmePath) "Root README.md should exist."

$script = Get-Content -Raw -Path $scriptPath
Assert-True ($script.Contains("BrowseForFolder")) "Script should open a folder picker for the game directory."
Assert-True ($script.Contains("GAME_PICK_FILE")) "Script should read the selected game path through a temp file so paths containing parentheses do not break cmd parsing."
Assert-True ($script.Contains("data_sts2_windows_x86_64\sts2.dll")) "Script should validate the selected Slay the Spire 2 directory."
Assert-True ($script.Contains("Steam -> Slay the Spire 2 -> Manage -> Browse local files")) "Script should tell players how to find the game directory from Steam."
Assert-True ($script.Contains(":add_mod")) "Script should implement add mod flow."
Assert-True ($script.Contains(":remove_mod")) "Script should implement remove mod flow."
Assert-True ($script.Contains("for /d %%A in (`"mods\*`")")) "Script should discover mods from this repository's mods directory."
Assert-True ($script.Contains("dist\%%~nxA.dll")) "Script should install built mod DLLs from each mod's dist directory."
Assert-True ($script.Contains("%APPDATA%\SlayTheSpire2\mod_configs")) "Script should initialize the VakuuRoomInjection config directory."
Assert-True ($script.Contains("OLD_TARGET_DIR")) "Script should clean up the old MapNodeChanger install directory when installing VakuuRoomInjection."

$readme = Get-Content -Raw -Path $readmePath
Assert-True ($readme.TrimStart().StartsWith("# Mod Manager")) "README should lead with mod management instructions."
Assert-True ($readme.Contains("manage-mods-win11.bat")) "README should guide players to the Win11 mod manager script."
Assert-True ($readme.Contains("mods\VakuuRoomInjection")) "README should document the unified mods directory."
Assert-True ($readme.Contains("Script Step Details")) "README should explain what each script step does."
Assert-True ($readme.Contains("Potential Side Effects")) "README should clearly list potential side effects."
Assert-True ($readme.Contains("PATH, registry, Steam, .NET, Godot, or system environment variables")) "README should say the script does not change dependency or system environment settings."

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $cmd = "set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTION=3`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after a Program Files (x86) game path is selected."
    Assert-True (($output -join "`n").Contains("Selected game folder:")) "Script should reach the main menu after a Program Files (x86) game path is selected."
}
