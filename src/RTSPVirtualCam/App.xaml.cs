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
            
            Log.Information("RTSP VirtualCam starting...");
            Log.Information($"Base Directory: {AppContext.BaseDirectory}");
            
            // Configure DI
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            Log.Information("Services configured successfully");
            
            // Show main window
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
        // Services - delay RtspService creation to avoid early LibVLC init
        services.AddSingleton<RtspService>();
        services.AddSingleton<IRtspService>(sp => sp.GetRequiredService<RtspService>());
        services.AddSingleton<IVirtualCameraService, VirtualCameraService>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
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
