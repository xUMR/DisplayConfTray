namespace DisplayConfTray;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var isSilent = args.Contains("--silent");
        var delay = GetDelayParam(args, Constants.DEFAULT_DELAY);
        Application.Run(new DisplayConfTrayContext(isSilent, delay));
    }

    private static int GetDelayParam(string[] args, int defaultDelay)
    {
        const string prefix = "--delay=";

        var delayParam = args.FirstOrDefault(a => a.StartsWith(prefix));
        return delayParam != null ? int.Parse(delayParam.AsSpan(prefix.Length)) : defaultDelay;
    }
}
