using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using LibVLCSharp.Shared;
using RTSPVirtualCam.Services;
using RTSPVirtualCam.ViewModels;

namespace RTSPVirtualCam.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly RtspService _rtspService;
    
    public MainWindow(MainViewModel viewModel, RtspService rtspService)
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        _rtspService = rtspService;
        DataContext = viewModel;
        
        // Wire up VLC VideoView
        _rtspService.ConnectionStateChanged += (s, e) =>
        {
            if (e.IsConnected)
            {
                Dispatcher.Invoke(() =>
                {
                    var mediaPlayer = _rtspService.GetMediaPlayer();
                    if (mediaPlayer != null)
                    {
                        VideoView.MediaPlayer = mediaPlayer;
                    }
                });
            }
        };
    }
    
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
        e.Handled = true;
    }
    
    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open settings window
        MessageBox.Show("Settings coming soon!", "Settings", MessageBoxButton.OK);
    }
    
    protected override void OnClosed(EventArgs e)
    {
        _rtspService.Dispose();
        base.OnClosed(e);
    }
}
