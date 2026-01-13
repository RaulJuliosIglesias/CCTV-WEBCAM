using System.Collections.Generic;

namespace RTSPVirtualCam.Models;

public class AppSettings
{
    public CameraSettings Camera { get; set; } = new();
    public bool StartMinimized { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;
    public bool RememberLastUrl { get; set; } = true;
    public bool AutoReconnect { get; set; } = true;
    public int MaxUrlHistory { get; set; } = 10;
    public string Theme { get; set; } = "system";
    public List<ConnectionInfo> UrlHistory { get; set; } = new();
}
