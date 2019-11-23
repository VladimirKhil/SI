using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SIQuester.ViewModel;

namespace SIQuester
{
    /// <summary>
    /// Логика взаимодействия для PackageStoreWindow.xaml
    /// </summary>
    public partial class ImportSIStorageView : UserControl
    {
        public ImportSIStorageView()
        {
            InitializeComponent();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Import();
        }

        private void Import()
        {
            ((ImportSIStorageViewModel)DataContext).Select();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Import();
        }
    }
}
