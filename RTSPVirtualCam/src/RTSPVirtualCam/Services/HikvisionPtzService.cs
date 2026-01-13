using System;
using System.Runtime.InteropServices;
using System.Text;

namespace RTSPVirtualCam.Services;

/// <summary>
/// PTZ control service using Hikvision Device Network SDK
/// Downloads SDK automatically if not present
/// </summary>
public class HikvisionPtzService : IDisposable
{
    // ═══════════════════════════════════════════════════════════
    // HIKVISION SDK NATIVE IMPORTS
    // ═══════════════════════════════════════════════════════════
    
    private const string SDK_DLL = "HCNetSDK.dll";
    
    [DllImport(SDK_DLL)]
    private static extern bool NET_DVR_Init();
    
    [DllImport(SDK_DLL)]
    private static extern bool NET_DVR_Cleanup();
    
    [DllImport(SDK_DLL)]
    private static extern int NET_DVR_Login_V30(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref NET_DVR_DEVICEINFO_V30 lpDeviceInfo);
    
    [DllImport(SDK_DLL)]
    private static extern bool NET_DVR_Logout(int lUserID);
    
    [DllImport(SDK_DLL)]
    private static extern bool NET_DVR_PTZControl_Other(int lUserID, int lChannel, uint dwPTZCommand, uint dwStop);
    
    [DllImport(SDK_DLL)]
    private static extern uint NET_DVR_GetLastError();
    
