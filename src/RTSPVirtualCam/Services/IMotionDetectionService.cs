using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for motion detection and analytics.
/// </summary>
public interface IMotionDetectionService : IDisposable
{
    event EventHandler<MotionDetectedEventArgs>? MotionDetected;
    event EventHandler<MotionEndedEventArgs>? MotionEnded;
    event Action<string, string>? OnLog;

    // Control
    Task<bool> EnableAsync(string cameraId, MotionDetectionSettings? settings = null);
    Task DisableAsync(string cameraId);
    bool IsEnabled(string cameraId);
    bool IsMotionActive(string cameraId);

    // Configuration
    MotionDetectionSettings GetSettings(string cameraId);
    Task UpdateSettingsAsync(string cameraId, MotionDetectionSettings settings);

    // Zones
    Task<bool> AddZoneAsync(string cameraId, DetectionZone zone);
    Task<bool> RemoveZoneAsync(string cameraId, string zoneId);
    Task<bool> UpdateZoneAsync(string cameraId, DetectionZone zone);
    List<DetectionZone> GetZones(string cameraId);

    // Events
    Task<List<MotionEvent>> GetEventsAsync(string? cameraId = null, DateTime? from = null, DateTime? to = null, int limit = 100);
    Task<bool> DeleteEventAsync(string eventId);
    Task ClearEventsAsync(string? cameraId = null, DateTime? before = null);

    // Analytics
    AnalyticsStats GetStats(string cameraId);
    Task<Dictionary<int, int>> GetHourlyDistributionAsync(string cameraId, DateTime date);
    Task<Dictionary<DayOfWeek, int>> GetDailyDistributionAsync(string cameraId, DateTime weekStart);

    // Frame processing
    void ProcessFrame(string cameraId, byte[] frameData, int width, int height, long timestamp);
}

public class MotionDetectedEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public MotionEvent Event { get; init; } = new();
    public double MotionPercentage { get; init; }
    public string? ZoneId { get; init; }
}

public class MotionEndedEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public MotionEvent Event { get; init; } = new();
    public TimeSpan Duration { get; init; }
}
