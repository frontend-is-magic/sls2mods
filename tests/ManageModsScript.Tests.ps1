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

function New-TestGameDir {
    $root = Join-Path $env:TEMP ("sts2-game-" + [guid]::NewGuid().ToString("N"))
    $dataDir = Join-Path $root "data_sts2_windows_x86_64"
    New-Item -ItemType Directory -Force -Path $dataDir | Out-Null
    Set-Content -Path (Join-Path $dataDir "sts2.dll") -Value "fake sts2" -Encoding ASCII
    return $root
}

function New-TestBaseLibSource {
    $root = Join-Path $env:TEMP ("sts2-baselib-" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    Set-Content -Path (Join-Path $root "BaseLib.dll") -Value "fake dll" -Encoding ASCII
    Set-Content -Path (Join-Path $root "BaseLib.pck") -Value "fake pck" -Encoding ASCII
    Set-Content -Path (Join-Path $root "BaseLib.json") -Value '{"mod_id":"BaseLib"}' -Encoding ASCII
    return $root
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
Assert-True ($script.Contains(":ensure_baselib_installed")) "Script should ensure BaseLib is installed after selecting a game directory."
Assert-True ($script.Contains("https://api.github.com/repos/Alchyr/BaseLib-StS2/releases/latest")) "Script should download BaseLib from the official GitHub latest release API."
Assert-True ($script.Contains("BaseLib.dll")) "Script should check/install BaseLib.dll."
Assert-True ($script.Contains("BaseLib.pck")) "Script should check/install BaseLib.pck."
Assert-True ($script.Contains("BaseLib.json")) "Script should check/install BaseLib.json."
Assert-True ($script.Contains("MOD_MANAGER_TEST_SKIP_BASELIB")) "Script should let tests skip real BaseLib installation."
Assert-True ($script.Contains("MOD_MANAGER_TEST_BASELIB_SOURCE")) "Script should let tests install BaseLib from a local fake source."
Assert-True ($script.Contains("third_party\BaseLib")) "Script should include a bundled BaseLib fallback directory."
Assert-True ($script.Contains("MOD_MANAGER_TEST_BASELIB_DOWNLOAD_FAIL")) "Script should let tests simulate a GitHub BaseLib download failure."
Assert-True ($script.Contains("MOD_MANAGER_TEST_BUNDLED_BASELIB_DIR")) "Script should let tests point bundled BaseLib fallback at a local fake source."
Assert-True ($script.Contains("MOD_MANAGER_TEST_NO_PAUSE")) "Script should let tests skip the interactive error pause."
Assert-True ($script.Contains("-UseBasicParsing")) "Script should use Windows PowerShell 5.1 compatible web requests."
Assert-True ($script.Contains("TimeoutSec")) "Script should set a bounded timeout for BaseLib downloads."
Assert-True ($script.Contains(":restore_vanilla_mods")) "Script should restore vanilla by clearing game mods when BaseLib is removed."

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
Assert-True ($readme.Contains("BaseLib")) "README should document BaseLib auto-install and removal behavior."
Assert-True ($readme.Contains("Alchyr/BaseLib-StS2")) "README should document the official BaseLib GitHub source."
Assert-True ($readme.Contains("third_party\BaseLib")) "README should document the bundled BaseLib fallback."
Assert-True ($readme.Contains("BaseLib bundled fallback")) "README should explain when the bundled BaseLib fallback is used."

$gitignore = Get-Content -Raw -Path $gitignorePath
Assert-True ($gitignore.Contains("mod-manager-config.yaml")) "Generated mod manager YAML config should be ignored by git."
Assert-True ($gitignore.Contains("!third_party/BaseLib/BaseLib.dll")) "Bundled BaseLib.dll should be allowed through the global DLL ignore rule."
Assert-True ($gitignore.Contains("!third_party/BaseLib/BaseLib.pck")) "Bundled BaseLib.pck should be allowed through the global PCK ignore rule."

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after a Program Files (x86) game path is selected."
    Assert-True (($output -join "`n").Contains("Selected game folder:")) "Script should reach the main menu after a Program Files (x86) game path is selected."
}

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    Set-Content -Path $configPath -Value "game_dir: $gameDir" -Encoding ASCII
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly when using a cached YAML game directory."
    Assert-True (($output -join "`n").Contains("Using saved game folder:")) "Script should use a valid YAML game directory without opening the folder picker."
}

if (Test-Path "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll") {
    $gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
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
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$gameDir`"& set `"MOD_MANAGER_TEST_ACTIONS=3,4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    $config = Get-Content -Raw -Path $configPath
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after changing the saved game directory."
    Assert-True (($output -join "`n").Contains("Saved game folder:")) "Change game folder should save the newly selected directory."
    Assert-True ($config.Contains("game_dir: $gameDir")) "Change game folder should overwrite the YAML config."
}

$tempGameDir = New-TestGameDir
$fakeBaseLib = New-TestBaseLibSource
try {
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_BASELIB_SOURCE=$fakeBaseLib`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    $targetBaseLib = Join-Path $tempGameDir "mods\BaseLib"
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after installing BaseLib from a local test source."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.dll")) "Script should install BaseLib.dll from the local test source."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.pck")) "Script should install BaseLib.pck from the local test source."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.json")) "Script should install BaseLib.json from the local test source."
    Assert-True (($output -join "`n").Contains("Installed BaseLib")) "Script should report BaseLib installation."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $fakeBaseLib -Recurse -Force -ErrorAction SilentlyContinue
}

