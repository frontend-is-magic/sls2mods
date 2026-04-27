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
if errorlevel 1 goto :end
call :save_game_dir_config

:after_game_dir

set "GAME_MODS=!GAME_DIR!\mods"
if not exist "!GAME_MODS!" mkdir "!GAME_MODS!"

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
echo Saved game folder:
echo !GAME_DIR!
goto :main_menu

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
    if exist "%%A\dist\%%~nxA.dll" if exist "%%A\dist\%%~nxA.json" (
        set /a MOD_COUNT+=1
        set "MOD_!MOD_COUNT!=%%~nxA"
        echo !MOD_COUNT!. %%~nxA
    )
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
set "EXAMPLE_CONFIG=%SCRIPT_DIR%mods\%MOD_ID%\%MOD_ID%Config.json.example"

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
set /p "MOD_CHOICE=Choose a mod to remove: "
call :resolve_choice "%MOD_CHOICE%"
if errorlevel 1 goto :main_menu

set "TARGET_DIR=!GAME_MODS!\!SELECTED_MOD!"
echo.
echo Removing !SELECTED_MOD!...
rmdir /s /q "%TARGET_DIR%"
echo Removed !SELECTED_MOD! from:
echo !TARGET_DIR!
goto :main_menu

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
popd >nul
endlocal
