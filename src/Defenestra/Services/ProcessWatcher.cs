using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using Defenestra.Models;

namespace Defenestra.Services;

public class ProcessWatcher
{
    private readonly DispatcherTimer _timer;
    private readonly HashSet<IntPtr> _appliedHandles = new();
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

    private void OnTick(object? sender, EventArgs e)
    {
        foreach (var profile in _profiles)
        {
            if (string.IsNullOrEmpty(profile.ExeName))
                continue;

            var hWnd = WindowManager.FindWindowByProcessName(profile.ExeName);
            if (hWnd == null || _appliedHandles.Contains(hWnd.Value))
                continue;

            WindowManager.ApplySettings(hWnd.Value, profile.X, profile.Y,
                profile.Width, profile.Height, profile.RemoveDecorations);

            _appliedHandles.Add(hWnd.Value);
            ProfileApplied?.Invoke(profile.Name);
        }

        // Clean up stale handles
        _appliedHandles.RemoveWhere(h =>
            WindowManager.FindWindowByProcessName(
                _profiles.FirstOrDefault(p =>
                    WindowManager.FindWindowByProcessName(p.ExeName) == h)?.ExeName ?? "") == null);
    }

    public void ClearAppliedCache()
    {
        _appliedHandles.Clear();
    }
}
