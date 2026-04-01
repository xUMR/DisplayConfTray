using System.Runtime.InteropServices;
using System.Text;

namespace DisplayConfTray;

public sealed class HotkeyWindow : NativeWindow, IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;

    public event Action<string> HotkeyPressed = _ => { };

    private readonly Dictionary<int, string> _hotkeys = new();

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            var id = m.WParam.ToInt32();
            if (_hotkeys.TryGetValue(id, out var hotkey))
            {
                HotkeyPressed.Invoke(hotkey);
            }
        }

        base.WndProc(ref m);
    }

    public bool Register(string hotkey)
    {
        var (mods, vk) = Parse(hotkey);
        if (mods == 0 && vk == 0) return false;

        var id = HashCode.Combine(mods, vk);

        if (_hotkeys.ContainsKey(id)) return true;

        if (RegisterHotKey(Handle, id, MOD_NOREPEAT | mods, vk))
        {
            _hotkeys[id] = hotkey;
            return true;
        }

        Console.WriteLine($"Failed to register hotkey: {hotkey}");
        return false;
    }

    /// <summary>
    /// The given input will be parsed, then a (modifiers, virtual key) pair will be returned.
    /// If the operation fails, (0, 0) will be returned.
    /// </summary>
    /// <param name="hotkey"></param>
    /// <returns></returns>
    public (uint mods, uint vk) Parse(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return (0, 0);

        var keyCombo = ParseKeys(hotkey);
        return GetHotkey(keyCombo);
    }

    private static Keys ParseKeys(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return Keys.None;

        var span = hotkey.AsSpan();
        var ranges = span.Split('+');

        var keyCombo = Keys.None;
        var expectModifier = true;
        foreach (var range in ranges)
        {
            var key = ParseKey(span[range.Start..range.End]);
            if (!IsModifier(key))
            {
                if (!expectModifier)
                {
                    return Keys.None;
                }

                expectModifier = false;
            }
            keyCombo |= key;
        }

        return keyCombo;
    }

    private static (uint mods, uint vk) GetHotkey(Keys keyCombo)
    {
        var vk = (uint)(keyCombo & Keys.KeyCode);
        var mods = 0u;

        if (keyCombo.HasFlag(Keys.Control)) mods |= MOD_CONTROL;
        if (keyCombo.HasFlag(Keys.Shift)) mods |= MOD_SHIFT;
        if (keyCombo.HasFlag(Keys.Alt)) mods |= MOD_ALT;

        return (mods, vk);
    }

    private static Keys ParseKey(ReadOnlySpan<char> key)
    {
        Span<char> keyLower = stackalloc char[key.Length];
        key.ToLowerInvariant(keyLower);

        var result = keyLower switch
        {
            // modifiers
            "ctrl" => Keys.Control,
            "shift" => Keys.Shift,
            "alt" => Keys.Alt,

            // keys
            "plus" => Keys.Oemplus,
            "minus" => Keys.OemMinus,
            "del" => Keys.Delete,
            "backspace" => Keys.Back,

            // use digits for number keys
            _ when keyLower.Length == 1 && char.IsAsciiDigit(keyLower[0]) => Keys.D0 + (keyLower[0] - '0'),

            // try to parse other keys
            _ => Keys.None
        };

        if (result != Keys.None) return result;

        return Enum.TryParse(keyLower, true, out result)
            ? result
            : Keys.None;
    }

    private static bool IsModifier(Keys key) => (key & Keys.Modifiers) != 0;

    public void Dispose()
    {
        Clear();
    }

    public void Clear()
    {
        foreach (var (id, _) in _hotkeys)
        {
            UnregisterHotKey(Handle, id);
        }

        _hotkeys.Clear();
    }

    public static string ToPrettyString(string hotkey)
    {
        return ToPrettyString(ParseKeys(hotkey));
    }

    public static string ToPrettyString(Keys keyCombo)
    {
        if (keyCombo == Keys.None) return "";

        var key = keyCombo & Keys.KeyCode;
        if (key == Keys.None) return "";

        var sb = new StringBuilder();

        if (keyCombo.HasFlag(Keys.Control)) sb.Append("Ctrl+");
        if (keyCombo.HasFlag(Keys.Shift)) sb.Append("Shift+");
        if (keyCombo.HasFlag(Keys.Alt)) sb.Append("Alt+");

        sb.Append(key.ToString());
        return sb.ToString();
    }
}
