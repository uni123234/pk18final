@echo off
setlocal
title Modular RPG Platform - Uninstaller
color 0C

echo ================================================================
echo   MODULAR RPG PLATFORM - UNINSTALLER
echo ================================================================
echo.
set /p CONFIRM="Are you sure you want to uninstall and remove shortcuts? (Y/N): "
if /i not "%CONFIRM%"=="Y" (
    echo Uninstallation cancelled.
    exit /b 0
)

set "SHORTCUT=%USERPROFILE%\Desktop\Modular RPG Platform.lnk"
if exist "%SHORTCUT%" (
    del "%SHORTCUT%"
    echo   [OK] Desktop shortcut removed.
)

set "APP_DIR=%LOCALAPPDATA%\ModularRpgPlatform"
if exist "%APP_DIR%" (
    echo   [INFO] User data found in %APP_DIR%
    set /p DELDATA="Delete user cache and save games too? (Y/N): "
    if /i "!DELDATA!"=="Y" (
        rmdir /s /q "%APP_DIR%"
        echo   [OK] User data directory removed.
    )
)

echo.
echo [OK] Uninstallation complete.
pause
endlocal
