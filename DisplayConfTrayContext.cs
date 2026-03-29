using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace DisplayConfTray;

public class DisplayConfTrayContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly RegistryUtility _registry = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.CONFIG_FILE_NAME);

    public DisplayConfTrayContext(bool isSilent, int delay)
    {
        if (_registry.IsDarkMode)
        {
            _menu.Renderer = new DarkModeRenderer();
            _menu.ForeColor = Color.White;
            _menu.BackColor = Color.FromArgb(30, 30, 30);

            _menu.ShowImageMargin = true;
            _menu.ShowCheckMargin = false;

            _menu.Padding = new Padding(2);
        }

        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            ContextMenuStrip = _menu,
            Text = Constants.APP_NAME,
            Visible = true
        };

        LoadProfiles();

        if (isSilent)
        {
            _ = LoadProfilesAsync(delay);
        }
        else
        {
            ShowBalloonTip("DisplayConfTray is running in the background.");
        }
    }

    private async Task LoadProfilesAsync(int delay)
    {
        await Task.Delay(delay * 1000);

        LoadProfiles();
    }

    private void LoadProfiles()
    {
        _menu.Items.Clear();

        var profileConfigExists = File.Exists(_configPath);
        if (profileConfigExists)
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var profiles = JsonSerializer.Deserialize<List<DisplayProfile>>(json);

                if (profiles != null)
                {
                    foreach (var p in profiles)
                    {
                        _menu.Items.Add(new ToolStripMenuItem(p.Name, null, (s, e) => DisplayManager.Apply(p)));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profiles: {ex.Message}");
            }
        }
        else
        {
            _menu.Items.Add(new ToolStripMenuItem($"{Constants.CONFIG_FILE_NAME} not found!") { Enabled = false });
            _menu.Items.Add("Create", null, (_, _) => CreateProfileConfig());
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Open Directory", null, (_, _) => OpenDirectory());
        _menu.Items.Add("Copy Default Config", null, (_, _) => CopyDefaultConfig());
        _menu.Items.Add("Reload", null, (_, _) => LoadProfiles());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateRunOnStartupMenuItem());
        _menu.Items.Add("Exit", null, (_, _) => { _trayIcon.Visible = false; Application.Exit(); });
    }

    private ToolStripMenuItem CreateRunOnStartupMenuItem()
    {
        var menuItem = new ToolStripMenuItem("Run at Startup")
        {
            CheckOnClick = true,
            Checked = _registry.RunOnStartup
        };

        menuItem.Click += (_, _) => _registry.RunOnStartup = menuItem.Checked;

        return menuItem;
    }

    private void CreateProfileConfig()
    {
        var json = GetDisplayConfig();
        File.WriteAllText(_configPath, json);

        LoadProfiles();
    }

    private void OpenDirectory()
    {
        var targetPath = File.Exists(_configPath)
            ? $"/select,\"{_configPath}\""
            : Application.ExecutablePath;

        Process.Start("explorer.exe", targetPath);
    }

    private void CopyDefaultConfig()
    {
        var json = GetDisplayConfig();

        Clipboard.SetText(json);
        ShowBalloonTip("Config copied to clipboard!");
    }

    private string GetDisplayConfig()
    {
        var currentProfiles = new List<DisplayProfile>();
        var monitors = DisplayManager.GetMonitors();

        // Screen.AllScreens detects every active monitor connected to the GPU
        foreach (var screen in Screen.AllScreens)
        {
            DisplayManager.DEVMODE dm = new() { dmSize = (short)Marshal.SizeOf<DisplayManager.DEVMODE>() };

            // We pass screen.DeviceName (e.g., "\\.\DISPLAY2") to get its specific settings
            if (DisplayManager.EnumDisplaySettings(screen.DeviceName, DisplayManager.ENUM_CURRENT_SETTINGS, ref dm))
            {
                // We also need to grab the DPI/Scale for this specific monitor
                int currentScale = GetCurrentScaleForScreen(screen);

                var monitor = monitors.FirstOrDefault(m => m.DeviceName == screen.DeviceName);
                if (monitor == default)
                {
                    Console.WriteLine($"Monitor not found: {screen.DeviceName}");
                    continue;
                }

                currentProfiles.Add(new DisplayProfile
                {
                    Name = $"Profile_{screen.DeviceName.Replace(".", "").Replace("\\", "")}",
                    MonitorId = monitor.MonitorId,
                    Width = dm.dmPelsWidth,
                    Height = dm.dmPelsHeight,
                    RefreshRate = dm.dmDisplayFrequency,
                    Scale = currentScale
                });
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(currentProfiles, options);
        return json;
    }

    private int GetCurrentScaleForScreen(Screen screen)
    {
        try
        {
            // Get the monitor handle from a point within the screen's bounds
            IntPtr hMonitor = MonitorFromPoint(screen.Bounds.Location, 2); // 2 = MONITOR_DEFAULTTONEAREST

            // 0 = MDT_EFFECTIVE_DPI (The scale the user actually sees)
            GetDpiForMonitor(hMonitor, 0, out uint dpiX, out uint _);

            // 96 DPI is the baseline for 100%
            return (int)((dpiX / 96.0) * 100);
        }
        catch
        {
            return 100; // Fallback if API fails
        }
    }

    private void ShowBalloonTip(string text, int timeout = 3000, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _trayIcon.ShowBalloonTip(timeout, "", text, icon);
    }

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);
}
