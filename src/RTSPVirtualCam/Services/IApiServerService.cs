using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for the REST API server for mobile companion app.
/// </summary>
public interface IApiServerService : IDisposable
{
    event EventHandler<ApiRequestEventArgs>? RequestReceived;
    event Action<string>? OnLog;

    bool IsRunning { get; }
    int Port { get; }
    string[] ListeningUrls { get; }

    Task StartAsync(int port = 8080);
    Task StopAsync();

    // Authentication
    string GenerateAuthToken();
    bool ValidateAuthToken(string token);
    void RevokeAuthToken(string token);

    // Settings
    void SetAuthRequired(bool required);
    void SetPort(int port);
}

public class ApiRequestEventArgs : EventArgs
{
    public string Method { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string ClientIp { get; init; } = string.Empty;
    public int StatusCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
