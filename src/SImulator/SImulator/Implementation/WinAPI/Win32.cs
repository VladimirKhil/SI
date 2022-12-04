using System;
using System.Runtime.InteropServices;

namespace SImulator.Implementation.WinAPI;

/// <summary>
/// Provides helper WinAPI methods.
/// </summary>
internal static class Win32
{
    internal delegate IntPtr LowLevelKeyboardProcDelegate(int nCode, IntPtr wParam, ref KBDLLHOOKSTRUCT lParam);

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
}
