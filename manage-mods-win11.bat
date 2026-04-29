@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
if not defined POWERSHELL_EXE set "POWERSHELL_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if defined MOD_MANAGER_TEST_CONFIG_PATH (
    set "CONFIG_PATH=%MOD_MANAGER_TEST_CONFIG_PATH%"
) else (
    set "CONFIG_PATH=%SCRIPT_DIR%mod-manager-config.yaml"
)
pushd "%SCRIPT_DIR%" >nul

echo Slay the Spire 2 Mod Manager
echo.
call :load_game_dir_config
if errorlevel 1 goto :select_initial_game_dir

echo Using saved game folder:
echo !GAME_DIR!
echo.
goto :after_game_dir

:select_initial_game_dir
echo Select your Slay the Spire 2 installation folder in the folder picker.
echo Tip: Steam -^> Slay the Spire 2 -^> Manage -^> Browse local files
rem Steam -> Slay the Spire 2 -> Manage -> Browse local files
echo.

call :choose_dir
if errorlevel 1 goto :fatal_no_game_dir
call :save_game_dir_config

:after_game_dir

set "GAME_MODS=!GAME_DIR!\mods"
if not exist "!GAME_MODS!" mkdir "!GAME_MODS!"
call :ensure_baselib_installed
if errorlevel 1 goto :fatal_baselib_error

:main_menu
echo.
echo Selected game folder:
echo !GAME_DIR!
echo.
echo 1. Add mod
echo 2. Remove mod
echo 3. Change game folder
echo 4. Exit
if defined MOD_MANAGER_TEST_ACTIONS (
    for /f "tokens=1,* delims=," %%A in ("!MOD_MANAGER_TEST_ACTIONS!") do (
        set "ACTION=%%A"
        set "MOD_MANAGER_TEST_ACTIONS=%%B"
    )
) else if defined MOD_MANAGER_TEST_ACTION (
    set "ACTION=%MOD_MANAGER_TEST_ACTION%"
) else (
    set /p "ACTION=Choose an option: "
)

if "%ACTION%"=="1" goto :add_mod
if "%ACTION%"=="2" goto :remove_mod
if "%ACTION%"=="3" goto :change_game_dir
if "%ACTION%"=="4" goto :end

echo Invalid option.
goto :main_menu

:load_game_dir_config
set "GAME_DIR="
if not exist "!CONFIG_PATH!" exit /b 1

for /f "usebackq tokens=1,* delims=:" %%A in ("!CONFIG_PATH!") do (
    if /i "%%A"=="game_dir" (
        set "GAME_DIR=%%B"
        if "!GAME_DIR:~0,1!"==" " set "GAME_DIR=!GAME_DIR:~1!"
    )
)

if not defined GAME_DIR exit /b 1

call :validate_game_dir
exit /b %ERRORLEVEL%

:save_game_dir_config
> "!CONFIG_PATH!" echo game_dir: !GAME_DIR!
exit /b 0

:choose_dir
if defined MOD_MANAGER_TEST_GAME_DIR (
    set "GAME_DIR=%MOD_MANAGER_TEST_GAME_DIR%"
    goto :validate_game_dir
)

set "GAME_PICK_FILE=%TEMP%\sts2_mod_manager_game_dir.txt"
del /q "%GAME_PICK_FILE%" 2>nul
"%POWERSHELL_EXE%" -NoProfile -STA -ExecutionPolicy Bypass -Command "$shell = New-Object -ComObject Shell.Application; $folder = $shell.BrowseForFolder(0, 'Select the Slay the Spire 2 installation folder. Steam: Slay the Spire 2 - Manage - Browse local files.', 0, 0); if ($null -ne $folder) { [Console]::WriteLine($folder.Self.Path) }" > "%GAME_PICK_FILE%"
set /p "GAME_DIR="<"%GAME_PICK_FILE%"
del /q "%GAME_PICK_FILE%" 2>nul

:validate_game_dir
if not defined GAME_DIR (
    echo No folder selected.
    exit /b 1
)

if not exist "!GAME_DIR!\data_sts2_windows_x86_64\sts2.dll" (
    echo.
    echo This folder does not look like a Slay the Spire 2 installation:
    echo !GAME_DIR!
    echo Expected file:
    echo !GAME_DIR!\data_sts2_windows_x86_64\sts2.dll
    exit /b 1
)

exit /b 0

:change_game_dir
echo.
echo Select the new Slay the Spire 2 installation folder.
echo Tip: Steam -^> Slay the Spire 2 -^> Manage -^> Browse local files
echo.
call :choose_dir
if errorlevel 1 goto :main_menu
call :save_game_dir_config
set "GAME_MODS=!GAME_DIR!\mods"
if not exist "!GAME_MODS!" mkdir "!GAME_MODS!"
call :ensure_baselib_installed
if errorlevel 1 goto :fatal_baselib_error
echo Saved game folder:
echo !GAME_DIR!
goto :main_menu

