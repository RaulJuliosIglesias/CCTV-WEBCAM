using System;
using System.Threading.Tasks;

namespace RTSPVirtualCam.Services;

public interface IVirtualCameraService
{
    event EventHandler<VirtualCameraEventArgs>? StateChanged;
    
    bool IsActive { get; }
    string CameraName { get; }
    
    Task<bool> StartAsync(string cameraName, int width, int height, int fps);
    Task StopAsync();
    void PushFrame(byte[] frameData, int width, int height);
}

public class VirtualCameraEventArgs : EventArgs
{
    public bool IsActive { get; init; }
    public string? ErrorMessage { get; init; }
}
