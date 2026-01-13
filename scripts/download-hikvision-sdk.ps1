#!/usr/bin/env pwsh
# =====================================================
# Hikvision SDK Downloader
# Downloads and extracts HCNetSDK.dll for PTZ control
# =====================================================

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Hikvision SDK Downloader" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir
$binDir = Join-Path $projectRoot "src\RTSPVirtualCam\bin\Debug\net8.0-windows\win-x64"
$tempDir = Join-Path $env:TEMP "hikvision_sdk_temp"
$sdkDir = Join-Path $tempDir "sdk"

# Create directories
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
New-Item -ItemType Directory -Force -Path $binDir | Out-Null

Write-Host "📦 Downloading Hikvision Device Network SDK..." -ForegroundColor Yellow
Write-Host ""

# SDK Download URL (official mirror)
$sdkUrl = "https://www.hikvision.com/content/dam/hikvision/en/support/download/sdk/CH-HCNetSDK_Win64.zip"
$zipFile = Join-Path $tempDir "HCNetSDK.zip"

try {
    # Download SDK
    Write-Host "⬇️  Downloading from Hikvision..." -ForegroundColor Cyan
    Invoke-WebRequest -Uri $sdkUrl -OutFile $zipFile -UseBasicParsing
    
    Write-Host "✅ Download complete" -ForegroundColor Green
    Write-Host ""
    
    # Extract
    Write-Host "📂 Extracting SDK..." -ForegroundColor Cyan
    Expand-Archive -Path $zipFile -DestinationPath $sdkDir -Force
    
    # Find HCNetSDK.dll (64-bit)
    $dllPath = Get-ChildItem -Path $sdkDir -Filter "HCNetSDK.dll" -Recurse | 
               Where-Object { $_.Directory.Name -like "*x64*" -or $_.Directory.Name -like "*64*" } |
               Select-Object -First 1
    
    if ($null -eq $dllPath) {
        # Try without filtering
        $dllPath = Get-ChildItem -Path $sdkDir -Filter "HCNetSDK.dll" -Recurse | Select-Object -First 1
    }
    
    if ($null -eq $dllPath) {
        throw "HCNetSDK.dll not found in downloaded SDK"
    }
    
    # Copy DLL to bin directory
    $destDll = Join-Path $binDir "HCNetSDK.dll"
    Copy-Item -Path $dllPath.FullName -Destination $destDll -Force
    
    Write-Host "✅ SDK installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📍 DLL location: $destDll" -ForegroundColor Cyan
    Write-Host ""
    
    # Check for additional dependencies
    $dllDir = $dllPath.Directory.FullName
    $dependencies = @(
        "HCCore.dll",
        "HCNetSDKCom.dll",
        "PlayCtrl.dll",
        "SuperRender.dll",
        "AudioRender.dll"
    )
    
    Write-Host "📦 Copying additional dependencies..." -ForegroundColor Cyan
    foreach ($dep in $dependencies) {
        $depPath = Join-Path $dllDir $dep
        if (Test-Path $depPath) {
            Copy-Item -Path $depPath -Destination $binDir -Force
            Write-Host "  ✓ $dep" -ForegroundColor Gray
        }
    }
    
    Write-Host ""
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host " PTZ CONTROL READY!" -ForegroundColor Green
    Write-Host "=========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "ℹ️  You can now use PTZ controls in the app" -ForegroundColor Cyan
    Write-Host "ℹ️  Restart the app if it's already running" -ForegroundColor Cyan
    Write-Host ""
    
} catch {
    Write-Host ""
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "📥 Manual download:" -ForegroundColor Yellow
    Write-Host "   1. Go to: https://www.hikvision.com/en/support/download/sdk/" -ForegroundColor Gray
    Write-Host "   2. Download 'Device Network SDK (Windows)'" -ForegroundColor Gray
    Write-Host "   3. Extract and copy HCNetSDK.dll to:" -ForegroundColor Gray
    Write-Host "      $binDir" -ForegroundColor Gray
    Write-Host ""
    exit 1
} finally {
    # Cleanup
    if (Test-Path $tempDir) {
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

pause
