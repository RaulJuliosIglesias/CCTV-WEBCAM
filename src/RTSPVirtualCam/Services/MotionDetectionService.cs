using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Motion detection service using frame differencing algorithm.
/// </summary>
public class MotionDetectionService : IMotionDetectionService
{
    private readonly ConcurrentDictionary<string, DetectionContext> _contexts = new();
    private readonly List<MotionEvent> _events = new();
    private readonly string _dataPath;
    private bool _disposed;

    public event EventHandler<MotionDetectedEventArgs>? MotionDetected;
    public event EventHandler<MotionEndedEventArgs>? MotionEnded;
    public event Action<string, string>? OnLog;

    public MotionDetectionService()
    {
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTSPVirtualCam", "motion");
        Directory.CreateDirectory(_dataPath);

        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var eventsFile = Path.Combine(_dataPath, "events.json");
            if (File.Exists(eventsFile))
            {
                var json = File.ReadAllText(eventsFile);
                var events = JsonSerializer.Deserialize<List<MotionEvent>>(json);
                if (events != null)
                {
                    // Keep only last 30 days of events
                    var cutoff = DateTime.Now.AddDays(-30);
                    _events.AddRange(events.Where(e => e.Timestamp > cutoff));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load motion events");
        }
    }

    private void SaveData()
    {
        try
        {
            var eventsFile = Path.Combine(_dataPath, "events.json");
            File.WriteAllText(eventsFile, JsonSerializer.Serialize(_events, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save motion events");
        }
    }

    public Task<bool> EnableAsync(string cameraId, MotionDetectionSettings? settings = null)
    {
        settings ??= new MotionDetectionSettings { CameraId = cameraId };

        var context = _contexts.GetOrAdd(cameraId, _ => new DetectionContext());
        context.Settings = settings;
        context.IsEnabled = true;
        context.Stats = new AnalyticsStats { CameraId = cameraId };

        LogMessage(cameraId, $"Motion detection enabled (sensitivity: {settings.Sensitivity}%)");
        return Task.FromResult(true);
    }

    public Task DisableAsync(string cameraId)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
        {
            context.IsEnabled = false;

            // End any active motion
            if (context.CurrentEvent != null)
            {
                EndMotionEvent(context);
            }

            LogMessage(cameraId, "Motion detection disabled");
        }
        return Task.CompletedTask;
    }

    public bool IsEnabled(string cameraId)
    {
        return _contexts.TryGetValue(cameraId, out var context) && context.IsEnabled;
    }

    public bool IsMotionActive(string cameraId)
    {
        return _contexts.TryGetValue(cameraId, out var context) && context.CurrentEvent != null;
    }

    public MotionDetectionSettings GetSettings(string cameraId)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
            return context.Settings;
        return new MotionDetectionSettings { CameraId = cameraId };
    }

    public Task UpdateSettingsAsync(string cameraId, MotionDetectionSettings settings)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
        {
            context.Settings = settings;
            LogMessage(cameraId, "Motion detection settings updated");
        }
        return Task.CompletedTask;
    }

    // Zone management
    public Task<bool> AddZoneAsync(string cameraId, DetectionZone zone)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
        {
            context.Settings.Zones.Add(zone);
            context.Settings.UseFullFrame = false;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> RemoveZoneAsync(string cameraId, string zoneId)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
        {
            var removed = context.Settings.Zones.RemoveAll(z => z.Id == zoneId) > 0;
            if (context.Settings.Zones.Count == 0)
                context.Settings.UseFullFrame = true;
            return Task.FromResult(removed);
        }
        return Task.FromResult(false);
    }

