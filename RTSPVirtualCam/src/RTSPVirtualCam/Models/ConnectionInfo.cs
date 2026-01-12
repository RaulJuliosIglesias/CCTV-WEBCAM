using System;

namespace RTSPVirtualCam.Models;

public record ConnectionInfo
{
    public string RtspUrl { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string Transport { get; init; } = "tcp"; // tcp or udp
    public int TimeoutSeconds { get; init; } = 30;
    public DateTime LastUsed { get; init; } = DateTime.UtcNow;
}
