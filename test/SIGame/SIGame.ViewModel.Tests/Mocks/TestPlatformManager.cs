using NSubstitute;
using SIGame.ViewModel.PlatformSpecific;
using System.Windows.Input;
using Utils.Commands;
using Utils.Timers;

namespace SIGame.ViewModel.Tests.Mocks;

/// <summary>
/// Test implementation of PlatformManager for testing purposes.
/// </summary>
internal sealed class TestPlatformManager : PlatformManager
{
    public override ICommand Close { get; } = new SimpleCommand(arg => { });

    public override double Volume => 0.5;

    public override void Activate(bool flash = true) { }

    public override bool Ask(string text) => true;

    public override string GetKeyName(int key) => key.ToString();

    public override void PlaySound(string? sound = null, double speed = 1, bool loop = false) { }

    public override string SelectColor() => "#FFFFFF";

    public override string SelectHumanAvatar() => "TestAvatar.png";

    public override string SelectLocalPackage(long? maxPackageSize) => "TestPackage.siq";

    public override string SelectLogsFolder(string initialFolder) => "/test/logs";

    public override string SelectSettingsForExport() => "/test/settings.xml";

    public override string SelectSettingsForImport() => "/test/settings.xml";

    public override string SelectMainBackground() => "background.jpg";

    public override string SelectStudiaBackground() => "studio_bg.jpg";

    public override string SelectLogo() => "logo.png";

    public override string SelectSound() => "sound.mp3";

    public override void SendErrorReport(Exception exc, bool isWarning = false) { }

    public override void ShowHelp(bool asDialog) { }

    public override void ShowMessage(string text, MessageType messageType, bool uiThread = false) { }

    public override IAnimatableTimer GetAnimatableTimer() => new TestAnimatableTimer();

    public override void ExecuteOnUIThread(Action action) => action();

    public override void ShowDialogWindow(object dataContext, Action onClose) { }

    public override void CloseDialogWindow() { }

    public override void UpdateVolume(double factor) { }
}
