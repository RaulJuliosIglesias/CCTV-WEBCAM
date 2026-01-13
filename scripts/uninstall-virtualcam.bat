@echo off
:: ===========================================
:: RTSP VirtualCam - Virtual Camera Uninstaller
:: Uses OBS Virtual Camera
:: ===========================================
:: Run this script as Administrator!

@cd /d "%~dp0"

echo.
echo ========================================
echo  RTSP VirtualCam - Virtual Camera Removal
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

echo [OK] Running as Administrator
echo.

:: Set paths
set SCRIPT_DIR=%~dp0
set DRIVER_DIR=%SCRIPT_DIR%softcam
set DLL64=%DRIVER_DIR%\obs-virtualcam-module64.dll
set DLL32=%DRIVER_DIR%\obs-virtualcam-module32.dll

:: Unregister 64-bit DLL
echo [STEP 1] Unregistering 64-bit virtual camera...
reg query "HKLM\SOFTWARE\Classes\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    regsvr32.exe /u /s "%DLL64%"
    echo [OK] 64-bit virtual camera unregistered
) else (
    echo [INFO] 64-bit virtual camera not registered, skipping
)

:: Unregister 32-bit DLL
echo.
echo [STEP 2] Unregistering 32-bit virtual camera...
reg query "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    regsvr32.exe /u /s "%DLL32%"
    echo [OK] 32-bit virtual camera unregistered
) else (
    echo [INFO] 32-bit virtual camera not registered, skipping
)

echo.
echo ========================================
echo  UNINSTALL COMPLETE!
echo ========================================
echo.
echo The virtual camera "OBS Virtual Camera" has been removed.
echo.
echo IMPORTANT: Restart your video apps to see the change.
echo.
pause
exit /b 0
