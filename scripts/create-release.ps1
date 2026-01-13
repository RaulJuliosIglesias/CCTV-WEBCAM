# ============================================
# RTSP VirtualCam - Create Release Package
# ============================================
param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "./releases",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Write-Title { param($msg) Write-Host "`n========================================" -ForegroundColor Cyan; Write-Host " $msg" -ForegroundColor Cyan; Write-Host "========================================`n" -ForegroundColor Cyan }
function Write-Step { param($msg) Write-Host "[>>] $msg" -ForegroundColor Yellow }
function Write-Success { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green }

Write-Host ""
Write-Host "  RTSP VirtualCam Release Builder" -ForegroundColor Cyan
Write-Host ""

Write-Title "Creating Release v$Version"

# Step 1: Clean
Write-Step "Cleaning previous builds..."
$publishDir = "./publish-temp"
$packageDir = "./package-temp"
$zipName = "RTSPVirtualCam-v$Version-portable-win-x64.zip"
$zipPath = Join-Path $OutputDir $zipName

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $packageDir) { Remove-Item -Recurse -Force $packageDir }
if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }
Write-Success "Clean complete"

# Step 2: Restore
Write-Step "Restoring NuGet packages..."
dotnet restore src/RTSPVirtualCam/RTSPVirtualCam.csproj --verbosity quiet
Write-Success "Restore complete"

# Step 3: Build
Write-Step "Building $Configuration..."
dotnet build src/RTSPVirtualCam/RTSPVirtualCam.csproj -c $Configuration --no-restore --verbosity quiet
Write-Success "Build complete"

# Step 4: Publish
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

# Force cleanup of build processes and release DLL locks
Write-Step "Releasing build processes and DLL locks..."
Get-Process | Where-Object {$_.ProcessName -like "*MSBuild*" -or $_.ProcessName -like "*VBCSCompiler*"} | Stop-Process -Force -ErrorAction SilentlyContinue
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()
Start-Sleep -Seconds 2
Write-Success "Processes released"

# Step 5: Create Clean Package Structure
Write-Step "Creating clean release package structure..."

New-Item -ItemType Directory -Path $packageDir | Out-Null
New-Item -ItemType Directory -Path "$packageDir/bin" | Out-Null
New-Item -ItemType Directory -Path "$packageDir/logs" | Out-Null

# Copy ALL application files to bin/ (required for .NET runtime)
Copy-Item "$publishDir/*" "$packageDir/bin" -Recurse

# Copy documentation to root
Copy-Item "README.md" $packageDir -ErrorAction SilentlyContinue
Copy-Item "LICENSE" $packageDir -ErrorAction SilentlyContinue
Copy-Item "CHANGELOG.md" $packageDir -ErrorAction SilentlyContinue

# Copy scripts folder
if (Test-Path "scripts") {
    New-Item -ItemType Directory -Path "$packageDir/scripts" | Out-Null
    Copy-Item "scripts/install-virtualcam.bat" "$packageDir/scripts" -ErrorAction SilentlyContinue
    Copy-Item "scripts/uninstall-virtualcam.bat" "$packageDir/scripts" -ErrorAction SilentlyContinue
    Copy-Item "scripts/softcam" "$packageDir/scripts" -Recurse -ErrorAction SilentlyContinue
}

# Create launcher batch file (uses %~dp0 for portable relative paths)
$batContent = '@echo off' + "`r`n" + 'start "" "%~dp0bin\RTSPVirtualCam.exe" %*'
Set-Content -Path "$packageDir/RTSPVirtualCam.bat" -Value $batContent -Encoding ASCII

# Create QUICKSTART.txt
$quickstart = "RTSP VirtualCam v$Version`r`n"
$quickstart += "========================`r`n`r`n"
$quickstart += "HOW TO START:`r`n"
$quickstart += "  Double-click RTSPVirtualCam.bat`r`n`r`n"
$quickstart += "FOLDER STRUCTURE:`r`n"
$quickstart += "  RTSPVirtualCam.bat - Start application`r`n"
$quickstart += "  bin/               - Application files`r`n"
$quickstart += "  scripts/           - Virtual camera installation`r`n"
$quickstart += "  logs/              - Application logs`r`n`r`n"
$quickstart += "FIRST TIME SETUP:`r`n"
$quickstart += "  Run scripts/install-virtualcam.bat as Administrator`r`n"
Set-Content -Path "$packageDir/QUICKSTART.txt" -Value $quickstart -Encoding UTF8

Write-Success "Clean package structure created"

# Step 6: Create ZIP
Write-Step "Creating ZIP archive..."
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$packageDir/*" -DestinationPath $zipPath -CompressionLevel Optimal
Write-Success "ZIP created: $zipName"

# Step 7: Cleanup
Write-Step "Cleaning temporary files..."
Remove-Item -Recurse -Force $publishDir
Remove-Item -Recurse -Force $packageDir
Write-Success "Cleanup complete"

# Summary
Write-Title "Release Package Complete!"
$zipInfo = Get-Item $zipPath
$sizeMB = [math]::Round($zipInfo.Length / 1MB, 2)
Write-Host "  Package: $zipName" -ForegroundColor White
Write-Host "  Size: $sizeMB MB" -ForegroundColor White
Write-Host ""
