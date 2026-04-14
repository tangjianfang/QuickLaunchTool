@echo off
setlocal

set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Release

set BUILD_DIR=build

echo [1/2] Configuring with CMake (VS 2022, x64)...
cmake -B "%BUILD_DIR%" -G "Visual Studio 17 2022" -A x64
if errorlevel 1 (
    echo.
    echo ERROR: CMake configuration failed.
    exit /b 1
)

echo.
echo [2/2] Building %CONFIG%...
cmake --build "%BUILD_DIR%" --config %CONFIG% --parallel
if errorlevel 1 (
    echo.
    echo ERROR: Build failed.
    exit /b 1
)

echo.
echo Build succeeded.
echo Output: bin\%CONFIG%\QuickLaunchToolCpp.exe
