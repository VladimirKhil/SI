using System;
using System.Windows.Input;

namespace SIGame.ViewModel.PlatformSpecific
{
    public abstract class PlatformManager
    {
        public static PlatformManager Instance;

        protected PlatformManager()
        {
            Instance = this;
        }

        public abstract void ShowMessage(string text, MessageType messageType, bool uiThread = false);
        public abstract bool Ask(string text);

        public abstract void ShowHelp(bool asDialog);
        public abstract string SelectColor();

        public abstract string SelectLogsFolder(string initialFolder);
        public abstract string SelectHumanAvatar();
        public abstract string SelectLocalPackage();
        public abstract string SelectSettingsForExport();
        public abstract string SelectSettingsForImport();
        public abstract string SelectStudiaBackground();
        public abstract string SelectMainBackground();
        public abstract string SelectLogo();
        public abstract string SelectSound();

        public abstract void Activate();

        public abstract void PlaySound(string sound = null, double speed = 1.0, bool loop = false);

        public abstract void SendErrorReport(Exception exc, bool isWarning = false);

        public abstract string GetKeyName(int key);

        public abstract Action ExecuteOnUIThread(Action action);
        public abstract Action<T> ExecuteOnUIThread<T>(Action<T> action);
        public abstract Action<T1, T2> ExecuteOnUIThread<T1, T2>(Action<T1, T2> action);

        public abstract ICommand Close { get; }

        public abstract IAnimatableTimer GetAnimatableTimer();
    }
}
