using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using MultiScreenDock.Core;

namespace MultiScreenDock.App;

public sealed class DockBarWindow : Window
{
    private readonly Canvas _rootCanvas;
    private readonly Border _glassBar;
    private readonly Border _hoverBubble;
    private readonly TextBlock _hoverBubbleText;

    private List<DockItemModel> _items = new();
    private readonly List<FrameworkElement> _tileElements = new();
    private double? _pointerAlongBar;
    private int? _lockedIndex;
    private double? _lockedLocalFraction;

    public RectD ScreenBounds { get; set; }
    public double BarSpan { get; private set; } = 420;
    public double BarThickness { get; private set; } = 56;
    public IntPtr Hwnd => new WindowInteropHelper(this).Handle;

    public event Action? OnRequestSettingsContextMenu;

    public DockBarWindow(RectD screenBounds)
    {
        ScreenBounds = screenBounds;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        ShowActivated = false;

        _rootCanvas = new Canvas { Background = Brushes.Transparent };
        _glassBar = new Border
        {
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(1.4)
        };
        _rootCanvas.Children.Add(_glassBar);

        _hoverBubbleText = new TextBlock
        {
            FontSize = 12.5,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(10, 4, 10, 4)
        };
        _hoverBubble = new Border
        {
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = _hoverBubbleText,
            Visibility = Visibility.Collapsed
        };
        _rootCanvas.Children.Add(_hoverBubble);

        Content = _rootCanvas;

        SourceInitialized += (_, _) => Win32Interop.MakeNonActivatingToolWindow(this);
        MouseMove += OnMouseMove;
        MouseLeave += OnMouseLeave;
        MouseRightButtonUp += (_, _) => OnRequestSettingsContextMenu?.Invoke();
    }

    public void UpdateItems(List<DockItemModel> items)
    {
        _items = items;
        RebuildTiles();
        LayoutWave();
    }

