using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SImulator.Implementation;
using SImulator.Implementation.RemoteBoard;
using SImulator.Properties;
using SImulator.ViewModel.Controllers;
using Utils.Web;

namespace SImulator.Views;

/// <summary>
/// Shows the game board in a native window (system WebKit) on the selected screen.
/// </summary>
internal sealed class BoardWindow : Window
{
    private const int Port = 8090;

    private readonly NativeWebView _webView = new();
    private readonly MacScreenDescriptor? _screen;
    private RemoteBoardServer? _server;
    private bool _canClose;

    public BoardWindow(MacScreenDescriptor? screen)
    {
        _screen = screen;

        Title = Properties.Resources.PresentationTitle;
        Width = 1280;
        Height = 720;
        Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0E, 0x2A));
        Content = _webView;

        if (screen?.Bounds is { } bounds)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = bounds.TopLeft;
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Closing += (_, e) => e.Cancel = !_canClose;
    }

    /// <summary>
    /// Starts the board bridge and shows the window.
    /// </summary>
    public async Task StartAsync(IWebInterop interop)
    {
        _server = await RemoteBoardServer.StartAsync(interop, Port);
        _webView.Source = new Uri(_server.Url);

        Show();

        if (_screen?.IsFullScreen == true)
        {
            WindowState = WindowState.FullScreen;
        }
    }

    /// <summary>
    /// Closes the window and stops the board bridge.
    /// </summary>
    public async Task StopAsync()
    {
        _canClose = true;
        Close();

        if (_server != null)
        {
            await _server.DisposeAsync();
            _server = null;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is WebPresentationController controller && controller.Stop.CanExecute(null))
        {
            controller.Stop.Execute(null);
            e.Handled = true;
            return;
        }

        e.Handled = KeyboardHub.OnKeyPressed(e.Key);
    }
}
