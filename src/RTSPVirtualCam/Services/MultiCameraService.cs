using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Service for managing multiple simultaneous camera connections.
/// Each camera has independent connection, preview, and virtual camera output.
/// </summary>
public class MultiCameraService : IMultiCameraService
{
    private readonly ConcurrentDictionary<string, CameraContext> _contexts = new();
    private readonly List<CameraInstance> _cameras = new();
    private readonly object _camerasLock = new();
    private LibVLC? _sharedLibVLC;
    private bool _disposed;

    public int MaxCameras { get; } = 16;

    public IReadOnlyList<CameraInstance> Cameras
    {
        get
        {
            lock (_camerasLock)
            {
                return _cameras.ToList().AsReadOnly();
            }
        }
    }

    public event EventHandler<CameraStateChangedEventArgs>? CameraStateChanged;
    public event EventHandler<CameraFrameEventArgs>? FrameReceived;
    public event EventHandler<CameraErrorEventArgs>? ErrorOccurred;
    public event Action<string, string>? OnLog;

    public MultiCameraService()
    {
        InitializeLibVLC();
    }

    private void InitializeLibVLC()
    {
        try
        {
            var exeDir = AppContext.BaseDirectory;
            var libvlcPath = System.IO.Path.Combine(exeDir, "libvlc", "win-x64");

            if (System.IO.Directory.Exists(libvlcPath))
            {
                Core.Initialize(libvlcPath);
            }
            else
            {
                Core.Initialize();
            }

            _sharedLibVLC = new LibVLC(
                "--rtsp-tcp",
                "--network-caching=300",
                "--no-video-title-show",
                "--quiet"
            );

            Log.Information("MultiCameraService: LibVLC initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MultiCameraService: Failed to initialize LibVLC");
            throw;
        }
    }

    public CameraInstance AddCamera()
    {
        lock (_camerasLock)
        {
            if (_cameras.Count >= MaxCameras)
            {
                throw new InvalidOperationException($"Maximum of {MaxCameras} cameras reached");
            }

            var slotIndex = _cameras.Count;
            var camera = new CameraInstance(slotIndex);
            _cameras.Add(camera);

            LogCamera(camera.Id, $"Camera slot {slotIndex + 1} added");
            return camera;
        }
    }

    public bool RemoveCamera(string cameraId)
    {
        lock (_camerasLock)
        {
            var camera = _cameras.FirstOrDefault(c => c.Id == cameraId);
            if (camera == null) return false;

            // Disconnect first
            DisconnectCameraAsync(cameraId).Wait();

            // Remove context
            if (_contexts.TryRemove(cameraId, out var context))
            {
                context.Dispose();
            }

            _cameras.Remove(camera);

            // Re-index remaining cameras
            for (int i = 0; i < _cameras.Count; i++)
            {
                _cameras[i].SlotIndex = i;
            }

            LogCamera(cameraId, "Camera removed");
            return true;
        }
    }

    public CameraInstance? GetCamera(string cameraId)
    {
        lock (_camerasLock)
        {
            return _cameras.FirstOrDefault(c => c.Id == cameraId);
        }
    }

    public CameraInstance? GetCameraBySlot(int slotIndex)
    {
        lock (_camerasLock)
        {
            return _cameras.FirstOrDefault(c => c.SlotIndex == slotIndex);
        }
    }

