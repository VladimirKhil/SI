using SImulator.Implementation;
using SImulator.Implementation.RemoteBoard;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Utils.Web;

namespace SImulator;

/// <summary>
/// Controls the game board shown in an external browser instead of the embedded WebView2.
/// </summary>
/// <remarks>
/// Used for <see cref="BrowserDisplayDescriptor" /> screen.
/// </remarks>
internal sealed class RemoteBoardWindow : Window
{
    private const int Port = 8090;

    private RemoteBoardServer? _server;
    private readonly TextBlock _status;

    public RemoteBoardWindow()
    {
        Title = Properties.Resources.PresentationTitle;
        Width = 520;
        Height = 220;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0E, 0x4A));

        _status = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16),
            Text = Properties.Resources.BrowserBoardStarting,
        };

        var openButton = new Button { Content = Properties.Resources.BrowserBoardOpen, Padding = new Thickness(12, 6, 12, 6), HorizontalAlignment = HorizontalAlignment.Left };
        openButton.Click += (_, _) => OpenBrowser();

        Content = new StackPanel { Margin = new Thickness(20), Children = { _status, openButton } };

        var stopBinding = new Binding("Stop");
        var escape = new KeyBinding { Key = Key.Escape };
        BindingOperations.SetBinding(escape, InputBinding.CommandProperty, stopBinding);
        InputBindings.Add(escape);

        Loaded += OnLoaded;
        Closing += (_, e) => e.Cancel = !DesktopManager.CanCloseMainView;
        Closed += async (_, _) =>
        {
            if (_server != null)
            {
                await _server.DisposeAsync();
            }
        };
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not IWebInterop interop)
        {
            _status.Text = string.Format(Properties.Resources.BrowserBoardError, nameof(IWebInterop));
            return;
        }

        try
        {
            _server = await RemoteBoardServer.StartAsync(interop, Port);
            _status.Text = string.Format(Properties.Resources.BrowserBoardOpened, _server.Url);
            OpenBrowser();
        }
        catch (Exception exc)
        {
            _status.Text = string.Format(Properties.Resources.BrowserBoardError, exc.Message);
        }
    }

    private void OpenBrowser()
    {
        if (_server == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(_server.Url) { UseShellExecute = true });
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
        }
    }
}
