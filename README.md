<div align="center">

# 🎥 RTSP VirtualCam

### Transform any RTSP camera into a virtual webcam

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows 11](https://img.shields.io/badge/Windows-11-0078D4?style=for-the-badge&logo=windows11)](https://www.microsoft.com/windows)
[![License: MIT](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen?style=for-the-badge)](CONTRIBUTING.md)

<p align="center">
  <strong>🇬🇧 English</strong> | <a href="docs/README_ES.md">🇪🇸 Español</a>
</p>

---

**A lightweight Windows desktop application that connects to Hikvision PTZ cameras (or any RTSP stream) and virtualizes them as webcams for use in Zoom, Teams, Google Meet, and other video conferencing applications.**

</div>

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🔌 **Easy Connection** | Just paste your RTSP URL and click "Virtualize" |
| ⚡ **Low Latency** | Optimized for real-time streaming with 300ms buffer |
| 🚫 **No Drivers** | Uses native Windows 11 MFCreateVirtualCamera API |
| 📺 **Universal** | Works with Zoom, Teams, Meet, OBS, Discord, and more |
| 🎨 **Modern UI** | Clean WPF interface with status indicators |
| 💾 **URL History** | Remembers your last 10 connections |

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
| **Operating System** | Windows 11 (Build 22000+) |
| **Runtime** | .NET 8 (included in portable version) |
| **Network** | Access to RTSP camera stream |

> ⚠️ **Note**: Windows 10 is not supported due to missing virtual camera API.

---

## 🚀 Quick Start

### Option 1: Download Portable Version (Recommended)

1. Download the latest release from [Releases](../../releases)
2. Extract `RTSPVirtualCam-portable.zip`
3. Run `RTSPVirtualCam.exe`
4. No installation required!

### Option 2: Build from Source

```powershell
# Clone the repository
git clone https://github.com/YOUR_USERNAME/CCTV-WEBCAM.git
cd CCTV-WEBCAM/RTSPVirtualCam

# Restore and build
dotnet restore
dotnet build

# Run
dotnet run --project src/RTSPVirtualCam
```

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

Click **📹 Virtualize** to create the virtual camera.

### Step 4: Use in Apps

Select **"RTSP VirtualCam"** as your camera in any video conferencing app.

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
├── 📄 LICENSE                     # MIT License
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

## 🗺️ Roadmap

### ✅ v1.0 - MVP (Current)
- [x] RTSP stream connection via LibVLC
- [x] Live preview in application
- [x] Virtual camera service (placeholder)
- [x] Modern WPF UI
- [x] URL history
- [x] Logging

### 🔄 v1.1 - Enhanced
- [ ] Full MFCreateVirtualCamera implementation
- [ ] Settings persistence
- [ ] Auto-reconnect on disconnect
- [ ] System tray support
- [ ] Dark mode theme

### 🔮 v1.2 - Advanced
- [ ] Multiple simultaneous cameras
- [ ] PTZ control integration
- [ ] Hardware acceleration (DXVA2)
- [ ] Windows 10 support (DirectShow)
- [ ] Installer package

---

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) first.

```bash
# Fork the repository
# Create your feature branch
git checkout -b feature/amazing-feature

# Commit your changes
git commit -m "Add amazing feature"

# Push to the branch
git push origin feature/amazing-feature

# Open a Pull Request
```

---

## 🐛 Troubleshooting

<details>
<summary><b>Camera not appearing in video apps</b></summary>

1. Restart the video conferencing application
2. Check if Windows Camera privacy settings allow access
3. Verify Windows 11 Build 22000 or higher
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

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- [VCamNetSample](https://github.com/smourier/VCamNetSample) - Virtual camera reference implementation
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) - VLC bindings for .NET
- [CommunityToolkit.MVVM](https://github.com/CommunityToolkit/dotnet) - MVVM toolkit

---

<div align="center">

**Made with ❤️ for the open source community**

⭐ Star this repository if you find it useful! ⭐

</div>