:ensure_baselib_installed
set "BASELIB_DIR=!GAME_MODS!\BaseLib"
set "BASELIB_MANUAL_URL=https://github.com/Alchyr/BaseLib-StS2/releases/latest"
set "BASELIB_DOWNLOAD_ERROR="
if defined MOD_MANAGER_TEST_BUNDLED_BASELIB_DIR (
    set "BUNDLED_BASELIB_DIR=%MOD_MANAGER_TEST_BUNDLED_BASELIB_DIR%"
) else (
    set "BUNDLED_BASELIB_DIR=%SCRIPT_DIR%third_party\BaseLib"
)
if defined MOD_MANAGER_TEST_SKIP_BASELIB (
    echo Skipping BaseLib check because MOD_MANAGER_TEST_SKIP_BASELIB is set.
    exit /b 0
)

if exist "!BASELIB_DIR!\BaseLib.dll" if exist "!BASELIB_DIR!\BaseLib.pck" if exist "!BASELIB_DIR!\BaseLib.json" exit /b 0

echo.
echo BaseLib is missing or incomplete. Installing BaseLib...

if defined MOD_MANAGER_TEST_BASELIB_SOURCE (
    call :install_baselib_from_local "%MOD_MANAGER_TEST_BASELIB_SOURCE%"
) else (
    call :install_baselib_from_github
    if errorlevel 1 (
        echo.
        echo Trying bundled BaseLib fallback...
        call :install_baselib_from_local "!BUNDLED_BASELIB_DIR!"
    )
)
if errorlevel 1 exit /b 1

if exist "!BASELIB_DIR!\BaseLib.dll" if exist "!BASELIB_DIR!\BaseLib.pck" if exist "!BASELIB_DIR!\BaseLib.json" (
    echo Installed BaseLib to:
    echo !BASELIB_DIR!
    exit /b 0
)

echo BaseLib installation did not create all required files.
echo Required files: BaseLib.dll, BaseLib.pck, BaseLib.json
exit /b 1

:install_baselib_from_local
set "BASELIB_SOURCE=%~1"
if not exist "!BASELIB_SOURCE!\BaseLib.dll" (
    echo Missing BaseLib.dll in !BASELIB_SOURCE!
    exit /b 1
)
if not exist "!BASELIB_SOURCE!\BaseLib.pck" (
    echo Missing BaseLib.pck in !BASELIB_SOURCE!
    exit /b 1
)
if not exist "!BASELIB_SOURCE!\BaseLib.json" (
    echo Missing BaseLib.json in !BASELIB_SOURCE!
    exit /b 1
)
if not exist "!BASELIB_DIR!" mkdir "!BASELIB_DIR!"
copy /y "!BASELIB_SOURCE!\BaseLib.dll" "!BASELIB_DIR!\BaseLib.dll" >nul
copy /y "!BASELIB_SOURCE!\BaseLib.pck" "!BASELIB_DIR!\BaseLib.pck" >nul
copy /y "!BASELIB_SOURCE!\BaseLib.json" "!BASELIB_DIR!\BaseLib.json" >nul
exit /b 0

:install_baselib_from_github
if defined MOD_MANAGER_TEST_BASELIB_DOWNLOAD_FAIL (
    set "BASELIB_DOWNLOAD_ERROR=Simulated GitHub BaseLib download failure."
    echo !BASELIB_DOWNLOAD_ERROR!
    exit /b 1
)
if not exist "!BASELIB_DIR!" mkdir "!BASELIB_DIR!"
set "BASELIB_RELEASE_API=https://api.github.com/repos/Alchyr/BaseLib-StS2/releases/latest"
set "BASELIB_ERROR_FILE=%TEMP%\sls2-baselib-download-error-%RANDOM%-%RANDOM%.txt"
set "BASELIB_DIR_ENV=!BASELIB_DIR!"
set "BASELIB_RELEASE_API_ENV=!BASELIB_RELEASE_API!"
set "BASELIB_ERROR_FILE_ENV=!BASELIB_ERROR_FILE!"
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; try { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; $target = $env:BASELIB_DIR_ENV; $api = $env:BASELIB_RELEASE_API_ENV; $headers = @{ 'User-Agent' = 'sls2mods-manager' }; New-Item -ItemType Directory -Force -Path $target | Out-Null; $release = Invoke-RestMethod -Headers $headers -Uri $api -TimeoutSec 20; foreach ($name in @('BaseLib.dll', 'BaseLib.pck', 'BaseLib.json')) { $asset = $release.assets | Where-Object { $_.name -eq $name } | Select-Object -First 1; if ($null -eq $asset) { throw ('Missing BaseLib release asset: ' + $name) }; Invoke-WebRequest -UseBasicParsing -Headers $headers -Uri $asset.browser_download_url -OutFile (Join-Path $target $name) -TimeoutSec 30 } } catch { Set-Content -Path $env:BASELIB_ERROR_FILE_ENV -Value $_.Exception.Message -Encoding UTF8; exit 1 }"
if errorlevel 1 (
    echo Failed to download BaseLib from GitHub.
    if exist "!BASELIB_ERROR_FILE!" (
        set /p "BASELIB_DOWNLOAD_ERROR="<"!BASELIB_ERROR_FILE!"
        del /q "!BASELIB_ERROR_FILE!" 2>nul
        echo Reason: !BASELIB_DOWNLOAD_ERROR!
    )
    exit /b 1
)
del /q "!BASELIB_ERROR_FILE!" 2>nul
exit /b 0

