using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// Recording configuration for a camera.
/// </summary>
public partial class RecordingSettings : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private RecordingFormat _format = RecordingFormat.MP4;

    [ObservableProperty]
    private RecordingQuality _quality = RecordingQuality.Original;

    [ObservableProperty]
    private int _maxFileSizeMB = 1024; // Split file at 1GB

    [ObservableProperty]
    private int _maxFileDurationMinutes = 60; // Split file at 60 minutes

    [ObservableProperty]
    private bool _includeAudio = true;

    [ObservableProperty]
    private bool _includeTimestamp = true;

    [ObservableProperty]
    private string _timestampFormat = "yyyy-MM-dd HH:mm:ss";

    [ObservableProperty]
    private TimestampPosition _timestampPosition = TimestampPosition.TopRight;

    [ObservableProperty]
    private string _fileNamePattern = "{camera}_{date}_{time}";

    // Storage management
    [ObservableProperty]
    private bool _autoDeleteOldFiles;

    [ObservableProperty]
    private int _retentionDays = 30;

    [ObservableProperty]
    private long _maxStorageGB = 100;

    public string GenerateFileName(string cameraName)
    {
        var now = DateTime.Now;
        return FileNamePattern
            .Replace("{camera}", SanitizeFileName(cameraName))
            .Replace("{date}", now.ToString("yyyy-MM-dd"))
            .Replace("{time}", now.ToString("HH-mm-ss"))
            .Replace("{datetime}", now.ToString("yyyyMMdd_HHmmss"));
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}

public enum RecordingQuality
{
    Original,   // Keep original stream quality
    High,       // Re-encode at high quality
    Medium,     // Re-encode at medium quality
    Low         // Re-encode at low quality for storage
}

public enum TimestampPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

/// <summary>
/// Scheduled recording configuration.
/// </summary>
public partial class ScheduledRecording : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Scheduled Recording";

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private ScheduleType _scheduleType = ScheduleType.Daily;

    [ObservableProperty]
    private TimeSpan _startTime;

    [ObservableProperty]
    private TimeSpan _endTime;

    [ObservableProperty]
    private TimeSpan _duration;

    // Days of week (for weekly schedule)
    public List<DayOfWeek> ActiveDays { get; set; } = new()
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    // Specific dates (for one-time recordings)
    public List<DateTime> SpecificDates { get; set; } = new();

    [ObservableProperty]
    private DateTime? _lastExecuted;

    [ObservableProperty]
    private DateTime? _nextExecution;

    [ObservableProperty]
    private RecordingTrigger _trigger = RecordingTrigger.Scheduled;

    public bool IsActiveNow()
    {
        if (!IsEnabled) return false;

        var now = DateTime.Now;
        var currentTime = now.TimeOfDay;

        switch (ScheduleType)
        {
            case ScheduleType.Always:
                return true;

            case ScheduleType.Daily:
                return currentTime >= StartTime && currentTime <= EndTime;

            case ScheduleType.Weekly:
                if (!ActiveDays.Contains(now.DayOfWeek)) return false;
                return currentTime >= StartTime && currentTime <= EndTime;

            case ScheduleType.OneTime:
                foreach (var date in SpecificDates)
                {
                    if (now.Date == date.Date)
                    {
                        return currentTime >= StartTime && currentTime <= EndTime;
                    }
                }
                return false;

            default:
                return false;
        }
    }
}

public enum ScheduleType
{
    Always,     // 24/7 recording
    Daily,      // Same time every day
    Weekly,     // Specific days of week
    OneTime     // Specific dates
}

public enum RecordingTrigger
{
    Scheduled,      // Time-based
    Motion,         // Motion detection triggered
    Manual,         // User-initiated
    Event           // External event/API trigger
}

/// <summary>
/// Represents a recorded file segment.
/// </summary>
public class RecordingSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CameraId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public long FileSizeBytes { get; set; }
    public RecordingTrigger Trigger { get; set; }
    public bool HasAudio { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int FrameRate { get; set; }
    public string ThumbnailPath { get; set; } = string.Empty;
}

/// <summary>
/// Snapshot configuration and result.
/// </summary>
public partial class SnapshotSettings : ObservableObject
{
    [ObservableProperty]
    private string _outputDirectory = string.Empty;

    [ObservableProperty]
    private SnapshotFormat _format = SnapshotFormat.JPEG;

    [ObservableProperty]
    private int _jpegQuality = 95;

    [ObservableProperty]
    private bool _includeTimestamp = true;

    [ObservableProperty]
    private string _fileNamePattern = "{camera}_{datetime}";

    [ObservableProperty]
    private bool _autoSnapshot;

    [ObservableProperty]
    private int _autoSnapshotIntervalSeconds = 60;
}

public enum SnapshotFormat
{
    JPEG,
    PNG,
    BMP
}

public class Snapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CameraId { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public SnapshotTrigger Trigger { get; set; }
}

public enum SnapshotTrigger
{
    Manual,
    Scheduled,
    Motion,
    Event
}
