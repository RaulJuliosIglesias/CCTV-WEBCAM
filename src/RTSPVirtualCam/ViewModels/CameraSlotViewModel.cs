using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;
using Serilog;

namespace RTSPVirtualCam.ViewModels;

public partial class CameraSlotViewModel : ObservableObject
{
    // Each slot has its OWN RtspService instance for independent control
    private RtspService? _ownRtspService;
    public RtspService? RtspService => _ownRtspService;
    
    // Each slot has its OWN virtual camera output
    // Slot 0 uses OBS Virtual Camera (widely installed), others use Unity Capture
    private UnityCaptureOutput? _unityCaptureOutput;
    private OBSVirtualCamOutput? _obsOutput;
    
    // Cancellation token for connection
    private CancellationTokenSource? _connectionCts;
    
    // Callback for logging to main UI
    public Action<string>? OnLog { get; set; }
    
    public int SlotIndex { get; }
    public string SlotName => $"Camera {SlotIndex + 1}";
    
    // Connection settings
    [ObservableProperty]
    private string _ipAddress = "192.168.1.100";
    
    [ObservableProperty]
    private int _port = 554;
    
    [ObservableProperty]
    private string _username = "admin";
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _ptzUsername = string.Empty;
    
    [ObservableProperty]
    private string _ptzPassword = string.Empty;
    
    [ObservableProperty]
    private CameraBrand _selectedBrand = CameraBrand.Hikvision;
    
    [ObservableProperty]
    private StreamType _selectedStream = StreamType.MainStream;
    
    [ObservableProperty]
    private int _channel = 1;
    
    [ObservableProperty]
    private string _manualUrl = string.Empty;
    
    [ObservableProperty]
    private bool _useManualUrl;
    
    // Status
    [ObservableProperty]
    private bool _isConnecting;
    
    [ObservableProperty]
    private bool _isConnected;
    
    [ObservableProperty]
    private bool _isVirtualized;
    
    [ObservableProperty]
    private bool _isRecording;
    
    [ObservableProperty]
    private bool _isSelected;
    
    [ObservableProperty]
    private string _statusText = "Not configured";
    
    [ObservableProperty]
    private string _statusIcon = "⚫";
    
    // Stream info
    [ObservableProperty]
    private string _resolution = "--";
    
    [ObservableProperty]
    private string _frameRate = "--";
    
    // Per-camera PREVIEW video adjustments
    [ObservableProperty]
    private bool _previewFlipHorizontal;
    
    [ObservableProperty]
    private bool _previewFlipVertical;
    
    [ObservableProperty]
    private int _previewBrightness = 0;
    
    [ObservableProperty]
    private int _previewContrast = 0;
    
    // Per-camera VIRTUAL OUTPUT video adjustments (separate from preview)
    [ObservableProperty]
    private bool _virtualFlipHorizontal;
    
    [ObservableProperty]
    private bool _virtualFlipVertical;
    
    [ObservableProperty]
    private int _virtualBrightness = 0;
    
    [ObservableProperty]
    private int _virtualContrast = 0;
    
    // Per-camera PTZ credentials  
    [ObservableProperty]
    private int _ptzMoveSpeed = 75;
    
    [ObservableProperty]
    private int _ptzZoomSpeed = 75;
    
    // Virtual camera output name (unique per slot)
    public string VirtualCameraName => $"RTSP Cam {SlotIndex + 1}";
    
    // Preview
    [ObservableProperty]
    private WriteableBitmap? _previewBitmap;
    
    // Generated URL
    public string GeneratedUrl
    {
        get
        {
            if (UseManualUrl && !string.IsNullOrWhiteSpace(ManualUrl))
                return ManualUrl;
            
            var connection = new CameraConnection
            {
                Brand = SelectedBrand,
                IpAddress = IpAddress,
                Port = Port,
                Username = Username,
                Password = Password,
                Channel = Channel,
                Stream = SelectedStream
            };
            return connection.GenerateRtspUrl();
        }
    }
    
    public CameraSlotViewModel(int slotIndex)
    {
        SlotIndex = slotIndex;
        // Create own RtspService instance for independent control
        _ownRtspService = new RtspService();
        _ownRtspService.OnPreviewFrame += OnPreviewFrameReceived;
        _ownRtspService.OnLog += msg => OnLog?.Invoke($"[{SlotName}] {msg}");
    }
    
