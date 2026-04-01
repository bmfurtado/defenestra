namespace Defenestra.Models;

public class MonitorPreset
{
    public string Name { get; set; } = string.Empty;
    public Alignment Alignment { get; set; } = Alignment.Center;
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() => Name;
}
