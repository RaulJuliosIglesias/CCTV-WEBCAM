using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RTSPVirtualCam.Models;

/// <summary>
/// Cloud configuration sync settings.
/// </summary>
public partial class CloudSyncSettings : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private CloudProvider _provider = CloudProvider.Custom;

    [ObservableProperty]
    private string _serverUrl = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _deviceId = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _deviceName = Environment.MachineName;

    [ObservableProperty]
    private bool _autoSync = true;

    [ObservableProperty]
    private int _syncIntervalMinutes = 5;

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    [ObservableProperty]
    private SyncState _syncState = SyncState.Idle;

    [ObservableProperty]
    private string _lastError = string.Empty;

    // What to sync
    [ObservableProperty]
    private bool _syncCameraProfiles = true;

    [ObservableProperty]
    private bool _syncPtzPresets = true;

    [ObservableProperty]
    private bool _syncRecordingSettings = true;

    [ObservableProperty]
    private bool _syncMotionSettings = true;

    [ObservableProperty]
    private bool _syncStreamingSettings = true;

    [ObservableProperty]
    private bool _syncApplicationSettings = true;

    // Conflict resolution
    [ObservableProperty]
    private ConflictResolution _conflictResolution = ConflictResolution.ServerWins;

    // Encryption
    [ObservableProperty]
    private bool _encryptData = true;

    [ObservableProperty]
    private string _encryptionKey = string.Empty;
}

public enum CloudProvider
{
    Custom,         // Self-hosted API
    Firebase,
    AWS,
    Azure,
    GoogleCloud
}

public enum SyncState
{
    Idle,
    Syncing,
    Error,
    Conflict
}

public enum ConflictResolution
{
    ServerWins,     // Server data overwrites local
    LocalWins,      // Local data overwrites server
    MostRecent,     // Most recently modified wins
    Manual          // User must resolve
}

/// <summary>
/// Represents synchronized configuration data.
/// </summary>
public class SyncData
{
    public string Version { get; set; } = "2.0";
    public DateTime LastModified { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    
    // Configuration data
    public List<CameraProfile> CameraProfiles { get; set; } = new();
    public List<AdvancedPtzPreset> PtzPresets { get; set; } = new();
    public List<PtzTour> PtzTours { get; set; } = new();
    public List<RecordingSettings> RecordingSettings { get; set; } = new();
    public List<MotionDetectionSettings> MotionSettings { get; set; } = new();
    public List<StreamingSettings> StreamingSettings { get; set; } = new();
    public ApplicationSettings? AppSettings { get; set; }
    
    // Checksum for integrity
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// Application-wide settings.
/// </summary>
public partial class ApplicationSettings : ObservableObject
{
    [ObservableProperty]
    private string _theme = "Dark";

    [ObservableProperty]
    private string _language = "en-US";

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _checkForUpdates = true;

    [ObservableProperty]
    private bool _enableLogging = true;

    [ObservableProperty]
    private LogLevel _logLevel = LogLevel.Information;

    [ObservableProperty]
    private int _maxLogFileSizeMB = 50;

    [ObservableProperty]
    private int _logRetentionDays = 7;

    // Default paths
    [ObservableProperty]
    private string _defaultRecordingPath = string.Empty;

    [ObservableProperty]
    private string _defaultSnapshotPath = string.Empty;

    // Performance
    [ObservableProperty]
    private bool _hardwareAccelerationEnabled = true;

    [ObservableProperty]
    private HardwareAccelerationType _hardwareAccelerationType = HardwareAccelerationType.Auto;

    [ObservableProperty]
    private int _maxConcurrentConnections = 16;

    [ObservableProperty]
    private int _previewQualityPercent = 75;

    // Network
    [ObservableProperty]
    private int _connectionTimeoutSeconds = 30;

    [ObservableProperty]
    private int _reconnectAttempts = 5;

    [ObservableProperty]
    private int _reconnectDelaySeconds = 10;

    [ObservableProperty]
    private bool _useProxyServer;

    [ObservableProperty]
    private string _proxyServer = string.Empty;

    [ObservableProperty]
    private int _proxyPort = 8080;

    // API Server (for mobile app)
    [ObservableProperty]
    private bool _apiServerEnabled;

    [ObservableProperty]
    private int _apiServerPort = 8080;

    [ObservableProperty]
    private string _apiAuthToken = string.Empty;

    [ObservableProperty]
    private bool _apiRequireAuth = true;
}

public enum LogLevel
{
    Verbose,
    Debug,
    Information,
    Warning,
    Error,
    Fatal
}

public enum HardwareAccelerationType
{
    Auto,
    None,
    DXVA2,      // DirectX Video Acceleration 2
    D3D11VA,    // Direct3D 11 Video Acceleration
    CUDA,       // NVIDIA CUDA
    QSV,        // Intel Quick Sync Video
    VAAPI       // Video Acceleration API (Linux)
}
