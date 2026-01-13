using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// Represents a PTZ tour with multiple waypoints and timing.
/// </summary>
public partial class PtzTour : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "New Tour";

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private bool _isLooping = true;

    [ObservableProperty]
    private int _loopCount; // 0 = infinite

    [ObservableProperty]
    private int _currentLoopIndex;

    [ObservableProperty]
    private int _currentWaypointIndex;

    [ObservableProperty]
    private DateTime? _lastExecuted;

    public List<PtzTourWaypoint> Waypoints { get; set; } = new();

    public TimeSpan TotalDuration
    {
        get
        {
            var total = TimeSpan.Zero;
            foreach (var wp in Waypoints)
            {
                total += TimeSpan.FromSeconds(wp.DwellTimeSeconds);
                total += TimeSpan.FromSeconds(wp.TransitionTimeSeconds);
            }
            return total;
        }
    }
}

/// <summary>
/// Represents a single waypoint in a PTZ tour.
/// </summary>
public partial class PtzTourWaypoint : ObservableObject
{
    [ObservableProperty]
    private int _order;

    [ObservableProperty]
    private int _presetId;

    [ObservableProperty]
    private string _presetName = string.Empty;

    [ObservableProperty]
    private int _dwellTimeSeconds = 10; // Time to stay at this position

    [ObservableProperty]
    private int _transitionTimeSeconds = 2; // Time to move to next position

    [ObservableProperty]
    private int _moveSpeed = 50; // Speed for transition (0-100)

    [ObservableProperty]
    private PtzAction _action = PtzAction.GoToPreset;

    // Manual position (if not using preset)
    [ObservableProperty]
    private float _pan;

    [ObservableProperty]
    private float _tilt;

    [ObservableProperty]
    private float _zoom;
}

public enum PtzAction
{
    GoToPreset,
    ManualPosition,
    PatternScan,
    AutoTrack
}

/// <summary>
/// Advanced PTZ preset with additional metadata.
/// </summary>
public partial class AdvancedPtzPreset : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _thumbnailPath = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime _lastUsed;

    [ObservableProperty]
    private int _useCount;

    // Position data (for cameras that support absolute positioning)
    [ObservableProperty]
    private float? _pan;

    [ObservableProperty]
    private float? _tilt;

    [ObservableProperty]
    private float? _zoom;

    [ObservableProperty]
    private float? _focus;

    // Keyboard shortcut
    [ObservableProperty]
    private string _keyboardShortcut = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(Name) ? $"Preset {Id}" : Name;
}

/// <summary>
/// Configuration for synchronized PTZ movements across multiple cameras.
/// </summary>
public partial class PtzSyncGroup : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Sync Group";

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _masterCameraId = string.Empty;

    [ObservableProperty]
    private PtzSyncMode _syncMode = PtzSyncMode.Mirror;

    public List<string> SlaveCameraIds { get; set; } = new();

    // Offset adjustments for each slave camera
    public Dictionary<string, PtzOffset> Offsets { get; set; } = new();
}

public enum PtzSyncMode
{
    Mirror,      // Exact copy of movements
    Inverted,    // Opposite movements
    Proportional // Scaled movements based on camera FOV
}

public class PtzOffset
{
    public float PanOffset { get; set; }
    public float TiltOffset { get; set; }
    public float ZoomOffset { get; set; }
    public float SpeedMultiplier { get; set; } = 1.0f;
}
