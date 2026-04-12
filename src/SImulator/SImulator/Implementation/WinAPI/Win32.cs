using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SImulator.Implementation.WinAPI;

/// <summary>
/// Provides helper WinAPI methods.
/// </summary>
internal static class Win32
{
    private const int MONITORINFOF_PRIMARY = 0x00000001;
    private const int CC_FULLOPEN = 0x00000002;

    internal delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    /// <remarks>
    /// When Debugger is attached the hook does not work.
    /// </remarks>
    [DllImport("user32", EntryPoint = "SetWindowsHookExA", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    internal static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProcDelegate lpfn, IntPtr hMod, int dwThreadId);
    
    [DllImport("user32", EntryPoint = "UnhookWindowsHookEx", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    internal static extern int UnhookWindowsHookEx(IntPtr hHook);
    
    [DllImport("user32", EntryPoint = "CallNextHookEx", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    internal static extern IntPtr CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

    internal const int WH_KEYBOARD_LL = 13;

    internal static IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorCallback, IntPtr.Zero);

        displays.Sort((left, right) =>
        {
            var primaryCompare = right.IsPrimary.CompareTo(left.IsPrimary);

            if (primaryCompare != 0)
            {
                return primaryCompare;
            }

            var xCompare = left.Left.CompareTo(right.Left);
            return xCompare != 0 ? xCompare : left.Top.CompareTo(right.Top);
        });

        return displays;

        bool MonitorCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var monitorInfo = new MONITORINFOEX
            {
                cbSize = Marshal.SizeOf<MONITORINFOEX>(),
            };

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                var workArea = monitorInfo.rcWork;

                displays.Add(new DisplayInfo(
                    workArea.Left,
                    workArea.Top,
                    workArea.Right - workArea.Left,
                    workArea.Bottom - workArea.Top,
                    (monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0));
            }

            return true;
        }
    }

    internal static bool TryChooseColor(out Color color)
    {
        var customColors = Marshal.AllocCoTaskMem(16 * sizeof(int));

        try
        {
            var chooseColor = new CHOOSECOLOR
            {
                lStructSize = Marshal.SizeOf<CHOOSECOLOR>(),
                hwndOwner = GetOwnerHandle(),
                lpCustColors = customColors,
                Flags = CC_FULLOPEN,
            };

            if (!ChooseColor(ref chooseColor))
            {
                color = default;
                return false;
            }

            color = Color.FromRgb(
                (byte)(chooseColor.rgbResult & 0xFF),
                (byte)((chooseColor.rgbResult >> 8) & 0xFF),
                (byte)((chooseColor.rgbResult >> 16) & 0xFF));

            return true;
        }
        finally
        {
            Marshal.FreeCoTaskMem(customColors);
        }
    }

    private static IntPtr GetOwnerHandle() =>
        Application.Current?.MainWindow != null
            ? new WindowInteropHelper(Application.Current.MainWindow).Handle
            : IntPtr.Zero;

    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public KBDLLHOOKSTRUCTFlags flags;
        public int time;
        public int dwExtraInfo;
    }

    [Flags]
    internal enum KBDLLHOOKSTRUCTFlags : int
    {
        LLKHF_EXTENDED = 0x01,
        LLKHF_INJECTED = 0x10,
        LLKHF_ALTDOWN = 0x20,
        LLKHF_UP = 0x80,
    }

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("comdlg32.dll", CharSet = CharSet.Auto)]
    private static extern bool ChooseColor(ref CHOOSECOLOR lpcc);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        internal int Left;
        internal int Top;
        internal int Right;
        internal int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        internal int cbSize;
        internal RECT rcMonitor;
        internal RECT rcWork;
        internal int dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        internal string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct CHOOSECOLOR
    {
        internal int lStructSize;
        internal IntPtr hwndOwner;
        internal IntPtr hInstance;
        internal int rgbResult;
        internal IntPtr lpCustColors;
        internal int Flags;
        internal IntPtr lCustData;
        internal IntPtr lpfnHook;
        internal IntPtr lpTemplateName;
    }
}
