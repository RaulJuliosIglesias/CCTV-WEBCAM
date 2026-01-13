using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for RTMP streaming to platforms like YouTube, Twitch, Facebook, etc.
/// </summary>
public interface IRtmpStreamingService : IDisposable
{
    event EventHandler<StreamingStateChangedEventArgs>? StateChanged;
    event EventHandler<StreamingStatsEventArgs>? StatsUpdated;
    event Action<string, string>? OnLog;

    // Streaming control
    Task<bool> StartStreamingAsync(string cameraId, StreamingSettings settings, CancellationToken ct = default);
    Task StopStreamingAsync(string cameraId);
    bool IsStreaming(string cameraId);
    StreamingState GetStreamingState(string cameraId);
    StreamingStats GetStreamingStats(string cameraId);

    // Configuration
    Task<bool> ValidateStreamKeyAsync(StreamingSettings settings, CancellationToken ct = default);
    List<StreamingPlatform> GetSupportedPlatforms();
    StreamingSettings GetDefaultSettings(StreamingPlatform platform);

    // Frame input
    void PushFrame(string cameraId, byte[] frameData, int width, int height, long timestamp);
    void PushAudio(string cameraId, byte[] audioData, int sampleRate, int channels, long timestamp);
}

public class StreamingStateChangedEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public StreamingState OldState { get; init; }
    public StreamingState NewState { get; init; }
    public string? ErrorMessage { get; init; }
}

public class StreamingStatsEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public StreamingStats Stats { get; init; } = new();
}

public class StreamingStats
{
    public TimeSpan Duration { get; set; }
    public long BytesUploaded { get; set; }
    public double UploadSpeedMbps { get; set; }
    public int CurrentBitrateKbps { get; set; }
    public int TargetBitrateKbps { get; set; }
    public long FramesSent { get; set; }
    public long FramesDropped { get; set; }
    public double DropRate => FramesSent > 0 ? (double)FramesDropped / FramesSent * 100 : 0;
    public int ReconnectAttempts { get; set; }
    public DateTime? LastReconnect { get; set; }
    public int QueuedFrames { get; set; }
    public int Latency { get; set; }
}
