# GitHub Repository Setup Guide

Complete guide for configuring your GitHub repository to distribute portable releases while keeping source code private.

## 🎯 Goal

- ✅ Keep source code **PRIVATE** (only you can see it)
- ✅ Distribute portable releases **PUBLICLY** (anyone can download)
- ✅ Automated builds with GitHub Actions
- ✅ Professional release management

## 📋 Configuration Options

### Option 1: Private Repo + Public Distribution Site (Recommended)

This approach uses a private repository for code and a separate public site for distribution.

#### Step-by-Step Setup

##### 1. Make Repository Private

```bash
# Via GitHub Web UI:
# Settings → General → Danger Zone → Change visibility → Make private
```

##### 2. Create Public Distribution Repository

```bash
# Create a new PUBLIC repository for releases only
# Name it: RTSPVirtualCam-Downloads or RTSPVirtualCam-Releases

# Structure:
RTSPVirtualCam-Releases/
├── README.md          # Download page
├── releases/          # Release files (managed by CI/CD)
└── docs/             # User documentation only
```

##### 3. Configure Deployment Token

```bash
# 1. Create Personal Access Token (PAT)
#    GitHub → Settings → Developer settings → Personal access tokens
#    Scopes: repo (full control)

# 2. Add to private repository secrets
#    Private Repo → Settings → Secrets → Actions
#    Name: RELEASE_REPO_TOKEN
#    Value: your_token_here
```

##### 4. Update Workflow

Add to `.github/workflows/build.yml`:

```yaml
  deploy-to-public:
    name: 📤 Deploy to Public Repo
    needs: release
    runs-on: ubuntu-latest
    if: startsWith(github.ref, 'refs/tags/v')
    
    steps:
    - name: 📥 Download release artifacts
      uses: actions/download-artifact@v4
      with:
        pattern: RTSPVirtualCam-*
        path: ./artifacts
        
    - name: 📤 Push to public repository
      env:
        RELEASE_TOKEN: ${{ secrets.RELEASE_REPO_TOKEN }}
        PUBLIC_REPO: YOUR_USERNAME/RTSPVirtualCam-Releases
      run: |
        git clone https://x-access-token:${RELEASE_TOKEN}@github.com/${PUBLIC_REPO}.git
        cd RTSPVirtualCam-Releases
        
        # Create release directory
        VERSION="${GITHUB_REF#refs/tags/v}"
        mkdir -p releases/v${VERSION}
        
        # Copy files
        cp ../artifacts/*.zip releases/v${VERSION}/
        cp ../artifacts/*.sha256 releases/v${VERSION}/
        
        # Update README with download link
        cat > README.md << 'EOF'
        # RTSP VirtualCam - Downloads
        
        ## Latest Release
        
        [Download v${VERSION}](releases/v${VERSION}/RTSPVirtualCam-v${VERSION}-portable-win-x64.zip)
        
        See [all releases](https://github.com/${PUBLIC_REPO}/releases)
        EOF
        
        # Commit and push
        git config user.name "GitHub Actions"
        git config user.email "actions@github.com"
        git add .
        git commit -m "Release v${VERSION}"
        git push
```

---

### Option 2: Private Repo with Direct Download Links

Simpler approach using GitHub's release asset URLs.

#### Setup

1. **Keep repository private**
2. **Create releases normally** (workflow already configured)
3. **Share direct download links**

#### Get Download URLs

After a release is created, get the direct URL:

```bash
# View release assets
gh release view v1.0.0

# Get direct download URL (format):
# https://github.com/USERNAME/REPO/releases/download/v1.0.0/RTSPVirtualCam-v1.0.0-portable-win-x64.zip
```

#### Create Public Landing Page

Option A: **GitHub Pages** (separate repo)
```bash
# Create public repo: rtspvirtualcam.github.io
# Add index.html with download links
```

Option B: **External website**
```html
<!-- Simple download page -->
<a href="https://github.com/USERNAME/REPO/releases/download/v1.0.0/RTSPVirtualCam-v1.0.0-portable-win-x64.zip">
  Download Latest Version
</a>
```

---

### Option 3: GitHub Packages / Releases API

Use GitHub's API to manage public releases from private repo.

Not recommended for binary distributions (better for libraries/SDKs).

---

## 🚀 Recommended Setup (Option 1 Detailed)

### Complete Implementation

#### 1. Private Repository (Source Code)

**RTSPVirtualCam** (Private)
- Contains all source code
- CI/CD workflow builds releases
- Only accessible to you

#### 2. Public Repository (Distribution)

**RTSPVirtualCam-Downloads** (Public)
- Contains only releases and documentation
- Auto-updated by CI/CD
- Anyone can download

#### 3. Workflow Integration

Complete workflow file:

