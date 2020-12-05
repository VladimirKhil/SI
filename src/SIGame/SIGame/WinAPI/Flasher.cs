using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SIWindows.WinAPI
{
    /// <summary>
    /// Для мигания окна
    /// </summary>
    /// <remarks>Source: http://pietschsoft.com/post/2009/01/CSharp-Flash-Window-in-Taskbar-via-Win32-FlashWindowEx.aspx </remarks>
    public sealed class Flasher
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            /// <summary>
            /// The size of the structure in bytes.
            /// </summary>
            public uint cbSize;
            /// <summary>
            /// A Handle to the Window to be Flashed. The window can be either opened or minimized.
            /// </summary>
            public IntPtr hwnd;
            /// <summary>
            /// The Flash Status.
            /// </summary>
            public uint dwFlags;
            /// <summary>
            /// The number of times to Flash the window.
            /// </summary>
            public uint uCount;
            /// <summary>
            /// The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
            /// </summary>
            public uint dwTimeout;
        }

        /// <summary>
        /// Stop flashing. The system restores the window to its original stae.
        /// </summary>
        public const uint FLASHW_STOP = 0;

        /// <summary>
        /// Flash the window caption.
        /// </summary>
        public const uint FLASHW_CAPTION = 1;

        /// <summary>
        /// Flash the taskbar button.
        /// </summary>
        public const uint FLASHW_TRAY = 2;

        /// <summary>
        /// Flash both the window caption and taskbar button.
        /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
        /// </summary>
        public const uint FLASHW_ALL = 3;

        /// <summary>
        /// Flash continuously, until the FLASHW_STOP flag is set.
        /// </summary>
        public const uint FLASHW_TIMER = 4;

        /// <summary>
        /// Flash continuously until the window comes to the foreground.
        /// </summary>
        public const uint FLASHW_TIMERNOFG = 12;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        /// <summary>
        /// Flash the spacified Window (Form) until it recieves focus.
        /// </summary>
        /// <param name="form">The Form (Window) to Flash.</param>
        /// <returns></returns>
        public static bool Flash(Window window)
        {
            if (window == null)
                return false;

            if (!(PresentationSource.FromVisual(window) is HwndSource handle))
                return false;

            var fi = Create_FLASHWINFO(handle.Handle, FLASHW_ALL | FLASHW_TIMERNOFG, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }

        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            var fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }

        /// <summary>
        /// Flash the specified Window (form) for the specified number of times
        /// </summary>
        /// <param name="form">The Form (Window) to Flash.</param>
        /// <param name="count">The number of times to Flash.</param>
        /// <returns></returns>
        public static bool Flash(Window window, uint count)
        {
            var handle = (HwndSource)HwndSource.FromVisual(window);
            var fi = Create_FLASHWINFO(handle.Handle, FLASHW_ALL, count, 0);
            return FlashWindowEx(ref fi);
        }

        /// <summary>
        /// Start Flashing the specified Window (form)
        /// </summary>
        /// <param name="form">The Form (Window) to Flash.</param>
        /// <returns></returns>
        public static bool Start(Window window)
        {
            var handle = (HwndSource)HwndSource.FromVisual(window);
            var fi = Create_FLASHWINFO(handle.Handle, FLASHW_ALL, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }

        /// <summary>
        /// Stop Flashing the specified Window (form)
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public static bool Stop(Window window)
        {
            var handle = (HwndSource)HwndSource.FromVisual(window);
            if (handle == null)
                return false;
            var fi = Create_FLASHWINFO(handle.Handle, FLASHW_STOP, uint.MaxValue, 0);
            return FlashWindowEx(ref fi);
        }
    }
}
