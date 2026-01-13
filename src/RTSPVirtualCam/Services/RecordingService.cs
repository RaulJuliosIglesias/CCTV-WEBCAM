using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using RTSPVirtualCam.Models;
using Serilog;
using Timer = System.Timers.Timer;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Service for recording streams and taking snapshots.
/// </summary>
public class RecordingService : IRecordingService
{
    private readonly ConcurrentDictionary<string, RecordingContext> _recordings = new();
    private readonly ConcurrentDictionary<string, AutoSnapshotContext> _autoSnapshots = new();
    private readonly List<ScheduledRecording> _schedules = new();
    private readonly List<RecordingSegment> _segments = new();
    private readonly Timer _scheduleTimer;
    private readonly string _dataPath;
    private RecordingSettings _defaultSettings = new();
    private SnapshotSettings _defaultSnapshotSettings = new();
    private bool _disposed;

    public event EventHandler<RecordingStateChangedEventArgs>? StateChanged;
    public event EventHandler<RecordingSegmentEventArgs>? SegmentCompleted;
    public event EventHandler<SnapshotEventArgs>? SnapshotTaken;
    public event Action<string, string>? OnLog;

    public RecordingService()
    {
        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTSPVirtualCam", "recordings");
        Directory.CreateDirectory(_dataPath);

        // Initialize default paths
        _defaultSettings.OutputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "RTSPVirtualCam");
        _defaultSnapshotSettings.OutputDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "RTSPVirtualCam");

        LoadData();

        // Schedule timer checks every minute
        _scheduleTimer = new Timer(60000);
        _scheduleTimer.Elapsed += OnScheduleTimerElapsed;
        _scheduleTimer.Start();
    }

    private void LoadData()
    {
        try
        {
            var schedulesFile = Path.Combine(_dataPath, "schedules.json");
            if (File.Exists(schedulesFile))
            {
                var json = File.ReadAllText(schedulesFile);
                var schedules = JsonSerializer.Deserialize<List<ScheduledRecording>>(json);
                if (schedules != null) _schedules.AddRange(schedules);
            }

            var segmentsFile = Path.Combine(_dataPath, "segments.json");
            if (File.Exists(segmentsFile))
            {
                var json = File.ReadAllText(segmentsFile);
                var segments = JsonSerializer.Deserialize<List<RecordingSegment>>(json);
                if (segments != null) _segments.AddRange(segments);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load recording data");
        }
    }

    private void SaveData()
    {
        try
        {
            var schedulesFile = Path.Combine(_dataPath, "schedules.json");
            File.WriteAllText(schedulesFile, JsonSerializer.Serialize(_schedules, new JsonSerializerOptions { WriteIndented = true }));

            var segmentsFile = Path.Combine(_dataPath, "segments.json");
            File.WriteAllText(segmentsFile, JsonSerializer.Serialize(_segments, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save recording data");
        }
    }

    public async Task<bool> StartRecordingAsync(string cameraId, RecordingSettings? settings = null, CancellationToken ct = default)
    {
        if (_recordings.ContainsKey(cameraId))
        {
            LogMessage(cameraId, "Recording already in progress");
            return false;
        }

        settings ??= _defaultSettings;
        var outputDir = string.IsNullOrEmpty(settings.OutputDirectory) 
            ? _defaultSettings.OutputDirectory 
            : settings.OutputDirectory;

        Directory.CreateDirectory(outputDir);

        var context = new RecordingContext
        {
            CameraId = cameraId,
            Settings = settings,
            State = RecordingState.Starting,
            StartTime = DateTime.Now
        };

        // Generate filename
        var fileName = settings.GenerateFileName(cameraId);
        var extension = settings.Format switch
        {
            RecordingFormat.MP4 => ".mp4",
            RecordingFormat.MKV => ".mkv",
            RecordingFormat.AVI => ".avi",
            RecordingFormat.TS => ".ts",
            _ => ".mp4"
        };
        context.FilePath = Path.Combine(outputDir, fileName + extension);

        // Initialize video writer (using a simple frame buffer approach)
        // In production, use FFmpeg or MediaFoundation for proper encoding
        context.FrameBuffer = new ConcurrentQueue<(byte[] data, int width, int height, long timestamp)>();
        context.WriterTask = Task.Run(() => WriteFramesAsync(context, ct), ct);

        _recordings[cameraId] = context;
        UpdateState(context, RecordingState.Recording);

        LogMessage(cameraId, $"Recording started: {context.FilePath}");
        return true;
    }

    private async Task WriteFramesAsync(RecordingContext context, CancellationToken ct)
    {
        var tempFramesDir = Path.Combine(_dataPath, "temp", context.CameraId);
        Directory.CreateDirectory(tempFramesDir);

        int frameIndex = 0;
        var frameFiles = new List<string>();

        try
        {
            while (!ct.IsCancellationRequested && context.State == RecordingState.Recording)
            {
                if (context.FrameBuffer.TryDequeue(out var frame))
                {
                    // Save frame as image (for later FFmpeg processing)
                    var framePath = Path.Combine(tempFramesDir, $"frame_{frameIndex:D8}.jpg");
                    await SaveFrameAsync(frame.data, frame.width, frame.height, framePath);
                    frameFiles.Add(framePath);
                    frameIndex++;
                    context.FrameCount++;

                    // Check file size/duration limits
                    if (ShouldSplitFile(context))
                    {
                        await FinalizeSegmentAsync(context, frameFiles, tempFramesDir);
                        frameFiles.Clear();
                        frameIndex = 0;
                    }
                }
                else
                {
                    await Task.Delay(10, ct);
                }
            }

            // Finalize remaining frames
            if (frameFiles.Count > 0)
            {
                await FinalizeSegmentAsync(context, frameFiles, tempFramesDir);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogMessage(context.CameraId, $"Recording error: {ex.Message}");
            UpdateState(context, RecordingState.Error);
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempFramesDir))
                    Directory.Delete(tempFramesDir, true);
            }
            catch { }
        }
    }

    private async Task SaveFrameAsync(byte[] bgra, int width, int height, string path)
    {
        await Task.Run(() =>
        {
            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(bgra, 0, bitmapData.Scan0, bgra.Length);
            bitmap.UnlockBits(bitmapData);

            bitmap.Save(path, ImageFormat.Jpeg);
        });
    }

    private bool ShouldSplitFile(RecordingContext context)
    {
        var duration = DateTime.Now - context.SegmentStartTime;
        if (duration.TotalMinutes >= context.Settings.MaxFileDurationMinutes)
            return true;

        // Check file size (estimate based on frame count)
        var estimatedSizeMB = context.FrameCount * 0.05; // ~50KB per frame estimate
        if (estimatedSizeMB >= context.Settings.MaxFileSizeMB)
            return true;

        return false;
    }

    private async Task FinalizeSegmentAsync(RecordingContext context, List<string> frameFiles, string tempDir)
    {
        if (frameFiles.Count == 0) return;

        var segment = new RecordingSegment
        {
            CameraId = context.CameraId,
            FilePath = context.FilePath,
            StartTime = context.SegmentStartTime,
            EndTime = DateTime.Now,
            Trigger = RecordingTrigger.Manual
        };

        // In production, use FFmpeg to encode frames to video
        // For now, we just save the metadata
        LogMessage(context.CameraId, $"Segment completed: {frameFiles.Count} frames, {segment.Duration.TotalSeconds:F1}s");

        _segments.Add(segment);
        SaveData();

        SegmentCompleted?.Invoke(this, new RecordingSegmentEventArgs
        {
            CameraId = context.CameraId,
            Segment = segment
        });

        // Reset for next segment
        context.SegmentStartTime = DateTime.Now;
        context.SegmentIndex++;
        var extension = Path.GetExtension(context.FilePath);
        context.FilePath = context.FilePath.Replace(extension, $"_{context.SegmentIndex}{extension}");
    }

    public async Task StopRecordingAsync(string cameraId)
    {
        if (_recordings.TryRemove(cameraId, out var context))
        {
            UpdateState(context, RecordingState.Stopping);

            // Wait for writer to finish
            if (context.WriterTask != null)
            {
                await context.WriterTask;
            }

            UpdateState(context, RecordingState.Idle);
            LogMessage(cameraId, "Recording stopped");
        }
    }

    public bool IsRecording(string cameraId) => _recordings.ContainsKey(cameraId);

    public RecordingState GetRecordingState(string cameraId)
    {
        if (_recordings.TryGetValue(cameraId, out var context))
            return context.State;
        return RecordingState.Idle;
    }

    public TimeSpan GetRecordingDuration(string cameraId)
    {
        if (_recordings.TryGetValue(cameraId, out var context))
            return DateTime.Now - context.StartTime;
        return TimeSpan.Zero;
    }

    public async Task<Snapshot?> TakeSnapshotAsync(string cameraId, SnapshotSettings? settings = null)
    {
        settings ??= _defaultSnapshotSettings;
        var outputDir = string.IsNullOrEmpty(settings.OutputDirectory)
            ? _defaultSnapshotSettings.OutputDirectory
            : settings.OutputDirectory;

        Directory.CreateDirectory(outputDir);

        // Get current frame from recording context or return null
        if (!_recordings.TryGetValue(cameraId, out var context))
        {
            LogMessage(cameraId, "Cannot take snapshot - no active recording context");
            return null;
        }

        // Wait for next frame
        (byte[] data, int width, int height, long timestamp) frame = default;
        int attempts = 0;
        while (attempts < 50)
        {
            if (context.LastFrame.data != null)
            {
                frame = context.LastFrame;
                break;
            }
            await Task.Delay(20);
            attempts++;
        }

        if (frame.data == null)
        {
            LogMessage(cameraId, "No frame available for snapshot");
            return null;
        }

        var fileName = settings.FileNamePattern
            .Replace("{camera}", SanitizeFileName(cameraId))
            .Replace("{datetime}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        var extension = settings.Format switch
        {
            SnapshotFormat.JPEG => ".jpg",
            SnapshotFormat.PNG => ".png",
            SnapshotFormat.BMP => ".bmp",
            _ => ".jpg"
        };

        var filePath = Path.Combine(outputDir, fileName + extension);

        try
        {
            using var bitmap = new Bitmap(frame.width, frame.height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, frame.width, frame.height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            Marshal.Copy(frame.data, 0, bitmapData.Scan0, frame.data.Length);
            bitmap.UnlockBits(bitmapData);

            var format = settings.Format switch
            {
                SnapshotFormat.PNG => ImageFormat.Png,
                SnapshotFormat.BMP => ImageFormat.Bmp,
                _ => ImageFormat.Jpeg
            };

            if (settings.Format == SnapshotFormat.JPEG)
            {
                var encoder = GetEncoder(ImageFormat.Jpeg);
                var qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)settings.JpegQuality);
                var encoderParams = new EncoderParameters(1) { Param = { [0] = qualityParam } };
                bitmap.Save(filePath, encoder, encoderParams);
            }
            else
            {
                bitmap.Save(filePath, format);
            }

            var snapshot = new Snapshot
            {
                CameraId = cameraId,
                FilePath = filePath,
                Timestamp = DateTime.Now,
                Width = frame.width,
                Height = frame.height,
                FileSizeBytes = new FileInfo(filePath).Length,
                Trigger = SnapshotTrigger.Manual
            };

            LogMessage(cameraId, $"Snapshot saved: {filePath}");

            SnapshotTaken?.Invoke(this, new SnapshotEventArgs
            {
                CameraId = cameraId,
                Snapshot = snapshot
            });

            return snapshot;
        }
        catch (Exception ex)
        {
            LogMessage(cameraId, $"Snapshot error: {ex.Message}");
            return null;
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
                return codec;
        }
        return codecs[0];
    }

    public async Task<bool> StartAutoSnapshotAsync(string cameraId, SnapshotSettings settings)
    {
        if (_autoSnapshots.ContainsKey(cameraId))
        {
            await StopAutoSnapshotAsync(cameraId);
        }

        var cts = new CancellationTokenSource();
        var context = new AutoSnapshotContext
        {
            Settings = settings,
            CancellationSource = cts
        };

        context.Task = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await TakeSnapshotAsync(cameraId, settings);
                await Task.Delay(TimeSpan.FromSeconds(settings.AutoSnapshotIntervalSeconds), cts.Token);
            }
        }, cts.Token);

        _autoSnapshots[cameraId] = context;
        LogMessage(cameraId, $"Auto-snapshot started: every {settings.AutoSnapshotIntervalSeconds}s");
        return true;
    }

    public Task StopAutoSnapshotAsync(string cameraId)
    {
        if (_autoSnapshots.TryRemove(cameraId, out var context))
        {
            context.CancellationSource.Cancel();
            LogMessage(cameraId, "Auto-snapshot stopped");
        }
        return Task.CompletedTask;
    }

    // Scheduled recording
    public Task<bool> AddScheduleAsync(ScheduledRecording schedule)
    {
        _schedules.Add(schedule);
        SaveData();
        LogMessage(schedule.CameraId, $"Schedule added: {schedule.Name}");
        return Task.FromResult(true);
    }

    public Task<bool> RemoveScheduleAsync(string scheduleId)
    {
        var removed = _schedules.RemoveAll(s => s.Id == scheduleId) > 0;
        if (removed) SaveData();
        return Task.FromResult(removed);
    }

    public Task<bool> UpdateScheduleAsync(ScheduledRecording schedule)
    {
        var existing = _schedules.FirstOrDefault(s => s.Id == schedule.Id);
        if (existing != null)
        {
            _schedules.Remove(existing);
            _schedules.Add(schedule);
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<List<ScheduledRecording>> GetSchedulesAsync(string? cameraId = null)
    {
        var schedules = cameraId != null
            ? _schedules.Where(s => s.CameraId == cameraId).ToList()
            : _schedules.ToList();
        return Task.FromResult(schedules);
    }

    public Task<bool> EnableScheduleAsync(string scheduleId)
    {
        var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (schedule != null)
        {
            schedule.IsEnabled = true;
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DisableScheduleAsync(string scheduleId)
    {
        var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
        if (schedule != null)
        {
            schedule.IsEnabled = false;
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private void OnScheduleTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        foreach (var schedule in _schedules.Where(s => s.IsEnabled))
        {
            var shouldRecord = schedule.IsActiveNow();
            var isRecording = IsRecording(schedule.CameraId);

            if (shouldRecord && !isRecording)
            {
                _ = StartRecordingAsync(schedule.CameraId);
                schedule.LastExecuted = DateTime.Now;
            }
            else if (!shouldRecord && isRecording)
            {
                _ = StopRecordingAsync(schedule.CameraId);
            }
        }
    }

    // Recording management
    public Task<List<RecordingSegment>> GetRecordingsAsync(string? cameraId = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _segments.AsEnumerable();

        if (cameraId != null)
            query = query.Where(s => s.CameraId == cameraId);
        if (from.HasValue)
            query = query.Where(s => s.StartTime >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.EndTime <= to.Value);

        return Task.FromResult(query.OrderByDescending(s => s.StartTime).ToList());
    }

    public Task<bool> DeleteRecordingAsync(string segmentId)
    {
        var segment = _segments.FirstOrDefault(s => s.Id == segmentId);
        if (segment != null)
        {
            try
            {
                if (File.Exists(segment.FilePath))
                    File.Delete(segment.FilePath);
                if (File.Exists(segment.ThumbnailPath))
                    File.Delete(segment.ThumbnailPath);

                _segments.Remove(segment);
                SaveData();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to delete recording: {segmentId}");
            }
        }
        return Task.FromResult(false);
    }

    public Task<long> GetStorageUsedAsync(string? cameraId = null)
    {
        var segments = cameraId != null
            ? _segments.Where(s => s.CameraId == cameraId)
            : _segments;

        return Task.FromResult(segments.Sum(s => s.FileSizeBytes));
    }

    public async Task CleanupOldRecordingsAsync(int retentionDays)
    {
        var cutoff = DateTime.Now.AddDays(-retentionDays);
        var toDelete = _segments.Where(s => s.EndTime < cutoff).ToList();

        foreach (var segment in toDelete)
        {
            await DeleteRecordingAsync(segment.Id);
        }

        LogMessage("System", $"Cleaned up {toDelete.Count} old recordings");
    }

    public RecordingSettings GetDefaultSettings() => _defaultSettings;
    public void SetDefaultSettings(RecordingSettings settings) => _defaultSettings = settings;
    public SnapshotSettings GetDefaultSnapshotSettings() => _defaultSnapshotSettings;
    public void SetDefaultSnapshotSettings(SnapshotSettings settings) => _defaultSnapshotSettings = settings;

    public void PushFrame(string cameraId, byte[] frameData, int width, int height, long timestamp)
    {
        if (_recordings.TryGetValue(cameraId, out var context))
        {
            context.LastFrame = (frameData, width, height, timestamp);

            if (context.State == RecordingState.Recording)
            {
                // Limit buffer size
                while (context.FrameBuffer.Count > 300)
                {
                    context.FrameBuffer.TryDequeue(out _);
                }

                context.FrameBuffer.Enqueue((frameData, width, height, timestamp));
            }
        }
    }

    private void UpdateState(RecordingContext context, RecordingState newState)
    {
        var oldState = context.State;
        context.State = newState;

        StateChanged?.Invoke(this, new RecordingStateChangedEventArgs
        {
            CameraId = context.CameraId,
            OldState = oldState,
            NewState = newState,
            FilePath = context.FilePath
        });
    }

    private void LogMessage(string cameraId, string message)
    {
        Log.Information($"[Recording:{cameraId}] {message}");
        OnLog?.Invoke(cameraId, message);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _scheduleTimer.Stop();
        _scheduleTimer.Dispose();

        foreach (var cameraId in _recordings.Keys.ToList())
        {
            StopRecordingAsync(cameraId).Wait();
        }

        foreach (var cameraId in _autoSnapshots.Keys.ToList())
        {
            StopAutoSnapshotAsync(cameraId).Wait();
        }

        SaveData();
    }

    private class RecordingContext
    {
        public string CameraId { get; set; } = string.Empty;
        public RecordingSettings Settings { get; set; } = new();
        public RecordingState State { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime SegmentStartTime { get; set; } = DateTime.Now;
        public int SegmentIndex { get; set; }
        public long FrameCount { get; set; }
        public ConcurrentQueue<(byte[] data, int width, int height, long timestamp)> FrameBuffer { get; set; } = new();
        public (byte[] data, int width, int height, long timestamp) LastFrame { get; set; }
        public Task? WriterTask { get; set; }
    }

    private class AutoSnapshotContext
    {
        public SnapshotSettings Settings { get; set; } = new();
        public CancellationTokenSource CancellationSource { get; set; } = new();
        public Task? Task { get; set; }
    }
}
