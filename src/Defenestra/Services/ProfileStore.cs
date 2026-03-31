using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Defenestra.Models;

namespace Defenestra.Services;

public class AppData
{
    public bool AutoApplyEnabled { get; set; }
    public List<GameProfile> Profiles { get; set; } = new();
}

public class ProfileStore
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Defenestra");

    private static readonly string DataPath = Path.Combine(DataDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private AppData _data = new();

    public ProfileStore()
    {
        Directory.CreateDirectory(DataDir);
        Load();
    }

    private void Load()
    {
        if (!File.Exists(DataPath))
            return;

        string json = File.ReadAllText(DataPath);
        _data = JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
    }

    private void Save()
    {
        string json = JsonSerializer.Serialize(_data, JsonOptions);
        File.WriteAllText(DataPath, json);
    }

    public List<GameProfile> GetProfiles() => _data.Profiles;

    public void SaveProfiles(List<GameProfile> profiles)
    {
        _data.Profiles = profiles;
        Save();
    }

    public bool GetAutoApplyEnabled() => _data.AutoApplyEnabled;

    public void SetAutoApplyEnabled(bool enabled)
    {
        _data.AutoApplyEnabled = enabled;
        Save();
    }
}
