@echo off
setlocal
echo ========================================================
echo  MODULAR RPG PLATFORM - AUTOMATED BUILD PIPELINE
echo  Configuration: Release (Optimized, No Debug Bloat)
echo ========================================================
echo.

where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo [ERROR] .NET SDK is required to build from source.
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

echo [1/2] Restoring solution dependencies...
dotnet restore Project.sln
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Dependency restoration failed.
    exit /b %ERRORLEVEL%
)

echo [2/2] Compiling solution in Release configuration...
dotnet build Project.sln -c Release --no-restore
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Release build failed.
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo  BUILD SUCCEEDED! Binaries generated in bin/Release/
echo ========================================================
endlocal
