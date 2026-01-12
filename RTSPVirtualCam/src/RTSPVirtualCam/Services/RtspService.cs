using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace RTSPVirtualCam.Services;

public class RtspService : IRtspService, IDisposable
{
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private Media? _media;
    
    public event EventHandler<RtspConnectionEventArgs>? ConnectionStateChanged;
    public event EventHandler<FrameEventArgs>? FrameReceived;
    
    public bool IsConnected => _mediaPlayer?.IsPlaying ?? false;
    
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
        Core.Initialize();
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
    
    public void Dispose()
    {
        DisconnectAsync().Wait();
    }
}
