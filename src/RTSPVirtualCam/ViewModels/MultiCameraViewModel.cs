using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;
using Serilog;

namespace RTSPVirtualCam.ViewModels;

/// <summary>
/// ViewModel for the v2.0 Multi-Camera Platform.
/// Manages multiple simultaneous camera connections with independent controls.
/// </summary>
public partial class MultiCameraViewModel : ObservableObject
{
    private readonly IMultiCameraService _cameraService;
    private readonly IAdvancedPtzService _ptzService;
    private readonly IRecordingService _recordingService;
    private readonly IRtmpStreamingService _streamingService;
    private readonly IMotionDetectionService _motionService;
    private readonly ICloudSyncService _cloudSyncService;
    private readonly IApiServerService _apiServerService;
    private readonly IHardwareAccelerationService _hwAccelService;
    private readonly StringBuilder _logBuilder = new();

    // Camera instances
    public ObservableCollection<CameraInstanceViewModel> Cameras { get; } = new();

    [ObservableProperty]
    private CameraInstanceViewModel? _selectedCamera;

    [ObservableProperty]
    private int _selectedCameraIndex;

    // Global status
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _connectedCameraCount;

    [ObservableProperty]
    private int _virtualizedCameraCount;

    [ObservableProperty]
    private int _recordingCameraCount;

    [ObservableProperty]
    private int _streamingCameraCount;

    // Logs
    [ObservableProperty]
    private string _logText = string.Empty;

    // API Server
    [ObservableProperty]
    private bool _isApiServerRunning;

    [ObservableProperty]
    private int _apiServerPort = 8080;

    // Hardware acceleration
    [ObservableProperty]
    private bool _isHardwareAccelerationEnabled;

    [ObservableProperty]
    private string _hardwareAccelerationStatus = string.Empty;

    // Cloud sync
    [ObservableProperty]
    private bool _isCloudSyncEnabled;

    [ObservableProperty]
    private DateTime? _lastSyncTime;

    // Layout options
    [ObservableProperty]
    private CameraLayoutMode _layoutMode = CameraLayoutMode.Grid2x2;

    public ObservableCollection<CameraLayoutMode> LayoutModes { get; } = new(Enum.GetValues<CameraLayoutMode>());

    public MultiCameraViewModel(
        IMultiCameraService cameraService,
        IAdvancedPtzService ptzService,
        IRecordingService recordingService,
        IRtmpStreamingService streamingService,
        IMotionDetectionService motionService,
        ICloudSyncService cloudSyncService,
        IApiServerService apiServerService,
        IHardwareAccelerationService hwAccelService)
    {
        _cameraService = cameraService;
        _ptzService = ptzService;
        _recordingService = recordingService;
        _streamingService = streamingService;
        _motionService = motionService;
        _cloudSyncService = cloudSyncService;
        _apiServerService = apiServerService;
        _hwAccelService = hwAccelService;

        // Subscribe to events
        _cameraService.CameraStateChanged += OnCameraStateChanged;
        _cameraService.FrameReceived += OnFrameReceived;
        _cameraService.ErrorOccurred += OnCameraError;
        _cameraService.OnLog += (id, msg) => AddLog($"[{id}] {msg}");

        _ptzService.OnLog += msg => AddLog($"[PTZ] {msg}");
        _recordingService.OnLog += (id, msg) => AddLog($"[Recording:{id}] {msg}");
        _streamingService.OnLog += (id, msg) => AddLog($"[Streaming:{id}] {msg}");
        _motionService.OnLog += (id, msg) => AddLog($"[Motion:{id}] {msg}");
        _motionService.MotionDetected += OnMotionDetected;
        _apiServerService.OnLog += msg => AddLog($"[API] {msg}");
        _hwAccelService.OnLog += msg => AddLog($"[HWAccel] {msg}");

        // Initialize
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        AddLog("RTSP VirtualCam v2.0 - Multi-Camera Platform");
        AddLog($"Base directory: {AppContext.BaseDirectory}");

        // Check hardware acceleration
        var hwInfo = _hwAccelService.GetInfo();
        if (hwInfo.IsSupported)
        {
            HardwareAccelerationStatus = $"GPU: {hwInfo.GpuName} ({hwInfo.RecommendedType})";
            AddLog($"Hardware acceleration available: {hwInfo.RecommendedType}");
        }
        else
        {
            HardwareAccelerationStatus = "Not available";
        }

        // Add default camera slots
        for (int i = 0; i < 4; i++)
        {
            AddCameraSlot();
        }

        AddLog($"Initialized {Cameras.Count} camera slots");
    }

