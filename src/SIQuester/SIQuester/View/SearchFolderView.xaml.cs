using SIQuester.ViewModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester;

/// <summary>
/// Логика взаимодействия для SearchView.xaml
/// </summary>
public partial class SearchFolderView : UserControl
{
    public SearchFolderView() => InitializeComponent();

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var item = ((ListBox)sender).SelectedItem;

        if (item != null)
        {
            ((SearchFolderViewModel)DataContext).Open.Execute(item);
        }
    }
}
