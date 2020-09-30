using SIEngine;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SIPackages.Core;
using System;
using System.Threading.Tasks;

namespace SImulator.ViewModel.PlatformSpecific
{
    /// <summary>
    /// Логика, различающаяся на разных платформах
    /// </summary>
    public abstract class PlatformManager
    {
        internal static PlatformManager Instance;

        public abstract ButtonManagers.ButtonManagerFactory ButtonManagerFactory { get; }

        protected PlatformManager()
        {
            Instance = this;
        }

        public abstract void CreatePlayersView(object dataContext);
        public abstract void ClosePlayersView();

        public abstract Task CreateMainView(object dataContext, int screenNumber);
        public abstract Task CloseMainView();

        public abstract IScreen[] GetScreens();
        public abstract string[] GetLocalComputers();
        public abstract string[] GetComPorts();

        public abstract bool IsEscapeKey(GameKey key);
        public abstract int GetKeyNumber(GameKey key);

        public abstract Task<IPackageSource> AskSelectPackage(object arg);
        public abstract string AskSelectColor();
        public abstract Task<string> AskSelectFile(string header);
        public abstract string AskSelectLogsFolder();
        public abstract Task<bool> AskStopGame();

        public abstract void ShowMessage(string text, bool error = true);
        public abstract void NavigateToSite();

        public abstract void PlaySound(string name, Action onFinish);
        public abstract ILogger CreateLogger(string folder);

        public abstract IExtendedGameHost CreateGameHost(EngineBase engine);

        public abstract void CreateServer(Type contract, int port, int screenIndex);
        public abstract void CloseServer();

        public abstract Task<IMedia> PrepareMedia(IMedia media);
        public abstract void ClearMedia();

        public abstract T GetCallback<T>();

        public abstract void InitSettings(AppSettings defaultSettings);
    }
}
