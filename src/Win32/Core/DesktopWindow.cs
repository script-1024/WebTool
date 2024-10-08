﻿using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32.Core;

public struct WindowPosition(int top, int left)
{
    public int Top { get; private set; } = top;
    public int Left { get; private set; } = left;
}

#region WindowEventArgs
public class WindowEventArgs(DesktopWindow window) : EventArgs
{
    public DesktopWindow Window { get; protected set; } = window;
}

public class WindowSizingEventArgs(DesktopWindow window) : WindowEventArgs(window);

public class WindowClosingEventArgs(DesktopWindow window) : WindowEventArgs(window)
{
    public void TryCancel()
    {
        Window.IsClosing = true;
        Window.Close();
    }
}

public class WindowPreferredLanguageChangedEventArgs(DesktopWindow window, string oldLanguage, string newLanguage) : WindowEventArgs(window)
{
    public string OldLanguage { get; private set; } = oldLanguage;
    public string NewLanguage { get; private set; } = newLanguage;
}

public class WindowDpiChangedEventArgs(DesktopWindow window, int newDpi) : WindowEventArgs(window)
{
    public int Dpi { get; private set; } = newDpi;
}

public class WindowOrientationChangedEventArgs(DesktopWindow window, DesktopWindow.Orientation newOrientation) : WindowEventArgs(window)
{
    public DesktopWindow.Orientation Orientation { get; private set; } = newOrientation;
}

public class WindowMovingEventArgs(DesktopWindow window, WindowPosition windowPosition) : WindowEventArgs(window)
{
    public WindowPosition NewPosition { get; private set; } = new(windowPosition.Top, windowPosition.Left);
}

public class WindowKeyEventArgs(DesktopWindow window, int key) : WindowEventArgs(window)
{
    public int Key { get; private set; } = key;
}
#endregion

public class DesktopWindow : Window
{
    public enum Orientation { Landscape, Portrait }
    public enum Placement { Center, TopLeftCorner, BottomLeftCorner } //Future: align to the top corner, etc..
    public int Width
    {
        get => DisplayInformation.ConvertPixelToEpx(_hwnd, GetWidthWin32(_hwnd));
        set => SetWindowWidthWin32(_hwnd, DisplayInformation.ConvertEpxToPixel(_hwnd, value));
    }

    public int Height
    {
        get => DisplayInformation.ConvertPixelToEpx(_hwnd, GetHeightWin32(_hwnd));
        set => SetWindowHeightWin32(_hwnd, DisplayInformation.ConvertEpxToPixel(_hwnd, value));
    }

    public int MinWidth { get; set; } = -1;
    public int MinHeight { get; set; } = -1;
    public int MaxWidth { get; set; } = -1;
    public int MaxHeight { get; set; } = -1;

    public bool IsClosing { get; set; }

    public event EventHandler<WindowClosingEventArgs> Closing;
    public event EventHandler<WindowPreferredLanguageChangedEventArgs> PreferredLanguageChanged;
    public event EventHandler<WindowMovingEventArgs> Moving;
    public event EventHandler<WindowSizingEventArgs> Sizing;
    public event EventHandler<WindowDpiChangedEventArgs> DpiChanged;
    public event EventHandler<WindowOrientationChangedEventArgs> OrientationChanged;
    public event EventHandler<WindowKeyEventArgs> KeyDown;

    private string _preferredLanguage;

    public HWND Hwnd => _hwnd;

    public uint Dpi => PInvoke.GetDpiForWindow(_hwnd);

    public DesktopWindow()
    {
        SubClassing();
        _currentOrientation = GetWindowOrientationWin32(_hwnd);
        _preferredLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
    }

