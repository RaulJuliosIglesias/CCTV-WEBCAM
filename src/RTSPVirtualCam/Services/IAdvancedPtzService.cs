using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for advanced PTZ management with presets, tours, and synchronized movements.
/// </summary>
public interface IAdvancedPtzService : IDisposable
{
    /// <summary>
    /// Event raised when PTZ operation completes.
    /// </summary>
    event EventHandler<PtzOperationEventArgs>? OperationCompleted;

    /// <summary>
    /// Event raised when a tour step changes.
    /// </summary>
    event EventHandler<PtzTourEventArgs>? TourStepChanged;

    /// <summary>
    /// Event raised for logging.
    /// </summary>
    event Action<string>? OnLog;

    // Basic PTZ movements
    Task<bool> MoveAsync(string cameraId, PtzDirection direction, int speed = 50, CancellationToken ct = default);
    Task<bool> StopAsync(string cameraId, CancellationToken ct = default);
    Task<bool> ZoomAsync(string cameraId, ZoomDirection direction, int speed = 50, CancellationToken ct = default);
    Task<bool> FocusAsync(string cameraId, FocusDirection direction, CancellationToken ct = default);
    Task<bool> AutoFocusAsync(string cameraId, CancellationToken ct = default);

    // Preset management
    Task<List<AdvancedPtzPreset>> GetPresetsAsync(string cameraId);
    Task<bool> GoToPresetAsync(string cameraId, int presetId, CancellationToken ct = default);
    Task<bool> SavePresetAsync(string cameraId, AdvancedPtzPreset preset, CancellationToken ct = default);
    Task<bool> DeletePresetAsync(string cameraId, int presetId, CancellationToken ct = default);
    Task<bool> RenamePresetAsync(string cameraId, int presetId, string newName, CancellationToken ct = default);

    // Tour management
    Task<List<PtzTour>> GetToursAsync(string cameraId);
    Task<bool> StartTourAsync(string cameraId, string tourId, CancellationToken ct = default);
    Task<bool> StopTourAsync(string cameraId);
    Task<bool> PauseTourAsync(string cameraId);
    Task<bool> ResumeTourAsync(string cameraId);
    Task<bool> SaveTourAsync(PtzTour tour);
    Task<bool> DeleteTourAsync(string tourId);
    bool IsTourRunning(string cameraId);

    // Synchronized PTZ
    Task<bool> CreateSyncGroupAsync(PtzSyncGroup group);
    Task<bool> DeleteSyncGroupAsync(string groupId);
    Task<List<PtzSyncGroup>> GetSyncGroupsAsync();
    Task<bool> EnableSyncGroupAsync(string groupId);
    Task<bool> DisableSyncGroupAsync(string groupId);
    Task<bool> SyncMoveAsync(string groupId, PtzDirection direction, int speed = 50, CancellationToken ct = default);

    // Absolute positioning (for supported cameras)
    Task<bool> GoToAbsolutePositionAsync(string cameraId, float pan, float tilt, float zoom, CancellationToken ct = default);
    Task<(float pan, float tilt, float zoom)?> GetCurrentPositionAsync(string cameraId, CancellationToken ct = default);

    // Pattern/Auto tracking
    Task<bool> StartAutoTrackAsync(string cameraId, CancellationToken ct = default);
    Task<bool> StopAutoTrackAsync(string cameraId, CancellationToken ct = default);
    Task<bool> StartPatternScanAsync(string cameraId, int patternId, CancellationToken ct = default);
    Task<bool> StopPatternScanAsync(string cameraId, CancellationToken ct = default);

    // Configuration
    void SetCameraCredentials(string cameraId, string host, string username, string password, int httpPort = 80);
    PtzCapabilities GetCameraCapabilities(string cameraId);
}

public class PtzOperationEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public PtzOperationType Operation { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}

public class PtzTourEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public string TourId { get; init; } = string.Empty;
    public int CurrentWaypointIndex { get; init; }
    public int TotalWaypoints { get; init; }
    public int CurrentLoop { get; init; }
    public int TotalLoops { get; init; }
    public PtzTourState State { get; init; }
}

public enum PtzOperationType
{
    Move,
    Stop,
    Zoom,
    Focus,
    GoToPreset,
    SavePreset,
    DeletePreset,
    StartTour,
    StopTour,
    AbsolutePosition,
    AutoTrack,
    PatternScan
}

public enum PtzDirection
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}

public enum ZoomDirection
{
    In,
    Out
}

public enum FocusDirection
{
    Near,
    Far
}

public enum PtzTourState
{
    Stopped,
    Running,
    Paused,
    MovingToWaypoint,
    DwellingAtWaypoint,
    Completed
}

public class PtzCapabilities
{
    public bool SupportsPTZ { get; set; }
    public bool SupportsAbsolutePosition { get; set; }
    public bool SupportsPresets { get; set; }
    public int MaxPresets { get; set; } = 255;
    public bool SupportsTours { get; set; }
    public bool SupportsPatterns { get; set; }
    public bool SupportsAutoTrack { get; set; }
    public bool SupportsFocus { get; set; }
    public bool SupportsIris { get; set; }
    public float MinPan { get; set; } = -180;
    public float MaxPan { get; set; } = 180;
    public float MinTilt { get; set; } = -90;
    public float MaxTilt { get; set; } = 90;
    public float MinZoom { get; set; } = 1;
    public float MaxZoom { get; set; } = 30;
    public int MinSpeed { get; set; } = 1;
    public int MaxSpeed { get; set; } = 100;
}
