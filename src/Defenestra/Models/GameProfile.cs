using System;
using System.Text.Json.Serialization;

namespace Defenestra.Models;

public class GameProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string ExeName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 2560;
    public int Height { get; set; } = 1440;
    public bool RemoveDecorations { get; set; } = true;
    public bool AutoApply { get; set; } = true;
    public string MonitorDeviceName { get; set; } = string.Empty;

    [JsonIgnore]
    public string Summary => $"{Width}x{Height} at ({X},{Y}){(RemoveDecorations ? " borderless" : "")}";
}
