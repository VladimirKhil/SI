using NUnit.Framework;
using SICore;
using SICore.PlatformSpecific;
using SIGame.ViewModel;
using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.ViewModel.Data;
using SIUI.ViewModel;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIGame.Tests
{
	[TestFixture]
	public class MainTest
	{
		private static readonly HttpClient HttpClient = new HttpClient();

		[TestCase(PackageSourceTypes.RandomServer)]
		[TestCase(PackageSourceTypes.SIStorage)]
		[TestCase(PackageSourceTypes.Local)]
		public async Task RunGame(PackageSourceTypes packageSourceType)
		{
			GameServerClientNew.GameServerPredefinedUri = "http://vladimirkhil.com:2301";// "http://127.0.0.1:8088";

			var coreManager = new DesktopCoreManager();
			var manager = new TestManager();

			var commonSettings = new CommonSettings();
			commonSettings.Humans2.Add(new HumanAccount { Name = "test_" + new Random().Next(10000), BirthDate = DateTime.Now });

			var userSettings = new UserSettings();

			var mainViewModel = new MainViewModel(commonSettings, userSettings);

			await mainViewModel.Open.ExecuteAsync(null);
			var siOnline = (SIOnlineViewModel)mainViewModel.ActiveView;

			await siOnline.Init();

			siOnline.NewGame.Execute(null);

			var gameSettings = (GameSettingsViewModel)siOnline.Content.Content.Data;
			gameSettings.NetworkGameName = "testGame" + new Random().Next(10000);
			gameSettings.NetworkGamePassword = "testpass";

			gameSettings.SelectPackage.Execute(packageSourceType);

			if (packageSourceType == PackageSourceTypes.SIStorage)
			{
				var storage = gameSettings.StorageInfo.Model.CurrentPackage = new Services.SI.PackageInfo
				{
					ID = 300,
					Guid = "d4e98453-fd31-4b62-b120-96a18d6684b3",
					Description = "3rd Anime"
				};
				await gameSettings.StorageInfo.LoadStorePackage.ExecuteAsync(null);
			}

			await gameSettings.BeginGame.ExecuteAsync(null);

			var game = (GameViewModel)mainViewModel.ActiveView;

			var tInfo = ((ViewerHumanLogic)game.Host.MyLogic).TInfo;
			tInfo.PropertyChanged += TInfo_PropertyChanged;

			await Task.Delay(5000);
			((PersonAccount)game.Data.Me).BeReadyCommand.Execute(null);
		}

		private static void TInfo_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(TableInfoViewModel.MediaSource))
			{
				Download(((TableInfoViewModel)sender).MediaSource.Uri);
			}
			else if (e.PropertyName == nameof(TableInfoViewModel.SoundSource))
			{
				Download(((TableInfoViewModel)sender).SoundSource.Uri);
			}
		}

		private static async void Download(string uri)
		{
            _ = await HttpClient.GetAsync(uri);
		}
	}
}
