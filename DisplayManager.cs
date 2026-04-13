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

    // placeholder – must be pure value types to allow FieldOffset overlap in DISPLAYCONFIG_MODE_INFO
    // Size must match the largest union member (DISPLAYCONFIG_TARGET_MODE = 48 bytes)
    // so that DISPLAYCONFIG_MODE_INFO total = 16 (header) + 48 (union) = 64 bytes
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    public struct DISPLAYCONFIG_SOURCE_MODE { }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    public struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO { }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public uint type;
        public uint size;
        public LUID adapterId;
        public uint id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string viewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_GET_DPI_SCALE
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int minScaleSteps;
        public int currentScaleSteps;
        public int maxScaleSteps;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DISPLAYCONFIG_SET_DPI_SCALE
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
        public int scaleSteps;
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

    [DllImport("user32.dll")]
    public static extern int SetDisplayConfig(
        uint numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[] pathArray,
        uint numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        uint flags);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_GET_DPI_SCALE getDpiScale);

    [DllImport("user32.dll")]
    public static extern int DisplayConfigSetDeviceInfo(
        ref DISPLAYCONFIG_SET_DPI_SCALE setDpiScale);

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

    // Flags for SetDisplayConfig
    public const uint SDC_USE_SUPPLIED_DISPLAY_CONFIG = 0x00000020;
    public const uint SDC_APPLY = 0x00000080;
    public const uint SDC_SAVE_TO_DATABASE = 0x00000200;
    public const uint SDC_ALLOW_CHANGES = 0x00000400;

    // Display mode info types
    public const uint DISPLAYCONFIG_MODE_INFO_TYPE_TARGET = 0x00000002;

    // DPI scale device info types (undocumented, stable since Windows 10 1803)
    public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 0x00000001;
    public const uint DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE = 0xFFFFFFFD; // -3
    public const uint DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE = 0xFFFFFFFC; // -4

    #endregion

    private static readonly int[] ScalePercentages =
        [100, 125, 150, 175, 200, 225, 250, 300, 350, 400, 450, 500];

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
            AppDialog.Show($"Monitor '{p.MonitorId}' not found. Is it connected?");
            return;
        }

        // Use ChangeDisplaySettingsEx for resolution (it handles mode switching well)
        DEVMODE dm = new() { dmSize = (short)Marshal.SizeOf<DEVMODE>() };

        if (EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm))
        {
            dm.dmPelsWidth = p.Width;
            dm.dmPelsHeight = p.Height;
            dm.dmDisplayFrequency = p.RefreshRate;
            dm.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY;

            var result = ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
            if (result != 0)
            {
                AppDialog.Show($"Failed to change settings. Error code: {result}");
                return;
            }
        }

        // Use SetDisplayConfig for precise refresh rate (numerator/denominator)
        SetRefreshRate(deviceName, p.RefreshRate);
        SetScale(deviceName, p.Scale);
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
                // MONITOR\XXXA1A1\{guid}
                // We match on the stable prefix (e.g. "MONITOR\XXXA1A1")
                if (monitor.DeviceID.Contains(monitorId, StringComparison.OrdinalIgnoreCase))
                {
                    return adapter.DeviceName; // e.g. \\.\DISPLAY1
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Queries the active display config, finds the path matching the given GDI device name,
    /// and returns the paths, modes, and matched path index.
    /// </summary>
    private static (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes, int pathIndex)
        FindDisplayConfigPath(string deviceName)
    {
        GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount);
        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);

        for (int i = 0; i < pathCount; i++)
        {
            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
            sourceName.header.id = paths[i].sourceInfo.id;

            if (DisplayConfigGetDeviceInfo(ref sourceName) != 0) continue;
            if (sourceName.viewGdiDeviceName == deviceName)
                return (paths, modes, i);
        }

        return (paths, modes, -1);
    }

    /// <summary>
    /// Sets the precise refresh rate using SetDisplayConfig with numerator/denominator.
    /// This avoids the rounding issues of ChangeDisplaySettingsEx's integer Hz field.
    /// </summary>
    private static void SetRefreshRate(string deviceName, int refreshRate)
    {
        var (paths, modes, pathIndex) = FindDisplayConfigPath(deviceName);
        if (pathIndex < 0) return;

        // Find the target mode for this path
        var targetModeIdx = paths[pathIndex].targetInfo.modeInfoIdx;
        if (targetModeIdx >= modes.Length) return;

        // Modify the target mode's vSyncFreq to the exact refresh rate
        var mode = modes[targetModeIdx];
        if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
        {
            mode.targetMode.targetVideoSignalInfo.vSyncFreq.Numerator = (uint)refreshRate;
            mode.targetMode.targetVideoSignalInfo.vSyncFreq.Denominator = 1;
            modes[targetModeIdx] = mode;

            var result = SetDisplayConfig(
                (uint)paths.Length, paths,
                (uint)modes.Length, modes,
                SDC_APPLY | SDC_USE_SUPPLIED_DISPLAY_CONFIG | SDC_SAVE_TO_DATABASE | SDC_ALLOW_CHANGES);

            if (result != 0)
            {
                // SDC_ALLOW_CHANGES lets Windows adjust slightly if needed,
                // but if it still fails, log it
                Console.WriteLine($"SetDisplayConfig for refresh rate failed: {result}");
            }
        }
    }

    private static void SetScale(string deviceName, int scalePercent)
    {
        var targetIndex = Array.IndexOf(ScalePercentages, scalePercent);
        if (targetIndex < 0) return;

        var (_, _, pathIndex) = FindDisplayConfigPath(deviceName);
        if (pathIndex < 0) return;

        // Re-query to get fresh path info (config may have changed after SetRefreshRate)
        GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out uint pathCount, out uint modeCount);
        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);

        for (int i = 0; i < pathCount; i++)
        {
            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            sourceName.header.adapterId = paths[i].sourceInfo.adapterId;
            sourceName.header.id = paths[i].sourceInfo.id;

            if (DisplayConfigGetDeviceInfo(ref sourceName) != 0) continue;
            if (sourceName.viewGdiDeviceName != deviceName) continue;

            // Get current DPI info to determine recommended scale
            var getDpi = new DISPLAYCONFIG_GET_DPI_SCALE();
            getDpi.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE;
            getDpi.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_DPI_SCALE>();
            getDpi.header.adapterId = paths[i].sourceInfo.adapterId;
            getDpi.header.id = paths[i].sourceInfo.id;

            if (DisplayConfigGetDeviceInfo(ref getDpi) != 0) return;

            var recommendedIndex = Math.Abs(getDpi.minScaleSteps);
            var relativeSteps = targetIndex - recommendedIndex;

            // Clamp to the monitor's supported range
            relativeSteps = Math.Clamp(relativeSteps, getDpi.minScaleSteps, getDpi.maxScaleSteps);

            // Skip if already at the desired scale
            if (getDpi.currentScaleSteps == relativeSteps) return;

            var setDpi = new DISPLAYCONFIG_SET_DPI_SCALE();
            setDpi.header.type = DISPLAYCONFIG_DEVICE_INFO_SET_DPI_SCALE;
            setDpi.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SET_DPI_SCALE>();
            setDpi.header.adapterId = paths[i].sourceInfo.adapterId;
            setDpi.header.id = paths[i].sourceInfo.id;
            setDpi.scaleSteps = relativeSteps;

            DisplayConfigSetDeviceInfo(ref setDpi);
            return;
        }
    }

    public static List<DisplayProfile> GetCurrentProfiles()
    {
        var currentProfiles = new List<DisplayProfile>();
        var monitors = GetMonitors();
        for (var i = 0; i < monitors.Count; i++)
        {
            var (_, monitorId, _) = monitors[i];
            if (TryGetDisplayProfile(monitorId, out var profile))
            {
                profile.Name = $"Profile {i + 1}";
                currentProfiles.Add(profile);
            }
        }

        return currentProfiles;
    }

    public static bool TryGetDisplayProfile(string monitorId, out DisplayProfile profile)
    {
        profile = new DisplayProfile { MonitorId = monitorId };
        DEVMODE dm = new() { dmSize = (short)Marshal.SizeOf<DEVMODE>() };

        var deviceName = ResolveDeviceName(monitorId);
        if (deviceName == null) return false;
        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm)) return false;

        profile.Width = dm.dmPelsWidth;
        profile.Height = dm.dmPelsHeight;

        // refresh rate & scale
        var (paths, modes, pathIndex) = FindDisplayConfigPath(deviceName);
        if (pathIndex < 0) return false;

        // Find the target mode for this path
        var targetModeIdx = paths[pathIndex].targetInfo.modeInfoIdx;
        if (targetModeIdx >= modes.Length) return false;

        // Modify the target mode's vSyncFreq to the exact refresh rate
        var mode = modes[targetModeIdx];
        if (mode.infoType == DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
        {
            var vSyncFreq = mode.targetMode.targetVideoSignalInfo.vSyncFreq;
            var refreshRate = (double)vSyncFreq.Numerator / vSyncFreq.Denominator;
            profile.RefreshRate = Convert.ToInt32(refreshRate);
        }
        else
        {
            profile.RefreshRate = Constants.DEFAULT_REFRESH_RATE;
            return false;
        }

        // scale
        var getDpi = new DISPLAYCONFIG_GET_DPI_SCALE();
        getDpi.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_DPI_SCALE;
        getDpi.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_GET_DPI_SCALE>();
        getDpi.header.adapterId = paths[pathIndex].sourceInfo.adapterId;
        getDpi.header.id = paths[pathIndex].sourceInfo.id;

        if (DisplayConfigGetDeviceInfo(ref getDpi) != 0) return false;

        var currentIndex = Math.Abs(getDpi.minScaleSteps) + getDpi.currentScaleSteps;
        if (currentIndex >= 0 && currentIndex < ScalePercentages.Length)
        {
            profile.Scale = ScalePercentages[currentIndex];
        }
        else
        {
            profile.Scale = Constants.DEFAULT_SCALE;
        }

        return true;
    }
}