    public void RefreshThemeAndPosition()
    {
        bool isDark = DockSettings.Shared.IsEffectiveDarkAppearance(Win32Interop.IsSystemDarkMode());
        if (isDark)
        {
            _glassBar.Background = new SolidColorBrush(Color.FromArgb(204, 16, 22, 36));
            _glassBar.BorderBrush = new SolidColorBrush(Color.FromArgb(95, 130, 210, 255));
            _hoverBubble.Background = new SolidColorBrush(Color.FromArgb(238, 18, 25, 42));
            _hoverBubble.BorderBrush = new SolidColorBrush(Color.FromArgb(95, 130, 210, 255));
            _hoverBubbleText.Foreground = Brushes.White;
        }
        else
        {
            _glassBar.Background = new SolidColorBrush(Color.FromArgb(215, 245, 248, 255));
            _glassBar.BorderBrush = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
            _hoverBubble.Background = new SolidColorBrush(Color.FromArgb(245, 250, 252, 255));
            _hoverBubble.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 160, 175, 195));
            _hoverBubbleText.Foreground = new SolidColorBrush(Color.FromRgb(20, 24, 32));
        }

        RebuildTiles();
        LayoutWave();
    }

    private void RebuildTiles()
    {
        foreach (var el in _tileElements)
        {
            _rootCanvas.Children.Remove(el);
        }
        _tileElements.Clear();

        bool isDark = DockSettings.Shared.IsEffectiveDarkAppearance(Win32Interop.IsSystemDarkMode());
        var dotBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(240, 255, 255, 255))
            : new SolidColorBrush(Color.FromArgb(225, 32, 38, 48));
        var sepBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(75, 255, 255, 255))
            : new SolidColorBrush(Color.FromArgb(65, 30, 40, 55));

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item.Kind == DockItemKind.Separator)
            {
                var sep = new Rectangle
                {
                    Fill = sepBrush,
                    RadiusX = 0.75,
                    RadiusY = 0.75
                };
                _tileElements.Add(sep);
                _rootCanvas.Children.Add(sep);
                continue;
            }

            var tileCanvas = new Grid
            {
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                AllowDrop = true,
                Tag = i
            };

            if (item.Icon != null)
            {
                var img = new Image
                {
                    Source = item.Icon,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                tileCanvas.Children.Add(img);
            }

            if (item.IsRunning && item.Kind != DockItemKind.RecycleBin)
            {
                var dot = new Ellipse
                {
                    Width = item.IsFrontmost ? 5.5 : 4.5,
                    Height = item.IsFrontmost ? 5.5 : 4.5,
                    Fill = dotBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, -6)
                };
                tileCanvas.Children.Add(dot);
            }

            int capturedIndex = i;
            tileCanvas.MouseLeftButtonDown += (_, e) =>
            {
                _lockedIndex = capturedIndex;
                var pt = e.GetPosition(tileCanvas);
                bool vert = DockSettings.Shared.DockPosition.IsVertical();
                double span = vert ? Math.Max(1, tileCanvas.ActualHeight) : Math.Max(1, tileCanvas.ActualWidth);
                _lockedLocalFraction = (vert ? pt.Y : pt.X) / span;
            };
            tileCanvas.MouseLeftButtonUp += (_, _) =>
            {
                _lockedIndex = null;
                _lockedLocalFraction = null;
                ActivateItem(item, tileCanvas);
            };
            tileCanvas.Drop += (_, e) => OnTileFileDrop(item, e);

            _tileElements.Add(tileCanvas);
            _rootCanvas.Children.Add(tileCanvas);
        }

        // Keep hover bubble on top
        _rootCanvas.Children.Remove(_hoverBubble);
        _rootCanvas.Children.Add(_hoverBubble);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(_rootCanvas);
        var edge = DockSettings.Shared.DockPosition;
        _pointerAlongBar = edge.IsVertical() ? pos.Y : pos.X;
        LayoutWave();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        _pointerAlongBar = null;
        _lockedIndex = null;
        _lockedLocalFraction = null;
        _hoverBubble.Visibility = Visibility.Collapsed;
        LayoutWave();
    }

    public void LayoutWave()
    {
        if (_items.Count == 0) return;

        var settings = DockSettings.Shared;
        var edge = settings.DockPosition;
        bool vert = edge.IsVertical();
        double availSpan = vert ? ScreenBounds.Height : ScreenBounds.Width;

        int appCount = _items.Count(it => it.Kind != DockItemKind.Separator);
        int sepCount = _items.Count - appCount;
        double baseIcon = DockLayoutMath.EffectiveIconSize(settings.IconSize, appCount, sepCount, availSpan);
        double tilePad = 3.0;
        double outerPad = 10.0;

        var isSep = _items.Select(it => it.Kind == DockItemKind.Separator).ToList();
        var slots = DockLayoutMath.ComputeWaveSlots(
            isSep,
            baseIcon,
            tilePad,
            outerPad,
            _pointerAlongBar,
            _lockedIndex,
            _lockedLocalFraction,
            settings.HoverMagnification);

        double minOrigin = slots.Min(s => s.Origin) - outerPad;
        double maxEnd = slots.Max(s => s.Origin + s.Span) + outerPad;
        BarSpan = Math.Max(120, maxEnd - minOrigin);
        BarThickness = baseIcon + 18.0;

        double maxWaveReserve = baseIcon * 0.65 + 48.0;
        double winW = vert ? (BarThickness + maxWaveReserve + settings.EdgeMargin) : Math.Min(ScreenBounds.Width, BarSpan + 120);
        double winH = vert ? Math.Min(ScreenBounds.Height, BarSpan + 120) : (BarThickness + maxWaveReserve + settings.EdgeMargin);

        Width = winW;
        Height = winH;

        double barOffsetAlong = ( (vert ? winH : winW) - BarSpan ) * 0.5 - minOrigin;

        if (!vert)
        {
            Left = ScreenBounds.MidX - winW * 0.5;
            Top = ScreenBounds.MaxY - winH;
            _glassBar.Width = BarSpan;
            _glassBar.Height = BarThickness;
            Canvas.SetLeft(_glassBar, minOrigin + barOffsetAlong);
            Canvas.SetTop(_glassBar, winH - BarThickness - settings.EdgeMargin);
        }
        else if (edge == DockScreenEdge.Left)
        {
            Left = ScreenBounds.MinX;
            Top = ScreenBounds.MidY - winH * 0.5;
            _glassBar.Width = BarThickness;
            _glassBar.Height = BarSpan;
            Canvas.SetLeft(_glassBar, settings.EdgeMargin);
            Canvas.SetTop(_glassBar, minOrigin + barOffsetAlong);
        }
        else
        {
            Left = ScreenBounds.MaxX - winW;
            Top = ScreenBounds.MidY - winH * 0.5;
            _glassBar.Width = BarThickness;
            _glassBar.Height = BarSpan;
            Canvas.SetLeft(_glassBar, winW - BarThickness - settings.EdgeMargin);
            Canvas.SetTop(_glassBar, minOrigin + barOffsetAlong);
        }

        int? hoveredIdx = null;
        double bestScale = 1.0;

        for (int i = 0; i < slots.Count && i < _tileElements.Count; i++)
        {
            var s = slots[i];
            var el = _tileElements[i];
            double along = s.Origin + barOffsetAlong;

            if (isSep[i])
            {
                if (!vert)
                {
                    el.Width = 1.5;
                    el.Height = BarThickness - 18;
                    Canvas.SetLeft(el, along + s.Span * 0.5);
                    Canvas.SetTop(el, winH - BarThickness - settings.EdgeMargin + 9);
                }
                else
                {
                    el.Width = BarThickness - 18;
                    el.Height = 1.5;
                    Canvas.SetLeft(el, edge == DockScreenEdge.Left ? (settings.EdgeMargin + 9) : (winW - BarThickness - settings.EdgeMargin + 9));
                    Canvas.SetTop(el, along + s.Span * 0.5);
                }
                continue;
            }

            el.Width = s.MagnifiedIconSize;
            el.Height = s.MagnifiedIconSize;

            if (!vert)
            {
                double x = along + (s.Span - s.MagnifiedIconSize) * 0.5;
                double y = winH - settings.EdgeMargin - 9 - s.MagnifiedIconSize - s.LiftOffset;
                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
            }
            else if (edge == DockScreenEdge.Left)
            {
                double x = settings.EdgeMargin + 9 + s.LiftOffset;
                double y = along + (s.Span - s.MagnifiedIconSize) * 0.5;
                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
            }
            else
            {
                double x = winW - settings.EdgeMargin - 9 - s.MagnifiedIconSize - s.LiftOffset;
                double y = along + (s.Span - s.MagnifiedIconSize) * 0.5;
                Canvas.SetLeft(el, x);
                Canvas.SetTop(el, y);
            }

            if (_pointerAlongBar.HasValue &&
                _pointerAlongBar.Value >= along &&
                _pointerAlongBar.Value <= along + s.Span &&
                s.Scale >= bestScale)
            {
                bestScale = s.Scale;
                hoveredIdx = i;
            }
        }

        if (hoveredIdx.HasValue)
        {
            var item = _items[hoveredIdx.Value];
            var el = _tileElements[hoveredIdx.Value];
            _hoverBubbleText.Text = item.Label;
            _hoverBubble.Visibility = Visibility.Visible;
            _hoverBubble.Measure(new Size(400, 80));
            var bSz = _hoverBubble.DesiredSize;

            double ex = Canvas.GetLeft(el);
            double ey = Canvas.GetTop(el);

            if (!vert)
            {
                Canvas.SetLeft(_hoverBubble, ex + el.Width * 0.5 - bSz.Width * 0.5);
                Canvas.SetTop(_hoverBubble, Math.Max(2, ey - bSz.Height - 8));
            }
            else if (edge == DockScreenEdge.Left)
            {
                Canvas.SetLeft(_hoverBubble, ex + el.Width + 10);
                Canvas.SetTop(_hoverBubble, ey + el.Height * 0.5 - bSz.Height * 0.5);
            }
            else
            {
                Canvas.SetLeft(_hoverBubble, Math.Max(2, ex - bSz.Width - 10));
                Canvas.SetTop(_hoverBubble, ey + el.Height * 0.5 - bSz.Height * 0.5);
            }
        }
        else
        {
            _hoverBubble.Visibility = Visibility.Collapsed;
        }
    }

    private void ActivateItem(DockItemModel item, FrameworkElement tileElement)
    {
        try
        {
            switch (item.Kind)
            {
                case DockItemKind.FileExplorer:
                    Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true });
                    break;
                case DockItemKind.RecycleBin:
                    Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true });
                    break;
                case DockItemKind.Directory:
                    bool isDark = DockSettings.Shared.IsEffectiveDarkAppearance(Win32Interop.IsSystemDarkMode());
                    var screenPt = tileElement.PointToScreen(new Point(0, 0));
                    var anchor = new Rect(screenPt.X, screenPt.Y, tileElement.ActualWidth, tileElement.ActualHeight);
                    var stackWin = new FolderStackWindow(item.TargetPath, anchor, DockSettings.Shared.DockPosition, isDark);
                    stackWin.Show();
                    stackWin.Activate();
                    break;
                case DockItemKind.Application:
                    if (item.IsRunning && item.RunningWindowHandle != IntPtr.Zero)
                    {
                        if (Win32Interop.IsIconic(item.RunningWindowHandle))
                        {
                            Win32Interop.ShowWindow(item.RunningWindowHandle, 9); // SW_RESTORE
                        }
                        Win32Interop.SetForegroundWindow(item.RunningWindowHandle);
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo(item.TargetPath) { UseShellExecute = true });
                    }
                    break;
            }
        }
        catch { }
    }

    private static void OnTileFileDrop(DockItemModel item, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0) return;

        try
        {
            if (item.Kind == DockItemKind.Directory && Directory.Exists(item.TargetPath))
            {
                foreach (string f in files)
                {
                    string dest = System.IO.Path.Combine(item.TargetPath, System.IO.Path.GetFileName(f));
                    if (!string.Equals(f, dest, StringComparison.OrdinalIgnoreCase) && !File.Exists(dest))
                    {
                        File.Move(f, dest);
                    }
                }
            }
            else if (item.Kind == DockItemKind.Application)
            {
                string args = string.Join(" ", files.Select(f => $"\"{f}\""));
                Process.Start(new ProcessStartInfo(item.TargetPath, args) { UseShellExecute = true });
            }
        }
        catch { }
    }
}