    // Camera management
    [RelayCommand]
    private void AddCameraSlot()
    {
        var camera = _cameraService.AddCamera();
        var vm = new CameraInstanceViewModel(camera, this);
        Cameras.Add(vm);

        if (SelectedCamera == null)
        {
            SelectedCamera = vm;
        }

        UpdateCounts();
    }

    [RelayCommand]
    private void RemoveCameraSlot()
    {
        if (SelectedCamera == null) return;

        _cameraService.RemoveCamera(SelectedCamera.Camera.Id);
        Cameras.Remove(SelectedCamera);
        SelectedCamera = Cameras.FirstOrDefault();

        UpdateCounts();
    }

    [RelayCommand]
    private async Task ConnectAllAsync()
    {
        AddLog("Connecting all cameras...");
        await _cameraService.ConnectAllAsync();
        UpdateCounts();
    }

    [RelayCommand]
    private async Task DisconnectAllAsync()
    {
        AddLog("Disconnecting all cameras...");
        await _cameraService.DisconnectAllAsync();
        UpdateCounts();
    }

    // API Server
    [RelayCommand]
    private async Task ToggleApiServerAsync()
    {
        if (IsApiServerRunning)
        {
            await _apiServerService.StopAsync();
            IsApiServerRunning = false;
            AddLog("API server stopped");
        }
        else
        {
            try
            {
                await _apiServerService.StartAsync(ApiServerPort);
                IsApiServerRunning = true;
                AddLog($"API server started on port {ApiServerPort}");
            }
            catch (Exception ex)
            {
                AddLog($"Failed to start API server: {ex.Message}");
            }
        }
    }

    // Hardware Acceleration
    [RelayCommand]
    private void ToggleHardwareAcceleration()
    {
        if (IsHardwareAccelerationEnabled)
        {
            _hwAccelService.Disable();
            IsHardwareAccelerationEnabled = false;
        }
        else
        {
            if (_hwAccelService.Enable())
            {
                IsHardwareAccelerationEnabled = true;
            }
        }
    }

    // Cloud Sync
    [RelayCommand]
    private async Task SyncWithCloudAsync()
    {
        AddLog("Syncing with cloud...");
        var success = await _cloudSyncService.SyncAsync();
        LastSyncTime = _cloudSyncService.GetLastSyncTime();
        AddLog(success ? "Cloud sync completed" : "Cloud sync failed");
    }

    // Recording
    [RelayCommand]
    private async Task StartAllRecordingAsync()
    {
        foreach (var cam in Cameras.Where(c => c.IsConnected && !c.IsRecording))
        {
            await _recordingService.StartRecordingAsync(cam.Camera.Id);
            cam.IsRecording = true;
        }
        UpdateCounts();
    }

    [RelayCommand]
    private async Task StopAllRecordingAsync()
    {
        foreach (var cam in Cameras.Where(c => c.IsRecording))
        {
            await _recordingService.StopRecordingAsync(cam.Camera.Id);
            cam.IsRecording = false;
        }
        UpdateCounts();
    }

    // Snapshots
    [RelayCommand]
    private async Task TakeAllSnapshotsAsync()
    {
        foreach (var cam in Cameras.Where(c => c.IsConnected))
        {
            await _cameraService.TakeSnapshotAsync(cam.Camera.Id);
        }
        AddLog($"Snapshots taken for {Cameras.Count(c => c.IsConnected)} cameras");
    }

