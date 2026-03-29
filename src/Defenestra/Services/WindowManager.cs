using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Defenestra.Models;

namespace Defenestra.Services;

public static class WindowManager
{
    // Window styles
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    private const uint WS_CAPTION = 0x00C00000;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_BORDER = 0x00800000;
    private const uint WS_DLGFRAME = 0x00400000;
    private const uint WS_SYSMENU = 0x00080000;
    private const uint WS_MINIMIZEBOX = 0x00020000;
    private const uint WS_MAXIMIZEBOX = 0x00010000;

    private const uint WS_EX_DLGMODALFRAME = 0x00000001;
    private const uint WS_EX_CLIENTEDGE = 0x00000200;
    private const uint WS_EX_STATICEDGE = 0x00020000;

    // SetWindowPos flags
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOOWNERZORDER = 0x0200;

    private const int SW_RESTORE = 9;

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public static List<WindowInfo> GetVisibleWindows()
    {
        var windows = new List<WindowInfo>();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return true;

            var sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            if (string.IsNullOrWhiteSpace(title))
                return true;

            GetWindowThreadProcessId(hWnd, out uint pid);

            string processName = string.Empty;
            string exePath = string.Empty;
            try
            {
                var proc = Process.GetProcessById((int)pid);
                processName = proc.ProcessName;

                // Try to get full exe path
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProcess != IntPtr.Zero)
                {
                    var exeSb = new StringBuilder(1024);
                    uint size = (uint)exeSb.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, exeSb, ref size))
                        exePath = exeSb.ToString();
                    CloseHandle(hProcess);
                }
            }
            catch
            {
                // Process may have exited
            }

            windows.Add(new WindowInfo
            {
                Handle = hWnd,
                Title = title,
                ProcessName = processName,
                ExePath = exePath
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static async Task ApplySettingsAsync(IntPtr hWnd, int x, int y, int width, int height, bool removeDecorations)
    {
        // Restore if minimized
        ShowWindow(hWnd, SW_RESTORE);

        if (removeDecorations)
        {
            int style = GetWindowLong(hWnd, GWL_STYLE);
            style &= ~(int)(WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME | WS_SYSMENU | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            SetWindowLong(hWnd, GWL_STYLE, style);

            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            exStyle &= ~(int)(WS_EX_DLGMODALFRAME | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle);
        }

        // Check if moving across monitors with different DPI
        IntPtr sourceMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
        var targetPt = new POINT { X = x + width / 2, Y = y + height / 2 };
        IntPtr targetMonitor = MonitorFromPoint(targetPt, MONITOR_DEFAULTTONEAREST);

        bool crossDpi = false;
        if (sourceMonitor != targetMonitor)
        {
            GetDpiForMonitor(sourceMonitor, 0, out uint srcDpiX, out _);
            GetDpiForMonitor(targetMonitor, 0, out uint tgtDpiX, out _);
            crossDpi = srcDpiX != tgtDpiX;
        }

        // First apply — moves the window to the target monitor
        SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height,
            SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOOWNERZORDER);

        if (crossDpi)
        {
            // Wait for Windows to process the DPI change, then re-apply with correct scaling
            await Task.Delay(100);
            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height,
                SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOOWNERZORDER);
        }
    }

    // Synchronous overload for ProcessWatcher
    public static void ApplySettings(IntPtr hWnd, int x, int y, int width, int height, bool removeDecorations)
    {
        ApplySettingsAsync(hWnd, x, y, width, height, removeDecorations).GetAwaiter().GetResult();
    }

    public static IntPtr? FindWindowByProcessName(string processName)
    {
        IntPtr? found = null;

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            int length = GetWindowTextLength(hWnd);
            if (length == 0)
                return true;

            GetWindowThreadProcessId(hWnd, out uint pid);
            try
            {
                var proc = Process.GetProcessById((int)pid);
                if (string.Equals(proc.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
                {
                    found = hWnd;
                    return false; // stop enumerating
                }
            }
            catch { }

            return true;
        }, IntPtr.Zero);

        return found;
    }
}
