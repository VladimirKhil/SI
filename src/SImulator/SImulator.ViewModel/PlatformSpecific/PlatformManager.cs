using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;

namespace SImulator.ViewModel.PlatformSpecific;

/// <summary>
/// Implements platform-specific logic.
/// </summary>
public abstract class PlatformManager
{
    public static PlatformManager Instance;

    public abstract ButtonManagers.ButtonManagerFactory ButtonManagerFactory { get; }

    protected PlatformManager()
    {
        Instance = this;
    }

    public abstract void CreatePlayersView(object dataContext);

    public abstract void ClosePlayersView();

    public abstract Task CreateMainViewAsync(object dataContext, int screenNumber);

    public abstract Task CloseMainViewAsync();

    public abstract IScreen[] GetScreens();

    public abstract string[] GetLocalComputers();

    public abstract string[] GetComPorts();

    public abstract bool IsEscapeKey(GameKey key);

    public abstract int GetKeyNumber(GameKey key);

    public abstract Task<IPackageSource?> AskSelectPackageAsync(object arg);

    public abstract string? AskSelectColor();

    public abstract Task<string?> AskSelectFileAsync(string header);

    public abstract string? AskSelectLogsFolder();

    public abstract Task<bool> AskStopGameAsync();

    public abstract void ShowMessage(string text, bool error = true);

    public abstract void NavigateToSite();

    public abstract void PlaySound(string name, Action onFinish);

    public abstract ILogger CreateLogger(string? folder);

    public abstract void ClearMedia();

    public abstract void InitSettings(AppSettings defaultSettings);
}
