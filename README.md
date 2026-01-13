<div align="center">

# 🎥 RTSP VirtualCam

### Transform any RTSP camera into a virtual webcam

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=for-the-badge&logo=windows11)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-Proprietary-red?style=for-the-badge)](LICENSE)
[![GitHub release](https://img.shields.io/github/v/release/RaulJuliosIglesias/CCTV-WEBCAM?style=for-the-badge)](../../releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/RaulJuliosIglesias/CCTV-WEBCAM/total?style=for-the-badge)](../../releases)

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="docs/README_ES.md">🇪🇸 Español</a>
</p>

---

**A lightweight Windows desktop application that connects to IP cameras (Hikvision, Dahua, or any RTSP stream) and virtualizes them as webcams for use in Zoom, Teams, Google Meet, and other video conferencing applications.**

</div>

---

## ✨ Features

### Core Features
| Feature | Description |
|---------|-------------|
| 🔌 **Easy Connection** | Just paste your RTSP URL or use the built-in camera discovery |
| ⚡ **Low Latency** | Optimized for real-time streaming with configurable buffer |
| 🪟 **Windows 10/11 Support** | Windows 11: Native API | Windows 10: OBS Virtual Camera driver |
| 📺 **Universal** | Works with Zoom, Teams, Meet, OBS, Discord, and more |
| 🎨 **Modern UI** | Clean WPF interface with real-time status indicators |
| 💾 **Connection History** | Remembers your last 10 connections with profiles |
| 🎮 **PTZ Control** | Integrated Pan-Tilt-Zoom for supported cameras |
| 📊 **Stream Info** | Real-time display of resolution, FPS, codec, and bitrate |

### v2.0 Multi-Camera Platform Features
| Feature | Description |
|---------|-------------|
| 📹 **Multi-Camera** | Connect up to 16 cameras simultaneously with independent controls |
| 🎯 **Advanced PTZ** | Presets, tours, synchronized movements across cameras |
| ⏺️ **Recording** | Stream recording with scheduled recording and snapshots |
| 📡 **RTMP Streaming** | Stream to YouTube, Twitch, Facebook Live |
| 🔍 **Motion Detection** | Analytics with motion zones and alerts |
| ☁️ **Cloud Sync** | Sync settings across devices |
| 📱 **REST API** | Control cameras from mobile apps |
| ⚡ **Hardware Accel** | DXVA2, D3D11VA, CUDA, Intel Quick Sync support |

---

## 📸 Screenshots

<div align="center">
<table>
<tr>
<td align="center"><b>Main Interface</b></td>
<td align="center"><b>Connected State</b></td>
</tr>
<tr>
<td><img src="docs/images/screenshot-main.png" width="400" alt="Main Interface"/></td>
<td><img src="docs/images/screenshot-connected.png" width="400" alt="Connected"/></td>
</tr>
</table>
</div>

---

## 📋 Requirements

| Requirement | Details |
|-------------|---------|
| **Operating System** | Windows 10 (1809+) or Windows 11 (Build 22000+) |
| **Runtime** | .NET 8 (included in portable version) |
| **Network** | Access to RTSP camera stream |
| **Admin Rights** | Required for Windows 10 driver installation only |

---

## 🖥️ Operating System Support

### Windows 11 (Build 22000+)
✅ **Native Virtual Camera Support**
- Uses Windows 11's built-in `MFCreateVirtualCamera` API
- No additional drivers required
- Zero installation - just run and virtualize

### Windows 10 (Version 1809+)
✅ **Supported with Automatic Driver Installation**
- Application includes **one-click driver installation**
- Uses OBS Virtual Camera driver (included)
- **Install Button**: Registers the virtual camera driver automatically
- **Uninstall Button**: Removes the driver cleanly
- Admin rights required only for driver installation

> 💡 **Windows 10 Setup**: Just click "Install" in the "VIRTUAL CAMERA DRIVER" section - no manual downloads needed!

---

## 🚀 Quick Start

### Option 1: Download Portable Version (Recommended)

**📥 [Download Latest Release](../../releases/latest)**

1. Download `RTSPVirtualCam-vX.X.X-portable-win-x64.zip` from [Releases](../../releases)
2. Extract to any folder
3. Run `RTSPVirtualCam.exe`
4. No installation required!

**Verify Download Integrity:**
```powershell
# Check SHA256 checksum
(Get-FileHash RTSPVirtualCam-v1.0.0-portable-win-x64.zip -Algorithm SHA256).Hash -eq `
  (Get-Content RTSPVirtualCam-v1.0.0-portable-win-x64.zip.sha256).Split()[0]
```

### Option 2: Build from Source

> **Note:** This repository may be private. For development access, contact the maintainer.

```powershell
# Clone the repository
git clone https://github.com/RaulJuliosIglesias/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restore and build
dotnet restore
dotnet build

# Run
dotnet run --project src/RTSPVirtualCam

# Build portable release
.\scripts\create-release.ps1 -Version "1.0.0"
```

**For Contributors:** See [DEVELOPMENT.md](docs/DEVELOPMENT.md) for detailed development guide.

---

## 📖 Usage Guide

### Step 1: Enter RTSP URL

Enter your camera's RTSP URL in the format:
```
rtsp://username:password@IP:port/path
```

### Step 2: Preview

Click **▶ Preview** to verify the stream is working correctly.

### Step 3: Virtualize

**For Windows 11 Users:**
Click **📹 Virtualize** to create the virtual camera instantly.

**For Windows 10 Users:**
1. If not installed, click **🔧 Install** in the "VIRTUAL CAMERA DRIVER" section
2. Approve the admin prompt (one-time setup)
3. Click **📹 Virtualize** to create the virtual camera

### Step 4: Use in Apps

Select **"OBS Virtual Camera"** (Windows 10) or **"RTSP VirtualCam"** (Windows 11) as your camera in any video conferencing app.

---

## 🔧 RTSP URL Examples

<details>
<summary><b>Hikvision Cameras</b></summary>

```bash
# Main stream (1080p/4K)
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/101

# Sub stream (720p/lower)
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/102

# Third stream  
rtsp://admin:password@192.168.1.100:554/Streaming/Channels/103
```
</details>

<details>
<summary><b>Dahua Cameras</b></summary>

```bash
# Main stream
rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=0

# Sub stream
rtsp://admin:password@192.168.1.100:554/cam/realmonitor?channel=1&subtype=1
```
</details>

<details>
<summary><b>Generic ONVIF</b></summary>

```bash
rtsp://admin:password@192.168.1.100:554/onvif1
rtsp://admin:password@192.168.1.100:554/stream1
```
</details>

<details>
<summary><b>Test Streams (for development)</b></summary>

```bash
rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mp4
```
</details>

---

## 📁 Project Structure

```
RTSPVirtualCam/
├── 📂 .github/                    # GitHub configuration
│   ├── workflows/                 # CI/CD pipelines
│   └── ISSUE_TEMPLATE/            # Issue templates
│
├── 📂 docs/                       # Documentation
│   ├── README_ES.md               # Spanish documentation
│   ├── INSTALLATION.md            # Installation guide
│   ├── USER_GUIDE.md              # User manual
│   ├── DEVELOPMENT.md             # Developer guide
│   └── TROUBLESHOOTING.md         # Common issues
│
├── 📂 scripts/                    # Utility scripts
│   ├── build-release.ps1          # Build release package
│   └── publish-portable.ps1       # Create portable version
│
├── 📂 src/RTSPVirtualCam/         # Main application
│   ├── 📂 Models/                 # Data models
│   │   ├── ConnectionInfo.cs
│   │   ├── CameraSettings.cs
│   │   └── AppSettings.cs
│   ├── 📂 Services/               # Business logic
│   │   ├── IRtspService.cs
│   │   ├── RtspService.cs
│   │   ├── IVirtualCameraService.cs
│   │   └── VirtualCameraService.cs
│   ├── 📂 ViewModels/             # MVVM ViewModels
│   │   └── MainViewModel.cs
│   ├── 📂 Views/                  # WPF Views
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   ├── 📂 Helpers/                # Utilities
│   │   └── Converters.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   └── appsettings.json
│
├── 📄 RTSPVirtualCam.sln          # Solution file
├── 📄 README.md                   # This file
├── 📄 LICENSE                     # Proprietary License
└── 📄 .gitignore                  # Git ignore rules
```

---

## 🛠️ Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 8.0 | Runtime & Framework |
| **WPF** | - | User Interface |
| **LibVLCSharp** | 3.8.5 | RTSP streaming & decoding |
| **CommunityToolkit.MVVM** | 8.2.2 | MVVM pattern |
| **Serilog** | 4.0.0 | Logging |
| **DirectN** | 1.18.0 | Windows API interop |

---

## 📦 Building Portable Version

To create a self-contained portable executable:

```powershell
# Navigate to project
cd RTSPVirtualCam

# Build portable release
dotnet publish src/RTSPVirtualCam -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish

# The executable will be at:
# ./publish/RTSPVirtualCam.exe
```

### 📍 Executable Location

After building, find your portable `.exe` at:

```
RTSPVirtualCam/
└── publish/
    └── RTSPVirtualCam.exe  ← Portable executable (self-contained)
```

Or in debug mode:
```
RTSPVirtualCam/
└── src/RTSPVirtualCam/bin/Debug/net8.0-windows/win-x64/
    └── RTSPVirtualCam.exe
```

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [USER_GUIDE.md](docs/USER_GUIDE.md) | Complete user manual |
| [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md) | Common issues and solutions |
| [DEPLOYMENT.md](docs/DEPLOYMENT.md) | Release and deployment guide |
| [DEVELOPMENT.md](docs/DEVELOPMENT.md) | Developer setup guide |
| [GITHUB_SETUP.md](docs/GITHUB_SETUP.md) | GitHub repository configuration |
| [README_ES.md](docs/README_ES.md) | Documentación en Español |

---

## 🗺️ Roadmap

### ✅ v1.0 - Initial Release
- [x] RTSP stream connection via LibVLC
- [x] Live preview in application with real-time stats
- [x] Virtual camera service (Windows 11 native + OBS fallback)
- [x] Modern WPF UI with dark/light theme support
- [x] Connection history and camera profiles
- [x] PTZ control for Hikvision cameras
- [x] Comprehensive logging and diagnostics
- [x] Multi-brand camera support (Hikvision, Dahua, ONVIF)
- [x] Portable deployment with auto-updater
- [x] Bilingual documentation (English/Spanish)

### ✅ v2.0 - Multi-Camera Platform (Current Release)
- [x] **Multiple simultaneous camera connections** with independent controls (up to 16 cameras)
- [x] **Advanced PTZ management** with presets, tours, and synchronized movements
- [x] **Stream recording and snapshot capabilities** with scheduled recording
- [x] **Hardware acceleration (DXVA2/D3D11VA/CUDA/QSV)** for improved performance
- [x] **Network bandwidth optimization** with adaptive bitrate
- [x] **Cloud configuration sync** for settings across devices
- [x] **REST API server** for mobile companion app integration
- [x] **Advanced analytics** with motion detection and alerts
- [x] **RTMP streaming support** for platforms like YouTube/Twitch/Facebook

### 🔮 v3.0 - Enterprise & AI Features (Future)
- [ ] **AI-powered camera auto-discovery** and configuration
- [ ] **Multi-platform support** (macOS, Linux)
- [ ] **Enterprise management console** for bulk camera deployment
- [ ] **Advanced security features** with encryption and authentication
- [ ] **Web-based interface** for remote management
- [ ] **Advanced video processing** with AI enhancement and filters
- [ ] **IoT device integration** for smart home/security systems
- [ ] **Scalable architecture** supporting hundreds of cameras
- [ ] **Professional broadcasting features** with NDI support
- [ ] **Audio streaming support** for synchronized audio-video

---

---

## 🐛 Troubleshooting

<details>
<summary><b>Camera not appearing in video apps</b></summary>

**Windows 11 Users:**
1. Restart the video conferencing application
2. Check if Windows Camera privacy settings allow access
3. Verify Windows 11 Build 22000 or higher

**Windows 10 Users:**
1. Ensure driver is installed (check "VIRTUAL CAMERA DRIVER" section)
2. Restart the video conferencing application
3. Look for "OBS Virtual Camera" (not "RTSP VirtualCam")
4. If driver missing, click "Install" button in the app
</details>

<details>
<summary><b>Connection timeout</b></summary>

1. Verify camera IP and port are correct
2. Check network connectivity to camera
3. Ensure RTSP is enabled on camera
4. Try using TCP transport (`--rtsp-tcp`)
</details>

<details>
<summary><b>Black screen in preview</b></summary>

1. Check camera credentials
2. Verify stream URL format
3. Test with VLC player first
</details>

---

## 📄 License

**© 2026 Raúl Julios Iglesias - All Rights Reserved**

This is proprietary software. Only downloading the executable for personal end-user use is permitted. Copying, redistribution, modification, or commercial use of the source code is strictly prohibited. See [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgments

- [VCamNetSample](https://github.com/smourier/VCamNetSample) - Virtual camera reference implementation
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - VLC bindings for .NET
- [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) - MVVM toolkit

---

<div align="center">

**© 2026 Raúl Julios Iglesias - All Rights Reserved**

</div>
