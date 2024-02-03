using System.Windows;

namespace SIQuester.View;

/// <summary>
/// Defines interaction logic for SelectTagsView.xaml.
/// </summary>
public partial class SelectTagsView : Window
{
    public SelectTagsView() => InitializeComponent();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
