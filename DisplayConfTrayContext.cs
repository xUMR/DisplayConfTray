using System.Diagnostics;
using System.Text.Json;

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

        Initialize();

        if (isSilent)
        {
            _ = InitializeAsync(delay);
        }
        else
        {
            ShowBalloonTip("DisplayConfTray is running in the background.");
        }
    }

    private async Task InitializeAsync(int delay)
    {
        await Task.Delay(delay * 1000);

        Initialize();
    }

    private void Initialize()
    {
        var profiles = LoadProfilesFromFile();
        InitializeMenuItems(profiles);
    }

    private List<DisplayProfile>? LoadProfilesFromFile()
    {
        var profileConfigExists = File.Exists(_configPath);
        if (profileConfigExists)
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                var profiles = JsonSerializer.Deserialize<List<DisplayProfile>>(json);

                return profiles;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profiles: {ex.Message}");
            }
        }

        return null;
    }

    private void InitializeMenuItems(List<DisplayProfile>? profiles)
    {
        _menu.Items.Clear();

        if (profiles != null && profiles.Count > 0)
        {
            var currentProfiles = GetCurrentProfiles();
            var activeIcon = GraphicsExtensions.CreateCircleImage(16, _menu.ForeColor);

            foreach (var profile in profiles)
            {
                var isActive = currentProfiles.Any(p => profile.Matches(p));
                var item = new ToolStripMenuItem(profile.Name, isActive ? activeIcon : null, (_, _) =>
                {
                    DisplayManager.Apply(profile);
                    InitializeMenuItems(profiles);
                });
                _menu.Items.Add(item);
            }
        }
        else
        {
            _menu.Items.Add(new ToolStripMenuItem($"Failed to load {Constants.CONFIG_FILE_NAME}!") { Enabled = false });
            _menu.Items.Add("Create", null, (_, _) => CreateProfileConfig());
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Open Directory", null, (_, _) => OpenDirectory());
        _menu.Items.Add("Copy Default Config", null, (_, _) => CopyDefaultConfig());
        _menu.Items.Add("Reload", null, (_, _) => Initialize());
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
        var json = GetDisplayConfigJson();
        File.WriteAllText(_configPath, json);

        Initialize();
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
        var json = GetDisplayConfigJson();

        Clipboard.SetText(json);
        ShowBalloonTip("Config copied to clipboard!");
    }

    private string GetDisplayConfigJson()
    {
        var currentProfiles = GetCurrentProfiles();
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(currentProfiles, options);
    }

    private List<DisplayProfile> GetCurrentProfiles()
    {
        var currentProfiles = new List<DisplayProfile>();
        var monitors = DisplayManager.GetMonitors();
        for (var i = 0; i < monitors.Count; i++)
        {
            var (_, monitorId, _) = monitors[i];
            if (DisplayManager.TryGetDisplayProfile(monitorId, out var profile))
            {
                profile.Name = $"Profile {i + 1}";
                currentProfiles.Add(profile);
            }
        }

        return currentProfiles;
    }

    private void ShowBalloonTip(string text, int timeout = 3000, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _trayIcon.ShowBalloonTip(timeout, "", text, icon);
    }
}
