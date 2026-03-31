namespace DisplayConfTray;

public class DisplayProfile
{
    public string Name { get; set; } = "Default";
    public string MonitorId { get; set; } = "";
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int RefreshRate { get; set; } = Constants.DEFAULT_REFRESH_RATE;
    public int Scale { get; set; } = Constants.DEFAULT_SCALE;

    public bool Matches(DisplayProfile other)
    {
        return MonitorId == other.MonitorId &&
               Width == other.Width &&
               Height == other.Height &&
               RefreshRate == other.RefreshRate &&
               Scale == other.Scale;
    }
}
