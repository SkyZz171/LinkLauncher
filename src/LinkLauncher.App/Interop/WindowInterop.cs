namespace LinkLauncher.App.Interop;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using LinkLauncher.Core.Models;

/// <summary>
/// Réglages de fenêtre propres à Windows : coins arrondis, bordure sombre et
/// ajustement de la zone agrandie à la zone de travail de l'écran.
/// </summary>
internal static class WindowInterop
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmUseImmersiveDarkMode = 20;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;

    private static readonly Dictionary<IntPtr, HwndSource> HookedWindows = [];

    /// <summary>Active ou désactive les coins arrondis de Windows 11.</summary>
    public static void SetRoundedCorners(Window window, bool enabled)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var preference = enabled ? 2 : 1;
        DwmSetWindowAttribute(handle, DwmWindowCornerPreference, ref preference, sizeof(int));
    }

    /// <summary>Indique à Windows si la bordure de la fenêtre doit suivre un thème sombre.</summary>
    public static void SetDarkBorder(Window window, AppTheme theme)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var useDarkMode = theme == AppTheme.Dark ? 1 : 0;
        DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
    }

    /// <summary>
    /// Empêche une fenêtre sans chrome système de dépasser la zone de travail
    /// lorsqu'elle est agrandie (barre des tâches masquée).
    /// </summary>
    public static void EnableWorkAreaClamp(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || HookedWindows.ContainsKey(handle))
        {
            return;
        }

        var source = HwndSource.FromHwnd(handle);
        if (source is null)
        {
            return;
        }

        source.AddHook(ClampToWorkArea);
        HookedWindows[handle] = source;
    }

    /// <summary>Retire le raccordement posé sur une fenêtre.</summary>
    public static void DisableWorkAreaClamp(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !HookedWindows.Remove(handle, out var source))
        {
            return;
        }

        source.RemoveHook(ClampToWorkArea);
    }

    private static IntPtr ClampToWorkArea(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message != WmGetMinMaxInfo)
        {
            return IntPtr.Zero;
        }

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return IntPtr.Zero;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workAreaWidth = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
        var workAreaHeight = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;

        minMaxInfo.MaxPosition.X = monitorInfo.WorkArea.Left - monitorInfo.Monitor.Left;
        minMaxInfo.MaxPosition.Y = monitorInfo.WorkArea.Top - monitorInfo.Monitor.Top;
        minMaxInfo.MaxSize.X = workAreaWidth;
        minMaxInfo.MaxSize.Y = workAreaHeight;
        minMaxInfo.MaxTrackSize.X = workAreaWidth;
        minMaxInfo.MaxTrackSize.Y = workAreaHeight;

        Marshal.StructureToPtr(minMaxInfo, lParam, fDeleteOld: true);
        handled = true;
        return IntPtr.Zero;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }
}
