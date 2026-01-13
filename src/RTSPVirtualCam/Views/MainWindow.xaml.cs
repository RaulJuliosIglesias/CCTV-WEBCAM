using System;
using System.Diagnostics;
using System.Linq;
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
        
        // Subscribe to layout changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedLayoutCount))
            {
                UpdateCameraGridLayout(_viewModel.SelectedLayoutCount);
            }
        };
        
        // Set initial layout
        Loaded += (s, e) => UpdateCameraGridLayout(1);
    }
    
    private void UpdateCameraGridLayout(int cameraCount)
    {
        // Find the UniformGrid in the ItemsControl
        if (CameraSlotsGrid?.ItemsPanel?.LoadContent() is System.Windows.Controls.Primitives.UniformGrid)
        {
            // We need to update the ItemsPanelTemplate dynamically
            // For now, use a simpler approach - update visibility of slots
        }
        
        // Update which slots are visible based on layout
        for (int i = 0; i < _viewModel.CameraSlots.Count; i++)
        {
            // All slots exist but only show up to cameraCount
            // The XAML binding will handle this via the collection
        }
        
        // Determine grid layout based on camera count
        int rows, cols;
        switch (cameraCount)
        {
            case 1: rows = 1; cols = 1; break;
            case 2: rows = 1; cols = 2; break;  // Side by side
            case 4: rows = 2; cols = 2; break;
            case 6: rows = 2; cols = 3; break;
            default: rows = 1; cols = 1; break;
        }
        
        // Update the ItemsPanel template dynamically
        var template = new ItemsPanelTemplate();
        var factory = new FrameworkElementFactory(typeof(System.Windows.Controls.Primitives.UniformGrid));
        factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.RowsProperty, rows);
        factory.SetValue(System.Windows.Controls.Primitives.UniformGrid.ColumnsProperty, cols);
        template.VisualTree = factory;
        CameraSlotsGrid.ItemsPanel = template;
        
        AddDebugLog($"Layout updated to {cameraCount} cameras ({rows}x{cols})");
    }
    
    private void AddDebugLog(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[KEYBOARD DEBUG] {message}");
        _viewModel?.AddLog($"⌨️ {message}");
    }
    
    private async void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        AddDebugLog($"PreviewKeyDown: Key={e.Key}, Source={e.Source?.GetType().Name}, FocusedElement={Keyboard.FocusedElement?.GetType().Name}");
        
        // Handle PTZ keys at preview level to ensure they're not intercepted by sliders
        if (IsPtzKey(e.Key))
        {
            // Only skip PTZ processing for TextBox (user is typing)
            if (Keyboard.FocusedElement is TextBox)
            {
                AddDebugLog("TextBox has focus, allowing normal text input");
                return;
            }
            
            // CRITICAL: Mark as handled IMMEDIATELY to prevent sliders from receiving the event
            // This must happen before the await or the event will propagate before being marked
            e.Handled = true;
            
            AddDebugLog($"Processing PTZ key: {e.Key}");
            await ProcessPtzKey(e.Key);
        }
        // Handle preset shortcuts
        else if (IsPresetKey(e.Key))
        {
            // Only skip for TextBox
            if (Keyboard.FocusedElement is TextBox)
            {
                return;
            }
            
            // Mark as handled immediately
            e.Handled = true;
            
            int presetId = await GetPresetIdFromKey(e.Key, e.KeyboardDevice.Modifiers);
            if (presetId > 0)
            {
                AddDebugLog($"Going to preset {presetId}");
                var preset = _viewModel.PtzPresets.FirstOrDefault(p => p.Id == presetId);
                if (preset != null)
                {
                    await _viewModel.GotoPresetAsync(preset);
                }
            }
        }
    }
    
    private bool IsPresetKey(Key key)
    {
        return key == Key.D1 || key == Key.D2 || key == Key.D3 || key == Key.D4 || key == Key.D5 ||
               key == Key.D6 || key == Key.D7 || key == Key.D8 || key == Key.D9 || key == Key.D0 ||
               key == Key.NumPad1 || key == Key.NumPad2 || key == Key.NumPad3 || key == Key.NumPad4 ||
               key == Key.NumPad5 || key == Key.NumPad6 || key == Key.NumPad7 || key == Key.NumPad8 ||
               key == Key.NumPad9 || key == Key.NumPad0;
    }
    
    private async Task<int> GetPresetIdFromKey(Key key, ModifierKeys modifiers)
    {
        int baseNumber = key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            Key.D6 or Key.NumPad6 => 6,
            Key.D7 or Key.NumPad7 => 7,
            Key.D8 or Key.NumPad8 => 8,
            Key.D9 or Key.NumPad9 => 9,
            Key.D0 or Key.NumPad0 => 10,
            _ => 0
        };
        
        if (baseNumber == 0) return 0;
        
        // No modifiers: 1-10
        if (modifiers == ModifierKeys.None)
            return baseNumber;
        
        // Ctrl: 11-20
        if (modifiers == ModifierKeys.Control)
            return 10 + baseNumber;
        
        // Shift: 21-30
        if (modifiers == ModifierKeys.Shift)
            return 20 + baseNumber;
        
        return 0;
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
        
        // Skip if already handled by PreviewKeyDown
        if (e.Handled) return;
        
        // Only skip for TextBox (user is typing)
        if (Keyboard.FocusedElement is TextBox)
        {
            AddDebugLog("TextBox has focus in KeyDown, allowing normal text input");
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
