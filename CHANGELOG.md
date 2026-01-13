# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-12

### Added
- 🎥 Initial release of RTSP VirtualCam
- 🔌 RTSP stream connection via LibVLCSharp
- 📺 Live preview in application window
- 📹 Virtual camera service (placeholder for MFCreateVirtualCamera)
- 💾 URL history (last 10 connections)
- 🎨 Modern WPF user interface
- 📊 Status bar with connection info (resolution, FPS, codec)
- 📝 Comprehensive logging with Serilog
- 🌍 Bilingual documentation (English/Spanish)
- 🔧 GitHub Actions CI/CD workflow
- 📦 Portable single-file executable

### Technical
- .NET 8 + WPF framework
- LibVLCSharp 3.8.5 for RTSP streaming
- CommunityToolkit.MVVM 8.2.2 for MVVM pattern
- Serilog for structured logging
- DirectN for Windows API interop

### Requirements
- Windows 11 Build 22000 or higher
- No installation required (portable)

---

## [2.0.0] - 2026-01-13

### Added - Multi-Camera Platform
- 📹 **Multi-Camera Support** - Connect up to 16 cameras simultaneously with independent controls
- 🎯 **Advanced PTZ Management** - Presets, tours, and synchronized movements across cameras
- ⏺️ **Recording Service** - Stream recording with MP4/MKV/AVI/TS formats, scheduled recording, and auto-cleanup
- 📸 **Snapshot Service** - Manual and automatic snapshots with JPEG/PNG/BMP support
- 📡 **RTMP Streaming** - Stream to YouTube, Twitch, Facebook Live, or any RTMP server
- 🔍 **Motion Detection** - Frame differencing with configurable zones, sensitivity, and alerts
- ⚡ **Hardware Acceleration** - DXVA2, D3D11VA, CUDA, and Intel Quick Sync support
- ☁️ **Cloud Sync** - Sync camera profiles and settings across devices with encryption
- 📱 **REST API Server** - Control cameras from mobile apps with full authentication
- 📊 **Analytics Dashboard** - Motion event history, hourly/daily distribution, statistics

### New Services
- `IMultiCameraService` / `MultiCameraService` - Core multi-camera management
- `IAdvancedPtzService` / `AdvancedPtzService` - PTZ presets, tours, sync groups
- `IRecordingService` / `RecordingService` - Recording and snapshots
- `IRtmpStreamingService` / `RtmpStreamingService` - RTMP streaming
- `IMotionDetectionService` / `MotionDetectionService` - Motion detection
- `ICloudSyncService` / `CloudSyncService` - Cloud configuration sync
- `IApiServerService` / `ApiServerService` - REST API server
- `IHardwareAccelerationService` / `HardwareAccelerationService` - GPU acceleration

### New Models
- `CameraInstance` - Single camera with all settings and state
- `PtzTour`, `PtzTourWaypoint` - PTZ patrol tour configuration
- `AdvancedPtzPreset` - Extended preset with metadata and thumbnails
- `PtzSyncGroup` - Synchronized PTZ across multiple cameras
- `RecordingSettings`, `ScheduledRecording` - Recording configuration
- `StreamingSettings` - RTMP streaming configuration
- `MotionDetectionSettings`, `DetectionZone`, `MotionEvent` - Motion detection
- `CloudSyncSettings`, `SyncData` - Cloud sync configuration
- `ApplicationSettings` - App-wide settings with hardware acceleration

### Technical
- Updated project to .NET 8.0 with version 2.0.0
- Added System.Drawing.Common for image processing
- Added AllowUnsafeBlocks for optimized frame processing
- Full backward compatibility with v1.0 single-camera mode

---

## [Unreleased]

### Planned for v3.0.0 - Enterprise & AI Features
- [ ] AI-powered camera auto-discovery and configuration
- [ ] Multi-platform support (macOS, Linux)
- [ ] Enterprise management console for bulk camera deployment
- [ ] Advanced security features with encryption and authentication
- [ ] Web-based interface for remote management
- [ ] Advanced video processing with AI enhancement and filters
- [ ] IoT device integration for smart home/security systems
- [ ] Scalable architecture supporting hundreds of cameras
- [ ] Professional broadcasting features with NDI support
- [ ] Audio streaming support for synchronized audio-video