    // Event handlers
    private void OnCameraStateChanged(object? sender, CameraStateChangedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            var vm = Cameras.FirstOrDefault(c => c.Camera.Id == e.CameraId);
            if (vm != null)
            {
                vm.UpdateFromCamera();
            }
            UpdateCounts();
        });
    }

    private void OnFrameReceived(object? sender, CameraFrameEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var vm = Cameras.FirstOrDefault(c => c.Camera.Id == e.CameraId);
            if (vm != null)
            {
                vm.UpdatePreviewFrame(e.FrameData, e.Width, e.Height);
            }

            // Feed frames to other services
            _recordingService.PushFrame(e.CameraId, e.FrameData, e.Width, e.Height, e.Timestamp);
            _streamingService.PushFrame(e.CameraId, e.FrameData, e.Width, e.Height, e.Timestamp);
            _motionService.ProcessFrame(e.CameraId, e.FrameData, e.Width, e.Height, e.Timestamp);
        });
    }

    private void OnCameraError(object? sender, CameraErrorEventArgs e)
    {
        AddLog($"[{e.CameraId}] Error: {e.ErrorMessage}");
    }

    private void OnMotionDetected(object? sender, MotionDetectedEventArgs e)
    {
        AddLog($"[{e.CameraId}] Motion detected: {e.MotionPercentage:F1}%");

        // Trigger recording on motion if enabled
        var settings = _motionService.GetSettings(e.CameraId);
        if (settings.RecordOnMotion && !_recordingService.IsRecording(e.CameraId))
        {
            _ = _recordingService.StartRecordingAsync(e.CameraId);
        }
    }

    private void UpdateCounts()
    {
        ConnectedCameraCount = Cameras.Count(c => c.IsConnected);
        VirtualizedCameraCount = Cameras.Count(c => c.IsVirtualized);
        RecordingCameraCount = Cameras.Count(c => c.IsRecording);
        StreamingCameraCount = Cameras.Count(c => c.IsStreaming);

        StatusText = $"{ConnectedCameraCount}/{Cameras.Count} connected";
    }

    public void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        _logBuilder.AppendLine(logEntry);
        LogText = _logBuilder.ToString();
        Log.Information(message);
    }

    [RelayCommand]
    private void ClearLog()
    {
        _logBuilder.Clear();
        LogText = string.Empty;
    }

    [RelayCommand]
    private void CopyLogs()
    {
        try
        {
            System.Windows.Clipboard.SetText(LogText);
            AddLog("Logs copied to clipboard");
        }
        catch { }
    }

    // Camera commands passthrough
    public async Task ConnectCameraAsync(string cameraId)
    {
        await _cameraService.ConnectCameraAsync(cameraId);
    }

    public async Task DisconnectCameraAsync(string cameraId)
    {
        await _cameraService.DisconnectCameraAsync(cameraId);
    }

    public async Task StartVirtualCameraAsync(string cameraId)
    {
        await _cameraService.StartVirtualCameraAsync(cameraId);
    }

    public async Task StopVirtualCameraAsync(string cameraId)
    {
        await _cameraService.StopVirtualCameraAsync(cameraId);
    }

    public async Task StartRecordingAsync(string cameraId)
    {
        await _recordingService.StartRecordingAsync(cameraId);
    }

    public async Task StopRecordingAsync(string cameraId)
    {
        await _recordingService.StopRecordingAsync(cameraId);
    }

    public async Task TakeSnapshotAsync(string cameraId)
    {
        await _cameraService.TakeSnapshotAsync(cameraId);
    }

    // PTZ passthrough
    public async Task PtzMoveAsync(string cameraId, PtzDirection direction, int speed)
    {
        await _ptzService.MoveAsync(cameraId, direction, speed);
    }

    public async Task PtzStopAsync(string cameraId)
    {
        await _ptzService.StopAsync(cameraId);
    }

    public async Task PtzZoomAsync(string cameraId, ZoomDirection direction, int speed)
    {
        await _ptzService.ZoomAsync(cameraId, direction, speed);
    }

    public async Task PtzGoToPresetAsync(string cameraId, int presetId)
    {
        await _ptzService.GoToPresetAsync(cameraId, presetId);
    }
}

/// <summary>
/// ViewModel for a single camera instance in the multi-camera grid.
/// </summary>
public partial class CameraInstanceViewModel : ObservableObject
{
    private readonly MultiCameraViewModel _parent;

