using System.Diagnostics;
using System.Text.Json;

namespace DisplayConfTray;

public class DisplayConfTrayContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly RegistryUtility _registry = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.CONFIG_FILE_NAME);
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly Dictionary<string, List<DisplayProfile>> _hotkeyToProfileList = new();
    private List<DisplayProfile> _profiles = new();

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

        _menu.Opening += (_, _) => InitializeMenuItems(_profiles);

        if (isSilent)
        {
            _ = InitializeAsync(delay);
        }
        else
        {
            Initialize();
            ShowBalloonTip("DisplayConfTray is running in the background.");
        }
    }

    private void SetupHotkeys(List<DisplayProfile> profiles)
    {
        _hotkeyWindow.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

        _hotkeyWindow.Clear();
        _hotkeyToProfileList.Clear();

        foreach (var profile in profiles)
        {
            var hotkey = profile.Hotkey;
            if (string.IsNullOrWhiteSpace(hotkey)) continue;

            var prettyHotkey = HotkeyWindow.ToPrettyString(hotkey);
            _hotkeyWindow.Register(prettyHotkey);

            if (!_hotkeyToProfileList.TryGetValue(prettyHotkey, out var value))
            {
                value = [];
                _hotkeyToProfileList[prettyHotkey] = value;
            }

            value.Add(profile);
        }
    }

    private void OnHotkeyPressed(string hotkey)
    {
        var prettyHotkey = HotkeyWindow.ToPrettyString(hotkey);
        if (!_hotkeyToProfileList.TryGetValue(prettyHotkey, out var profiles) || profiles.Count == 0)
            return;

        var currentProfiles = DisplayManager.GetCurrentProfiles();
        if (profiles.Count > 1)
        {
            var nextIndex = 0;
            foreach (var profile in currentProfiles)
            {
                var activeProfileIndex = profiles.FindIndex(p => profile.Matches(p));
                if (activeProfileIndex > -1)
                {
                    nextIndex = (activeProfileIndex + 1) % profiles.Count;
                    break;
                }
            }

            var nextProfile = profiles[nextIndex];
            DisplayManager.Apply(nextProfile);
        }
        else
        {
            var nextProfile = profiles[0];
            var isAlreadyActive = currentProfiles.Any(p => p.Matches(nextProfile));
            if (isAlreadyActive)
            {
                ShowBalloonTip($"{nextProfile.Name} is already active.");
                return;
            }

            DisplayManager.Apply(nextProfile);
        }
    }

    private async Task InitializeAsync(int delay)
    {
        await Task.Delay(delay * 1000);

        Initialize();
    }

    private void Initialize()
    {
        _profiles = LoadProfilesFromFile() ?? [];
        InitializeMenuItems(_profiles);
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
                if (profiles == null) return null;

                if (profiles.DistinctBy(p => p.Name).Count() != profiles.Count)
                {
                    throw new Exception("Duplicate profile names found!");
                }

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
        _hotkeyWindow.Clear();

        if (profiles != null && profiles.Count > 0)
        {
            SetupHotkeys(profiles);

            var currentProfiles = DisplayManager.GetCurrentProfiles();
            var activeIcon = GraphicsExtensions.CreateCircleImage(16, _menu.ForeColor);

            foreach (var profile in profiles)
            {
                var isActive = currentProfiles.Any(p => profile.Matches(p));
                var item = new ToolStripMenuItem(profile.Name, isActive ? activeIcon : null, (_, _) =>
                {
                    DisplayManager.Apply(profile);
                    InitializeMenuItems(profiles);
                });

                item.ShortcutKeyDisplayString = HotkeyWindow.ToPrettyString(profile.Hotkey);

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
        _menu.Items.Add("Reload Config", null, (_, _) => Initialize());
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
        var currentProfiles = DisplayManager.GetCurrentProfiles();
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(currentProfiles, options);
    }

    private void ShowBalloonTip(string text, int timeout = 3000, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _trayIcon.ShowBalloonTip(timeout, "", text, icon);
    }
}
