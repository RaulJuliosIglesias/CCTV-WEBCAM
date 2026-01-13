using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Virtual Camera Service using Windows 11 MFCreateVirtualCamera API.
/// For full implementation, reference: https://github.com/smourier/VCamNetSample
/// </summary>
public class VirtualCameraService : IVirtualCameraService, IDisposable
{
    public event EventHandler<VirtualCameraEventArgs>? StateChanged;
    
    public bool IsActive { get; private set; }
    public string CameraName { get; private set; } = "RTSP VirtualCam";
    
    // Frame buffer for virtual camera
    private byte[]? _currentFrame;
    private readonly object _frameLock = new();
    private int _frameWidth;
    private int _frameHeight;
    
    public async Task<bool> StartAsync(string cameraName, int width, int height, int fps)
    {
        // NOTE: Full implementation requires:
        // 1. COM-registered Media Source (VCamSource.dll)
        // 2. MFCreateVirtualCamera API call
        // See VCamNetSample for complete implementation
        
        try
        {
            CameraName = cameraName;
            _frameWidth = width;
            _frameHeight = height;
            
            // Check Windows version
            if (!IsWindows11OrLater())
            {
                StateChanged?.Invoke(this, new VirtualCameraEventArgs 
                { 
                    IsActive = false, 
                    ErrorMessage = "Windows 11 required for virtual camera" 
                });
                return false;
            }
            
            // TODO: Implement MFCreateVirtualCamera
            // This is a placeholder - actual implementation needs
            // the VCamSource COM component registered
            
            IsActive = true;
            StateChanged?.Invoke(this, new VirtualCameraEventArgs { IsActive = true });
            
            return true;
        }
        catch (Exception ex)
        {
            StateChanged?.Invoke(this, new VirtualCameraEventArgs 
            { 
                IsActive = false, 
                ErrorMessage = ex.Message 
            });
            return false;
        }
    }
    
    public Task StopAsync()
    {
        IsActive = false;
        _currentFrame = null;
        StateChanged?.Invoke(this, new VirtualCameraEventArgs { IsActive = false });
        return Task.CompletedTask;
    }
    
    public void PushFrame(byte[] frameData, int width, int height)
    {
        lock (_frameLock)
        {
            _currentFrame = frameData;
            _frameWidth = width;
            _frameHeight = height;
        }
    }
    
    public (byte[]? frame, int width, int height) GetCurrentFrame()
    {
        lock (_frameLock)
        {
            return (_currentFrame, _frameWidth, _frameHeight);
        }
    }
    
    private static bool IsWindows11OrLater()
    {
        var version = Environment.OSVersion.Version;
        // Windows 11 is build 22000+
        return version.Major >= 10 && version.Build >= 22000;
    }
    
    public void Dispose()
    {
        StopAsync().Wait();
    }
}
