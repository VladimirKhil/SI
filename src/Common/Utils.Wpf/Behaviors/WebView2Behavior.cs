using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Diagnostics;
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

        var coreWebView = webView2.CoreWebView2;

        if (coreWebView != null)
        {
            // Prevent WebView for producing sound after unload
            coreWebView.IsMuted = true;

            if (webView2.DataContext is IWebInterop webInterop)
            {
                webInterop.SendJsonMessage -= coreWebView.PostWebMessageAsJson;
                webView2.WebMessageReceived -= WebView2_WebMessageReceived;
            }
        }
    }

    private static async void UpdateWebView2Environment(WebView2 webView2)
    {
        try
        {
            // Allowing autoplay
            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);

            await webView2.EnsureCoreWebView2Async(environment);

            if (webView2.DataContext is IWebInterop webInterop)
            {
                webInterop.SendJsonMessage += webView2.CoreWebView2.PostWebMessageAsJson;
                webView2.WebMessageReceived += WebView2_WebMessageReceived;
            }
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
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
