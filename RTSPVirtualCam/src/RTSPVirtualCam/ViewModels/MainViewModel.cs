using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RTSPVirtualCam.Models;
using RTSPVirtualCam.Services;

namespace RTSPVirtualCam.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRtspService _rtspService;
    private readonly IVirtualCameraService _virtualCameraService;
    
    [ObservableProperty]
    private string _rtspUrl = string.Empty;
    
    [ObservableProperty]
    private bool _isConnecting;
    
    [ObservableProperty]
    private bool _isConnected;
    
    [ObservableProperty]
    private bool _isVirtualized;
    
    [ObservableProperty]
    private string _statusText = "Disconnected";
    
    [ObservableProperty]
    private string _resolution = "--";
    
    [ObservableProperty]
    private string _frameRate = "--";
    
    [ObservableProperty]
    private string _codec = "--";
    
    [ObservableProperty]
    private string _transport = "TCP";
    
    public ObservableCollection<ConnectionInfo> UrlHistory { get; } = new();
    
    public MainViewModel(IRtspService rtspService, IVirtualCameraService virtualCameraService)
    {
        _rtspService = rtspService;
        _virtualCameraService = virtualCameraService;
        
        _rtspService.ConnectionStateChanged += OnConnectionStateChanged;
        _virtualCameraService.StateChanged += OnVirtualCameraStateChanged;
    }
    
    [RelayCommand(CanExecute = nameof(CanPreview))]
    private async Task PreviewAsync()
    {
        if (string.IsNullOrWhiteSpace(RtspUrl)) return;
        
        IsConnecting = true;
        StatusText = "Connecting...";
        
        var success = await _rtspService.ConnectAsync(RtspUrl);
        
        IsConnecting = false;
        
        if (success)
        {
            UpdateStreamInfo();
        }
    }
    
    private bool CanPreview() => !IsConnecting && !IsConnected && !string.IsNullOrWhiteSpace(RtspUrl);
    
    [RelayCommand(CanExecute = nameof(CanVirtualize))]
    private async Task VirtualizeAsync()
    {
        if (!IsConnected) return;
        
        var success = await _virtualCameraService.StartAsync(
            "RTSP VirtualCam",
            _rtspService.Width,
            _rtspService.Height,
            _rtspService.FrameRate
        );
        
        if (success)
        {
            StatusText = "Virtual Camera Active";
            AddToHistory(RtspUrl);
        }
    }
    
    private bool CanVirtualize() => IsConnected && !IsVirtualized;
    
    [RelayCommand(CanExecute = nameof(CanStop))]
    private async Task StopAsync()
    {
        if (IsVirtualized)
        {
            await _virtualCameraService.StopAsync();
        }
        
        if (IsConnected)
        {
            await _rtspService.DisconnectAsync();
        }
        
        ClearStreamInfo();
    }
    
    private bool CanStop() => IsConnected || IsVirtualized;
    
    private void OnConnectionStateChanged(object? sender, RtspConnectionEventArgs e)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            IsConnected = e.IsConnected;
            StatusText = e.IsConnected ? "Connected" : (e.ErrorMessage ?? "Disconnected");
            
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
            StatusText = e.IsActive ? "Virtual Camera Active" : (e.ErrorMessage ?? StatusText);
            
            VirtualizeCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        });
    }
    
    private void UpdateStreamInfo()
    {
        Resolution = $"{_rtspService.Width}x{_rtspService.Height}";
        FrameRate = $"{_rtspService.FrameRate} fps";
        Codec = _rtspService.Codec ?? "--";
    }
    
    private void ClearStreamInfo()
    {
        Resolution = "--";
        FrameRate = "--";
        Codec = "--";
        StatusText = "Disconnected";
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
    
    partial void OnRtspUrlChanged(string value)
    {
        PreviewCommand.NotifyCanExecuteChanged();
    }
}
