using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for stream recording and snapshot capabilities.
/// </summary>
public interface IRecordingService : IDisposable
{
    event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    event EventHandler<RecordingSegmentEventArgs>? SegmentCompleted;
    event EventHandler<SnapshotEventArgs>? SnapshotTaken;
    event Action<string, string>? OnLog;

    // Recording control
    Task<bool> StartRecordingAsync(string cameraId, RecordingSettings? settings = null, CancellationToken ct = default);
    Task StopRecordingAsync(string cameraId);
    bool IsRecording(string cameraId);
    RecordingState GetRecordingState(string cameraId);
    TimeSpan GetRecordingDuration(string cameraId);

    // Snapshots
    Task<Snapshot?> TakeSnapshotAsync(string cameraId, SnapshotSettings? settings = null);
    Task<bool> StartAutoSnapshotAsync(string cameraId, SnapshotSettings settings);
    Task StopAutoSnapshotAsync(string cameraId);

    // Scheduled recording
    Task<bool> AddScheduleAsync(ScheduledRecording schedule);
    Task<bool> RemoveScheduleAsync(string scheduleId);
    Task<bool> UpdateScheduleAsync(ScheduledRecording schedule);
    Task<List<ScheduledRecording>> GetSchedulesAsync(string? cameraId = null);
    Task<bool> EnableScheduleAsync(string scheduleId);
    Task<bool> DisableScheduleAsync(string scheduleId);

    // Recording management
    Task<List<RecordingSegment>> GetRecordingsAsync(string? cameraId = null, DateTime? from = null, DateTime? to = null);
    Task<bool> DeleteRecordingAsync(string segmentId);
    Task<long> GetStorageUsedAsync(string? cameraId = null);
    Task CleanupOldRecordingsAsync(int retentionDays);

    // Settings
    RecordingSettings GetDefaultSettings();
    void SetDefaultSettings(RecordingSettings settings);
    SnapshotSettings GetDefaultSnapshotSettings();
    void SetDefaultSnapshotSettings(SnapshotSettings settings);

    // Frame input (called by camera service)
    void PushFrame(string cameraId, byte[] frameData, int width, int height, long timestamp);
}

public class RecordingStateChangedEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public RecordingState OldState { get; init; }
    public RecordingState NewState { get; init; }
    public string? FilePath { get; init; }
    public string? ErrorMessage { get; init; }
}

public class RecordingSegmentEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public RecordingSegment Segment { get; init; } = new();
}

public class SnapshotEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public Snapshot Snapshot { get; init; } = new();
}

public enum RecordingState
{
    Idle,
    Starting,
    Recording,
    Paused,
    Stopping,
    Error
}
