# DisplayConfTray

A system tray utility for switching between display profiles:
- Resolution
- Refresh rate
- Scale (TO DO)

---

Example config (`profiles.json`):
```json
[
  {
    "Name": "Default (4K)",
    "DeviceName": "\\\\.\\DISPLAY1",
    "Width": 3840,
    "Height": 2160,
    "RefreshRate": 120,
    "Scale": 150
  },
  {
    "Name": "Gaming (2K)",
    "DeviceName": "\\\\.\\DISPLAY1",
    "Width": 2560,
    "Height": 1440,
    "RefreshRate": 240,
    "Scale": 100
  }
]
```

---

The icon is from [Lucide](https://lucide.dev/icons/monitor-cog).
