# Windows 10 Virtual Camera Setup

This document explains how to enable virtual camera support on Windows 10 using SoftCam.

## Why is this needed?

| Windows Version | Virtual Camera Method | Needs Setup |
|-----------------|----------------------|-------------|
| **Windows 11** | Native `MFCreateVirtualCamera` API | ❌ No |
| **Windows 10** | SoftCam DirectShow filter | ✅ Yes (one-time) |

Windows 10 does not have the `MFCreateVirtualCamera` API, so we use **SoftCam** - an open-source DirectShow virtual camera.

---

## Option 1: Automatic Setup (Recommended)

### Install
Run `install-virtualcam.bat` **as Administrator**:
```cmd
.\scripts\install-virtualcam.bat
```

### Uninstall
Run `uninstall-virtualcam.bat` **as Administrator**:
```cmd
.\scripts\uninstall-virtualcam.bat
```

---

## Option 2: Manual Setup

### Step 1: Download SoftCam

Download the pre-built DLL from:
- [SoftCam Releases](https://github.com/tshino/softcam/releases)

Or build from source:
```powershell
git clone https://github.com/tshino/softcam.git
cd softcam
# Build with Visual Studio or CMake
```

### Step 2: Register the DLL

**As Administrator:**
```cmd
# For 64-bit
regsvr32 "C:\Path\To\softcam.dll"

# For 32-bit (if needed)
regsvr32 "C:\Path\To\softcam_x86.dll"
```

### Step 3: Verify

Open any video app (Zoom, Teams, etc.) and look for "SoftCam" in the camera list.

---

## Uninstalling

**As Administrator:**
```cmd
regsvr32 /u "C:\Path\To\softcam.dll"
```

This completely removes the virtual camera from your system.

---

## Troubleshooting

### "DLL Registration failed"
- Run as Administrator
- Make sure Visual C++ Redistributable is installed

### Camera not appearing in apps
- Restart the application after registration
- Some apps need a full restart (not just the camera picker)

### 32-bit vs 64-bit
- Use the DLL that matches your application (most are 64-bit)
- Some older apps may need the 32-bit version

---

## Security Notes

- ✅ SoftCam is open-source (MIT license)
- ✅ No external network access
- ✅ Can be completely removed with `regsvr32 /u`
- ✅ Does not modify system files
