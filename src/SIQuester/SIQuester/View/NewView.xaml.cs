using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SIQuester
{
    /// <summary>
    /// Логика взаимодействия для NewView.xaml
    /// </summary>
    public partial class NewView : UserControl
    {
        public NewView()
        {
            InitializeComponent();
        }

        private void ListView_DoubleClick(object sender, RoutedEventArgs e)
        {
            ((SIQuester.ViewModel.NewViewModel)DataContext).Create.Execute(null);
        }
    }
}
