using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для GameReportView.xaml
    /// </summary>
    public partial class GameReportView : UserControl
    {
        public GameReportView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(tbComments);
        }
    }
}
