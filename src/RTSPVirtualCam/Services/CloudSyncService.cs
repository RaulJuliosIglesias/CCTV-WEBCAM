using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using RTSPVirtualCam.Models;
using Serilog;
using Timer = System.Timers.Timer;

namespace RTSPVirtualCam.Services;

/// <summary>
/// Cloud synchronization service for settings across devices.
/// </summary>
public class CloudSyncService : ICloudSyncService
{
    private readonly HttpClient _httpClient;
    private readonly CameraProfileService _profileService;
    private readonly string _dataPath;
    private CloudSyncSettings _settings = new();
    private Timer? _autoSyncTimer;
    private SyncState _currentState = SyncState.Idle;
    private DateTime? _lastSyncTime;
    private bool _disposed;

    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;
    public event EventHandler<SyncConflictEventArgs>? ConflictDetected;
    public event Action<string>? OnLog;

    public CloudSyncService(CameraProfileService profileService)
    {
        _profileService = profileService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        _dataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTSPVirtualCam", "sync");
        Directory.CreateDirectory(_dataPath);

        LoadSettings();
    }

    private void LoadSettings()
    {
        try
        {
            var settingsFile = Path.Combine(_dataPath, "sync_settings.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                var settings = JsonSerializer.Deserialize<CloudSyncSettings>(json);
                if (settings != null) _settings = settings;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load sync settings");
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settingsFile = Path.Combine(_dataPath, "sync_settings.json");
            File.WriteAllText(settingsFile, JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save sync settings");
        }
    }

    public async Task<bool> SyncAsync(CancellationToken ct = default)
    {
        if (!_settings.IsEnabled || string.IsNullOrEmpty(_settings.ServerUrl))
        {
            LogMessage("Sync disabled or server not configured");
            return false;
        }

        try
        {
            _currentState = SyncState.Syncing;

            // Pull from server first
            var pullSuccess = await PullAsync(ct);

            // Then push local changes
            var pushSuccess = await PushAsync(ct);

            _currentState = SyncState.Idle;
            _lastSyncTime = DateTime.Now;
            _settings.LastSyncTime = _lastSyncTime;
            SaveSettings();

            var success = pullSuccess && pushSuccess;

            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = success,
                Direction = SyncDirection.Both,
                ItemsSynced = 0 // Would count actual items
            });

            LogMessage($"Sync completed: {(success ? "success" : "partial failure")}");
            return success;
        }
        catch (Exception ex)
        {
            _currentState = SyncState.Error;
            _settings.LastError = ex.Message;

            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs
            {
                Success = false,
                Direction = SyncDirection.Both,
                ErrorMessage = ex.Message
            });

            LogMessage($"Sync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PushAsync(CancellationToken ct = default)
    {
        if (!_settings.IsEnabled || string.IsNullOrEmpty(_settings.ServerUrl))
            return false;

        try
        {
            var data = await ExportDataAsync();

            // Encrypt if enabled
            var json = JsonSerializer.Serialize(data);
            if (_settings.EncryptData && !string.IsNullOrEmpty(_settings.EncryptionKey))
            {
                json = Encrypt(json, _settings.EncryptionKey);
            }

            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/sync/{_settings.DeviceId}";
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            AddAuthHeader(request);
            var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                LogMessage("Push completed successfully");
                return true;
            }

            LogMessage($"Push failed: {response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            LogMessage($"Push error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> PullAsync(CancellationToken ct = default)
    {
        if (!_settings.IsEnabled || string.IsNullOrEmpty(_settings.ServerUrl))
            return false;

        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/sync/{_settings.DeviceId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthHeader(request);

            var response = await _httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // No data on server yet
                    LogMessage("No server data found - will push on next sync");
                    return true;
                }
                LogMessage($"Pull failed: {response.StatusCode}");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync(ct);

            // Decrypt if needed
            if (_settings.EncryptData && !string.IsNullOrEmpty(_settings.EncryptionKey))
            {
                json = Decrypt(json, _settings.EncryptionKey);
            }

            var serverData = JsonSerializer.Deserialize<SyncData>(json);
            if (serverData != null)
            {
                // Check for conflicts
                var localData = await ExportDataAsync();
                if (serverData.LastModified > localData.LastModified)
                {
                    // Server is newer - apply changes
                    await ImportDataAsync(serverData, merge: true);
                    LogMessage("Pulled and applied server changes");
                }
                else if (serverData.LastModified < localData.LastModified)
                {
                    // Local is newer - will push
                    LogMessage("Local data is newer - will push on sync");
                }
                else
                {
                    LogMessage("Data is up to date");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            LogMessage($"Pull error: {ex.Message}");
            return false;
        }
    }

    public void StartAutoSync()
    {
        if (_autoSyncTimer != null)
        {
            _autoSyncTimer.Stop();
            _autoSyncTimer.Dispose();
        }

        _autoSyncTimer = new Timer(_settings.SyncIntervalMinutes * 60 * 1000);
        _autoSyncTimer.Elapsed += async (s, e) => await SyncAsync();
        _autoSyncTimer.Start();

        _settings.AutoSync = true;
        SaveSettings();

        LogMessage($"Auto-sync started (every {_settings.SyncIntervalMinutes} minutes)");
    }

    public void StopAutoSync()
    {
        _autoSyncTimer?.Stop();
        _autoSyncTimer?.Dispose();
        _autoSyncTimer = null;

        _settings.AutoSync = false;
        SaveSettings();

        LogMessage("Auto-sync stopped");
    }

    public SyncState GetSyncState() => _currentState;
    public DateTime? GetLastSyncTime() => _lastSyncTime;
    public bool IsAutoSyncEnabled() => _settings.AutoSync && _autoSyncTimer != null;

    public CloudSyncSettings GetSettings() => _settings;

    public Task UpdateSettingsAsync(CloudSyncSettings settings)
    {
        _settings = settings;
        SaveSettings();

        if (settings.AutoSync && settings.IsEnabled)
        {
            StartAutoSync();
        }
        else
        {
            StopAutoSync();
        }

        return Task.CompletedTask;
    }

    public async Task<bool> ValidateConnectionAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_settings.ServerUrl))
            return false;

        try
        {
            var url = $"{_settings.ServerUrl.TrimEnd('/')}/api/health";
            var response = await _httpClient.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<SyncData> ExportDataAsync()
    {
        var data = new SyncData
        {
            Version = "2.0",
            LastModified = DateTime.UtcNow,
            DeviceId = _settings.DeviceId,
            DeviceName = _settings.DeviceName,
            CameraProfiles = _profileService.GetProfiles(),
            Checksum = string.Empty
        };

        // Calculate checksum
        var json = JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        data.Checksum = Convert.ToBase64String(hash);

        return Task.FromResult(data);
    }

    public Task<bool> ImportDataAsync(SyncData data, bool merge = true)
    {
        try
        {
            if (merge)
            {
                // Merge camera profiles
                foreach (var profile in data.CameraProfiles)
                {
                    var existing = _profileService.GetProfile(profile.Id);
                    if (existing == null)
                    {
                        _profileService.SaveProfile(profile);
                    }
                    else
                    {
                        // Conflict resolution
                        switch (_settings.ConflictResolution)
                        {
                            case ConflictResolution.ServerWins:
                                _profileService.SaveProfile(profile);
                                break;
                            case ConflictResolution.LocalWins:
                                // Keep local
                                break;
                            case ConflictResolution.MostRecent:
                                // Would need modification timestamps
                                _profileService.SaveProfile(profile);
                                break;
                        }
                    }
                }
            }
            else
            {
                // Replace all
                foreach (var existingProfile in _profileService.GetProfiles())
                {
                    _profileService.DeleteProfile(existingProfile.Id);
                }
                foreach (var profile in data.CameraProfiles)
                {
                    _profileService.SaveProfile(profile);
                }
            }

            LogMessage($"Imported {data.CameraProfiles.Count} camera profiles");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            LogMessage($"Import failed: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> ResetLocalDataAsync()
    {
        try
        {
            foreach (var profile in _profileService.GetProfiles())
            {
                _profileService.DeleteProfile(profile.Id);
            }

            LogMessage("Local data reset");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            LogMessage($"Reset failed: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    private void AddAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_settings.ApiKey))
        {
            request.Headers.Add("X-API-Key", _settings.ApiKey);
        }
    }

    private static string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string Decrypt(string cipherText, string key)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.Key = keyBytes;

        var iv = new byte[16];
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private void LogMessage(string message)
    {
        Log.Information($"[CloudSync] {message}");
        OnLog?.Invoke(message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopAutoSync();
        _httpClient.Dispose();
        SaveSettings();
    }
}
