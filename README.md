# Defenestra

A Windows desktop app for managing borderless game windows on ultrawide monitors. Place your game in the center of a 32:9 display and keep other windows on the sides.

## Features

- **Window Picker** — select any running window from a dropdown
- **Multi-Monitor Support** — detects all connected monitors with correct DPI handling
- **Smart Presets** — auto-generates layout presets based on the selected monitor's resolution:
  - Center 16:9
  - Center 21:9
  - Left / Center / Right Third
  - Fullscreen
- **Manual Configuration** — fine-tune X, Y, Width, and Height values
- **Remove Decorations** — strips title bars and borders from windows
- **Per-Game Profiles** — save and load settings per game executable
- **Auto-Apply** — automatically applies saved profiles when a game launches
- **System Tray** — minimizes to tray, runs in the background, double-click to restore
- **HiDPI Aware** — handles cross-monitor DPI transitions correctly (PerMonitorV2)

## Usage

1. Launch the app
2. Select your target monitor (e.g., your ultrawide)
3. Pick a preset or enter custom values
4. Select a game window from the dropdown
5. Click **Apply**
6. Optionally click **Save Profile** to remember settings for that game
7. Enable **Auto-apply profiles when games launch** for hands-free operation

## Building

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet build
dotnet run --project src/Defenestra
```

To publish a single-file executable:

```bash
dotnet publish src/Defenestra -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish
```

## Tech Stack

- .NET 8 / WPF
- Win32 P/Invoke (SetWindowPos, SetWindowLong, EnumWindows, EnumDisplayMonitors)
- Hardcodet.NotifyIcon.Wpf (system tray)
- Catppuccin Mocha color scheme

## Disclaimer

This app is 100% vibe coded. Built entirely through conversational AI prompts without a single line of manually written code. It works on my machine. If it works on yours too, that's a happy coincidence. Use at your own risk.

## License

MIT
