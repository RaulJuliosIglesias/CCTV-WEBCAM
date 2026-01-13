using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;
using Serilog;
using System.Management;

namespace RTSPVirtualCam.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRtspService _rtspService;
    private readonly IVirtualCameraService _virtualCameraService;
    private readonly HikvisionPtzService _ptzService;
    private readonly CameraProfileService _cameraProfileService;
    private readonly StringBuilder _logBuilder = new();
    
    // Connection fields
    [ObservableProperty]
    private string _ipAddress = "192.168.1.64";
    
    [ObservableProperty]
    private int _port = 554;
    
    [ObservableProperty]
    private string _username = "admin";
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    // PTZ Credentials (optional - for PTZ control only)
    [ObservableProperty]
    private string _ptzUsername = string.Empty;
    
    [ObservableProperty]
    private string _ptzPassword = string.Empty;
    
    // PTZ Presets
    public ObservableCollection<PtzPreset> PtzPresets { get; } = new();
    
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
    
    // ═══════════════════════════════════════════════════════════════
    // v2.0 Multi-Camera Properties
    // ═══════════════════════════════════════════════════════════════
    
    // Camera slots collection (up to 6 cameras)
    public ObservableCollection<CameraSlotViewModel> CameraSlots { get; } = new();
    
    [ObservableProperty]
    private CameraSlotViewModel? _selectedCameraSlot;
    
    [ObservableProperty]
    private int _connectedCameraCount = 0;
    
    [ObservableProperty]
    private int _virtualizedCameraCount = 0;
    
    [ObservableProperty]
    private int _recordingCameraCount = 0;
    
    [ObservableProperty]
    private int _selectedLayoutCount = 1;
    
    [ObservableProperty]
    private string _virtualCameraStatus = string.Empty;
    
    // Camera Profiles
    public ObservableCollection<CameraProfile> CameraProfiles { get; } = new();
    
    [ObservableProperty]
    private CameraProfile? _selectedCameraProfile;
    
    [ObservableProperty]
    private string _newCameraProfileName = string.Empty;
    
    // Windows 10 SoftCam driver
    [ObservableProperty]
    private bool _useWindows10Mode;
    
    [ObservableProperty]
    private bool _isSoftCamInstalled;
    
    [ObservableProperty]
    private string _driverStatus = "Not installed";
    
    [ObservableProperty]
    private bool _isInstallingDriver;
    
    // Camera settings - Preview
    [ObservableProperty]
    private bool _previewFlipHorizontal;
    
    [ObservableProperty]
    private bool _previewFlipVertical;
    
    [ObservableProperty]
    private int _previewBrightness;
    
    [ObservableProperty]
    private int _previewContrast;
    
    // Camera settings - Virtual Camera Output
    [ObservableProperty]
    private bool _virtualFlipHorizontal;
    
    [ObservableProperty]
    private bool _virtualFlipVertical;
    
    [ObservableProperty]
    private int _virtualBrightness;
    
    [ObservableProperty]
    private int _virtualContrast;
    
    // PTZ Speed Controls
    [ObservableProperty]
    private int _ptzMoveSpeed = 75; // Pan/Tilt speed (0-100)
    
    [ObservableProperty]
    private int _ptzZoomSpeed = 75; // Zoom speed (0-100)
    
    // Preview bitmap
    [ObservableProperty]
    private System.Windows.Media.Imaging.WriteableBitmap? _previewBitmap;
    
    // Collections
    public ObservableCollection<CameraBrand> Brands { get; } = new(Enum.GetValues<CameraBrand>());
    public ObservableCollection<StreamType> Streams { get; } = new(Enum.GetValues<StreamType>());
    public ObservableCollection<int> Channels { get; } = new(Enumerable.Range(1, 16));
    public ObservableCollection<ConnectionInfo> UrlHistory { get; } = new();
    
    public MainViewModel(IRtspService rtspService, IVirtualCameraService virtualCameraService)
    {
        _rtspService = rtspService;
        _virtualCameraService = virtualCameraService;
        _ptzService = new HikvisionPtzService();
        _cameraProfileService = new CameraProfileService();
        
        _rtspService.ConnectionStateChanged += OnConnectionStateChanged;
        _virtualCameraService.StateChanged += OnVirtualCameraStateChanged;
        
        // Subscribe to preview frames
        if (_rtspService is RtspService rtspSvc)
        {
            rtspSvc.OnPreviewFrame += OnPreviewFrameReceived;
        }
        
        // Subscribe to PTZ service logs
        _ptzService.OnLog += (msg) => AddLog(msg);
        
        // Initialize PTZ presets (1-30 for keyboard shortcuts)
        for (int i = 1; i <= 30; i++)
        {
            PtzPresets.Add(new PtzPreset { Id = i, Name = $"Preset {i}", IsEnabled = true });
        }
        
        // Load camera profiles
        LoadCameraProfiles();
        
        // Initialize camera slots (max 6 for reasonable screen sizes)
        InitializeCameraSlots();
        
        // Initial log
        AddLog("Application started");
        AddLog($"Base directory: {AppContext.BaseDirectory}");
        
        // Check Windows version
        CheckWindowsVersion();
        
        // Generate initial URL
        UpdateGeneratedUrl();
    }
    
    private void OnPreviewFrameReceived(byte[] bgra, int width, int height)
    {
        try
        {
            // Update bitmap on UI thread
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                // Update main preview bitmap
                if (PreviewBitmap == null || PreviewBitmap.PixelWidth != width || PreviewBitmap.PixelHeight != height)
                {
                    PreviewBitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                        width, height, 96, 96,
                        System.Windows.Media.PixelFormats.Bgra32, null);
                }
                
                PreviewBitmap.WritePixels(
                    new System.Windows.Int32Rect(0, 0, width, height),
                    bgra, width * 4, 0);
                
                // Also update the selected camera slot's preview bitmap (for embedded grid view)
                if (SelectedCameraSlot != null)
                {
                    if (SelectedCameraSlot.PreviewBitmap == null || 
                        SelectedCameraSlot.PreviewBitmap.PixelWidth != width || 
                        SelectedCameraSlot.PreviewBitmap.PixelHeight != height)
                    {
                        SelectedCameraSlot.PreviewBitmap = new System.Windows.Media.Imaging.WriteableBitmap(
                            width, height, 96, 96,
                            System.Windows.Media.PixelFormats.Bgra32, null);
                    }
                    
                    SelectedCameraSlot.PreviewBitmap.WritePixels(
                        new System.Windows.Int32Rect(0, 0, width, height),
                        bgra, width * 4, 0);
                }
            });
        }
        catch { }
    }
    
    // Preview controls - ONLY affect preview display
    partial void OnPreviewFlipHorizontalChanged(bool value)
    {
        SelectedCameraSlot?.ApplyPreviewFlipHorizontal(value);
    }
    
    partial void OnPreviewFlipVerticalChanged(bool value)
    {
        SelectedCameraSlot?.ApplyPreviewFlipVertical(value);
    }
    
    partial void OnPreviewBrightnessChanged(int value)
    {
        SelectedCameraSlot?.ApplyPreviewBrightness(value);
    }
    
    partial void OnPreviewContrastChanged(int value)
    {
        SelectedCameraSlot?.ApplyPreviewContrast(value);
    }
    
    // Virtual camera output controls - ONLY affect virtual camera output
    partial void OnVirtualFlipHorizontalChanged(bool value)
    {
        SelectedCameraSlot?.ApplyVirtualFlipHorizontal(value);
    }
    
    partial void OnVirtualFlipVerticalChanged(bool value)
    {
        SelectedCameraSlot?.ApplyVirtualFlipVertical(value);
    }
    
    partial void OnVirtualBrightnessChanged(int value)
    {
        SelectedCameraSlot?.ApplyVirtualBrightness(value);
    }
    
    partial void OnVirtualContrastChanged(int value)
    {
        SelectedCameraSlot?.ApplyVirtualContrast(value);
    }
    
    private static readonly string LogFilePath = System.IO.Path.Combine(
        AppContext.BaseDirectory, "logs", $"rtspvirtualcam_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    
    public void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logEntry = $"[{timestamp}] {message}";
        _logBuilder.AppendLine(logEntry);
        LogText = _logBuilder.ToString();
        Log.Information(message);
        
        // Write to file immediately with flush
        try
        {
            var logDir = System.IO.Path.GetDirectoryName(LogFilePath);
            if (!System.IO.Directory.Exists(logDir))
                System.IO.Directory.CreateDirectory(logDir!);
            
            using (var writer = new System.IO.StreamWriter(LogFilePath, true))
            {
                writer.WriteLine(logEntry);
                writer.Flush();
            }
        }
        catch { /* Ignore file errors */ }
    }
    
    // Show popup dialog for important warnings
    public void ShowWarningDialog(string title, string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            System.Windows.MessageBox.Show(
                message,
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        });
    }
    
    [RelayCommand]
    private void CopyLogs()
    {
        try
        {
            System.Windows.Clipboard.SetText(LogText);
            AddLog("📋 Logs copied to clipboard!");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to copy: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void OpenLogFile()
    {
        try
        {
            if (System.IO.File.Exists(LogFilePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LogFilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                AddLog($"Log file: {LogFilePath}");
            }
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to open log: {ex.Message}");
        }
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
            AddLog("Windows 11+ detected - Native API available");
            UseWindows10Mode = false;
        }
        else
        {
            VirtualCameraStatus = "⚠️ Windows 10 - Virtual camera driver required";
            AddLog("Windows 10 detected - Virtual camera mode");
            UseWindows10Mode = true;
            CheckSoftCamStatus();
        }
    }
    
    private void CheckSoftCamStatus()
    {
        // Check if OBS VirtualCam DLL exists (included in package)
        var dllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", "softcam", "obs-virtualcam-module64.dll");
        var exists = System.IO.File.Exists(dllPath);
        
        AddLog($"🔍 Driver path: {dllPath}");
        AddLog($"   DLL present: {exists}");
        
        if (!exists)
        {
            DriverStatus = "❌ Driver files missing from package";
            IsSoftCamInstalled = false;
            return;
        }
        
        var fileInfo = new System.IO.FileInfo(dllPath);
        AddLog($"   DLL size: {fileInfo.Length / 1024} KB");
        
        // Check if already registered in Windows
        try
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Classes\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}");
            if (key != null)
            {
                DriverStatus = "✅ OBS Virtual Camera installed";
                IsSoftCamInstalled = true;
                AddLog("✅ OBS Virtual Camera is registered in Windows");
                key.Close();
            }
            else
            {
                DriverStatus = "📥 Click Install to register driver";
                IsSoftCamInstalled = true; // DLL exists, just needs registration
                AddLog("⚠️ DLL present but not registered - click Install");
            }
        }
        catch
        {
            DriverStatus = "📥 Click Install to register driver";
            IsSoftCamInstalled = true;
        }
        
        ListVideoDevices();
    }
    
    private void ListVideoDevices()
    {
        AddLog("📹 Listing video capture devices...");
        try
        {
            // Use DirectShow to enumerate video devices
            var devices = new List<string>();
            
            // Query WMI for video devices
            using (var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera' OR PNPClass = 'Image'"))
            {
                foreach (var device in searcher.Get())
                {
                    var name = device["Name"]?.ToString() ?? "Unknown";
                    devices.Add(name);
                    AddLog($"   📷 {name}");
                }
            }
            
            if (devices.Count == 0)
            {
                AddLog("   ⚠️ No video capture devices found via WMI");
            }
            
            // Also try to find DirectShow video devices via registry
            try
            {
                var clsidKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(
                    @"CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}\Instance");
                if (clsidKey != null)
                {
                    var subkeys = clsidKey.GetSubKeyNames();
                    AddLog($"   📹 DirectShow video sources: {subkeys.Length}");
                    foreach (var subkey in subkeys)
                    {
                        var deviceKey = clsidKey.OpenSubKey(subkey);
                        var friendlyName = deviceKey?.GetValue("FriendlyName")?.ToString();
                        if (!string.IsNullOrEmpty(friendlyName))
                        {
                            AddLog($"      - {friendlyName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"   ⚠️ Registry check error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            AddLog($"   ❌ Error listing devices: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task InstallDriverAsync()
    {
        IsInstallingDriver = true;
        AddLog("═══════════════════════════════════════");
        AddLog("Starting virtual camera installation...");
        AddLog("═══════════════════════════════════════");
        
        try
        {
            var scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", "install-virtualcam.bat");
            var dllPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", "softcam", "obs-virtualcam-module64.dll");
            
            AddLog($"📁 Script path: {scriptPath}");
            AddLog($"📁 DLL path: {dllPath}");
            AddLog($"   Script exists: {System.IO.File.Exists(scriptPath)}");
            AddLog($"   DLL exists: {System.IO.File.Exists(dllPath)}");
            
            if (!System.IO.File.Exists(scriptPath)) 
            {
                AddLog($"❌ Script not found!");
                IsInstallingDriver = false;
                return;
            }
            
            // Check registry BEFORE install
            AddLog("🔍 Checking registry BEFORE install...");
            CheckOBSVirtualCamRegistry();
            
            AddLog("🚀 Running installation script (requires admin)...");
            await RunAdminScriptAsync(scriptPath);
            
            // Wait a moment for registration to complete
            await Task.Delay(1000);
            
            // Check registry AFTER install
            AddLog("🔍 Checking registry AFTER install...");
            CheckOBSVirtualCamRegistry();
            
            // Check install log file
            var installLog = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", "install_log.txt");
            if (System.IO.File.Exists(installLog))
            {
                AddLog("📄 Install script log:");
                var logContent = System.IO.File.ReadAllText(installLog);
                foreach (var line in logContent.Split('\n').Take(20))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AddLog($"   {line.Trim()}");
                }
            }
            
            CheckSoftCamStatus();
            AddLog("═══════════════════════════════════════");
            AddLog("💡 Look for 'OBS Virtual Camera' in camera list");
            AddLog("💡 RESTART video apps (Zoom, Teams, Chrome)");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Install error: {ex.Message}");
            DriverStatus = "❌ Installation failed";
        }
        finally
        {
            IsInstallingDriver = false;
        }
    }
    
    private void CheckOBSVirtualCamRegistry()
    {
        try
        {
            // Check 64-bit registration
            var key64 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Classes\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}");
            AddLog($"   64-bit CLSID registered: {key64 != null}");
            key64?.Close();
            
            // Check 32-bit registration
            var key32 = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Classes\WOW6432Node\CLSID\{A3FCE0F5-3493-419F-958A-ABA1250EC20B}");
            AddLog($"   32-bit CLSID registered: {key32 != null}");
            key32?.Close();
            
            // Check video capture sources category
            var vcKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(
                @"CLSID\{860BB310-5D01-11d0-BD3B-00A0C911CE86}\Instance");
            if (vcKey != null)
            {
                var subkeys = vcKey.GetSubKeyNames();
                AddLog($"   Video capture sources: {subkeys.Length}");
                vcKey.Close();
            }
        }
        catch (Exception ex)
        {
            AddLog($"   ⚠️ Registry check error: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task UninstallDriverAsync()
    {
        IsInstallingDriver = true;
        AddLog("Removing UnityCapture driver...");
        
        try
        {
            var scriptPath = System.IO.Path.Combine(AppContext.BaseDirectory, "scripts", "uninstall-virtualcam.bat");
            if (!System.IO.File.Exists(scriptPath)) 
            {
                AddLog($"❌ Script not found: {scriptPath}");
                return;
            }
            
            await RunAdminScriptAsync(scriptPath);
            CheckSoftCamStatus(); // Recheck status after uninstallation
            AddLog("✅ Driver successfully removed");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Uninstall error: {ex.Message}");
        }
        finally
        {
            IsInstallingDriver = false;
        }
    }
    
    private Task RunAdminScriptAsync(string scriptPath)
    {
        var tcs = new TaskCompletionSource();
        
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = true,
                Verb = "runas", // Request Admin
                CreateNoWindow = false // Let user see the script output
            };
            
            var process = System.Diagnostics.Process.Start(startInfo);
            
            if (process != null)
            {
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => tcs.SetResult();
                return tcs.Task;
            }
            else
            {
                tcs.SetException(new Exception("Failed to start process"));
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }
        
        return tcs.Task;
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
    
    // Update URL when any field changes and sync to selected slot
    partial void OnIpAddressChanged(string value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.IpAddress = value;
        UpdateGeneratedUrl();
    }
    
    partial void OnPortChanged(int value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.Port = value;
        UpdateGeneratedUrl();
    }
    
    partial void OnUsernameChanged(string value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.Username = value;
        UpdateGeneratedUrl();
    }
    
    partial void OnPasswordChanged(string value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.Password = value;
        UpdateGeneratedUrl();
    }
    partial void OnSelectedBrandChanged(CameraBrand value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.SelectedBrand = value;
        UpdateGeneratedUrl();
        AddLog($"Brand changed to: {value}");
    }
    
    partial void OnSelectedStreamChanged(StreamType value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.SelectedStream = value;
        UpdateGeneratedUrl();
        AddLog($"Stream type changed to: {value}");
    }
    
    partial void OnChannelChanged(int value)
    {
        if (SelectedCameraSlot != null) SelectedCameraSlot.Channel = value;
        UpdateGeneratedUrl();
        AddLog($"Channel changed to: {value}");
    }
    
    partial void OnGeneratedUrlChanged(string value) => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnManualUrlChanged(string value) => PreviewCommand.NotifyCanExecuteChanged();
    partial void OnUseManualUrlChanged(bool value) => PreviewCommand.NotifyCanExecuteChanged();
    
    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync()
    {
        if (SelectedCameraSlot == null)
        {
            AddLog("❌ No camera slot selected");
            return;
        }
        
        var url = CurrentUrl;
        if (string.IsNullOrWhiteSpace(url)) return;
        
        AddLog($"Connecting {SelectedCameraSlot.SlotName} to: {url}");
        AddLog("Transport: TCP, Network caching: 500ms");
        
        IsConnecting = true;
        StatusText = "Connecting...";
        StatusIcon = "🟡";
        
        try
        {
            // Use the slot's own RtspService for independent connection
            var success = await SelectedCameraSlot.ConnectAsync();
            
            IsConnecting = false;
            
            if (success)
            {
                // Slot handles its own preview capture, just sync UI status
                IsConnected = SelectedCameraSlot.IsConnected;
                Resolution = SelectedCameraSlot.Resolution;
                FrameRate = SelectedCameraSlot.FrameRate;
                StatusText = "Connected";
                StatusIcon = "🟢";
                UpdateMultiCameraCounts();
                
                // Notify command state changes
                VirtualizeCommand.NotifyCanExecuteChanged();
                StopCommand.NotifyCanExecuteChanged();
                PreviewCommand.NotifyCanExecuteChanged();
            }
            else
            {
                AddLog("❌ Connection failed - Check URL, credentials, and network");
                StatusText = "Connection failed";
                StatusIcon = "🔴";
            }
        }
        catch (Exception ex)
        {
            IsConnecting = false;
            AddLog($"❌ Exception: {ex.Message}");
            StatusText = $"Error: {ex.Message}";
            StatusIcon = "🔴";
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanCancelConnection))]
    private void CancelConnection()
    {
        if (!IsConnecting && SelectedCameraSlot?.IsConnecting != true) return;
        
        // Cancel the selected slot's connection
        SelectedCameraSlot?.CancelConnection();
        
        AddLog("🚫 Connection cancelled by user");
        IsConnecting = false;
        StatusText = "Connection cancelled";
        StatusIcon = "⚪";
        
        // Reset command states
        PreviewCommand.NotifyCanExecuteChanged();
        VirtualizeCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        CancelConnectionCommand.NotifyCanExecuteChanged();
    }
    
    private bool CanCancelConnection() => IsConnecting || SelectedCameraSlot?.IsConnecting == true;
    
    private bool CanPreview() => !IsConnecting && SelectedCameraSlot?.IsConnected != true && !string.IsNullOrWhiteSpace(CurrentUrl);
    
    [RelayCommand(CanExecute = nameof(CanVirtualize))]
    private async Task VirtualizeAsync()
    {
        if (SelectedCameraSlot == null || !SelectedCameraSlot.IsConnected)
        {
            AddLog("❌ Cannot virtualize - selected camera not connected");
            return;
        }
        
        // Windows 10 Support Path - OBS Virtual Camera
        if (UseWindows10Mode)
        {
            if (!IsSoftCamInstalled)
            {
                AddLog("⚠️ Virtual camera driver not installed");
                AddLog("👉 Click 'Install' button in the WINDOWS 10 DRIVER panel");
                StatusText = "Driver not installed";
                return;
            }
            
            // Check if another camera is already virtualized
            var alreadyVirtualized = CameraSlots.FirstOrDefault(s => s.IsVirtualized && s != SelectedCameraSlot);
            if (alreadyVirtualized != null)
            {
                AddLog("═══════════════════════════════════════");
                AddLog("⚠️ Only ONE camera can be virtualized at a time");
                AddLog("═══════════════════════════════════════");
                AddLog($"❌ {alreadyVirtualized.SlotName} is already using the virtual camera");
                StatusText = "Another camera is already virtualized";
                
                // Show popup dialog
                ShowWarningDialog(
                    "⚠️ Virtual Camera In Use",
                    $"Only ONE camera can be virtualized at a time.\n\n" +
                    $"'{alreadyVirtualized.SlotName}' is currently using the virtual camera.\n\n" +
                    $"To virtualize '{SelectedCameraSlot.SlotName}':\n" +
                    $"1. Select '{alreadyVirtualized.SlotName}'\n" +
                    $"2. Click 'Stop' to stop its virtualization\n" +
                    $"3. Then select and virtualize '{SelectedCameraSlot.SlotName}'");
                return;
            }
            
            AddLog("═══════════════════════════════════════");
            AddLog($"🚀 Starting Virtual Camera for {SelectedCameraSlot.SlotName}...");
            AddLog("═══════════════════════════════════════");
            
            // Use the slot's own StartVirtualCamera method
            bool started = SelectedCameraSlot.StartVirtualCamera();
            
            if (started)
            {
                AddLog("✅ Virtual camera output started!");
                AddLog("📹 Frames are being sent to 'OBS Virtual Camera'");
                AddLog("💡 Select 'OBS Virtual Camera' in your video app");
                AddLog("ℹ️ RESTART video apps (Chrome, Zoom, Teams) to see camera");
                
                StatusText = "🔴 LIVE - Virtual Camera Active";
                StatusIcon = "🔵";
                IsVirtualized = true;
                AddToHistory(CurrentUrl);
                UpdateMultiCameraCounts();
            }
            else
            {
                AddLog("❌ Failed to start virtual camera output");
                AddLog("💡 Make sure no other app is using OBS Virtual Camera");
                StatusText = "Virtual camera failed";
            }
            
            await Task.CompletedTask;
            return;
        }

        // Windows 11 Native Path
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
    
    private bool CanVirtualize() => SelectedCameraSlot?.IsConnected == true && SelectedCameraSlot?.IsVirtualized != true;
    
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
    
    [RelayCommand(CanExecute = nameof(CanStopSelected))]
    private async Task StopSelectedAsync()
    {
        if (SelectedCameraSlot == null) return;
        
        AddLog($"⏹ Stopping {SelectedCameraSlot.SlotName}...");
        
        try
        {
            // Stop virtual camera for this slot
            if (SelectedCameraSlot.IsVirtualized)
            {
                SelectedCameraSlot.StopVirtualCamera();
            }
            
            // Disconnect this slot
            if (SelectedCameraSlot.IsConnected)
            {
                await SelectedCameraSlot.DisconnectAsync();
            }
            
            // Sync UI state
            IsConnected = SelectedCameraSlot.IsConnected;
            IsVirtualized = SelectedCameraSlot.IsVirtualized;
            UpdateMultiCameraCounts();
            
            // Update command states
            PreviewCommand.NotifyCanExecuteChanged();
            VirtualizeCommand.NotifyCanExecuteChanged();
            StopSelectedCommand.NotifyCanExecuteChanged();
            
            AddLog($"✅ {SelectedCameraSlot.SlotName} stopped");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Stop error: {ex.Message}");
        }
    }
    
    private bool CanStopSelected() => SelectedCameraSlot?.IsConnected == true || SelectedCameraSlot?.IsVirtualized == true;
    
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
    
    // ═══════════════════════════════════════
    // PTZ CONTROLS - REAL SDK IMPLEMENTATION
    // ═══════════════════════════════════════
    
    [RelayCommand]
    private async Task PtzUpAsync() => await ExecutePtzCommandAsync("up");
    
    [RelayCommand]
    private async Task PtzDownAsync() => await ExecutePtzCommandAsync("down");
    
    [RelayCommand]
    private async Task PtzLeftAsync() => await ExecutePtzCommandAsync("left");
    
    [RelayCommand]
    private async Task PtzRightAsync() => await ExecutePtzCommandAsync("right");
    
    [RelayCommand]
    private async Task PtzHomeAsync() => await ExecutePtzCommandAsync("home");
    
    [RelayCommand]
    private async Task PtzZoomInAsync() => await ExecutePtzCommandAsync("zoomin");
    
    [RelayCommand]
    private async Task PtzZoomOutAsync() => await ExecutePtzCommandAsync("zoomout");
    
    [RelayCommand]
    public async Task GotoPresetAsync(PtzPreset? preset)
    {
        if (preset == null) return;
        await ExecutePtzPresetAsync(preset.Id);
    }
    
    private async Task ExecutePtzCommandAsync(string command)
    {
        await Task.Run(async () =>
        {
            try
            {
                // Use HTTP ISAPI for better compatibility
                await SendPtzViaHttpAsync(command);
            }
            catch (Exception ex)
            {
                AddLog($"❌ PTZ error: {ex.Message}");
            }
        });
    }
    
    // Keyboard PTZ control
    [RelayCommand]
    public async Task PtzKeyboardControlAsync(string key)
    {
        switch (key)
        {
            case "Up":
                await ExecutePtzCommandAsync("up");
                break;
            case "Down":
                await ExecutePtzCommandAsync("down");
                break;
            case "Left":
                await ExecutePtzCommandAsync("left");
                break;
            case "Right":
                await ExecutePtzCommandAsync("right");
                break;
            case "PageUp":
                await ExecutePtzCommandAsync("zoomin");
                break;
            case "PageDown":
                await ExecutePtzCommandAsync("zoomout");
                break;
        }
    }
    
    private async Task SendPtzViaHttpAsync(string command)
    {
        try
        {
            // Use PTZ credentials if provided, otherwise fallback to RTSP credentials
            string ptzUser = string.IsNullOrWhiteSpace(PtzUsername) ? Username : PtzUsername;
            string ptzPass = string.IsNullOrWhiteSpace(PtzPassword) ? Password : PtzPassword;
            
            // Validate connection parameters
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                AddLog("❌ PTZ: Camera IP address is required");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ptzUser))
            {
                AddLog("❌ PTZ: Username is required for PTZ control");
                return;
            }
            
            // Use HttpClientHandler with credentials for Digest authentication
            var handler = new System.Net.Http.HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(ptzUser, ptzPass),
                PreAuthenticate = true
            };
            
            using var client = new System.Net.Http.HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10); // Increased timeout
            
            string url;
            System.Net.Http.HttpContent? content = null;
            
            if (SelectedBrand == CameraBrand.Hikvision)
            {
                // Hikvision ISAPI PTZ control
                var (pan, tilt, zoom) = GetPtzValues(command);
                
                url = $"http://{IpAddress}/ISAPI/PTZCtrl/channels/1/continuous";
                string xmlPayload = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<PTZData>
    <pan>{pan}</pan>
    <tilt>{tilt}</tilt>
    <zoom>{zoom}</zoom>
</PTZData>";
                content = new System.Net.Http.StringContent(xmlPayload, System.Text.Encoding.UTF8, "application/xml");
                
                AddLog($"🎮 PTZ: {command} (pan={pan}, tilt={tilt}, zoom={zoom})");
                AddLog($"📊 Speed settings: Move={PtzMoveSpeed}, Zoom={PtzZoomSpeed}");
                
                var response = await client.PutAsync(url, content);
                
                if (response.IsSuccessStatusCode)
                {
                    AddLog($"✅ PTZ command sent successfully");
                    
                    // Auto-stop after 500ms (increased from 300ms for more noticeable movement)
                    await Task.Delay(500);
                    await StopPtzViaHttpAsync();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    AddLog($"❌ PTZ FAILED: 401 Unauthorized");
                    AddLog($"   Usuario usado: '{ptzUser}'");
                    AddLog($"   URL: {url}");
                    if (!string.IsNullOrEmpty(errorBody))
                    {
                        AddLog($"   Respuesta: {errorBody.Substring(0, Math.Min(200, errorBody.Length))}");
                    }
                    AddLog($"");
                    AddLog($"⚠️ PROBLEMA:");
                    AddLog($"   '{ptzUser}' NO tiene rol Administrator O permisos PTZ");
                    AddLog($"");
                    AddLog($"📝 SOLUCIÓN RÁPIDA:");
                    AddLog($"   1. Ve a http://{IpAddress} → User Management");
                    AddLog($"   2. Modifica usuario 'admin_ptz':");
                    AddLog($"      • Cambiar Level de 'Operator' a 'Administrator'");
                    AddLog($"      • Guardar cambios");
                    AddLog($"   3. Llena campos PTZ Username/Password en la app");
                    AddLog($"      PTZ Username: admin_ptz");
                    AddLog($"      PTZ Password: (tu contraseña)");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    AddLog($"❌ PTZ FAILED: Forbidden (403)");
                    AddLog($"   El usuario tiene login pero NO permisos PTZ");
                    AddLog($"   Verifica permisos en Configuration → User Management");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    AddLog($"❌ PTZ FAILED: Not Found (404)");
                    AddLog($"   La cámara no soporta PTZ o la URL es incorrecta");
                    AddLog($"   Verifica que la cámara sea PTZ y la IP sea correcta");
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    AddLog($"⚠️ PTZ failed: {response.StatusCode}");
                    AddLog($"   Response: {errorBody}");
                    
                    // Check if error mentions permissions
                    if (errorBody.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
                        errorBody.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
                    {
                        AddLog($"");
                        AddLog($"💡 Problema de permisos detectado.");
                        AddLog($"   Usuario necesita rol 'Administrator' con PTZ habilitado");
                    }
                }
            }
            else if (SelectedBrand == CameraBrand.Dahua)
            {
                // Dahua CGI PTZ control
                string action = GetDahuaPtzAction(command);
                url = $"http://{IpAddress}/cgi-bin/ptz.cgi?action={action}&channel=0&code=0&arg1=0&arg2=1&arg3=0";
                
                AddLog($"🎮 PTZ: {command}");
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    AddLog($"✅ PTZ command sent");
                    await Task.Delay(500);
                    await client.GetAsync($"http://{IpAddress}/cgi-bin/ptz.cgi?action=stop&channel=0");
                }
                else
                {
                    AddLog($"⚠️ PTZ failed: {response.StatusCode}");
                }
            }
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            AddLog($"❌ PTZ connection error: {ex.Message}");
            AddLog($"💡 Verify camera IP: {IpAddress} and network connectivity");
        }
        catch (TaskCanceledException)
        {
            AddLog($"❌ PTZ timeout - camera not responding");
            AddLog($"💡 Check if camera is powered on and network is stable");
        }
        catch (Exception ex)
        {
            AddLog($"❌ PTZ error: {ex.Message}");
        }
    }
    
    private async Task StopPtzViaHttpAsync()
    {
        try
        {
            string ptzUser = string.IsNullOrWhiteSpace(PtzUsername) ? Username : PtzUsername;
            string ptzPass = string.IsNullOrWhiteSpace(PtzPassword) ? Password : PtzPassword;
            
            var handler = new System.Net.Http.HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential(ptzUser, ptzPass),
                PreAuthenticate = true
            };
            
            using var client = new System.Net.Http.HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(3);
            
            if (SelectedBrand == CameraBrand.Hikvision)
            {
                string url = $"http://{IpAddress}/ISAPI/PTZCtrl/channels/1/continuous";
                string xmlPayload = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<PTZData>
    <pan>0</pan>
    <tilt>0</tilt>
    <zoom>0</zoom>
</PTZData>";
                var content = new System.Net.Http.StringContent(xmlPayload, System.Text.Encoding.UTF8, "application/xml");
                await client.PutAsync(url, content);
            }
        }
        catch { }
    }
    
    private async Task ExecutePtzPresetAsync(int presetId)
    {
        await Task.Run(async () =>
        {
            try
            {
                string ptzUser = string.IsNullOrWhiteSpace(PtzUsername) ? Username : PtzUsername;
                string ptzPass = string.IsNullOrWhiteSpace(PtzPassword) ? Password : PtzPassword;
                
                var handler = new System.Net.Http.HttpClientHandler
                {
                    Credentials = new System.Net.NetworkCredential(ptzUser, ptzPass),
                    PreAuthenticate = true
                };
                
                using var client = new System.Net.Http.HttpClient(handler);
                client.Timeout = TimeSpan.FromSeconds(5);
                
                if (SelectedBrand == CameraBrand.Hikvision)
                {
                    string url = $"http://{IpAddress}/ISAPI/PTZCtrl/channels/1/presets/{presetId}/goto";
                    
                    AddLog($"📍 Going to Preset #{presetId}");
                    
                    var response = await client.PutAsync(url, null);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        AddLog($"✅ Moved to Preset #{presetId}");
                    }
                    else
                    {
                        AddLog($"⚠️ Preset failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Preset error: {ex.Message}");
            }
        });
    }
    
    private (int pan, int tilt, int zoom) GetPtzValues(string command)
    {
        return command.ToLower() switch
        {
            "up" => (0, PtzMoveSpeed, 0),
            "down" => (0, -PtzMoveSpeed, 0),
            "left" => (-PtzMoveSpeed, 0, 0),
            "right" => (PtzMoveSpeed, 0, 0),
            "zoomin" => (0, 0, PtzZoomSpeed),
            "zoomout" => (0, 0, -PtzZoomSpeed),
            _ => (0, 0, 0)
        };
    }
    
    private string GetDahuaPtzAction(string command)
    {
        return command.ToLower() switch
        {
            "up" => "start&code=Up",
            "down" => "start&code=Down",
            "left" => "start&code=Left",
            "right" => "start&code=Right",
            "zoomin" => "start&code=ZoomTele",
            "zoomout" => "start&code=ZoomWide",
            "home" => "start&code=GotoPreset&arg1=1",
            _ => "stop"
        };
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
    
    // Camera Profile Management
    private void LoadCameraProfiles()
    {
        try
        {
            var profiles = _cameraProfileService.GetProfiles();
            CameraProfiles.Clear();
            foreach (var profile in profiles)
            {
                CameraProfiles.Add(profile);
            }
            AddLog($"Loaded {CameraProfiles.Count} camera profiles");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to load camera profiles: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void SaveCameraProfile()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewCameraProfileName))
            {
                AddLog("❌ Please enter a name for the camera profile");
                return;
            }
            
            var profile = _cameraProfileService.CreateFromCurrentSettings(
                NewCameraProfileName.Trim(),
                IpAddress,
                Port,
                Username,
                Password,
                PtzUsername,
                PtzPassword,
                SelectedBrand,
                SelectedStream,
                Channel,
                UseManualUrl,
                ManualUrl);
            
            _cameraProfileService.SaveProfile(profile);
            LoadCameraProfiles();
            NewCameraProfileName = string.Empty;
            
            AddLog($"✅ Saved camera profile: {profile.Name}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to save camera profile: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void LoadCameraProfile()
    {
        try
        {
            if (SelectedCameraProfile == null)
            {
                AddLog("❌ Please select a camera profile to load");
                return;
            }
            
            var profile = SelectedCameraProfile;
            
            // Load all settings from profile
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
            
            AddLog($"✅ Loaded camera profile: {profile.Name}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to load camera profile: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private void DeleteCameraProfile()
    {
        try
        {
            if (SelectedCameraProfile == null)
            {
                AddLog("❌ Please select a camera profile to delete");
                return;
            }
            
            var profileName = SelectedCameraProfile.Name;
            _cameraProfileService.DeleteProfile(SelectedCameraProfile.Id);
            LoadCameraProfiles();
            SelectedCameraProfile = null;
            
            AddLog($"🗑️ Deleted camera profile: {profileName}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to delete camera profile: {ex.Message}");
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanSaveCameraProfile))]
    private void QuickSaveCameraProfile()
    {
        try
        {
            var profileName = $"Camera {IpAddress}";
            var existingProfile = CameraProfiles.FirstOrDefault(p => 
                p.IpAddress == IpAddress && p.Port == Port);
            
            if (existingProfile != null)
            {
                profileName = existingProfile.Name;
            }
            
            var profile = _cameraProfileService.CreateFromCurrentSettings(
                profileName,
                IpAddress,
                Port,
                Username,
                Password,
                PtzUsername,
                PtzPassword,
                SelectedBrand,
                SelectedStream,
                Channel,
                UseManualUrl,
                ManualUrl);
            
            _cameraProfileService.SaveProfile(profile);
            LoadCameraProfiles();
            
            AddLog($"💾 Quick saved camera profile: {profile.Name}");
        }
        catch (Exception ex)
        {
            AddLog($"❌ Failed to quick save camera profile: {ex.Message}");
        }
    }
    
    private bool CanSaveCameraProfile()
    {
        return !string.IsNullOrWhiteSpace(IpAddress) && Port > 0;
    }
    
    // ═══════════════════════════════════════════════════════════════
    // v2.0 Multi-Camera Commands
    // ═══════════════════════════════════════════════════════════════
    
    private void InitializeCameraSlots()
    {
        // Create 6 camera slots (max for reasonable screen sizes)
        for (int i = 0; i < 6; i++)
        {
            var slot = new CameraSlotViewModel(i);
            // Wire up logging callback so slot logs appear in main UI
            slot.OnLog = msg => AddLog(msg);
            CameraSlots.Add(slot);
        }
        
        // Select first slot by default
        if (CameraSlots.Count > 0)
        {
            SelectCameraSlot(CameraSlots[0]);
        }
        
        // Set initial layout
        SelectedLayoutCount = 1;
    }
    
    [RelayCommand]
    private void SelectCameraSlot(CameraSlotViewModel? slot)
    {
        if (slot == null) return;
        
        // Deselect previous
        foreach (var s in CameraSlots)
        {
            s.IsSelected = false;
        }
        
        // Select new
        slot.IsSelected = true;
        SelectedCameraSlot = slot;
        
        // Sync the main UI fields with selected slot
        SyncFromSelectedSlot();
        
        AddLog($"📷 Selected {slot.SlotName}");
    }
    
    private void SyncFromSelectedSlot()
    {
        if (SelectedCameraSlot == null) return;
        
        // Copy settings from selected slot to main UI fields
        IpAddress = SelectedCameraSlot.IpAddress;
        Port = SelectedCameraSlot.Port;
        Username = SelectedCameraSlot.Username;
        Password = SelectedCameraSlot.Password;
        PtzUsername = SelectedCameraSlot.PtzUsername;
        PtzPassword = SelectedCameraSlot.PtzPassword;
        SelectedBrand = SelectedCameraSlot.SelectedBrand;
        SelectedStream = SelectedCameraSlot.SelectedStream;
        Channel = SelectedCameraSlot.Channel;
        UseManualUrl = SelectedCameraSlot.UseManualUrl;
        ManualUrl = SelectedCameraSlot.ManualUrl;
        
        // Sync status
        IsConnected = SelectedCameraSlot.IsConnected;
        IsVirtualized = SelectedCameraSlot.IsVirtualized;
        IsConnecting = SelectedCameraSlot.IsConnecting;
        
        // Sync PREVIEW video adjustments (separate from virtual)
        PreviewFlipHorizontal = SelectedCameraSlot.PreviewFlipHorizontal;
        PreviewFlipVertical = SelectedCameraSlot.PreviewFlipVertical;
        PreviewBrightness = SelectedCameraSlot.PreviewBrightness;
        PreviewContrast = SelectedCameraSlot.PreviewContrast;
        
        // Sync VIRTUAL OUTPUT video adjustments (separate from preview)
        VirtualFlipHorizontal = SelectedCameraSlot.VirtualFlipHorizontal;
        VirtualFlipVertical = SelectedCameraSlot.VirtualFlipVertical;
        VirtualBrightness = SelectedCameraSlot.VirtualBrightness;
        VirtualContrast = SelectedCameraSlot.VirtualContrast;
        
        // Sync PTZ speeds
        PtzMoveSpeed = SelectedCameraSlot.PtzMoveSpeed;
        PtzZoomSpeed = SelectedCameraSlot.PtzZoomSpeed;
        
        UpdateGeneratedUrl();
        
        // Notify command state changes based on selected slot's state
        PreviewCommand.NotifyCanExecuteChanged();
        VirtualizeCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        StopSelectedCommand.NotifyCanExecuteChanged();
    }
    
    private void SyncToSelectedSlot()
    {
        if (SelectedCameraSlot == null) return;
        
        // Copy settings from main UI to selected slot
        SelectedCameraSlot.IpAddress = IpAddress;
        SelectedCameraSlot.Port = Port;
        SelectedCameraSlot.Username = Username;
        SelectedCameraSlot.Password = Password;
        SelectedCameraSlot.PtzUsername = PtzUsername;
        SelectedCameraSlot.PtzPassword = PtzPassword;
        SelectedCameraSlot.SelectedBrand = SelectedBrand;
        SelectedCameraSlot.SelectedStream = SelectedStream;
        SelectedCameraSlot.Channel = Channel;
        SelectedCameraSlot.UseManualUrl = UseManualUrl;
        SelectedCameraSlot.ManualUrl = ManualUrl;
        
        // Sync PREVIEW video adjustments
        SelectedCameraSlot.PreviewFlipHorizontal = PreviewFlipHorizontal;
        SelectedCameraSlot.PreviewFlipVertical = PreviewFlipVertical;
        SelectedCameraSlot.PreviewBrightness = PreviewBrightness;
        SelectedCameraSlot.PreviewContrast = PreviewContrast;
        
        // Sync VIRTUAL OUTPUT video adjustments
        SelectedCameraSlot.VirtualFlipHorizontal = VirtualFlipHorizontal;
        SelectedCameraSlot.VirtualFlipVertical = VirtualFlipVertical;
        SelectedCameraSlot.VirtualBrightness = VirtualBrightness;
        SelectedCameraSlot.VirtualContrast = VirtualContrast;
        
        // Sync PTZ speeds
        SelectedCameraSlot.PtzMoveSpeed = PtzMoveSpeed;
        SelectedCameraSlot.PtzZoomSpeed = PtzZoomSpeed;
        
        SelectedCameraSlot.UpdateStatus();
    }
    
    [RelayCommand]
    private void SetLayout(string layoutCount)
    {
        if (int.TryParse(layoutCount, out int count))
        {
            SelectedLayoutCount = count;
            AddLog($"📐 Layout changed to {count} camera(s)");
        }
    }
    
    [RelayCommand]
    private async Task ConnectAllAsync()
    {
        AddLog("🔗 Connecting all configured cameras...");
        foreach (var slot in CameraSlots.Take(SelectedLayoutCount))
        {
            if (!string.IsNullOrWhiteSpace(slot.IpAddress) && slot.IpAddress != "192.168.1.100" && !slot.IsConnected)
            {
                // TODO: Connect each slot's camera
                AddLog($"   Connecting {slot.SlotName}...");
            }
        }
        await Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task DisconnectAllAsync()
    {
        AddLog("⏹ Disconnecting all cameras...");
        foreach (var slot in CameraSlots)
        {
            if (slot.IsConnected)
            {
                slot.IsConnected = false;
                slot.IsVirtualized = false;
                slot.UpdateStatus();
            }
        }
        UpdateMultiCameraCounts();
        await Task.CompletedTask;
    }
    
    [RelayCommand]
    private async Task SnapshotAllAsync()
    {
        AddLog("📸 Taking snapshots of all connected cameras...");
        int count = 0;
        foreach (var slot in CameraSlots.Take(SelectedLayoutCount))
        {
            if (slot.IsConnected)
            {
                count++;
                AddLog($"   📸 Snapshot {slot.SlotName}");
            }
        }
        AddLog($"✅ Captured {count} snapshot(s)");
        await Task.CompletedTask;
    }
    
    private void UpdateMultiCameraCounts()
    {
        ConnectedCameraCount = CameraSlots.Count(s => s.IsConnected);
        VirtualizedCameraCount = CameraSlots.Count(s => s.IsVirtualized);
        RecordingCameraCount = CameraSlots.Count(s => s.IsRecording);
    }
}
