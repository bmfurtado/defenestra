using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using Defenestra.Views;

namespace Defenestra;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MenuItem? _startupItem;

    private const string AppName = "Defenestra";
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mainWindow = new MainWindow();

        // Load the custom icon from embedded resources
        var iconUri = new Uri("pack://application:,,,/Assets/icon.ico");
        var iconStream = GetResourceStream(iconUri)?.Stream;
        var icon = iconStream != null
            ? new System.Drawing.Icon(iconStream)
            : System.Drawing.SystemIcons.Application;

        _trayIcon = new TaskbarIcon
        {
            Icon = icon,
            ToolTipText = "Defenestra"
        };

        var contextMenu = new ContextMenu();

        var showItem = new MenuItem { Header = "Show" };
        showItem.Click += (_, _) => ShowMainWindow();
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new Separator());

        _startupItem = new MenuItem
        {
            Header = "Start with Windows",
            IsCheckable = true,
            IsChecked = IsStartupEnabled()
        };
        _startupItem.Click += (_, _) => ToggleStartup();
        contextMenu.Items.Add(_startupItem);

        contextMenu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        bool startMinimized = e.Args.Length > 0
            && string.Equals(e.Args[0], "--minimized", StringComparison.OrdinalIgnoreCase);

        if (!startMinimized)
            _mainWindow.Show();
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, false);
        return key?.GetValue(AppName) != null;
    }

    private void ToggleStartup()
    {
        if (_startupItem == null) return;

        if (_startupItem.IsChecked)
        {
            // Add to startup - use the exe path
            string exePath = Environment.ProcessPath ?? "";
            if (!string.IsNullOrEmpty(exePath))
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
                key?.SetValue(AppName, $"\"{exePath}\" --minimized");
            }
        }
        else
        {
            // Remove from startup
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, true);
            key?.DeleteValue(AppName, false);
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _trayIcon?.Dispose();
        _mainWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
