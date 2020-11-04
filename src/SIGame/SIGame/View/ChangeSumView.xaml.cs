using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для ChangeSumView.xaml
    /// </summary>
    public partial class ChangeSumView : UserControl
    {
        public ChangeSumView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {            
            Keyboard.Focus(tbSum);
            await Task.Delay(100);
            tbSum.SelectAll();
        }
    }
}
