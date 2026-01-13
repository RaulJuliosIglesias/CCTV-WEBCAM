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
    
    // Camera settings - PREVIEW
    private bool _previewFlipHorizontal;
    private bool _previewFlipVertical;
    private int _previewBrightness; // -100 to 100
    private int _previewContrast;   // -100 to 100
    
    // Camera settings - VIRTUAL CAMERA OUTPUT
    private bool _virtualFlipHorizontal;
    private bool _virtualFlipVertical;
    private int _virtualBrightness; // -100 to 100
    private int _virtualContrast;   // -100 to 100
    
    public event EventHandler<RtspConnectionEventArgs>? ConnectionStateChanged;
    public event EventHandler<FrameEventArgs>? FrameReceived;
    public event Action<string>? OnLog;
    public event Action<byte[], int, int>? OnPreviewFrame; // BGRA frame for UI preview
    
    public bool IsConnected => _mediaPlayer?.IsPlaying ?? false;
    public bool IsVirtualCamActive => _obsOutput?.IsRunning ?? false;
    
    // Preview settings properties
    public bool PreviewFlipHorizontal { get => _previewFlipHorizontal; set => _previewFlipHorizontal = value; }
    public bool PreviewFlipVertical { get => _previewFlipVertical; set => _previewFlipVertical = value; }
    public int PreviewBrightness { get => _previewBrightness; set => _previewBrightness = Math.Clamp(value, -100, 100); }
    public int PreviewContrast { get => _previewContrast; set => _previewContrast = Math.Clamp(value, -100, 100); }
    
    // Virtual camera output settings properties
    public bool VirtualFlipHorizontal { get => _virtualFlipHorizontal; set => _virtualFlipHorizontal = value; }
    public bool VirtualFlipVertical { get => _virtualFlipVertical; set => _virtualFlipVertical = value; }
    public int VirtualBrightness { get => _virtualBrightness; set => _virtualBrightness = Math.Clamp(value, -100, 100); }
    public int VirtualContrast { get => _virtualContrast; set => _virtualContrast = Math.Clamp(value, -100, 100); }
    
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
            OnLog?.Invoke($"[DEBUG] StartVirtualCamera called: {width}x{height}@{fps}fps");
            
            if (_obsOutput != null)
            {
                OnLog?.Invoke("[DEBUG] Stopping existing OBS output...");
                _obsOutput.Stop();
                _obsOutput.Dispose();
                _obsOutput = null;
            }
            
            OnLog?.Invoke("[DEBUG] Creating new OBSVirtualCamOutput...");
            _obsOutput = new OBSVirtualCamOutput();
            _obsOutput.OnLog += msg => OnLog?.Invoke(msg);
            
            OnLog?.Invoke("[DEBUG] Starting OBS output...");
            if (!_obsOutput.Start((uint)width, (uint)height, fps))
            {
                OnLog?.Invoke("❌ Failed to start OBS Virtual Camera output");
                return false;
            }
            
            OnLog?.Invoke("[DEBUG] OBS output started successfully");
            
            _frameWidth = width;
            _frameHeight = height;
            _virtualCamEnabled = true;
            _frameCount = 0;
            
            // Allocate frame buffer for BGRA
            int bufferSize = width * height * 4;
            OnLog?.Invoke($"[DEBUG] Allocating frame buffer: {bufferSize} bytes");
            
            if (_frameBuffer != IntPtr.Zero)
            {
                OnLog?.Invoke("[DEBUG] Freeing existing frame buffer...");
                Marshal.FreeHGlobal(_frameBuffer);
                _frameBuffer = IntPtr.Zero;
            }
            _frameBuffer = Marshal.AllocHGlobal(bufferSize);
            OnLog?.Invoke($"[DEBUG] Frame buffer allocated at: 0x{_frameBuffer:X}");
            
            // Set up video callbacks to capture frames
            // Must stop and restart the player for callbacks to take effect
            if (_mediaPlayer != null && _media != null)
            {
                OnLog?.Invoke("🔄 Restarting stream with frame capture...");
                
                OnLog?.Invoke("[DEBUG] Stopping media player...");
                _mediaPlayer.Stop();
                
                OnLog?.Invoke("[DEBUG] Setting video callbacks...");
                _mediaPlayer.SetVideoCallbacks(
                    LockCallback,
                    UnlockCallback,
                    DisplayCallback
                );
                
                OnLog?.Invoke($"[DEBUG] Setting video format RV32 {width}x{height}...");
                _mediaPlayer.SetVideoFormat("RV32", (uint)width, (uint)height, (uint)(width * 4));
                
                OnLog?.Invoke("[DEBUG] Starting playback...");
                _mediaPlayer.Play(_media);
                
                OnLog?.Invoke($"✅ Virtual camera capturing: {width}x{height}");
                OnLog?.Invoke("📹 Frame capture active - sending to OBS Virtual Camera");
            }
            else
            {
                OnLog?.Invoke("[DEBUG] WARNING: MediaPlayer or Media is null!");
            }
            
            OnLog?.Invoke("[DEBUG] StartVirtualCamera completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌❌❌ Virtual camera CRASH: {ex.Message}");
            OnLog?.Invoke($"[DEBUG] Stack trace: {ex.StackTrace}");
            return false;
        }
    }
    
    public void StopVirtualCamera()
    {
        try
        {
            OnLog?.Invoke("[DEBUG] StopVirtualCamera called");
            
            // CRITICAL: Disable flag FIRST to stop callback processing
            _virtualCamEnabled = false;
            
            // Stop media player to stop callbacks from firing
            if (_mediaPlayer != null && _media != null)
            {
                OnLog?.Invoke("[DEBUG] Stopping media player to drain callbacks...");
                _mediaPlayer.Stop();
                
                // Clear video callbacks
                OnLog?.Invoke("[DEBUG] Clearing video callbacks...");
                _mediaPlayer.SetVideoCallbacks(null, null, null);
                
                // Wait for callbacks to drain
                OnLog?.Invoke("[DEBUG] Waiting for callbacks to drain...");
                System.Threading.Thread.Sleep(100);
                
                // Restart player with preview callbacks (not virtual camera)
                OnLog?.Invoke("[DEBUG] Restarting with preview callbacks...");
                _mediaPlayer.SetVideoCallbacks(
                    LockCallback,
                    UnlockCallback,
                    PreviewDisplayCallback
                );
                _mediaPlayer.SetVideoFormat("RV32", (uint)_frameWidth, (uint)_frameHeight, (uint)(_frameWidth * 4));
                _mediaPlayer.Play(_media);
                OnLog?.Invoke("[DEBUG] Preview restored");
            }
            
            if (_obsOutput != null)
            {
                OnLog?.Invoke("[DEBUG] Stopping OBS output...");
                _obsOutput.Stop();
                OnLog?.Invoke("[DEBUG] Disposing OBS output...");
                _obsOutput.Dispose();
                _obsOutput = null;
            }
            
            // DON'T free the frame buffer - it's still needed for preview!
            // if (_frameBuffer != IntPtr.Zero)
            // {
            //     OnLog?.Invoke("[DEBUG] Freeing frame buffer...");
            //     Marshal.FreeHGlobal(_frameBuffer);
            //     _frameBuffer = IntPtr.Zero;
            // }
            
            OnLog?.Invoke("✅ Virtual camera stopped");
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌❌❌ StopVirtualCamera CRASH: {ex.Message}");
            OnLog?.Invoke($"[DEBUG] Stack trace: {ex.StackTrace}");
        }
    }
    
    // Enable software rendering for embedded preview (without OBS virtual camera)
    private bool _previewCaptureEnabled = false;
    
    public bool StartPreviewCapture(int width = 1280, int height = 720)
    {
        try
        {
            if (_mediaPlayer == null || _media == null)
            {
                OnLog?.Invoke("❌ Cannot start preview - not connected");
                return false;
            }
            
            _frameWidth = width;
            _frameHeight = height;
            _frameCount = 0;
            _previewCaptureEnabled = true;
            
            // Allocate frame buffer for BGRA
            int bufferSize = width * height * 4;
            if (_frameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_frameBuffer);
            }
            _frameBuffer = Marshal.AllocHGlobal(bufferSize);
            
            OnLog?.Invoke("🔄 Starting embedded preview capture...");
            
            // Stop current playback
            _mediaPlayer.Stop();
            
            // Configure video callbacks for frame capture
            _mediaPlayer.SetVideoCallbacks(
                LockCallback,
                UnlockCallback,
                PreviewDisplayCallback
            );
            
            _mediaPlayer.SetVideoFormat("RV32", (uint)width, (uint)height, (uint)(width * 4));
            
            // Restart playback with callbacks active
            _mediaPlayer.Play(_media);
            
            OnLog?.Invoke($"✅ Preview capture active: {width}x{height}");
            return true;
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌ Preview capture error: {ex.Message}");
            return false;
        }
    }
    
    public void StopPreviewCapture()
    {
        _previewCaptureEnabled = false;
        OnLog?.Invoke("Preview capture stopped");
    }
    
    private void PreviewDisplayCallback(IntPtr opaque, IntPtr picture)
    {
        if (!_previewCaptureEnabled || _frameBuffer == IntPtr.Zero)
            return;
        
        try
        {
            _frameCount++;
            
            // Copy BGRA data from buffer
            int size = _frameWidth * _frameHeight * 4;
            byte[] frame = new byte[size];
            Marshal.Copy(_frameBuffer, frame, 0, size);
            
            // Apply flip transformations if enabled
            if (_previewFlipHorizontal || _previewFlipVertical)
            {
                frame = ApplyFlip(frame, _frameWidth, _frameHeight, _previewFlipHorizontal, _previewFlipVertical);
            }
            
            // Apply brightness/contrast if needed
            if (_previewBrightness != 0 || _previewContrast != 0)
            {
                ApplyBrightnessContrast(frame, _previewBrightness, _previewContrast);
            }
            
            // Send to UI preview (every 2nd frame to reduce load)
            if (_frameCount % 2 == 0)
            {
                OnPreviewFrame?.Invoke(frame, _frameWidth, _frameHeight);
            }
        }
        catch { }
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
            
            // Copy BGRA data from buffer (original frame)
            int size = _frameWidth * _frameHeight * 4;
            byte[] originalFrame = new byte[size];
            Marshal.Copy(_frameBuffer, originalFrame, 0, size);
            
            // === PREVIEW FRAME (for UI) ===
            byte[] previewFrame = new byte[size];
            Array.Copy(originalFrame, previewFrame, size);
            
            // Apply preview transformations
            if (_previewFlipHorizontal || _previewFlipVertical)
            {
                previewFrame = ApplyFlip(previewFrame, _frameWidth, _frameHeight, _previewFlipHorizontal, _previewFlipVertical);
            }
            
            if (_previewBrightness != 0 || _previewContrast != 0)
            {
                ApplyBrightnessContrast(previewFrame, _previewBrightness, _previewContrast);
            }
            
            // Send to UI preview (every 2nd frame to reduce load)
            if (_frameCount % 2 == 0)
            {
                OnPreviewFrame?.Invoke(previewFrame, _frameWidth, _frameHeight);
            }
            
            // === VIRTUAL CAMERA OUTPUT FRAME ===
            byte[] virtualFrame = new byte[size];
            Array.Copy(originalFrame, virtualFrame, size);
            
            // Apply virtual camera transformations
            if (_virtualFlipHorizontal || _virtualFlipVertical)
            {
                virtualFrame = ApplyFlip(virtualFrame, _frameWidth, _frameHeight, _virtualFlipHorizontal, _virtualFlipVertical);
            }
            
            if (_virtualBrightness != 0 || _virtualContrast != 0)
            {
                ApplyBrightnessContrast(virtualFrame, _virtualBrightness, _virtualContrast);
            }
            
            // Convert to NV12 and send to OBS Virtual Camera
            byte[] nv12Data = OBSVirtualCamOutput.BgraToNv12(virtualFrame, _frameWidth, _frameHeight);
            
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
            for (int c = 0; c < 3; c++) // B, G, R channels
            {
                float value = bgra[i + c];
                
                // Apply contrast (centered at 128)
                value = (value - 128) * contrastFactor + 128;
                
                // Apply brightness
                value += brightnessFactor;
                
                bgra[i + c] = (byte)Math.Clamp(value, 0, 255);
            }
        }
    }
    
    public void Dispose()
    {
        StopVirtualCamera();
        DisconnectAsync().Wait();
    }
}
