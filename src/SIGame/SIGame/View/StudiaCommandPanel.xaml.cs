using SIGame.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SIGame;

/// <summary>
/// Provides interaction logic for StudiaCommandPanel.xaml.
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

    private void Game_Clicked()
    {
        if (gameButton.IsEnabled)
        {
            gameBorder.Visibility = Visibility.Visible;
            BeginStoryboard(_sb);
        }
    }

    private void Next_Clicked()
    {
        if (forward.IsEnabled)
        {
            forward.Visibility = Visibility.Visible;
            BeginStoryboard(_nextSB);
        }
    }

    private void Studia_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is GameViewModel oldGameViewModel)
        {
            oldGameViewModel.GameButtonPressed -= Game_Clicked;
            oldGameViewModel.NextButtonPressed -= Next_Clicked;
        }

        if (e.NewValue is GameViewModel gameViewModel)
        {
            gameViewModel.GameButtonPressed += Game_Clicked;
            gameViewModel.NextButtonPressed += Next_Clicked;
        }
    }

    public void OnMouseRightButtonDown()
    {
        var pressCmd = ((GameViewModel)DataContext).PressGameButton;

        if (pressCmd != null && pressCmd.CanBeExecuted)
        {
            pressCmd.Execute(null);
        }
    }
}