    public CameraInstance Camera { get; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isVirtualized;

    [ObservableProperty]
    private bool _isRecording;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private string _statusText = "Disconnected";

    [ObservableProperty]
    private string _resolution = "--";

    [ObservableProperty]
    private string _frameRateText = "--";

    [ObservableProperty]
    private WriteableBitmap? _previewBitmap;

    // Connection settings
    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private int _port = 554;

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _rtspUrl = string.Empty;

    [ObservableProperty]
    private CameraBrand _brand = CameraBrand.Hikvision;

    [ObservableProperty]
    private StreamType _streamType = StreamType.MainStream;

    [ObservableProperty]
    private int _channel = 1;

    // PTZ
    [ObservableProperty]
    private int _ptzSpeed = 50;

    public CameraInstanceViewModel(CameraInstance camera, MultiCameraViewModel parent)
    {
        Camera = camera;
        _parent = parent;
        UpdateFromCamera();
    }

    public void UpdateFromCamera()
    {
        Name = Camera.Name;
        IsConnected = Camera.IsConnected;
        IsConnecting = Camera.IsConnecting;
        IsVirtualized = Camera.IsVirtualized;
        IsRecording = Camera.IsRecording;
        IsStreaming = Camera.IsStreaming;
        StatusText = Camera.StatusMessage;
        Resolution = Camera.Resolution;
        FrameRateText = Camera.FrameRateDisplay;
        IpAddress = Camera.IpAddress;
        Port = Camera.Port;
        Username = Camera.Username;
        Password = Camera.Password;
        RtspUrl = Camera.RtspUrl;
        Brand = Camera.Brand;
        StreamType = Camera.StreamType;
        Channel = Camera.Channel;
    }

    public void UpdatePreviewFrame(byte[] bgra, int width, int height)
    {
        try
        {
            if (PreviewBitmap == null || PreviewBitmap.PixelWidth != width || PreviewBitmap.PixelHeight != height)
            {
                PreviewBitmap = new WriteableBitmap(width, height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32, null);
            }

            PreviewBitmap.WritePixels(
                new System.Windows.Int32Rect(0, 0, width, height),
                bgra, width * 4, 0);
        }
        catch { }
    }

    private void SyncToCamera()
    {
        Camera.Name = Name;
        Camera.IpAddress = IpAddress;
        Camera.Port = Port;
        Camera.Username = Username;
        Camera.Password = Password;
        Camera.RtspUrl = RtspUrl;
        Camera.Brand = Brand;
        Camera.StreamType = StreamType;
        Camera.Channel = Channel;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        SyncToCamera();
        await _parent.ConnectCameraAsync(Camera.Id);
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _parent.DisconnectCameraAsync(Camera.Id);
    }

    [RelayCommand]
    private async Task VirtualizeAsync()
    {
        await _parent.StartVirtualCameraAsync(Camera.Id);
        IsVirtualized = Camera.IsVirtualized;
    }

    [RelayCommand]
    private async Task StopVirtualAsync()
    {
        await _parent.StopVirtualCameraAsync(Camera.Id);
        IsVirtualized = Camera.IsVirtualized;
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        await _parent.StartRecordingAsync(Camera.Id);
        IsRecording = true;
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        await _parent.StopRecordingAsync(Camera.Id);
        IsRecording = false;
    }

    [RelayCommand]
    private async Task TakeSnapshotAsync()
    {
        await _parent.TakeSnapshotAsync(Camera.Id);
    }

    // PTZ commands
    [RelayCommand]
    private async Task PtzUpAsync() => await _parent.PtzMoveAsync(Camera.Id, PtzDirection.Up, PtzSpeed);

    [RelayCommand]
    private async Task PtzDownAsync() => await _parent.PtzMoveAsync(Camera.Id, PtzDirection.Down, PtzSpeed);

    [RelayCommand]
    private async Task PtzLeftAsync() => await _parent.PtzMoveAsync(Camera.Id, PtzDirection.Left, PtzSpeed);

    [RelayCommand]
    private async Task PtzRightAsync() => await _parent.PtzMoveAsync(Camera.Id, PtzDirection.Right, PtzSpeed);

    [RelayCommand]
    private async Task PtzStopAsync() => await _parent.PtzStopAsync(Camera.Id);

    [RelayCommand]
    private async Task PtzZoomInAsync() => await _parent.PtzZoomAsync(Camera.Id, ZoomDirection.In, PtzSpeed);

    [RelayCommand]
    private async Task PtzZoomOutAsync() => await _parent.PtzZoomAsync(Camera.Id, ZoomDirection.Out, PtzSpeed);
}

public enum CameraLayoutMode
{
    Single,
    Grid2x2,
    Grid3x3,
    Grid4x4,
    Horizontal,
    Vertical
}
