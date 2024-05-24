using System.Windows;

namespace Utils.Wpf.Views;

/// <summary>
/// Provides interaction logic for InputTextWindow.xaml.
/// </summary>
public partial class InputTextWindow : Window
{
    public InputTextWindow() => InitializeComponent();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
