namespace DisplayConfTray;

public class DisplayProfile
{
    public string Name { get; set; } = "Default";
    public string DeviceName { get; set; } = @"\\.\DISPLAY1";
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int RefreshRate { get; set; } = 60;
    public int Scale { get; set; } = 100;
}
