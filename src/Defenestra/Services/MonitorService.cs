using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Defenestra.Models;

namespace Defenestra.Services;

public static class MonitorService
{
    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    private const int CCHDEVICENAME = 32;
    private const uint MONITORINFOF_PRIMARY = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string szDevice;
    }

    public static List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                monitors.Add(new MonitorInfo
                {
                    DeviceName = mi.szDevice,
                    Left = mi.rcMonitor.Left,
                    Top = mi.rcMonitor.Top,
                    Width = mi.rcMonitor.Right - mi.rcMonitor.Left,
                    Height = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                    IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0
                });
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    public static List<MonitorPreset> GetPresetsForMonitor(MonitorInfo monitor)
    {
        int monW = monitor.Width;
        int monH = monitor.Height;

        var presets = new List<MonitorPreset>();

        // Center 16:9
        int w16x9 = monH * 16 / 9;
        if (w16x9 < monW)
        {
            presets.Add(new MonitorPreset
            {
                Name = $"Center 16:9 ({w16x9}x{monH})",
                Alignment = Alignment.Center,
                Width = w16x9,
                Height = monH
            });
        }

        // Center 21:9
        int w21x9 = monH * 21 / 9;
        if (w21x9 < monW)
        {
            presets.Add(new MonitorPreset
            {
                Name = $"Center 21:9 ({w21x9}x{monH})",
                Alignment = Alignment.Center,
                Width = w21x9,
                Height = monH
            });
        }

        // Thirds
        int third = monW / 3;
        presets.Add(new MonitorPreset
        {
            Name = $"Left Third ({third}x{monH})",
            Alignment = Alignment.CenterLeft,
            Width = third,
            Height = monH
        });
        presets.Add(new MonitorPreset
        {
            Name = $"Center Third ({third}x{monH})",
            Alignment = Alignment.Center,
            Width = third,
            Height = monH
        });
        presets.Add(new MonitorPreset
        {
            Name = $"Right Third ({third}x{monH})",
            Alignment = Alignment.CenterRight,
            Width = third,
            Height = monH
        });

        // Fullscreen
        presets.Add(new MonitorPreset
        {
            Name = $"Fullscreen ({monW}x{monH})",
            Alignment = Alignment.Center,
            Width = monW,
            Height = monH
        });

        return presets;
    }
}
