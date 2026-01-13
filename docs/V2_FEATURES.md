# RTSP VirtualCam v2.0 - Multi-Camera Platform

## Overview

Version 2.0 transforms RTSP VirtualCam from a single-camera application into a full-featured multi-camera surveillance and streaming platform. This document describes all the new features and how to use them.

---

## 🎥 Multi-Camera Support

### Up to 16 Simultaneous Cameras
- Connect and manage multiple RTSP cameras at once
- Each camera has independent controls for connection, PTZ, recording, and streaming
- Grid layout options: 1x1, 2x2, 3x3, 4x4, horizontal, vertical

### Camera Management
```csharp
// Add a new camera slot
var camera = multiCameraService.AddCamera();

// Configure camera
camera.IpAddress = "192.168.1.100";
camera.Port = 554;
camera.Username = "admin";
camera.Password = "password";
camera.Brand = CameraBrand.Hikvision;

// Connect
await multiCameraService.ConnectCameraAsync(camera.Id);
```

---

## 🎯 Advanced PTZ Management

### Presets
- Save up to 255 presets per camera
- Named presets with thumbnails
- Keyboard shortcuts for quick access

### Tours
- Create automated patrol tours
- Multiple waypoints with customizable dwell times
- Looping and one-time tours
- Pause/resume functionality

### Synchronized PTZ
- Control multiple cameras as a group
- Mirror, inverted, or proportional movements
- Offset adjustments for each camera

```csharp
// Create a PTZ tour
var tour = new PtzTour
{
    Name = "Perimeter Patrol",
    IsLooping = true,
    Waypoints = new List<PtzTourWaypoint>
    {
        new() { PresetId = 1, DwellTimeSeconds = 10 },
        new() { PresetId = 2, DwellTimeSeconds = 15 },
        new() { PresetId = 3, DwellTimeSeconds = 10 }
    }
};

await ptzService.SaveTourAsync(tour);
await ptzService.StartTourAsync(cameraId, tour.Id);
```

---

## ⏺️ Recording & Snapshots

### Stream Recording
- Record to MP4, MKV, AVI, or TS format
- Automatic file splitting by size or duration
- Original quality or re-encoded options
- Timestamp overlay support

### Scheduled Recording
- Daily, weekly, or one-time schedules
- Motion-triggered recording
- Pre/post motion buffer
- Automatic cleanup of old recordings

### Snapshots
- Manual or automatic snapshots
- JPEG, PNG, or BMP format
- Configurable quality settings
- Motion-triggered snapshots

```csharp
// Start recording
await recordingService.StartRecordingAsync(cameraId, new RecordingSettings
{
    Format = RecordingFormat.MP4,
    IncludeTimestamp = true,
    MaxFileDurationMinutes = 60
});

// Take a snapshot
var snapshot = await recordingService.TakeSnapshotAsync(cameraId);
```

---

## 📡 RTMP Streaming

### Supported Platforms
- **YouTube Live** - rtmp://a.rtmp.youtube.com/live2
- **Twitch** - rtmp://live.twitch.tv/app
- **Facebook Live** - rtmps://live-api-s.facebook.com:443/rtmp
- **Custom RTMP** - Any RTMP server

### Stream Settings
- Video: H.264, configurable bitrate (500-8000 kbps)
- Audio: AAC, configurable bitrate (64-320 kbps)
- Resolution: 720p, 1080p, or custom
- Frame rate: 24-60 fps
- Encoding presets: ultrafast to veryslow

```csharp
// Start streaming to YouTube
await streamingService.StartStreamingAsync(cameraId, new StreamingSettings
{
    Platform = StreamingPlatform.YouTube,
    StreamKey = "your-stream-key",
    VideoBitrate = 4500,
    VideoWidth = 1920,
    VideoHeight = 1080
});
```

---

## 🔍 Motion Detection

### Detection Features
- Frame differencing algorithm
- Configurable sensitivity (0-100%)
- Detection zones (include/exclude)
- Noise reduction and filtering

### Actions on Motion
- Start recording automatically
- Take snapshot
- Desktop notification
- Webhook call
- Email alert

### Analytics
- Motion event history
- Hourly/daily activity distribution
- Average motion duration
- Event count statistics

```csharp
// Enable motion detection
await motionService.EnableAsync(cameraId, new MotionDetectionSettings
{
    Sensitivity = 50,
    RecordOnMotion = true,
    SnapshotOnMotion = true,
    CooldownSeconds = 5
});

// Add a detection zone
await motionService.AddZoneAsync(cameraId, new DetectionZone
{
    Name = "Entrance",
    X = 0.1, Y = 0.1, Width = 0.3, Height = 0.4,
    Type = ZoneType.Include
});
```

---

## ⚡ Hardware Acceleration

### Supported Technologies
- **DXVA2** - DirectX Video Acceleration 2 (Windows Vista+)
- **D3D11VA** - Direct3D 11 Video Acceleration (Windows 8+)
- **CUDA** - NVIDIA GPU acceleration
- **QSV** - Intel Quick Sync Video

### Auto-Detection
The system automatically detects your GPU and recommends the best acceleration method.

```csharp
// Check hardware capabilities
var info = hwAccelService.GetInfo();
Console.WriteLine($"GPU: {info.GpuName}");
Console.WriteLine($"Recommended: {info.RecommendedType}");

// Enable hardware acceleration
hwAccelService.Enable(HardwareAccelerationType.Auto);
```

