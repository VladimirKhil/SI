using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для AnswerView.xaml
    /// </summary>
    public partial class AnswerView : UserControl
    {
        public AnswerView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(input);
        }
    }
}
