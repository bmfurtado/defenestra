using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Defenestra.Models;

namespace Defenestra.Services;

public class ProfileStore
{
    private static readonly string ProfilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "profiles.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public List<GameProfile> Load()
    {
        if (!File.Exists(ProfilePath))
            return new List<GameProfile>();

        string json = File.ReadAllText(ProfilePath);
        return JsonSerializer.Deserialize<List<GameProfile>>(json, JsonOptions)
               ?? new List<GameProfile>();
    }

    public void Save(List<GameProfile> profiles)
    {
        string json = JsonSerializer.Serialize(profiles, JsonOptions);
        File.WriteAllText(ProfilePath, json);
    }
}