---

## ☁️ Cloud Sync

### Sync Features
- Camera profiles
- PTZ presets and tours
- Recording settings
- Motion detection configuration
- Application settings

### Conflict Resolution
- Server wins
- Local wins
- Most recent wins
- Manual resolution

### Security
- AES-256 encryption
- API key authentication
- Device identification

```csharp
// Configure cloud sync
await cloudSyncService.UpdateSettingsAsync(new CloudSyncSettings
{
    IsEnabled = true,
    ServerUrl = "https://your-server.com",
    ApiKey = "your-api-key",
    EncryptData = true,
    AutoSync = true,
    SyncIntervalMinutes = 5
});

// Manual sync
await cloudSyncService.SyncAsync();
```

---

## 📱 REST API

### API Server
Start the built-in REST API server to control cameras from mobile apps or other clients.

```csharp
// Start API server
await apiServerService.StartAsync(8080);

// Generate auth token
var token = apiServerService.GenerateAuthToken();
```

### Available Endpoints

#### Status
```
GET /api/status
```

#### Cameras
```
GET    /api/cameras              # List all cameras
GET    /api/cameras/{id}         # Get camera details
POST   /api/cameras              # Add camera
DELETE /api/cameras/{id}         # Remove camera
POST   /api/cameras/{id}/connect     # Connect camera
POST   /api/cameras/{id}/disconnect  # Disconnect camera
POST   /api/cameras/{id}/virtualize  # Start virtual camera
POST   /api/cameras/{id}/snapshot    # Take snapshot
GET    /api/cameras/{id}/frame       # Get current frame (base64)
```

#### PTZ
```
POST /api/ptz/{cameraId}/up
POST /api/ptz/{cameraId}/down
POST /api/ptz/{cameraId}/left
POST /api/ptz/{cameraId}/right
POST /api/ptz/{cameraId}/stop
POST /api/ptz/{cameraId}/zoom-in
POST /api/ptz/{cameraId}/zoom-out
POST /api/ptz/{cameraId}/preset     # Body: { "presetId": 1 }
GET  /api/ptz/{cameraId}/presets
GET  /api/ptz/{cameraId}/tours
```

#### Recording
```
POST /api/recording/{cameraId}/start
POST /api/recording/{cameraId}/stop
POST /api/recording/{cameraId}/snapshot
GET  /api/recording/{cameraId}/status
```

#### Streaming
```
POST /api/streaming/{cameraId}/start   # Body: { "platform": "YouTube", "streamKey": "..." }
POST /api/streaming/{cameraId}/stop
GET  /api/streaming/{cameraId}/status
```

### Authentication
Include the auth token in the request header:
```
X-API-Token: your-auth-token
```

---

## Architecture

### New Services (v2.0)

| Service | Interface | Description |
|---------|-----------|-------------|
| MultiCameraService | IMultiCameraService | Manages multiple camera connections |
| AdvancedPtzService | IAdvancedPtzService | PTZ presets, tours, and sync |
| RecordingService | IRecordingService | Recording and snapshots |
| RtmpStreamingService | IRtmpStreamingService | RTMP streaming |
| MotionDetectionService | IMotionDetectionService | Motion detection |
| CloudSyncService | ICloudSyncService | Cloud configuration sync |
| ApiServerService | IApiServerService | REST API server |
| HardwareAccelerationService | IHardwareAccelerationService | GPU acceleration |

### New Models (v2.0)

| Model | Description |
|-------|-------------|
| CameraInstance | Single camera with all settings |
| PtzTour | PTZ patrol tour configuration |
| AdvancedPtzPreset | Extended preset with metadata |
| PtzSyncGroup | Synchronized PTZ group |
| RecordingSettings | Recording configuration |
| ScheduledRecording | Scheduled recording config |
| StreamingSettings | RTMP streaming config |
| MotionDetectionSettings | Motion detection config |
| DetectionZone | Motion detection zone |
| MotionEvent | Detected motion event |
| CloudSyncSettings | Cloud sync configuration |
| ApplicationSettings | App-wide settings |

---

## Upgrading from v1.0

v2.0 is fully backward compatible with v1.0. Your existing camera profiles and settings will continue to work. The single-camera interface is still available via `MainViewModel`.

To use multi-camera features, switch to `MultiCameraViewModel`:

```csharp
// In your view
var vm = serviceProvider.GetRequiredService<MultiCameraViewModel>();
```

---

## Requirements

### Minimum
- Windows 10 version 1809 or later
- .NET 8.0 Runtime
- 4 GB RAM
- DirectX 11 compatible GPU

### Recommended for Multi-Camera
- Windows 11
- 16 GB RAM
- NVIDIA GTX 1060 / AMD RX 580 or better
- Gigabit Ethernet
- SSD for recording

### For RTMP Streaming
- FFmpeg (automatically detected if in PATH)
- Upload bandwidth: 10+ Mbps recommended

---

## Known Limitations

1. **Audio**: Audio passthrough is not yet implemented (planned for v3.0)
2. **ONVIF**: Limited ONVIF support - primarily tested with Hikvision/Dahua
3. **Mobile App**: REST API is ready, but mobile app is not included
4. **Cloud Sync**: Requires your own server endpoint

---

© 2026 Raúl Julios Iglesias - All Rights Reserved