    public Task<bool> UpdateZoneAsync(string cameraId, DetectionZone zone)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
        {
            var existing = context.Settings.Zones.FirstOrDefault(z => z.Id == zone.Id);
            if (existing != null)
            {
                context.Settings.Zones.Remove(existing);
                context.Settings.Zones.Add(zone);
                return Task.FromResult(true);
            }
        }
        return Task.FromResult(false);
    }

    public List<DetectionZone> GetZones(string cameraId)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
            return context.Settings.Zones;
        return new List<DetectionZone>();
    }

    // Frame processing - core motion detection algorithm
    public void ProcessFrame(string cameraId, byte[] frameData, int width, int height, long timestamp)
    {
        if (!_contexts.TryGetValue(cameraId, out var context) || !context.IsEnabled)
            return;

        context.FrameCount++;

        // Skip frames based on analysis rate
        if (context.FrameCount % context.Settings.AnalysisFrameRate != 0)
            return;

        // Convert to grayscale for analysis
        var grayscale = ConvertToGrayscale(frameData, width, height);

        // Apply blur for noise reduction
        if (context.Settings.NoiseReductionEnabled)
        {
            grayscale = ApplyBoxBlur(grayscale, width, height, context.Settings.BlurRadius);
        }

        // Compare with previous frame
        if (context.PreviousFrame != null)
        {
            var (motionPercent, motionMask, boundingBox) = DetectMotion(
                context.PreviousFrame, grayscale, width, height, context.Settings);

            // Check zones
            var activeZone = CheckZones(motionMask, width, height, context.Settings);

            // Motion threshold check
            bool motionDetected = motionPercent >= context.Settings.Threshold &&
                                  motionPercent >= context.Settings.MinAreaPercent &&
                                  motionPercent <= context.Settings.MaxAreaPercent;

            if (motionDetected)
            {
                if (context.CurrentEvent == null)
                {
                    // New motion event
                    StartMotionEvent(context, cameraId, motionPercent, activeZone, boundingBox);
                }
                else
                {
                    // Update ongoing event
                    context.CurrentEvent.MotionPercentage = Math.Max(context.CurrentEvent.MotionPercentage, motionPercent);
                    context.LastMotionTime = DateTime.Now;
                }
            }
            else if (context.CurrentEvent != null)
            {
                // Check if motion has ended (cooldown period)
                var sinceLastMotion = DateTime.Now - context.LastMotionTime;
                if (sinceLastMotion.TotalSeconds >= context.Settings.CooldownSeconds)
                {
                    EndMotionEvent(context);
                }
            }
        }

        context.PreviousFrame = grayscale;
    }

    private void StartMotionEvent(DetectionContext context, string cameraId, double motionPercent,
        DetectionZone? zone, (double x, double y, double w, double h) boundingBox)
    {
        var motionEvent = new MotionEvent
        {
            CameraId = cameraId,
            Timestamp = DateTime.Now,
            MotionPercentage = motionPercent,
            ZoneId = zone?.Id ?? string.Empty,
            BoundingX = boundingBox.x,
            BoundingY = boundingBox.y,
            BoundingWidth = boundingBox.w,
            BoundingHeight = boundingBox.h
        };

        context.CurrentEvent = motionEvent;
        context.LastMotionTime = DateTime.Now;
        context.Stats.TotalMotionEvents++;
        context.Stats.EventsToday++;
        context.Stats.LastMotionEvent = DateTime.Now;

        // Update hourly distribution
        var hour = DateTime.Now.Hour;
        context.Stats.HourlyActivityDistribution[hour]++;

        LogMessage(cameraId, $"Motion detected: {motionPercent:F1}%");

        MotionDetected?.Invoke(this, new MotionDetectedEventArgs
        {
            CameraId = cameraId,
            Event = motionEvent,
            MotionPercentage = motionPercent,
            ZoneId = zone?.Id
        });
    }

    private void EndMotionEvent(DetectionContext context)
    {
        if (context.CurrentEvent == null) return;

        context.CurrentEvent.EndTime = DateTime.Now;
        var duration = context.CurrentEvent.Duration;

        _events.Add(context.CurrentEvent);

        // Update stats
        context.Stats.AverageMotionDurationSeconds =
            (context.Stats.AverageMotionDurationSeconds * (context.Stats.TotalMotionEvents - 1) + duration.TotalSeconds) /
            context.Stats.TotalMotionEvents;

        LogMessage(context.CurrentEvent.CameraId, $"Motion ended: {duration.TotalSeconds:F1}s");

        MotionEnded?.Invoke(this, new MotionEndedEventArgs
        {
            CameraId = context.CurrentEvent.CameraId,
            Event = context.CurrentEvent,
            Duration = duration
        });

        context.CurrentEvent = null;
        SaveData();
    }

    private static byte[] ConvertToGrayscale(byte[] bgra, int width, int height)
    {
        var grayscale = new byte[width * height];
        for (int i = 0; i < width * height; i++)
        {
            int idx = i * 4;
            // Luminosity formula
            grayscale[i] = (byte)(bgra[idx + 2] * 0.299 + bgra[idx + 1] * 0.587 + bgra[idx] * 0.114);
        }
        return grayscale;
    }

    private static byte[] ApplyBoxBlur(byte[] grayscale, int width, int height, int radius)
    {
        var result = new byte[grayscale.Length];
        int kernelSize = radius * 2 + 1;
        float kernelArea = kernelSize * kernelSize;

        for (int y = radius; y < height - radius; y++)
        {
            for (int x = radius; x < width - radius; x++)
            {
                int sum = 0;
                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        sum += grayscale[(y + ky) * width + (x + kx)];
                    }
                }
                result[y * width + x] = (byte)(sum / kernelArea);
            }
        }
        return result;
    }

    private static (double percent, byte[] mask, (double x, double y, double w, double h) boundingBox) DetectMotion(
        byte[] prev, byte[] curr, int width, int height, MotionDetectionSettings settings)
    {
        var mask = new byte[width * height];
        int threshold = (int)(settings.Sensitivity * 2.55); // Convert 0-100 to 0-255
        int motionPixels = 0;

        int minX = width, minY = height, maxX = 0, maxY = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width + x;
                int diff = Math.Abs(curr[idx] - prev[idx]);

                if (diff > threshold)
                {
                    mask[idx] = 255;
                    motionPixels++;

                    // Update bounding box
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        double percent = (double)motionPixels / (width * height) * 100;

        // Normalize bounding box to 0-1 range
        var boundingBox = motionPixels > 0
            ? ((double)minX / width, (double)minY / height, (double)(maxX - minX) / width, (double)(maxY - minY) / height)
            : (0.0, 0.0, 0.0, 0.0);

        return (percent, mask, boundingBox);
    }

    private static DetectionZone? CheckZones(byte[] motionMask, int width, int height, MotionDetectionSettings settings)
    {
        if (settings.UseFullFrame || settings.Zones.Count == 0)
            return null;

        foreach (var zone in settings.Zones.Where(z => z.IsEnabled && z.Type == ZoneType.Include))
        {
            int zoneX = (int)(zone.X * width);
            int zoneY = (int)(zone.Y * height);
            int zoneW = (int)(zone.Width * width);
            int zoneH = (int)(zone.Height * height);

            int motionInZone = 0;
            int totalPixels = zoneW * zoneH;

            for (int y = zoneY; y < Math.Min(zoneY + zoneH, height); y++)
            {
                for (int x = zoneX; x < Math.Min(zoneX + zoneW, width); x++)
                {
                    if (motionMask[y * width + x] > 0)
                        motionInZone++;
                }
            }

            double zonePercent = (double)motionInZone / totalPixels * 100;
            if (zonePercent >= zone.Sensitivity)
            {
                return zone;
            }
        }

        return null;
    }

    // Events
    public Task<List<MotionEvent>> GetEventsAsync(string? cameraId = null, DateTime? from = null, DateTime? to = null, int limit = 100)
    {
        var query = _events.AsEnumerable();

        if (cameraId != null)
            query = query.Where(e => e.CameraId == cameraId);
        if (from.HasValue)
            query = query.Where(e => e.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.Timestamp <= to.Value);

        return Task.FromResult(query.OrderByDescending(e => e.Timestamp).Take(limit).ToList());
    }

    public Task<bool> DeleteEventAsync(string eventId)
    {
        var removed = _events.RemoveAll(e => e.Id == eventId) > 0;
        if (removed) SaveData();
        return Task.FromResult(removed);
    }

    public Task ClearEventsAsync(string? cameraId = null, DateTime? before = null)
    {
        if (cameraId != null && before.HasValue)
            _events.RemoveAll(e => e.CameraId == cameraId && e.Timestamp < before.Value);
        else if (cameraId != null)
            _events.RemoveAll(e => e.CameraId == cameraId);
        else if (before.HasValue)
            _events.RemoveAll(e => e.Timestamp < before.Value);
        else
            _events.Clear();

        SaveData();
        return Task.CompletedTask;
    }

    // Analytics
    public AnalyticsStats GetStats(string cameraId)
    {
        if (_contexts.TryGetValue(cameraId, out var context))
            return context.Stats;
        return new AnalyticsStats { CameraId = cameraId };
    }

    public Task<Dictionary<int, int>> GetHourlyDistributionAsync(string cameraId, DateTime date)
    {
        var events = _events.Where(e => e.CameraId == cameraId && e.Timestamp.Date == date.Date);
        var distribution = new Dictionary<int, int>();

        for (int i = 0; i < 24; i++)
            distribution[i] = 0;

        foreach (var evt in events)
        {
            distribution[evt.Timestamp.Hour]++;
        }

        return Task.FromResult(distribution);
    }

    public Task<Dictionary<DayOfWeek, int>> GetDailyDistributionAsync(string cameraId, DateTime weekStart)
    {
        var weekEnd = weekStart.AddDays(7);
        var events = _events.Where(e => e.CameraId == cameraId && e.Timestamp >= weekStart && e.Timestamp < weekEnd);

        var distribution = new Dictionary<DayOfWeek, int>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            distribution[day] = 0;

        foreach (var evt in events)
        {
            distribution[evt.Timestamp.DayOfWeek]++;
        }

        return Task.FromResult(distribution);
    }

    private void LogMessage(string cameraId, string message)
    {
        Log.Information($"[Motion:{cameraId}] {message}");
        OnLog?.Invoke(cameraId, message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // End all active events
        foreach (var context in _contexts.Values.Where(c => c.CurrentEvent != null))
        {
            EndMotionEvent(context);
        }

        SaveData();
    }

    private class DetectionContext
    {
        public MotionDetectionSettings Settings { get; set; } = new();
        public bool IsEnabled { get; set; }
        public byte[]? PreviousFrame { get; set; }
        public long FrameCount { get; set; }
        public MotionEvent? CurrentEvent { get; set; }
        public DateTime LastMotionTime { get; set; }
        public AnalyticsStats Stats { get; set; } = new();
    }
}
