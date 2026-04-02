using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace DisplayConfTray;

public class DisplayConfTrayContext : ApplicationContext
{
    private static readonly MethodInfo? ShowContextMenuMethod =
        typeof(NotifyIcon).GetMethod("ShowContextMenu",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly NotifyIcon _trayIcon;
    private readonly RegistryUtility _registry = new();
    private readonly ContextMenuStrip _menu = new();
    private readonly string _configPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.CONFIG_FILE_NAME);

    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly Dictionary<string, List<DisplayProfile>> _hotkeyToProfileList = new();
    private List<DisplayProfile> _profiles = new();
    private readonly Icon _baseIcon;
    private Icon? _tempIcon;

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

        _baseIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!;
        _trayIcon = new NotifyIcon
        {
            Icon = _baseIcon,
            ContextMenuStrip = _menu,
            Text = Application.ProductName,
            Visible = true
        };

        _menu.Opening += (_, _) => InitializeMenuItems(_profiles);
        _trayIcon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowContextMenuMethod?.Invoke(_trayIcon, null);
        };

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

        HashSet<string>? failedHotkeys = null;
        foreach (var profile in profiles)
        {
            var hotkey = profile.Hotkey;
            if (string.IsNullOrWhiteSpace(hotkey)) continue;

            var prettyHotkey = HotkeyWindow.ToPrettyString(hotkey);
            if (!_hotkeyWindow.Register(prettyHotkey))
            {
                failedHotkeys ??= [];
                failedHotkeys.Add(prettyHotkey);
            }

            if (!_hotkeyToProfileList.TryGetValue(prettyHotkey, out var value))
            {
                value = [];
                _hotkeyToProfileList[prettyHotkey] = value;
            }

            value.Add(profile);
        }

        if (failedHotkeys == null) return;

        foreach (var hotkey in failedHotkeys)
        {
            MessageBox.Show($"Failed to register hotkey: {hotkey}");
        }
    }

    private void OnHotkeyPressed(string hotkey)
    {
        var prettyHotkey = HotkeyWindow.ToPrettyString(hotkey);
        if (!_hotkeyToProfileList.TryGetValue(prettyHotkey, out var profiles) || profiles.Count == 0)
            return;

        DisplayProfile nextProfile;
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

            nextProfile = profiles[nextIndex];
        }
        else
        {
            nextProfile = profiles[0];
            var isAlreadyActive = currentProfiles.Any(p => p.Matches(nextProfile));
            if (isAlreadyActive)
            {
                ShowBalloonTip($"{nextProfile.Name} is already active.");
                return;
            }
        }

        ApplyProfileThenUpdateMenuItems(nextProfile, _profiles);
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
                MessageBox.Show($"Error loading profiles: {ex.Message}", Application.ProductName);
            }
        }

        return null;
    }

    private void InitializeMenuItems(List<DisplayProfile>? profiles)
    {
        _menu.Items.Clear();
        _hotkeyWindow.Clear();

        var profileConfigMenuItems = CreateProfileConfigMenuItems(profiles);
        foreach (var menuItem in profileConfigMenuItems) { _menu.Items.Add(menuItem); }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Open Directory", null, (_, _) => OpenDirectory());
        _menu.Items.Add("Copy Default Config", null, (_, _) => CopyDefaultConfig());
        _menu.Items.Add("Reload Config", null, (_, _) => Initialize());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateRunOnStartupMenuItem());
        _menu.Items.Add("About", null, (_, _) => ShowAboutMessage());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => { Dispose(); Application.Exit(); });

        UpdateTrayIcon(profileConfigMenuItems);
    }

    private List<ToolStripMenuItem> CreateProfileConfigMenuItems(List<DisplayProfile>? profiles)
    {
        var menuItems = new List<ToolStripMenuItem>();
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
                    ApplyProfileThenUpdateMenuItems(profile, profiles);
                });

                item.ShortcutKeyDisplayString = HotkeyWindow.ToPrettyString(profile.Hotkey);

                menuItems.Add(item);
            }
        }
        else
        {
            menuItems.Add(new ToolStripMenuItem($"Failed to load {Constants.CONFIG_FILE_NAME}!") { Enabled = false });
            menuItems.Add(new ToolStripMenuItem("Create Config", null, (_, _) => CreateProfileConfig()));
        }

        return menuItems;
    }

    private void ApplyProfileThenUpdateMenuItems(DisplayProfile profile, List<DisplayProfile>? profiles)
    {
        DisplayManager.Apply(profile);
        InitializeMenuItems(profiles);
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

    private void UpdateTrayIcon(List<ToolStripMenuItem> menuItems)
    {
        var activeCount = 0;
        var activeItemIndex = -1;
        for (var i = 0; i < menuItems.Count; i++)
        {
            var menuItem = menuItems[i];
            if (menuItem.Image != null)
            {
                activeCount++;
                activeItemIndex = i;
            }
        }

        string iconText;
        string tooltipText;
        switch (activeCount)
        {
            case 0:
                iconText = "";
                tooltipText = Application.ProductName ?? Constants.APP_NAME;
                break;
            case 1:
                iconText = _profiles[activeItemIndex].IconText;
                tooltipText = $"{Application.ProductName}: {menuItems[activeItemIndex].Text}";
                break;
            default:
                iconText = "*";
                tooltipText = $"{Application.ProductName} (*)";
                break;
        }

        UpdateTrayIconText(iconText);
        _trayIcon.Text = tooltipText;
    }

    private void UpdateTrayIconText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _trayIcon.Icon = _baseIcon;
            return;
        }

        using var baseBitmap = new Bitmap(_baseIcon.ToBitmap());
        using var graphics = Graphics.FromImage(baseBitmap);

        using var brush = new SolidBrush(_menu.ForeColor);
        using var font = new Font("Consolas", 18, FontStyle.Bold);

        graphics.DrawString(text, font, brush, new PointF(6, -1));

        var oldIcon = _tempIcon;

        var hIcon = baseBitmap.GetHicon();
        _tempIcon = Icon.FromHandle(hIcon);

        _trayIcon.Icon = _tempIcon;

        // destroy the old GDI handle to prevent memory leaks
        if (oldIcon != null)
        {
            DestroyIcon(oldIcon.Handle);
            oldIcon.Dispose();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    private void ShowBalloonTip(string text, int timeout = 3000, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _trayIcon.ShowBalloonTip(timeout, "", text, icon);
    }

    private void ShowAboutMessage()
    {
        MessageBox.Show(
            $"{Application.ProductName} v{Application.ProductVersion}\n" + Constants.ABOUT_TEXT,
            "About",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    protected override void Dispose(bool disposing)
    {
        if (_tempIcon != null)
        {
            DestroyIcon(_tempIcon.Handle);
            _tempIcon.Dispose();
        }

        _trayIcon.Dispose();

        base.Dispose(disposing);
    }
}
