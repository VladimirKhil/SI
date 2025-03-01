using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Utils.Web;

namespace Utils.Wpf.Behaviors;

/// <summary>
/// Attaches additional behavior for WebView2 control.
/// </summary>
public static class WebView2Behavior
{
    public static bool GetIsAttached(DependencyObject obj) => (bool)obj.GetValue(IsAttachedProperty);

    public static void SetIsAttached(DependencyObject obj, bool value) => obj.SetValue(IsAttachedProperty, value);

    public static readonly DependencyProperty IsAttachedProperty =
        DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(WebView2), new PropertyMetadata(false, OnIsAttachedChanged));

    public static string[] GetAllowedHosts(DependencyObject obj) => (string[])obj.GetValue(AllowedHostsProperty);

    public static void SetAllowedHosts(DependencyObject obj, string[] value) => obj.SetValue(AllowedHostsProperty, value);

    public static readonly DependencyProperty AllowedHostsProperty =
        DependencyProperty.RegisterAttached("AllowedHosts", typeof(string[]), typeof(WebView2), new PropertyMetadata(Array.Empty<string>()));

    public static bool GetAllowLocalFilesAccess(DependencyObject obj) => (bool)obj.GetValue(AllowLocalFilesAccessProperty);

    public static void SetAllowLocalFilesAccess(DependencyObject obj, bool value) => obj.SetValue(AllowLocalFilesAccessProperty, value);

    public static readonly DependencyProperty AllowLocalFilesAccessProperty =
        DependencyProperty.RegisterAttached("AllowLocalFilesAccess", typeof(bool), typeof(WebView2), new PropertyMetadata(false));

    public static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (!(bool)e.NewValue)
        {
            return;
        }

        var webView2 = (WebView2)d;

        UpdateWebView2Environment(webView2);
        
        webView2.NavigationStarting += WebView2_NavigationStarting;
        webView2.Unloaded += WebView2_Unloaded;
    }

    private static void WebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (sender is not DependencyObject obj)
        {
            return;
        }

        var allowedHosts = GetAllowedHosts(obj);

        if (allowedHosts.Length == 0 || allowedHosts.Any(host => e.Uri.StartsWith(host)))
        {
            return;
        }

        e.Cancel = true;
    }

    private static void WebView2_Unloaded(object sender, RoutedEventArgs e)
    {
        var webView2 = (WebView2)sender;

        if (webView2 == null)
        {
            return;
        }

        webView2.NavigationStarting -= WebView2_NavigationStarting;
        webView2.Unloaded -= WebView2_Unloaded;

        try
        {
            var coreWebView = webView2.CoreWebView2;

            if (coreWebView != null)
            {
                // Prevent WebView for producing sound after unload
                coreWebView.IsMuted = true;

                if (webView2.DataContext is IWebInterop webInterop)
                {
                    webInterop.SendJsonMessage -= coreWebView.PostWebMessageAsJson;
                    webView2.WebMessageReceived -= WebView2_WebMessageReceived;
                    webView2.CoreWebView2.ProcessFailed -= CoreWebView2_ProcessFailed;
                }
            }
        }
        catch (InvalidOperationException exc)
        {
            Trace.TraceError(exc.ToString());
        }
    }

    private static async void UpdateWebView2Environment(WebView2 webView2)
    {
        try
        {
            var allowLocalFilesAccess = GetAllowLocalFilesAccess(webView2);
            // Allowing autoplay
            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required" + (allowLocalFilesAccess ? " --allow-file-access-from-files" : ""));
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);

            try
            {
                await webView2.EnsureCoreWebView2Async(environment);
            }
            catch (COMException exc) when ((uint)exc.HResult == 0x8007139F)
            {
                // Delete the registry key that prevents WebView2 from loading
                // See https://github.com/MicrosoftEdge/WebView2Feedback/issues/3008#issuecomment-1916313157
                DeleteHighDpiAwareKey();
                await webView2.EnsureCoreWebView2Async(environment);
            }

            if (webView2.DataContext is IWebInterop webInterop)
            {
                webInterop.SendJsonMessage += webView2.CoreWebView2.PostWebMessageAsJson;
                webView2.WebMessageReceived += WebView2_WebMessageReceived;
                webView2.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(exc.ToString(), "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void DeleteHighDpiAwareKey()
    {
        const string problemFolder = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers";
        const string problemSuffix = "msedgewebview2.exe";
        const string problemValue = "HIGHDPIAWARE";

        try
        {
            using var baseKey = Registry.CurrentUser.OpenSubKey(problemFolder, true);
            
            if (baseKey == null)
            {
                Trace.WriteLine($"Registry key not found: {problemFolder}");
                return;
            }

            var valueNames = baseKey.GetValueNames();

            foreach (var valueName in valueNames)
            {
                if (!valueName.EndsWith(problemSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                var value = baseKey.GetValue(valueName)?.ToString();
                
                if (value != null && value.Contains(problemValue))
                {
                    baseKey.DeleteValue(valueName);
                    Trace.WriteLine($"Deleted registry key: {valueName}");
                }
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    private static void CoreWebView2_ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (sender is CoreWebView2)
        {
            MessageBox.Show($"{e.ExitCode} {e.Reason} {e.ExitCode}", "Browser error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void WebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (sender is WebView2 webView2 && webView2.DataContext is IWebInterop webInterop)
        {
            webInterop.OnMessage(e.WebMessageAsJson);
        }
    }
}
