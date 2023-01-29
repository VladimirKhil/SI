using SIGame.ViewModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Логика взаимодействия для SIStorageView.xaml
/// </summary>
public partial class SIStorageView : UserControl
{
    public SIStorageView()
    {
        InitializeComponent();
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var item = ((Selector)sender).SelectedItem;

        if (item == null)
        {
            return;
        }

        var cmd = ((SIStorageViewModel)DataContext).LoadStorePackage;

        if (cmd != null && cmd.CanExecute(item))
        {
            cmd.ExecuteAsync(item);
        }
    }
}
