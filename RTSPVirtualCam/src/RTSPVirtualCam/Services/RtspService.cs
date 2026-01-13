using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace RTSPVirtualCam.Services;

public class RtspService : IRtspService, IDisposable
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    
    // Frame capture for virtual camera
    private OBSVirtualCamOutput? _obsOutput;
    private IntPtr _frameBuffer = IntPtr.Zero;
    private int _frameWidth;
    private int _frameHeight;
    private readonly object _frameLock = new();
    private bool _virtualCamEnabled;
    private long _frameCount;
    
    public event EventHandler<RtspConnectionEventArgs>? ConnectionStateChanged;
    public event EventHandler<FrameEventArgs>? FrameReceived;
    public event Action<string>? OnLog;
    
    public bool IsConnected => _mediaPlayer?.IsPlaying ?? false;
    public bool IsVirtualCamActive => _obsOutput?.IsRunning ?? false;
    
    public int Width
    {
        get
        {
            var tracks = _mediaPlayer?.Media?.Tracks;
            if (tracks == null) return 0;
            var videoTrack = tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
            return (int)videoTrack.Data.Video.Width;
        }
    }
    
    public int Height
    {
        get
        {
            var tracks = _mediaPlayer?.Media?.Tracks;
            if (tracks == null) return 0;
            var videoTrack = tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
            return (int)videoTrack.Data.Video.Height;
        }
    }
    
    public int FrameRate => 30; // Will be detected from stream
    
    public string? Codec
    {
        get
        {
            var tracks = _mediaPlayer?.Media?.Tracks;
            if (tracks == null) return null;
            var videoTrack = tracks.FirstOrDefault(t => t.TrackType == TrackType.Video);
            return videoTrack.Codec.ToString();
        }
    }
    
    public RtspService()
    {
        // Initialize LibVLC - it will auto-detect libvlc folder
        try
        {
            // For single-file apps, use AppContext.BaseDirectory
            var exeDir = AppContext.BaseDirectory;
            var libvlcPath = System.IO.Path.Combine(exeDir, "libvlc", "win-x64");
            
            if (System.IO.Directory.Exists(libvlcPath))
            {
                Core.Initialize(libvlcPath);
            }
            else
            {
                // Fallback: let LibVLC find it automatically
                Core.Initialize();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LibVLC init error: {ex.Message}");
            // Try default initialization as fallback
            try { Core.Initialize(); } catch { }
        }
    }
    
    public async Task<bool> ConnectAsync(string rtspUrl, CancellationToken ct = default)
    {
        try
        {
            await DisconnectAsync();
            
            _libVLC = new LibVLC(
                "--rtsp-tcp",           // Use TCP for stability
                "--network-caching=300", // Low latency
                "--no-video-title-show",
                "--quiet"
            );
            
            _mediaPlayer = new MediaPlayer(_libVLC);
            _media = new Media(_libVLC, rtspUrl, FromType.FromLocation);
            
            var tcs = new TaskCompletionSource<bool>();
            
            _mediaPlayer.Playing += (s, e) =>
            {
                ConnectionStateChanged?.Invoke(this, new RtspConnectionEventArgs { IsConnected = true });
                tcs.TrySetResult(true);
            };
            
            _mediaPlayer.EncounteredError += (s, e) =>
            {
                ConnectionStateChanged?.Invoke(this, new RtspConnectionEventArgs 
                { 
                    IsConnected = false, 
                    ErrorMessage = "Failed to connect to RTSP stream" 
                });
                tcs.TrySetResult(false);
            };
            
            _mediaPlayer.Play(_media);
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));
            
            return await tcs.Task.WaitAsync(cts.Token);
        }
        catch (Exception ex)
        {
            ConnectionStateChanged?.Invoke(this, new RtspConnectionEventArgs 
            { 
                IsConnected = false, 
                ErrorMessage = ex.Message 
            });
            return false;
        }
    }
    
    public Task DisconnectAsync()
    {
        _mediaPlayer?.Stop();
        _media?.Dispose();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();
        
        _media = null;
        _mediaPlayer = null;
        _libVLC = null;
        
        ConnectionStateChanged?.Invoke(this, new RtspConnectionEventArgs { IsConnected = false });
        return Task.CompletedTask;
    }
    
    public IntPtr GetHwnd() => IntPtr.Zero; // VideoView handles this
    
    public MediaPlayer? GetMediaPlayer() => _mediaPlayer;
    
    /// <summary>
    /// Start sending frames to OBS Virtual Camera
    /// </summary>
    public bool StartVirtualCamera(int width, int height, int fps = 30)
    {
        try
        {
            if (_obsOutput != null)
            {
                _obsOutput.Stop();
                _obsOutput.Dispose();
            }
            
            _obsOutput = new OBSVirtualCamOutput();
            _obsOutput.OnLog += msg => OnLog?.Invoke(msg);
            
            if (!_obsOutput.Start((uint)width, (uint)height, fps))
            {
                OnLog?.Invoke("❌ Failed to start OBS Virtual Camera output");
                return false;
            }
            
            _frameWidth = width;
            _frameHeight = height;
            _virtualCamEnabled = true;
            _frameCount = 0;
            
            // Allocate frame buffer for BGRA
            int bufferSize = width * height * 4;
            if (_frameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_frameBuffer);
            }
            _frameBuffer = Marshal.AllocHGlobal(bufferSize);
            
            // Set up video callbacks to capture frames
            if (_mediaPlayer != null)
            {
                _mediaPlayer.SetVideoCallbacks(
                    LockCallback,
                    UnlockCallback,
                    DisplayCallback
                );
                
                _mediaPlayer.SetVideoFormat("RV32", (uint)width, (uint)height, (uint)(width * 4));
                
                OnLog?.Invoke($"✅ Virtual camera capturing: {width}x{height}");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌ Virtual camera error: {ex.Message}");
            return false;
        }
    }
    
    public void StopVirtualCamera()
    {
        _virtualCamEnabled = false;
        
        _obsOutput?.Stop();
        _obsOutput?.Dispose();
        _obsOutput = null;
        
        if (_frameBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_frameBuffer);
            _frameBuffer = IntPtr.Zero;
        }
        
        OnLog?.Invoke("Virtual camera stopped");
    }
    
    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        Marshal.WriteIntPtr(planes, _frameBuffer);
        return IntPtr.Zero;
    }
    
    private void UnlockCallback(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        // Frame data is now in _frameBuffer
    }
    
    private void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        if (!_virtualCamEnabled || _obsOutput == null || _frameBuffer == IntPtr.Zero)
            return;
        
        try
        {
            _frameCount++;
            
            // Copy BGRA data from buffer
            int size = _frameWidth * _frameHeight * 4;
            byte[] bgraData = new byte[size];
            Marshal.Copy(_frameBuffer, bgraData, 0, size);
            
            // Convert BGRA to NV12 and send to OBS
            byte[] nv12Data = OBSVirtualCamOutput.BgraToNv12(bgraData, _frameWidth, _frameHeight);
            
            // Get timestamp in nanoseconds
            ulong timestamp = (ulong)(DateTime.UtcNow.Ticks * 100);
            
            _obsOutput.SendFrame(nv12Data, timestamp);
            
            // Log every 100 frames
            if (_frameCount % 100 == 0)
            {
                OnLog?.Invoke($"📹 Sent {_frameCount} frames to virtual camera");
            }
        }
        catch (Exception ex)
        {
            if (_frameCount % 100 == 0)
            {
                OnLog?.Invoke($"⚠️ Frame error: {ex.Message}");
            }
        }
    }
    
    public void Dispose()
    {
        StopVirtualCamera();
        DisconnectAsync().Wait();
    }
}
