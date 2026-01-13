using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// Motion detection and analytics configuration.
/// </summary>
public partial class MotionDetectionSettings : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private int _sensitivity = 50; // 0-100

    [ObservableProperty]
    private int _threshold = 25; // Minimum pixel change percentage

    [ObservableProperty]
    private int _minAreaPercent = 1; // Minimum motion area (% of frame)

    [ObservableProperty]
    private int _maxAreaPercent = 100; // Maximum motion area (% of frame)

    [ObservableProperty]
    private int _cooldownSeconds = 5; // Time between motion events

    [ObservableProperty]
    private int _analysisFrameRate = 5; // Analyze every N frames

    // Detection zones (regions of interest)
    public List<DetectionZone> Zones { get; set; } = new();

    [ObservableProperty]
    private bool _useFullFrame = true; // If false, only analyze defined zones

    // Noise reduction
    [ObservableProperty]
    private bool _noiseReductionEnabled = true;

    [ObservableProperty]
    private int _blurRadius = 3;

    [ObservableProperty]
    private bool _erodeEnabled = true;

    [ObservableProperty]
    private bool _dilateEnabled = true;

    // Scheduling
    [ObservableProperty]
    private bool _scheduledEnabled;

    public List<MotionDetectionSchedule> Schedules { get; set; } = new();

    // Actions on motion
    [ObservableProperty]
    private bool _recordOnMotion = true;

    [ObservableProperty]
    private int _preMotionBufferSeconds = 5;

    [ObservableProperty]
    private int _postMotionBufferSeconds = 10;

    [ObservableProperty]
    private bool _snapshotOnMotion = true;

    [ObservableProperty]
    private bool _alertOnMotion;

    [ObservableProperty]
    private MotionAlertType _alertType = MotionAlertType.Notification;

    [ObservableProperty]
    private string _webhookUrl = string.Empty;

    [ObservableProperty]
    private string _emailRecipient = string.Empty;
}

/// <summary>
/// A detection zone within a camera frame.
/// </summary>
public partial class DetectionZone : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Zone";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private ZoneType _type = ZoneType.Include;

    // Normalized coordinates (0.0 - 1.0)
    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    // For polygon zones
    public List<Point2D> Points { get; set; } = new();

    [ObservableProperty]
    private int _sensitivity = 50; // Zone-specific sensitivity override
}

public class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

public enum ZoneType
{
    Include,    // Detect motion in this zone
    Exclude     // Ignore motion in this zone
}

/// <summary>
/// Motion detection schedule for specific time periods.
/// </summary>
public partial class MotionDetectionSchedule : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Schedule";

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;

    public List<DayOfWeek> ActiveDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    };

    [ObservableProperty]
    private int _sensitivityOverride = -1; // -1 = use default

    public bool IsActiveNow()
    {
        if (!IsEnabled) return false;

        var now = DateTime.Now;
        if (!ActiveDays.Contains(now.DayOfWeek)) return false;

        var currentTime = now.TimeOfDay;
        return currentTime >= StartTime && currentTime <= EndTime;
    }
}

public enum MotionAlertType
{
    None,
    Notification,   // Desktop notification
    Sound,          // Play sound
    Webhook,        // HTTP POST
    Email,          // Send email
    All             // All of the above
}

/// <summary>
/// A motion event detected by the system.
/// </summary>
public class MotionEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CameraId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - Timestamp;
    public double MotionPercentage { get; set; }
    public int MotionPixels { get; set; }
    public string ZoneId { get; set; } = string.Empty;
    public string SnapshotPath { get; set; } = string.Empty;
    public string RecordingPath { get; set; } = string.Empty;
    public bool AlertSent { get; set; }
    
    // Bounding box of motion (normalized 0-1)
    public double BoundingX { get; set; }
    public double BoundingY { get; set; }
    public double BoundingWidth { get; set; }
    public double BoundingHeight { get; set; }
}

/// <summary>
/// Analytics statistics for a camera.
/// </summary>
public partial class AnalyticsStats : ObservableObject
{
    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private int _totalMotionEvents;

    [ObservableProperty]
    private int _eventsToday;

    [ObservableProperty]
    private int _eventsThisWeek;

    [ObservableProperty]
    private int _eventsThisMonth;

    [ObservableProperty]
    private DateTime? _lastMotionEvent;

    [ObservableProperty]
    private double _averageMotionDurationSeconds;

    [ObservableProperty]
    private double _averageMotionPercentage;

    [ObservableProperty]
    private TimeSpan _totalRecordingTime;

    [ObservableProperty]
    private long _totalStorageUsedBytes;

    [ObservableProperty]
    private int _snapshotsTaken;

    // Hourly activity distribution (0-23)
    public int[] HourlyActivityDistribution { get; set; } = new int[24];

    // Daily activity distribution (Sun=0, Sat=6)
    public int[] DailyActivityDistribution { get; set; } = new int[7];
}
