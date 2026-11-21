using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using MultiScreenDock.Core;

namespace MultiScreenDock.App;

public enum DockItemKind
{
    FileExplorer,
    Application,
    Directory,
    Separator,
    RecycleBin
}

public sealed record DockItemModel(
    string Id,
    string Label,
    DockItemKind Kind,
    string TargetPath,
    ImageSource? Icon,
    bool IsRunning,
    bool IsFrontmost,
    IntPtr RunningWindowHandle = default
);

public sealed class DockModelReader
{
    public static DockModelReader Shared { get; } = new();

    private readonly Dictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private List<(string Label, string Path)> _pinnedApps = new();
    private DateTime _lastPinnedDirWriteTime = DateTime.MinValue;

    private DockModelReader()
    {
        ReloadPinnedApps(clearIcons: false);
    }

    private static string TaskbarPinnedDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

    public void ReloadPinnedApps(bool clearIcons)
    {
        if (clearIcons) _iconCache.Clear();

        var list = new List<(string Label, string Path)>();
        string dir = TaskbarPinnedDirectory;
        if (Directory.Exists(dir))
        {
            _lastPinnedDirWriteTime = Directory.GetLastWriteTimeUtc(dir);
            foreach (string lnk in Directory.EnumerateFiles(dir, "*.lnk"))
            {
                string label = Path.GetFileNameWithoutExtension(lnk);
                if (label.Contains("File Explorer", StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("Explorador", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                list.Add((label, lnk));
            }
        }

        if (list.Count == 0)
        {
            list = GetDefaultFallbackApps();
        }
        _pinnedApps = list;
    }

    public void CheckPinnedDirChanged()
    {
        string dir = TaskbarPinnedDirectory;
        if (!Directory.Exists(dir)) return;
        DateTime mod = Directory.GetLastWriteTimeUtc(dir);
        if (mod != _lastPinnedDirWriteTime)
        {
            ReloadPinnedApps(clearIcons: false);
        }
    }

    public static List<(string Label, string Path)> GetDefaultFallbackApps()
    {
        var results = new List<(string Label, string Path)>();
        var candidates = new (string Label, string Path)[]
        {
            ("Microsoft Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe")),
            ("Notepad", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "notepad.exe")),
            ("Calculator", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "calc.exe")),
            ("Command Prompt", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe")),
            ("Task Manager", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Taskmgr.exe"))
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c.Path))
            {
                results.Add(c);
            }
        }
        return results;
    }

    private ImageSource? GetCachedIcon(string path)
    {
        if (_iconCache.TryGetValue(path, out var cached)) return cached;
        var icon = Win32Interop.ExtractFileIcon(path);
        _iconCache[path] = icon;
        return icon;
    }

