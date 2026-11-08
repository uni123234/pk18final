@echo off
setlocal EnableDelayedExpansion
title Modular RPG Platform - Setup & Installer
color 0B

echo ================================================================
echo   MODULAR RPG PLATFORM AND CHARACTER CATALOG - INSTALLER
echo ================================================================
echo.

echo [1/4] Checking System Architecture...
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    echo   [OK] 64-bit Architecture detected (%PROCESSOR_ARCHITECTURE%).
) else if "%PROCESSOR_ARCHITEW6432%"=="AMD64" (
    echo   [OK] 64-bit WOW64 Architecture detected.
) else (
    echo   [WARNING] 32-bit Architecture detected. 64-bit build recommended.
)

echo.
echo [2/4] Checking .NET Desktop Runtime Environment...
where dotnet >nul 2>nul
if %ERRORLEVEL% equ 0 (
    dotnet --list-runtimes 2>nul | findstr /i "Microsoft.WindowsDesktop.App" >nul
    if !ERRORLEVEL! equ 0 (
        echo   [OK] .NET Windows Desktop Runtime found.
    ) else (
        echo   [INFO] Native desktop runtime not explicitly listed.
        echo   [INFO] Application standalone build includes bundled runtime.
    )
) else (
    echo   [INFO] Standalone distribution mode selected (bundled CLR).
)

echo.
echo [3/4] Preparing User Storage Directories in LocalAppData...
set "APP_DIR=%LOCALAPPDATA%\ModularRpgPlatform"
if not exist "%APP_DIR%" mkdir "%APP_DIR%"
if not exist "%APP_DIR%\cache\portraits" mkdir "%APP_DIR%\cache\portraits"
if not exist "%APP_DIR%\saves" mkdir "%APP_DIR%\saves"
echo   [OK] Local cache directory initialized: %APP_DIR%\cache\portraits
echo   [OK] Local saves directory initialized: %APP_DIR%\saves

echo.
echo [4/4] Creating Desktop Shortcut...
set "SCRIPT_DIR=%~dp0"
set "SHORTCUT_PATH=%USERPROFILE%\Desktop\Modular RPG Platform.lnk"
set "TARGET_EXE=%SCRIPT_DIR%run.bat"

powershell -NoProfile -Command ^
    "$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('%SHORTCUT_PATH%'); $s.TargetPath = '%TARGET_EXE%'; $s.WorkingDirectory = '%SCRIPT_DIR%'; $s.Description = 'Modular RPG Platform and Character Catalog'; $s.Save()"

if exist "%SHORTCUT_PATH%" (
    echo   [OK] Desktop shortcut created successfully: %SHORTCUT_PATH%
) else (
    echo   [INFO] Desktop shortcut creation skipped. Launcher script available at run.bat
)

echo.
echo ================================================================
echo   INSTALLATION COMPLETED SUCCESSFULLY!
echo   Launch via:
echo     - Desktop Shortcut: "Modular RPG Platform"
echo     - Desktop GUI:      run.bat
echo     - Terminal CLI:     run_cli.bat
echo ================================================================
pause
endlocal
