# Deployment Guide

This guide explains how to configure your GitHub repository to keep the source code private while allowing anyone to download the portable releases. The repository uses automated CI/CD workflows to build and release the application.

## Distribution Strategy

### Current Approach: Public Repository

The repository is currently **public** with:
- **Source code visible** for transparency and community contribution
- **Automated releases** via GitHub Actions
- **Portable executables** available for immediate download
- **Complete documentation** in both English and Spanish

### Alternative: Private Repository Strategy

If you need to keep source code private:

### Option 1: Private Repository with Public Releases (Recommended)

This is the most common approach for distributing software without exposing source code.

#### Step 1: Make Repository Private

1. Go to your repository on GitHub
2. Click **Settings** → **General**
3. Scroll to **Danger Zone**
4. Click **Change visibility** → **Make private**
5. Confirm by typing the repository name

#### Step 2: Configure Release Visibility

GitHub releases on private repositories are **automatically private** by default, but you can share release assets via direct links.

**Important**: As of GitHub's current behavior:
- Private repository releases are only visible to repository collaborators
- However, you can create a **public landing page** or **documentation site** with direct download links

#### Alternative: Use GitHub Pages for Distribution

1. Enable GitHub Pages in your repository settings
2. Create a simple landing page that links to releases
3. The landing page can be public even if the repository is private

### Option 2: Two Repository Strategy

A cleaner approach for public distribution:

#### Repository 1: Source Code (Private)
- Contains all source code
- Only accessible to you and collaborators
- CI/CD builds and tests here

#### Repository 2: Releases Only (Public)
- Public repository
- Contains only README, documentation, and releases
- Automatically updated by CI/CD from private repo

**Setup**:
```yaml
# Add this to your private repo's workflow
- name: Push to public release repo
  env:
    RELEASE_REPO_TOKEN: ${{ secrets.RELEASE_REPO_TOKEN }}
  run: |
    git clone https://x-access-token:${RELEASE_REPO_TOKEN}@github.com/YOUR_USERNAME/RTSPVirtualCam-Releases.git
    cd RTSPVirtualCam-Releases
    # Copy release files
    cp ../release-files/* ./
    git add .
    git commit -m "Release v$VERSION"
    git push
```

### Option 3: Use GitHub Packages or Releases API

Create public releases programmatically:

```yaml
- name: Create public release
  uses: actions/create-release@v1
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
  with:
    tag_name: v${{ steps.version.outputs.VERSION }}
    release_name: Release v${{ steps.version.outputs.VERSION }}
    draft: false
    prerelease: false
```

## Automated Deployment

### Current CI/CD Workflow Features

- **Multi-target Support**: Windows 10 (1809+) and Windows 11 (Build 22000+)
- **Self-contained Publishing**: Includes .NET 8 runtime and all dependencies
- **Native Dependencies**: LibVLC, DirectN, and OBS Virtual Camera drivers included
- **Automated Testing**: Basic smoke tests for virtual camera creation
- **Version Management**: Semantic versioning with automatic changelog extraction
- **Security**: SHA256 checksums for release verification

The repository includes an automated workflow (`.github/workflows/build.yml`) that:

1. **Builds on every push** to main/master branch
2. **Creates releases** when you push a version tag
3. **Packages portable version** automatically with all dependencies
4. **Generates SHA256 checksums** for security verification
5. **Extracts changelog** from CHANGELOG.md automatically
6. **Supports both Windows 11 native** and **Windows 10 OBS fallback** virtual camera

### How to Create a Release

#### Method 1: Using Git Tags (Recommended)

```bash
# Create and push a version tag
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

This will:
- ✅ Trigger the build workflow
- ✅ Compile the application
- ✅ Create portable ZIP package
- ✅ Generate SHA256 checksums
- ✅ Create GitHub Release with files attached
- ✅ Extract changelog from CHANGELOG.md

#### Method 2: Manual Trigger

1. Go to **Actions** tab in GitHub
2. Select **🚀 Build and Release** workflow
3. Click **Run workflow**
4. Enter version number (e.g., `1.2.3`) or leave as `auto`
5. Click **Run workflow**

#### Method 3: Using GitHub CLI

```bash
# Install GitHub CLI
gh release create v1.0.0 \
  --title "RTSP VirtualCam v1.0.0" \
  --notes "See CHANGELOG.md for details"
