# RTSPVirtualCam v2.0 Roadmap

## 🎯 Vision

RTSPVirtualCam v2.0 will transform from a single-camera application into a **Multi-Camera Management System**, allowing users to configure, control, and virtualize up to **4 independent cameras simultaneously**. This enables seamless switching between multiple camera sources during video conferences.

---

## 🚀 Key Features for v2.0

### 1. Multi-Camera Support (Up to 4 Cameras)

| Feature | Description |
|---------|-------------|
| **4 Independent Camera Slots** | Configure up to 4 different RTSP cameras |
| **Individual Configuration** | Each camera has its own IP, credentials, PTZ settings |
| **Separate Connection State** | Connect/disconnect cameras independently |
| **Individual Preview Windows** | View all cameras simultaneously in a grid |

### 2. Unified Multi-View Dashboard

```
┌─────────────────────────────────────────────────────────────┐
│  RTSPVirtualCam v2.0 - Multi-Camera Dashboard               │
├─────────────┬─────────────┬─────────────┬─────────────────── │
│  📷 CAM 1   │  📷 CAM 2   │  📷 CAM 3   │  📷 CAM 4         │
│  ┌───────┐  │  ┌───────┐  │  ┌───────┐  │  ┌───────┐        │
│  │Preview│  │  │Preview│  │  │Preview│  │  │Preview│        │
│  └───────┘  │  └───────┘  │  └───────┘  │  └───────┘        │
│  [▶ Start]  │  [▶ Start]  │  [■ Stop]   │  [▶ Start]        │
│  [🔵 Virtual]│ [⚪ Off]    │  [🔵 Virtual]│ [⚪ Off]          │
├─────────────┴─────────────┴─────────────┴───────────────────┤
│  Active Virtual Cameras: CAM 1, CAM 3                       │
│  Meeting Apps will see: "RTSPVirtualCam 1", "RTSPVirtualCam 3"│
└─────────────────────────────────────────────────────────────┘
```

### 3. Independent Virtualization Control

| Capability | Description |
|------------|-------------|
| **Per-Camera Virtualization** | Choose which cameras appear as virtual webcams |
| **Multiple Virtual Outputs** | Up to 4 virtual cameras available in meeting apps |
| **Named Virtual Cameras** | Each virtual camera has a distinct name |
| **Hot-Swap Support** | Start/stop virtualization without affecting other cameras |

### 4. Camera Selection in Meeting Apps

When using Zoom, Teams, Google Meet, etc.:
- **RTSPVirtualCam 1** → Camera Slot 1
- **RTSPVirtualCam 2** → Camera Slot 2
- **RTSPVirtualCam 3** → Camera Slot 3
- **RTSPVirtualCam 4** → Camera Slot 4

Users can switch between cameras mid-meeting by selecting a different virtual camera source.

---

## 📋 Detailed Feature Specifications

### 3.1 Camera Slot Architecture

```
CameraSlot
├── SlotId (1-4)
├── CameraProfile (saved configuration)
├── ConnectionState (Disconnected/Connecting/Connected)
├── VirtualizationState (Off/Active)
├── PreviewFrame (live video feed)
├── PTZController (pan/tilt/zoom/presets)
└── Settings
    ├── FlipHorizontal
    ├── FlipVertical
    ├── Brightness
    └── Contrast
```

### 3.2 User Interface Modes

#### Mode A: Grid View (Default)
- 2x2 grid showing all 4 camera previews
- Quick status indicators for each camera
- One-click connect/virtualize buttons

#### Mode B: Focus View
- One camera displayed large with full controls
- Other 3 cameras as thumbnails on the side
- Click thumbnail to switch focus

#### Mode C: Single Camera (Classic)
- Traditional single-camera interface
- For users who only need one camera
- Simplified UI similar to v1.x

### 3.3 Virtual Camera Registration

Each camera slot registers a separate virtual camera device:

