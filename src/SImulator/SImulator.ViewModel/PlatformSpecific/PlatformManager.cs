using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Controllers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using Utils.Timers;

namespace SImulator.ViewModel.PlatformSpecific;

/// <summary>
/// Implements platform-specific logic.
/// </summary>
public abstract class PlatformManager
{
    public static PlatformManager Instance;

    /// <summary>
    /// Creates a presentation controller for the given screen.
    /// Override in tests to return a test implementation that does not require a browser.
    /// </summary>
    public virtual IPresentationController CreatePresentationController(
        IDisplayDescriptor displayDescriptor,
        IPresentationListener presentationListener,
        SoundsSettings soundsSettings,
        bool sendCommonMessages)
        => new WebPresentationController(displayDescriptor, presentationListener, soundsSettings, sendCommonMessages);

    protected PlatformManager()
    {
        Instance = this;
    }

    public abstract void CreatePlayersView(object dataContext);

    public abstract void ClosePlayersView();

    public abstract Task CreateMainViewAsync(object dataContext, IDisplayDescriptor screen);

    public abstract Task CloseMainViewAsync();

    public abstract IDisplayDescriptor[] GetScreens();

    public abstract string[] GetLocalComputers();

    public abstract bool IsEscapeKey(GameKey key);

    public abstract int GetKeyNumber(GameKey key);

    public abstract Task<IPackageSource?> AskSelectPackageAsync(string arg);

    public abstract Task<string?> AskSelectFileAsync(string header);

    public abstract string? AskSelectLogsFolder();

    public abstract Task<bool> AskStopGameAsync();

    public abstract IGameLogger CreateGameLogger(string? folder);

    public abstract IAnimatableTimer CreateAnimatableTimer();
}
