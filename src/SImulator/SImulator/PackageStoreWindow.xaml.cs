using System.Windows;
using System.Windows.Input;

namespace SImulator;

/// <summary>
/// Provides interaction logic for PackageStoreWindow.xaml.
/// </summary>
public partial class PackageStoreWindow : Window
{
    public PackageStoreWindow()
    {
        InitializeComponent();
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
