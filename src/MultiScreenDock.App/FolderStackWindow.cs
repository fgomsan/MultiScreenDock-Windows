using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MultiScreenDock.Core;

namespace MultiScreenDock.App;

public sealed class FolderStackWindow : Window
{
    private string _currentDirectory;
    private readonly string _rootDirectory;
    private readonly StackPanel _itemsHost;
    private readonly TextBlock _headerTitle;
    private readonly Button _backButton;

    public FolderStackWindow(string directoryPath, Rect anchorScreenRect, DockScreenEdge edge, bool isDark)
    {
        _rootDirectory = directoryPath;
        _currentDirectory = directoryPath;

        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.WidthAndHeight;

        var bgBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(238, 18, 25, 42))
            : new SolidColorBrush(Color.FromArgb(242, 245, 248, 255));
        var borderBrush = isDark
            ? new SolidColorBrush(Color.FromArgb(110, 120, 205, 255))
            : new SolidColorBrush(Color.FromArgb(140, 180, 195, 215));
        var fgBrush = isDark ? Brushes.White : Brushes.Black;

        var outerBorder = new Border
        {
            Background = bgBrush,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            MinWidth = 300,
            MaxWidth = 520,
            MaxHeight = 460
        };

        var rootStack = new DockPanel();

        var headerBar = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        _backButton = new Button
        {
            Content = "←",
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(0, 0, 8, 0),
            Visibility = Visibility.Collapsed
        };
        _backButton.Click += (_, _) =>
        {
            var parent = Directory.GetParent(_currentDirectory);
            if (parent != null && parent.FullName.StartsWith(_rootDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _currentDirectory = parent.FullName;
                PopulateDirectory();
            }
        };
        DockPanel.SetDock(_backButton, Dock.Left);
        headerBar.Children.Add(_backButton);

        var openInExplorerBtn = new Button
        {
            Content = "↗",
            Padding = new Thickness(8, 2, 8, 2),
            ToolTip = L10n.FileExplorerLabel
        };
        openInExplorerBtn.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_currentDirectory}\"") { UseShellExecute = true });
                Close();
            }
            catch { }
        };
        DockPanel.SetDock(openInExplorerBtn, Dock.Right);
        headerBar.Children.Add(openInExplorerBtn);

        _headerTitle = new TextBlock
        {
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = fgBrush,
            VerticalAlignment = VerticalAlignment.Center
        };
        headerBar.Children.Add(_headerTitle);

        DockPanel.SetDock(headerBar, Dock.Top);
        rootStack.Children.Add(headerBar);

        _itemsHost = new StackPanel { Orientation = Orientation.Vertical };
        var scroller = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 380,
            Content = _itemsHost
        };
        rootStack.Children.Add(scroller);

        outerBorder.Child = rootStack;
        Content = outerBorder;

        PopulateDirectory();

        Loaded += (_, _) =>
        {
            Left = edge switch
            {
                DockScreenEdge.Left => anchorScreenRect.Right + 10,
                DockScreenEdge.Right => anchorScreenRect.Left - ActualWidth - 10,
                _ => Math.Max(12, anchorScreenRect.Left + anchorScreenRect.Width * 0.5 - ActualWidth * 0.5)
            };
            Top = edge switch
            {
                DockScreenEdge.Bottom => Math.Max(12, anchorScreenRect.Top - ActualHeight - 10),
                _ => Math.Max(12, anchorScreenRect.Top + anchorScreenRect.Height * 0.5 - ActualHeight * 0.5)
            };
        };

        Deactivated += (_, _) => Close();
    }

    private void PopulateDirectory()
    {
        _itemsHost.Children.Clear();
        _headerTitle.Text = Path.GetFileName(_currentDirectory);
        _backButton.Visibility = string.Equals(_currentDirectory, _rootDirectory, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Collapsed
            : Visibility.Visible;

        bool isDark = DockSettings.Shared.IsEffectiveDarkAppearance(Win32Interop.IsSystemDarkMode());
        var fgBrush = isDark ? Brushes.White : Brushes.Black;
        var style = DockSettings.Shared.FolderStackStyle;

        try
        {
            var entries = new DirectoryInfo(_currentDirectory)
                .EnumerateFileSystemInfos()
                .Where(e => (e.Attributes & FileAttributes.Hidden) == 0)
                .OrderByDescending(e => e.LastWriteTimeUtc)
                .Take(36)
                .ToList();

            if (style == FolderStackStylePreference.Grid)
            {
                var wrap = new WrapPanel { Width = 440 };
                foreach (var entry in entries)
                {
                    wrap.Children.Add(CreateGridTile(entry, fgBrush));
                }
                _itemsHost.Children.Add(wrap);
            }
            else
            {
                foreach (var entry in entries)
                {
                    _itemsHost.Children.Add(CreateListRow(entry, fgBrush, isFan: style == FolderStackStylePreference.Fan));
                }
            }
        }
        catch { }
    }

    private UIElement CreateGridTile(FileSystemInfo entry, Brush fgBrush)
    {
        var panel = new StackPanel
        {
            Width = 102,
            Margin = new Thickness(4),
            Cursor = Cursors.Hand,
            Background = Brushes.Transparent
        };
        var icon = Win32Interop.ExtractFileIcon(entry.FullName);
        if (icon != null)
        {
            panel.Children.Add(new Image
            {
                Source = icon,
                Width = 42,
                Height = 42,
                Margin = new Thickness(0, 4, 0, 4)
            });
        }
        panel.Children.Add(new TextBlock
        {
            Text = entry.Name,
            Foreground = fgBrush,
            FontSize = 11.5,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        AttachEntryInteractions(panel, entry);
        return panel;
    }

    private UIElement CreateListRow(FileSystemInfo entry, Brush fgBrush, bool isFan)
    {
        var row = new DockPanel
        {
            Margin = new Thickness(isFan ? 12 : 2, 3, 2, 3),
            Cursor = Cursors.Hand,
            Background = Brushes.Transparent
        };
        var icon = Win32Interop.ExtractFileIcon(entry.FullName);
        if (icon != null)
        {
            var img = new Image { Source = icon, Width = 24, Height = 24, Margin = new Thickness(0, 0, 8, 0) };
            DockPanel.SetDock(img, Dock.Left);
            row.Children.Add(img);
        }
        row.Children.Add(new TextBlock
        {
            Text = entry.Name,
            Foreground = fgBrush,
            FontSize = 12.5,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        AttachEntryInteractions(row, entry);
        return row;
    }

    private void AttachEntryInteractions(FrameworkElement element, FileSystemInfo entry)
    {
        Point? dragStart = null;
        element.MouseLeftButtonDown += (_, e) => { dragStart = e.GetPosition(element); };
        element.MouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && dragStart.HasValue)
            {
                var pos = e.GetPosition(element);
                if (Math.Abs(pos.X - dragStart.Value.X) > 6 || Math.Abs(pos.Y - dragStart.Value.Y) > 6)
                {
                    dragStart = null;
                    var data = new DataObject();
                    var files = new StringCollection { entry.FullName };
                    data.SetFileDropList(files);
                    DragDrop.DoDragDrop(element, data, DragDropEffects.Copy | DragDropEffects.Move);
                }
            }
        };
        element.MouseLeftButtonUp += (_, _) =>
        {
            if (entry is DirectoryInfo dir)
            {
                _currentDirectory = dir.FullName;
                PopulateDirectory();
            }
            else
            {
                try
                {
                    Process.Start(new ProcessStartInfo(entry.FullName) { UseShellExecute = true });
                    Close();
                }
                catch { }
            }
        };
    }
}
