namespace Defenestra.Models;

public class MonitorInfo
{
    public string DeviceName { get; set; } = string.Empty;
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsPrimary { get; set; }

    public string DisplayName => IsPrimary
        ? $"{DeviceName} - {Width}x{Height} (Primary)"
        : $"{DeviceName} - {Width}x{Height} at ({Left},{Top})";

    public override string ToString() => DisplayName;
}
