using SI.GameServer.Client;
using System.Windows.Input;

namespace SIGame.ViewModel.PlatformSpecific;

public abstract class PlatformManager : IUIThreadExecutor
{
    public static PlatformManager Instance;

    public IServiceProvider? ServiceProvider { get; set; }

    protected PlatformManager()
    {
        Instance = this;
    }

    public abstract void ShowMessage(string text, MessageType messageType, bool uiThread = false);

    public abstract bool Ask(string text);

    public abstract void ShowHelp(bool asDialog);

    public abstract string? SelectColor();

    public abstract string? SelectLogsFolder(string initialFolder);

    public abstract string? SelectHumanAvatar();

    /// <summary>
    /// Selects local game package.
    /// </summary>
    /// <param name="maxPackageSize">Maximum allowed package size.</param>
    public abstract string? SelectLocalPackage(long? maxPackageSize);

    public abstract string SelectSettingsForExport();

    public abstract string SelectSettingsForImport();

    public abstract string SelectStudiaBackground();

    public abstract string SelectMainBackground();

    public abstract string SelectLogo();

    public abstract string SelectSound();

    public abstract void Activate();

    public abstract void PlaySound(string? sound = null, double speed = 1.0, bool loop = false);

    public abstract void SendErrorReport(Exception exc, bool isWarning = false);

    public abstract string GetKeyName(int key);

    public abstract void ExecuteOnUIThread(Action action);

    public abstract ICommand Close { get; }

    public abstract IAnimatableTimer GetAnimatableTimer();

    public abstract void ShowDialogWindow(object dataContext, Action onClose);

    public abstract void CloseDialogWindow();
}
