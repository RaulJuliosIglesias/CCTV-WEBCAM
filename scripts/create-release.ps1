# ============================================
# RTSP VirtualCam - Create Release Package
# ============================================
# This script creates a portable release package
# ready for GitHub Releases
# ============================================

param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "./releases",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

# Colors
function Write-Title { param($msg) Write-Host "`n========================================" -ForegroundColor Cyan; Write-Host " $msg" -ForegroundColor Cyan; Write-Host "========================================`n" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "→ $msg" -ForegroundColor Yellow }
function Write-Success { param($msg) Write-Host "✓ $msg" -ForegroundColor Green }
function Write-Error { param($msg) Write-Host "✗ $msg" -ForegroundColor Red }

# Banner
Write-Host ""
Write-Host "  ██████╗ ████████╗███████╗██████╗ " -ForegroundColor Cyan
Write-Host "  ██╔══██╗╚══██╔══╝██╔════╝██╔══██╗" -ForegroundColor Cyan
Write-Host "  ██████╔╝   ██║   ███████╗██████╔╝" -ForegroundColor Cyan
Write-Host "  ██╔══██╗   ██║   ╚════██║██╔═══╝ " -ForegroundColor Cyan
Write-Host "  ██║  ██║   ██║   ███████║██║     " -ForegroundColor Cyan
Write-Host "  ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝     " -ForegroundColor Cyan
Write-Host "  VirtualCam Release Builder       " -ForegroundColor Gray
Write-Host ""

Write-Title "Creating Release v$Version"

# ============================================
# Step 1: Clean
# ============================================
Write-Step "Cleaning previous builds..."

$publishDir = "./publish-temp"
$packageDir = "./package-temp"
$zipName = "RTSPVirtualCam-v$Version-portable-win-x64.zip"
$zipPath = Join-Path $OutputDir $zipName

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $packageDir) { Remove-Item -Recurse -Force $packageDir }
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

Write-Success "Clean complete"

# ============================================
# Step 2: Restore
# ============================================
Write-Step "Restoring NuGet packages..."
dotnet restore src/RTSPVirtualCam/RTSPVirtualCam.csproj --verbosity quiet
Write-Success "Restore complete"

# ============================================
# Step 3: Build
# ============================================
Write-Step "Building $Configuration..."
dotnet build src/RTSPVirtualCam/RTSPVirtualCam.csproj -c $Configuration --no-restore --verbosity quiet
Write-Success "Build complete"

# ============================================
# Step 4: Publish
# ============================================
Write-Step "Publishing portable executable..."
dotnet publish src/RTSPVirtualCam/RTSPVirtualCam.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir `
    --verbosity quiet

Write-Success "Publish complete"

# ============================================
# Step 5: Create Package Structure
# ============================================
Write-Step "Creating release package structure..."

New-Item -ItemType Directory -Path $packageDir | Out-Null

# Copy main files
Copy-Item "$publishDir/*" $packageDir -Recurse

# Copy documentation
Copy-Item "README.md" $packageDir -ErrorAction SilentlyContinue
Copy-Item "LICENSE" $packageDir -ErrorAction SilentlyContinue

# Create a simple launcher readme
$launcherReadme = @"
# RTSP VirtualCam v$Version

## Quick Start
1. Run RTSPVirtualCam.exe
2. Enter your RTSP URL
3. Click "Preview" to test
4. Click "Virtualize" to create virtual camera
5. Select "RTSP VirtualCam" in your video app

## RTSP URL Examples
- Hikvision: rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101
- Dahua: rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=0

## Requirements
- Windows 11 Build 22000+

## Support
- GitHub: https://github.com/YOUR_USERNAME/CCTV-WEBCAM
- Issues: https://github.com/YOUR_USERNAME/CCTV-WEBCAM/issues
"@

$launcherReadme | Out-File -FilePath "$packageDir/QUICKSTART.txt" -Encoding UTF8

Write-Success "Package structure created"

# ============================================
# Step 6: Create ZIP
# ============================================
Write-Step "Creating ZIP archive..."

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$packageDir/*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Success "ZIP created: $zipName"

# ============================================
# Step 7: Cleanup
# ============================================
Write-Step "Cleaning temporary files..."
Remove-Item -Recurse -Force $publishDir
Remove-Item -Recurse -Force $packageDir
Write-Success "Cleanup complete"

# ============================================
# Summary
# ============================================
Write-Title "Release Package Complete!"

$zipInfo = Get-Item $zipPath
$sizeMB = [math]::Round($zipInfo.Length / 1MB, 2)

Write-Host "  📦 Package: " -NoNewline -ForegroundColor Gray
Write-Host $zipName -ForegroundColor White

Write-Host "  📁 Location: " -NoNewline -ForegroundColor Gray
Write-Host (Resolve-Path $OutputDir) -ForegroundColor White

Write-Host "  📊 Size: " -NoNewline -ForegroundColor Gray
Write-Host "$sizeMB MB" -ForegroundColor White

Write-Host ""
Write-Host "  To upload to GitHub:" -ForegroundColor Gray
Write-Host "  1. Go to GitHub → Releases → Create new release" -ForegroundColor Gray
Write-Host "  2. Tag: v$Version" -ForegroundColor Gray
Write-Host "  3. Attach: $zipName" -ForegroundColor Gray
Write-Host ""