    private void OnPreviewFrameReceived(byte[] bgra, int width, int height)
    {
        try
        {
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (PreviewBitmap == null || PreviewBitmap.PixelWidth != width || PreviewBitmap.PixelHeight != height)
                {
                    PreviewBitmap = new WriteableBitmap(width, height, 96, 96,
                        System.Windows.Media.PixelFormats.Bgra32, null);
                }
                
                PreviewBitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, width, height),
                    bgra, width * 4, 0);
            });
        }
        catch { }
    }
    
    public async Task<bool> ConnectAsync()
    {
        if (_ownRtspService == null) return false;
        
        // Cancel any previous connection attempt
        _connectionCts?.Cancel();
        _connectionCts = new CancellationTokenSource();
        var ct = _connectionCts.Token;
        
        IsConnecting = true;
        UpdateStatus();
        
        try
        {
            var url = GeneratedUrl;
            OnLog?.Invoke($"🔗 {SlotName} connecting to: {url}");
            
            var success = await _ownRtspService.ConnectAsync(url, ct);
            
            if (ct.IsCancellationRequested)
            {
                IsConnecting = false;
                OnLog?.Invoke($"🚫 {SlotName} connection cancelled");
                UpdateStatus();
                return false;
            }
            
            IsConnecting = false;
            IsConnected = success;
            
            if (success)
            {
                Resolution = $"{_ownRtspService.Width}x{_ownRtspService.Height}";
                FrameRate = $"{_ownRtspService.FrameRate} fps";
                
                // Start preview capture
                int width = _ownRtspService.Width > 0 ? _ownRtspService.Width : 1280;
                int height = _ownRtspService.Height > 0 ? _ownRtspService.Height : 720;
                _ownRtspService.StartPreviewCapture(width, height);
                
                OnLog?.Invoke($"✅ {SlotName} connected: {Resolution}");
            }
            else
            {
                OnLog?.Invoke($"❌ {SlotName} connection failed");
            }
            
            UpdateStatus();
            return success;
        }
        catch (OperationCanceledException)
        {
            IsConnecting = false;
            OnLog?.Invoke($"🚫 {SlotName} connection cancelled");
            UpdateStatus();
            return false;
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            OnLog?.Invoke($"❌ {SlotName} error: {ex.Message}");
            UpdateStatus();
            return false;
        }
    }
    
    public async void CancelConnection()
    {
        if (IsConnecting)
        {
            OnLog?.Invoke($"🚫 {SlotName} cancelling connection...");
            
            // Cancel the token
            _connectionCts?.Cancel();
            
            // Force stop the media player
            if (_ownRtspService != null)
            {
                await _ownRtspService.DisconnectAsync();
            }
            
            IsConnecting = false;
            IsConnected = false;
            UpdateStatus();
            OnLog?.Invoke($"✅ {SlotName} connection cancelled");
        }
    }
    
    public async Task DisconnectAsync()
    {
        if (_ownRtspService == null) return;
        
        await _ownRtspService.DisconnectAsync();
        IsConnected = false;
        IsVirtualized = false;
        PreviewBitmap = null;
        UpdateStatus();
        OnLog?.Invoke($"⏹ {SlotName} disconnected");
    }
    
    public bool StartVirtualCamera()
    {
        try
        {
            if (_ownRtspService == null || !IsConnected)
            {
                OnLog?.Invoke($"❌ {SlotName}: Cannot start virtual camera - not connected");
                return false;
            }
            
            int width = _ownRtspService.Width > 0 ? _ownRtspService.Width : 1280;
            int height = _ownRtspService.Height > 0 ? _ownRtspService.Height : 720;
            int fps = _ownRtspService.FrameRate > 0 ? _ownRtspService.FrameRate : 30;
            
            OnLog?.Invoke($"═══════════════════════════════════════");
            OnLog?.Invoke($"📹 {SlotName} starting virtual camera...");
            OnLog?.Invoke($"📐 Resolution: {width}x{height} @ {fps}fps");
            OnLog?.Invoke($"═══════════════════════════════════════");
            
            // Use the RtspService's built-in StartVirtualCamera which handles
            // proper BGRA to NV12 conversion and frame capture
            bool started = _ownRtspService.StartVirtualCamera(width, height, fps);
            
            if (started)
            {
                IsVirtualized = true;
                UpdateStatus();
                OnLog?.Invoke($"✅ {SlotName} → OBS Virtual Camera");
                OnLog?.Invoke($"💡 Select 'OBS Virtual Camera' in your video app");
                OnLog?.Invoke($"ℹ️ RESTART video apps to see camera");
            }
            else
            {
                OnLog?.Invoke($"❌ Failed to start virtual camera for {SlotName}");
            }
            
            return started;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌❌❌ CRASH in StartVirtualCamera: {ex.Message}");
            OnLog?.Invoke($"Stack: {ex.StackTrace}");
            return false;
        }
    }
    
    public void StopVirtualCamera()
    {
        if (_ownRtspService != null)
        {
            _ownRtspService.StopVirtualCamera();
        }
        
        IsVirtualized = false;
        UpdateStatus();
        OnLog?.Invoke($"⏹ {SlotName} virtual camera stopped");
    }
    
    // PREVIEW controls - only affect preview display
    public void ApplyPreviewFlipHorizontal(bool value)
    {
        if (_ownRtspService == null) return;
        PreviewFlipHorizontal = value;
        _ownRtspService.PreviewFlipHorizontal = value;
        OnLog?.Invoke($"📺 {SlotName} Preview Flip H: {value}");
    }
    
    public void ApplyPreviewFlipVertical(bool value)
    {
        if (_ownRtspService == null) return;
        PreviewFlipVertical = value;
        _ownRtspService.PreviewFlipVertical = value;
        OnLog?.Invoke($"📺 {SlotName} Preview Flip V: {value}");
    }
    
    public void ApplyPreviewBrightness(int value)
    {
        if (_ownRtspService == null) return;
        PreviewBrightness = value;
        _ownRtspService.PreviewBrightness = value;
    }
    
    public void ApplyPreviewContrast(int value)
    {
        if (_ownRtspService == null) return;
        PreviewContrast = value;
        _ownRtspService.PreviewContrast = value;
    }
    
    // VIRTUAL OUTPUT controls - only affect virtual camera output
    public void ApplyVirtualFlipHorizontal(bool value)
    {
        if (_ownRtspService == null) return;
        VirtualFlipHorizontal = value;
        _ownRtspService.VirtualFlipHorizontal = value;
        OnLog?.Invoke($"📹 {SlotName} Virtual Flip H: {value}");
    }
    
    public void ApplyVirtualFlipVertical(bool value)
    {
        if (_ownRtspService == null) return;
        VirtualFlipVertical = value;
        _ownRtspService.VirtualFlipVertical = value;
        OnLog?.Invoke($"📹 {SlotName} Virtual Flip V: {value}");
    }
    
    public void ApplyVirtualBrightness(int value)
    {
        if (_ownRtspService == null) return;
        VirtualBrightness = value;
        _ownRtspService.VirtualBrightness = value;
    }
    
    public void ApplyVirtualContrast(int value)
    {
        if (_ownRtspService == null) return;
        VirtualContrast = value;
        _ownRtspService.VirtualContrast = value;
    }
    
    partial void OnIpAddressChanged(string value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnPortChanged(int value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnChannelChanged(int value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnSelectedBrandChanged(CameraBrand value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnSelectedStreamChanged(StreamType value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnManualUrlChanged(string value) => OnPropertyChanged(nameof(GeneratedUrl));
    partial void OnUseManualUrlChanged(bool value) => OnPropertyChanged(nameof(GeneratedUrl));
    
    public void UpdateStatus()
    {
        if (IsConnecting)
        {
            StatusText = "Connecting...";
            StatusIcon = "🟡";
        }
        else if (IsVirtualized)
        {
            StatusText = $"🔴 LIVE - {Resolution}";
            StatusIcon = "🔵";
        }
        else if (IsConnected)
        {
            StatusText = $"Connected - {Resolution}";
            StatusIcon = "🟢";
        }
        else if (!string.IsNullOrWhiteSpace(IpAddress) && IpAddress != "192.168.1.100")
        {
            StatusText = "Configured - Not connected";
            StatusIcon = "⚪";
        }
        else
        {
            StatusText = "Not configured";
            StatusIcon = "⚫";
        }
    }
    
    public void LoadFromProfile(CameraProfile profile)
    {
        IpAddress = profile.IpAddress;
        Port = profile.Port;
        Username = profile.Username;
        Password = profile.Password;
        PtzUsername = profile.PtzUsername;
        PtzPassword = profile.PtzPassword;
        SelectedBrand = profile.Brand;
        SelectedStream = profile.Stream;
        Channel = profile.Channel;
        UseManualUrl = profile.UseManualUrl;
        ManualUrl = profile.ManualUrl;
        
        UpdateStatus();
        Log.Information($"Slot {SlotIndex + 1}: Loaded profile for {IpAddress}");
    }
    
    public CameraProfile ToProfile(string name)
    {
        return new CameraProfile
        {
            Name = name,
            IpAddress = IpAddress,
            Port = Port,
            Username = Username,
            Password = Password,
            PtzUsername = PtzUsername,
            PtzPassword = PtzPassword,
            Brand = SelectedBrand,
            Stream = SelectedStream,
            Channel = Channel,
            UseManualUrl = UseManualUrl,
            ManualUrl = ManualUrl
        };
    }
}
