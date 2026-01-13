using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        
        // Enable keyboard focus for PTZ control
        this.KeyDown += MainWindow_KeyDown;
        this.PreviewKeyDown += MainWindow_PreviewKeyDown; // Add preview event
        this.Focusable = true;
        this.Loaded += (s, e) => 
        {
            this.Focus();
            AddDebugLog("Window loaded, focus set to main window");
        };
        this.GotFocus += (s, e) => AddDebugLog("Main window got focus");
        this.LostFocus += (s, e) => AddDebugLog("Main window lost focus");
        this.MouseDown += (s, e) => 
        {
            if (!(e.Source is TextBox || e.Source is Button || e.Source is Slider))
            {
                this.Focus();
                AddDebugLog("Window clicked, focus set to main window");
            }
        };
    }
    
    private void AddDebugLog(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[KEYBOARD DEBUG] {message}");
        _viewModel?.AddLog($"⌨️ {message}");
    }
    
    private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        AddDebugLog($"PreviewKeyDown: Key={e.Key}, Source={e.Source?.GetType().Name}, FocusedElement={Keyboard.FocusedElement?.GetType().Name}");
        
        // Handle PTZ keys at preview level to ensure they're not intercepted
        if (IsPtzKey(e.Key))
        {
            if (Keyboard.FocusedElement is TextBox)
            {
                AddDebugLog("TextBox has focus, ignoring PTZ key");
                return;
            }
            
            AddDebugLog($"Processing PTZ key: {e.Key}");
            await ProcessPtzKey(e.Key);
            e.Handled = true;
        }
    }
    
    private bool IsPtzKey(Key key)
    {
        return key == Key.Up || key == Key.Down || key == Key.Left || 
               key == Key.Right || key == Key.PageUp || key == Key.PageDown;
    }
    
    private async Task ProcessPtzKey(Key key)
    {
        try
        {
            switch (key)
            {
                case Key.Up:
                    await _viewModel.PtzKeyboardControlAsync("Up");
                    break;
                case Key.Down:
                    await _viewModel.PtzKeyboardControlAsync("Down");
                    break;
                case Key.Left:
                    await _viewModel.PtzKeyboardControlAsync("Left");
                    break;
                case Key.Right:
                    await _viewModel.PtzKeyboardControlAsync("Right");
                    break;
                case Key.PageUp:
                    await _viewModel.PtzKeyboardControlAsync("PageUp");
                    break;
                case Key.PageDown:
                    await _viewModel.PtzKeyboardControlAsync("PageDown");
                    break;
            }
        }
        catch (Exception ex)
        {
            AddDebugLog($"Error processing PTZ key: {ex.Message}");
        }
    }
    
    private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        AddDebugLog($"KeyDown: Key={e.Key}, Source={e.Source?.GetType().Name}, Handled={e.Handled}");
        
        // Only handle PTZ keys when not in text boxes and not already handled
        if (e.Handled) return;
        
        if (Keyboard.FocusedElement is TextBox)
        {
            AddDebugLog("TextBox has focus, ignoring KeyDown");
            return;
        }
        
        if (IsPtzKey(e.Key))
        {
            AddDebugLog($"Processing PTZ key in KeyDown: {e.Key}");
            await ProcessPtzKey(e.Key);
            e.Handled = true;
        }
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
