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

### Planned for v1.1.0
- [ ] Full MFCreateVirtualCamera implementation
- [ ] Settings persistence (JSON)
- [ ] Auto-reconnect on connection loss
- [ ] System tray support
- [ ] Dark mode theme

### Planned for v1.2.0
- [ ] Multiple simultaneous cameras
- [ ] PTZ control integration
- [ ] Hardware acceleration (DXVA2)
- [ ] Windows 10 support (DirectShow fallback)
