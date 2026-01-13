namespace RTSPVirtualCam.Models;

/// <summary>
/// Camera brands with known RTSP URL patterns
/// </summary>
public enum CameraBrand
{
    Hikvision,
    Dahua,
    Generic
}

/// <summary>
/// Stream types for IP cameras
/// </summary>
public enum StreamType
{
    MainStream,   // High quality (1080p/4K)
    SubStream,    // Low quality (720p/480p)
    ThirdStream   // Mobile stream
}

/// <summary>
/// Camera connection configuration
/// </summary>
public class CameraConnection
{
    public string IpAddress { get; set; } = "192.168.1.64";
    public int Port { get; set; } = 554;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public CameraBrand Brand { get; set; } = CameraBrand.Hikvision;
    public StreamType Stream { get; set; } = StreamType.MainStream;
    public int Channel { get; set; } = 1;
    
    /// <summary>
    /// Generates the RTSP URL based on the camera brand and settings
    /// </summary>
    public string GenerateRtspUrl()
    {
        var credentials = string.IsNullOrEmpty(Password) 
            ? Username 
            : $"{Username}:{Password}";
            
        return Brand switch
        {
            CameraBrand.Hikvision => GenerateHikvisionUrl(credentials),
            CameraBrand.Dahua => GenerateDahuaUrl(credentials),
            CameraBrand.Generic => $"rtsp://{credentials}@{IpAddress}:{Port}/stream{(int)Stream + 1}",
            _ => $"rtsp://{credentials}@{IpAddress}:{Port}/"
        };
    }
    
    private string GenerateHikvisionUrl(string credentials)
    {
        // Hikvision format: /Streaming/Channels/XYZ
        // X = channel (1, 2, 3...)
        // YZ = stream type (01 = main, 02 = sub, 03 = third)
        int streamCode = Channel * 100 + (int)Stream + 1;
        return $"rtsp://{credentials}@{IpAddress}:{Port}/Streaming/Channels/{streamCode}";
    }
    
    private string GenerateDahuaUrl(string credentials)
    {
        // Dahua format: /cam/realmonitor?channel=X&subtype=Y
        // subtype: 0 = main, 1 = sub
        int subtype = Stream == StreamType.MainStream ? 0 : 1;
        return $"rtsp://{credentials}@{IpAddress}:{Port}/cam/realmonitor?channel={Channel}&subtype={subtype}";
    }
}
