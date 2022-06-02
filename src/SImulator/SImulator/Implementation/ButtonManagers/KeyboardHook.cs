using System;
using System.Windows.Input;
using System.Windows;
using System.Runtime.InteropServices;
using SImulator.Implementation.WinAPI;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel;

namespace SImulator.Implementation.ButtonManagers
{
    /// <summary>
    /// Provides keyboard-based player buttons.
    /// </summary>
    internal sealed class KeyboardHook : ButtonManagerBase
    {
        private IntPtr _hookPtr = IntPtr.Zero;
        private readonly Win32.LowLevelKeyboardProcDelegate _callbackPtr;

        public KeyboardHook()
        {
            _callbackPtr = new Win32.LowLevelKeyboardProcDelegate(KeyboardHookHandler);
        }

        public override bool Run()
        {
            _hookPtr = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _callbackPtr, Marshal.GetHINSTANCE(Application.Current.GetType().Module), 0);

            if (_hookPtr == IntPtr.Zero)
            {
                MessageBox.Show("Ошибка прослушивания клавиатуры", MainViewModel.ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return true;
        }

        public override void Stop()
        {
            if (_hookPtr != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(_hookPtr);
                _hookPtr = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardHookHandler(int nCode, IntPtr wParam, ref Win32.KBDLLHOOKSTRUCT lParam)
        {
            var key = KeyInterop.KeyFromVirtualKey(lParam.vkCode);
            if (OnKeyPressed((GameKey)key))
            {
                return new IntPtr(1);
            }

            return Win32.CallNextHookEx(_hookPtr, nCode, wParam, ref lParam);
        }
    }
}