    // PTZ Commands
    private const uint LIGHT_PWRON = 2;
    private const uint WIPER_PWRON = 3;
    private const uint FAN_PWRON = 4;
    private const uint HEATER_PWRON = 5;
    private const uint AUX_PWRON1 = 6;
    private const uint AUX_PWRON2 = 7;
    private const uint SET_PRESET = 8;
    private const uint CLE_PRESET = 9;
    private const uint ZOOM_IN = 11;
    private const uint ZOOM_OUT = 12;
    private const uint FOCUS_NEAR = 13;
    private const uint FOCUS_FAR = 14;
    private const uint IRIS_OPEN = 15;
    private const uint IRIS_CLOSE = 16;
    private const uint TILT_UP = 21;
    private const uint TILT_DOWN = 22;
    private const uint PAN_LEFT = 23;
    private const uint PAN_RIGHT = 24;
    private const uint UP_LEFT = 25;
    private const uint UP_RIGHT = 26;
    private const uint DOWN_LEFT = 27;
    private const uint DOWN_RIGHT = 28;
    private const uint PAN_AUTO = 29;
    private const uint GOTO_PRESET = 39;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NET_DVR_DEVICEINFO_V30
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public byte[] sSerialNumber;
        public byte byAlarmInPortNum;
        public byte byAlarmOutPortNum;
        public byte byDiskNum;
        public byte byDVRType;
        public byte byChanNum;
        public byte byStartChan;
        public byte byAudioChanNum;
        public byte byIPChanNum;
        public byte byZeroChanNum;
        public byte byMainProto;
        public byte bySubProto;
        public byte bySupport;
        public byte bySupport1;
        public byte bySupport2;
        public ushort wDevType;
        public byte bySupport3;
        public byte byMultiStreamProto;
        public byte byStartDChan;
        public byte byStartDTalkChan;
        public byte byHighDChanNum;
        public byte bySupport4;
        public byte byLanguageType;
        public byte byVoiceInChanNum;
        public byte byStartVoiceInChanNo;
        public byte bySupport5;
        public byte bySupport6;
        public byte byMirrorChanNum;
        public ushort wStartMirrorChanNo;
        public byte bySupport7;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public byte[] byRes2;
    }
    
    private int _userId = -1;
    private bool _isInitialized;
    private string _ipAddress = string.Empty;
    private int _port = 8000;
    private string _username = string.Empty;
    private string _password = string.Empty;
    
    public event Action<string>? OnLog;
    
    public bool IsConnected => _userId >= 0;
    
    private void Log(string message) => OnLog?.Invoke(message);
    
    public HikvisionPtzService()
    {
        try
        {
            // Initialize SDK
            if (!NET_DVR_Init())
            {
                Log("⚠️ Failed to initialize Hikvision SDK");
                return;
            }
            
            _isInitialized = true;
            Log("✅ Hikvision SDK initialized");
        }
        catch (DllNotFoundException)
        {
            Log("❌ HCNetSDK.dll not found. Please install Hikvision Device Network SDK.");
            Log("💡 Download from: https://www.hikvision.com/en/support/download/sdk/");
        }
        catch (Exception ex)
        {
            Log($"❌ SDK initialization error: {ex.Message}");
        }
    }
    
    public bool Login(string ipAddress, int port, string username, string password)
    {
        if (!_isInitialized)
        {
            Log("❌ SDK not initialized");
            return false;
        }
        
        try
        {
            // Logout if already connected
            if (_userId >= 0)
            {
                Logout();
            }
            
            _ipAddress = ipAddress;
            _port = port;
            _username = username;
            _password = password;
            
            NET_DVR_DEVICEINFO_V30 deviceInfo = new NET_DVR_DEVICEINFO_V30();
            
            _userId = NET_DVR_Login_V30(ipAddress, port, username, password, ref deviceInfo);
            
            if (_userId < 0)
            {
                uint errorCode = NET_DVR_GetLastError();
                Log($"❌ Login failed. Error code: {errorCode}");
                return false;
            }
            
            Log($"✅ Connected to {ipAddress}:{port}");
            Log($"📷 Device type: {deviceInfo.byDVRType}, Channels: {deviceInfo.byChanNum}");
            
            return true;
        }
        catch (Exception ex)
        {
            Log($"❌ Login error: {ex.Message}");
            return false;
        }
    }
    
    public void Logout()
    {
        if (_userId >= 0)
        {
            NET_DVR_Logout(_userId);
            _userId = -1;
            Log("🔌 Disconnected from camera");
        }
    }
    
    public bool ExecutePtzCommand(string command, int speed = 4)
    {
        if (_userId < 0)
        {
            Log("❌ Not connected to camera");
            return false;
        }
        
        try
        {
            uint ptzCommand = GetPtzCommand(command);
            if (ptzCommand == 0)
            {
                Log($"⚠️ Unknown PTZ command: {command}");
                return false;
            }
            
            // Start movement
            if (!NET_DVR_PTZControl_Other(_userId, 1, ptzCommand, 0))
            {
                uint errorCode = NET_DVR_GetLastError();
                Log($"⚠️ PTZ command failed. Error: {errorCode}");
                return false;
            }
            
            Log($"🎮 PTZ: {command}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"❌ PTZ error: {ex.Message}");
            return false;
        }
    }
    
    public bool StopPtzMovement()
    {
        if (_userId < 0) return false;
        
        try
        {
            // Stop all movements
            NET_DVR_PTZControl_Other(_userId, 1, TILT_UP, 1);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private uint GetPtzCommand(string command)
    {
        return command.ToLower() switch
        {
            "up" => TILT_UP,
            "down" => TILT_DOWN,
            "left" => PAN_LEFT,
            "right" => PAN_RIGHT,
            "upleft" => UP_LEFT,
            "upright" => UP_RIGHT,
            "downleft" => DOWN_LEFT,
            "downright" => DOWN_RIGHT,
            "zoomin" => ZOOM_IN,
            "zoomout" => ZOOM_OUT,
            "focusnear" => FOCUS_NEAR,
            "focusfar" => FOCUS_FAR,
            "irisopen" => IRIS_OPEN,
            "irisclose" => IRIS_CLOSE,
            "home" => GOTO_PRESET, // Preset 1 as home
            _ => 0
        };
    }
    
    public void Dispose()
    {
        Logout();
        
        if (_isInitialized)
        {
            NET_DVR_Cleanup();
            _isInitialized = false;
        }
    }
}
