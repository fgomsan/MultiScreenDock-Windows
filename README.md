# MultiScreenDock for Windows (Windows 10 & Windows 11)

[![Microsoft Store](https://img.shields.io/badge/Microsoft_Store-Get_it_now-0078D4?logo=microsoft&logoColor=white)](https://apps.microsoft.com/detail/9N08WLJHKWLR)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Windows 10 & 11](https://img.shields.io/badge/Windows-10%20%26%2011%20(x64%20%2B%20ARM64)-0078D4?logo=windows)](https://github.com/fgomsan/MultiScreenDock-Windows/releases)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-ffdd00?logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/fgomsan)

**Get it on the official Microsoft Store:** [**https://apps.microsoft.com/detail/9N08WLJHKWLR**](https://apps.microsoft.com/detail/9N08WLJHKWLR) (or via [`ms-windows-store://pdp/?productid=9N08WLJHKWLR`](ms-windows-store://pdp/?productid=9N08WLJHKWLR)).

**MultiScreenDock for Windows** brings the simultaneous multi-monitor frosted-glass Dock of [MultiScreenDock for macOS](https://github.com/fgomsan/MultiScreenDock) to **Windows 10 and Windows 11** — restoring **Left, Right, and Bottom Dock positions** (which Windows 11 removed from its native taskbar), **Smart Auto-Hide (IntelliHide)**, **120 Hz raised-cosine magnification with focal lock**, and **interactive Folder Stacks (Fan, Grid, and List)** across 2 to 6+ monitors.

---

## Key Features

- **Simultaneous Dock on Every Monitor:** Places a non-activating (`WS_EX_NOACTIVATE`) frosted-glass Dock on every connected display — including vertically stacked upper and lower monitor arrays.
- **Bottom, Left & Right Edge Positions:** Pin your Dock to the **Bottom**, **Left**, or **Right** edge of every monitor (restoring vertical Docks on Windows 11!) with full vertical 120 Hz cosine wave magnification.
- **3 Intelligent Visibility Modes:**
  1. **Smart Auto-Hide (IntelliHide):** Stays visible when a monitor's Dock zone is clear, and automatically hides per-monitor only when a window overlaps the Dock area.
  2. **Edge Auto-Hide:** Reveals smoothly on edge hover, with an anti-crossing dwell filter (`150 ms` vs `80 ms`) for stacked or adjacent monitors.
  3. **Always Visible:** Keeps the Dock permanently visible on your monitors.
- **Interactive Folder Stacks (Fan, Grid & List):** Click `Downloads` or `Documents` on any monitor to open a **Grid**, **Fan**, or **List** Folder Stack with subfolder navigation and direct file drag-and-drop into any application.
- **Light Glass & Dark Glass Appearance:** Automatically follows the Windows 10/11 Light/Dark theme (`AppsUseLightTheme`) or locks to **Light Glass** or **Dark Glass**.
- **Per-Monitor Fullscreen Auto-Suppression:** Automatically steps out of the way on any display running a fullscreen game, video, or presentation while staying active on your other monitors.
- **8 Built-in Languages:** English, Español, Français, Deutsch, Português, Italiano, 日本語, and 简体中文.

---

## Build & Run

```powershell
dotnet test MultiScreenDock.sln
dotnet publish src/MultiScreenDock.App/MultiScreenDock.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## License

[MIT](LICENSE) — Copyright © 2026 Fernando Gómez Sancha.
