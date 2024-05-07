using System.Windows;

namespace SImulator;

/// <summary>
/// Supports interaction logic for WebWindow.xaml.
/// </summary>
public partial class WebWindow : Window
{
    public WebWindow() => InitializeComponent();

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = !MainWindow.CanClose;
    }
}
