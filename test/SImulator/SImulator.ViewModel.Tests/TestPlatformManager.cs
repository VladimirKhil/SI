using NUnit.Framework;
using SIEngine;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SImulator.ViewModel.Tests
{
    internal sealed class TestPlatformManager : PlatformManager
    {
        public override ButtonManagerFactory ButtonManagerFactory { get; } = new TestButtonManager();

        public override string AskSelectColor()
        {
            throw new NotImplementedException();
        }

        public override Task<string> AskSelectFileAsync(string header)
        {
            throw new NotImplementedException();
        }

        public override string AskSelectLogsFolder()
        {
            throw new NotImplementedException();
        }

        public override Task<IPackageSource> AskSelectPackageAsync(object arg)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> AskStopGameAsync()
        {
            throw new NotImplementedException();
        }

        public override void ClearMedia()
        {
            throw new NotImplementedException();
        }

        public override Task CloseMainViewAsync()
        {
            throw new NotImplementedException();
        }

        public override void ClosePlayersView()
        {
            
        }

        public override IExtendedGameHost CreateGameHost(EngineBase engine)
        {
            return new TestGameHost(engine);
        }

        public override ILogger CreateLogger(string folder) => new TestLogger();

        public override Task CreateMainViewAsync(object dataContext, int screenNumber) => Task.CompletedTask;

        public override void CreatePlayersView(object dataContext)
        {
            throw new NotImplementedException();
        }

#if LEGACY
        public override void CreateServer(Type contract, int port, int screenIndex)
        {
            throw new NotImplementedException();
        }

        public override void CloseServer()
        {
            throw new NotImplementedException();
        }

        public override T GetCallback<T>()
        {
            throw new NotImplementedException();
        }
#endif

        public override string[] GetComPorts()
        {
            throw new NotImplementedException();
        }

        public override int GetKeyNumber(GameKey key)
        {
            throw new NotImplementedException();
        }

        public override string[] GetLocalComputers()
        {
            throw new NotImplementedException();
        }

        public override IScreen[] GetScreens() => new IScreen[] { new TestScreen() };

        public override void InitSettings(AppSettings defaultSettings)
        {
            throw new NotImplementedException();
        }

        public override bool IsEscapeKey(GameKey key)
        {
            throw new NotImplementedException();
        }

        public override void NavigateToSite()
        {
            throw new NotImplementedException();
        }

        public override void PlaySound(string name, Action onFinish)
        {
            
        }

        public override Task<IMedia> PrepareMediaAsync(IMedia media, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override void ShowMessage(string text, bool error = true) => Assert.Fail(text);
    }
}
