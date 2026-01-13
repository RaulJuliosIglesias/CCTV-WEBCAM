using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Hardware acceleration service for improved video decoding performance.
/// Supports DXVA2, D3D11VA, CUDA, and Intel Quick Sync.
/// </summary>
public class HardwareAccelerationService : IHardwareAccelerationService
{
    private HardwareAccelerationType _currentType = HardwareAccelerationType.None;
    private HardwareAccelerationInfo? _cachedInfo;
    private bool _disposed;

    public event Action<string>? OnLog;

    public bool IsSupported => AvailableTypes.Count > 1; // More than just "None"
    public HardwareAccelerationType CurrentType => _currentType;
    public bool IsEnabled => _currentType != HardwareAccelerationType.None;

    public List<HardwareAccelerationType> AvailableTypes
    {
        get
        {
            var types = new List<HardwareAccelerationType> { HardwareAccelerationType.None };
            var info = GetInfo();

            if (info.SupportsDXVA2) types.Add(HardwareAccelerationType.DXVA2);
            if (info.SupportsD3D11VA) types.Add(HardwareAccelerationType.D3D11VA);
            if (info.SupportsCUDA) types.Add(HardwareAccelerationType.CUDA);
            if (info.SupportsQSV) types.Add(HardwareAccelerationType.QSV);

            if (types.Count > 1) types.Insert(0, HardwareAccelerationType.Auto);

            return types;
        }
    }

    public HardwareAccelerationInfo GetInfo()
    {
        if (_cachedInfo != null) return _cachedInfo;

        var info = new HardwareAccelerationInfo();

        try
        {
            // Query GPU information via WMI
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var gpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (gpu != null)
            {
                info.GpuName = gpu["Name"]?.ToString() ?? "Unknown";
                info.GpuVendor = gpu["AdapterCompatibility"]?.ToString() ?? "Unknown";
                info.DriverVersion = gpu["DriverVersion"]?.ToString() ?? "Unknown";

                var ramBytes = gpu["AdapterRAM"];
                if (ramBytes != null)
                {
                    info.GpuMemoryMB = Convert.ToInt64(ramBytes) / (1024 * 1024);
                }

                LogMessage($"GPU: {info.GpuName}");
                LogMessage($"Vendor: {info.GpuVendor}");
                LogMessage($"Memory: {info.GpuMemoryMB} MB");
                LogMessage($"Driver: {info.DriverVersion}");

                // Detect capabilities based on vendor
                var vendorLower = info.GpuVendor.ToLower();
                var nameLower = info.GpuName.ToLower();

                // DXVA2/D3D11VA - supported on most modern GPUs
                info.SupportsDXVA2 = IsWindowsVistaOrLater();
                info.SupportsD3D11VA = IsWindows8OrLater();

                // NVIDIA CUDA
                info.SupportsCUDA = vendorLower.Contains("nvidia") || nameLower.Contains("nvidia") || nameLower.Contains("geforce") || nameLower.Contains("quadro");

                // Intel Quick Sync
                info.SupportsQSV = vendorLower.Contains("intel") || nameLower.Contains("intel") || nameLower.Contains("hd graphics") || nameLower.Contains("uhd graphics") || nameLower.Contains("iris");

                // Determine recommended type
                if (info.SupportsCUDA)
                    info.RecommendedType = HardwareAccelerationType.CUDA;
                else if (info.SupportsQSV)
                    info.RecommendedType = HardwareAccelerationType.QSV;
                else if (info.SupportsD3D11VA)
                    info.RecommendedType = HardwareAccelerationType.D3D11VA;
                else if (info.SupportsDXVA2)
                    info.RecommendedType = HardwareAccelerationType.DXVA2;
                else
                    info.RecommendedType = HardwareAccelerationType.None;

                // Supported codecs (common hardware-accelerated codecs)
                info.SupportedCodecs = new List<string> { "H.264/AVC", "H.265/HEVC", "VP9", "AV1" };
                info.IsSupported = info.SupportsDXVA2 || info.SupportsD3D11VA || info.SupportsCUDA || info.SupportsQSV;
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to query GPU info: {ex.Message}");
        }

        _cachedInfo = info;
        return info;
    }

    public bool Enable(HardwareAccelerationType type = HardwareAccelerationType.Auto)
    {
        var info = GetInfo();

        if (type == HardwareAccelerationType.Auto)
        {
            type = info.RecommendedType;
        }

        // Validate the requested type is supported
        if (!AvailableTypes.Contains(type))
        {
            LogMessage($"Hardware acceleration type {type} is not supported on this system");
            return false;
        }

        _currentType = type;
        LogMessage($"Hardware acceleration enabled: {type}");
        return true;
    }

    public void Disable()
    {
        _currentType = HardwareAccelerationType.None;
        LogMessage("Hardware acceleration disabled");
    }

    public HardwarePerformanceStats GetPerformanceStats()
    {
        var stats = new HardwarePerformanceStats
        {
            IsHardwareDecoding = IsEnabled,
            ActiveDecoder = _currentType.ToString()
        };

        try
        {
            stats.GpuUsagePercent = GetGpuUsagePercent();
            stats.GpuMemoryUsedMB = GetGpuMemoryUsedMB();
            stats.GpuMemoryTotalMB = GetInfo().GpuMemoryMB;
        }
        catch (Exception ex)
        {
            LogMessage($"Failed to get performance stats: {ex.Message}");
        }

        return stats;
    }

    public double GetGpuUsagePercent()
    {
        try
        {
            // Query GPU usage via performance counters
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine");
            var engines = searcher.Get().Cast<ManagementObject>().ToList();

            if (engines.Any())
            {
                var utilization = engines
                    .Select(e => Convert.ToDouble(e["UtilizationPercentage"] ?? 0))
                    .Average();
                return utilization;
            }
        }
        catch
        {
            // Performance counters may not be available
        }

        return 0;
    }

    public long GetGpuMemoryUsedMB()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUAdapterMemory");
            var memory = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

            if (memory != null)
            {
                var usedBytes = Convert.ToInt64(memory["DedicatedUsage"] ?? 0);
                return usedBytes / (1024 * 1024);
            }
        }
        catch
        {
            // Performance counters may not be available
        }