```

## Enhanced Build Features

### Portable Package Contents

Each release includes:
```
RTSPVirtualCam-v1.0.0-portable-win-x64/
├── RTSPVirtualCam.exe           # Main application
├── libvlc/                       # VLC native libraries
├── scripts/                      # Utility scripts
│   ├── install-virtualcam.bat    # Windows 10 driver install
│   └── install-unity-multicam.bat # Unity capture plugin
├── appsettings.json             # Application configuration
├── CHANGELOG.md                  # Version history
└── RTSPVirtualCam-v1.0.0.zip.sha256 # Security checksum
```

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR.MINOR.PATCH** (e.g., 1.2.3)
  - **MAJOR**: Breaking changes
  - **MINOR**: New features (backward compatible)
  - **PATCH**: Bug fixes

### Version Detection

The workflow automatically detects versions:

| Trigger | Version Format | Example |
|---------|----------------|---------|
| Tag `v1.2.3` | `1.2.3` | Release version |
| Manual input | User defined | `1.0.0-beta.1` |
| Auto (dev builds) | `0.0.0-dev.N` | `0.0.0-dev.42` |

## Changelog Management

Update `CHANGELOG.md` before releasing:

```markdown
# Changelog

## [1.0.0] - 2024-01-13

### Added
- Initial release
- RTSP stream support
- Virtual camera creation

### Fixed
- Connection stability issues

### Changed
- Improved UI responsiveness
```

The workflow automatically extracts the relevant section for the release notes.

## Security Best Practices

### 1. Verify Downloads

Provide SHA256 checksums with every release:

```powershell
# Verify integrity
(Get-FileHash RTSPVirtualCam-v1.0.0-portable-win-x64.zip -Algorithm SHA256).Hash -eq `
  (Get-Content RTSPVirtualCam-v1.0.0-portable-win-x64.zip.sha256).Split()[0]
```

### 2. Sign Releases (Optional)

For production releases, consider code signing:

```yaml
- name: Sign executable
  run: |
    signtool sign /f certificate.pfx /p ${{ secrets.CERT_PASSWORD }} /t http://timestamp.digicert.com RTSPVirtualCam.exe
```

### 3. Protect Secrets

Never commit:
- API keys
- Certificates
- Passwords

Use GitHub Secrets instead:
1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Add secrets (e.g., `CERT_PASSWORD`, `RELEASE_TOKEN`)

## Troubleshooting

### Workflow fails on publish

**Problem**: `dotnet publish` fails with missing dependencies

**Solution**:
```bash
# Restore packages locally first
dotnet restore
dotnet build
```

### Release not created

**Problem**: Tag pushed but no release appears

**Solution**: Check workflow logs in **Actions** tab
- Ensure tag format is `v*` (e.g., `v1.0.0`, not `1.0.0`)
- Verify `contents: write` permission in workflow

### Large file size

**Problem**: ZIP is too large (>100MB)

**Solution**:
1. Review dependencies in `.csproj`
2. Consider using `PublishTrimmed=true`
3. Exclude unnecessary files

## Distribution Checklist

Before releasing:

- [ ] Update `CHANGELOG.md` with new version
- [ ] Update version in README if hardcoded
- [ ] Test build locally: `.\scripts\create-release.ps1 -Version "1.0.0"`
- [ ] Verify all features work in Release build
- [ ] Update documentation if needed
- [ ] Create and push version tag
- [ ] Verify GitHub Actions completes successfully
- [ ] Test download and extraction of release
- [ ] Verify checksums match
- [ ] Announce release (if applicable)

## Quick Reference

### Local Build
```powershell
# Build release locally
.\scripts\create-release.ps1 -Version "1.0.0"
```

### Create Release
```bash
# Tag and release
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

### Verify Release
```powershell
# Check release files
gh release view v1.0.0
gh release download v1.0.0
```

## Support

For issues with deployment:
- Check [Actions logs](../../actions)
- Review [Troubleshooting Guide](./TROUBLESHOOTING.md)
- Open an [issue](../../issues)
