using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiScreenDock.Core;

public enum DockVisibilityMode
{
    AutoHide,
    SmartAutoHide,
    AlwaysVisible
}

public enum DockAppearanceMode
{
    System,
    Light,
    Dark
}

public enum DockScreenEdge
{
    Bottom,
    Left,
    Right
}

public enum FolderStackStylePreference
{
    Fan,
    Grid,
    List
}

public enum AutoHideResponsiveness
{
    Fast,
    Balanced,
    Relaxed
}

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double MinX => X;
    public double MinY => Y;
    public double MaxX => X + Width;
    public double MaxY => Y + Height;
    public double MidX => X + Width * 0.5;
    public double MidY => Y + Height * 0.5;

    public bool Contains(double px, double py) =>
        px >= MinX && px <= MaxX && py >= MinY && py <= MaxY;

    public bool Contains(RectD other) =>
        other.MinX >= MinX && other.MaxX <= MaxX &&
        other.MinY >= MinY && other.MaxY <= MaxY;

    public bool Intersects(RectD other)
    {
        double ix = Math.Max(0, Math.Min(MaxX, other.MaxX) - Math.Max(MinX, other.MinX));
        double iy = Math.Max(0, Math.Min(MaxY, other.MaxY) - Math.Max(MinY, other.MinY));
        return ix > 0 && iy > 0;
    }

    public RectD Intersection(RectD other)
    {
        double x1 = Math.Max(MinX, other.MinX);
        double y1 = Math.Max(MinY, other.MinY);
        double x2 = Math.Min(MaxX, other.MaxX);
        double y2 = Math.Min(MaxY, other.MaxY);
        if (x2 <= x1 || y2 <= y1) return new RectD(0, 0, 0, 0);
        return new RectD(x1, y1, x2 - x1, y2 - y1);
    }
}

public readonly record struct ScreenIdentityDescriptor(
    int Index,
    string BaseKey,
    int OriginX,
    int OriginY,
    uint UnitNumber,
    uint DisplayID
);

public readonly record struct WaveSlotLayout(
    double Origin,
    double Span,
    double MagnifiedIconSize,
    double LiftOffset,
    double Scale
);

public static class DockLayoutMath
{
    public static bool IsVertical(this DockScreenEdge edge) => edge != DockScreenEdge.Bottom;

    public static double EdgeDwellSeconds(this AutoHideResponsiveness resp, bool hasAdjacentMonitor)
    {
        if (hasAdjacentMonitor)
        {
            return resp switch
            {
                AutoHideResponsiveness.Fast => 0.09,
                AutoHideResponsiveness.Balanced => 0.15,
                AutoHideResponsiveness.Relaxed => 0.25,
                _ => 0.15
            };
        }
        return resp switch
        {
            AutoHideResponsiveness.Fast => 0.04,
            AutoHideResponsiveness.Balanced => 0.08,
            AutoHideResponsiveness.Relaxed => 0.16,
            _ => 0.08
        };
    }

    public static double HideDelaySeconds(this AutoHideResponsiveness resp) =>
        resp switch
        {
            AutoHideResponsiveness.Fast => 0.22,
            AutoHideResponsiveness.Balanced => 0.32,
            AutoHideResponsiveness.Relaxed => 0.45,
            _ => 0.32
        };

    /// <summary>
    /// Computes effective icon size so the Dock never overflows the available display span (width for Bottom, height for Left/Right).
    /// </summary>
    public static double EffectiveIconSize(
        double preferredIconSize,
        int appAndFolderCount,
        int separatorCount,
        double availableScreenSpan)
    {
        double clampedPref = Math.Clamp(preferredIconSize, 20.0, 80.0);
        if (appAndFolderCount <= 0) return clampedPref;

        double maxBarSpan = Math.Max(160.0, availableScreenSpan - 28.0);
        // Each tile takes iconSize + 5pt pad, separators take 13pt, outer horizontal padding takes 20pt + max wave expansion margin (1.35x)
        double fixedOverhead = 24.0 + separatorCount * 13.0 + appAndFolderCount * 5.0;
        double maxAllowedIcon = (maxBarSpan - fixedOverhead) / Math.Max(1.0, appAndFolderCount + 1.2);
        if (maxAllowedIcon >= clampedPref) return clampedPref;
        if (maxAllowedIcon >= 20.0) return Math.Floor(maxAllowedIcon);
        return Math.Max(14.0, Math.Floor(maxAllowedIcon));
    }

