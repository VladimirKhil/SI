using System;
using System.Diagnostics;
using System.Windows;

namespace SIQuester.ViewModel.Helpers
{
    public static class Browser
    {
        public static void Open(string uri, Action<Exception> onError = null)
        {
            try
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {uri.Replace("&", "^&")}") { CreateNoWindow = true });
            }
            catch (Exception exc)
            {
                if (onError != null)
                {
                    onError(exc);
                }
                else
                {
                    MessageBox.Show($"Error while navigating to {uri}: {exc.Message}");
                }
            }
        }
    }
}
