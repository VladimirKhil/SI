using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Provides interaction logic for AnswerView.xaml.
/// </summary>
public partial class AnswerView : UserControl
{
    public AnswerView() => InitializeComponent();

    private void UserControl_Loaded(object sender, RoutedEventArgs e) => Keyboard.Focus(input);

    private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Command == ApplicationCommands.Copy ||
            e.Command == ApplicationCommands.Cut ||
            e.Command == ApplicationCommands.Paste)
        {
            e.Handled = true;
        }
    }
}
