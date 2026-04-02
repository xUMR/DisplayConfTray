using System.Text.Json.Serialization;

namespace DisplayConfTray;

public class DisplayProfile
{
    public string Name { get; set; } = "Default";

    public string IconText
    {
        get
        {
            if (!string.IsNullOrEmpty(field)) return field[..1];
            if (string.IsNullOrWhiteSpace(Name)) return "";
            return Name[..1];
        }
        // ReSharper disable once UnusedMember.Global
        set => field = value[..1];
    } = "";

    public string MonitorId { get; set; } = "";

    [JsonIgnore] public int Width { get; set; } = 1920;
    [JsonIgnore] public int Height { get; set; } = 1080;

    public string Resolution
    {
        get => $"{Width}x{Height}";
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var span = value.AsSpan();
            if (span.CountAny('x', 'X') != 1) return;

            var x = span.IndexOfAny('x', 'X');
            if (int.TryParse(span[..x], out var width) &&
                int.TryParse(span[(x + 1)..], out var height))
            {
                Width = width;
                Height = height;
            }
            else
            {
                throw new ArgumentException($"Invalid resolution: {value}");
            }
        }
    }

    public int RefreshRate { get; set; } = Constants.DEFAULT_REFRESH_RATE;
    public int Scale { get; set; } = Constants.DEFAULT_SCALE;

    public string Hotkey { get; set; } = "";

    public bool Matches(DisplayProfile other)
    {
        return MonitorId == other.MonitorId &&
               Width == other.Width &&
               Height == other.Height &&
               RefreshRate == other.RefreshRate &&
               Scale == other.Scale;
    }
}
