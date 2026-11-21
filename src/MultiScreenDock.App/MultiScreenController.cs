using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.Win32;
using MultiScreenDock.Core;
using Application = System.Windows.Application;

namespace MultiScreenDock.App;

public sealed class MultiScreenController : IDisposable
{
    public static MultiScreenController Shared { get; } = new();

    private readonly Dictionary<string, DockBarWindow> _panels = new();
    private readonly Dictionary<string, DateTime> _edgeEnterTime = new();
    private readonly Dictionary<string, DateTime> _keepAliveExitTime = new();
    private readonly DispatcherTimer _fastTimer;
    private readonly DispatcherTimer _modelSyncTimer;
    private NotifyIcon? _trayIcon;

    private MultiScreenController()
    {
        _fastTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _fastTimer.Tick += (_, _) => EvaluateVisibilityTick();

        _modelSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(850)
        };
        _modelSyncTimer.Tick += (_, _) => SyncModelAcrossPanels();
    }

    public void Start()
    {
        SetupTrayIcon();
        DockSettings.Shared.OnChange += () =>
        {
            RebuildScreens();
            RefreshTrayMenu();
        };
        SystemEvents.DisplaySettingsChanged += (_, _) => RebuildScreens();
        SystemEvents.UserPreferenceChanged += (_, _) =>
        {
            foreach (var panel in _panels.Values) panel.RefreshThemeAndPosition();
        };

        RebuildScreens();
        _fastTimer.Start();
        _modelSyncTimer.Start();
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Text = "MultiScreenDock",
            Icon = SystemIcons.Application,
            Visible = true
        };
        RefreshTrayMenu();
    }

    public void RefreshTrayMenu()
    {
        if (_trayIcon == null) return;
        _trayIcon.Text = L10n.TooltipStatus.Length > 63
            ? L10n.TooltipStatus.Substring(0, 63)
            : L10n.TooltipStatus;

        var s = DockSettings.Shared;
        var menu = new ContextMenuStrip();

        // Visibility Modes
        var autoItem = new ToolStripMenuItem(L10n.ModeAutoHide) { Checked = s.VisibilityMode == DockVisibilityMode.AutoHide };
        autoItem.Click += (_, _) => s.VisibilityMode = DockVisibilityMode.AutoHide;
        menu.Items.Add(autoItem);

        var smartItem = new ToolStripMenuItem(L10n.ModeSmartAutoHide) { Checked = s.VisibilityMode == DockVisibilityMode.SmartAutoHide };
        smartItem.Click += (_, _) => s.VisibilityMode = DockVisibilityMode.SmartAutoHide;
        menu.Items.Add(smartItem);

        var fixedItem = new ToolStripMenuItem(L10n.ModeAlwaysVisible) { Checked = s.VisibilityMode == DockVisibilityMode.AlwaysVisible };
        fixedItem.Click += (_, _) => s.VisibilityMode = DockVisibilityMode.AlwaysVisible;
        menu.Items.Add(fixedItem);

        menu.Items.Add(new ToolStripSeparator());

        // Position Submenu (Bottom / Left / Right)
        var posMenu = new ToolStripMenuItem(L10n.PositionMenu);
        foreach (var (title, edge) in new[]
        {
            (L10n.PositionBottom, DockScreenEdge.Bottom),
            (L10n.PositionLeft, DockScreenEdge.Left),
            (L10n.PositionRight, DockScreenEdge.Right)
        })
        {
            var pItem = new ToolStripMenuItem(title) { Checked = s.DockPosition == edge };
            pItem.Click += (_, _) => s.DockPosition = edge;
            posMenu.DropDownItems.Add(pItem);
        }
        menu.Items.Add(posMenu);

        // Appearance Submenu (System / Light Glass / Dark Glass)
        var appMenu = new ToolStripMenuItem(L10n.AppearanceMenu);
        foreach (var (title, mode) in new[]
        {
            (L10n.AppearanceSystemAuto, DockAppearanceMode.System),
            (L10n.AppearanceLight, DockAppearanceMode.Light),
            (L10n.AppearanceDark, DockAppearanceMode.Dark)
        })
        {
            var aItem = new ToolStripMenuItem(title) { Checked = s.AppearanceMode == mode };
            aItem.Click += (_, _) => s.AppearanceMode = mode;
            appMenu.DropDownItems.Add(aItem);
        }
        menu.Items.Add(appMenu);

        // Icon Size Submenu
        var sizeMenu = new ToolStripMenuItem(L10n.IconSizeMenu);
        foreach (double sz in new[] { 28.0, 36.0, 44.0, 52.0 })
        {
            var szItem = new ToolStripMenuItem($"{(int)sz} px") { Checked = Math.Abs(s.IconSize - sz) < 1.0 };
            szItem.Click += (_, _) => s.IconSize = sz;
            sizeMenu.DropDownItems.Add(szItem);
        }
        menu.Items.Add(sizeMenu);

        // Folder Stack Style Submenu
        var stackMenu = new ToolStripMenuItem(L10n.StackStyleMenu);
        foreach (var (title, st) in new[]
        {
            (L10n.StackStyleFan, FolderStackStylePreference.Fan),
            (L10n.StackStyleGrid, FolderStackStylePreference.Grid),
            (L10n.StackStyleList, FolderStackStylePreference.List)
        })
        {
            var stItem = new ToolStripMenuItem(title) { Checked = s.FolderStackStyle == st };
            stItem.Click += (_, _) => s.FolderStackStyle = st;
            stackMenu.DropDownItems.Add(stItem);
        }
        menu.Items.Add(stackMenu);

        // Language Submenu
        var langMenu = new ToolStripMenuItem(L10n.LanguageMenu);
        foreach (var lang in Enum.GetValues<AppLanguage>())
        {
            string label = lang == AppLanguage.System
                ? $"Auto ({L10n.SystemDetectedLanguage.NativeDisplayName()})"
                : lang.NativeDisplayName();
            var lItem = new ToolStripMenuItem(label) { Checked = s.AppLanguage == lang };
            lItem.Click += (_, _) => s.AppLanguage = lang;
            langMenu.DropDownItems.Add(lItem);
        }
        menu.Items.Add(langMenu);

        menu.Items.Add(new ToolStripSeparator());

        var magItem = new ToolStripMenuItem(L10n.HoverMagnification) { Checked = s.HoverMagnification };
        magItem.Click += (_, _) => s.HoverMagnification = !s.HoverMagnification;
        menu.Items.Add(magItem);

        var unpinItem = new ToolStripMenuItem(L10n.ShowUnpinnedApps) { Checked = s.ShowRunningUnpinned };
        unpinItem.Click += (_, _) => s.ShowRunningUnpinned = !s.ShowRunningUnpinned;
        menu.Items.Add(unpinItem);

        var fsItem = new ToolStripMenuItem(L10n.KeepOverFullScreen) { Checked = s.ShowOverFullScreen };
        fsItem.Click += (_, _) => s.ShowOverFullScreen = !s.ShowOverFullScreen;
        menu.Items.Add(fsItem);

        var skipPrimaryItem = new ToolStripMenuItem(L10n.SkipPrimaryDisplay) { Checked = s.SkipPrimaryDisplay };
        skipPrimaryItem.Click += (_, _) => s.SkipPrimaryDisplay = !s.SkipPrimaryDisplay;
        menu.Items.Add(skipPrimaryItem);

        menu.Items.Add(new ToolStripSeparator());

        var syncItem = new ToolStripMenuItem(L10n.SyncNow);
        syncItem.Click += (_, _) =>
        {
            DockModelReader.Shared.ReloadPinnedApps(clearIcons: true);
            SyncModelAcrossPanels();
        };
        menu.Items.Add(syncItem);

        var loginItem = new ToolStripMenuItem(L10n.LaunchAtLogin) { Checked = Win32Interop.IsLaunchAtLoginEnabled() };
        loginItem.Click += (_, _) =>
        {
            Win32Interop.SetLaunchAtLogin(!Win32Interop.IsLaunchAtLoginEnabled());
            RefreshTrayMenu();
        };
        menu.Items.Add(loginItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem(L10n.QuitApp);
        quitItem.Click += (_, _) =>
        {
            Dispose();
            Application.Current.Shutdown();
        };
        menu.Items.Add(quitItem);

        _trayIcon.ContextMenuStrip = menu;
    }

    public void RebuildScreens()
    {
        foreach (var old in _panels.Values) old.Close();
        _panels.Clear();
        _edgeEnterTime.Clear();
        _keepAliveExitTime.Clear();

        var settings = DockSettings.Shared;
        var screens = Screen.AllScreens;
        var descriptors = screens.Select((scr, idx) =>
            new ScreenIdentityDescriptor(
                Index: idx,
                BaseKey: scr.DeviceName,
                OriginX: scr.Bounds.X,
                OriginY: scr.Bounds.Y,
                UnitNumber: (uint)idx,
                DisplayID: (uint)idx
            )).ToList();

        var keys = DockLayoutMath.ResolvePersistentScreenKeys(descriptors);
        var items = DockModelReader.Shared.BuildItems(settings.ShowRunningUnpinned);

        for (int i = 0; i < screens.Length; i++)
        {
            var scr = screens[i];
            if (settings.SkipPrimaryDisplay && scr.Primary) continue;
            string key = keys[i];
            if (settings.DisabledScreens.Contains(key)) continue;

            var bounds = new RectD(scr.Bounds.X, scr.Bounds.Y, scr.Bounds.Width, scr.Bounds.Height);
            var win = new DockBarWindow(bounds);
            win.OnRequestSettingsContextMenu += () =>
            {
                if (_trayIcon?.ContextMenuStrip != null && Win32Interop.GetCursorPos(out var pt))
                {
                    _trayIcon.ContextMenuStrip.Show(pt.X, pt.Y);
                }
            };
            win.RefreshThemeAndPosition();
            win.UpdateItems(items);
            win.Show();
            _panels[key] = win;
        }

        EvaluateVisibilityTick();
    }

    private void SyncModelAcrossPanels()
    {
        if (_panels.Count == 0) return;
        var items = DockModelReader.Shared.BuildItems(DockSettings.Shared.ShowRunningUnpinned);
        foreach (var panel in _panels.Values)
        {
            panel.UpdateItems(items);
        }
    }

    private void EvaluateVisibilityTick()
    {
        if (_panels.Count == 0) return;
        if (!Win32Interop.GetCursorPos(out var pt)) return;

        var settings = DockSettings.Shared;
        var edge = settings.DockPosition;
        var allFrames = _panels.Values.Select(p => p.ScreenBounds).ToList();
        var ownHandles = _panels.Values.Select(p => p.Hwnd).Where(h => h != IntPtr.Zero).ToHashSet();

        List<RectD>? windowRects = null;
        if (!settings.ShowOverFullScreen || settings.VisibilityMode == DockVisibilityMode.SmartAutoHide)
        {
            windowRects = Win32Interop.GetVisibleStandardWindowRects(ownHandles);
        }

        DateTime now = DateTime.UtcNow;

        foreach (var (key, panel) in _panels)
        {
            var screen = panel.ScreenBounds;

            // 1. Per-display fullscreen suppression
            if (!settings.ShowOverFullScreen &&
                windowRects != null &&
                DockLayoutMath.DoesAnyWindowFillScreen(screen, windowRects))
            {
                panel.Visibility = Visibility.Hidden;
                continue;
            }

            // 2. Always Visible mode
            if (settings.VisibilityMode == DockVisibilityMode.AlwaysVisible)
            {
                panel.Visibility = Visibility.Visible;
                continue;
            }

            bool hasNeighbor = DockLayoutMath.HasAdjacentMonitor(edge, screen, allFrames);
            var trigZone = DockLayoutMath.TriggerZone(screen, panel.BarSpan, panel.BarThickness, hasNeighbor, edge);
            var keepZone = DockLayoutMath.KeepAliveZone(screen, panel.BarSpan, panel.BarThickness, settings.EdgeMargin, edge);
            bool inTrig = trigZone.Contains(pt.X, pt.Y);
            bool inKeep = keepZone.Contains(pt.X, pt.Y);

            // 3. Smart Auto-Hide mode: visible unless a window overlaps the dodge zone
            if (settings.VisibilityMode == DockVisibilityMode.SmartAutoHide && windowRects != null)
            {
                var dodgeZone = DockLayoutMath.DockDodgeZone(screen, panel.BarSpan, panel.BarThickness, settings.EdgeMargin, edge);
                bool overlapped = DockLayoutMath.DoesAnyWindowOverlapZone(screen, dodgeZone, windowRects);
                if (!overlapped)
                {
                    panel.Visibility = Visibility.Visible;
                    _edgeEnterTime.Remove(key);
                    _keepAliveExitTime.Remove(key);
                    continue;
                }
            }

            // 4. Edge Auto-Hide dwell & hide delay
            if (panel.Visibility != Visibility.Visible)
            {
                if (inTrig)
                {
                    if (!_edgeEnterTime.TryGetValue(key, out DateTime entered))
                    {
                        _edgeEnterTime[key] = now;
                    }
                    else if ((now - entered).TotalSeconds >= settings.Responsiveness.EdgeDwellSeconds(hasNeighbor))
                    {
                        panel.Visibility = Visibility.Visible;
                        _keepAliveExitTime.Remove(key);
                    }
                }
                else
                {
                    _edgeEnterTime.Remove(key);
                }
            }
            else
            {
                if (inKeep)
                {
                    _keepAliveExitTime.Remove(key);
                }
                else
                {
                    if (!_keepAliveExitTime.TryGetValue(key, out DateTime exited))
                    {
                        _keepAliveExitTime[key] = now;
                    }
                    else if ((now - exited).TotalSeconds >= settings.Responsiveness.HideDelaySeconds())
                    {
                        panel.Visibility = Visibility.Hidden;
                        _edgeEnterTime.Remove(key);
                        _keepAliveExitTime.Remove(key);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _fastTimer.Stop();
        _modelSyncTimer.Stop();
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
        foreach (var panel in _panels.Values) panel.Close();
        _panels.Clear();
    }
}