    public async Task<bool> ConnectCameraAsync(string cameraId, CancellationToken ct = default)
    {
        var camera = GetCamera(cameraId);
        if (camera == null)
        {
            LogCamera(cameraId, "Camera not found");
            return false;
        }

        if (camera.IsConnected)
        {
            LogCamera(cameraId, "Already connected");
            return true;
        }

        try
        {
            UpdateCameraState(camera, CameraConnectionState.Connecting);

            // Get or create context
            var context = GetOrCreateContext(cameraId);

            // Build RTSP URL if not manually specified
            var rtspUrl = camera.RtspUrl;
            if (string.IsNullOrEmpty(rtspUrl) && !string.IsNullOrEmpty(camera.IpAddress))
            {
                var connection = new CameraConnection
                {
                    IpAddress = camera.IpAddress,
                    Port = camera.Port,
                    Username = camera.Username,
                    Password = camera.Password,
                    Brand = camera.Brand,
                    Stream = camera.StreamType,
                    Channel = camera.Channel
                };
                rtspUrl = connection.GenerateRtspUrl();
                camera.RtspUrl = rtspUrl;
            }

            if (string.IsNullOrEmpty(rtspUrl))
            {
                throw new InvalidOperationException("No RTSP URL configured");
            }

            LogCamera(cameraId, $"Connecting to: {MaskPassword(rtspUrl)}");

            // Create media player
            context.MediaPlayer = new MediaPlayer(_sharedLibVLC!);
            context.Media = new Media(_sharedLibVLC!, rtspUrl, FromType.FromLocation);

            var tcs = new TaskCompletionSource<bool>();

            context.MediaPlayer.Playing += (s, e) =>
            {
                UpdateCameraState(camera, CameraConnectionState.Connected);
                camera.IsConnected = true;
                camera.IsConnecting = false;
                UpdateStreamInfo(camera, context);
                tcs.TrySetResult(true);
            };

            context.MediaPlayer.EncounteredError += (s, e) =>
            {
                UpdateCameraState(camera, CameraConnectionState.Error);
                camera.IsConnected = false;
                camera.IsConnecting = false;
                camera.StatusMessage = "Connection failed";
                tcs.TrySetResult(false);

                ErrorOccurred?.Invoke(this, new CameraErrorEventArgs
                {
                    CameraId = cameraId,
                    ErrorMessage = "Failed to connect to RTSP stream",
                    ErrorType = CameraErrorType.Connection
                });
            };

            context.MediaPlayer.EndReached += (s, e) =>
            {
                LogCamera(cameraId, "Stream ended");
                UpdateCameraState(camera, CameraConnectionState.Disconnected);
                camera.IsConnected = false;
            };

            // Set up frame capture
            SetupFrameCapture(camera, context);

            context.MediaPlayer.Play(context.Media);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var success = await tcs.Task.WaitAsync(cts.Token);

            if (success)
            {
                LogCamera(cameraId, "Connected successfully");
                camera.StatusMessage = "Connected";
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            LogCamera(cameraId, "Connection cancelled or timed out");
            UpdateCameraState(camera, CameraConnectionState.Disconnected);
            camera.IsConnecting = false;
            return false;
        }
        catch (Exception ex)
        {
            LogCamera(cameraId, $"Connection error: {ex.Message}");
            UpdateCameraState(camera, CameraConnectionState.Error);
            camera.StatusMessage = ex.Message;
            camera.IsConnecting = false;

            ErrorOccurred?.Invoke(this, new CameraErrorEventArgs
            {
                CameraId = cameraId,
                ErrorMessage = ex.Message,
                Exception = ex,
                ErrorType = CameraErrorType.Connection
            });

            return false;
        }
    }

    private void SetupFrameCapture(CameraInstance camera, CameraContext context)
    {
        int width = camera.Width > 0 ? camera.Width : 1280;
        int height = camera.Height > 0 ? camera.Height : 720;
        int bufferSize = width * height * 4;

        context.FrameBuffer = Marshal.AllocHGlobal(bufferSize);
        context.FrameWidth = width;
        context.FrameHeight = height;

        context.MediaPlayer!.SetVideoCallbacks(
            (opaque, planes) =>
            {
                Marshal.WriteIntPtr(planes, context.FrameBuffer);
                return IntPtr.Zero;
            },
            (opaque, picture, planes) => { },
            (opaque, picture) =>
            {
                ProcessFrame(camera, context);
            }
        );

        context.MediaPlayer.SetVideoFormat("RV32", (uint)width, (uint)height, (uint)(width * 4));
    }

    private void ProcessFrame(CameraInstance camera, CameraContext context)
    {
        if (context.FrameBuffer == IntPtr.Zero) return;

        try
        {
            context.FrameCount++;
            camera.FramesReceived = context.FrameCount;

            int size = context.FrameWidth * context.FrameHeight * 4;
            byte[] frameData = new byte[size];
            Marshal.Copy(context.FrameBuffer, frameData, 0, size);

            // Apply transformations
            if (camera.FlipHorizontal || camera.FlipVertical)
            {
                frameData = ApplyFlip(frameData, context.FrameWidth, context.FrameHeight,
                    camera.FlipHorizontal, camera.FlipVertical);
            }

            if (camera.Brightness != 0 || camera.Contrast != 0)
            {
                ApplyBrightnessContrast(frameData, camera.Brightness, camera.Contrast);
            }

            // Store current frame
            lock (context.FrameLock)
            {
                context.CurrentFrame = frameData;
            }

            // Raise frame event (throttled)
            if (context.FrameCount % 2 == 0)
            {
                FrameReceived?.Invoke(this, new CameraFrameEventArgs
                {
                    CameraId = camera.Id,
                    FrameData = frameData,
                    Width = context.FrameWidth,
                    Height = context.FrameHeight,
                    Timestamp = DateTime.UtcNow.Ticks,
                    FrameNumber = context.FrameCount
                });
            }

            // Send to virtual camera if active
            if (camera.IsVirtualized && context.VirtualCamOutput != null)
            {
                var nv12Data = OBSVirtualCamOutput.BgraToNv12(frameData, context.FrameWidth, context.FrameHeight);
                ulong timestamp = (ulong)(DateTime.UtcNow.Ticks * 100);
                context.VirtualCamOutput.SendFrame(nv12Data, timestamp);
            }
        }
        catch (Exception ex)
        {
            if (context.FrameCount % 100 == 0)
            {
                LogCamera(camera.Id, $"Frame error: {ex.Message}");
            }
        }
    }

    public async Task DisconnectCameraAsync(string cameraId)
    {
        var camera = GetCamera(cameraId);
        if (camera == null) return;

        if (_contexts.TryGetValue(cameraId, out var context))
        {
            try
            {
                // Stop virtual camera first
                if (camera.IsVirtualized)
                {
                    await StopVirtualCameraAsync(cameraId);
                }

                context.MediaPlayer?.Stop();
                context.Media?.Dispose();
                context.MediaPlayer?.Dispose();

                context.Media = null;
                context.MediaPlayer = null;

                if (context.FrameBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(context.FrameBuffer);
                    context.FrameBuffer = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                LogCamera(cameraId, $"Disconnect error: {ex.Message}");
            }
        }

        camera.IsConnected = false;
        camera.IsVirtualized = false;
        UpdateCameraState(camera, CameraConnectionState.Disconnected);
        LogCamera(cameraId, "Disconnected");
    }

    public async Task ConnectAllAsync(CancellationToken ct = default)
    {
        var tasks = new List<Task<bool>>();

        foreach (var camera in Cameras.Where(c => !c.IsConnected && !string.IsNullOrEmpty(c.RtspUrl)))
        {
            tasks.Add(ConnectCameraAsync(camera.Id, ct));
        }

        await Task.WhenAll(tasks);
    }

    public async Task DisconnectAllAsync()
    {
        var tasks = Cameras.Where(c => c.IsConnected)
            .Select(c => DisconnectCameraAsync(c.Id));
        await Task.WhenAll(tasks);
    }

    public async Task<bool> StartVirtualCameraAsync(string cameraId)
    {
        var camera = GetCamera(cameraId);
        if (camera == null || !camera.IsConnected)
        {
            LogCamera(cameraId, "Cannot start virtual camera - not connected");
            return false;
        }

        var context = GetOrCreateContext(cameraId);

        try
        {
            context.VirtualCamOutput = new OBSVirtualCamOutput();
            context.VirtualCamOutput.OnLog += msg => LogCamera(cameraId, msg);

            if (!context.VirtualCamOutput.Start((uint)context.FrameWidth, (uint)context.FrameHeight, 30))
            {
                LogCamera(cameraId, "Failed to start virtual camera output");
                return false;
            }

            camera.IsVirtualized = true;
            LogCamera(cameraId, "Virtual camera started");
            return true;
        }
        catch (Exception ex)
        {
            LogCamera(cameraId, $"Virtual camera error: {ex.Message}");
            return false;
        }
    }

    public async Task StopVirtualCameraAsync(string cameraId)
    {
        var camera = GetCamera(cameraId);
        if (camera == null) return;

        if (_contexts.TryGetValue(cameraId, out var context))
        {
            context.VirtualCamOutput?.Stop();
            context.VirtualCamOutput?.Dispose();
            context.VirtualCamOutput = null;
        }

        camera.IsVirtualized = false;
        LogCamera(cameraId, "Virtual camera stopped");
    }

    public async Task<string?> TakeSnapshotAsync(string cameraId, string? outputPath = null)
    {
        var camera = GetCamera(cameraId);
        if (camera == null || !camera.IsConnected) return null;

        var (frame, width, height) = GetCurrentFrame(cameraId);
        if (frame == null) return null;

        try
        {
            outputPath ??= System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "RTSPVirtualCam",
                $"{camera.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

            var dir = System.IO.Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // Convert BGRA to bitmap and save
            using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Marshal.Copy(frame, 0, bitmapData.Scan0, frame.Length);
            bitmap.UnlockBits(bitmapData);

            bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Jpeg);

            LogCamera(cameraId, $"Snapshot saved: {outputPath}");
            return outputPath;
        }
        catch (Exception ex)
        {
            LogCamera(cameraId, $"Snapshot error: {ex.Message}");
            return null;
        }
    }

    public (byte[]? frame, int width, int height) GetCurrentFrame(string cameraId)
    {
        if (!_contexts.TryGetValue(cameraId, out var context))
            return (null, 0, 0);

        lock (context.FrameLock)
        {
            return (context.CurrentFrame, context.FrameWidth, context.FrameHeight);
        }
    }

    public void UpdateCamera(CameraInstance camera)
    {
        lock (_camerasLock)
        {
            var existing = _cameras.FirstOrDefault(c => c.Id == camera.Id);
            if (existing != null)
            {
                var index = _cameras.IndexOf(existing);
                _cameras[index] = camera;
            }
        }
    }

    public async Task LoadFromProfilesAsync(IEnumerable<CameraProfile> profiles)
    {
        lock (_camerasLock)
        {
            _cameras.Clear();
        }

        int slotIndex = 0;
        foreach (var profile in profiles)
        {
            var camera = new CameraInstance(slotIndex++)
            {
                Name = profile.Name,
                IpAddress = profile.IpAddress,
                Port = profile.Port,
                Username = profile.Username,
                Password = profile.Password,
                Brand = profile.Brand,
                StreamType = profile.Stream,
                Channel = profile.Channel,
                RtspUrl = profile.UseManualUrl ? profile.ManualUrl : string.Empty,
                PtzUsername = profile.PtzUsername,
                PtzPassword = profile.PtzPassword
            };

            lock (_camerasLock)
            {
                _cameras.Add(camera);
            }
        }

        LogCamera("System", $"Loaded {_cameras.Count} camera profiles");
    }

    public IEnumerable<CameraProfile> SaveToProfiles()
    {
        return Cameras.Select(c => new CameraProfile
        {
            Id = c.Id,
            Name = c.Name,
            IpAddress = c.IpAddress,
            Port = c.Port,
            Username = c.Username,
            Password = c.Password,
            Brand = c.Brand,
            Stream = c.StreamType,
            Channel = c.Channel,
            UseManualUrl = !string.IsNullOrEmpty(c.RtspUrl),
            ManualUrl = c.RtspUrl,
            PtzUsername = c.PtzUsername,
            PtzPassword = c.PtzPassword
        });
    }

    private CameraContext GetOrCreateContext(string cameraId)
    {
        return _contexts.GetOrAdd(cameraId, _ => new CameraContext());
    }

    private void UpdateCameraState(CameraInstance camera, CameraConnectionState newState)
    {
        var oldState = camera.ConnectionState;
        camera.ConnectionState = newState;

        CameraStateChanged?.Invoke(this, new CameraStateChangedEventArgs
        {
            CameraId = camera.Id,
            OldState = oldState,
            NewState = newState
        });
    }

    private void UpdateStreamInfo(CameraInstance camera, CameraContext context)
    {
        if (context.MediaPlayer?.Media?.Tracks == null) return;

        var videoTrack = context.MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
        if (videoTrack.TrackType == TrackType.Video)
        {
            camera.Width = (int)videoTrack.Data.Video.Width;
            camera.Height = (int)videoTrack.Data.Video.Height;
            camera.FrameRate = (int)videoTrack.Data.Video.FrameRateNum;
            camera.Codec = videoTrack.Codec.ToString();

            // Update frame capture dimensions
            context.FrameWidth = camera.Width;
            context.FrameHeight = camera.Height;
        }

        var audioTrack = context.MediaPlayer.Media.Tracks.FirstOrDefault(t => t.TrackType == TrackType.Audio);
        if (audioTrack.TrackType == TrackType.Audio)
        {
            camera.AudioEnabled = true;
        }
    }

    private void LogCamera(string cameraId, string message)
    {
        var logMessage = $"[{cameraId}] {message}";
        Log.Information(logMessage);
        OnLog?.Invoke(cameraId, message);
    }

    private static string MaskPassword(string url)
    {
        try
        {
            var uri = new Uri(url);
            if (!string.IsNullOrEmpty(uri.UserInfo) && uri.UserInfo.Contains(':'))
            {
                var parts = uri.UserInfo.Split(':');
                return url.Replace(parts[1], "****");
            }
        }
        catch { }
        return url;
    }

    private static byte[] ApplyFlip(byte[] bgra, int width, int height, bool flipH, bool flipV)
    {
        byte[] result = new byte[bgra.Length];
        int stride = width * 4;

        for (int y = 0; y < height; y++)
        {
            int srcY = flipV ? (height - 1 - y) : y;

            for (int x = 0; x < width; x++)
            {
                int srcX = flipH ? (width - 1 - x) : x;

                int srcIdx = srcY * stride + srcX * 4;
                int dstIdx = y * stride + x * 4;

                result[dstIdx] = bgra[srcIdx];
                result[dstIdx + 1] = bgra[srcIdx + 1];
                result[dstIdx + 2] = bgra[srcIdx + 2];
                result[dstIdx + 3] = bgra[srcIdx + 3];
            }
        }

        return result;
    }

    private static void ApplyBrightnessContrast(byte[] bgra, int brightness, int contrast)
    {
        float brightnessFactor = brightness / 100f * 255f;
        float contrastFactor = (100f + contrast) / 100f;

        for (int i = 0; i < bgra.Length; i += 4)
        {
            for (int c = 0; c < 3; c++)
            {
                float value = bgra[i + c];
                value = (value - 128) * contrastFactor + 128;
                value += brightnessFactor;
                bgra[i + c] = (byte)Math.Clamp(value, 0, 255);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DisconnectAllAsync().Wait();

        foreach (var context in _contexts.Values)
        {
            context.Dispose();
        }
        _contexts.Clear();

        _sharedLibVLC?.Dispose();

        Log.Information("MultiCameraService disposed");
    }

    private class CameraContext : IDisposable
    {
        public MediaPlayer? MediaPlayer { get; set; }
        public Media? Media { get; set; }
        public IntPtr FrameBuffer { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public byte[]? CurrentFrame { get; set; }
        public object FrameLock { get; } = new();
        public long FrameCount { get; set; }
        public OBSVirtualCamOutput? VirtualCamOutput { get; set; }

        public void Dispose()
        {
            VirtualCamOutput?.Stop();
            VirtualCamOutput?.Dispose();
            MediaPlayer?.Stop();
            Media?.Dispose();
            MediaPlayer?.Dispose();

            if (FrameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(FrameBuffer);
                FrameBuffer = IntPtr.Zero;
            }
        }
    }
}
