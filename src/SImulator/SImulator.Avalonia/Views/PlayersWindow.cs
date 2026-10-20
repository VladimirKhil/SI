using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SImulator.Implementation;
using SImulator.Properties;

namespace SImulator.Views;

/// <summary>
/// Separate window with players list.
/// </summary>
internal sealed class PlayersWindow : Window
{
    internal bool CanClose { get; set; }

    public PlayersWindow()
    {
        Title = Properties.Resources.Players;
        Width = 760;
        Height = 360;
        Content = new PlayersTable { Margin = new Thickness(8) };
        Closing += (_, e) => e.Cancel = !CanClose;
        AddHandler(KeyDownEvent, (_, e) => e.Handled = KeyboardHub.OnKeyPressed(e.Key), RoutingStrategies.Tunnel);
    }
}
