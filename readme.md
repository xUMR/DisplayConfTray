# DisplayConfTray

A system tray utility for switching between display profiles:
- Resolution
- Refresh rate
- Scale

---
## Requirements

.NET 10 is required. Use either method:
- [Download .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Winget: `winget install -e --id Microsoft.DotNet.DesktopRuntime.10`

---

Example config (`config.json`):
```json
[
  {
    "Name": "Default (4K)",
    "MonitorId": "MONITOR\\XXXXXXX",
    "Width": 3840,
    "Height": 2160,
    "RefreshRate": 120,
    "Scale": 150,
    "Hotkey": "Ctrl+Shift+1"
  },
  {
    "Name": "Game (2K)",
    "MonitorId": "MONITOR\\XXXXXXX",
    "Width": 2560,
    "Height": 1440,
    "RefreshRate": 240,
    "Scale": 100,
    "Hotkey": "Ctrl+Shift+2"
  }
]
```
- You can use the same hotkey to cycle between profiles.  

---

The icon is from [Lucide](https://lucide.dev/icons/monitor-cog).
