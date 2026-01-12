# Publish Portable Version Script
# Run this from the RTSPVirtualCam directory

param(
    [string]$OutputDir = "./publish",
    [string]$Configuration = "Release"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " RTSP VirtualCam - Build Portable" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean output directory
if (Test-Path $OutputDir) {
    Write-Host "Cleaning output directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $OutputDir
}

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore

# Build and publish
Write-Host "Building portable executable..." -ForegroundColor Yellow
dotnet publish src/RTSPVirtualCam `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Portable executable location:" -ForegroundColor Cyan
    Write-Host "  $OutputDir\RTSPVirtualCam.exe" -ForegroundColor White
    Write-Host ""
    
    # Get file size
    $exePath = Join-Path $OutputDir "RTSPVirtualCam.exe"
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        Write-Host "File size: $([math]::Round($size, 2)) MB" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    exit 1
}
