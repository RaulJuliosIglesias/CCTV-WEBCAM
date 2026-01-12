namespace RTSPVirtualCam.Models;

public record CameraSettings
{
    public string CameraName { get; init; } = "RTSP VirtualCam";
    public int Width { get; init; } = 1920;
    public int Height { get; init; } = 1080;
    public int FrameRate { get; init; } = 30;
    public bool AutoResolution { get; init; } = true;
}
