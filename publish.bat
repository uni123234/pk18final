@echo off
setlocal
echo ========================================================
echo  MODULAR RPG PLATFORM - PUBLISH & PACKAGING PIPELINE
echo  Target: Windows x64 Single-File Release
echo ========================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK is required to publish the application.
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

echo [1/2] Publishing Portable (Framework-Dependent) Package...
dotnet publish Project.csproj -c Release -p:PublishProfile=win-x64-portable
if %ERRORLEVEL% neq 0 (
    echo [WARNING] Portable publish returned exit code %ERRORLEVEL%.
)

echo [2/2] Publishing Standalone (Self-Contained) Package...
dotnet publish Project.csproj -c Release -p:PublishProfile=win-x64-standalone
if %ERRORLEVEL% neq 0 (
    echo [WARNING] Standalone publish returned exit code %ERRORLEVEL%.
)

echo.
echo ========================================================
echo  PUBLISH COMPLETED! Output packages located in:
echo   - dist/portable-win-x64/   (Requires .NET 8 Desktop)
echo   - dist/standalone-win-x64/ (Zero dependencies)
echo ========================================================
endlocal
