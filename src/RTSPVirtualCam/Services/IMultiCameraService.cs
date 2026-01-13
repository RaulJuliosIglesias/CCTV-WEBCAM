using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for managing multiple simultaneous camera connections.
/// </summary>
public interface IMultiCameraService : IDisposable
{
    /// <summary>
    /// Maximum number of concurrent camera connections supported.
    /// </summary>
    int MaxCameras { get; }

    /// <summary>
    /// Currently active camera instances.
    /// </summary>
    IReadOnlyList<CameraInstance> Cameras { get; }

    /// <summary>
    /// Event raised when a camera's state changes.
    /// </summary>
    event EventHandler<CameraStateChangedEventArgs>? CameraStateChanged;

    /// <summary>
    /// Event raised when a frame is received from any camera.
    /// </summary>
    event EventHandler<CameraFrameEventArgs>? FrameReceived;

    /// <summary>
    /// Event raised when an error occurs on any camera.
    /// </summary>
    event EventHandler<CameraErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Event raised for logging/diagnostics.
    /// </summary>
    event Action<string, string>? OnLog; // (cameraId, message)

    /// <summary>
    /// Adds a new camera slot.
    /// </summary>
    CameraInstance AddCamera();

    /// <summary>
    /// Removes a camera by ID.
    /// </summary>
    bool RemoveCamera(string cameraId);

    /// <summary>
    /// Gets a camera instance by ID.
    /// </summary>
    CameraInstance? GetCamera(string cameraId);

    /// <summary>
    /// Gets a camera instance by slot index.
    /// </summary>
    CameraInstance? GetCameraBySlot(int slotIndex);

    /// <summary>
    /// Connects a specific camera to its RTSP stream.
    /// </summary>
    Task<bool> ConnectCameraAsync(string cameraId, CancellationToken ct = default);

    /// <summary>
    /// Disconnects a specific camera.
    /// </summary>
    Task DisconnectCameraAsync(string cameraId);

    /// <summary>
    /// Connects all configured cameras.
    /// </summary>
    Task ConnectAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Disconnects all cameras.
    /// </summary>
    Task DisconnectAllAsync();

    /// <summary>
    /// Starts virtual camera output for a specific camera.
    /// </summary>
    Task<bool> StartVirtualCameraAsync(string cameraId);

    /// <summary>
    /// Stops virtual camera output for a specific camera.
    /// </summary>
    Task StopVirtualCameraAsync(string cameraId);

    /// <summary>
    /// Takes a snapshot from a specific camera.
    /// </summary>
    Task<string?> TakeSnapshotAsync(string cameraId, string? outputPath = null);

    /// <summary>
    /// Gets the current frame from a specific camera.
    /// </summary>
    (byte[]? frame, int width, int height) GetCurrentFrame(string cameraId);

    /// <summary>
    /// Updates camera configuration.
    /// </summary>
    void UpdateCamera(CameraInstance camera);

    /// <summary>
    /// Loads camera configurations from profile.
    /// </summary>
    Task LoadFromProfilesAsync(IEnumerable<CameraProfile> profiles);

    /// <summary>
    /// Saves current camera configurations to profiles.
    /// </summary>
    IEnumerable<CameraProfile> SaveToProfiles();
}

public class CameraStateChangedEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public CameraConnectionState OldState { get; init; }
    public CameraConnectionState NewState { get; init; }
    public string? Message { get; init; }
}

public class CameraFrameEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public byte[] FrameData { get; init; } = Array.Empty<byte>();
    public int Width { get; init; }
    public int Height { get; init; }
    public long Timestamp { get; init; }
    public long FrameNumber { get; init; }
}

public class CameraErrorEventArgs : EventArgs
{
    public string CameraId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public CameraErrorType ErrorType { get; init; }
}

public enum CameraErrorType
{
    Connection,
    Authentication,
    Stream,
    VirtualCamera,
    Recording,
    Network,
    Unknown
}