| Virtual Device | Source | Windows Device Name |
|----------------|--------|---------------------|
| VCam 1 | Slot 1 | "RTSPVirtualCam 1" |
| VCam 2 | Slot 2 | "RTSPVirtualCam 2" |
| VCam 3 | Slot 3 | "RTSPVirtualCam 3" |
| VCam 4 | Slot 4 | "RTSPVirtualCam 4" |

### 3.4 PTZ Control Per Camera

Each camera maintains its own PTZ controller:
- Independent speed settings
- Separate preset banks (1-30 per camera)
- Quick-access preset buttons per camera
- PTZ control follows focused camera

---

## 🛠️ Technical Implementation Plan

### Phase 1: Core Multi-Camera Infrastructure
**Timeline: 2-3 weeks**

- [ ] Refactor `MainViewModel` to `MultiCameraViewModel`
- [ ] Create `CameraSlotViewModel` for individual camera management
- [ ] Implement `CameraSlotCollection` with ObservableCollection
- [ ] Update `CameraProfileService` for multi-camera profiles
- [ ] Design new database schema for camera slots

### Phase 2: Multi-Camera UI
**Timeline: 2-3 weeks**

- [ ] Design Grid View layout (2x2)
- [ ] Implement Focus View with thumbnail strip
- [ ] Create camera slot selection controls
- [ ] Add per-camera status indicators
- [ ] Implement drag-and-drop camera reordering

### Phase 3: Multiple Virtual Camera Outputs
**Timeline: 3-4 weeks**

- [ ] Research Windows virtual camera limitations
- [ ] Implement multiple DirectShow filter registration
- [ ] Create named virtual camera outputs
- [ ] Handle concurrent frame streaming to multiple outputs
- [ ] Optimize memory usage for multiple streams

### Phase 4: Independent Control System
**Timeline: 2-3 weeks**

- [ ] Implement per-camera connect/disconnect
- [ ] Add per-camera virtualization toggle
- [ ] Create unified control panel for batch operations
- [ ] Add keyboard shortcuts per camera slot (F1-F4)
- [ ] Implement camera state persistence

### Phase 5: Testing & Polish
**Timeline: 1-2 weeks**

- [ ] Performance testing with 4 simultaneous cameras
- [ ] Memory optimization
- [ ] UI/UX refinements
- [ ] Documentation update
- [ ] Beta testing with users

---

## 📊 Data Model Changes

### CameraSlot Entity
```csharp
public class CameraSlot
{
    public int SlotId { get; set; }  // 1-4
    public string? ProfileId { get; set; }
    public CameraProfile? Profile { get; set; }
    public bool IsConnected { get; set; }
    public bool IsVirtualized { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastConnected { get; set; }
}
```

### MultiCameraConfiguration
```csharp
public class MultiCameraConfiguration
{
    public List<CameraSlot> Slots { get; set; }
    public int ActiveSlotId { get; set; }
    public ViewMode CurrentViewMode { get; set; }
    public bool AutoConnectOnStartup { get; set; }
    public bool RememberLastConfiguration { get; set; }
}
```

---

## 🎨 UI/UX Mockups

