using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame
{
    /// <summary>
    /// Interaction logic for ComputerAccountView.xaml
    /// </summary>
    public partial class ComputerAccountView : UserControl
    {
        public ComputerAccountView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(tbName);
        }
    }
}