```yaml
# .github/workflows/build-and-deploy.yml
name: 🚀 Build and Deploy

on:
  push:
    tags: [ 'v*' ]

jobs:
  build:
    runs-on: windows-latest
    # ... (existing build steps)
    
  create-release:
    needs: build
    runs-on: ubuntu-latest
    # ... (existing release steps)
    
  deploy-public:
    needs: create-release
    runs-on: ubuntu-latest
    steps:
    - name: Checkout public repo
      uses: actions/checkout@v4
      with:
        repository: YOUR_USERNAME/RTSPVirtualCam-Downloads
        token: ${{ secrets.RELEASE_REPO_TOKEN }}
        
    - name: Download artifacts
      uses: actions/download-artifact@v4
      with:
        pattern: RTSPVirtualCam-*
        path: ./temp
        
    - name: Setup release
      run: |
        VERSION="${GITHUB_REF#refs/tags/v}"
        mkdir -p releases/v${VERSION}
        mv temp/*.zip releases/v${VERSION}/
        mv temp/*.sha256 releases/v${VERSION}/
        
    - name: Update README
      run: |
        VERSION="${GITHUB_REF#refs/tags/v}"
        cat > README.md << EOF
        # 🎥 RTSP VirtualCam
        
        Transform RTSP camera streams into virtual webcams for Windows 11.
        
        ## 📥 Download
        
        ### Latest: v${VERSION}
        
        - [RTSPVirtualCam-v${VERSION}-portable-win-x64.zip](releases/v${VERSION}/RTSPVirtualCam-v${VERSION}-portable-win-x64.zip)
        - [Checksum (SHA256)](releases/v${VERSION}/RTSPVirtualCam-v${VERSION}-portable-win-x64.zip.sha256)
        
        ## 🚀 Quick Start
        
        1. Download the ZIP file above
        2. Extract to any folder
        3. Run RTSPVirtualCam.exe
        4. Enter your RTSP URL and click "Virtualize"
        
        ## 📋 Requirements
        
        - Windows 11 (Build 22000+)
        - No installation required
        
        ## 📚 Documentation
        
        - [User Guide](docs/USER_GUIDE.md)
        - [Troubleshooting](docs/TROUBLESHOOTING.md)
        
        ## 🔒 Security
        
        Verify download integrity:
        \`\`\`powershell
        (Get-FileHash RTSPVirtualCam-v${VERSION}-portable-win-x64.zip -Algorithm SHA256).Hash -eq (Get-Content RTSPVirtualCam-v${VERSION}-portable-win-x64.zip.sha256).Split()[0]
        \`\`\`
        
        ## 🆘 Support
        
        [Report issues](https://github.com/YOUR_USERNAME/RTSPVirtualCam-Downloads/issues)
        EOF
        
    - name: Commit and push
      run: |
        git config user.name "GitHub Actions"
        git config user.email "actions@github.com"
        git add .
        git commit -m "Release v${VERSION}" || exit 0
        git push
```

## ✅ Configuration Checklist

### Initial Setup
- [ ] Create private repository (or make existing repo private)
- [ ] Create public distribution repository
- [ ] Generate Personal Access Token (PAT)
- [ ] Add PAT to private repo secrets as `RELEASE_REPO_TOKEN`
- [ ] Update workflow with public repo name
- [ ] Copy documentation to public repo

### Before Each Release
- [ ] Update CHANGELOG.md in private repo
- [ ] Create version tag: `git tag -a v1.0.0 -m "Release v1.0.0"`
- [ ] Push tag: `git push origin v1.0.0`
- [ ] Verify Actions workflow completes
- [ ] Check public repo updated
- [ ] Test download from public repo

### Security
- [ ] Never commit secrets or tokens
- [ ] Use repository secrets for sensitive data
- [ ] Verify `.gitignore` excludes sensitive files
- [ ] Enable branch protection on main
- [ ] Require pull request reviews (if team)

## 🔐 Security Best Practices

### 1. Token Management

```bash
# Use fine-grained tokens (recommended)
# Permissions needed:
# - Contents: Read and write
# - Only for specific repositories
```

### 2. Secret Protection

```yaml
# Never expose secrets in logs
- name: Deploy
  env:
    TOKEN: ${{ secrets.RELEASE_REPO_TOKEN }}
  run: |
    # ❌ DON'T: echo $TOKEN
    # ✅ DO: Use it directly without printing
    git clone https://x-access-token:${TOKEN}@github.com/...
```

### 3. Verify Workflows

```bash
# Test locally first
act -j build  # Using 'act' to test GitHub Actions locally
```

## 📊 Comparison Table

| Feature | Private Repo Only | Private + Public Repos | Public Repo |
|---------|------------------|----------------------|-------------|
| Source code privacy | ✅ Private | ✅ Private | ❌ Public |
| Easy downloads | ⚠️ Need links | ✅ Direct access | ✅ Direct access |
| Discovery | ❌ Hidden | ✅ Public downloads | ✅ Fully public |
| Maintenance | ✅ Simple | ⚠️ Two repos | ✅ Simple |
| Professional | ⚠️ Depends | ✅ Very professional | ✅ Professional |

## 🆘 Troubleshooting

### Workflow can't push to public repo

**Error**: `Permission denied`

**Solution**: 
1. Verify PAT has `repo` scope
2. Check token hasn't expired
3. Ensure token is added to secrets correctly

### Public repo not updating

**Error**: No new files appear

**Solution**:
```bash
# Check workflow logs
# Actions → Build and Deploy → deploy-public

# Verify artifact was created
# Actions → Artifacts
```

### Download links broken

**Error**: 404 Not Found

**Solution**:
1. Check file paths in README.md
2. Ensure files copied to correct directory
3. Verify git push succeeded

## 📞 Support

Questions? Check:
- [DEPLOYMENT.md](./DEPLOYMENT.md) - Deployment guide
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)