### Grid View Layout
```
┌──────────────────────────────────────────────────────────────┐
│ [≡] RTSPVirtualCam v2.0          [Grid] [Focus] [Single] [⚙]│
├──────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────┐ ┌─────────────────────────────┐  │
│ │     CAMERA SLOT 1       │ │     CAMERA SLOT 2           │  │
│ │   ┌───────────────┐     │ │   ┌───────────────┐         │  │
│ │   │               │     │ │   │               │         │  │
│ │   │   PREVIEW     │     │ │   │   PREVIEW     │         │  │
│ │   │               │     │ │   │               │         │  │
│ │   └───────────────┘     │ │   └───────────────┘         │  │
│ │ Profile: Office Cam     │ │ Profile: Hallway Cam        │  │
│ │ Status: 🟢 Connected    │ │ Status: ⚪ Disconnected     │  │
│ │ Virtual: 🔵 Active      │ │ Virtual: ⚫ Off             │  │
│ │ [Connect] [Virtualize]  │ │ [Connect] [Virtualize]      │  │
│ └─────────────────────────┘ └─────────────────────────────┘  │
│ ┌─────────────────────────┐ ┌─────────────────────────────┐  │
│ │     CAMERA SLOT 3       │ │     CAMERA SLOT 4           │  │
│ │   ┌───────────────┐     │ │   ┌───────────────┐         │  │
│ │   │               │     │ │   │               │         │  │
│ │   │   PREVIEW     │     │ │   │   PREVIEW     │         │  │
│ │   │               │     │ │   │               │         │  │
│ │   └───────────────┘     │ │   └───────────────┘         │  │
│ │ Profile: (none)         │ │ Profile: Parking Cam        │  │
│ │ Status: ⚪ No camera    │ │ Status: 🟢 Connected        │  │
│ │ Virtual: ⚫ Off         │ │ Virtual: 🔵 Active          │  │
│ │ [+ Add Camera]          │ │ [Connect] [Virtualize]      │  │
│ └─────────────────────────┘ └─────────────────────────────┘  │
├──────────────────────────────────────────────────────────────┤
│ 📊 Active: 2/4 cameras | Virtual: 2/4 outputs | CPU: 12%    │
└──────────────────────────────────────────────────────────────┘
```

### Focus View Layout
```
┌──────────────────────────────────────────────────────────────┐
│ [≡] RTSPVirtualCam v2.0          [Grid] [Focus] [Single] [⚙]│
├──────────────────────────────────────────────────────────────┤
│ ┌──────────────────────────────────────────┐ ┌─────────────┐ │
│ │                                          │ │  SLOT 1 📷  │ │
│ │                                          │ │ ┌─────────┐ │ │
│ │                                          │ │ │ thumb 1 │ │ │
│ │           FOCUSED CAMERA                 │ │ └─────────┘ │ │
│ │              SLOT 2                      │ ├─────────────┤ │
│ │                                          │ │  SLOT 3 📷  │ │
│ │         (Large Preview)                  │ │ ┌─────────┐ │ │
│ │                                          │ │ │ thumb 3 │ │ │
│ │                                          │ │ └─────────┘ │ │
│ │                                          │ ├─────────────┤ │
│ └──────────────────────────────────────────┘ │  SLOT 4 📷  │ │
│ ┌──────────────────────────────────────────┐ │ ┌─────────┐ │ │
│ │ PTZ Controls for Slot 2                  │ │ │ thumb 4 │ │ │
│ │ [←] [↑] [Home] [↓] [→]  Speed: ███░░    │ │ └─────────┘ │ │
│ │ Presets: [1][2][3][4][5][6][7][8][9][10]│ └─────────────┘ │
│ └──────────────────────────────────────────┘                 │
├──────────────────────────────────────────────────────────────┤
│ 🎯 Focused: Slot 2 - Hallway Cam | 🔵 Virtualized           │
└──────────────────────────────────────────────────────────────┘
```

---

## ⌨️ Keyboard Shortcuts (v2.0)

| Shortcut | Action |
|----------|--------|
| `F1` | Focus Camera Slot 1 |
| `F2` | Focus Camera Slot 2 |
| `F3` | Focus Camera Slot 3 |
| `F4` | Focus Camera Slot 4 |
| `Ctrl+1` | Toggle Virtualization Slot 1 |
| `Ctrl+2` | Toggle Virtualization Slot 2 |
| `Ctrl+3` | Toggle Virtualization Slot 3 |
| `Ctrl+4` | Toggle Virtualization Slot 4 |
| `Ctrl+G` | Switch to Grid View |
| `Ctrl+F` | Switch to Focus View |
| `Ctrl+S` | Switch to Single View |
| `Ctrl+A` | Connect All Cameras |
| `Ctrl+Shift+A` | Disconnect All Cameras |

