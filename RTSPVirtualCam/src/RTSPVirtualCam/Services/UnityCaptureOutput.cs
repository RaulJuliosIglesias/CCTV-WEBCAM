using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Sends frames to Unity Capture virtual camera.
/// Unity Capture supports multiple virtual cameras with custom names.
/// Each camera uses its own shared memory identified by device number.
/// </summary>
public class UnityCaptureOutput : IDisposable
{
    // Unity Capture Plugin DLL imports
    [DllImport("UnityCapturePlugin.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int UnityCaptureInterface_Query([MarshalAs(UnmanagedType.LPWStr)] string deviceName);
    
    [DllImport("UnityCapturePlugin.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int UnityCaptureInterface_SendFrame(
        int deviceId,
        IntPtr data,
        int width,
        int height,
        int stride,
        int format,
        int mirrorHorizontal,
        int mirrorVertical,
        int timeout);
    
    // Format constants for Unity Capture
    private const int FORMAT_ARGB = 0;
    private const int FORMAT_BGRA = 1;
    private const int FORMAT_RGBA = 2;
    private const int FORMAT_RGB24 = 3;
    private const int FORMAT_BGR24 = 4;
    
    private int _deviceId = -1;
    private string _deviceName = string.Empty;
    private int _width;
    private int _height;
    private bool _isRunning;
    private IntPtr _frameBuffer = IntPtr.Zero;
    private int _bufferSize;
    
    public bool IsRunning => _isRunning;
    public string DeviceName => _deviceName;
    public int DeviceId => _deviceId;
    
    public event Action<string>? OnLog;
    
    private void Log(string message) => OnLog?.Invoke(message);
    
    /// <summary>
    /// Start sending frames to a Unity Capture virtual camera device.
    /// </summary>
    /// <param name="deviceNumber">Device number (0-9). Each number creates a separate virtual camera.</param>
    /// <param name="width">Frame width</param>
    /// <param name="height">Frame height</param>
    public bool Start(int deviceNumber, int width, int height)
    {
        try
        {
            _deviceName = $"Unity Video Capture #{deviceNumber}";
            _width = width;
            _height = height;
            
            Log($"🎥 Initializing Unity Capture device #{deviceNumber}...");
            
            // Query the device to get its ID
            _deviceId = UnityCaptureInterface_Query(_deviceName);
            
            if (_deviceId < 0)
            {
                Log($"⚠️ Device not registered. Trying default device name...");
                // Try with default Unity Capture device name format
                _deviceName = deviceNumber == 0 ? "Unity Video Capture" : $"Unity Video Capture #{deviceNumber}";
                _deviceId = UnityCaptureInterface_Query(_deviceName);
            }
            
            if (_deviceId < 0)
            {
                Log($"❌ Unity Capture device '{_deviceName}' not found");
                Log($"💡 Make sure Unity Capture is installed (run Install.bat)");
                return false;
            }
            
            // Allocate frame buffer for BGRA
            _bufferSize = width * height * 4;
            _frameBuffer = Marshal.AllocHGlobal(_bufferSize);
            
            _isRunning = true;
            Log($"✅ Unity Capture device ready: {_deviceName} (ID: {_deviceId})");
            Log($"📐 Resolution: {width}x{height}");
            
            return true;
        }
        catch (DllNotFoundException)
        {
            Log("❌ UnityCapturePlugin.dll not found");
            Log("💡 Make sure the DLL is in the scripts/softcam folder");
            return false;
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
        if (!_isRunning || _deviceId < 0 || _frameBuffer == IntPtr.Zero)
            return;
        
        try
        {
            // Copy frame data to unmanaged buffer
            Marshal.Copy(bgraData, 0, _frameBuffer, Math.Min(bgraData.Length, _bufferSize));
            
            // Send frame to Unity Capture (BGRA format, 1000ms timeout)
            int result = UnityCaptureInterface_SendFrame(
                _deviceId,
                _frameBuffer,
                _width,
                _height,
                _width * 4, // stride
                FORMAT_BGRA,
                0, // no horizontal mirror
                0, // no vertical mirror
                1000); // timeout ms
            
            if (result != 0)
            {
                // Non-zero result indicates error
                // Log($"⚠️ Send frame result: {result}");
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
        
        if (_frameBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_frameBuffer);
            _frameBuffer = IntPtr.Zero;
        }
        
        _deviceId = -1;
        Log($"Unity Capture device '{_deviceName}' stopped");
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
