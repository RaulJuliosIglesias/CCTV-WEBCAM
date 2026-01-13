using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;
using Serilog;

namespace RTSPVirtualCam.Views;

public partial class MultiCameraWindow : Window
{
    private readonly IMultiCameraService _cameraService;
    private readonly IAdvancedPtzService _ptzService;
    private readonly IRecordingService _recordingService;
    private readonly IHardwareAccelerationService _hwAccelService;
    
    private readonly List<CameraSlot> _cameraSlots = new();
    private int _currentLayout = 4; // 2x2 = 4 cameras
    private int _selectedSlotIndex = 0;
    private bool _isInitialized = false;
    
    public MultiCameraWindow(
        IMultiCameraService cameraService,
        IAdvancedPtzService ptzService,
        IRecordingService recordingService,
        IHardwareAccelerationService hwAccelService)
    {
        _cameraService = cameraService;
        _ptzService = ptzService;
        _recordingService = recordingService;
        _hwAccelService = hwAccelService;
        
        InitializeComponent();
        
        // Subscribe to events
        _cameraService.CameraStateChanged += OnCameraStateChanged;
        _cameraService.FrameReceived += OnFrameReceived;
        _cameraService.ErrorOccurred += OnCameraError;
        
        // Initialize after component is ready
        Loaded += OnWindowLoaded;
        
        Log.Information("MultiCameraWindow initialized");
    }
    
    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _isInitialized = true;
        InitializeCameraSlots();
        UpdateLayout(4);
        UpdateHardwareAccelStatus();
    }
    
    private void InitializeCameraSlots()
    {
        // Create 16 camera slots (max supported)
        for (int i = 0; i < 16; i++)
        {
            var camera = _cameraService.AddCamera();
            camera.Name = $"Camera {i + 1}";
            camera.SlotIndex = i;
            
            var slot = new CameraSlot
            {
                Camera = camera,
                Index = i
            };
            
            _cameraSlots.Add(slot);
        }
        
        // Select first slot
        SelectSlot(0);
    }
    
    private void UpdateLayout(int cameraCount)
    {
        _currentLayout = cameraCount;
        CameraGrid.Children.Clear();
        CameraGrid.RowDefinitions.Clear();
        CameraGrid.ColumnDefinitions.Clear();
        
        int rows, cols;
        switch (cameraCount)
        {
            case 1:
                rows = cols = 1;
                break;
            case 4:
                rows = cols = 2;
                break;
            case 9:
                rows = cols = 3;
                break;
            case 16:
                rows = cols = 4;
                break;
            default:
                rows = cols = 2;
                break;
        }
        
        // Create grid layout
        for (int i = 0; i < rows; i++)
            CameraGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        for (int i = 0; i < cols; i++)
            CameraGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        
        // Add camera slots
        for (int i = 0; i < cameraCount && i < _cameraSlots.Count; i++)
        {
            var slot = _cameraSlots[i];
            var slotUI = CreateCameraSlotUI(slot, i);
            
            Grid.SetRow(slotUI, i / cols);
            Grid.SetColumn(slotUI, i % cols);
            CameraGrid.Children.Add(slotUI);
        }
        
        UpdateStatusCounts();
    }
    
    private Border CreateCameraSlotUI(CameraSlot slot, int index)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x36)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5A)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(4),
            Tag = index
        };
        
        // Highlight selected slot
        if (index == _selectedSlotIndex)
        {
            border.BorderBrush = new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA));
            border.BorderThickness = new Thickness(3);
        }
        
        var grid = new Grid();
        
        // Preview image
        var previewImage = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        slot.PreviewImage = previewImage;
        grid.Children.Add(previewImage);
        
        // Overlay with camera info
        var overlay = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(10, 6, 10, 6)
        };
        
        var overlayStack = new StackPanel();
        
        var nameText = new TextBlock
        {
            Text = slot.Camera.Name,
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 12
        };
        slot.NameText = nameText;
        overlayStack.Children.Add(nameText);
        
        var statusText = new TextBlock
        {
            Text = slot.Camera.IsConnected ? $"🟢 {slot.Camera.Resolution}" : "⚫ Disconnected",
            Foreground = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86)),
            FontSize = 10
        };
        slot.StatusText = statusText;
        overlayStack.Children.Add(statusText);
        
        overlay.Child = overlayStack;
        grid.Children.Add(overlay);
        
        // Status indicators (top right)
        var indicators = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 8, 0)
        };
        
        if (slot.Camera.IsVirtualized)
        {
            indicators.Children.Add(new TextBlock { Text = "📹", FontSize = 16, Margin = new Thickness(2) });
        }
        if (slot.Camera.IsRecording)
        {
            indicators.Children.Add(new TextBlock { Text = "⏺️", FontSize = 16, Margin = new Thickness(2) });
        }
        
        slot.IndicatorsPanel = indicators;
        grid.Children.Add(indicators);
        
        // Click handler to select slot
        border.MouseLeftButtonDown += (s, e) =>
        {
            SelectSlot(index);
            e.Handled = true;
        };
        
        // Double-click to connect/disconnect
        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                if (slot.Camera.IsConnected)
                    _ = DisconnectCameraAsync(slot.Camera.Id);
                else
                    _ = ConnectCameraAsync(slot.Camera.Id);
                e.Handled = true;
            }
        };
        
        border.Child = grid;
        slot.SlotBorder = border;
        
        return border;
    }
    
    private void SelectSlot(int index)
    {
        if (index < 0 || index >= _cameraSlots.Count) return;
        
        // Deselect previous
        if (_selectedSlotIndex < _cameraSlots.Count && _cameraSlots[_selectedSlotIndex].SlotBorder != null)
        {
            _cameraSlots[_selectedSlotIndex].SlotBorder.BorderBrush = 
                new SolidColorBrush(Color.FromRgb(0x45, 0x47, 0x5A));
            _cameraSlots[_selectedSlotIndex].SlotBorder.BorderThickness = new Thickness(2);
        }
        
        _selectedSlotIndex = index;
        var slot = _cameraSlots[index];
        
        // Highlight new selection
        if (slot.SlotBorder != null)
        {
            slot.SlotBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x89, 0xB4, 0xFA));
            slot.SlotBorder.BorderThickness = new Thickness(3);
        }
        
        // Update control panel
        UpdateControlPanel(slot);
    }
    
    private void UpdateControlPanel(CameraSlot slot)
    {
        SelectedCameraName.Text = slot.Camera.Name;
        SelectedCameraStatus.Text = slot.Camera.IsConnected 
            ? $"🟢 Connected - {slot.Camera.Resolution}" 
            : "⚫ Disconnected";
        SelectedCameraStatus.Foreground = slot.Camera.IsConnected 
            ? new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)) 
            : new SolidColorBrush(Color.FromRgb(0xF3, 0x8B, 0xA8));
        
        TxtIpAddress.Text = slot.Camera.IpAddress ?? "192.168.1.100";
        TxtPort.Text = slot.Camera.Port.ToString();
        TxtUsername.Text = slot.Camera.Username ?? "admin";
        TxtRtspUrl.Text = slot.Camera.RtspUrl ?? "";
        
        // Update button states
        BtnConnect.IsEnabled = !slot.Camera.IsConnected;
        BtnDisconnect.IsEnabled = slot.Camera.IsConnected;
        BtnVirtualize.IsEnabled = slot.Camera.IsConnected && !slot.Camera.IsVirtualized;
        BtnStopVirtual.IsEnabled = slot.Camera.IsVirtualized;
        BtnRecord.IsEnabled = slot.Camera.IsConnected;
        BtnSnapshot.IsEnabled = slot.Camera.IsConnected;
    }
    
    private void UpdateStatusCounts()
    {
        int connected = 0, virtualized = 0, recording = 0;
        int total = Math.Min(_currentLayout, _cameraSlots.Count);
        
        for (int i = 0; i < total; i++)
        {
            if (_cameraSlots[i].Camera.IsConnected) connected++;
            if (_cameraSlots[i].Camera.IsVirtualized) virtualized++;
            if (_cameraSlots[i].Camera.IsRecording) recording++;
        }
        
        ConnectedCount.Text = $"{connected}/{total}";
        VirtualCount.Text = virtualized.ToString();
        RecordingCount.Text = recording.ToString();
    }
    
    private void UpdateHardwareAccelStatus()
    {
        var info = _hwAccelService.GetInfo();
        HwAccelStatus.Text = info.IsSupported 
            ? $"GPU: {info.GpuName.Substring(0, Math.Min(30, info.GpuName.Length))}..." 
            : "GPU: Software";
    }
    
    // Event handlers
    private void OnCameraStateChanged(object? sender, CameraStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            var slot = _cameraSlots.Find(s => s.Camera.Id == e.CameraId);
            if (slot != null)
            {
                // Update slot UI
                if (slot.StatusText != null)
                {
                    slot.StatusText.Text = slot.Camera.IsConnected 
                        ? $"🟢 {slot.Camera.Resolution}" 
                        : "⚫ Disconnected";
                }
                
                // Update control panel if this is selected
                if (slot.Index == _selectedSlotIndex)
                {
                    UpdateControlPanel(slot);
                }
                
                UpdateStatusCounts();
            }
        });
    }
    
    private void OnFrameReceived(object? sender, CameraFrameEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var slot = _cameraSlots.Find(s => s.Camera.Id == e.CameraId);
            if (slot?.PreviewImage != null)
            {
                try
                {
                    // Create WriteableBitmap if needed
                    if (slot.Bitmap == null || slot.Bitmap.PixelWidth != e.Width || slot.Bitmap.PixelHeight != e.Height)
                    {
                        slot.Bitmap = new WriteableBitmap(e.Width, e.Height, 96, 96, PixelFormats.Bgra32, null);
                        slot.PreviewImage.Source = slot.Bitmap;
                    }
                    
                    slot.Bitmap.WritePixels(
                        new Int32Rect(0, 0, e.Width, e.Height),
                        e.FrameData, e.Width * 4, 0);
                }
                catch { }
            }
        });
    }
    
    private void OnCameraError(object? sender, CameraErrorEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"Error: {e.ErrorMessage}";
            Log.Error($"Camera {e.CameraId}: {e.ErrorMessage}");
        });
    }
    
    // Layout change handlers
    private void OnLayoutChanged(object sender, RoutedEventArgs e)
    {
        // Skip if not fully initialized yet
        if (!_isInitialized) return;
        
        if (sender is RadioButton rb)
        {
            int count = rb.Name switch
            {
                "Layout1x1" => 1,
                "Layout2x2" => 4,
                "Layout3x3" => 9,
                "Layout4x4" => 16,
                _ => 4
            };
            UpdateLayout(count);
        }
    }
    
    // Camera control handlers
    private async void OnConnectClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        
        // Update camera settings from UI
        slot.Camera.IpAddress = TxtIpAddress.Text;
        slot.Camera.Port = int.TryParse(TxtPort.Text, out var port) ? port : 554;
        slot.Camera.Username = TxtUsername.Text;
        slot.Camera.Password = TxtPassword.Password;
        slot.Camera.RtspUrl = TxtRtspUrl.Text;
        
        await ConnectCameraAsync(slot.Camera.Id);
    }
    
    private async Task ConnectCameraAsync(string cameraId)
    {
        StatusText.Text = "Connecting...";
        var success = await _cameraService.ConnectCameraAsync(cameraId);
        StatusText.Text = success ? "Connected" : "Connection failed";
    }
    
    private async void OnDisconnectClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await DisconnectCameraAsync(slot.Camera.Id);
    }
    
    private async Task DisconnectCameraAsync(string cameraId)
    {
        StatusText.Text = "Disconnecting...";
        await _cameraService.DisconnectCameraAsync(cameraId);
        StatusText.Text = "Disconnected";
    }
    
    private async void OnConnectAllClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Connecting all cameras...";
        await _cameraService.ConnectAllAsync();
        StatusText.Text = "All cameras connected";
    }
    
    private async void OnDisconnectAllClick(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Disconnecting all cameras...";
        await _cameraService.DisconnectAllAsync();
        StatusText.Text = "All cameras disconnected";
    }
    
    private async void OnVirtualizeClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _cameraService.StartVirtualCameraAsync(slot.Camera.Id);
        UpdateControlPanel(slot);
    }
    
    private async void OnStopVirtualClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _cameraService.StopVirtualCameraAsync(slot.Camera.Id);
        UpdateControlPanel(slot);
    }
    
    private async void OnRecordClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        if (slot.Camera.IsRecording)
        {
            await _recordingService.StopRecordingAsync(slot.Camera.Id);
            BtnRecord.Content = "⏺️ Record";
        }
        else
        {
            await _recordingService.StartRecordingAsync(slot.Camera.Id);
            BtnRecord.Content = "⏹ Stop Rec";
        }
    }
    
    private async void OnSnapshotClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        var path = await _cameraService.TakeSnapshotAsync(slot.Camera.Id);
        if (!string.IsNullOrEmpty(path))
        {
            StatusText.Text = $"Snapshot saved: {path}";
        }
    }
    
    private async void OnSnapshotAllClick(object sender, RoutedEventArgs e)
    {
        int count = 0;
        for (int i = 0; i < _currentLayout && i < _cameraSlots.Count; i++)
        {
            var slot = _cameraSlots[i];
            if (slot.Camera.IsConnected)
            {
                await _cameraService.TakeSnapshotAsync(slot.Camera.Id);
                count++;
            }
        }
        StatusText.Text = $"Snapshots taken: {count}";
    }
    
    // PTZ handlers
    private async void OnPtzUp(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.MoveAsync(slot.Camera.Id, PtzDirection.Up, (int)PtzSpeedSlider.Value);
    }
    
    private async void OnPtzDown(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.MoveAsync(slot.Camera.Id, PtzDirection.Down, (int)PtzSpeedSlider.Value);
    }
    
    private async void OnPtzLeft(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.MoveAsync(slot.Camera.Id, PtzDirection.Left, (int)PtzSpeedSlider.Value);
    }
    
    private async void OnPtzRight(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.MoveAsync(slot.Camera.Id, PtzDirection.Right, (int)PtzSpeedSlider.Value);
    }
    
    private async void OnPtzStop(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.StopAsync(slot.Camera.Id);
    }
    
    private async void OnPtzStopClick(object sender, RoutedEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.StopAsync(slot.Camera.Id);
    }
    
    private async void OnZoomIn(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.ZoomAsync(slot.Camera.Id, ZoomDirection.In, (int)PtzSpeedSlider.Value);
    }
    
    private async void OnZoomOut(object sender, MouseButtonEventArgs e)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.ZoomAsync(slot.Camera.Id, ZoomDirection.Out, (int)PtzSpeedSlider.Value);
    }
    
    // Preset handlers
    private async void OnPreset1(object sender, RoutedEventArgs e) => await GoToPreset(1);
    private async void OnPreset2(object sender, RoutedEventArgs e) => await GoToPreset(2);
    private async void OnPreset3(object sender, RoutedEventArgs e) => await GoToPreset(3);
    private async void OnPreset4(object sender, RoutedEventArgs e) => await GoToPreset(4);
    private async void OnPreset5(object sender, RoutedEventArgs e) => await GoToPreset(5);
    private async void OnPreset6(object sender, RoutedEventArgs e) => await GoToPreset(6);
    
    private async Task GoToPreset(int presetId)
    {
        var slot = _cameraSlots[_selectedSlotIndex];
        await _ptzService.GoToPresetAsync(slot.Camera.Id, presetId);
    }
    
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Settings panel coming soon!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        if (_cameraService != null)
        {
            _cameraService.CameraStateChanged -= OnCameraStateChanged;
            _cameraService.FrameReceived -= OnFrameReceived;
            _cameraService.ErrorOccurred -= OnCameraError;
        }
        
        base.OnClosed(e);
    }
    
    private class CameraSlot
    {
        public CameraInstance Camera { get; set; } = new();
        public int Index { get; set; }
        public Border? SlotBorder { get; set; }
        public Image? PreviewImage { get; set; }
        public WriteableBitmap? Bitmap { get; set; }
        public TextBlock? NameText { get; set; }
        public TextBlock? StatusText { get; set; }
        public StackPanel? IndicatorsPanel { get; set; }
    }
}
