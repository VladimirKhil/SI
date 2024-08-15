using SIGame.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SIGame;

/// <summary>
/// Defines game room.
/// </summary>
public partial class Studia : UserControl
{
    public Studia() => InitializeComponent();

    // TODO: can Table call the command directly?
    private void Table_MediaEnded(object sender, RoutedEventArgs e)
    {
        var viewed = ((GameViewModel)DataContext).AtomViewed;
        
        if (viewed != null && viewed.CanExecute(null))
        {
            viewed.Execute(null);
        }
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) =>
        studiaCommandPanel.OnMouseRightButtonDown();
}
