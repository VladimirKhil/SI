using System.Diagnostics;

namespace Utils;

/// <summary>
/// Provides helper method for opening links in browser.
/// </summary>
public static class Browser
{
    /// <summary>
    /// Opens link in default browser.
    /// </summary>
    /// <param name="uri">Link to open.</param>
    public static void Open(string uri) =>
        Process.Start(new ProcessStartInfo("cmd", $"/c start {uri.Replace("&", "^&")}") { CreateNoWindow = true });
}
