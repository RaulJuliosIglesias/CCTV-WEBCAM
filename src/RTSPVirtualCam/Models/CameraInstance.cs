using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// Represents a single camera instance in the multi-camera platform.
/// Each instance has independent connection, PTZ, recording, and streaming controls.
/// </summary>
public partial class CameraInstance : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = "Camera";

    [ObservableProperty]
    private int _slotIndex;

    // Connection settings
    [ObservableProperty]
    private string _rtspUrl = string.Empty;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private int _port = 554;

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private CameraBrand _brand = CameraBrand.Hikvision;

    [ObservableProperty]
    private StreamType _streamType = StreamType.MainStream;

    [ObservableProperty]
    private int _channel = 1;

    // Connection state
    [ObservableProperty]
    private CameraConnectionState _connectionState = CameraConnectionState.Disconnected;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isVirtualized;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isStreaming; // RTMP streaming

    [ObservableProperty]
    private string _statusMessage = "Ready";

    // Stream information
    [ObservableProperty]
    private int _width;

    [ObservableProperty]
    private int _height;

    [ObservableProperty]
    private int _frameRate;

    [ObservableProperty]
    private string _codec = string.Empty;

    [ObservableProperty]
    private long _bitrate;

    [ObservableProperty]
    private long _framesReceived;

    [ObservableProperty]
    private long _bytesReceived;

    // Audio settings
    [ObservableProperty]
    private bool _audioEnabled;

    [ObservableProperty]
    private int _audioVolume = 100;

    [ObservableProperty]
    private bool _audioMuted;

    // Video adjustments
    [ObservableProperty]
    private bool _flipHorizontal;

    [ObservableProperty]
    private bool _flipVertical;

    [ObservableProperty]
    private int _brightness;

    [ObservableProperty]
    private int _contrast;

    [ObservableProperty]
    private int _saturation;

    // PTZ settings
    [ObservableProperty]
    private bool _ptzSupported;

    [ObservableProperty]
    private string _ptzUsername = string.Empty;

    [ObservableProperty]
    private string _ptzPassword = string.Empty;

    [ObservableProperty]
    private int _ptzMoveSpeed = 75;

    [ObservableProperty]
    private int _ptzZoomSpeed = 75;

    [ObservableProperty]
    private int _currentPreset;

    // Recording settings
    [ObservableProperty]
    private string _recordingPath = string.Empty;

    [ObservableProperty]
    private RecordingFormat _recordingFormat = RecordingFormat.MP4;

    [ObservableProperty]
    private DateTime? _recordingStartTime;

    [ObservableProperty]
    private TimeSpan _recordingDuration;

    // Streaming settings (RTMP)
    [ObservableProperty]
    private string _rtmpUrl = string.Empty;

    [ObservableProperty]
    private string _streamKey = string.Empty;

    [ObservableProperty]
    private StreamingPlatform _streamingPlatform = StreamingPlatform.Custom;

    // Analytics
    [ObservableProperty]
    private bool _motionDetectionEnabled;

    [ObservableProperty]
    private int _motionSensitivity = 50;

    [ObservableProperty]
    private DateTime? _lastMotionDetected;

    [ObservableProperty]
    private int _motionEventCount;

    // Network stats
    [ObservableProperty]
    private double _currentBitrateMbps;

    [ObservableProperty]
    private double _averageBitrateMbps;

    [ObservableProperty]
    private int _packetLossPercent;

    [ObservableProperty]
    private int _latencyMs;

    // Preview bitmap for UI
    [ObservableProperty]
    private System.Windows.Media.Imaging.WriteableBitmap? _previewBitmap;

    public string DisplayName => string.IsNullOrEmpty(Name) ? $"Camera {SlotIndex + 1}" : Name;

    public string Resolution => Width > 0 && Height > 0 ? $"{Width}x{Height}" : "--";

    public string FrameRateDisplay => FrameRate > 0 ? $"{FrameRate} fps" : "--";

    public string BitrateDisplay => Bitrate > 0 ? $"{Bitrate / 1000.0:F1} kbps" : "--";

    public CameraInstance()
    {
    }

    public CameraInstance(int slotIndex)
    {
        SlotIndex = slotIndex;
        Name = $"Camera {slotIndex + 1}";
    }

    public CameraInstance Clone()
    {
        return new CameraInstance
        {
            Id = Id,
            Name = Name,
            SlotIndex = SlotIndex,
            RtspUrl = RtspUrl,
            IpAddress = IpAddress,
            Port = Port,
            Username = Username,
            Password = Password,
            Brand = Brand,
            StreamType = StreamType,
            Channel = Channel,
            AudioEnabled = AudioEnabled,
            AudioVolume = AudioVolume,
            FlipHorizontal = FlipHorizontal,
            FlipVertical = FlipVertical,
            Brightness = Brightness,
            Contrast = Contrast,
            Saturation = Saturation,
            PtzSupported = PtzSupported,
            PtzUsername = PtzUsername,
            PtzPassword = PtzPassword,
            PtzMoveSpeed = PtzMoveSpeed,
            PtzZoomSpeed = PtzZoomSpeed,
            RecordingPath = RecordingPath,
            RecordingFormat = RecordingFormat,
            RtmpUrl = RtmpUrl,
            StreamKey = StreamKey,
            StreamingPlatform = StreamingPlatform,
            MotionDetectionEnabled = MotionDetectionEnabled,
            MotionSensitivity = MotionSensitivity
        };
    }
}

public enum CameraConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error
}

public enum RecordingFormat
{
    MP4,
    MKV,
    AVI,
    TS
}

public enum StreamingPlatform
{
    Custom,
    YouTube,
    Twitch,
    Facebook,
    RTMP
}
