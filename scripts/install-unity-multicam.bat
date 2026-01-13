@echo off
:: ===========================================
:: Unity Capture - Multiple Virtual Camera Installer
:: Registers multiple Unity Capture devices
:: ===========================================
:: Run this script as Administrator!

@cd /d "%~dp0"

echo.
echo ========================================
echo  Unity Capture - Multi-Camera Setup
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
set DLL64=%SCRIPT_DIR%softcam\UnityCaptureFilter64.dll
set DLL32=%SCRIPT_DIR%softcam\UnityCaptureFilter32.dll

:: Prompt for number of devices
set /p NUM_DEVICES="Enter number of virtual cameras to install (1-10): "

if "%NUM_DEVICES%"=="" set NUM_DEVICES=1

:: Validate input
if %NUM_DEVICES% LSS 1 set NUM_DEVICES=1
if %NUM_DEVICES% GTR 10 set NUM_DEVICES=10

echo.
echo [INFO] Installing %NUM_DEVICES% Unity Capture device(s)...
echo.

:: Verify DLLs exist
if not exist "%DLL64%" (
    echo [ERROR] UnityCaptureFilter64.dll not found!
    echo Expected: %DLL64%
    pause
    exit /b 1
)

echo [OK] Found UnityCaptureFilter64.dll

:: Register 64-bit DLL with number of devices
echo.
echo [STEP 1] Registering 64-bit virtual cameras...
regsvr32.exe /s /n /i:%NUM_DEVICES% "%DLL64%"

if %errorLevel% neq 0 (
    echo [WARNING] Registration may have failed, trying with dialog...
    regsvr32.exe /n /i:%NUM_DEVICES% "%DLL64%"
) else (
    echo [OK] 64-bit virtual cameras registered
)

:: Register 32-bit DLL if exists
if exist "%DLL32%" (
    echo.
    echo [STEP 2] Registering 32-bit virtual cameras...
    regsvr32.exe /s /n /i:%NUM_DEVICES% "%DLL32%"
    
    if %errorLevel% neq 0 (
        echo [WARNING] 32-bit registration may have failed
    ) else (
        echo [OK] 32-bit virtual cameras registered
    )
)

:: List registered devices
echo.
echo [STEP 3] Verifying registration...
reg query "HKCR\CLSID" /s /f "Unity Video Capture" 2>nul | findstr /i "FriendlyName"

echo.
echo ========================================
echo  INSTALLATION COMPLETE!
echo ========================================
echo.
echo %NUM_DEVICES% Unity Capture virtual camera(s) installed:

for /L %%i in (0,1,%NUM_DEVICES%) do (
    if %%i LSS %NUM_DEVICES% (
        if %%i==0 (
            echo   - Unity Video Capture
        ) else (
            echo   - Unity Video Capture #%%i
        )
    )
)

echo.
echo IMPORTANT:
echo   1. RESTART your video apps (Zoom, Teams, Chrome, etc.)
echo   2. Select "Unity Video Capture" in camera list
echo.
pause
exit /b 0
