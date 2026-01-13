using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Sends frames to OBS Virtual Camera via shared memory.
/// Based on OBS Studio's shared-memory-queue implementation.
/// </summary>
public class OBSVirtualCamOutput : IDisposable
{
    private const string VIDEO_NAME = "OBSVirtualCamVideo";
    private const int FRAME_HEADER_SIZE = 32;
    
    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private bool _isRunning;
    private uint _width;
    private uint _height;
    private uint _writeIdx;
    
    // Queue header structure offsets
    private const int OFFSET_WRITE_IDX = 0;
    private const int OFFSET_READ_IDX = 4;
    private const int OFFSET_STATE = 8;
    private const int OFFSET_FRAME_OFFSETS = 12; // 3 x uint32
    private const int OFFSET_TYPE = 24;
    private const int OFFSET_CX = 28;
    private const int OFFSET_CY = 32;
    private const int OFFSET_INTERVAL = 36;
    private const int HEADER_SIZE = 128; // Aligned header size
    
    // Queue states
    private const uint STATE_INVALID = 0;
    private const uint STATE_STARTING = 1;
    private const uint STATE_READY = 2;
    private const uint STATE_STOPPING = 3;
    
    public bool IsRunning => _isRunning;
    public uint Width => _width;
    public uint Height => _height;
    
    public event Action<string>? OnLog;
    
    private void Log(string message) => OnLog?.Invoke(message);
    
    public bool Start(uint width, uint height, double fps)
    {
        try
        {
            _width = width;
            _height = height;
            
            // NV12 frame size: width * height * 1.5
            uint frameSize = width * height * 3 / 2;
            
            // Calculate offsets for triple buffering
            uint[] frameOffsets = new uint[3];
            uint size = HEADER_SIZE;
            
            for (int i = 0; i < 3; i++)
            {
                frameOffsets[i] = size;
                size += frameSize + FRAME_HEADER_SIZE;
                size = AlignSize(size, 32);
            }
            
            // Check if already exists (another instance running)
            try
            {
                using var existing = MemoryMappedFile.OpenExisting(VIDEO_NAME, MemoryMappedFileRights.Read);
                Log("❌ OBS Virtual Camera already in use by another application");
                return false;
            }
            catch (System.IO.FileNotFoundException)
            {
                // Good - not in use
            }
            
            // Create shared memory
            _mmf = MemoryMappedFile.CreateNew(VIDEO_NAME, size, MemoryMappedFileAccess.ReadWrite);
            _accessor = _mmf.CreateViewAccessor(0, size, MemoryMappedFileAccess.ReadWrite);
            
            // Write header
            _accessor.Write(OFFSET_STATE, STATE_STARTING);
            _accessor.Write(OFFSET_CX, width);
            _accessor.Write(OFFSET_CY, height);
            
            ulong interval = (ulong)(10000000.0 / fps);
            _accessor.Write(OFFSET_INTERVAL, interval);
            
            // Write frame offsets
            for (int i = 0; i < 3; i++)
            {
                _accessor.Write(OFFSET_FRAME_OFFSETS + i * 4, frameOffsets[i]);
            }
            
            _writeIdx = 0;
            _isRunning = true;
            
            Log($"✅ OBS Virtual Camera output started: {width}x{height} @ {fps}fps");
            return true;
        }
        catch (Exception ex)
        {
            Log($"❌ Failed to start OBS output: {ex.Message}");
            return false;
        }
    }
    
    public void SendFrame(byte[] nv12Data, ulong timestamp)
    {
        if (!_isRunning || _accessor == null) return;
        
        try
        {
            uint idx = _writeIdx % 3;
            
            // Get frame offset from header
            uint frameOffset = _accessor.ReadUInt32(OFFSET_FRAME_OFFSETS + (int)(idx * 4));
            
            // Write timestamp
            _accessor.Write((long)frameOffset, timestamp);
            
            // Write frame data (NV12 format)
            _accessor.WriteArray((long)(frameOffset + FRAME_HEADER_SIZE), nv12Data, 0, nv12Data.Length);
            
            // Update indices
            _writeIdx++;
            _accessor.Write(OFFSET_WRITE_IDX, _writeIdx);
            _accessor.Write(OFFSET_READ_IDX, _writeIdx);
            _accessor.Write(OFFSET_STATE, STATE_READY);
        }
        catch (Exception ex)
        {
            Log($"⚠️ Frame send error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Convert BGRA frame to NV12 format required by OBS Virtual Camera
    /// </summary>
    public static byte[] BgraToNv12(byte[] bgra, int width, int height)
    {
        int ySize = width * height;
        int uvSize = ySize / 2;
        byte[] nv12 = new byte[ySize + uvSize];
        
        // Convert BGRA to NV12 (Y plane + interleaved UV plane)
        int bgraStride = width * 4;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int bgraIdx = y * bgraStride + x * 4;
                int b = bgra[bgraIdx];
                int g = bgra[bgraIdx + 1];
                int r = bgra[bgraIdx + 2];
                
                // Y component
                int yVal = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
                nv12[y * width + x] = (byte)Math.Clamp(yVal, 0, 255);
                
                // UV components (subsampled 2x2)
                if (y % 2 == 0 && x % 2 == 0)
                {
                    int uvIdx = ySize + (y / 2) * width + x;
                    int u = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                    int v = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;
                    nv12[uvIdx] = (byte)Math.Clamp(u, 0, 255);
                    nv12[uvIdx + 1] = (byte)Math.Clamp(v, 0, 255);
                }
            }
        }
        
        return nv12;
    }
    
    private static uint AlignSize(uint size, uint alignment)
    {
        return (size + alignment - 1) & ~(alignment - 1);
    }
    
    public void Stop()
    {
        if (!_isRunning) return;
        
        try
        {
            if (_accessor != null)
            {
                _accessor.Write(OFFSET_STATE, STATE_STOPPING);
            }
        }
        catch { }
        
        _isRunning = false;
        
        _accessor?.Dispose();
        _accessor = null;
        
        _mmf?.Dispose();
        _mmf = null;
        
        Log("OBS Virtual Camera output stopped");
    }
    
    public void Dispose()
    {
        Stop();
    }
}
