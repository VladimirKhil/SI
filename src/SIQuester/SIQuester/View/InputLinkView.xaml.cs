using System.Windows;

namespace SIQuester;

/// <summary>
/// Interaction logic for InputLinkView.xaml
/// </summary>
public partial class InputLinkView : Window
{
    public InputLinkView()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