$tempGameDir = New-TestGameDir
try {
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly when BaseLib auto-install is skipped in tests."
    Assert-True (($output -join "`n").Contains("Skipping BaseLib check")) "Script should report skipped BaseLib checks in tests."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
}

$tempGameDir = New-TestGameDir
$fakeBundledBaseLib = New-TestBaseLibSource
try {
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_BASELIB_DOWNLOAD_FAIL=1`"& set `"MOD_MANAGER_TEST_BUNDLED_BASELIB_DIR=$fakeBundledBaseLib`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    $targetBaseLib = Join-Path $tempGameDir "mods\BaseLib"
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after installing BaseLib from the bundled fallback."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.dll")) "Bundled fallback should install BaseLib.dll."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.pck")) "Bundled fallback should install BaseLib.pck."
    Assert-True (Test-Path (Join-Path $targetBaseLib "BaseLib.json")) "Bundled fallback should install BaseLib.json."
    Assert-True (($output -join "`n").Contains("Trying bundled BaseLib fallback")) "Script should report bundled fallback after download failure."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path $fakeBundledBaseLib -Recurse -Force -ErrorAction SilentlyContinue
}

$tempGameDir = New-TestGameDir
try {
    $missingBundledBaseLib = Join-Path $env:TEMP ("sts2-missing-baselib-" + [guid]::NewGuid().ToString("N"))
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_BASELIB_DOWNLOAD_FAIL=1`"& set `"MOD_MANAGER_TEST_BUNDLED_BASELIB_DIR=$missingBundledBaseLib`"& set `"MOD_MANAGER_TEST_NO_PAUSE=1`"& set `"MOD_MANAGER_TEST_ACTION=4`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -ne 0) "Script should fail with a nonzero exit code when GitHub and bundled BaseLib both fail."
    Assert-True (($output -join "`n").Contains("Could not install BaseLib automatically")) "Script should show a clear automatic install failure."
    Assert-True (($output -join "`n").Contains("https://github.com/Alchyr/BaseLib-StS2/releases/latest")) "Script should show the manual BaseLib download URL."
    Assert-True (($output -join "`n").Contains("BaseLib.dll, BaseLib.pck, and BaseLib.json")) "Script should show the required BaseLib files."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
}

$tempGameDir = New-TestGameDir
try {
    $modsDir = Join-Path $tempGameDir "mods"
    New-Item -ItemType Directory -Force -Path (Join-Path $modsDir "BaseLib") | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $modsDir "VakuuRoomInjection") | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $modsDir "CardRewardEnchantments") | Out-Null
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_ACTIONS=2,4`"& set `"MOD_MANAGER_TEST_MOD_CHOICE=1`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after restoring vanilla from BaseLib removal."
    Assert-True (-not (Test-Path (Join-Path $modsDir "BaseLib"))) "Restoring vanilla should remove BaseLib."
    Assert-True (-not (Test-Path (Join-Path $modsDir "VakuuRoomInjection"))) "Restoring vanilla should remove other installed mods."
    Assert-True (-not (Test-Path (Join-Path $modsDir "CardRewardEnchantments"))) "Restoring vanilla should remove all installed mods."
    Assert-True (($output -join "`n").Contains("Restored vanilla game mods")) "Script should report vanilla restoration."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
}

$tempGameDir = New-TestGameDir
try {
    $modsDir = Join-Path $tempGameDir "mods"
    New-Item -ItemType Directory -Force -Path (Join-Path $modsDir "BaseLib") | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $modsDir "VakuuRoomInjection") | Out-Null
    $configPath = Join-Path $env:TEMP ("sts2-mod-manager-" + [guid]::NewGuid().ToString("N") + ".yaml")
    $cmd = "set `"MOD_MANAGER_TEST_SKIP_BASELIB=1`"& set `"MOD_MANAGER_TEST_CONFIG_PATH=$configPath`"& set `"MOD_MANAGER_TEST_GAME_DIR=$tempGameDir`"& set `"MOD_MANAGER_TEST_ACTIONS=2,4`"& set `"MOD_MANAGER_TEST_MOD_CHOICE=2`"& `"$scriptPath`""
    $output = & cmd.exe /d /c $cmd
    Remove-Item -Path $configPath -Force -ErrorAction SilentlyContinue
    Assert-True ($LASTEXITCODE -eq 0) "Script should exit cleanly after removing a normal mod."
    Assert-True (Test-Path (Join-Path $modsDir "BaseLib")) "Removing a normal mod should keep BaseLib."
    Assert-True (-not (Test-Path (Join-Path $modsDir "VakuuRoomInjection"))) "Removing a normal mod should delete only the selected mod."
    Assert-True (($output -join "`n").Contains("Removed VakuuRoomInjection")) "Script should report normal mod removal."
}
finally {
    Remove-Item -Path $tempGameDir -Recurse -Force -ErrorAction SilentlyContinue
}
