using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MultiScreenDock.Core;

public enum AppLanguage
{
    System,
    En,
    Es,
    Fr,
    De,
    Pt,
    It,
    Ja,
    ZhHans
}

public static class AppLanguageExtensions
{
    public static string NativeDisplayName(this AppLanguage lang) =>
        lang switch
        {
            AppLanguage.En => "English",
            AppLanguage.Es => "Español",
            AppLanguage.Fr => "Français",
            AppLanguage.De => "Deutsch",
            AppLanguage.Pt => "Português",
            AppLanguage.It => "Italiano",
            AppLanguage.Ja => "日本語",
            AppLanguage.ZhHans => "简体中文",
            _ => ""
        };
}

public sealed class DockSettingsData
{
    public DockVisibilityMode VisibilityMode { get; set; } = DockVisibilityMode.SmartAutoHide;
    public AutoHideResponsiveness Responsiveness { get; set; } = AutoHideResponsiveness.Balanced;
    public bool ShowHiddenHintPill { get; set; } = true;
    public bool SkipPrimaryDisplay { get; set; } = false;
    public double IconSize { get; set; } = 40.0;
    public bool ShowOverFullScreen { get; set; } = false;
    public bool HoverMagnification { get; set; } = true;
    public bool ShowRunningUnpinned { get; set; } = true;
    public double EdgeMargin { get; set; } = 8.0;
    public HashSet<string> DisabledScreens { get; set; } = new();
    public AppLanguage AppLanguage { get; set; } = AppLanguage.System;
    public FolderStackStylePreference FolderStackStyle { get; set; } = FolderStackStylePreference.Grid;
    public DockAppearanceMode AppearanceMode { get; set; } = DockAppearanceMode.System;
    public DockScreenEdge DockPosition { get; set; } = DockScreenEdge.Bottom;
}

public sealed class DockSettings
{
    public static DockSettings Shared { get; } = new();

    private readonly string _settingsFilePath;
    private DockSettingsData _data = new();

    public event Action? OnChange;

    public DockSettings(string? customSettingsFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(customSettingsFilePath))
        {
            _settingsFilePath = customSettingsFilePath!;
        }
        else
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "MultiScreenDock");
            Directory.CreateDirectory(dir);
            _settingsFilePath = Path.Combine(dir, "settings.json");
        }
        Load();
    }

    public DockVisibilityMode VisibilityMode
    {
        get => _data.VisibilityMode;
        set { _data.VisibilityMode = value; SaveAndNotify(); }
    }

    public AutoHideResponsiveness Responsiveness
    {
        get => _data.Responsiveness;
        set { _data.Responsiveness = value; SaveAndNotify(); }
    }

    public bool ShowHiddenHintPill
    {
        get => _data.ShowHiddenHintPill;
        set { _data.ShowHiddenHintPill = value; SaveAndNotify(); }
    }

    public bool SkipPrimaryDisplay
    {
        get => _data.SkipPrimaryDisplay;
        set { _data.SkipPrimaryDisplay = value; SaveAndNotify(); }
    }

    public double IconSize
    {
        get => Math.Clamp(_data.IconSize, 20.0, 80.0);
        set { _data.IconSize = Math.Clamp(value, 20.0, 80.0); SaveAndNotify(); }
    }

    public bool ShowOverFullScreen
    {
        get => _data.ShowOverFullScreen;
        set { _data.ShowOverFullScreen = value; SaveAndNotify(); }
    }

    public bool HoverMagnification
    {
        get => _data.HoverMagnification;
        set { _data.HoverMagnification = value; SaveAndNotify(); }
    }

    public bool ShowRunningUnpinned
    {
        get => _data.ShowRunningUnpinned;
        set { _data.ShowRunningUnpinned = value; SaveAndNotify(); }
    }

    public double EdgeMargin
    {
        get => Math.Clamp(_data.EdgeMargin, 0.0, 40.0);
        set { _data.EdgeMargin = Math.Clamp(value, 0.0, 40.0); SaveAndNotify(); }
    }

    public HashSet<string> DisabledScreens
    {
        get => new(_data.DisabledScreens);
        set { _data.DisabledScreens = new(value); SaveAndNotify(); }
    }

    public AppLanguage AppLanguage
    {
        get => _data.AppLanguage;
        set { _data.AppLanguage = value; SaveAndNotify(); }
    }

    public FolderStackStylePreference FolderStackStyle
    {
        get => _data.FolderStackStyle;
        set { _data.FolderStackStyle = value; SaveAndNotify(); }
    }

    public DockAppearanceMode AppearanceMode
    {
        get => _data.AppearanceMode;
        set { _data.AppearanceMode = value; SaveAndNotify(); }
    }

    public DockScreenEdge DockPosition
    {
        get => _data.DockPosition;
        set { _data.DockPosition = value; SaveAndNotify(); }
    }

    public bool IsEffectiveDarkAppearance(bool systemIsDark)
    {
        return AppearanceMode switch
        {
            DockAppearanceMode.Dark => true,
            DockAppearanceMode.Light => false,
            _ => systemIsDark
        };
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var parsed = JsonSerializer.Deserialize<DockSettingsData>(json);
                if (parsed != null)
                {
                    _data = parsed;
                    _data.IconSize = Math.Clamp(_data.IconSize, 20.0, 80.0);
                    _data.EdgeMargin = Math.Clamp(_data.EdgeMargin, 0.0, 40.0);
                }
            }
        }
        catch
        {
            _data = new DockSettingsData();
        }
    }

    private void SaveAndNotify()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore write errors in restricted test sandboxes
        }
        OnChange?.Invoke();
    }
}