    /// <summary>
    /// Continuous 120 Hz raised-cosine magnification wave with focal-point locking so the icon under the cursor never drifts.
    /// </summary>
    public static List<WaveSlotLayout> ComputeWaveSlots(
        IReadOnlyList<bool> isSeparatorList,
        double baseIconSize,
        double tilePad,
        double outerPadding,
        double? pointerAlongBar,
        int? lockedIndex,
        double? lockedLocalFraction,
        bool magnificationEnabled,
        double maxScale = 1.42)
    {
        int count = isSeparatorList.Count;
        var baseSpans = new double[count];
        var baseCenters = new double[count];
        double cursor = outerPadding;

        for (int i = 0; i < count; i++)
        {
            double w = isSeparatorList[i] ? 13.0 : (baseIconSize + tilePad * 2.0);
            baseSpans[i] = w;
            baseCenters[i] = cursor + w * 0.5;
            cursor += w;
        }

        var scales = new double[count];
        double radius = Math.Max(baseIconSize * 2.55, 88.0);

        for (int i = 0; i < count; i++)
        {
            if (!magnificationEnabled || pointerAlongBar is null || isSeparatorList[i])
            {
                scales[i] = 1.0;
                continue;
            }
            double dist = Math.Abs(pointerAlongBar.Value - baseCenters[i]);
            if (dist >= radius)
            {
                scales[i] = 1.0;
            }
            else
            {
                double cosine = 0.5 * (1.0 + Math.Cos(Math.PI * dist / radius));
                scales[i] = 1.0 + (maxScale - 1.0) * cosine;
            }
        }

        var rawSpans = new double[count];
        for (int i = 0; i < count; i++)
        {
            rawSpans[i] = isSeparatorList[i]
                ? 13.0
                : Math.Round(baseIconSize * scales[i] + tilePad * 2.0, 2);
        }

        double totalRawSpan = rawSpans.Sum();
        double totalBaseSpan = baseSpans.Sum();

        double startOrigin = outerPadding - (totalRawSpan - totalBaseSpan) * 0.5;

        // Focal-point lock: keep the locked tile directly under pointerAlongBar at its lockedLocalFraction
        if (magnificationEnabled &&
            pointerAlongBar.HasValue &&
            lockedIndex.HasValue &&
            lockedIndex.Value >= 0 &&
            lockedIndex.Value < count &&
            !isSeparatorList[lockedIndex.Value])
        {
            double frac = Math.Clamp(lockedLocalFraction ?? 0.5, 0.1, 0.9);
            double sumBefore = 0;
            for (int i = 0; i < lockedIndex.Value; i++) sumBefore += rawSpans[i];
            startOrigin = pointerAlongBar.Value - (sumBefore + rawSpans[lockedIndex.Value] * frac);
        }

        var result = new List<WaveSlotLayout>(count);
        double pos = startOrigin;
        for (int i = 0; i < count; i++)
        {
            double sc = scales[i];
            double magSz = isSeparatorList[i] ? 1.0 : Math.Round(baseIconSize * sc, 2);
            double lift = isSeparatorList[i] ? 0.0 : Math.Round((sc - 1.0) * baseIconSize * 0.32, 2);
            result.Add(new WaveSlotLayout(
                Origin: Math.Round(pos, 2),
                Span: rawSpans[i],
                MagnifiedIconSize: magSz,
                LiftOffset: lift,
                Scale: sc
            ));
            pos += rawSpans[i];
        }

        return result;
    }

