using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiScreenDock.Core;
using Xunit;

namespace MultiScreenDock.Tests;

public class LayoutAndSettingsTests
{
    [Fact]
    public void TriggerZone_IsStrictlyContainedInKeepAliveZone_OnAllThreeEdges()
    {
        var screen = new RectD(1920, 0, 2560, 1440);
        foreach (var edge in Enum.GetValues<DockScreenEdge>())
        {
            var trigNoNeighbor = DockLayoutMath.TriggerZone(screen, 580, 56, false, edge);
            var trigWithNeighbor = DockLayoutMath.TriggerZone(screen, 580, 56, true, edge);
            var keepAlive = DockLayoutMath.KeepAliveZone(screen, 580, 56, 8, edge);

            Assert.True(keepAlive.Contains(trigNoNeighbor), $"keepAlive must contain trigger (no neighbor) on {edge}");
            Assert.True(keepAlive.Contains(trigWithNeighbor), $"keepAlive must contain trigger (with neighbor) on {edge}");
        }
    }

    [Fact]
    public void WaveMagnification_WithFocalLock_KeepsClickedTileAnchoredUnderPointer()
    {
        var isSep = new List<bool> { false, false, false, false, true, false, false };
        double baseIcon = 40.0;
        double pad = 3.0;
        double outer = 10.0;

        int lockedIdx = 2;
        double lockedFrac = 0.5;
        double pointer = 185.0;

        var slots = DockLayoutMath.ComputeWaveSlots(
            isSep,
            baseIcon,
            pad,
            outer,
            pointerAlongBar: pointer,
            lockedIndex: lockedIdx,
            lockedLocalFraction: lockedFrac,
            magnificationEnabled: true);

        double actualAnchor = slots[lockedIdx].Origin + slots[lockedIdx].Span * lockedFrac;
        Assert.Equal(pointer, actualAnchor, precision: 2);
        Assert.True(slots[lockedIdx].MagnifiedIconSize > baseIcon);
    }

    [Fact]
    public void StackedAndSideBySideDisplays_AreDetectedAccurately()
    {
        var upper = new RectD(0, 0, 2560, 1440);
        var lower = new RectD(0, 1440, 2560, 1440);
        var right = new RectD(2560, 1440, 1920, 1080);
        var all = new[] { upper, lower, right };

        Assert.True(DockLayoutMath.HasMonitorDirectlyBelow(upper, all));
        Assert.False(DockLayoutMath.HasMonitorDirectlyAbove(upper, all));
        Assert.True(DockLayoutMath.HasMonitorDirectlyAbove(lower, all));
        Assert.True(DockLayoutMath.HasAdjacentMonitor(DockScreenEdge.Right, lower, all));
        Assert.True(DockLayoutMath.HasAdjacentMonitor(DockScreenEdge.Left, right, all));
    }

    [Fact]
    public void SmartAutoHide_And_FullscreenDetection_WorkPerDisplay()
    {
        var screen1 = new RectD(0, 0, 2560, 1440);
        var screen2 = new RectD(2560, 0, 2560, 1440);
        var dodge1 = DockLayoutMath.DockDodgeZone(screen1, 580, 56, 8, DockScreenEdge.Bottom);

        var highWindow = new RectD(100, 100, 1200, 800); // Above bottom dodge zone
        var overlappingWindow = new RectD(900, 700, 1000, 730); // Reaches Y=1430 (inside bottom dodge zone)
        var fullscreenOnScreen2 = new RectD(2560, 0, 2560, 1440);

        Assert.False(DockLayoutMath.DoesAnyWindowOverlapZone(screen1, dodge1, new[] { highWindow, fullscreenOnScreen2 }));
        Assert.True(DockLayoutMath.DoesAnyWindowOverlapZone(screen1, dodge1, new[] { overlappingWindow }));

        Assert.False(DockLayoutMath.DoesAnyWindowFillScreen(screen1, new[] { highWindow, fullscreenOnScreen2 }));
        Assert.True(DockLayoutMath.DoesAnyWindowFillScreen(screen2, new[] { highWindow, fullscreenOnScreen2 }));
    }

    [Fact]
    public void TwinMonitorDisambiguation_AssignsDeterministicSlots()
    {
        var descriptors = new List<ScreenIdentityDescriptor>
        {
            new(0, "GSM-27UL850", 2560, 0, 1, 101),
            new(1, "GSM-27UL850", 0, 0, 0, 100),
            new(2, "BUILTIN-16", 900, 1440, 2, 102)
        };

        var keys = DockLayoutMath.ResolvePersistentScreenKeys(descriptors);
        Assert.Equal("GSM-27UL850@slot:2", keys[0]);
        Assert.Equal("GSM-27UL850@slot:1", keys[1]);
        Assert.Equal("BUILTIN-16", keys[2]);
    }

    [Fact]
    public void SettingsPersistence_And_8LanguageLocalization_AreComplete()
    {
        string tmpFile = Path.Combine(Path.GetTempPath(), $"msd_win_test_{Guid.NewGuid()}.json");
        try
        {
            var s = new DockSettings(tmpFile)
            {
                DockPosition = DockScreenEdge.Left,
                AppearanceMode = DockAppearanceMode.Light,
                IconSize = 120.0 // Should clamp to 80
            };
            Assert.Equal(80.0, s.IconSize);

            var s2 = new DockSettings(tmpFile);
            Assert.Equal(DockScreenEdge.Left, s2.DockPosition);
            Assert.Equal(DockAppearanceMode.Light, s2.AppearanceMode);
            Assert.False(s2.IsEffectiveDarkAppearance(systemIsDark: true));
        }
        finally
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
        }

        foreach (var lang in Enum.GetValues<AppLanguage>().Where(l => l != AppLanguage.System))
        {
            L10n.OverrideLanguageForTesting = lang;
            Assert.False(string.IsNullOrWhiteSpace(L10n.TooltipStatus));
            Assert.False(string.IsNullOrWhiteSpace(L10n.PositionMenu));
            Assert.False(string.IsNullOrWhiteSpace(L10n.AppearanceMenu));
            Assert.False(string.IsNullOrWhiteSpace(L10n.FileExplorerLabel));
            Assert.False(string.IsNullOrWhiteSpace(L10n.RecycleBinLabel));
        }
        L10n.OverrideLanguageForTesting = null;
    }
}
