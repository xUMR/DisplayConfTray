namespace DisplayConfTray;

public static class AppDialog
{
    public static void Show(
        string text,
        string? caption = null,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.Information)
    {
        var appName = Application.ProductName ?? Constants.APP_NAME;
        caption = string.IsNullOrWhiteSpace(caption)
            ? appName
            : $"{caption} - {appName}";

        if (RegistryUtility.IsDarkMode)
            DarkMessageBox.Show(text, caption, buttons, icon);
        else
            MessageBox.Show(text, caption, buttons, icon);
    }
}
