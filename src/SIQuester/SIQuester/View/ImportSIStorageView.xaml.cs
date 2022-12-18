using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIQuester;

/// <summary>
/// Defines interaction logic for ImportSIStorageView.
/// </summary>
public partial class ImportSIStorageView : UserControl
{
    public ImportSIStorageView() => InitializeComponent();

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => Import();

    private void Button_Click(object sender, RoutedEventArgs e) => Import();

    private void Import() => ((ImportSIStorageViewModel)DataContext).Select();
}
