# Windows 10 Virtual Camera Setup

This document explains how to enable virtual camera support on Windows 10 using **UnityCapture**.

## Why is this needed?

| Windows Version | Virtual Camera Method | Needs Setup |
|-----------------|----------------------|-------------|
| **Windows 11** | Native `MFCreateVirtualCamera` API | ❌ No |
| **Windows 10** | UnityCapture DirectShow filter | ✅ Yes (one-time) |

Windows 10 does not have the native virtual camera API, so we use **UnityCapture** - an open-source DirectShow virtual camera driver that works with Zoom, Teams, OBS, etc.

---

## Option 1: Automatic Setup (In-App)

1. Open **RTSP VirtualCam**
2. In the "WINDOWS 10 DRIVER" section (bottom left), click **Install**
3. Accept the Administrator prompt
4. Wait for "✅ Driver installed" message

---

## Option 2: Script Setup

### Install
Run `install-virtualcam.bat` **as Administrator**:
```cmd
.\scripts\install-virtualcam.bat
```
This script will download the driver from GitHub and register it.

### Uninstall
Run `uninstall-virtualcam.bat` **as Administrator**:
```cmd
.\scripts\uninstall-virtualcam.bat
```

---

## Troubleshooting

### "Download failed"
- Check your internet connection
- Try manual installation steps below

### Manual Installation
1. Download the driver zip: [UnityCapture Master](https://github.com/schellingb/UnityCapture/archive/refs/heads/master.zip)
2. Extract the zip file
3. Go to `Install` folder inside the zip
4. Copy `UnityCaptureFilter64.dll` to `scripts/softcam/` folder in the application directory
5. Run `install-virtualcam.bat` again

### Camera name
The camera will appear as **"Unity Video Capture"** in your video applications.

### 32-bit vs 64-bit
- The installer prioritizes the 64-bit driver (standard for modern apps)
- It also registers the 32-bit driver if available

---

## Security Notes

- ✅ UnityCapture is open-source (MIT license)
- ✅ Drivers are downloaded directly from the official GitHub repository
- ✅ Can be completely removed with `uninstall-virtualcam.bat`
