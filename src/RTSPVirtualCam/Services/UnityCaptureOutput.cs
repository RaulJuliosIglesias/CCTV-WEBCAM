using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Sends frames to Unity Capture virtual camera using direct shared memory.
/// Unity Capture supports multiple virtual cameras (up to 10).
/// Each camera uses its own shared memory identified by device number.
/// Shared memory naming: UnityCapture_Data0, UnityCapture_Data1, etc.
/// </summary>
public class UnityCaptureOutput : IDisposable
{
    // Windows API for shared memory
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr OpenFileMappingA(uint dwDesiredAccess, bool bInheritHandle, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr CreateFileMappingA(IntPtr hFile, IntPtr lpAttributes, uint flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr OpenMutexA(uint dwDesiredAccess, bool bInheritHandle, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr CreateMutexA(IntPtr lpMutexAttributes, bool bInitialOwner, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReleaseMutex(IntPtr hMutex);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr OpenEventA(uint dwDesiredAccess, bool bInheritHandle, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr CreateEventA(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetEvent(IntPtr hEvent);
    
    // Constants
    private const uint FILE_MAP_WRITE = 0x0002;
    private const uint PAGE_READWRITE = 0x04;
    private const uint SYNCHRONIZE = 0x00100000;
    private const uint EVENT_MODIFY_STATE = 0x0002;
    private const uint INFINITE = 0xFFFFFFFF;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
    
    // Max size for 4K resolution (RGBA 16-bit per pixel)
    private const int MAX_SHARED_IMAGE_SIZE = 3840 * 2160 * 4 * 2;
    
    // Shared memory header structure (matches Unity Capture shared.inl)
    [StructLayout(LayoutKind.Sequential)]
    private struct SharedMemHeader
    {
        public uint maxSize;
        public int width;
        public int height;
        public int stride;
        public int format;
        public int resizemode;
        public int mirrormode;
        public int timeout;
        // data follows after this header
    }
    
    private int _deviceNumber;
    private string _deviceName = string.Empty;
    private int _width;
    private int _height;
    private bool _isRunning;
    
    private IntPtr _hMutex = IntPtr.Zero;
    private IntPtr _hWantFrameEvent = IntPtr.Zero;
    private IntPtr _hSentFrameEvent = IntPtr.Zero;
    private IntPtr _hSharedFile = IntPtr.Zero;
    private IntPtr _pSharedBuf = IntPtr.Zero;
    
    private long _frameCount;
    
    public bool IsRunning => _isRunning;
    public string DeviceName => _deviceName;
    public int DeviceNumber => _deviceNumber;
    
    public event Action<string>? OnLog;
    
    private void Log(string message) => OnLog?.Invoke(message);
    
    /// <summary>
    /// Start sending frames to a Unity Capture virtual camera device.
    /// </summary>
    /// <param name="deviceNumber">Device number (0-9). Each number is a separate virtual camera.</param>
    /// <param name="width">Frame width</param>
    /// <param name="height">Frame height</param>
    public bool Start(int deviceNumber, int width, int height)
    {
        try
        {
            _deviceNumber = deviceNumber;
            _deviceName = deviceNumber == 0 ? "Unity Video Capture" : $"Unity Video Capture #{deviceNumber}";
            _width = width;
            _height = height;
            _frameCount = 0;
            
            Log($"🎥 Initializing Unity Capture device #{deviceNumber}...");
            
            // Build shared memory names based on device number
            char capNumChar = deviceNumber == 0 ? '\0' : (char)('0' + deviceNumber);
            string mutexName = deviceNumber == 0 ? "UnityCapture_Mutx" : $"UnityCapture_Mutx{capNumChar}";
            string wantEventName = deviceNumber == 0 ? "UnityCapture_Want" : $"UnityCapture_Want{capNumChar}";
            string sentEventName = deviceNumber == 0 ? "UnityCapture_Sent" : $"UnityCapture_Sent{capNumChar}";
            string sharedDataName = deviceNumber == 0 ? "UnityCapture_Data" : $"UnityCapture_Data{capNumChar}";
            
            // Open or create mutex
            _hMutex = OpenMutexA(SYNCHRONIZE, false, mutexName);
            if (_hMutex == IntPtr.Zero)
            {
                Log($"⚠️ Unity Capture device #{deviceNumber} not active (no receiver)");
                Log($"💡 Make sure a video app is requesting the camera");
                // We can still create the shared memory and wait for a receiver
            }
            
            // Create events for frame synchronization
            _hWantFrameEvent = CreateEventA(IntPtr.Zero, false, false, wantEventName);
            _hSentFrameEvent = OpenEventA(EVENT_MODIFY_STATE, false, sentEventName);
            
            // Create shared memory
            int totalSize = Marshal.SizeOf<SharedMemHeader>() + MAX_SHARED_IMAGE_SIZE;
            _hSharedFile = CreateFileMappingA(INVALID_HANDLE_VALUE, IntPtr.Zero, PAGE_READWRITE, 0, (uint)totalSize, sharedDataName);
            
            if (_hSharedFile == IntPtr.Zero)
            {
                Log($"❌ Failed to create shared memory for device #{deviceNumber}");
                return false;
            }
            
            _pSharedBuf = MapViewOfFile(_hSharedFile, FILE_MAP_WRITE, 0, 0, UIntPtr.Zero);
            
            if (_pSharedBuf == IntPtr.Zero)
            {
                Log($"❌ Failed to map shared memory for device #{deviceNumber}");
                CloseHandle(_hSharedFile);
                _hSharedFile = IntPtr.Zero;
                return false;
            }
            
            // Initialize header
            var header = new SharedMemHeader
            {
                maxSize = MAX_SHARED_IMAGE_SIZE,
                width = 0,
                height = 0,
                stride = 0,
                format = 0, // FORMAT_UINT8
                resizemode = 0,
                mirrormode = 0,
                timeout = 1000
            };
            Marshal.StructureToPtr(header, _pSharedBuf, false);
            
            _isRunning = true;
            Log($"✅ Unity Capture device #{deviceNumber} ready");
            Log($"📐 Resolution: {width}x{height}");
            Log($"📝 Shared memory: {sharedDataName}");
            
            return true;
        }
        catch (Exception ex)
        {
            Log($"❌ Unity Capture error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Send a BGRA frame to the virtual camera.
    /// </summary>
    public void SendFrame(byte[] bgraData)
    {
        if (!_isRunning || _pSharedBuf == IntPtr.Zero)
            return;
        
        try
        {
            int dataSize = _width * _height * 4;
            if (bgraData.Length < dataSize)
                return;
            
            // Lock mutex if available
            if (_hMutex != IntPtr.Zero)
            {
                WaitForSingleObject(_hMutex, INFINITE);
            }
            
            try
            {
                // Write header
                int headerSize = Marshal.SizeOf<SharedMemHeader>();
                Marshal.WriteInt32(_pSharedBuf, 4, _width);  // width at offset 4
                Marshal.WriteInt32(_pSharedBuf, 8, _height); // height at offset 8
                Marshal.WriteInt32(_pSharedBuf, 12, _width * 4); // stride at offset 12
                Marshal.WriteInt32(_pSharedBuf, 16, 0); // format = FORMAT_UINT8
                Marshal.WriteInt32(_pSharedBuf, 20, 0); // resizemode
                Marshal.WriteInt32(_pSharedBuf, 24, 0); // mirrormode
                Marshal.WriteInt32(_pSharedBuf, 28, 1000); // timeout
                
                // Write frame data after header
                IntPtr dataPtr = IntPtr.Add(_pSharedBuf, headerSize);
                Marshal.Copy(bgraData, 0, dataPtr, dataSize);
            }
            finally
            {
                // Unlock mutex
                if (_hMutex != IntPtr.Zero)
                {
                    ReleaseMutex(_hMutex);
                }
            }
            
            // Signal that frame is ready
            if (_hSentFrameEvent != IntPtr.Zero)
            {
                SetEvent(_hSentFrameEvent);
            }
            
            _frameCount++;
            if (_frameCount % 100 == 0)
            {
                Log($"📹 [{_deviceName}] Sent {_frameCount} frames");
            }
        }
        catch (Exception ex)
        {
            Log($"⚠️ Frame send error: {ex.Message}");
        }
    }
    
    public void Stop()
    {
        _isRunning = false;
        
        if (_pSharedBuf != IntPtr.Zero)
        {
            UnmapViewOfFile(_pSharedBuf);
            _pSharedBuf = IntPtr.Zero;
        }
        
        if (_hSharedFile != IntPtr.Zero)
        {
            CloseHandle(_hSharedFile);
            _hSharedFile = IntPtr.Zero;
        }
        
        if (_hMutex != IntPtr.Zero)
        {
            CloseHandle(_hMutex);
            _hMutex = IntPtr.Zero;
        }
        
        if (_hWantFrameEvent != IntPtr.Zero)
        {
            CloseHandle(_hWantFrameEvent);
            _hWantFrameEvent = IntPtr.Zero;
        }
        
        if (_hSentFrameEvent != IntPtr.Zero)
        {
            CloseHandle(_hSentFrameEvent);
            _hSentFrameEvent = IntPtr.Zero;
        }
        
        Log($"⏹ Unity Capture device #{_deviceNumber} stopped");
    }
    
    public void Dispose()
    {
        Stop();
    }
}

/// <summary>
/// Manages multiple Unity Capture virtual cameras.
/// </summary>
public class MultiCameraManager : IDisposable
{
    private readonly Dictionary<int, UnityCaptureOutput> _cameras = new();
    
    public event Action<string>? OnLog;
    
    private void Log(string message) => OnLog?.Invoke(message);
    
    /// <summary>
    /// Create a new virtual camera with the specified device number.
    /// </summary>
    public UnityCaptureOutput? CreateCamera(int deviceNumber, int width, int height)
    {
        if (_cameras.ContainsKey(deviceNumber))
        {
            Log($"⚠️ Camera #{deviceNumber} already exists");
            return _cameras[deviceNumber];
        }
        
        var camera = new UnityCaptureOutput();
        camera.OnLog += msg => OnLog?.Invoke(msg);
        
        if (camera.Start(deviceNumber, width, height))
        {
            _cameras[deviceNumber] = camera;
            return camera;
        }
        
        camera.Dispose();
        return null;
    }
    
    /// <summary>
    /// Get an existing camera by device number.
    /// </summary>
    public UnityCaptureOutput? GetCamera(int deviceNumber)
    {
        return _cameras.TryGetValue(deviceNumber, out var camera) ? camera : null;
    }
    
    /// <summary>
    /// Send frame to all active cameras.
    /// </summary>
    public void SendFrameToAll(byte[] bgraData)
    {
        foreach (var camera in _cameras.Values)
        {
            camera.SendFrame(bgraData);
        }
    }
    
    /// <summary>
    /// Remove a camera.
    /// </summary>
    public void RemoveCamera(int deviceNumber)
    {
        if (_cameras.TryGetValue(deviceNumber, out var camera))
        {
            camera.Dispose();
            _cameras.Remove(deviceNumber);
        }
    }
    
    /// <summary>
    /// Get list of active device numbers.
    /// </summary>
    public IEnumerable<int> GetActiveDevices() => _cameras.Keys;
    
    /// <summary>
    /// Get count of active cameras.
    /// </summary>
    public int Count => _cameras.Count;
    
    public void Dispose()
    {
        foreach (var camera in _cameras.Values)
        {
            camera.Dispose();
        }
        _cameras.Clear();
    }
}
