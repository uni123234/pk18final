@echo off
setlocal
title Modular RPG Platform - GUI Launcher
cd /d "%~dp0"

if exist "dist\standalone-win-x64\ModularRpgApp.exe" (
    start "" "dist\standalone-win-x64\ModularRpgApp.exe"
    exit /b 0
)

if exist "dist\portable-win-x64\ModularRpgApp.exe" (
    start "" "dist\portable-win-x64\ModularRpgApp.exe"
    exit /b 0
)

if exist "bin\Release\net8.0-windows\ModularRpgApp.exe" (
    start "" "bin\Release\net8.0-windows\ModularRpgApp.exe"
    exit /b 0
)

where dotnet >nul 2>nul
if %ERRORLEVEL% equ 0 (
    echo Starting Modular RPG Platform via dotnet run...
    dotnet run --project Project.csproj
    exit /b %ERRORLEVEL%
)

echo [ERROR] No compiled executable or .NET runner found.
echo Please execute publish.bat or install the .NET SDK.
pause
exit /b 1
