using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// RTMP streaming service for platforms like YouTube, Twitch, Facebook.
/// Uses FFmpeg for encoding and streaming.
/// </summary>
public class RtmpStreamingService : IRtmpStreamingService
{
    private readonly ConcurrentDictionary<string, StreamingContext> _streams = new();
    private readonly HttpClient _httpClient;
    private readonly string _ffmpegPath;
    private bool _disposed;

    public event EventHandler<StreamingStateChangedEventArgs>? StateChanged;
    public event EventHandler<StreamingStatsEventArgs>? StatsUpdated;
    public event Action<string, string>? OnLog;

    public RtmpStreamingService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Find FFmpeg
        _ffmpegPath = FindFFmpeg();
        if (string.IsNullOrEmpty(_ffmpegPath))
        {
            Log.Warning("FFmpeg not found. RTMP streaming will not work.");
        }
    }

    private string FindFFmpeg()
    {
        // Check common locations
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"),
            Path.Combine(AppContext.BaseDirectory, "ffmpeg", "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            "ffmpeg" // In PATH
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return path;
        }

        // Check PATH
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "ffmpeg",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(5000);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            if (!string.IsNullOrEmpty(output) && File.Exists(output.Split('\n')[0]))
                return output.Split('\n')[0].Trim();
        }
        catch { }

        return string.Empty;
    }

    public async Task<bool> StartStreamingAsync(string cameraId, StreamingSettings settings, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_ffmpegPath))
        {
            LogMessage(cameraId, "FFmpeg not found. Please install FFmpeg.");
            return false;
        }

        if (_streams.ContainsKey(cameraId))
        {
            LogMessage(cameraId, "Stream already in progress");
            return false;
        }

        var rtmpUrl = settings.GetFullRtmpUrl();
        if (string.IsNullOrEmpty(rtmpUrl))
        {
            LogMessage(cameraId, "Invalid RTMP URL or stream key");
            return false;
        }

        var context = new StreamingContext
        {
            CameraId = cameraId,
            Settings = settings,
            RtmpUrl = rtmpUrl,
            State = StreamingState.Starting,
            StartTime = DateTime.Now
        };

        // Create named pipe for frame input
        context.PipeName = $"rtsp_stream_{cameraId}_{Guid.NewGuid():N}";
        context.FrameQueue = new ConcurrentQueue<(byte[] data, int width, int height, long timestamp)>();
        context.CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _streams[cameraId] = context;
        UpdateState(context, StreamingState.Starting);

        // Start FFmpeg process
        context.StreamTask = Task.Run(() => RunStreamAsync(context), context.CancellationSource.Token);

        // Start stats updater
        context.StatsTask = Task.Run(() => UpdateStatsAsync(context), context.CancellationSource.Token);

        LogMessage(cameraId, $"Streaming started to {MaskUrl(rtmpUrl)}");
        return true;
    }

    private async Task RunStreamAsync(StreamingContext context)
    {
        var settings = context.Settings;
        var tempDir = Path.Combine(Path.GetTempPath(), "rtsp_stream", context.CameraId);
        Directory.CreateDirectory(tempDir);

        try
        {
            // Build FFmpeg command
            var ffmpegArgs = BuildFFmpegArgs(context, tempDir);

            context.FFmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            context.FFmpegProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    ParseFFmpegOutput(context, e.Data);
                }
            };

            context.FFmpegProcess.Start();
            context.FFmpegProcess.BeginErrorReadLine();

            UpdateState(context, StreamingState.Streaming);

            // Feed frames to FFmpeg
            while (!context.CancellationSource.Token.IsCancellationRequested &&
                   context.State == StreamingState.Streaming)
            {
                if (context.FrameQueue.TryDequeue(out var frame))
                {
                    try
                    {
                        // Write raw BGRA frame to stdin
                        await context.FFmpegProcess.StandardInput.BaseStream.WriteAsync(
                            frame.data, 0, frame.data.Length, context.CancellationSource.Token);
                        context.Stats.FramesSent++;
                    }
                    catch (Exception ex)
                    {
                        context.Stats.FramesDropped++;
                        if (context.Stats.FramesDropped % 100 == 0)
                        {
                            LogMessage(context.CameraId, $"Dropped {context.Stats.FramesDropped} frames: {ex.Message}");
                        }
                    }
                }
                else
                {
                    await Task.Delay(1, context.CancellationSource.Token);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogMessage(context.CameraId, $"Streaming error: {ex.Message}");
            UpdateState(context, StreamingState.Error);

            // Attempt reconnect
            if (settings.ReconnectAttempts > context.Stats.ReconnectAttempts)
            {
                context.Stats.ReconnectAttempts++;
                context.Stats.LastReconnect = DateTime.Now;
                UpdateState(context, StreamingState.Reconnecting);

                await Task.Delay(TimeSpan.FromSeconds(settings.ReconnectDelaySeconds));
                await RunStreamAsync(context);
            }
        }
        finally
        {
            try
            {
                context.FFmpegProcess?.Kill();
                context.FFmpegProcess?.Dispose();
            }
            catch { }

            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }

    private string BuildFFmpegArgs(StreamingContext context, string tempDir)
    {
        var settings = context.Settings;
        var preset = settings.Preset.ToString().ToLower();
        var profile = settings.Profile.ToString().ToLower();

        // Input: raw video frames from stdin
        var args = $"-f rawvideo -pix_fmt bgra -s {settings.VideoWidth}x{settings.VideoHeight} " +
                   $"-r {settings.VideoFrameRate} -i pipe:0 ";

        // Video encoding
        args += $"-c:v libx264 -preset {preset} -profile:v {profile} " +
                $"-b:v {settings.VideoBitrate}k -maxrate {settings.MaxBitrate}k " +
                $"-bufsize {settings.BufferSize}k " +
                $"-g {settings.VideoFrameRate * settings.KeyframeInterval} " +
                $"-keyint_min {settings.VideoFrameRate * settings.KeyframeInterval} ";

        // Pixel format conversion
        args += "-pix_fmt yuv420p ";

        // Audio (if enabled)
        if (settings.AudioEnabled)
        {
            args += $"-f lavfi -i anullsrc=r={settings.AudioSampleRate}:cl=stereo " +
                    $"-c:a aac -b:a {settings.AudioBitrate}k -ar {settings.AudioSampleRate} ";
        }

        // Output format
        args += "-f flv ";

        // RTMP URL
        args += $"\"{context.RtmpUrl}\"";

        return args;
    }

    private void ParseFFmpegOutput(StreamingContext context, string line)
    {
        // Parse FFmpeg stats from stderr
        if (line.Contains("bitrate="))
        {
            try
            {
                var bitrateMatch = System.Text.RegularExpressions.Regex.Match(line, @"bitrate=\s*(\d+\.?\d*)kbits/s");
                if (bitrateMatch.Success)
                {
                    context.Stats.CurrentBitrateKbps = (int)double.Parse(bitrateMatch.Groups[1].Value);
                }
            }
            catch { }
        }

        if (line.Contains("speed="))
        {
            try
            {
                var speedMatch = System.Text.RegularExpressions.Regex.Match(line, @"speed=\s*(\d+\.?\d*)x");
                if (speedMatch.Success)
                {
                    var speed = double.Parse(speedMatch.Groups[1].Value);
                    if (speed < 0.9)
                    {
                        LogMessage(context.CameraId, $"Encoding too slow: {speed:F2}x");
                    }
                }
            }
            catch { }
        }

        // Check for errors
        if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            LogMessage(context.CameraId, $"FFmpeg: {line}");
        }
    }

    private async Task UpdateStatsAsync(StreamingContext context)
    {
        while (!context.CancellationSource.Token.IsCancellationRequested)
        {
            context.Stats.Duration = DateTime.Now - context.StartTime;
            context.Stats.QueuedFrames = context.FrameQueue.Count;
            context.Stats.TargetBitrateKbps = context.Settings.VideoBitrate;

            StatsUpdated?.Invoke(this, new StreamingStatsEventArgs
            {
                CameraId = context.CameraId,
                Stats = context.Stats
            });

            await Task.Delay(1000, context.CancellationSource.Token);
        }
    }

    public async Task StopStreamingAsync(string cameraId)
    {
        if (_streams.TryRemove(cameraId, out var context))
        {
            context.CancellationSource.Cancel();

            try
            {
                if (context.StreamTask != null)
                    await context.StreamTask;
                if (context.StatsTask != null)
                    await context.StatsTask;
            }
            catch { }

            context.FFmpegProcess?.Kill();
            context.FFmpegProcess?.Dispose();

            UpdateState(context, StreamingState.Stopped);
            LogMessage(cameraId, "Streaming stopped");
        }
    }

    public bool IsStreaming(string cameraId) => _streams.ContainsKey(cameraId) &&
        _streams[cameraId].State == StreamingState.Streaming;

    public StreamingState GetStreamingState(string cameraId)
    {
        if (_streams.TryGetValue(cameraId, out var context))
            return context.State;
        return StreamingState.Stopped;
    }

    public StreamingStats GetStreamingStats(string cameraId)
    {
        if (_streams.TryGetValue(cameraId, out var context))
            return context.Stats;
        return new StreamingStats();
    }

    public async Task<bool> ValidateStreamKeyAsync(StreamingSettings settings, CancellationToken ct = default)
    {
        // Basic validation - check if RTMP endpoint is reachable
        try
        {
            var url = settings.GetFullRtmpUrl();
            if (string.IsNullOrEmpty(url)) return false;

            // Extract hostname from RTMP URL
            var uri = new Uri(url.Replace("rtmp://", "http://").Replace("rtmps://", "https://"));
            var testUrl = $"https://{uri.Host}";

            var response = await _httpClient.GetAsync(testUrl, ct);
            return true; // If we get any response, the host is reachable
        }
        catch
        {
            return false;
        }
    }

    public List<StreamingPlatform> GetSupportedPlatforms()
    {
        return new List<StreamingPlatform>
        {
            StreamingPlatform.YouTube,
            StreamingPlatform.Twitch,
            StreamingPlatform.Facebook,
            StreamingPlatform.Custom
        };
    }

    public StreamingSettings GetDefaultSettings(StreamingPlatform platform)
    {
        var settings = new StreamingSettings
        {
            Platform = platform,
            RtmpUrl = StreamingSettings.GetDefaultRtmpUrl(platform)
        };

        // Platform-specific defaults
        switch (platform)
        {
            case StreamingPlatform.YouTube:
                settings.VideoBitrate = 4500;
                settings.VideoWidth = 1920;
                settings.VideoHeight = 1080;
                settings.VideoFrameRate = 30;
                settings.KeyframeInterval = 2;
                settings.Preset = StreamingPreset.Veryfast;
                break;

            case StreamingPlatform.Twitch:
                settings.VideoBitrate = 6000;
                settings.VideoWidth = 1920;
                settings.VideoHeight = 1080;
                settings.VideoFrameRate = 60;
                settings.KeyframeInterval = 2;
                settings.Preset = StreamingPreset.Fast;
                break;

            case StreamingPlatform.Facebook:
                settings.VideoBitrate = 4000;
                settings.VideoWidth = 1280;
                settings.VideoHeight = 720;
                settings.VideoFrameRate = 30;
                settings.KeyframeInterval = 2;
                settings.Preset = StreamingPreset.Veryfast;
                break;
        }

        return settings;
    }

    public void PushFrame(string cameraId, byte[] frameData, int width, int height, long timestamp)
    {
        if (_streams.TryGetValue(cameraId, out var context) && context.State == StreamingState.Streaming)
        {
            // Limit queue size to prevent memory issues
            while (context.FrameQueue.Count > 60)
            {
                context.FrameQueue.TryDequeue(out _);
                context.Stats.FramesDropped++;
            }

            context.FrameQueue.Enqueue((frameData, width, height, timestamp));
        }
    }

    public void PushAudio(string cameraId, byte[] audioData, int sampleRate, int channels, long timestamp)
    {
        // TODO: Implement audio pipeline
        // For now, audio is generated as silence in FFmpeg
    }

    private void UpdateState(StreamingContext context, StreamingState newState)
    {
        var oldState = context.State;
        context.State = newState;
        context.Settings.State = newState;

        StateChanged?.Invoke(this, new StreamingStateChangedEventArgs
        {
            CameraId = context.CameraId,
            OldState = oldState,
            NewState = newState
        });
    }

    private void LogMessage(string cameraId, string message)
    {
        Log.Information($"[Streaming:{cameraId}] {message}");
        OnLog?.Invoke(cameraId, message);
    }

    private static string MaskUrl(string url)
    {
        // Mask stream key in URL for logging
        var parts = url.Split('/');
        if (parts.Length > 0)
        {
            parts[parts.Length - 1] = "****";
        }
        return string.Join("/", parts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var cameraId in _streams.Keys.ToList())
        {
            StopStreamingAsync(cameraId).Wait();
        }

        _httpClient.Dispose();
    }

    private class StreamingContext
    {
        public string CameraId { get; set; } = string.Empty;
        public StreamingSettings Settings { get; set; } = new();
        public string RtmpUrl { get; set; } = string.Empty;
        public StreamingState State { get; set; }
        public DateTime StartTime { get; set; }
        public string PipeName { get; set; } = string.Empty;
        public ConcurrentQueue<(byte[] data, int width, int height, long timestamp)> FrameQueue { get; set; } = new();
        public CancellationTokenSource CancellationSource { get; set; } = new();
        public Process? FFmpegProcess { get; set; }
        public Task? StreamTask { get; set; }
        public Task? StatsTask { get; set; }
        public StreamingStats Stats { get; set; } = new();
    }
}
