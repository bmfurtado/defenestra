using System;
using System.Text.Json.Serialization;

namespace Defenestra.Models;

public class GameProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string ExeName { get; set; } = string.Empty;
    public Alignment Alignment { get; set; } = Alignment.Center;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int Width { get; set; } = 2560;
    public int Height { get; set; } = 1440;
    public bool RemoveDecorations { get; set; } = true;
    public bool AutoApply { get; set; } = true;
    public string MonitorDeviceName { get; set; } = string.Empty;

    [JsonIgnore]
    public string Summary => $"{Width}x{Height} {Alignment}{(OffsetX != 0 || OffsetY != 0 ? $" +({OffsetX},{OffsetY})" : "")}{(RemoveDecorations ? " borderless" : "")}";
}