    /// <summary>
    /// Note: Windows screen coordinates have Y=0 at the TOP of the primary monitor and Y increasing downward.
    /// Bottom edge is screen.MaxY; Top edge is screen.MinY; Left is screen.MinX; Right is screen.MaxX.
    /// </summary>
    public static RectD TriggerZone(RectD screenFrame, double barSpan, double barThickness, bool hasNeighborOnEdge, DockScreenEdge edge)
    {
        double stripDepth = hasNeighborOnEdge ? 26.0 : 20.0;
        if (!edge.IsVertical())
        {
            double w = Math.Min(Math.Max(screenFrame.Width * 0.45, barSpan + 64.0), Math.Max(120.0, screenFrame.Width - 24.0));
            double x = screenFrame.MidX - w * 0.5;
            double y = screenFrame.MaxY - stripDepth;
            return new RectD(x, y, w, stripDepth);
        }
        else
        {
            double h = Math.Min(Math.Max(screenFrame.Height * 0.45, barSpan + 64.0), Math.Max(120.0, screenFrame.Height - 24.0));
            double y = screenFrame.MidY - h * 0.5;
            double x = (edge == DockScreenEdge.Left)
                ? screenFrame.MinX
                : screenFrame.MaxX - stripDepth;
            return new RectD(x, y, stripDepth, h);
        }
    }

    public static RectD KeepAliveZone(RectD screenFrame, double barSpan, double barThickness, double edgeMargin, DockScreenEdge edge)
    {
        if (!edge.IsVertical())
        {
            double trigW = Math.Min(Math.Max(screenFrame.Width * 0.45, barSpan + 64.0), Math.Max(120.0, screenFrame.Width - 24.0));
            double w = Math.Min(screenFrame.Width, Math.Max(trigW + 40.0, barSpan + 120.0));
            double h = Math.Max(48.0, barThickness + edgeMargin + 56.0);
            double x = screenFrame.MidX - w * 0.5;
            double y = screenFrame.MaxY - h;
            return new RectD(x, y, w, h);
        }
        else
        {
            double trigH = Math.Min(Math.Max(screenFrame.Height * 0.45, barSpan + 64.0), Math.Max(120.0, screenFrame.Height - 24.0));
            double h = Math.Min(screenFrame.Height, Math.Max(trigH + 40.0, barSpan + 120.0));
            double w = Math.Max(48.0, barThickness + edgeMargin + 56.0);
            double y = screenFrame.MidY - h * 0.5;
            double x = (edge == DockScreenEdge.Left)
                ? screenFrame.MinX
                : screenFrame.MaxX - w;
            return new RectD(x, y, w, h);
        }
    }

    public static RectD DockDodgeZone(RectD screenFrame, double barSpan, double barThickness, double edgeMargin, DockScreenEdge edge)
    {
        if (!edge.IsVertical())
        {
            double w = Math.Min(screenFrame.Width, barSpan + 8.0);
            double h = barThickness + edgeMargin + 4.0;
            return new RectD(screenFrame.MidX - w * 0.5, screenFrame.MaxY - h, w, h);
        }
        else
        {
            double h = Math.Min(screenFrame.Height, barSpan + 8.0);
            double w = barThickness + edgeMargin + 4.0;
            double y = screenFrame.MidY - h * 0.5;
            double x = (edge == DockScreenEdge.Left)
                ? screenFrame.MinX
                : screenFrame.MaxX - w;
            return new RectD(x, y, w, h);
        }
    }

    /// <summary>
    /// In Windows screen coordinates (Y grows downward), a monitor directly below targetFrame has other.MinY ≈ targetFrame.MaxY.
    /// </summary>
    public static bool HasMonitorDirectlyBelow(RectD targetFrame, IEnumerable<RectD> allFrames)
    {
        foreach (var other in allFrames)
        {
            if (other == targetFrame) continue;
            double verticalGap = Math.Abs(other.MinY - targetFrame.MaxY);
            double horizontalOverlap = Math.Min(targetFrame.MaxX, other.MaxX) - Math.Max(targetFrame.MinX, other.MinX);
            if (verticalGap <= 24.0 && horizontalOverlap >= 64.0) return true;
        }
        return false;
    }

