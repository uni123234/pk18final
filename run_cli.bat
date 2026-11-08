@echo off
setlocal
title Modular RPG Platform - CLI Terminal
cd /d "%~dp0"

if exist "dist\standalone-win-x64\ModularRpgApp.exe" (
    "dist\standalone-win-x64\ModularRpgApp.exe" --cli
    exit /b %ERRORLEVEL%
)

if exist "dist\portable-win-x64\ModularRpgApp.exe" (
    "dist\portable-win-x64\ModularRpgApp.exe" --cli
    exit /b %ERRORLEVEL%
)

if exist "bin\Release\net8.0-windows\ModularRpgApp.exe" (
    "bin\Release\net8.0-windows\ModularRpgApp.exe" --cli
    exit /b %ERRORLEVEL%
)

where dotnet >nul 2>nul
if %ERRORLEVEL% equ 0 (
    dotnet run --project Project.csproj -- --cli
    exit /b %ERRORLEVEL%
)

echo [ERROR] No compiled executable or .NET runner found.
pause
exit /b 1