    public void SetWindowPlacement(Placement placement)
    {
        switch (placement)
        {
            case Placement.Center:
                PlacementCenterWindowInMonitorWin32(_hwnd);
                break;
            case Placement.TopLeftCorner:
                PlacementTopLeftWindowInMonitorWin32(_hwnd);
                break;
            case Placement.BottomLeftCorner:
                PlacementBottomLeftWindowInMonitorWin32(_hwnd);
                break;
        }
    }

    public void SetWindowPlacement(int topExp, int leftExp)
    {
        SetWindowPlacementWin32(_hwnd, DisplayInformation.ConvertEpxToPixel(_hwnd, topExp),
                                       DisplayInformation.ConvertEpxToPixel(_hwnd, leftExp));
    }

    public WindowPosition GetWindowPosition()
    {
        //windowPosition comes in pixels(Win32), so you need to convert into epx
        WindowPosition windowPosition = GetWindowPositionWin32(_hwnd);

        return new(DisplayInformation.ConvertPixelToEpx(_hwnd, windowPosition.Top),
                   DisplayInformation.ConvertPixelToEpx(_hwnd, windowPosition.Left));
    }

    public string Icon
    {
        get => _iconResource;
        set
        {
            _iconResource = value;
            LoadIcon(_hwnd, _iconResource);
        }
    }

