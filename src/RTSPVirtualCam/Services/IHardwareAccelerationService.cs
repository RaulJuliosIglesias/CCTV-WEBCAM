using System;
using System.Collections.Generic;
using RTSPVirtualCam.Models;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Interface for hardware acceleration management (DXVA2, D3D11VA, etc.).
/// </summary>
public interface IHardwareAccelerationService : IDisposable
{
    event Action<string>? OnLog;

    // Capabilities
    bool IsSupported { get; }
    HardwareAccelerationType CurrentType { get; }
    List<HardwareAccelerationType> AvailableTypes { get; }
    HardwareAccelerationInfo GetInfo();

    // Configuration
    bool Enable(HardwareAccelerationType type = HardwareAccelerationType.Auto);
    void Disable();
    bool IsEnabled { get; }

    // Performance monitoring
    HardwarePerformanceStats GetPerformanceStats();
    double GetGpuUsagePercent();
    long GetGpuMemoryUsedMB();

    // LibVLC options for hardware acceleration
    string[] GetLibVlcOptions();
}

public class HardwareAccelerationInfo
{
    public bool IsSupported { get; set; }
    public HardwareAccelerationType RecommendedType { get; set; }
    public string GpuName { get; set; } = string.Empty;
    public string GpuVendor { get; set; } = string.Empty;
    public long GpuMemoryMB { get; set; }
    public string DriverVersion { get; set; } = string.Empty;
    public bool SupportsDXVA2 { get; set; }
    public bool SupportsD3D11VA { get; set; }
    public bool SupportsCUDA { get; set; }
    public bool SupportsQSV { get; set; }
    public List<string> SupportedCodecs { get; set; } = new();
}

public class HardwarePerformanceStats
{
    public double GpuUsagePercent { get; set; }
    public long GpuMemoryUsedMB { get; set; }
    public long GpuMemoryTotalMB { get; set; }
    public double CpuUsagePercent { get; set; }
    public int DecodedFrames { get; set; }
    public int DroppedFrames { get; set; }
    public double AverageDecodeTimeMs { get; set; }
    public double AverageRenderTimeMs { get; set; }
    public bool IsHardwareDecoding { get; set; }
    public string ActiveDecoder { get; set; } = string.Empty;
}
