using System;
using Defenestra.Models;

namespace Defenestra.Services;

public static class AlignmentCalculator
{
    /// <summary>
    /// Computes the absolute screen position for a window given its alignment,
    /// offsets, size, and target monitor.
    /// </summary>
    public static (int x, int y) ComputeAbsolutePosition(
        Alignment alignment, int offsetX, int offsetY,
        int windowWidth, int windowHeight, MonitorInfo monitor)
    {
        // Compute the anchor point on the monitor
        int anchorX = alignment switch
        {
            Alignment.TopLeft or Alignment.CenterLeft or Alignment.BottomLeft
                => monitor.Left,
            Alignment.TopCenter or Alignment.Center or Alignment.BottomCenter
                => monitor.Left + (monitor.Width - windowWidth) / 2,
            Alignment.TopRight or Alignment.CenterRight or Alignment.BottomRight
                => monitor.Left + monitor.Width - windowWidth,
            _ => monitor.Left + (monitor.Width - windowWidth) / 2
        };

        int anchorY = alignment switch
        {
            Alignment.TopLeft or Alignment.TopCenter or Alignment.TopRight
                => monitor.Top,
            Alignment.CenterLeft or Alignment.Center or Alignment.CenterRight
                => monitor.Top + (monitor.Height - windowHeight) / 2,
            Alignment.BottomLeft or Alignment.BottomCenter or Alignment.BottomRight
                => monitor.Top + monitor.Height - windowHeight,
            _ => monitor.Top + (monitor.Height - windowHeight) / 2
        };

        return (anchorX + offsetX, anchorY + offsetY);
    }

    /// <summary>
    /// Infers the best-matching alignment and residual offsets from absolute coordinates.
    /// Used for V1→V2 config migration.
    /// </summary>
    public static (Alignment alignment, int offsetX, int offsetY) InferAlignmentFromAbsolute(
        int x, int y, int windowWidth, int windowHeight, MonitorInfo monitor)
    {
        Alignment bestAlignment = Alignment.Center;
        int bestOffsetX = 0, bestOffsetY = 0;
        int bestDistance = int.MaxValue;

        foreach (Alignment alignment in Enum.GetValues<Alignment>())
        {
            var (ax, ay) = ComputeAbsolutePosition(alignment, 0, 0, windowWidth, windowHeight, monitor);
            int ox = x - ax;
            int oy = y - ay;
            int distance = Math.Abs(ox) + Math.Abs(oy);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestAlignment = alignment;
                bestOffsetX = ox;
                bestOffsetY = oy;
            }
        }

        return (bestAlignment, bestOffsetX, bestOffsetY);
    }
}
