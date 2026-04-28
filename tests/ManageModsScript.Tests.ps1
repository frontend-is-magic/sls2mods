$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot "manage-mods-win11.bat"
$modRoot = Join-Path $repoRoot "mods\VakuuRoomInjection"
$readmePath = Join-Path $repoRoot "README.md"
$gitignorePath = Join-Path $repoRoot ".gitignore"
$commonBuildPath = Join-Path $repoRoot "utils\Build\ModBuild.ps1"

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
Assert-True (Test-Path $commonBuildPath) "Common mod build helper should exist under utils\Build."

$script = Get-Content -Raw -Path $scriptPath
Assert-True ($script.Contains("BrowseForFolder")) "Script should open a folder picker for the game directory."
Assert-True ($script.Contains("GAME_PICK_FILE")) "Script should read the selected game path through a temp file so paths containing parentheses do not break cmd parsing."
Assert-True ($script.Contains("mod-manager-config.yaml")) "Script should store the selected game directory in a YAML config file."
Assert-True ($script.Contains(":load_game_dir_config")) "Script should load the game directory from config before opening the folder picker."
Assert-True ($script.Contains(":save_game_dir_config")) "Script should save the selected game directory after a successful folder picker selection."
Assert-True ($script.Contains(":change_game_dir")) "Script should provide a menu flow for changing the saved game directory."
Assert-True ($script.Contains("data_sts2_windows_x86_64\sts2.dll")) "Script should validate the selected Slay the Spire 2 directory."
Assert-True ($script.Contains("Steam -> Slay the Spire 2 -> Manage -> Browse local files")) "Script should tell players how to find the game directory from Steam."
Assert-True ($script.Contains(":add_mod")) "Script should implement add mod flow."
Assert-True ($script.Contains(":remove_mod")) "Script should implement remove mod flow."
Assert-True ($script.Contains("for /d %%A in (`"mods\*`")")) "Script should discover mods from this repository's mods directory."
Assert-True ($script.Contains("dist\%%~nxA.dll")) "Script should install built mod DLLs from each mod's dist directory."
Assert-True ($script.Contains(":ensure_repo_mod_built")) "Add mod should try to build source mods when dist artifacts are missing."
Assert-True ($script.Contains("build.ps1")) "Add mod should discover each source mod's build script."
Assert-True ($script.Contains("-File `"%REPO_MOD_DIR%\build.ps1`"")) "Add mod should run each source mod build script before deciding it is not installable."
Assert-True ($script.Contains("%APPDATA%\SlayTheSpire2\mod_configs")) "Script should initialize the BaseLib mod config directory."
Assert-True ($script.Contains(":init_mod_config")) "Script should initialize config for any installed mod with a config example."
Assert-True ($script.Contains("%MOD_ID%Config.json.example")) "Script should discover each mod's example config by mod id."
Assert-True ($script.Contains("%DIST_DIR%\%MOD_ID%Config.json.example")) "Script should initialize installed configs from dist templates."
Assert-True ($script.Contains("OLD_TARGET_DIR")) "Script should clean up the old MapNodeChanger install directory when installing VakuuRoomInjection."

$repoMods = Get-ChildItem -Path (Join-Path $repoRoot "mods") -Directory
foreach ($mod in $repoMods) {
    $modId = $mod.Name
    $buildPath = Join-Path $mod.FullName "build.ps1"
    $manifestPath = Join-Path $mod.FullName "$modId.json"
    $examplePath = Join-Path $mod.FullName "$modId`Config.json.example"

    Assert-True (Test-Path $manifestPath) "$modId should have a BaseLib manifest named $modId.json."
    Assert-True (Test-Path $examplePath) "$modId should have an example config named $modId`Config.json.example."

    $manifest = Get-Content -Raw -Path $manifestPath
    Assert-True ($manifest.Contains('"has_pck": false')) "$modId manifest should mark the mod as not requiring a pck."
    Assert-True ($manifest.Contains('"has_dll": true')) "$modId manifest should mark the mod as a DLL mod."
    Assert-True ($manifest.Contains('"dependencies": ["BaseLib"]')) "$modId manifest should declare the BaseLib dependency."
    Assert-True ($manifest.Contains('"affects_gameplay": true')) "$modId manifest should declare that it affects gameplay."

    if (Test-Path $buildPath) {
        $build = Get-Content -Raw -Path $buildPath
        Assert-True ($build.Contains("utils\Build\ModBuild.ps1")) "$modId build should dot-source the common build helper."
        Assert-True ($build.Contains("Build-Sls2Mod")) "$modId build should call the common Build-Sls2Mod helper."
        Assert-True (-not $build.Contains("dotnet build")) "$modId build should delegate dotnet build to the common helper."
    }
}

$commonBuild = Get-Content -Raw -Path $commonBuildPath
Assert-True ($commonBuild.Contains("function Build-Sls2Mod")) "Common build helper should define Build-Sls2Mod."
Assert-True ($commonBuild.Contains("sts2.dll")) "Common build helper should validate and copy sts2.dll."
Assert-True ($commonBuild.Contains("dotnet build")) "Common build helper should build the mod project."
Assert-True ($commonBuild.Contains("Config.json.example")) "Common build helper should copy example configs into dist."
Assert-True ($commonBuild.Contains("CleanDistNames")) "Common build helper should support old dist artifact cleanup."

$readme = Get-Content -Raw -Path $readmePath
Assert-True ($readme.TrimStart().StartsWith("# Mod Manager")) "README should lead with mod management instructions."
Assert-True ($readme.Contains("manage-mods-win11.bat")) "README should guide players to the Win11 mod manager script."
Assert-True ($readme.Contains("mods\VakuuRoomInjection")) "README should document the unified mods directory."
Assert-True ($readme.Contains("Script Step Details")) "README should explain what each script step does."
Assert-True ($readme.Contains("Potential Side Effects")) "README should clearly list potential side effects."
Assert-True ($readme.Contains("PATH, registry, Steam, .NET, Godot, or system environment variables")) "README should say the script does not change dependency or system environment settings."
Assert-True ($readme.Contains("Config File")) "README should document the mod manager YAML config file."
Assert-True ($readme.Contains("Change game folder")) "README should document the menu option for changing the saved game directory."

$gitignore = Get-Content -Raw -Path $gitignorePath
Assert-True ($gitignore.Contains("mod-manager-config.yaml")) "Generated mod manager YAML config should be ignored by git."

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after a Program Files (x86) game path is selected."
    Assert-True (($output -join "`n").Contains("Selected game folder:")) "Script should reach the main menu after a Program Files (x86) game path is selected."
}

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    Set-Content -Path $configPath -Value "game_dir: $gameDir" -Encoding ASCII
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly when using a cached YAML game directory."
    Assert-True (($output -join "`n").Contains("Using saved game folder:")) "Script should use a valid YAML game directory without opening the folder picker."
}

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    $config = Get-Content -Raw -Path $configPath
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after first-time folder selection."
    Assert-True ($config.Contains("game_dir: $gameDir")) "Script should write the first selected game directory to YAML config."
}

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    Set-Content -Path $configPath -Value "game_dir: C:\NotARealSlayTheSpire2Folder" -Encoding ASCII
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTIONS=3,4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    $config = Get-Content -Raw -Path $configPath
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after changing the saved game directory."
    Assert-True (($output -join "`n").Contains("Saved game folder:")) "Change game folder should save the newly selected directory."
    Assert-True ($config.Contains("game_dir: $gameDir")) "Change game folder should overwrite the YAML config."
}
