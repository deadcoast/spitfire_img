using System.Runtime.InteropServices;

namespace ImageViewer.App.Services;

public static class WindowPlacementService
{
    public sealed record DisplayWorkArea(int X, int Y, int Width, int Height);

    public static bool IsSavedWindowPositionVisible(int x, int y)
    {
        return IsSavedWindowPositionVisible(x, y, EnumerateDisplayWorkAreas());
    }

    public static bool IsSavedWindowPositionVisible(int x, int y, IEnumerable<DisplayWorkArea> displayWorkAreas)
    {
        return displayWorkAreas.Any(workArea =>
            x >= workArea.X &&
            y >= workArea.Y &&
            x < workArea.X + workArea.Width &&
            y < workArea.Y + workArea.Height);
    }

    private static IReadOnlyList<DisplayWorkArea> EnumerateDisplayWorkAreas()
    {
        var displays = new List<DisplayWorkArea>();
        EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (monitor, _, _, _) =>
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (GetMonitorInfo(monitor, ref info))
                {
                    displays.Add(new DisplayWorkArea(
                        info.Work.Left,
                        info.Work.Top,
                        info.Work.Right - info.Work.Left,
                        info.Work.Bottom - info.Work.Top));
                }

                return true;
            },
            IntPtr.Zero);

        return displays;
    }

    private delegate bool MonitorEnumProc(
        IntPtr monitor,
        IntPtr deviceContext,
        IntPtr monitorRect,
        IntPtr data);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr deviceContext,
        IntPtr clippingRect,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