    public List<DockItemModel> BuildItems(bool includeRunningUnpinned)
    {
        CheckPinnedDirChanged();
        IntPtr fg = Win32Interop.GetForegroundWindow();
        var runningWindows = EnumerateRunningAppWindows(fg);

        var items = new List<DockItemModel>();
        var claimedPids = new HashSet<uint>();

        // 1. File Explorer (Windows equivalent of Finder)
        string explorerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        items.Add(new DockItemModel(
            Id: "explorer",
            Label: L10n.FileExplorerLabel,
            Kind: DockItemKind.FileExplorer,
            TargetPath: explorerPath,
            Icon: GetCachedIcon(explorerPath),
            IsRunning: true,
            IsFrontmost: false
        ));

        // 2. Pinned Taskbar Applications
        for (int i = 0; i < _pinnedApps.Count; i++)
        {
            var entry = _pinnedApps[i];
            var match = runningWindows.FirstOrDefault(w =>
                w.Title.Contains(entry.Label, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(w.ExePath), entry.Label, StringComparison.OrdinalIgnoreCase));

            if (match.Pid != 0) claimedPids.Add(match.Pid);

            items.Add(new DockItemModel(
                Id: $"pinned-{i}-{entry.Path}",
                Label: entry.Label,
                Kind: DockItemKind.Application,
                TargetPath: entry.Path,
                Icon: GetCachedIcon(entry.Path),
                IsRunning: match.Pid != 0,
                IsFrontmost: match.IsFrontmost,
                RunningWindowHandle: match.Hwnd
            ));
        }

        // 3. Running Unpinned Regular Applications
        if (includeRunningUnpinned)
        {
            var extras = new List<DockItemModel>();
            uint ownPid = (uint)Environment.ProcessId;
            foreach (var win in runningWindows)
            {
                if (win.Pid == ownPid || claimedPids.Contains(win.Pid)) continue;
                if (string.IsNullOrEmpty(win.ExePath)) continue;
                string exeName = Path.GetFileName(win.ExePath);
                if (string.Equals(exeName, "explorer.exe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(exeName, "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(exeName, "TextInputHost.exe", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                claimedPids.Add(win.Pid);
                string label = string.IsNullOrWhiteSpace(win.ProcessName) ? win.Title : win.ProcessName;
                extras.Add(new DockItemModel(
                    Id: $"running-{win.Pid}",
                    Label: label,
                    Kind: DockItemKind.Application,
                    TargetPath: win.ExePath,
                    Icon: GetCachedIcon(win.ExePath),
                    IsRunning: true,
                    IsFrontmost: win.IsFrontmost,
                    RunningWindowHandle: win.Hwnd
                ));
            }

            if (extras.Count > 0)
            {
                items.Add(new DockItemModel("sep-running", "", DockItemKind.Separator, "", null, false, false));
                items.AddRange(extras);
            }
        }

        // 4. Final Separator + Pinned Folders (Downloads & Documents) + Recycle Bin
        items.Add(new DockItemModel("sep-others", "", DockItemKind.Separator, "", null, false, false));

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloadsPath = Path.Combine(userProfile, "Downloads");
        if (Directory.Exists(downloadsPath))
        {
            items.Add(new DockItemModel(
                Id: "folder-downloads",
                Label: Path.GetFileName(downloadsPath),
                Kind: DockItemKind.Directory,
                TargetPath: downloadsPath,
                Icon: GetCachedIcon(downloadsPath),
                IsRunning: false,
                IsFrontmost: false
            ));
        }

        string docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (Directory.Exists(docsPath))
        {
            items.Add(new DockItemModel(
                Id: "folder-documents",
                Label: Path.GetFileName(docsPath),
                Kind: DockItemKind.Directory,
                TargetPath: docsPath,
                Icon: GetCachedIcon(docsPath),
                IsRunning: false,
                IsFrontmost: false
            ));
        }

        bool rbFull = Win32Interop.IsRecycleBinFull();
        string shell32Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        items.Add(new DockItemModel(
            Id: "recycle-bin",
            Label: L10n.RecycleBinLabel,
            Kind: DockItemKind.RecycleBin,
            TargetPath: "shell:RecycleBinFolder",
            Icon: GetCachedIcon(shell32Path),
            IsRunning: rbFull,
            IsFrontmost: false
        ));

        return items;
    }

    private static List<(IntPtr Hwnd, uint Pid, string Title, string ProcessName, string ExePath, bool IsFrontmost)> EnumerateRunningAppWindows(IntPtr foregroundHwnd)
    {
        var list = new List<(IntPtr, uint, string, string, string, bool)>();
        Win32Interop.EnumWindows((hWnd, _) =>
        {
            if (!Win32Interop.IsWindowVisible(hWnd)) return true;
            int exStyle = Win32Interop.GetWindowLong(hWnd, Win32Interop.GWL_EXSTYLE);
            if ((exStyle & Win32Interop.WS_EX_TOOLWINDOW) != 0) return true;

            var sb = new StringBuilder(256);
            Win32Interop.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();
            if (string.IsNullOrEmpty(title) || title == "Program Manager") return true;

            Win32Interop.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == 0) return true;

            try
            {
                using var proc = Process.GetProcessById((int)pid);
                string exe = proc.MainModule?.FileName ?? "";
                string friendly = proc.ProcessName;
                list.Add((hWnd, pid, title, friendly, exe, hWnd == foregroundHwnd));
            }
            catch { }
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