:fatal_baselib_error
echo.
echo Could not install BaseLib automatically.
if defined BASELIB_DOWNLOAD_ERROR (
    echo Last download error:
    echo !BASELIB_DOWNLOAD_ERROR!
)
echo.
echo The script tried GitHub first, then the bundled BaseLib fallback:
echo !BUNDLED_BASELIB_DIR!
echo.
echo Please install BaseLib manually from:
echo !BASELIB_MANUAL_URL!
echo Then copy BaseLib.dll, BaseLib.pck, and BaseLib.json to:
echo !BASELIB_DIR!
call :pause_before_exit
set "EXIT_CODE=1"
goto :end

:fatal_no_game_dir
call :fatal_error "No valid Slay the Spire 2 folder was selected."
goto :end

:fatal_error
echo.
echo %~1
call :pause_before_exit
set "EXIT_CODE=1"
goto :end

:pause_before_exit
if defined MOD_MANAGER_TEST_NO_PAUSE exit /b 0
echo.
pause
exit /b 0

:add_mod
call :list_repo_mods
if "%MOD_COUNT%"=="0" (
    echo No installable mods were found under "%SCRIPT_DIR%mods".
    goto :main_menu
)

echo.
set /p "MOD_CHOICE=Choose a mod to add: "
call :resolve_choice "%MOD_CHOICE%"
if errorlevel 1 goto :main_menu

call :install_mod "!SELECTED_MOD!"
goto :main_menu

:list_repo_mods
set "MOD_COUNT=0"
echo.
echo Available mods:
for /d %%A in ("mods\*") do (
    call :ensure_repo_mod_built "%%~fA" "%%~nxA"
    if exist "%%A\dist\%%~nxA.dll" if exist "%%A\dist\%%~nxA.json" (
        set /a MOD_COUNT+=1
        set "MOD_!MOD_COUNT!=%%~nxA"
        echo !MOD_COUNT!. %%~nxA
    )
)
exit /b 0

:ensure_repo_mod_built
set "REPO_MOD_DIR=%~1"
set "REPO_MOD_ID=%~2"
if exist "%REPO_MOD_DIR%\dist\%REPO_MOD_ID%.dll" if exist "%REPO_MOD_DIR%\dist\%REPO_MOD_ID%.json" exit /b 0
if not exist "%REPO_MOD_DIR%\build.ps1" exit /b 0

echo Building %REPO_MOD_ID%...
"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%REPO_MOD_DIR%\build.ps1" -GameDir "!GAME_DIR!"
if errorlevel 1 (
    echo Could not build %REPO_MOD_ID%. It will not be listed until its dist files exist.
    exit /b 1
)
exit /b 0

:resolve_choice
set "SELECTED_MOD="
set "CHOICE=%~1"
if not defined CHOICE (
    echo No option selected.
    exit /b 1
)

for /l %%N in (1,1,%MOD_COUNT%) do (
    if "!CHOICE!"=="%%N" set "SELECTED_MOD=!MOD_%%N!"
)

if not defined SELECTED_MOD (
    echo Invalid option.
    exit /b 1
)

exit /b 0

:install_mod
set "MOD_ID=%~1"
set "SOURCE_DIR=%SCRIPT_DIR%mods\%MOD_ID%"
set "DIST_DIR=%SOURCE_DIR%\dist"
set "TARGET_DIR=%GAME_MODS%\%MOD_ID%"

echo.
echo Installing %MOD_ID%...

if not exist "%DIST_DIR%\%MOD_ID%.dll" (
    echo Missing required file: %DIST_DIR%\%MOD_ID%.dll
    exit /b 1
)

