using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Defenestra.Models;

namespace Defenestra.Services;

public class AppData
{
    public int Version { get; set; } = 2;
    public bool AutoApplyEnabled { get; set; }
    public List<GameProfile> Profiles { get; set; } = new();
}

public class ProfileStore
{
    private const int CurrentVersion = 2;

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Defenestra");

    private static readonly string DataPath = Path.Combine(DataDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
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

        // Check version before deserializing
        var doc = JsonSerializer.Deserialize<JsonObject>(json);
        int version = doc?["Version"]?.GetValue<int>() ?? 0;

        if (version < 2)
        {
            MigrateV1ToV2(json);
            return;
        }

        // Future migrations would go here:
        // if (version < 3) { MigrateV2ToV3(); return; }

        _data = JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
    }

    private void MigrateV1ToV2(string json)
    {
        // V1 had X/Y as absolute coordinates, no Version field
        var doc = JsonSerializer.Deserialize<JsonObject>(json);
        if (doc == null)
        {
            _data = new AppData();
            Save();
            return;
        }

        _data = new AppData
        {
            AutoApplyEnabled = doc["AutoApplyEnabled"]?.GetValue<bool>() ?? false
        };

        var profilesNode = doc["Profiles"]?.AsArray();
        if (profilesNode != null)
        {
            var monitors = MonitorService.GetAllMonitors();

            foreach (var profileNode in profilesNode)
            {
                if (profileNode == null) continue;

                int oldX = profileNode["X"]?.GetValue<int>() ?? 0;
                int oldY = profileNode["Y"]?.GetValue<int>() ?? 0;
                int width = profileNode["Width"]?.GetValue<int>() ?? 2560;
                int height = profileNode["Height"]?.GetValue<int>() ?? 1440;
                string monitorDevice = profileNode["MonitorDeviceName"]?.GetValue<string>() ?? string.Empty;

                // Find the monitor this profile was saved for
                var monitor = monitors.FirstOrDefault(m => m.DeviceName == monitorDevice)
                    ?? monitors.FirstOrDefault(m => m.IsPrimary)
                    ?? monitors.FirstOrDefault();

                Alignment alignment = Alignment.Center;
                int offsetX = 0, offsetY = 0;

                if (monitor != null)
                {
                    (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
                        oldX, oldY, width, height, monitor);
                }

                _data.Profiles.Add(new GameProfile
                {
                    Id = profileNode["Id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N")[..8],
                    Name = profileNode["Name"]?.GetValue<string>() ?? string.Empty,
                    ExeName = profileNode["ExeName"]?.GetValue<string>() ?? string.Empty,
                    Alignment = alignment,
                    OffsetX = offsetX,
                    OffsetY = offsetY,
                    Width = width,
                    Height = height,
                    RemoveDecorations = profileNode["RemoveDecorations"]?.GetValue<bool>() ?? true,
                    AutoApply = profileNode["AutoApply"]?.GetValue<bool>() ?? true,
                    MonitorDeviceName = monitorDevice
                });
            }
        }

        _data.Version = CurrentVersion;
        Save();
    }

    private void Save()
    {
        _data.Version = CurrentVersion;
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
