using Microsoft.Win32;

namespace DisplayConfTray;

public class RegistryUtility
{
    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public bool RunOnStartup
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
            return key?.GetValue(Constants.APP_NAME) != null;
        }
        set
        {
            // Quote the path for safety
            var appPath = $"\"{Application.ExecutablePath}\" --silent --delay={Constants.DEFAULT_DELAY}";

            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            if (key == null) return;

            if (value)
                key.SetValue(Constants.APP_NAME, appPath);
            else
                key.DeleteValue(Constants.APP_NAME, false);
        }
    }

    public bool IsDarkMode
    {
        get
        {
            const string registryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            // If the value is 0, it's Dark Mode. If it's 1 (or missing), it's Light Mode.
            object? registryValue = Registry.GetValue(registryKey, valueName, null);

            if (registryValue is int i)
            {
                return i == 0;
            }

            return false; // Default to light if we can't find it
        }
    }
}
