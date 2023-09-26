using SIQuester.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace SIQuester;

/// <summary>
/// Provides interaction logic for NewView.xaml.
/// </summary>
public partial class NewView : UserControl
{
    public NewView() => InitializeComponent();

    private void ListView_DoubleClick(object sender, RoutedEventArgs e) => ((NewViewModel)DataContext)?.Create.Execute(null);
}
