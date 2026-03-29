using System;
using System.IO;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Defenestra.Views;

namespace Defenestra;

public partial class App : Application
{
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;

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

        var contextMenu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Show" };
        showItem.Click += (_, _) => ShowMainWindow();
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => ExitApp();
        contextMenu.Items.Add(exitItem);

        _trayIcon.ContextMenu = contextMenu;
        _trayIcon.TrayMouseDoubleClick += (_, _) => ShowMainWindow();

        _mainWindow.Show();
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
