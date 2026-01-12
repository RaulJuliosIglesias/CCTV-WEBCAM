using System;
using System.Threading;
using System.Threading.Tasks;

namespace RTSPVirtualCam.Services;

public interface IRtspService
{
    event EventHandler<RtspConnectionEventArgs>? ConnectionStateChanged;
    event EventHandler<FrameEventArgs>? FrameReceived;
    
    bool IsConnected { get; }
    int Width { get; }
    int Height { get; }
    int FrameRate { get; }
    string? Codec { get; }
    
    Task<bool> ConnectAsync(string rtspUrl, CancellationToken ct = default);
    Task DisconnectAsync();
    IntPtr GetHwnd(); // For WPF VideoView
}

public class RtspConnectionEventArgs : EventArgs
{
    public bool IsConnected { get; init; }
    public string? ErrorMessage { get; init; }
}

public class FrameEventArgs : EventArgs
{
    public byte[] FrameData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public long Timestamp { get; init; }
}
