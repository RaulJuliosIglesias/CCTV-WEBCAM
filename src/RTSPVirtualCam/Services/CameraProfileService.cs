using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RTSPVirtualCam.Models;
using Serilog;

namespace RTSPVirtualCam.Services;

public class CameraProfileService
{
    private readonly string _profilesFilePath;
    private List<CameraProfile> _profiles = new();

    public CameraProfileService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RTSPVirtualCam");
        
        Directory.CreateDirectory(appDataPath);
        _profilesFilePath = Path.Combine(appDataPath, "camera_profiles.json");
        
        LoadProfiles();
    }

    public List<CameraProfile> GetProfiles()
    {
        return _profiles.ToList();
    }

    public CameraProfile? GetProfile(string id)
    {
        return _profiles.FirstOrDefault(p => p.Id == id);
    }

    public void SaveProfile(CameraProfile profile)
    {
        try
        {
            var existingIndex = _profiles.FindIndex(p => p.Id == profile.Id);
            
            if (existingIndex >= 0)
            {
                _profiles[existingIndex] = profile;
                Log.Information($"Updated camera profile: {profile.Name}");
            }
            else
            {
                _profiles.Add(profile);
                Log.Information($"Added new camera profile: {profile.Name}");
            }

            SaveToFile();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save camera profile");
            throw;
        }
    }

    public void DeleteProfile(string id)
    {
        try
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveToFile();
                Log.Information($"Deleted camera profile: {profile.Name}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete camera profile");
            throw;
        }
    }

    private void LoadProfiles()
    {
        try
        {
            if (File.Exists(_profilesFilePath))
            {
                var json = File.ReadAllText(_profilesFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var loadedProfiles = JsonSerializer.Deserialize<List<CameraProfile>>(json, options);
                if (loadedProfiles != null)
                {
                    _profiles = loadedProfiles;
                    Log.Information($"Loaded {_profiles.Count} camera profiles");
                }
            }
            else
            {
                _profiles = new List<CameraProfile>();
                Log.Information("No existing camera profiles found, starting fresh");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load camera profiles");
            _profiles = new List<CameraProfile>();
        }
    }

    private void SaveToFile()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_profiles, options);
            File.WriteAllText(_profilesFilePath, json);
            
            Log.Debug($"Saved {_profiles.Count} camera profiles to file");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save camera profiles to file");
            throw;
        }
    }

    public CameraProfile CreateFromCurrentSettings(
        string name,
        string ipAddress,
        int port,
        string username,
        string password,
        string ptzUsername,
        string ptzPassword,
        CameraBrand brand,
        StreamType stream,
        int channel,
        bool useManualUrl,
        string manualUrl)
    {
        return new CameraProfile
        {
            Name = name,
            IpAddress = ipAddress,
            Port = port,
            Username = username,
            Password = password,
            PtzUsername = ptzUsername,
            PtzPassword = ptzPassword,
            Brand = brand,
            Stream = stream,
            Channel = channel,
            UseManualUrl = useManualUrl,
            ManualUrl = manualUrl
        };
    }
}
