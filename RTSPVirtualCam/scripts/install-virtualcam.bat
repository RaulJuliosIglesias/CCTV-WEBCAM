@echo off
:: ===========================================
:: RTSP VirtualCam - Virtual Camera Installer
:: For Windows 10 (uses SoftCam DirectShow)
:: ===========================================
:: Run this script as Administrator!

echo.
echo ========================================
echo  RTSP VirtualCam - Virtual Camera Setup
echo ========================================
echo.

:: Check for admin
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] This script requires Administrator privileges!
    echo.
    echo Right-click and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

:: Check Windows version
for /f "tokens=4-5 delims=. " %%i in ('ver') do set VERSION=%%i.%%j
echo [INFO] Detected Windows version: %VERSION%

:: Set paths
set SCRIPT_DIR=%~dp0
set SOFTCAM_DLL=%SCRIPT_DIR%softcam\softcam.dll
set SOFTCAM_DLL_X86=%SCRIPT_DIR%softcam\softcam_x86.dll

:: Check if DLL exists
if not exist "%SOFTCAM_DLL%" (
    echo.
    echo [ERROR] SoftCam DLL not found at: %SOFTCAM_DLL%
    echo.
    echo Please download SoftCam from:
    echo   https://github.com/tshino/softcam/releases
    echo.
    echo Then extract softcam.dll to:
    echo   %SCRIPT_DIR%softcam\
    echo.
    pause
    exit /b 1
)

:: Register the DLL
echo.
echo [STEP 1] Registering 64-bit virtual camera...
regsvr32 /s "%SOFTCAM_DLL%"
if %errorLevel% neq 0 (
    echo [WARNING] 64-bit registration may have failed
) else (
    echo [OK] 64-bit virtual camera registered
)

:: Optional: Register 32-bit version if exists
if exist "%SOFTCAM_DLL_X86%" (
    echo.
    echo [STEP 2] Registering 32-bit virtual camera...
    regsvr32 /s "%SOFTCAM_DLL_X86%"
    if %errorLevel% neq 0 (
        echo [WARNING] 32-bit registration may have failed
    ) else (
        echo [OK] 32-bit virtual camera registered
    )
)

echo.
echo ========================================
echo  INSTALLATION COMPLETE!
echo ========================================
echo.
echo The virtual camera "SoftCam" is now available.
echo.
echo To use it:
echo   1. Open RTSP VirtualCam
echo   2. Connect to your RTSP stream
echo   3. Click "Virtualize as Webcam"
echo   4. Select "SoftCam" in your video app
echo.
echo To uninstall, run: uninstall-virtualcam.bat
echo.
pause
