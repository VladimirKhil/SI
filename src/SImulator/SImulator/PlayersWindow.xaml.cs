using System.Windows;

namespace SImulator;

/// <summary>
/// Provides interaction logic for  PlayersWindow.xaml.
/// </summary>
public partial class PlayersWindow : Window
{
    public bool CanClose { get; set; } = false;

    public PlayersWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = !CanClose;
    }
}
