using SIGame.ViewModel.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;
using Utils.Commands;

namespace SIGame.ViewModel;

/// <summary>
/// Defines a start menu view model.
/// </summary>
public sealed class StartMenuViewModel : INotifyPropertyChanged
{
    private const string TwitchUri = @"https://www.twitch.tv/directory/category/sigame";
    private const string DiscordUri = @"https://discord.gg/pGXTE37gpv";

    public UICommandCollection MainCommands { get; } = new();

    public HumanPlayerViewModel Human { get; }

    public ICommand SetProfile { get; }

    public ICommand NavigateToVK { get; private set; }

    public ICommand NavigateToTwitch { get; private set; }

    public ICommand NavigateToDiscord { get; private set; }

    private ICommand? _update;

    public ICommand? Update
    {
        get => _update;
        set
        {
            _update = value;
            OnPropertyChanged();
        }
    }

    public ICommand CancelUpdate { get; set; }

    public Version UpdateVersion { get; set; } = new();

    public string UpdateVersionMessage => string.Format(Resources.UpdateVersionMessage, UpdateVersion);

    public StartMenuViewModel(HumanPlayerViewModel human, ICommand setProfile)
    {
        Human = human;
        SetProfile = setProfile;

        NavigateToVK = new SimpleCommand(NavigateToVK_Executed)
        { 
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name == "ru-RU"
        };

        NavigateToTwitch = new SimpleCommand(NavigateToTwitch_Executed);
        
        NavigateToDiscord = new SimpleCommand(NavigateToDiscord_Executed)
        {
            CanBeExecuted = Thread.CurrentThread.CurrentUICulture.Name != "ru-RU"
        };

        CancelUpdate = new SimpleCommand(obj => Update = null);
    }

    private void NavigateToVK_Executed(object? arg)
    {
        try
        {
            Browser.Open(Resources.GroupLink);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", Resources.GroupLink, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToTwitch_Executed(object? arg)
    {
        try
        {
            Browser.Open(TwitchUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", TwitchUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void NavigateToDiscord_Executed(object? arg)
    {
        try
        {
            Browser.Open(DiscordUri);
        }
        catch (Exception exc)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(
                string.Format(Resources.SiteNavigationError + "\r\n{1}", DiscordUri, exc.Message),
                PlatformSpecific.MessageType.Error);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
