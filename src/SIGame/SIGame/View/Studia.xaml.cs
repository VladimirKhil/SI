using SICore;
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

    private void Table_MediaEnded(object sender, RoutedEventArgs e)
    {
        var logic = ((GameViewModel)DataContext).Host.MyLogic;
        if (logic == null)
        {
            return;
        }

        var viewed = ((ViewerData)logic.Data).AtomViewed;
        if (viewed != null && viewed.CanExecute(null))
        {
            viewed.Execute(null);
        }
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        => studiaCommandPanel.OnMouseRightButtonDown();
}
