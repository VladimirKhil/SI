using SICore;
using SIGame.ViewModel.PlatformSpecific;
using System.Windows.Input;

namespace SIGame.Tests;

internal sealed class TestManager : PlatformManager
{
	public override ICommand Close { get; } = new CustomCommand((arg) => { });

	public override void Activate() { }

	public override bool Ask(string text) => throw new NotImplementedException();

	public override string GetKeyName(int key) => key.ToString();

	public override void PlaySound(string? sound = null, double speed = 1, bool loop = false)
	{
		
	}

	public override string SelectColor() => throw new NotImplementedException();

	public override string SelectHumanAvatar() => throw new NotImplementedException();

	public override string SelectLocalPackage(long? maxPackageSize) => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SIGameTest.siq");

	public override string SelectLogsFolder(string initialFolder) => throw new NotImplementedException();

	public override string SelectSettingsForExport()
	{
		throw new NotImplementedException();
	}

	public override string SelectSettingsForImport()
	{
		throw new NotImplementedException();
	}

	public override string SelectMainBackground()
	{
		throw new NotImplementedException();
	}

	public override string SelectStudiaBackground()
	{
		throw new NotImplementedException();
	}

	public override string SelectLogo()
	{
		throw new NotImplementedException();
	}

	public override string SelectSound()
	{
		throw new NotImplementedException();
	}

	public override void SendErrorReport(Exception exc, bool isWarning = false)
	{
		throw new NotImplementedException();
	}

	public override void ShowHelp(bool asDialog)
	{
		throw new NotImplementedException();
	}

	public override void ShowMessage(string text, MessageType messageType, bool uiThread = false)
	{
		throw new NotImplementedException();
	}

	public override IAnimatableTimer GetAnimatableTimer()
	{
		return new AnimatableTimerMock();
	}

	public override void ExecuteOnUIThread(Action action)
	{
		action();
	}

        public override void ShowDialogWindow(object dataContext, Action onClose)
        {
            throw new NotImplementedException();
        }

        public override void CloseDialogWindow()
        {
            throw new NotImplementedException();
        }
    }
