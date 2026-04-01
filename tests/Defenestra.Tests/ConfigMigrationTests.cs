using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Defenestra.Models;
using Defenestra.Services;

namespace Defenestra.Tests;

public class ConfigMigrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void V1Config_MigratesCorrectly_CenteredWindow()
    {
        // Simulate a V1 config with a centered 16:9 window on a 5120x1440 monitor
        // Center 16:9 = x:1280, y:0, w:2560, h:1440
        var monitor = new MonitorInfo
        {
            DeviceName = @"\\.\DISPLAY2",
            Left = 1920, Top = 0, Width = 5120, Height = 1440, IsPrimary = false
        };

        int oldX = 1920 + 1280; // 3200 — centered on the second monitor
        int oldY = 0;
        int width = 2560;
        int height = 1440;

        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            oldX, oldY, width, height, monitor);

        Assert.Equal(Alignment.Center, alignment);
        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void V1Config_MigratesCorrectly_TopLeftWindow()
    {
        var monitor = new MonitorInfo
        {
            DeviceName = @"\\.\DISPLAY1",
            Left = 0, Top = 0, Width = 1920, Height = 1080, IsPrimary = true
        };

        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            0, 0, 960, 540, monitor);

        Assert.Equal(Alignment.TopLeft, alignment);
        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void V1Config_MigratesCorrectly_RightThird()
    {
        // Right third of a 5120x1440 monitor: x=3413, w=1707
        var monitor = new MonitorInfo
        {
            DeviceName = @"\\.\DISPLAY1",
            Left = 0, Top = 0, Width = 5120, Height = 1440, IsPrimary = true
        };

        int third = 5120 / 3; // 1706
        int oldX = third * 2; // 3412

        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            oldX, 0, third, 1440, monitor);

        // Window is full height so vertical alignment doesn't matter —
        // TopRight wins because it's checked first with the same distance.
        // TopRight anchor: x = 5120 - 1706 = 3414, so offset = 3412 - 3414 = -2
        Assert.Equal(Alignment.TopRight, alignment);
        Assert.Equal(-2, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void V1Config_WithOffset_PreservesPosition()
    {
        var monitor = new MonitorInfo
        {
            DeviceName = @"\\.\DISPLAY1",
            Left = 0, Top = 0, Width = 3840, Height = 2160, IsPrimary = true
        };

        // A window that's roughly centered but shifted 50px right, 30px down
        int centeredX = (3840 - 1920) / 2; // 960
        int oldX = centeredX + 50;
        int oldY = (2160 - 1080) / 2 + 30;

        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            oldX, oldY, 1920, 1080, monitor);

        // Verify roundtrip: the computed absolute position matches the original
        var (newX, newY) = AlignmentCalculator.ComputeAbsolutePosition(
            alignment, offsetX, offsetY, 1920, 1080, monitor);

        Assert.Equal(oldX, newX);
        Assert.Equal(oldY, newY);
    }

    [Fact]
    public void V2Config_SerializesAlignmentAsString()
    {
        var profile = new GameProfile
        {
            Name = "TestGame",
            ExeName = "test.exe",
            Alignment = Alignment.CenterRight,
            OffsetX = 10,
            OffsetY = -5,
            Width = 2560,
            Height = 1440
        };

        string json = JsonSerializer.Serialize(profile, JsonOptions);
        var doc = JsonSerializer.Deserialize<JsonObject>(json);

        // Alignment should be a string, not an integer
        Assert.Equal("CenterRight", doc?["Alignment"]?.GetValue<string>());
    }

    [Fact]
    public void V2Config_DeserializesFromString()
    {
        string json = """
        {
            "Id": "test1234",
            "Name": "TestGame",
            "ExeName": "test.exe",
            "Alignment": "BottomCenter",
            "OffsetX": 0,
            "OffsetY": 0,
            "Width": 1920,
            "Height": 1080
        }
        """;

        var profile = JsonSerializer.Deserialize<GameProfile>(json, JsonOptions);

        Assert.NotNull(profile);
        Assert.Equal(Alignment.BottomCenter, profile.Alignment);
    }

    [Fact]
    public void V1Json_HasNoVersionField()
    {
        // V1 configs have no Version field — absence implies V1
        string v1Json = """
        {
            "AutoApplyEnabled": true,
            "Profiles": [
                {
                    "Id": "abc12345",
                    "Name": "game",
                    "ExeName": "game.exe",
                    "X": 1280,
                    "Y": 0,
                    "Width": 2560,
                    "Height": 1440
                }
            ]
        }
        """;

        var doc = JsonSerializer.Deserialize<JsonObject>(v1Json);
        int version = doc?["Version"]?.GetValue<int>() ?? 0;

        Assert.Equal(0, version); // No version = V1
    }

    [Fact]
    public void V2Json_HasVersionField()
    {
        string v2Json = """
        {
            "Version": 2,
            "AutoApplyEnabled": false,
            "Profiles": []
        }
        """;

        var doc = JsonSerializer.Deserialize<JsonObject>(v2Json);
        int version = doc?["Version"]?.GetValue<int>() ?? 0;

        Assert.Equal(2, version);
    }
}
