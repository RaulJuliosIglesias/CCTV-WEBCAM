@echo off
:: ===========================================
:: RTSP VirtualCam - Virtual Camera Installer
:: Uses OBS Virtual Camera (works with Chrome/Zoom/Teams)
:: ===========================================
:: Run this script as Administrator!

@cd /d "%~dp0"

:: Create log file
set LOG_FILE=%~dp0install_log.txt
echo. > "%LOG_FILE%"
echo [%date% %time%] Installation started >> "%LOG_FILE%"

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

echo [OK] Running as Administrator
echo.

:: Set paths - DLLs are included in the package
set SCRIPT_DIR=%~dp0
set DRIVER_DIR=%SCRIPT_DIR%softcam
set DLL64=%DRIVER_DIR%\obs-virtualcam-module64.dll
set DLL32=%DRIVER_DIR%\obs-virtualcam-module32.dll

:: Verify DLLs exist
echo [STEP 1] Verifying driver files...
echo [INFO] 64-bit DLL: %DLL64% >> "%LOG_FILE%"
echo [INFO] 32-bit DLL: %DLL32% >> "%LOG_FILE%"

if not exist "%DLL64%" (
    echo [ERROR] obs-virtualcam-module64.dll not found!
    echo [ERROR] The driver files are missing from the package.
    echo.
    echo Expected location: %DLL64%
    echo.
    pause
    exit /b 1
)

echo [OK] obs-virtualcam-module64.dll found
dir "%DLL64%" >> "%LOG_FILE%"

if exist "%DLL32%" (
    echo [OK] obs-virtualcam-module32.dll found
    dir "%DLL32%" >> "%LOG_FILE%"
)

:: Check if already installed
echo.
echo [STEP 2] Checking for existing installation...

reg query "HKLM\SOFTWARE\Classes\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] 64-bit Virtual Cam already registered
    echo [INFO] 64-bit already registered >> "%LOG_FILE%"
) else (
    echo [INFO] 64-bit Virtual Cam not found, installing...
    goto install64
)

reg query "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    echo [INFO] 32-bit Virtual Cam already registered
    echo [INFO] 32-bit already registered >> "%LOG_FILE%"
    goto endSuccess
) else (
    echo [INFO] 32-bit Virtual Cam not found, installing...
    goto install32
)

:install32
echo.
echo [STEP 3] Registering 32-bit virtual camera...
echo [INFO] Running: regsvr32 /i /s "%DLL32%" >> "%LOG_FILE%"

regsvr32.exe /i /s "%DLL32%"

reg query "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] 32-bit Virtual Cam successfully installed
    echo [OK] 32-bit installed >> "%LOG_FILE%"
) else (
    echo [WARNING] 32-bit Virtual Cam installation may have failed
    echo [WARNING] 32-bit failed >> "%LOG_FILE%"
)
goto checkInstall64

:install64
echo.
echo [STEP 4] Registering 64-bit virtual camera...
echo [INFO] Running: regsvr32 /i /s "%DLL64%" >> "%LOG_FILE%"

regsvr32.exe /i /s "%DLL64%"

reg query "HKLM\SOFTWARE\Classes\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] 64-bit Virtual Cam successfully installed
    echo [OK] 64-bit installed >> "%LOG_FILE%"
) else (
    echo [ERROR] 64-bit Virtual Cam installation failed!
    echo [ERROR] 64-bit failed >> "%LOG_FILE%"
    echo.
    echo Trying with visible dialog...
    regsvr32.exe /i "%DLL64%"
)

:checkInstall64
reg query "HKLM\SOFTWARE\Classes\WOW6432Node\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}" >nul 2>&1
if %errorLevel% neq 0 (
    goto install32
)

:endSuccess
echo.
echo ========================================
echo  INSTALLATION COMPLETE!
echo ========================================
echo.
echo Virtual camera "OBS Virtual Camera" is now registered.
echo.
echo IMPORTANT:
echo   1. RESTART your video apps (Zoom, Teams, Chrome, etc.)
echo   2. Look for "OBS Virtual Camera" in camera list
echo.
echo Log saved to: %LOG_FILE%
echo.
pause
exit /b 0
