using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace SIQuester.Implementation;

internal static class FileHelper
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.U4)]
    private static extern int GetLongPathName(
        [MarshalAs(UnmanagedType.LPWStr)] string lpszShortPath,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpszLongPath,
        [MarshalAs(UnmanagedType.U4)] int cchBuffer);

    internal static string GetLongPathName(string shortPathName)
    {
        var longPath = new StringBuilder(255);

        if (GetLongPathName(shortPathName, longPath, longPath.Capacity) == 0)
        {
            throw new Exception($"Error getting long file name for {shortPathName}", new Win32Exception(Marshal.GetLastWin32Error()));
        }

        return longPath.ToString();
    }
}