    public static bool HasMonitorDirectlyAbove(RectD targetFrame, IEnumerable<RectD> allFrames)
    {
        foreach (var other in allFrames)
        {
            if (other == targetFrame) continue;
            double verticalGap = Math.Abs(targetFrame.MinY - other.MaxY);
            double horizontalOverlap = Math.Min(targetFrame.MaxX, other.MaxX) - Math.Max(targetFrame.MinX, other.MinX);
            if (verticalGap <= 24.0 && horizontalOverlap >= 64.0) return true;
        }
        return false;
    }

    public static bool HasAdjacentMonitor(DockScreenEdge edge, RectD targetFrame, IEnumerable<RectD> allFrames)
    {
        switch (edge)
        {
            case DockScreenEdge.Bottom:
                return HasMonitorDirectlyBelow(targetFrame, allFrames);
            case DockScreenEdge.Left:
                foreach (var other in allFrames)
                {
                    if (other == targetFrame) continue;
                    double gap = Math.Abs(targetFrame.MinX - other.MaxX);
                    double overlapY = Math.Min(targetFrame.MaxY, other.MaxY) - Math.Max(targetFrame.MinY, other.MinY);
                    if (gap <= 24.0 && overlapY >= 64.0) return true;
                }
                return false;
            case DockScreenEdge.Right:
                foreach (var other in allFrames)
                {
                    if (other == targetFrame) continue;
                    double gap = Math.Abs(other.MinX - targetFrame.MaxX);
                    double overlapY = Math.Min(targetFrame.MaxY, other.MaxY) - Math.Max(targetFrame.MinY, other.MinY);
                    if (gap <= 24.0 && overlapY >= 64.0) return true;
                }
                return false;
            default:
                return false;
        }
    }

    public static bool DoesAnyWindowOverlapZone(RectD screenFrame, RectD dodgeZone, IEnumerable<RectD> windowRects)
    {
        foreach (var win in windowRects)
        {
            var onScreen = win.Intersection(screenFrame);
            if (onScreen.Width < 80.0 || onScreen.Height < 60.0) continue;
            // Ignore desktop wallpaper shell windows that cover >=99% of the entire virtual desktop
            var hit = onScreen.Intersection(dodgeZone);
            if (hit.Width >= 6.0 && hit.Height >= 6.0) return true;
        }
        return false;
    }

    public static bool DoesAnyWindowFillScreen(RectD screenFrame, IEnumerable<RectD> windowRects)
    {
        foreach (var win in windowRects)
        {
            var onScreen = win.Intersection(screenFrame);
            if (onScreen.Width >= screenFrame.Width - 2.0 &&
                onScreen.Height >= screenFrame.Height - 2.0)
            {
                return true;
            }
        }
        return false;
    }

    public static List<string> ResolvePersistentScreenKeys(IReadOnlyList<ScreenIdentityDescriptor> descriptors)
    {
        if (descriptors.Count == 0) return new List<string>();
        var counts = new Dictionary<string, int>();
        foreach (var d in descriptors)
        {
            counts[d.BaseKey] = counts.GetValueOrDefault(d.BaseKey) + 1;
        }

        var resolved = descriptors.Select(d => d.BaseKey).ToList();
        foreach (var kv in counts.Where(kv => kv.Value > 1))
        {
            var group = descriptors
                .Where(d => d.BaseKey == kv.Key)
                .OrderBy(d => d.OriginX)
                .ThenBy(d => d.OriginY)
                .ThenBy(d => d.UnitNumber)
                .ThenBy(d => d.DisplayID)
                .ThenBy(d => d.Index)
                .ToList();

            for (int slot = 0; slot < group.Count; slot++)
            {
                int idx = group[slot].Index;
                if (idx >= 0 && idx < resolved.Count)
                {
                    resolved[idx] = $"{kv.Key}@slot:{slot + 1}";
                }
            }
        }

        return resolved;
    }
}