if not exist "%DIST_DIR%\%MOD_ID%.json" (
    echo Missing required file: %DIST_DIR%\%MOD_ID%.json
    exit /b 1
)

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
copy /y "%DIST_DIR%\%MOD_ID%.dll" "%TARGET_DIR%\%MOD_ID%.dll" >nul
copy /y "%DIST_DIR%\%MOD_ID%.json" "%TARGET_DIR%\%MOD_ID%.json" >nul

del /q "%TARGET_DIR%\%MOD_ID%.pck" 2>nul
del /q "%TARGET_DIR%\%MOD_ID%Config.json" 2>nul

call :init_mod_config
if /i "%MOD_ID%"=="VakuuRoomInjection" call :cleanup_old_vakuu_install

echo Installed %MOD_ID% to:
echo %TARGET_DIR%
exit /b 0

:init_mod_config
set "CONFIG_DIR=%APPDATA%\SlayTheSpire2\mod_configs"
set "CONFIG_PATH=%CONFIG_DIR%\%MOD_ID%Config.json"
set "EXAMPLE_CONFIG=%DIST_DIR%\%MOD_ID%Config.json.example"
if not exist "%EXAMPLE_CONFIG%" set "EXAMPLE_CONFIG=%SCRIPT_DIR%mods\%MOD_ID%\%MOD_ID%Config.json.example"

if not exist "%CONFIG_DIR%" mkdir "%CONFIG_DIR%"
if exist "%CONFIG_PATH%" exit /b 0

if /i "%MOD_ID%"=="VakuuRoomInjection" call :migrate_old_vakuu_config
if exist "%CONFIG_PATH%" exit /b 0
if exist "%EXAMPLE_CONFIG%" copy /y "%EXAMPLE_CONFIG%" "%CONFIG_PATH%" >nul

exit /b 0

:migrate_old_vakuu_config
set "OLD_CONFIG_PATH=%CONFIG_DIR%\MapNodeChangerConfig.json"
set "CONFIG_PATH=%CONFIG_DIR%\VakuuRoomInjectionConfig.json"
if exist "%CONFIG_PATH%" exit /b 0
if exist "%OLD_CONFIG_PATH%" copy /y "%OLD_CONFIG_PATH%" "%CONFIG_PATH%" >nul
exit /b 0

:cleanup_old_vakuu_install
set "OLD_TARGET_DIR=%GAME_MODS%\MapNodeChanger"
if exist "%OLD_TARGET_DIR%" rmdir /s /q "%OLD_TARGET_DIR%"
del /q "%TARGET_DIR%\MapNodeChanger.pck" 2>nul
del /q "%TARGET_DIR%\MapNodeChangerConfig.json" 2>nul
exit /b 0

:remove_mod
call :list_installed_mods
if "%MOD_COUNT%"=="0" (
    echo No installed mods were found under "!GAME_MODS!".
    goto :main_menu
)

echo.
if defined MOD_MANAGER_TEST_MOD_CHOICE (
    set "MOD_CHOICE=%MOD_MANAGER_TEST_MOD_CHOICE%"
) else (
    set /p "MOD_CHOICE=Choose a mod to remove: "
)
call :resolve_choice "%MOD_CHOICE%"
if errorlevel 1 goto :main_menu

if /i "!SELECTED_MOD!"=="BaseLib" (
    call :restore_vanilla_mods
    goto :main_menu
)

set "TARGET_DIR=!GAME_MODS!\!SELECTED_MOD!"
echo.
echo Removing !SELECTED_MOD!...
rmdir /s /q "%TARGET_DIR%"
echo Removed !SELECTED_MOD! from:
echo !TARGET_DIR!
goto :main_menu

:restore_vanilla_mods
echo.
echo Removing BaseLib restores the vanilla game mod folder.
echo This deletes every installed mod folder under:
echo !GAME_MODS!
for /d %%A in ("!GAME_MODS!\*") do (
    rmdir /s /q "%%~fA"
)
echo Restored vanilla game mods.
exit /b 0

:list_installed_mods
set "MOD_COUNT=0"
echo.
echo Installed mods:
if not exist "!GAME_MODS!" exit /b 0
for /d %%A in ("!GAME_MODS!\*") do (
    set /a MOD_COUNT+=1
    set "MOD_!MOD_COUNT!=%%~nxA"
    echo !MOD_COUNT!. %%~nxA
)
exit /b 0

:end
if defined EXIT_CODE (
    set "FINAL_EXIT_CODE=!EXIT_CODE!"
) else (
    set "FINAL_EXIT_CODE=0"
)
popd >nul
endlocal & exit /b %FINAL_EXIT_CODE%
