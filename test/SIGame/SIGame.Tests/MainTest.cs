using NUnit.Framework;
using SICore;
using SICore.Connections;
using SICore.PlatformSpecific;
using SIGame.ViewModel;
using SIGame.ViewModel.PackageSources;
using SIUI.ViewModel;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIGame.Tests
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    public class MainTest
    {
        private static readonly HttpClient HttpClient = new();

        [TestCase(PackageSourceTypes.RandomServer, GameRole.Player, true)]
        [TestCase(PackageSourceTypes.SIStorage, GameRole.Player, true)]
        [TestCase(PackageSourceTypes.Local, GameRole.Player, true)]
        [TestCase(PackageSourceTypes.RandomServer, GameRole.Viewer, true)]
        [TestCase(PackageSourceTypes.RandomServer, GameRole.Showman, true)]
        [TestCase(PackageSourceTypes.RandomServer, GameRole.Player, false)]
        public async Task GameCreateAndRun_Ok_Async(
            PackageSourceTypes packageSourceType,
            GameRole gameRole,
            bool useSignalRConnection)
        {
            const string GameServerUri = "https://sionline.ru/siserver/1";

            var coreManager = new DesktopCoreManager();
            var manager = new TestManager();

            var commonSettings = new CommonSettings();
            commonSettings.Humans2.Add(new HumanAccount { Name = "test_" + new Random().Next(10000), BirthDate = DateTime.Now });

            var userSettings = new UserSettings
            {
                GameServerUri = GameServerUri,
                UseSignalRConnection = useSignalRConnection
            };

            var mainViewModel = new MainViewModel(commonSettings, userSettings);

            await mainViewModel.Open.ExecuteAsync(null);

            var contentBox = mainViewModel.ActiveView as ContentBox;
            Assert.IsNull(contentBox, ((LoginViewModel)contentBox?.Data)?.Error);

            var siOnline = (SIOnlineViewModel)mainViewModel.ActiveView;

            Assert.IsNotNull(siOnline);

            await siOnline.InitAsync();

            siOnline.NewGame.Execute(null);

            var gameSettings = (GameSettingsViewModel)siOnline.Content.Content.Data;
            gameSettings.NetworkGameName = "testGame" + new Random().Next(10000);
            gameSettings.NetworkGamePassword = "testpass";

            gameSettings.Role = gameRole;

            gameSettings.SelectPackage.Execute(packageSourceType);

            if (packageSourceType == PackageSourceTypes.SIStorage)
            {
                var counter = 10;

                while (gameSettings.StorageInfo.Model.Packages == null && counter > 0)
                {
                    counter--;
                    await Task.Delay(1000);
                }

                if (gameSettings.StorageInfo.Model.Packages == null)
                {
                    Assert.Fail("Could not load storage packages");
                }

                var package = gameSettings.StorageInfo.Model.Packages.FirstOrDefault(p => p.ID == 300);

                Assert.IsNotNull(package);

                var storage = gameSettings.StorageInfo.Model.CurrentPackage = package;

                await gameSettings.StorageInfo.LoadStorePackage.ExecuteAsync(null);
            }

            await gameSettings.BeginGame.ExecuteAsync(null);

            Assert.IsFalse(gameSettings.IsProgress);
            Assert.IsNull(gameSettings.ErrorMessage);

            var siOnlineError = mainViewModel.ActiveView as SIOnlineViewModel;
            Assert.IsNull(siOnlineError, siOnlineError?.Error);

            var game = (GameViewModel)mainViewModel.ActiveView;

            var tInfo = ((ViewerHumanLogic)game.Host.MyLogic).TInfo;
            tInfo.PropertyChanged += TInfo_PropertyChanged;

            await Task.Delay(5000);
            if (gameRole != GameRole.Viewer)
            {
                ((PersonAccount)game.Data.Me).BeReadyCommand.Execute(null);
            }

            if (packageSourceType == PackageSourceTypes.Local) // One mode check is enough
            {
                var host = game.Host;

                await Task.Delay(50000);
            }

            game.EndGame.Execute(null);

            await Task.Delay(5000);

            // Sometimes it does not have time to login again

            var contentBox2 = mainViewModel.ActiveView as ContentBox;
            Assert.IsNull(contentBox2, ((LoginViewModel)contentBox2?.Data)?.Error);

            var siOnline2 = (SIOnlineViewModel)mainViewModel.ActiveView;
            siOnline2.Cancel.Execute(null);
        }

        private static async void TInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TableInfoViewModel.MediaSource))
            {
                var mediaSource = ((TableInfoViewModel)sender).MediaSource;

                if (mediaSource != null)
                {
                    var result = await HttpClient.GetAsync(mediaSource.Uri);
                    Assert.IsTrue(result.IsSuccessStatusCode);
                }
            }
            else if (e.PropertyName == nameof(TableInfoViewModel.SoundSource))
            {
                await HttpClient.GetAsync(((TableInfoViewModel)sender).SoundSource.Uri);
            }
        }
    }
}
