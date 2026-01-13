using System;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for cloud configuration synchronization.
/// </summary>
public interface ICloudSyncService : IDisposable
{
    event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
    event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    event Action<string>? OnLog;

    // Sync control
    Task<bool> SyncAsync(CancellationToken ct = default);
    Task<bool> PushAsync(CancellationToken ct = default);
    Task<bool> PullAsync(CancellationToken ct = default);
    void StartAutoSync();
    void StopAutoSync();

    // Status
    SyncState GetSyncState();
    DateTime? GetLastSyncTime();
    bool IsAutoSyncEnabled();

    // Configuration
    CloudSyncSettings GetSettings();
    Task UpdateSettingsAsync(CloudSyncSettings settings);
    Task<bool> ValidateConnectionAsync(CancellationToken ct = default);

    // Data management
    Task<SyncData> ExportDataAsync();
    Task<bool> ImportDataAsync(SyncData data, bool merge = true);
    Task<bool> ResetLocalDataAsync();
}

public class SyncCompletedEventArgs : EventArgs
{
    public bool Success { get; init; }
    public SyncDirection Direction { get; init; }
    public int ItemsSynced { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

public class SyncConflictEventArgs : EventArgs
{
    public string ItemType { get; init; } = string.Empty;
    public string ItemId { get; init; } = string.Empty;
    public DateTime LocalModified { get; init; }
    public DateTime ServerModified { get; init; }
    public ConflictResolution SuggestedResolution { get; init; }
}

public enum SyncDirection
{
    Push,
    Pull,
    Both
}
