using Microsoft.Win32;

namespace DisplayConfTray;

public static class RegistryUtility
{
    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool RunOnStartup
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

    public static readonly bool IsDarkMode =
        Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", null) is 0;
}
