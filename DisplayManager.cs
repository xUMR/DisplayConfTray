using System.Runtime.InteropServices;

namespace DisplayConfTray;

internal static class DisplayManager
{
    #region Structs

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
    {
        public ulong pixelRate;
        public DISPLAYCONFIG_RATIONAL hSyncFreq;
        public DISPLAYCONFIG_RATIONAL vSyncFreq; // This is your Refresh Rate
        public DISPLAYCONFIG_2DREGION activeSize;
        public DISPLAYCONFIG_2DREGION totalSize;
        public uint videoStandard;
        public int scanLineOrdering;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_2DREGION
    {
        public uint cx;
        public uint cy;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        public uint outputTechnology;
        public uint rotation;
        public uint scaling;
        public DISPLAYCONFIG_RATIONAL refreshRate;
        public uint scanLineOrdering;
        public bool targetAvailable;
        public uint statusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
        public uint flags;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct DISPLAYCONFIG_MODE_INFO
    {
        [FieldOffset(0)] public uint infoType;
        [FieldOffset(4)] public uint id;
        [FieldOffset(8)] public LUID adapterId;
        [FieldOffset(16)] public DISPLAYCONFIG_TARGET_MODE targetMode;
        [FieldOffset(16)] public DISPLAYCONFIG_SOURCE_MODE sourceMode;
        [FieldOffset(16)] public DISPLAYCONFIG_DESKTOP_IMAGE_INFO desktopImageInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_TARGET_MODE
    {
        public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    // placeholder
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SOURCE_MODE
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] dummy;
    }

    // placeholder
    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] dummy;
    }

    #endregion

    #region Extern Methods

    [DllImport("user32.dll", EntryPoint = "EnumDisplayDevicesW", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport("user32.dll", EntryPoint = "EnumDisplaySettingsW", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplaySettings(
        [MarshalAs(UnmanagedType.LPWStr)] string? lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettingsExW", CharSet = CharSet.Unicode)]
    public static extern int ChangeDisplaySettingsEx(
        [MarshalAs(UnmanagedType.LPWStr)] string? lpszDeviceName,
        ref DEVMODE lpDevMode,
        IntPtr hwnd,
        uint dwflags,
        IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern int GetDisplayConfigBufferSizes(
        uint flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport("user32.dll")]
    public static extern int QueryDisplayConfig(
        uint flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr topologyId);

    #endregion

    #region Flags

    public const int ENUM_CURRENT_SETTINGS = -1;
    public const uint CDS_UPDATEREGISTRY = 0x01;
    public const int DM_PELSWIDTH = 0x80000;
    public const int DM_PELSHEIGHT = 0x100000;
    public const int DM_DISPLAYFREQUENCY = 0x400000;

    // Flags for QueryDisplayConfig
    public const uint QDC_ALL_PATHS = 0x00000001;
    public const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;

    #endregion

    private static void LogModes(string deviceName)
    {
        DEVMODE tempMode = new DEVMODE();
        for (int i = 0; EnumDisplaySettings(deviceName, i, ref tempMode); i++)
        {
            Console.WriteLine($"Found mode: {tempMode.dmPelsWidth}x{tempMode.dmPelsHeight} @ {tempMode.dmDisplayFrequency}Hz & {tempMode.dmLogPixels}");
        }
    }

    /// <summary>
    /// Resolves the current \\.\DISPLAYx name for a monitor identified by its hardware ID.
    /// </summary>
    public static string? ResolveDeviceName(string monitorId)
    {
        var adapter = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

        for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
        {
            var monitor = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

            for (uint j = 0; EnumDisplayDevices(adapter.DeviceName, j, ref monitor, 0); j++)
            {
                // monitor.DeviceID looks like:
                // MONITOR\DELA1A1\{guid}
                // We match on the stable prefix (e.g. "MONITOR\DELA1A1")
                if (monitor.DeviceID.Contains(monitorId, StringComparison.OrdinalIgnoreCase))
                {
                    return adapter.DeviceName; // e.g. \\.\DISPLAY1
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Lists all connected monitors with their current DeviceName and stable hardware ID.
    /// Useful for finding the MonitorId to put in profiles.
    /// </summary>
    public static List<(string DeviceName, string MonitorId, string FriendlyName)> GetMonitors()
    {
        var results = new List<(string, string, string)>();
        var adapter = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

        for (uint i = 0; EnumDisplayDevices(null, i, ref adapter, 0); i++)
        {
            var monitor = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

            for (uint j = 0; EnumDisplayDevices(adapter.DeviceName, j, ref monitor, 0); j++)
            {
                // Extract the stable part: "MONITOR\<HardwareId>"
                var parts = monitor.DeviceID.Split('\\');
                var stableId = parts.Length >= 3
                    ? $"{parts[0]}\\{parts[1]}"
                    : monitor.DeviceID;

                results.Add((adapter.DeviceName, stableId, monitor.DeviceString));
            }
        }

        return results;
    }

    public static void Apply(DisplayProfile p)
    {
        var deviceName = ResolveDeviceName(p.MonitorId);
        if (deviceName == null)
        {
            MessageBox.Show($"Monitor '{p.MonitorId}' not found. Is it connected?");
            return;
        }

        DEVMODE dm = new() { dmSize = (short)Marshal.SizeOf<DEVMODE>() };

        if (EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
        {
            dm.dmPelsWidth = p.Width;
            dm.dmPelsHeight = p.Height;
            dm.dmDisplayFrequency = p.RefreshRate;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;

            var result = ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
            // todo scaling

            if (result != 0) // DISP_CHANGE_SUCCESSFUL is 0
            {
                MessageBox.Show($"Failed to change settings. Error code: {result}");
            }
        }
    }
}
