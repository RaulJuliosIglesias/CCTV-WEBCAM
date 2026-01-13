using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// RTMP streaming configuration for platforms like YouTube, Twitch, etc.
/// </summary>
public partial class StreamingSettings : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _cameraId = string.Empty;

    [ObservableProperty]
    private string _name = "Stream";

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private StreamingPlatform _platform = StreamingPlatform.Custom;

    [ObservableProperty]
    private string _rtmpUrl = string.Empty;

    [ObservableProperty]
    private string _streamKey = string.Empty;

    [ObservableProperty]
    private string _backupRtmpUrl = string.Empty;

    // Video encoding settings
    [ObservableProperty]
    private StreamingVideoCodec _videoCodec = StreamingVideoCodec.H264;

    [ObservableProperty]
    private int _videoBitrate = 4500; // kbps

    [ObservableProperty]
    private int _videoWidth = 1920;

    [ObservableProperty]
    private int _videoHeight = 1080;

    [ObservableProperty]
    private int _videoFrameRate = 30;

    [ObservableProperty]
    private int _keyframeInterval = 2; // seconds

    [ObservableProperty]
    private StreamingPreset _preset = StreamingPreset.Veryfast;

    [ObservableProperty]
    private StreamingProfile _profile = StreamingProfile.High;

    // Audio encoding settings
    [ObservableProperty]
    private bool _audioEnabled = true;

    [ObservableProperty]
    private StreamingAudioCodec _audioCodec = StreamingAudioCodec.AAC;

    [ObservableProperty]
    private int _audioBitrate = 160; // kbps

    [ObservableProperty]
    private int _audioSampleRate = 48000;

    [ObservableProperty]
    private int _audioChannels = 2;

    // Advanced settings
    [ObservableProperty]
    private bool _adaptiveBitrate = true;

    [ObservableProperty]
    private int _minBitrate = 1500; // kbps

    [ObservableProperty]
    private int _maxBitrate = 6000; // kbps

    [ObservableProperty]
    private int _bufferSize = 4500; // kbps

    [ObservableProperty]
    private bool _lowLatencyMode;

    [ObservableProperty]
    private int _reconnectAttempts = 5;

    [ObservableProperty]
    private int _reconnectDelaySeconds = 10;

    // Status
    [ObservableProperty]
    private StreamingState _state = StreamingState.Stopped;

    [ObservableProperty]
    private DateTime? _startedAt;

    [ObservableProperty]
    private TimeSpan _streamDuration;

    [ObservableProperty]
    private long _bytesUploaded;

    [ObservableProperty]
    private int _currentBitrate;

    [ObservableProperty]
    private int _droppedFrames;

    [ObservableProperty]
    private double _uploadSpeedMbps;

    public string GetFullRtmpUrl()
    {
        if (string.IsNullOrEmpty(StreamKey))
            return RtmpUrl;

        return Platform switch
        {
            StreamingPlatform.YouTube => $"rtmp://a.rtmp.youtube.com/live2/{StreamKey}",
            StreamingPlatform.Twitch => $"rtmp://live.twitch.tv/app/{StreamKey}",
            StreamingPlatform.Facebook => $"rtmps://live-api-s.facebook.com:443/rtmp/{StreamKey}",
            _ => RtmpUrl.TrimEnd('/') + "/" + StreamKey
        };
    }

    public static string GetDefaultRtmpUrl(StreamingPlatform platform)
    {
        return platform switch
        {
            StreamingPlatform.YouTube => "rtmp://a.rtmp.youtube.com/live2",
            StreamingPlatform.Twitch => "rtmp://live.twitch.tv/app",
            StreamingPlatform.Facebook => "rtmps://live-api-s.facebook.com:443/rtmp",
            _ => string.Empty
        };
    }
}

public enum StreamingVideoCodec
{
    H264,
    H265,
    VP8,
    VP9,
    AV1
}

public enum StreamingAudioCodec
{
    AAC,
    MP3,
    Opus
}

public enum StreamingPreset
{
    Ultrafast,
    Superfast,
    Veryfast,
    Faster,
    Fast,
    Medium,
    Slow,
    Slower,
    Veryslow
}

public enum StreamingProfile
{
    Baseline,
    Main,
    High
}

public enum StreamingState
{
    Stopped,
    Starting,
    Streaming,
    Reconnecting,
    Error
}

/// <summary>
/// Network bandwidth optimization settings.
/// </summary>
public partial class BandwidthSettings : ObservableObject
{
    [ObservableProperty]
    private bool _adaptiveBitrateEnabled = true;

    [ObservableProperty]
    private int _targetBitrateKbps = 4000;

    [ObservableProperty]
    private int _minBitrateKbps = 500;

    [ObservableProperty]
    private int _maxBitrateKbps = 8000;

    [ObservableProperty]
    private int _bufferSizeMs = 1000;

    [ObservableProperty]
    private bool _autoQualityEnabled = true;

    [ObservableProperty]
    private NetworkQualityLevel _currentQuality = NetworkQualityLevel.High;

    [ObservableProperty]
    private int _measurementIntervalSeconds = 5;

    // Traffic shaping
    [ObservableProperty]
    private bool _trafficShapingEnabled;

    [ObservableProperty]
    private int _maxBandwidthMbps = 100;

    [ObservableProperty]
    private int _bandwidthReservationPercent = 80; // Reserve 80% for streaming

    // Congestion control
    [ObservableProperty]
    private CongestionControlAlgorithm _congestionControl = CongestionControlAlgorithm.AIMD;

    [ObservableProperty]
    private int _packetLossThresholdPercent = 2;

    [ObservableProperty]
    private int _latencyThresholdMs = 500;
}

public enum NetworkQualityLevel
{
    Low,
    Medium,
    High,
    Ultra
}

public enum CongestionControlAlgorithm
{
    AIMD,       // Additive Increase Multiplicative Decrease
    BBR,        // Bottleneck Bandwidth and RTT
    CUBIC,      // Cubic function
    Vegas       // TCP Vegas style
}
