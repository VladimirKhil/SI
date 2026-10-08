using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SImulator.Implementation;
using SImulator.ViewModel;
using SImulator.ViewModel.Core;

namespace SImulator.Views;

/// <summary>
/// SImulator control (moderator) window.
/// </summary>
public partial class MainWindow : Window
{
    private bool _closeConfirmed;

    public MainWindow()
    {
        InitializeComponent();

        var version = typeof(MainWindow).Assembly.GetName().Version;
        Title = $"SImulator {version?.ToString(3)} (macOS preview)";
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Closing += OnClosing;
    }

    private void MediaControl_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string action } && Avalonia.Application.Current is App app)
        {
            app.PlatformManager.ControlMedia(action);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainViewModel main && main.OnKeyboardPressed((GameKey)e.Key))
        {
            e.Handled = true;
            return;
        }

        if (e.Source is not TextBox)
        {
            e.Handled = KeyboardHub.OnKeyPressed(e.Key);
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || DataContext is not MainViewModel main)
        {
            return;
        }

        e.Cancel = true;

        if (await main.RaiseStop())
        {
            _closeConfirmed = true;
            Close();
        }
    }
}
