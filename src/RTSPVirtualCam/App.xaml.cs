using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using RTSPVirtualCam.Services;
using RTSPVirtualCam.ViewModels;
using RTSPVirtualCam.Views;
using Serilog;

namespace RTSPVirtualCam;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Handle unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        
        try
        {
            // Configure logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/rtspvirtualcam.log", 
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();
            
            Log.Information("RTSP VirtualCam v2.0 - Multi-Camera Platform starting...");
            Log.Information($"Base Directory: {AppContext.BaseDirectory}");
            
            // Configure DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            Log.Information("Services configured successfully");
            
            // Show main window (with v2.0 multi-camera features integrated)
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            
            Log.Information("Main window displayed");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Error starting application:\n\n{ex.Message}\n\nSee logs for details.", 
                "RTSP VirtualCam - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // ═══════════════════════════════════════════════════════════════
        // v1.0 Legacy Services (maintained for backward compatibility)
        // ═══════════════════════════════════════════════════════════════
        services.AddSingleton<RtspService>();
        services.AddSingleton<IRtspService>(sp => sp.GetRequiredService<RtspService>());
        services.AddSingleton<IVirtualCameraService, VirtualCameraService>();
        services.AddSingleton<CameraProfileService>();
        
        // ═══════════════════════════════════════════════════════════════
        // v2.0 Multi-Camera Platform Services
        // ═══════════════════════════════════════════════════════════════
        
        // Core multi-camera service
        services.AddSingleton<IMultiCameraService, MultiCameraService>();
        
        // Advanced PTZ with presets, tours, and synchronized movements
        services.AddSingleton<IAdvancedPtzService, AdvancedPtzService>();
        
        // Recording and snapshots with scheduling
        services.AddSingleton<IRecordingService, RecordingService>();
        
        // RTMP streaming for YouTube/Twitch/Facebook
        services.AddSingleton<IRtmpStreamingService, RtmpStreamingService>();
        
        // Motion detection and analytics
        services.AddSingleton<IMotionDetectionService, MotionDetectionService>();
        
        // Cloud configuration sync
        services.AddSingleton<ICloudSyncService>(sp => 
            new CloudSyncService(sp.GetRequiredService<CameraProfileService>()));
        
        // Hardware acceleration (DXVA2, D3D11VA, CUDA, QSV)
        services.AddSingleton<IHardwareAccelerationService, HardwareAccelerationService>();
        
        // REST API server for mobile companion app
        services.AddSingleton<IApiServerService>(sp => new ApiServerService(
            sp.GetRequiredService<IMultiCameraService>(),
            sp.GetRequiredService<IAdvancedPtzService>(),
            sp.GetRequiredService<IRecordingService>(),
            sp.GetRequiredService<IRtmpStreamingService>()
        ));
        
        // ═══════════════════════════════════════════════════════════════
        // ViewModels
        // ═══════════════════════════════════════════════════════════════
        
        // v1.0 ViewModel (single camera mode)
        services.AddTransient<MainViewModel>();
        
        // v2.0 ViewModel (multi-camera mode)
        services.AddTransient<MultiCameraViewModel>(sp => new MultiCameraViewModel(
            sp.GetRequiredService<IMultiCameraService>(),
            sp.GetRequiredService<IAdvancedPtzService>(),
            sp.GetRequiredService<IRecordingService>(),
            sp.GetRequiredService<IRtmpStreamingService>(),
            sp.GetRequiredService<IMotionDetectionService>(),
            sp.GetRequiredService<ICloudSyncService>(),
            sp.GetRequiredService<IApiServerService>(),
            sp.GetRequiredService<IHardwareAccelerationService>()
        ));
        
        // ═══════════════════════════════════════════════════════════════
        // Views
        // ═══════════════════════════════════════════════════════════════
        services.AddTransient<MainWindow>();
        
        // v2.0 Multi-Camera Window
        services.AddTransient<MultiCameraWindow>(sp => new MultiCameraWindow(
            sp.GetRequiredService<IMultiCameraService>(),
            sp.GetRequiredService<IAdvancedPtzService>(),
            sp.GetRequiredService<IRecordingService>(),
            sp.GetRequiredService<IHardwareAccelerationService>()
        ));
    }
    
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled exception");
        MessageBox.Show($"Fatal error:\n\n{ex?.Message}", "RTSP VirtualCam - Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Dispatcher exception");
        MessageBox.Show($"Error:\n\n{e.Exception.Message}", "RTSP VirtualCam - Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("RTSP VirtualCam shutting down...");
        Log.CloseAndFlush();
        
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
