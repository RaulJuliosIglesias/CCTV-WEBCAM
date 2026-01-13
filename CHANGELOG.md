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

## [Unreleased]

### Planned for v2.0.0 - Multi-Camera Platform
- [ ] Multiple simultaneous camera connections with independent controls
- [ ] Advanced PTZ management with presets, tours, and synchronized movements
- [ ] Stream recording and snapshot capabilities with scheduled recording
- [ ] Audio streaming support for synchronized audio-video
- [ ] Hardware acceleration (DXVA2) for improved performance
- [ ] Network bandwidth optimization with adaptive bitrate
- [ ] Cloud configuration sync for settings across devices
- [ ] Mobile companion app for remote camera control
- [ ] Advanced analytics with motion detection and alerts
- [ ] RTMP streaming support for platforms like YouTube/Twitch

### Planned for v3.0.0 - Enterprise & AI Features
- [ ] AI-powered camera auto-discovery and configuration
- [ ] Multi-platform support (macOS, Linux)
- [ ] Enterprise management console for bulk camera deployment
- [ ] Advanced security features with encryption and authentication
- [ ] API and SDK for third-party integrations
- [ ] Web-based interface for remote management
- [ ] Advanced video processing with AI enhancement and filters
- [ ] IoT device integration for smart home/security systems
- [ ] Scalable architecture supporting hundreds of cameras
- [ ] Professional broadcasting features with NDI support
