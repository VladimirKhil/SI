using SICore;
using SIGame.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SIGame;

/// <summary>
/// Логика взаимодействия для StudiaCommandPanel.xaml
/// </summary>
public partial class StudiaCommandPanel : UserControl
{
    private readonly Storyboard _sb;
    private readonly Storyboard _nextSB;

    public StudiaCommandPanel()
    {
        InitializeComponent();

        _sb = (Storyboard)Resources["gameSB"];
        _sb.Completed += Sb_Completed;
        _nextSB = (Storyboard)Resources["nextSB"];

        DataContextChanged += Studia_DataContextChanged;
    }

    private void Sb_Completed(object? sender, EventArgs e)
    {
        gameBorder.Visibility = Visibility.Hidden;
    }

    private void RaiseButtonClick()
    {
        if (gameButton.IsEnabled)
        {
            gameBorder.Visibility = Visibility.Visible;
            BeginStoryboard(_sb);
        }
    }

    private void RaiseNextClick()
    {
        if (forward.IsEnabled)
        {
            forward.Visibility = Visibility.Visible;
            BeginStoryboard(_nextSB);
        }
    }

    private void Studia_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not GameViewModel game)
        {
            return;
        }

        if (game.Host.MyLogic is IViewerLogic logic)
        {
            ((ViewerData)logic.Data).PlayerDataExtensions.PressButton += RaiseButtonClick;
            ((ViewerData)logic.Data).PlayerDataExtensions.PressNextButton += RaiseNextClick;
        }
    }

    public void OnMouseRightButtonDown()
    {
        if (((GameViewModel)DataContext)?.Host?.MyLogic is not IViewerLogic logic)
        {
            return;
        }

        var pressCmd = ((ViewerData)logic.Data).PlayerDataExtensions.PressGameButton;

        if (pressCmd != null && pressCmd.CanBeExecuted)
        {
            RaiseButtonClick();
            pressCmd.Execute(null);
        }
    }
}
