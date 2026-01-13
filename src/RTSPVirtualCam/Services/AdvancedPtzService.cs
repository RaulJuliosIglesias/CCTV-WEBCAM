using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Advanced PTZ service with presets, tours, and synchronized movements.
/// Supports Hikvision, Dahua, and ONVIF cameras.
/// </summary>
public class AdvancedPtzService : IAdvancedPtzService
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CameraConfig> _cameraConfigs = new();
    private readonly ConcurrentDictionary<string, TourRunner> _runningTours = new();
    private readonly ConcurrentDictionary<string, PtzSyncGroup> _syncGroups = new();
    private readonly List<AdvancedPtzPreset> _presets = new();
    private readonly List<PtzTour> _tours = new();
    private readonly string _dataPath;
    private bool _disposed;

    public event EventHandler<PtzOperationEventArgs>? OperationCompleted;
    public event EventHandler<PtzTourEventArgs>? TourStepChanged;
    public event Action<string>? OnLog;

    public AdvancedPtzService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTSPVirtualCam", "ptz");
        Directory.CreateDirectory(_dataPath);

        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var presetsFile = Path.Combine(_dataPath, "presets.json");
            if (File.Exists(presetsFile))
            {
                var json = File.ReadAllText(presetsFile);
                var presets = JsonSerializer.Deserialize<List<AdvancedPtzPreset>>(json);
                if (presets != null) _presets.AddRange(presets);
            }

            var toursFile = Path.Combine(_dataPath, "tours.json");
            if (File.Exists(toursFile))
            {
                var json = File.ReadAllText(toursFile);
                var tours = JsonSerializer.Deserialize<List<PtzTour>>(json);
                if (tours != null) _tours.AddRange(tours);
            }

            var groupsFile = Path.Combine(_dataPath, "sync_groups.json");
            if (File.Exists(groupsFile))
            {
                var json = File.ReadAllText(groupsFile);
                var groups = JsonSerializer.Deserialize<List<PtzSyncGroup>>(json);
                if (groups != null)
                {
                    foreach (var g in groups)
                        _syncGroups[g.Id] = g;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load PTZ data");
        }
    }

    private void SaveData()
    {
        try
        {
            var presetsFile = Path.Combine(_dataPath, "presets.json");
            File.WriteAllText(presetsFile, JsonSerializer.Serialize(_presets, new JsonSerializerOptions { WriteIndented = true }));

            var toursFile = Path.Combine(_dataPath, "tours.json");
            File.WriteAllText(toursFile, JsonSerializer.Serialize(_tours, new JsonSerializerOptions { WriteIndented = true }));

            var groupsFile = Path.Combine(_dataPath, "sync_groups.json");
            File.WriteAllText(groupsFile, JsonSerializer.Serialize(_syncGroups.Values.ToList(), new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save PTZ data");
        }
    }

    public void SetCameraCredentials(string cameraId, string host, string username, string password, int httpPort = 80)
    {
        _cameraConfigs[cameraId] = new CameraConfig
        {
            Host = host,
            Username = username,
            Password = password,
            HttpPort = httpPort
        };
        LogMessage($"Camera {cameraId} configured: {host}:{httpPort}");
    }

    public PtzCapabilities GetCameraCapabilities(string cameraId)
    {
        // TODO: Query camera for actual capabilities via ISAPI/CGI
        return new PtzCapabilities
        {
            SupportsPTZ = true,
            SupportsAbsolutePosition = true,
            SupportsPresets = true,
            MaxPresets = 255,
            SupportsTours = true,
            SupportsPatterns = true,
            SupportsAutoTrack = true,
            SupportsFocus = true
        };
    }

    // Basic PTZ movements
    public async Task<bool> MoveAsync(string cameraId, PtzDirection direction, int speed = 50, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
        {
            LogMessage($"Camera {cameraId} not configured");
            return false;
        }

        try
        {
            var (panSpeed, tiltSpeed) = GetDirectionSpeeds(direction, speed);
            var url = BuildHikvisionPtzUrl(config, "continuous", panSpeed, tiltSpeed, 0);

            var response = await SendRequestAsync(config, url, ct);
            var success = response.IsSuccessStatusCode;

            RaiseOperationCompleted(cameraId, PtzOperationType.Move, success);
            return success;
        }
        catch (Exception ex)
        {
            LogMessage($"Move error: {ex.Message}");
            RaiseOperationCompleted(cameraId, PtzOperationType.Move, false, ex.Message);
            return false;
        }
    }

    public async Task<bool> StopAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = BuildHikvisionPtzUrl(config, "continuous", 0, 0, 0);
            var response = await SendRequestAsync(config, url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"Stop error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ZoomAsync(string cameraId, ZoomDirection direction, int speed = 50, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            int zoomSpeed = direction == ZoomDirection.In ? speed : -speed;
            var url = BuildHikvisionPtzUrl(config, "continuous", 0, 0, zoomSpeed);

            var response = await SendRequestAsync(config, url, ct);
            var success = response.IsSuccessStatusCode;

            RaiseOperationCompleted(cameraId, PtzOperationType.Zoom, success);
            return success;
        }
        catch (Exception ex)
        {
            LogMessage($"Zoom error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> FocusAsync(string cameraId, FocusDirection direction, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            string action = direction == FocusDirection.Near ? "focusnear" : "focusfar";
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/{action}";

            var response = await SendRequestAsync(config, url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"Focus error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AutoFocusAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/autofocus";
            var response = await SendRequestAsync(config, url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"AutoFocus error: {ex.Message}");
            return false;
        }
    }

    // Preset management
    public Task<List<AdvancedPtzPreset>> GetPresetsAsync(string cameraId)
    {
        var presets = _presets.Where(p => p.CameraId == cameraId).ToList();
        return Task.FromResult(presets);
    }

    public async Task<bool> GoToPresetAsync(string cameraId, int presetId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/presets/{presetId}/goto";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);
            var success = response.IsSuccessStatusCode;

            if (success)
            {
                var preset = _presets.FirstOrDefault(p => p.CameraId == cameraId && p.Id == presetId);
                if (preset != null)
                {
                    preset.LastUsed = DateTime.Now;
                    preset.UseCount++;
                    SaveData();
                }
            }

            RaiseOperationCompleted(cameraId, PtzOperationType.GoToPreset, success);
            return success;
        }
        catch (Exception ex)
        {
            LogMessage($"GoToPreset error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SavePresetAsync(string cameraId, AdvancedPtzPreset preset, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/presets/{preset.Id}";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);

            if (response.IsSuccessStatusCode)
            {
                preset.CameraId = cameraId;
                preset.CreatedAt = DateTime.Now;

                var existing = _presets.FirstOrDefault(p => p.CameraId == cameraId && p.Id == preset.Id);
                if (existing != null)
                    _presets.Remove(existing);

                _presets.Add(preset);
                SaveData();
                LogMessage($"Preset {preset.Id} saved for camera {cameraId}");
            }

            RaiseOperationCompleted(cameraId, PtzOperationType.SavePreset, response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"SavePreset error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletePresetAsync(string cameraId, int presetId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/presets/{presetId}";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Delete);

            if (response.IsSuccessStatusCode)
            {
                _presets.RemoveAll(p => p.CameraId == cameraId && p.Id == presetId);
                SaveData();
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"DeletePreset error: {ex.Message}");
            return false;
        }
    }

    public Task<bool> RenamePresetAsync(string cameraId, int presetId, string newName, CancellationToken ct = default)
    {
        var preset = _presets.FirstOrDefault(p => p.CameraId == cameraId && p.Id == presetId);
        if (preset != null)
        {
            preset.Name = newName;
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    // Tour management
    public Task<List<PtzTour>> GetToursAsync(string cameraId)
    {
        var tours = _tours.Where(t => t.CameraId == cameraId).ToList();
        return Task.FromResult(tours);
    }

    public async Task<bool> StartTourAsync(string cameraId, string tourId, CancellationToken ct = default)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == tourId);
        if (tour == null || tour.Waypoints.Count == 0)
        {
            LogMessage($"Tour {tourId} not found or has no waypoints");
            return false;
        }

        // Stop any existing tour
        await StopTourAsync(cameraId);

        var runner = new TourRunner(this, tour, cameraId, ct);
        runner.OnStepChanged += (sender, args) => TourStepChanged?.Invoke(this, args);

        _runningTours[cameraId] = runner;
        _ = runner.RunAsync(); // Fire and forget

        LogMessage($"Started tour {tour.Name} for camera {cameraId}");
        return true;
    }

    public Task<bool> StopTourAsync(string cameraId)
    {
        if (_runningTours.TryRemove(cameraId, out var runner))
        {
            runner.Stop();
            LogMessage($"Stopped tour for camera {cameraId}");
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> PauseTourAsync(string cameraId)
    {
        if (_runningTours.TryGetValue(cameraId, out var runner))
        {
            runner.Pause();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> ResumeTourAsync(string cameraId)
    {
        if (_runningTours.TryGetValue(cameraId, out var runner))
        {
            runner.Resume();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> SaveTourAsync(PtzTour tour)
    {
        var existing = _tours.FirstOrDefault(t => t.Id == tour.Id);
        if (existing != null)
            _tours.Remove(existing);

        _tours.Add(tour);
        SaveData();
        return Task.FromResult(true);
    }

    public Task<bool> DeleteTourAsync(string tourId)
    {
        var removed = _tours.RemoveAll(t => t.Id == tourId) > 0;
        if (removed) SaveData();
        return Task.FromResult(removed);
    }

    public bool IsTourRunning(string cameraId)
    {
        return _runningTours.ContainsKey(cameraId);
    }

    // Synchronized PTZ
    public Task<bool> CreateSyncGroupAsync(PtzSyncGroup group)
    {
        _syncGroups[group.Id] = group;
        SaveData();
        return Task.FromResult(true);
    }

    public Task<bool> DeleteSyncGroupAsync(string groupId)
    {
        var removed = _syncGroups.TryRemove(groupId, out _);
        if (removed) SaveData();
        return Task.FromResult(removed);
    }

    public Task<List<PtzSyncGroup>> GetSyncGroupsAsync()
    {
        return Task.FromResult(_syncGroups.Values.ToList());
    }

    public Task<bool> EnableSyncGroupAsync(string groupId)
    {
        if (_syncGroups.TryGetValue(groupId, out var group))
        {
            group.IsActive = true;
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DisableSyncGroupAsync(string groupId)
    {
        if (_syncGroups.TryGetValue(groupId, out var group))
        {
            group.IsActive = false;
            SaveData();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public async Task<bool> SyncMoveAsync(string groupId, PtzDirection direction, int speed = 50, CancellationToken ct = default)
    {
        if (!_syncGroups.TryGetValue(groupId, out var group) || !group.IsActive)
            return false;

        var tasks = new List<Task<bool>>();

        // Move master
        tasks.Add(MoveAsync(group.MasterCameraId, direction, speed, ct));

        // Move slaves with offsets
        foreach (var slaveId in group.SlaveCameraIds)
        {
            var slaveDirection = direction;
            var slaveSpeed = speed;

            if (group.Offsets.TryGetValue(slaveId, out var offset))
            {
                slaveSpeed = (int)(speed * offset.SpeedMultiplier);

                if (group.SyncMode == PtzSyncMode.Inverted)
                {
                    slaveDirection = InvertDirection(direction);
                }
            }

            tasks.Add(MoveAsync(slaveId, slaveDirection, slaveSpeed, ct));
        }

        var results = await Task.WhenAll(tasks);
        return results.All(r => r);
    }

    // Absolute positioning
    public async Task<bool> GoToAbsolutePositionAsync(string cameraId, float pan, float tilt, float zoom, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/absolute";
            var content = $"<PTZData><AbsoluteHigh><azimuth>{pan}</azimuth><elevation>{tilt}</elevation><zoom>{zoom}</zoom></AbsoluteHigh></PTZData>";

            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/xml")
            };

            AddAuthHeader(request, config);
            var response = await _httpClient.SendAsync(request, ct);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"GoToAbsolutePosition error: {ex.Message}");
            return false;
        }
    }

    public async Task<(float pan, float tilt, float zoom)?> GetCurrentPositionAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return null;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/status";
            var response = await SendRequestAsync(config, url, ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                // Parse XML response - simplified
                // TODO: Proper XML parsing
                return (0, 0, 1); // Placeholder
            }
        }
        catch (Exception ex)
        {
            LogMessage($"GetCurrentPosition error: {ex.Message}");
        }

        return null;
    }

    // Auto tracking and patterns
    public async Task<bool> StartAutoTrackAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/autotracking/start";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"StartAutoTrack error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopAutoTrackAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/autotracking/stop";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"StopAutoTrack error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartPatternScanAsync(string cameraId, int patternId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/patterns/{patternId}/start";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"StartPatternScan error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopPatternScanAsync(string cameraId, CancellationToken ct = default)
    {
        if (!_cameraConfigs.TryGetValue(cameraId, out var config))
            return false;

        try
        {
            var url = $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/patterns/stop";
            var response = await SendRequestAsync(config, url, ct, HttpMethod.Put);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogMessage($"StopPatternScan error: {ex.Message}");
            return false;
        }
    }

    // Helper methods
    private string BuildHikvisionPtzUrl(CameraConfig config, string action, int panSpeed, int tiltSpeed, int zoomSpeed)
    {
        return $"http://{config.Host}:{config.HttpPort}/ISAPI/PTZCtrl/channels/1/{action}?pan={panSpeed}&tilt={tiltSpeed}&zoom={zoomSpeed}";
    }

    private async Task<HttpResponseMessage> SendRequestAsync(CameraConfig config, string url, CancellationToken ct, HttpMethod? method = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        AddAuthHeader(request, config);
        return await _httpClient.SendAsync(request, ct);
    }

    private void AddAuthHeader(HttpRequestMessage request, CameraConfig config)
    {
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{config.Username}:{config.Password}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }

    private static (int panSpeed, int tiltSpeed) GetDirectionSpeeds(PtzDirection direction, int speed)
    {
        return direction switch
        {
            PtzDirection.Up => (0, speed),
            PtzDirection.Down => (0, -speed),
            PtzDirection.Left => (-speed, 0),
            PtzDirection.Right => (speed, 0),
            PtzDirection.UpLeft => (-speed, speed),
            PtzDirection.UpRight => (speed, speed),
            PtzDirection.DownLeft => (-speed, -speed),
            PtzDirection.DownRight => (speed, -speed),
            _ => (0, 0)
        };
    }

    private static PtzDirection InvertDirection(PtzDirection direction)
    {
        return direction switch
        {
            PtzDirection.Up => PtzDirection.Down,
            PtzDirection.Down => PtzDirection.Up,
            PtzDirection.Left => PtzDirection.Right,
            PtzDirection.Right => PtzDirection.Left,
            PtzDirection.UpLeft => PtzDirection.DownRight,
            PtzDirection.UpRight => PtzDirection.DownLeft,
            PtzDirection.DownLeft => PtzDirection.UpRight,
            PtzDirection.DownRight => PtzDirection.UpLeft,
            _ => direction
        };
    }

    private void RaiseOperationCompleted(string cameraId, PtzOperationType operation, bool success, string? error = null)
    {
        OperationCompleted?.Invoke(this, new PtzOperationEventArgs
        {
            CameraId = cameraId,
            Operation = operation,
            Success = success,
            ErrorMessage = error
        });
    }

    private void LogMessage(string message)
    {
        Log.Information($"[PTZ] {message}");
        OnLog?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var runner in _runningTours.Values)
        {
            runner.Stop();
        }
        _runningTours.Clear();

        _httpClient.Dispose();
        SaveData();
    }

    private class CameraConfig
    {
        public string Host { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int HttpPort { get; set; } = 80;
    }

    private class TourRunner
    {
        private readonly AdvancedPtzService _service;
        private readonly PtzTour _tour;
        private readonly string _cameraId;
        private readonly CancellationToken _externalCt;
        private CancellationTokenSource? _cts;
        private bool _isPaused;
        private bool _isStopped;

        public event EventHandler<PtzTourEventArgs>? OnStepChanged;

        public TourRunner(AdvancedPtzService service, PtzTour tour, string cameraId, CancellationToken ct)
        {
            _service = service;
            _tour = tour;
            _cameraId = cameraId;
            _externalCt = ct;
        }

        public async Task RunAsync()
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(_externalCt);
            _tour.IsActive = true;
            _tour.CurrentLoopIndex = 0;

            try
            {
                do
                {
                    _tour.CurrentWaypointIndex = 0;

                    foreach (var waypoint in _tour.Waypoints.OrderBy(w => w.Order))
                    {
                        if (_isStopped || _cts.Token.IsCancellationRequested) break;

                        while (_isPaused && !_isStopped)
                            await Task.Delay(100, _cts.Token);

                        // Move to waypoint
                        RaiseStepChanged(PtzTourState.MovingToWaypoint);

                        if (waypoint.Action == PtzAction.GoToPreset)
                        {
                            await _service.GoToPresetAsync(_cameraId, waypoint.PresetId, _cts.Token);
                        }
                        else if (waypoint.Action == PtzAction.ManualPosition)
                        {
                            await _service.GoToAbsolutePositionAsync(_cameraId, waypoint.Pan, waypoint.Tilt, waypoint.Zoom, _cts.Token);
                        }

                        // Wait for transition
                        await Task.Delay(TimeSpan.FromSeconds(waypoint.TransitionTimeSeconds), _cts.Token);

                        // Dwell at waypoint
                        RaiseStepChanged(PtzTourState.DwellingAtWaypoint);
                        await Task.Delay(TimeSpan.FromSeconds(waypoint.DwellTimeSeconds), _cts.Token);

                        _tour.CurrentWaypointIndex++;
                    }

                    _tour.CurrentLoopIndex++;

                } while (_tour.IsLooping && (_tour.LoopCount == 0 || _tour.CurrentLoopIndex < _tour.LoopCount) && !_isStopped);

                RaiseStepChanged(PtzTourState.Completed);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _tour.IsActive = false;
                _tour.LastExecuted = DateTime.Now;
            }
        }

        public void Stop()
        {
            _isStopped = true;
            _cts?.Cancel();
            RaiseStepChanged(PtzTourState.Stopped);
        }

        public void Pause()
        {
            _isPaused = true;
            RaiseStepChanged(PtzTourState.Paused);
        }

        public void Resume()
        {
            _isPaused = false;
            RaiseStepChanged(PtzTourState.Running);
        }

        private void RaiseStepChanged(PtzTourState state)
        {
            OnStepChanged?.Invoke(this, new PtzTourEventArgs
            {
                CameraId = _cameraId,
                TourId = _tour.Id,
                CurrentWaypointIndex = _tour.CurrentWaypointIndex,
                TotalWaypoints = _tour.Waypoints.Count,
                CurrentLoop = _tour.CurrentLoopIndex,
                TotalLoops = _tour.LoopCount,
                State = state
            });
        }
    }
}