---

## 🔧 Configuration File Structure (v2.0)

```json
{
  "version": "2.0",
  "viewMode": "grid",
  "autoConnectOnStartup": true,
  "slots": [
    {
      "slotId": 1,
      "profileId": "abc-123",
      "autoConnect": true,
      "autoVirtualize": true
    },
    {
      "slotId": 2,
      "profileId": "def-456",
      "autoConnect": true,
      "autoVirtualize": false
    },
    {
      "slotId": 3,
      "profileId": null,
      "autoConnect": false,
      "autoVirtualize": false
    },
    {
      "slotId": 4,
      "profileId": "ghi-789",
      "autoConnect": true,
      "autoVirtualize": true
    }
  ],
  "profiles": [
    {
      "id": "abc-123",
      "name": "Office Camera",
      "ipAddress": "192.168.1.64",
      "port": 554,
      "username": "admin",
      "password": "encrypted:...",
      "brand": "Hikvision",
      "stream": "MainStream",
      "channel": 1
    }
  ]
}
```

---

## 📈 Performance Considerations

### Resource Usage Estimates (4 cameras @ 1080p)

| Resource | Single Camera | 4 Cameras | Optimization Target |
|----------|---------------|-----------|---------------------|
| CPU | ~5-8% | ~15-25% | < 20% |
| RAM | ~150 MB | ~400 MB | < 500 MB |
| GPU (decode) | ~10% | ~30% | Use hardware decode |
| Network | ~4 Mbps | ~16 Mbps | Depends on streams |

### Optimization Strategies

1. **Hardware Decoding**: Use GPU for H.264/H.265 decoding
2. **Lazy Loading**: Only decode visible camera previews
3. **Frame Skipping**: Reduce preview FPS when not focused
4. **Memory Pooling**: Reuse frame buffers across cameras
5. **Async Processing**: Parallel stream processing per camera

---

## 🎯 Success Metrics

| Metric | Target |
|--------|--------|
| Max simultaneous cameras | 4 |
| Preview latency | < 200ms |
| Virtualization latency | < 100ms |
| Memory per camera | < 100 MB |
| CPU per camera | < 5% |
| Startup time (4 cameras) | < 5 seconds |
| Hot-swap time | < 500ms |

---

## 📅 Estimated Timeline

| Phase | Duration | Milestone |
|-------|----------|-----------|
| Phase 1: Core Infrastructure | 2-3 weeks | Multi-camera data model complete |
| Phase 2: Multi-Camera UI | 2-3 weeks | Grid/Focus views functional |
| Phase 3: Multiple Virtual Outputs | 3-4 weeks | 4 virtual cameras working |
| Phase 4: Independent Control | 2-3 weeks | Full independent control |
| Phase 5: Testing & Polish | 1-2 weeks | v2.0 Release Candidate |
| **Total** | **10-15 weeks** | **v2.0 Release** |

---

## 🔮 Future Considerations (v2.1+)

- **6+ Camera Support** for larger deployments
- **Camera Scenes** - predefined layouts with multiple cameras
- **Picture-in-Picture** - overlay one camera on another
- **Recording** - record from any/all cameras
- **Motion Detection** - alerts when motion detected
- **Cloud Sync** - sync camera profiles across devices
- **REST API** - control cameras programmatically
- **Mobile App** - monitor cameras from phone

---

## 📝 Migration Path from v1.x

Users upgrading from v1.x will:
1. Keep all existing camera profiles
2. Profiles automatically assigned to Slot 1
3. Single View mode provides familiar v1.x experience
4. No breaking changes to saved configurations

---

## 🤝 Contributing

We welcome contributions to the v2.0 development:
- UI/UX design suggestions
- Performance optimization ideas
- Testing on different camera brands
- Documentation improvements

---

*Document Version: 1.0*
*Last Updated: January 2026*
*Target Release: Q2 2026*
