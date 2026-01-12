using System.Windows;
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
        
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/rtspvirtualcam.log", 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
        
        Log.Information("RTSP VirtualCam starting...");
        
        // Configure DI
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<RtspService>();
        services.AddSingleton<IRtspService>(sp => sp.GetRequiredService<RtspService>());
        services.AddSingleton<IVirtualCameraService, VirtualCameraService>();
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        
        // Views
        services.AddTransient<MainWindow>();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("RTSP VirtualCam shutting down...");
        Log.CloseAndFlush();
        
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