        return 0;
    }

    public string[] GetLibVlcOptions()
    {
        var options = new List<string>
        {
            "--rtsp-tcp",
            "--network-caching=300",
            "--no-video-title-show",
            "--quiet"
        };

        if (IsEnabled)
        {
            switch (_currentType)
            {
                case HardwareAccelerationType.DXVA2:
                    options.Add("--avcodec-hw=dxva2");
                    LogMessage("Using DXVA2 hardware acceleration");
                    break;

                case HardwareAccelerationType.D3D11VA:
                    options.Add("--avcodec-hw=d3d11va");
                    LogMessage("Using D3D11VA hardware acceleration");
                    break;

                case HardwareAccelerationType.CUDA:
                    options.Add("--avcodec-hw=cuda");
                    LogMessage("Using CUDA hardware acceleration");
                    break;

                case HardwareAccelerationType.QSV:
                    options.Add("--avcodec-hw=qsv");
                    LogMessage("Using Intel Quick Sync hardware acceleration");
                    break;

                case HardwareAccelerationType.Auto:
                    options.Add("--avcodec-hw=any");
                    LogMessage("Using automatic hardware acceleration");
                    break;
            }

            // Additional options for better hardware decoding
            options.Add("--avcodec-threads=0"); // Auto-detect optimal thread count
            options.Add("--avcodec-skip-frame=0"); // Don't skip frames
            options.Add("--avcodec-skip-idct=0");
        }

        return options.ToArray();
    }

    private static bool IsWindowsVistaOrLater()
    {
        return Environment.OSVersion.Version.Major >= 6;
    }

    private static bool IsWindows8OrLater()
    {
        var version = Environment.OSVersion.Version;
        return version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
    }

    private void LogMessage(string message)
    {
        Log.Information($"[HWAccel] {message}");
        OnLog?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disable();
    }
}
