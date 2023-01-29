using SICore;
using SIGame.ViewModel.Properties;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils;

namespace SIGame.ViewModel;

public sealed class StartMenuViewModel : INotifyPropertyChanged
{
    public UICommandCollection MainCommands { get; } = new UICommandCollection();

    public HumanPlayerViewModel Human { get; set; }

    public ICommand SetProfile { get; set; }

    public ICommand NavigateToVK { get; private set; }

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

    public Version UpdateVersion { get; set; }

    public string UpdateVersionMessage => string.Format(Resources.UpdateVersionMessage, UpdateVersion);

    public StartMenuViewModel()
    {
        NavigateToVK = new CustomCommand(NavigateToVK_Executed);

        CancelUpdate = new CustomCommand(obj => Update = null);
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public event PropertyChangedEventHandler? PropertyChanged;
}
