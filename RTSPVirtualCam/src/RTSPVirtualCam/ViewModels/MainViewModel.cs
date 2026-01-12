using System;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;
using Serilog;

namespace RTSPVirtualCam.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRtspService _rtspService;
    private readonly IVirtualCameraService _virtualCameraService;
    private readonly StringBuilder _logBuilder = new();
    
    // Connection fields
    [ObservableProperty]
    private string _ipAddress = "192.168.1.100";
    
    [ObservableProperty]
    private int _port = 554;
    
    [ObservableProperty]
    private string _username = "admin";
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private CameraBrand _selectedBrand = CameraBrand.Hikvision;
    
    [ObservableProperty]
    private StreamType _selectedStream = StreamType.MainStream;
    
    [ObservableProperty]
    private int _channel = 1;
    
    [ObservableProperty]
    private string _generatedUrl = string.Empty;
    
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
    private string _statusText = "Ready";
    
    [ObservableProperty]
    private string _statusIcon = "⚪";
    
    // Stream info
    [ObservableProperty]
    private string _resolution = "--";
    
    [ObservableProperty]
    private string _frameRate = "--";
    
    [ObservableProperty]
    private string _codec = "--";
    
    [ObservableProperty]
    private string _transport = "TCP";
    
    // Logs
    [ObservableProperty]
    private string _logText = string.Empty;
    
    // Windows version info
    [ObservableProperty]
    private bool _isWindows11OrLater;
    
    [ObservableProperty]
    private string _virtualCameraStatus = string.Empty;
    
    // Collections
    public ObservableCollection<CameraBrand> Brands { get; } = new(Enum.GetValues<CameraBrand>());
    public ObservableCollection<StreamType> Streams { get; } = new(Enum.GetValues<StreamType>());
    public ObservableCollection<int> Channels { get; } = new(Enumerable.Range(1, 16));
    public ObservableCollection<ConnectionInfo> UrlHistory { get; } = new();
    
    public MainViewModel(IRtspService rtspService, IVirtualCameraService virtualCameraService)
    {
        _rtspService = rtspService;
        _virtualCameraService = virtualCameraService;
        
        _rtspService.ConnectionStateChanged += OnConnectionStateChanged;
        _virtualCameraService.StateChanged += OnVirtualCameraStateChanged;
        
        // Initial log
        AddLog("Application started");
        AddLog($"Base directory: {AppContext.BaseDirectory}");
        
        // Check Windows version
        CheckWindowsVersion();
        
        // Generate initial URL
        UpdateGeneratedUrl();
    }
    
    private void AddLog(string message)
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
        AddLog("Log cleared");
    }
    
    private void CheckWindowsVersion()
    {
        var version = Environment.OSVersion.Version;
        IsWindows11OrLater = version.Major >= 10 && version.Build >= 22000;
        
        AddLog($"Windows version: {version.Major}.{version.Minor}.{version.Build}");
        
        if (IsWindows11OrLater)
        {
            VirtualCameraStatus = "✅ Windows 11 detected - Native virtual camera supported";
            AddLog("Windows 11+ detected - Virtual camera API available");
        }
        else
        {
            VirtualCameraStatus = "⚠️ Windows 10 - Virtual camera requires OBS Virtual Camera";
            AddLog("Windows 10 detected - Native virtual camera NOT available");
            AddLog("Tip: Install OBS Studio for virtual camera support on Windows 10");
        }
    }
    
    private void UpdateGeneratedUrl()
    {
        var connection = new CameraConnection
        {
            IpAddress = IpAddress,
            Port = Port,
            Username = Username,
            Password = Password,
            Brand = SelectedBrand,
            Stream = SelectedStream,
            Channel = Channel
        };
        
        GeneratedUrl = connection.GenerateRtspUrl();
    }
    
    public string CurrentUrl => UseManualUrl ? ManualUrl : GeneratedUrl;
    
    // Update URL when any field changes
    partial void OnIpAddressChanged(string value) => UpdateGeneratedUrl();
    partial void OnPortChanged(int value) => UpdateGeneratedUrl();
    partial void OnUsernameChanged(string value) => UpdateGeneratedUrl();
    partial void OnPasswordChanged(string value) => UpdateGeneratedUrl();
    partial void OnSelectedBrandChanged(CameraBrand value) { UpdateGeneratedUrl(); AddLog($"Brand changed to: {value}"); }
    partial void OnSelectedStreamChanged(StreamType value) { UpdateGeneratedUrl(); AddLog($"Stream type changed to: {value}"); }
    partial void OnChannelChanged(int value) { UpdateGeneratedUrl(); AddLog($"Channel changed to: {value}"); }
    
    partial void OnGeneratedUrlChanged(string value) => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnManualUrlChanged(string value) => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnUseManualUrlChanged(bool value) => PreviewCommand.NotifyCanExecuteChanged();
    
    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync()
    {
        var url = CurrentUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        
        AddLog($"Connecting to: {url}");
        AddLog("Transport: TCP, Network caching: 500ms");
        
        IsConnecting = true;
        StatusText = "Connecting...";
        StatusIcon = "🟡";
        
        try
        {
            var success = await _rtspService.ConnectAsync(url);
            
            IsConnecting = false;
            
            if (success)
            {
                AddLog("✅ Connection successful!");
                UpdateStreamInfo();
            }
            else
            {
                AddLog("❌ Connection failed - Check URL, credentials, and network");
                AddLog("Tip: Test the URL in VLC first to verify it works");
            }
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            AddLog($"❌ Exception: {ex.Message}");
            if (ex.InnerException != null)
            {
                AddLog($"   Inner: {ex.InnerException.Message}");
            }
            StatusText = $"Error: {ex.Message}";
            StatusIcon = "🔴";
        }
    }
    
    private bool CanPreview() => !IsConnecting && !IsConnected && !string.IsNullOrWhiteSpace(CurrentUrl);
    
    [RelayCommand(CanExecute = nameof(CanVirtualize))]
    private async Task VirtualizeAsync()
    {
        if (!IsConnected)
        {
            AddLog("❌ Cannot virtualize - not connected to stream");
            return;
        }
        
        if (!IsWindows11OrLater)
        {
            AddLog("❌ Windows 11 required for native virtual camera");
            AddLog("💡 For Windows 10, install OBS Studio and use OBS Virtual Camera");
            AddLog("   1. Install OBS Studio from https://obsproject.com");
            AddLog("   2. Start Virtual Camera in OBS");
            AddLog("   3. The RTSP preview will work, use OBS for virtualization");
            StatusText = "Virtual camera requires Windows 11 or OBS";
            return;
        }
        
        AddLog("Starting virtual camera...");
        
        try
        {
            var success = await _virtualCameraService.StartAsync(
                "RTSP VirtualCam",
                _rtspService.Width,
                _rtspService.Height,
                _rtspService.FrameRate
            );
            
            if (success)
            {
                AddLog("✅ Virtual camera started successfully");
                StatusText = "Virtual Camera Active";
                StatusIcon = "🔵";
                AddToHistory(CurrentUrl);
            }
            else
            {
                AddLog("❌ Failed to start virtual camera");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Virtual camera error: {ex.Message}");
        }
    }
    
    private bool CanVirtualize() => IsConnected && !IsVirtualized;
    
    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        AddLog("Stopping...");
        
        try
        {
            if (IsVirtualized)
            {
                await _virtualCameraService.StopAsync();
                AddLog("Virtual camera stopped");
            }
            
            if (IsConnected)
            {
                await _rtspService.DisconnectAsync();
                AddLog("Stream disconnected");
            }
            
            ClearStreamInfo();
        }
        catch (Exception ex)
        {
            AddLog($"❌ Stop error: {ex.Message}");
        }
    }
    
    private bool CanStop() => IsConnected || IsVirtualized;
    
    [RelayCommand]
    private void CopyUrl()
    {
        try
        {
            System.Windows.Clipboard.SetText(CurrentUrl);
            AddLog("URL copied to clipboard");
            StatusText = "URL copied";
        }
        catch (Exception ex)
        {
            AddLog($"Failed to copy: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void TestInVlc()
    {
        try
        {
            var vlcPaths = new[]
            {
                @"C:\Program Files\VideoLAN\VLC\vlc.exe",
                @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe"
            };
            
            var vlcPath = vlcPaths.FirstOrDefault(System.IO.File.Exists);
            
            if (vlcPath != null)
            {
                AddLog($"Opening VLC: {CurrentUrl}");
                System.Diagnostics.Process.Start(vlcPath, $"\"{CurrentUrl}\"");
                StatusText = "Opening in VLC...";
            }
            else
            {
                AddLog("❌ VLC not found. Install VLC to test RTSP streams.");
                StatusText = "VLC not installed";
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ VLC error: {ex.Message}");
        }
    }
    
    private void OnConnectionStateChanged(object? sender, RtspConnectionEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = e.IsConnected;
            
            if (e.IsConnected)
            {
                StatusText = "Connected";
                StatusIcon = "🟢";
            }
            else
            {
                StatusText = e.ErrorMessage ?? "Disconnected";
                StatusIcon = "⚪";
                
                if (!string.IsNullOrEmpty(e.ErrorMessage))
                {
                    AddLog($"❌ {e.ErrorMessage}");
                }
            }
            
            PreviewCommand.NotifyCanExecuteChanged();
            VirtualizeCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
    }
    
    private void OnVirtualCameraStateChanged(object? sender, VirtualCameraEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsVirtualized = e.IsActive;
            
            if (e.IsActive)
            {
                StatusText = "Virtual Camera Active";
                StatusIcon = "🔵";
            }
            else if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                AddLog($"❌ {e.ErrorMessage}");
            }
            
            VirtualizeCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
    }
    
    private void UpdateStreamInfo()
    {
        Resolution = $"{_rtspService.Width}x{_rtspService.Height}";
        FrameRate = $"{_rtspService.FrameRate} fps";
        Codec = _rtspService.Codec ?? "--";
        AddLog($"Stream info: {Resolution}, {FrameRate}, Codec: {Codec}");
    }
    
    private void ClearStreamInfo()
    {
        Resolution = "--";
        FrameRate = "--";
        Codec = "--";
        StatusText = "Disconnected";
        StatusIcon = "⚪";
    }
    
    private void AddToHistory(string url)
    {
        var existing = UrlHistory.FirstOrDefault(h => h.RtspUrl == url);
        if (existing != null)
        {
            UrlHistory.Remove(existing);
        }
        
        UrlHistory.Insert(0, new ConnectionInfo 
        { 
            RtspUrl = url, 
            LastUsed = DateTime.UtcNow 
        });
        
        while (UrlHistory.Count > 10)
        {
            UrlHistory.RemoveAt(UrlHistory.Count - 1);
        }
    }
}
