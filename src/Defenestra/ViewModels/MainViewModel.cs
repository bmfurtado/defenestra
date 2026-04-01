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
    private bool _populatingFromPreset;
    private bool _populatingFromProfile;

    public ObservableCollection<WindowInfo> Windows { get; } = new();
    public ObservableCollection<MonitorInfo> Monitors { get; } = new();
    public ObservableCollection<MonitorPreset> Presets { get; } = new();
    public ObservableCollection<GameProfile> Profiles { get; } = new();

    public Alignment[] AlignmentOptions { get; } = Enum.GetValues<Alignment>();

    private WindowInfo? _selectedWindow;
    public WindowInfo? SelectedWindow
    {
        get => _selectedWindow;
        set { _selectedWindow = value; OnPropertyChanged(); ClearProfileIfManual(); }
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
                _populatingFromPreset = true;
                SelectedAlignment = value.Alignment;
                OffsetX = value.OffsetX;
                OffsetY = value.OffsetY;
                Width = value.Width;
                Height = value.Height;
                _populatingFromPreset = false;
            }
        }
    }

    private GameProfile? _selectedProfile;
    public GameProfile? SelectedProfile
    {
        get => _selectedProfile;
        set { _selectedProfile = value; OnPropertyChanged(); LoadProfile(); }
    }

    private Alignment _selectedAlignment = Alignment.Center;
    public Alignment SelectedAlignment
    {
        get => _selectedAlignment;
        set { _selectedAlignment = value; OnPropertyChanged(); ClearPresetIfManual(); }
    }

    private int _offsetX;
    public int OffsetX
    {
        get => _offsetX;
        set { _offsetX = value; OnPropertyChanged(); ClearPresetIfManual(); }
    }

    private int _offsetY;
    public int OffsetY
    {
        get => _offsetY;
        set { _offsetY = value; OnPropertyChanged(); ClearPresetIfManual(); }
    }

    private int _width = 2560;
    public int Width
    {
        get => _width;
        set { _width = value; OnPropertyChanged(); ClearPresetIfManual(); }
    }

    private int _height = 1440;
    public int Height
    {
        get => _height;
        set { _height = value; OnPropertyChanged(); ClearPresetIfManual(); }
    }

    private void ClearPresetIfManual()
    {
        if (!_populatingFromPreset && _selectedPreset != null)
        {
            _selectedPreset = null;
            OnPropertyChanged(nameof(SelectedPreset));
        }
        ClearProfileIfManual();
    }

    private void ClearProfileIfManual()
    {
        if (!_populatingFromProfile && _selectedProfile != null)
        {
            _selectedProfile = null;
            OnPropertyChanged(nameof(SelectedProfile));
        }
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
        if (SelectedWindow == null || SelectedMonitor == null) return;

        try
        {
            var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
                SelectedAlignment, OffsetX, OffsetY, Width, Height, SelectedMonitor);

            await WindowManager.ApplySettingsAsync(SelectedWindow.Handle, x, y, Width, Height, RemoveDecorations);
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
            existing.Alignment = SelectedAlignment;
            existing.OffsetX = OffsetX;
            existing.OffsetY = OffsetY;
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
                Alignment = SelectedAlignment,
                OffsetX = OffsetX,
                OffsetY = OffsetY,
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

        _populatingFromProfile = true;
        SelectedAlignment = SelectedProfile.Alignment;
        OffsetX = SelectedProfile.OffsetX;
        OffsetY = SelectedProfile.OffsetY;
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

        _populatingFromProfile = false;
        StatusMessage = $"Loaded profile: {SelectedProfile.Name}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
