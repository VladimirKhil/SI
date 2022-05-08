using System;
using System.Diagnostics;
using System.Windows;

namespace Utils
{
    /// <summary>
    /// Provides helper method for opening links in browser.
    /// </summary>
    public static class Browser
    {
        /// <summary>
        /// Opens link in default browser.
        /// </summary>
        /// <param name="uri">Link to open.</param>
        /// <param name="onError">Optional error handler. Shows MessageBox as a default handler.</param>
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
                    MessageBox.Show(
                        $"Error while navigating to {uri}: {exc.Message}",
                        "Uri open error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
            }
        }
    }
}
