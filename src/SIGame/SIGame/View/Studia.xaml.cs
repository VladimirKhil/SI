using SIGame.ViewModel;
using SIUI.Behaviors;
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
        var mediaElement = (MediaElement)e.OriginalSource;

        var contentType = MediaController.GetContentType(mediaElement);
        var contentValue = MediaController.GetContentValue(mediaElement);

        ((GameViewModel)DataContext).OnMediaContentCompleted(contentType, contentValue);
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e) =>
        studiaCommandPanel.OnMouseRightButtonDown();
}
