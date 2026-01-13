using System;
using System.Runtime.InteropServices;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Sends frames to OBS Virtual Camera via shared memory.
/// Based on OBS Studio's shared-memory-queue implementation.
/// Uses native Windows API for proper cross-process memory mapping.
/// </summary>
public class OBSVirtualCamOutput : IDisposable
{
    private const string VIDEO_NAME = "OBSVirtualCamVideo";
    private const int FRAME_HEADER_SIZE = 32;
    
    // Native Windows API
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFileMappingW(
        IntPtr hFile, IntPtr lpAttributes, uint flProtect,
        uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr OpenFileMappingW(uint dwDesiredAccess, bool bInheritHandle, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr MapViewOfFile(
        IntPtr hFileMappingObject, uint dwDesiredAccess,
        uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    
    private const uint PAGE_READWRITE = 0x04;
    private const uint FILE_MAP_ALL_ACCESS = 0xF001F;
    private const uint FILE_MAP_READ = 0x0004;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    
    private IntPtr _hMapFile = IntPtr.Zero;
    private IntPtr _pBuf = IntPtr.Zero;
    private bool _isRunning;
    private uint _width;
    private uint _height;
    private uint _writeIdx;
    private uint _totalSize;
    private uint[] _frameOffsets = new uint[3];
    
    // Queue header structure offsets (must match OBS exactly)
    // struct queue_header layout in C with alignment:
    private const int OFFSET_WRITE_IDX = 0;      // uint32_t write_idx
    private const int OFFSET_READ_IDX = 4;       // uint32_t read_idx  
    private const int OFFSET_STATE = 8;          // uint32_t state
    private const int OFFSET_FRAME_OFFSETS = 12; // uint32_t offsets[3] - 12 bytes
    private const int OFFSET_TYPE = 24;          // uint32_t type
    private const int OFFSET_CX = 28;            // uint32_t cx
    private const int OFFSET_CY = 32;            // uint32_t cy
    // 4 bytes padding here for uint64_t alignment!
    private const int OFFSET_INTERVAL = 40;      // uint64_t interval (aligned to 8 bytes)
    // reserved[8] starts at 48, 32 bytes = ends at 80
    // Total header size aligned to 32: 96 bytes
    
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
            
            // Calculate size exactly like OBS does
            // Header: 48 bytes (with padding) + 32 bytes reserved = 80 bytes
            // Aligned to 32 bytes = 96 bytes
            uint size = 96; // sizeof(queue_header) with alignment
            
            // Calculate offsets for triple buffering
            for (int i = 0; i < 3; i++)
            {
                _frameOffsets[i] = size;
                size += frameSize + FRAME_HEADER_SIZE;
                size = AlignSize(size, 32);
            }
            
            _totalSize = size;
            
            Log($"📊 Shared memory size: {size} bytes");
            Log($"📊 Frame offsets: [{_frameOffsets[0]}, {_frameOffsets[1]}, {_frameOffsets[2]}]");
            
            // Try to create shared memory using Windows API
            // If it already exists, we'll take over (OBS may have left it or we're replacing another sender)
            _hMapFile = CreateFileMappingW(
                INVALID_HANDLE_VALUE,
                IntPtr.Zero,
                PAGE_READWRITE,
                0,
                size,
                VIDEO_NAME);
            
            int createError = Marshal.GetLastWin32Error();
            bool alreadyExisted = (createError == 183); // ERROR_ALREADY_EXISTS
            
            if (_hMapFile == IntPtr.Zero)
            {
                // Try to open existing one instead
                _hMapFile = OpenFileMappingW(FILE_MAP_ALL_ACCESS, false, VIDEO_NAME);
                if (_hMapFile == IntPtr.Zero)
                {
                    Log($"❌ CreateFileMapping failed: error {createError}");
                    return false;
                }
                Log($"📂 Opened existing shared memory: {VIDEO_NAME}");
            }
            else if (alreadyExisted)
            {
                Log($"📂 Taking over existing shared memory: {VIDEO_NAME}");
            }
            else
            {
                Log($"✅ Created new shared memory: {VIDEO_NAME}");
            }
            
            // Map view of file
            _pBuf = MapViewOfFile(_hMapFile, FILE_MAP_ALL_ACCESS, 0, 0, UIntPtr.Zero);
            
            if (_pBuf == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                CloseHandle(_hMapFile);
                _hMapFile = IntPtr.Zero;
                Log($"❌ MapViewOfFile failed: error {error}");
                return false;
            }
            
            Log($"✅ Mapped memory at: 0x{_pBuf:X}");
            
            // Initialize header (write directly to memory)
            WriteUInt32(OFFSET_WRITE_IDX, 0);
            WriteUInt32(OFFSET_READ_IDX, 0);
            WriteUInt32(OFFSET_STATE, STATE_STARTING);
            
            // Write frame offsets
            for (int i = 0; i < 3; i++)
            {
                WriteUInt32(OFFSET_FRAME_OFFSETS + i * 4, _frameOffsets[i]);
            }
            
            WriteUInt32(OFFSET_TYPE, 0); // SHARED_QUEUE_TYPE_VIDEO
            WriteUInt32(OFFSET_CX, width);
            WriteUInt32(OFFSET_CY, height);
            
            ulong interval = (ulong)(10000000.0 / fps);
            WriteUInt64(OFFSET_INTERVAL, interval);
            
            _writeIdx = 0;
            _isRunning = true;
            
            // Write config file that OBS Virtual Camera filter reads for resolution
            WriteConfigFile(width, height, interval);
            
            Log($"✅ OBS Virtual Camera output started: {width}x{height} @ {fps}fps");
            Log($"📋 Header state: {ReadUInt32(OFFSET_STATE)}");
            Log($"📋 Header cx: {ReadUInt32(OFFSET_CX)}, cy: {ReadUInt32(OFFSET_CY)}");
            Log($"💡 OBS Virtual Camera is now receiving frames from this app");
            Log($"💡 Other apps (Chrome, Zoom) can read from the virtual camera");
            return true;
        }
        catch (Exception ex)
        {
            Log($"❌ Failed to start OBS output: {ex.Message}");
            return false;
        }
    }
    
    private void WriteUInt32(int offset, uint value)
    {
        Marshal.WriteInt32(_pBuf + offset, (int)value);
    }
    
    private void WriteUInt64(int offset, ulong value)
    {
        Marshal.WriteInt64(_pBuf + offset, (long)value);
    }
    
    private uint ReadUInt32(int offset)
    {
        return (uint)Marshal.ReadInt32(_pBuf + offset);
    }
    
    private void WriteConfigFile(uint width, uint height, ulong interval)
    {
        try
        {
            // OBS Virtual Camera filter reads this file for resolution info
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configPath = System.IO.Path.Combine(appData, "obs-virtualcam.txt");
            string content = $"{width}x{height}x{interval}";
            System.IO.File.WriteAllText(configPath, content);
            Log($"📝 Config file written: {configPath}");
        }
        catch (Exception ex)
        {
            Log($"⚠️ Could not write config file: {ex.Message}");
        }
    }
    
    private long _frameSentCount = 0;
    
    public void SendFrame(byte[] nv12Data, ulong timestamp)
    {
        if (!_isRunning || _pBuf == IntPtr.Zero) return;
        
        try
        {
            // Increment write index first (like OBS does)
            _writeIdx++;
            uint idx = _writeIdx % 3;
            
            // Get frame offset
            uint frameOffset = _frameOffsets[idx];
            
            // Write timestamp at frame header
            Marshal.WriteInt64(_pBuf + (int)frameOffset, (long)timestamp);
            
            // Write frame data (NV12 format) after header
            Marshal.Copy(nv12Data, 0, _pBuf + (int)frameOffset + FRAME_HEADER_SIZE, nv12Data.Length);
            
            // Update write index in header
            WriteUInt32(OFFSET_WRITE_IDX, _writeIdx);
            
            // Update read index to match write index (signal new frame available)
            WriteUInt32(OFFSET_READ_IDX, _writeIdx);
            
            // Set state to READY
            WriteUInt32(OFFSET_STATE, STATE_READY);
            
            _frameSentCount++;
            
            // Log state every 100 frames
            if (_frameSentCount % 100 == 0)
            {
                uint currentState = ReadUInt32(OFFSET_STATE);
                uint readIdx = ReadUInt32(OFFSET_READ_IDX);
                Log($"📊 Frame #{_frameSentCount}: state={currentState}, readIdx={readIdx}, writeIdx={_writeIdx}");
            }
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
            if (_pBuf != IntPtr.Zero)
            {
                WriteUInt32(OFFSET_STATE, STATE_STOPPING);
            }
        }
        catch { }
        
        _isRunning = false;
        
        if (_pBuf != IntPtr.Zero)
        {
            UnmapViewOfFile(_pBuf);
            _pBuf = IntPtr.Zero;
        }
        
        if (_hMapFile != IntPtr.Zero)
        {
            CloseHandle(_hMapFile);
            _hMapFile = IntPtr.Zero;
        }
        
        Log("OBS Virtual Camera output stopped");
    }
    
    public void Dispose()
    {
        Stop();
    }
}
