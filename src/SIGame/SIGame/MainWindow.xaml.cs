using SIGame.ViewModel;
using SIWindows.WinAPI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Provides interaction logic for MainWindow.xaml.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        UserSettings.Default.PropertyChanged += Default_PropertyChanged;
    }

    private void Default_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(UserSettings.FullScreen))
        {
            return;
        }
        
        if (UserSettings.Default.FullScreen)
        {
            Maximize();
        }
        else
        {
            Minimize();
        }
    }

    private void Minimize()
    {
        Visibility = Visibility.Collapsed;
        WindowStyle = WindowStyle.SingleBorderWindow;
        ResizeMode = ResizeMode.CanResize;
        Visibility = Visibility.Visible;
    }

    internal void Maximize()
    {
        Visibility = Visibility.Collapsed;
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Visibility = Visibility.Visible;
    }

    private async void Close_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        await Task.Delay(500);
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var appSettings = UserSettings.Default.GameSettings.AppSettings;
        var key = (int)e.Key;

        if (key == appSettings.GameButtonKey2)
        {
            e.Handled = OnGameButtonKeyPressed(e.Key);
        }
        else if (key == appSettings.MoveNextKey)
        {
            e.Handled = OnMoveNextKeyPressed();
        }
    }

    private bool OnMoveNextKeyPressed()
    {
        if (((MainViewModel)DataContext).ActiveView is not GameViewModel game)
        {
            return false;
        }

        var appSettings = UserSettings.Default.GameSettings.AppSettings;

        if (appSettings.BindNextButton && game.Move.CanBeExecuted)
        {
            game.Move.Execute(1);
            return true;
        }

        return false;
    }

    private bool OnGameButtonKeyPressed(Key key)
    {
        if (key == Key.Space || key == Key.Back || key == Key.Delete || key >= Key.A && key <= Key.Z || key >= Key.D0 && key <= Key.D9)
        {
            if (Keyboard.FocusedElement is TextBox)
            {
                return false;
            }
        }

        if (((MainViewModel)DataContext).ActiveView is not GameViewModel game)
        {
            return false;
        }

        var data = game.Data;

        if (data.IsPlayer)
        {
            data.PlayerDataExtensions.OnPressButton();

            if (data.PlayerDataExtensions.PressGameButton != null && data.PlayerDataExtensions.PressGameButton.CanExecute(null))
            {
                data.PlayerDataExtensions.PressGameButton.Execute(null);
                return true;
            }
        }

        return false;
    }

    public void FlashIfNeeded(bool flash) => Dispatcher.BeginInvoke(() => FlashCore(flash));

    private void FlashCore(bool flash)
    {
        if (IsActive)
        {
            return;
        }

        if (flash)
        {
            Flasher.Flash(this);
        }
        else
        {
            Flasher.Stop(this);
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
