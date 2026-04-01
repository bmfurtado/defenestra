using Defenestra.Models;
using Defenestra.Services;

namespace Defenestra.Tests;

public class AlignmentCalculatorTests
{
    private static MonitorInfo MakeMonitor(int left = 0, int top = 0, int width = 5120, int height = 1440)
        => new() { DeviceName = @"\\.\DISPLAY1", Left = left, Top = top, Width = width, Height = height, IsPrimary = true };

    [Fact]
    public void Center_PlacesWindowInMiddle()
    {
        var monitor = MakeMonitor();
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.Center, 0, 0, 2560, 1440, monitor);

        Assert.Equal(1280, x); // (5120 - 2560) / 2
        Assert.Equal(0, y);    // (1440 - 1440) / 2
    }

    [Fact]
    public void TopLeft_PlacesAtOrigin()
    {
        var monitor = MakeMonitor();
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.TopLeft, 0, 0, 2560, 1440, monitor);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void BottomRight_PlacesAtCorner()
    {
        var monitor = MakeMonitor(width: 3840, height: 2160);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.BottomRight, 0, 0, 1920, 1080, monitor);

        Assert.Equal(1920, x); // 3840 - 1920
        Assert.Equal(1080, y); // 2160 - 1080
    }

    [Fact]
    public void CenterLeft_PlacesAtLeftEdgeVerticallyCentered()
    {
        var monitor = MakeMonitor(width: 5120, height: 1440);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.CenterLeft, 0, 0, 1707, 1440, monitor);

        Assert.Equal(0, x);
        Assert.Equal(0, y); // (1440 - 1440) / 2
    }

    [Fact]
    public void CenterRight_PlacesAtRightEdge()
    {
        var monitor = MakeMonitor(width: 5120, height: 1440);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.CenterRight, 0, 0, 1707, 1440, monitor);

        Assert.Equal(3413, x); // 5120 - 1707
        Assert.Equal(0, y);
    }

    [Fact]
    public void Offset_ShiftsFromAnchor()
    {
        var monitor = MakeMonitor();
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.Center, 100, -50, 2560, 1440, monitor);

        Assert.Equal(1380, x); // 1280 + 100
        Assert.Equal(-50, y);  // 0 + (-50)
    }

    [Fact]
    public void MonitorOffset_IsRespected()
    {
        var monitor = MakeMonitor(left: 1920, top: 0, width: 5120, height: 1440);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.Center, 0, 0, 2560, 1440, monitor);

        Assert.Equal(3200, x); // 1920 + (5120 - 2560) / 2
        Assert.Equal(0, y);
    }

    [Fact]
    public void TopCenter_PlacesAtTopMiddle()
    {
        var monitor = MakeMonitor(width: 3840, height: 2160);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.TopCenter, 0, 0, 1920, 1080, monitor);

        Assert.Equal(960, x);  // (3840 - 1920) / 2
        Assert.Equal(0, y);
    }

    [Fact]
    public void BottomCenter_PlacesAtBottomMiddle()
    {
        var monitor = MakeMonitor(width: 3840, height: 2160);
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.BottomCenter, 0, 0, 1920, 1080, monitor);

        Assert.Equal(960, x);
        Assert.Equal(1080, y); // 2160 - 1080
    }

    [Fact]
    public void InferAlignment_ExactCenter_ReturnsCenter()
    {
        var monitor = MakeMonitor();
        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            1280, 0, 2560, 1440, monitor);

        Assert.Equal(Alignment.Center, alignment);
        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void InferAlignment_TopLeft_ReturnsTopLeft()
    {
        var monitor = MakeMonitor();
        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            0, 0, 2560, 1440, monitor);

        Assert.Equal(Alignment.TopLeft, alignment);
        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void InferAlignment_SlightlyOffCenter_ReturnsCenterWithOffset()
    {
        var monitor = MakeMonitor();
        // Centered would be x=1280, so x=1290 is 10px off
        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            1290, 5, 2560, 1440, monitor);

        Assert.Equal(Alignment.Center, alignment);
        Assert.Equal(10, offsetX);
        Assert.Equal(5, offsetY);
    }

    [Fact]
    public void InferAlignment_OnSecondMonitor_ReturnsCorrectResult()
    {
        var monitor = MakeMonitor(left: 1920, top: 0, width: 5120, height: 1440);
        // Centered on this monitor: x = 1920 + (5120 - 2560) / 2 = 3200
        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            3200, 0, 2560, 1440, monitor);

        Assert.Equal(Alignment.Center, alignment);
        Assert.Equal(0, offsetX);
        Assert.Equal(0, offsetY);
    }

    [Fact]
    public void InferAlignment_Roundtrip_PreservesPosition()
    {
        var monitor = MakeMonitor(left: 1920, top: 0);

        // Start with a known alignment + offset
        var (x, y) = AlignmentCalculator.ComputeAbsolutePosition(
            Alignment.CenterRight, 15, -10, 1707, 1440, monitor);

        // Infer back
        var (alignment, offsetX, offsetY) = AlignmentCalculator.InferAlignmentFromAbsolute(
            x, y, 1707, 1440, monitor);

        // Compute again and verify we get the same absolute position
        var (x2, y2) = AlignmentCalculator.ComputeAbsolutePosition(
            alignment, offsetX, offsetY, 1707, 1440, monitor);

        Assert.Equal(x, x2);
        Assert.Equal(y, y2);
    }
}
