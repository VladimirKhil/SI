using SIGame.ViewModel;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace SIGame;

/// <summary>
/// Provides interaction logic for MainView.xaml.
/// </summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Dispatcher.CurrentDispatcher.ShutdownStarted += CurrentDispatcher_ShutdownStarted;
    }

    private void CurrentDispatcher_ShutdownStarted(object? sender, EventArgs e)
    {
        var main = (MainViewModel)DataContext;

        if (main != null)
        {
            main.ActiveView = null;
        }
    }

    private void Body_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ((MainViewModel)DataContext).IsSlideMenuOpen = false;
    }
}
