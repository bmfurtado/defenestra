using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Defenestra.Models;

namespace Defenestra.Services;

public class ProcessWatcher
{
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<string, IntPtr> _appliedWindows = new(StringComparer.OrdinalIgnoreCase);
    private List<GameProfile> _profiles = new();
    private bool _enabled;

    public event Action<string>? ProfileApplied;

    public ProcessWatcher()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timer.Tick += OnTick;
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (value)
                _timer.Start();
            else
                _timer.Stop();
        }
    }

    public void UpdateProfiles(List<GameProfile> profiles)
    {
        _profiles = profiles.Where(p => p.AutoApply).ToList();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        var monitors = MonitorService.GetAllMonitors();

        foreach (var profile in _profiles)
        {
            if (string.IsNullOrEmpty(profile.ExeName))
                continue;

            var hWnd = WindowManager.FindWindowByProcessName(profile.ExeName);
            if (hWnd == null)
            {
                // Process gone — clear so we re-apply if it comes back
                _appliedWindows.Remove(profile.ExeName);
                continue;
            }

            // Only apply if we haven't applied to this exact handle before
            if (_appliedWindows.TryGetValue(profile.ExeName, out var lastHandle) && lastHandle == hWnd.Value)
                continue;

            // Resolve alignment to absolute coordinates
            var monitor = monitors.FirstOrDefault(m => m.DeviceName == profile.MonitorDeviceName)
                ?? monitors.FirstOrDefault(m => m.IsPrimary)
                ?? monitors.FirstOrDefault();

            if (monitor == null)
                continue;

            var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
                profile.Alignment, profile.OffsetX, profile.OffsetY,
                profile.Width, profile.Height, monitor);

            await WindowManager.ApplySettingsAsync(hWnd.Value, x, y,
                profile.Width, profile.Height, profile.RemoveDecorations);

            _appliedWindows[profile.ExeName] = hWnd.Value;
            ProfileApplied?.Invoke(profile.Name);
        }
    }

    public void ClearAppliedCache()
    {
        _appliedWindows.Clear();
    }
}
