@echo off
:: ===========================================
:: RTSP VirtualCam - Virtual Camera Uninstaller
:: Removes SoftCam DirectShow filter
:: ===========================================
:: Run this script as Administrator!

echo.
echo ========================================
echo  RTSP VirtualCam - Uninstall Virtual Cam
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

:: Set paths
set SCRIPT_DIR=%~dp0
set SOFTCAM_DLL=%SCRIPT_DIR%softcam\softcam.dll
set SOFTCAM_DLL_X86=%SCRIPT_DIR%softcam\softcam_x86.dll

echo.
echo [STEP 1] Unregistering 64-bit virtual camera...
if exist "%SOFTCAM_DLL%" (
    regsvr32 /u /s "%SOFTCAM_DLL%"
    echo [OK] 64-bit virtual camera unregistered
) else (
    echo [SKIP] 64-bit DLL not found
)

echo.
echo [STEP 2] Unregistering 32-bit virtual camera...
if exist "%SOFTCAM_DLL_X86%" (
    regsvr32 /u /s "%SOFTCAM_DLL_X86%"
    echo [OK] 32-bit virtual camera unregistered
) else (
    echo [SKIP] 32-bit DLL not found
)

echo.
echo ========================================
echo  UNINSTALL COMPLETE!
echo ========================================
echo.
echo The virtual camera has been removed from your system.
echo.
echo You can safely delete the softcam folder now.
echo.
pause
