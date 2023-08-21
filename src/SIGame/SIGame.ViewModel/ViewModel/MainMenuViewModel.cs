using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Defines application main menu view model.
/// </summary>
public sealed class MainMenuViewModel : ViewModel<UserSettings>
{
    public ICommand OpenLogs => GameCommands.OpenLogs;
    public ICommand Comment => GameCommands.Comment;
    public ICommand Donate => GameCommands.Donate;
    public ICommand Help => GameCommands.Help;

    private bool _isVisible = false;

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    public string[] Languages { get; } = new string[] { "ru-RU", "en-US" };

    public MainMenuViewModel(UserSettings settings)
        : base(settings)
    {
        
    }
}