    public void Maximize() => PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MAXIMIZE);

    public void Minimize() => PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);

    public void Restore() => PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_RESTORE);

    public void Hide() => PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_HIDE);

    public void BringToTop() => PInvoke.SetWindowPos(_hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOPMOST, 0, 0, 0, 0,
                                        SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE);

    #region Private
    private string _iconResource;
    private HWND _hwnd = HWND.Null;
    Orientation _currentOrientation;

    private void OnClosing() => Closing.Invoke(this, new(this));

    private void OnWindowLanguageChanged(string oldLanguage, string newLanguage) => PreferredLanguageChanged.Invoke(this, new(this, oldLanguage, newLanguage));

    private void OnWindowMoving()
    {
        var windowPosition = GetWindowPositionWin32(_hwnd);

        //windowPosition comes in pixels(Win32), so we need to convert into epx
        WindowMovingEventArgs windowMovingEventArgs = new(this,
            new WindowPosition(
                DisplayInformation.ConvertPixelToEpx(_hwnd, windowPosition.Top),
                DisplayInformation.ConvertPixelToEpx(_hwnd, windowPosition.Left)));

        Moving.Invoke(this, windowMovingEventArgs);
    }
    private void OnWindowSizing() => Sizing.Invoke(this, new(this));

    private void OnWindowDpiChanged(int newDpi) => DpiChanged.Invoke(this, new(this, newDpi));

    private void OnWindowOrientationChanged(Orientation newOrinetation) => OrientationChanged.Invoke(this, new(this, newOrinetation));

    private void OnWindowKeyDown(int key) => KeyDown.Invoke(this, new(this, key));

    private WNDPROC newWndProc = null;
    private WNDPROC oldWndProc = null;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern WNDPROC SetWindowLongPtr32(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, WNDPROC newProc);
    
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern WNDPROC SetWindowLongPtr64(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, WNDPROC newProc);
    
    // This static method is required because Win32 does not support
    // GetWindowLongPtr directly
    private static WNDPROC SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, WNDPROC newProc)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, newProc);
        else
            return SetWindowLongPtr32(hWnd, nIndex, newProc);
    }

    private void SubClassing()
    {
        //Get the Window's HWND
        _hwnd = (HWND)WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (_hwnd == HWND.Null) throw new NullReferenceException("The Window Handle is null.");
        newWndProc = new WNDPROC(NewWindowProc);
        oldWndProc = SetWindowLongPtr(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, newWndProc);
    }

    private static void LoadIcon(HWND hwnd, string iconName)
    {
        const int ICON_SMALL = 0;
        const int ICON_BIG = 1;

        var hSmallIcon = PInvoke.LoadImage(
            HINSTANCE.Null,
            iconName,
            GDI_IMAGE_TYPE.IMAGE_ICON,
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSMICON),
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYSMICON),
            IMAGE_FLAGS.LR_LOADFROMFILE);

        PInvoke.SendMessage(hwnd, (uint)WINDOW_MESSAGES.WM_SETICON, (WPARAM)ICON_SMALL, (LPARAM)hSmallIcon.Value);

        var hBigIcon = PInvoke.LoadImage(
            HINSTANCE.Null,
            iconName,
            GDI_IMAGE_TYPE.IMAGE_ICON,
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXICON),
            PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYICON),
            IMAGE_FLAGS.LR_LOADFROMFILE);

        PInvoke.SendMessage(hwnd, (uint)WINDOW_MESSAGES.WM_SETICON, (WPARAM)ICON_BIG, (LPARAM)hBigIcon.Value);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    private LRESULT NewWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
    {
        switch ((WINDOW_MESSAGES)Msg)
        {
            case WINDOW_MESSAGES.WM_GETMINMAXINFO:
                MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                if (MinWidth >= 0) minMaxInfo.ptMinTrackSize.X = DisplayInformation.ConvertEpxToPixel(hWnd, MinWidth);
                if (MinHeight >= 0) minMaxInfo.ptMinTrackSize.Y = DisplayInformation.ConvertEpxToPixel(hWnd, MinHeight);
                if (MaxWidth > 0) minMaxInfo.ptMaxTrackSize.X = DisplayInformation.ConvertEpxToPixel(hWnd, MaxWidth);
                if (MaxHeight > 0) minMaxInfo.ptMaxTrackSize.Y = DisplayInformation.ConvertEpxToPixel(hWnd, MaxHeight);
                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;

            case WINDOW_MESSAGES.WM_SETTINGCHANGE:
                var newLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
                if (_preferredLanguage != newLanguage)
                {
                    OnWindowLanguageChanged(_preferredLanguage, newLanguage);
                    _preferredLanguage = newLanguage;
                }
                break;

            case WINDOW_MESSAGES.WM_CLOSE:

                //If there is a Closing event handler and the close message wasn't send via
                //this event (that set IsClosing=true), the message is ignored. 
                if (this.Closing is not null)
                {
                    if (IsClosing == false)
                    {
                        OnClosing();
                    }
                    return (LRESULT)0;
                }
                break;

            case WINDOW_MESSAGES.WM_MOVE:
                if (this.Moving is not null)
                {
                    OnWindowMoving();
                }
                break;
            case WINDOW_MESSAGES.WM_SIZING:
                if (this.Sizing is not null)
                {
                    OnWindowSizing();
                }
                break;
            case WINDOW_MESSAGES.WM_DPICHANGED:
                if (this.DpiChanged is not null)
                {
                    uint dpi = HiWord((nint)wParam.Value);
                    OnWindowDpiChanged((int)dpi);
                }
                break;
            case WINDOW_MESSAGES.WM_DISPLAYCHANGE:
                if (this.OrientationChanged is not null)
                {
                    var newOrinetation = GetWindowOrientationWin32(hWnd);
                    if (newOrinetation != _currentOrientation)
                    {
                        _currentOrientation = newOrinetation;
                        OnWindowOrientationChanged(newOrinetation);
                    }
                }
                break;
            //This don't work.
            case WINDOW_MESSAGES.WM_KEYDOWN:
                if (this.KeyDown is not null)
                {
                    int value = (int)wParam.Value;
                    OnWindowKeyDown(value);
                }
                break;
        }
        return PInvoke.CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
    }

    private static Orientation GetWindowOrientationWin32(HWND hwnd)
    {
        Orientation orientationEnum;
        int theScreenWidth = DisplayInformation.GetDisplay(hwnd).ScreenWidth;
        int theScreenHeight = DisplayInformation.GetDisplay(hwnd).ScreenHeight;
        if (theScreenWidth > theScreenHeight)
            orientationEnum = Orientation.Landscape;
        else
            orientationEnum = Orientation.Portrait;
        return orientationEnum;
    }

    private static uint HiWord(IntPtr ptr)
    {
        uint value = (uint)(int)ptr;
        if ((value & 0x80000000) == 0x80000000)
            return (value >> 16);
        else
            return (value >> 16) & 0xffff;
    }

    private static int GetWidthWin32(HWND hwnd)
    {
        PInvoke.GetWindowRect(hwnd, out RECT rc);
        return rc.right - rc.left;
    }

    private static int GetHeightWin32(HWND hwnd)
    {
        PInvoke.GetWindowRect(hwnd, out RECT rc);
        return rc.bottom - rc.top;
    }

    private static void SetWindowSizeWin32(HWND hwnd, int width, int height)
    {
        PInvoke.SetWindowPos(hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOP,
            0, 0, width, height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private static WindowPosition GetWindowPositionWin32(HWND hwnd)
    {
        PInvoke.GetWindowRect(hwnd, out RECT rc);
        return new WindowPosition(rc.top, rc.left);
    }

    private static void SetWindowWidthWin32(HWND hwnd, int width)
    {
        int currentHeightInPixels = GetHeightWin32(hwnd);

        PInvoke.SetWindowPos(hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOP,
            0, 0, width, currentHeightInPixels,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private static void SetWindowHeightWin32(HWND hwnd, int height)
    {
        int currentWidthInPixels = GetWidthWin32(hwnd);

        PInvoke.SetWindowPos(hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOP,
            0, 0, currentWidthInPixels, height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private void PlacementTopLeftWindowInMonitorWin32(HWND hwnd)
    {
        var displayInfo = DisplayInformation.GetDisplay(hwnd);
        SetWindowPlacementWin32(hwnd, displayInfo.WorkArea.top, displayInfo.WorkArea.left);
    }
    private void PlacementBottomLeftWindowInMonitorWin32(HWND hwnd)
    {
        var displayInfo = DisplayInformation.GetDisplay(hwnd);
        SetWindowPlacementWin32(hwnd, displayInfo.WorkArea.bottom - GetHeightWin32(_hwnd), displayInfo.WorkArea.left);
    }

    private static void SetWindowPlacementWin32(HWND hwnd, int top, int left)
    {
        PInvoke.SetWindowPos(hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOP,
            left, top, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private static void PlacementCenterWindowInMonitorWin32(HWND hwnd)
    {
        PInvoke.GetWindowRect(hwnd, out RECT rc);
        ClipOrCenterRectToMonitorWin32(ref rc, true, true);
        PInvoke.SetWindowPos(hwnd, SPECIAL_WINDOW_HANDLES.HWND_TOP,
            rc.left, rc.top, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }

    private static void ClipOrCenterRectToMonitorWin32(ref RECT prc, bool UseWorkArea, bool IsCenter)
    {
        int w = prc.right - prc.left;
        int h = prc.bottom - prc.top;

        HMONITOR hMonitor = PInvoke.MonitorFromRect(in prc, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        
        MONITORINFO mi = new();
        mi.cbSize = (uint)Marshal.SizeOf(mi);

        PInvoke.GetMonitorInfo(hMonitor, ref mi);

        RECT rc = UseWorkArea ? mi.rcWork : mi.rcMonitor;

        if (IsCenter)
        {
            prc.left = rc.left + (rc.right - rc.left - w) / 2;
            prc.top = rc.top + (rc.bottom - rc.top - h) / 2;
            prc.right = prc.left + w;
            prc.bottom = prc.top + h;
        }
        else
        {
            prc.left = Math.Max(rc.left, Math.Min(rc.right - w, prc.left));
            prc.top = Math.Max(rc.top, Math.Min(rc.bottom - h, prc.top));
            prc.right = prc.left + w;
            prc.bottom = prc.top + h;
        }
    }

    #endregion
}

