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
    private PixelRect? _fullScreenBounds;

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
            // Borderless window covering the screen (not the macOS full screen mode with a separate Space),
            // so the control window and dialogs stay available on the same display
            WindowStartupLocation = WindowStartupLocation.Manual;
            WindowDecorations = WindowDecorations.None;
            CanResize = false;
            Position = bounds.TopLeft;
            _fullScreenBounds = bounds;
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
        _server = await RemoteBoardServer.StartAsync(new BoardKeysInterop(interop, OnBoardKeyPressed), Port);
        _webView.Source = new Uri(_server.Url);

        Show();

        if (_fullScreenBounds is { } bounds)
        {
            Position = bounds.TopLeft;
            var scaling = _screen?.Scaling ?? 1.0;
            Width = bounds.Width / scaling;
            Height = bounds.Height / scaling;
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

    /// <summary>
    /// Processes a key pressed inside the board page (WebKit consumes keyboard events of the window).
    /// </summary>
    private void OnBoardKeyPressed(string key)
    {
        if (key == "Escape")
        {
            if (DataContext is WebPresentationController controller && controller.Stop.CanExecute(null))
            {
                controller.Stop.Execute(null);
            }

            return;
        }

        if (key.Length == 1 && char.IsAsciiDigit(key[0]) && key[0] != '0')
        {
            KeyboardHub.OnKeyPressed(Key.D1 + (key[0] - '1'));
        }
    }

    /// <summary>
    /// Passes board messages to the presentation controller and reports pressed keys.
    /// </summary>
    private sealed class BoardKeysInterop(IWebInterop inner, Action<string> onKeyPressed) : IWebInterop
    {
        public event Action<string>? SendJsonMessage
        {
            add => inner.SendJsonMessage += value;
            remove => inner.SendJsonMessage -= value;
        }

        public void OnMessage(string webMessageAsJson)
        {
            inner.OnMessage(webMessageAsJson);

            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(webMessageAsJson);
                var root = document.RootElement;

                if (root.TryGetProperty("type", out var type) && type.GetString() == "keyPressed"
                    && root.TryGetProperty("key", out var key) && key.GetString() is { } keyName)
                {
                    onKeyPressed(keyName);
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }
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
