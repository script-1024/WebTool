using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Windows.Win32.Core;

public class DisplayInfo
{
    public string Availability { get; set; }
    public int ScreenHeight { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenEfectiveHeight
    {
        get
        {
            _ = PInvoke.GetDpiForMonitor(hMonitor,
                MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out uint widthDPI, out _);
            float scalingFactor = (float)widthDPI / 96;
            return (int)(ScreenHeight / scalingFactor);
        }
    }
    public int ScreenEfectiveWidth
    {
        get
        {
            _ = PInvoke.GetDpiForMonitor(hMonitor,
                MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out _, out uint heightDPI);
            float scalingFactor = (float)heightDPI / 96;
            return (int)(ScreenWidth / scalingFactor);
        }
    }
    public RECT WorkArea { get; set; }
    public HMONITOR hMonitor { get; set; }
}

unsafe public class DisplayInformation
{
    public static int ConvertEpxToPixel(HWND hwnd, int effectivePixels)
    {
        float scalingFactor = GetScalingFactor(hwnd);
        return (int)(effectivePixels * scalingFactor);
    }

    public static int ConvertPixelToEpx(HWND hwnd, int pixels)
    {
        float scalingFactor = GetScalingFactor(hwnd);
        return (int)(pixels / scalingFactor);
    }

    public static float GetScalingFactor(HWND hwnd)
    {
        var dpi = PInvoke.GetDpiForWindow(hwnd);
        float scalingFactor = (float)dpi / 96;
        return scalingFactor;
    }

    public static DisplayInfo GetDisplay(HWND hwnd)
    {
        DisplayInfo di = null;
        PInvoke.GetWindowRect(hwnd, out RECT rc);
        HMONITOR hMonitor = PInvoke.MonitorFromRect(in rc, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

        MONITORINFO mi = new();
        mi.cbSize = (uint)Marshal.SizeOf(mi);
        bool success = PInvoke.GetMonitorInfo(hMonitor, ref mi);
        if (success)
        {
            di = ConvertMonitorInfoToDisplayInfo(hMonitor, mi);
        }
        return di;
    }

    private static DisplayInfo ConvertMonitorInfoToDisplayInfo(HMONITOR hMonitor, MONITORINFO mi)
    {
        return new DisplayInfo
        {
            ScreenWidth = mi.rcMonitor.right - mi.rcMonitor.left,
            ScreenHeight = mi.rcMonitor.bottom - mi.rcMonitor.top,
            WorkArea = mi.rcWork,
            Availability = mi.dwFlags.ToString(),
            hMonitor = hMonitor
        };
    }

    unsafe public static List<DisplayInfo> GetDisplays()
    {
        List<DisplayInfo> col = new();

        _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            delegate (HMONITOR hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = (uint)Marshal.SizeOf(mi);
                bool success = PInvoke.GetMonitorInfo(hMonitor, ref mi);
                if (success)
                {
                    DisplayInfo di = ConvertMonitorInfoToDisplayInfo(hMonitor, mi);
                    col.Add(di);
                }
                return true;
            }, IntPtr.Zero);
        return col;
    }

    public enum UserInteractionModeEnum { Touch, Mouse };
    public static UserInteractionModeEnum UserInteractionMode
    {
        get
        {
            // TODO: Have a counterpart event listeining the message WM_SETTINGCHANGE
            UserInteractionModeEnum userInteractionMode = UserInteractionModeEnum.Mouse;
            int SM_CONVERTIBLESLATEMODE = 0x2003;
            int state = GetSystemMetrics(SM_CONVERTIBLESLATEMODE);//O for tablet
            if (state == 0)
            {
                userInteractionMode = UserInteractionModeEnum.Touch;
            }
            return userInteractionMode;
        }
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "GetSystemMetrics")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
    delegate bool EnumMonitorsDelegate(HMONITOR hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

}
