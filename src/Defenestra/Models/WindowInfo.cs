using System;

namespace Defenestra.Models;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(ProcessName)
        ? Title
        : $"{Title}  [{ProcessName}]";

    public override string ToString() => DisplayName;
}
