using SIQuester.ViewModel;
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

    private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter || DataContext is not SelectTagsViewModel viewModel || !viewModel.AddItem.CanBeExecuted)
        {
            return;
        }

        viewModel.AddItem.Execute(null);
    }

    private void Button_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
