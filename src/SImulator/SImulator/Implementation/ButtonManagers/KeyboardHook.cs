using SImulator.Implementation.WinAPI;
using SImulator.Properties;
using SImulator.ViewModel;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace SImulator.Implementation.ButtonManagers;

/// <summary>
/// Provides keyboard-based player buttons.
/// </summary>
internal sealed class KeyboardHook : ButtonManagerBase
{
    private IntPtr _hookPtr = IntPtr.Zero;
    private readonly Win32.LowLevelKeyboardProcDelegate _callbackPtr;

    public KeyboardHook(IButtonManagerListener buttonManagerListener) : base(buttonManagerListener)
    {
        _callbackPtr = new Win32.LowLevelKeyboardProcDelegate(KeyboardHookHandler);
    }

    public override bool Start()
    {
        using (var process = Process.GetCurrentProcess())
        using (var module = process.MainModule)
        {
            _hookPtr = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _callbackPtr, module.BaseAddress, 0);
        }

        if (_hookPtr == IntPtr.Zero)
        {
            var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;

            MessageBox.Show(
                $"{Resources.KeyboardListeningError}: {errorMessage}",
                MainViewModel.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        return true;
    }

    public override void Stop()
    {
        if (_hookPtr != IntPtr.Zero)
        {
            var result = Win32.UnhookWindowsHookEx(_hookPtr);

            if (result == 0)
            {
                var errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;

                MessageBox.Show(
                    $"{Resources.KeyboardDetachError}: {errorMessage}",
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            _hookPtr = IntPtr.Zero;
        }
    }

    private IntPtr KeyboardHookHandler(int nCode, IntPtr wParam, ref Win32.KBDLLHOOKSTRUCT lParam)
    {
        var key = KeyInterop.KeyFromVirtualKey(lParam.vkCode);

        if (Listener.OnKeyPressed((GameKey)key))
        {
            return new IntPtr(1);
        }

        return Win32.CallNextHookEx(_hookPtr, nCode, wParam, ref lParam);
    }
}
