using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Defenestra.Models;
using Defenestra.Services;

namespace Defenestra.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ProfileStore _profileStore = new();
    private readonly ProcessWatcher _processWatcher = new();

    public ObservableCollection<WindowInfo> Windows { get; } = new();
    public ObservableCollection<MonitorInfo> Monitors { get; } = new();
    public ObservableCollection<MonitorPreset> Presets { get; } = new();
    public ObservableCollection<GameProfile> Profiles { get; } = new();

    private WindowInfo? _selectedWindow;
    public WindowInfo? SelectedWindow
    {
        get => _selectedWindow;
        set { _selectedWindow = value; OnPropertyChanged(); }
    }

    private MonitorInfo? _selectedMonitor;
    public MonitorInfo? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            _selectedMonitor = value;
            OnPropertyChanged();
            if (value != null)
                RefreshPresets();
        }
    }

    private MonitorPreset? _selectedPreset;
    public MonitorPreset? SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            _selectedPreset = value;
            OnPropertyChanged();
            if (value != null)
            {
                PosX = value.X;
                PosY = value.Y;
                Width = value.Width;
                Height = value.Height;
            }
        }
    }

    private GameProfile? _selectedProfile;
    public GameProfile? SelectedProfile
    {
        get => _selectedProfile;
        set { _selectedProfile = value; OnPropertyChanged(); }
    }

    private int _posX;
    public int PosX
    {
        get => _posX;
        set { _posX = value; OnPropertyChanged(); }
    }

    private int _posY;
    public int PosY
    {
        get => _posY;
        set { _posY = value; OnPropertyChanged(); }
    }

    private int _width = 2560;
    public int Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); }
    }

    private int _height = 1440;
    public int Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); }
    }

    private bool _removeDecorations = true;
    public bool RemoveDecorations
    {
        get => _removeDecorations;
        set { _removeDecorations = value; OnPropertyChanged(); }
    }

    private bool _autoApplyEnabled;
    public bool AutoApplyEnabled
    {
        get => _autoApplyEnabled;
        set
        {
            _autoApplyEnabled = value;
            _processWatcher.Enabled = value;
            _profileStore.SetAutoApplyEnabled(value);
            OnPropertyChanged();
        }
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand RefreshWindowsCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand LoadProfileCommand { get; }

    public MainViewModel()
    {
        RefreshWindowsCommand = new RelayCommand(RefreshWindows);
        ApplyCommand = new RelayCommand(ApplySettings, () => SelectedWindow != null);
        SaveProfileCommand = new RelayCommand(SaveProfile, () => SelectedWindow != null);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null);
        LoadProfileCommand = new RelayCommand(LoadProfile, () => SelectedProfile != null);

        _processWatcher.ProfileApplied += name =>
            StatusMessage = $"Auto-applied profile: {name}";

        LoadMonitors();
        LoadProfiles();
        LoadSettings();
        RefreshWindows();
    }

    private void LoadMonitors()
    {
        Monitors.Clear();
        foreach (var mon in MonitorService.GetAllMonitors())
            Monitors.Add(mon);

        // Default to primary monitor
        SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();
    }

    private void RefreshPresets()
    {
        if (SelectedMonitor == null) return;

        Presets.Clear();
        foreach (var preset in MonitorService.GetPresetsForMonitor(SelectedMonitor))
            Presets.Add(preset);

        SelectedPreset = Presets.FirstOrDefault(p => p.Name.Contains("16:9")) ?? Presets.FirstOrDefault();
        StatusMessage = $"Monitor: {SelectedMonitor.DeviceName} ({SelectedMonitor.Width}x{SelectedMonitor.Height})";
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var profile in _profileStore.GetProfiles())
            Profiles.Add(profile);

        _processWatcher.UpdateProfiles(Profiles.ToList());
    }

    private void LoadSettings()
    {
        AutoApplyEnabled = _profileStore.GetAutoApplyEnabled();
    }

    public void RefreshWindows()
    {
        Windows.Clear();
        foreach (var win in WindowManager.GetVisibleWindows()
                     .OrderBy(w => w.ProcessName)
                     .ThenBy(w => w.Title))
        {
            Windows.Add(win);
        }
        StatusMessage = $"Found {Windows.Count} windows";
    }

    private async void ApplySettings()
    {
        if (SelectedWindow == null) return;

        try
        {
            await WindowManager.ApplySettingsAsync(SelectedWindow.Handle, PosX, PosY, Width, Height, RemoveDecorations);
            StatusMessage = $"Applied to: {SelectedWindow.Title}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private void SaveProfile()
    {
        if (SelectedWindow == null) return;

        string monitorDevice = SelectedMonitor?.DeviceName ?? string.Empty;

        // Check if a profile already exists for this exe
        var existing = Profiles.FirstOrDefault(p =>
            string.Equals(p.ExeName, SelectedWindow.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.X = PosX;
            existing.Y = PosY;
            existing.Width = Width;
            existing.Height = Height;
            existing.RemoveDecorations = RemoveDecorations;
            existing.MonitorDeviceName = monitorDevice;
            StatusMessage = $"Updated profile: {existing.Name}";
        }
        else
        {
            var profile = new GameProfile
            {
                Name = SelectedWindow.ProcessName,
                ExeName = SelectedWindow.ProcessName,
                X = PosX,
                Y = PosY,
                Width = Width,
                Height = Height,
                RemoveDecorations = RemoveDecorations,
                MonitorDeviceName = monitorDevice
            };
            Profiles.Add(profile);
            StatusMessage = $"Saved profile: {profile.Name}";
        }

        _profileStore.SaveProfiles(Profiles.ToList());
        _processWatcher.UpdateProfiles(Profiles.ToList());
    }

    private void DeleteProfile()
    {
        if (SelectedProfile == null) return;

        string name = SelectedProfile.Name;
        Profiles.Remove(SelectedProfile);
        _profileStore.SaveProfiles(Profiles.ToList());
        _processWatcher.UpdateProfiles(Profiles.ToList());
        StatusMessage = $"Deleted profile: {name}";
    }

    private void LoadProfile()
    {
        if (SelectedProfile == null) return;

        PosX = SelectedProfile.X;
        PosY = SelectedProfile.Y;
        Width = SelectedProfile.Width;
        Height = SelectedProfile.Height;
        RemoveDecorations = SelectedProfile.RemoveDecorations;

        // Select the monitor this profile was saved for
        if (!string.IsNullOrEmpty(SelectedProfile.MonitorDeviceName))
        {
            var monitor = Monitors.FirstOrDefault(m =>
                m.DeviceName == SelectedProfile.MonitorDeviceName);
            if (monitor != null)
                SelectedMonitor = monitor;
        }

        StatusMessage = $"Loaded profile: {SelectedProfile.Name}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
