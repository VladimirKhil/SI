using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SI.GameServer.Client;
using SICore;
using SICore.PlatformSpecific;
using SIData;
using SIGame.ViewModel;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.PackageSources;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Client;
using SIStorage.Service.Client;
using SIUI.ViewModel;
using System.ComponentModel;
using System.Net;

namespace SIGame.Tests;

[Parallelizable(ParallelScope.All)]
[TestFixture]
public class MainTest
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

    [TestCase(PackageSourceTypes.RandomServer, GameRole.Player)]
    [TestCase(PackageSourceTypes.SIStorage, GameRole.Player)]
    [TestCase(PackageSourceTypes.Local, GameRole.Player)]
    [TestCase(PackageSourceTypes.RandomServer, GameRole.Viewer)]
    [TestCase(PackageSourceTypes.RandomServer, GameRole.Showman)]
    public async Task GameCreateAndRun_Ok_Async(
        PackageSourceTypes packageSourceType,
        GameRole gameRole)
    {
        var coreManager = new DesktopCoreManager();
        var manager = new TestManager();

        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "test_" + new Random().Next(10000), BirthDate = DateTime.Now });

        var userSettings = new UserSettings();

        var appState = new AppState();

        var configurationBuilder = new ConfigurationBuilder();

        configurationBuilder.AddJsonFile("appsettings.json");

        var configuration = configurationBuilder.Build();

        var services = new ServiceCollection();

        services.AddSIGameServerClient(configuration);
        services.AddSingleton<IUIThreadExecutor>(manager);
        services.AddSIStorageServiceClient(configuration);
        services.AddSIStatisticsServiceClient(configuration);

        var serviceProvider = services.BuildServiceProvider();
        manager.ServiceProvider = serviceProvider;

        var mainViewModel = new MainViewModel(commonSettings, userSettings, appState, serviceProvider);

        await mainViewModel.Open.ExecuteAsync(null);

        var contentBox = mainViewModel.ActiveView as ContentBox;
        Assert.That(contentBox, Is.Null, ((LoginViewModel?)contentBox?.Data)?.Error);

        var siOnline = (SIOnlineViewModel?)mainViewModel.ActiveView;

        Assert.That(siOnline, Is.Not.Null);

        await siOnline!.InitAsync();

        siOnline.NewGame.Execute(null);

        var gameSettings = (GameSettingsViewModel)siOnline.Content.Content.Data;
        gameSettings.NetworkGameName = "testGame" + new Random().Next(10000);
        gameSettings.NetworkGamePassword = "testpass";
        gameSettings.Role = gameRole;

        if (packageSourceType == PackageSourceTypes.RandomServer)
        {
            await gameSettings.SelectPackage.ExecuteAsync(new SIStorageParameters { StorageIndex = 0, IsRandom = true });
        }
        else if (packageSourceType == PackageSourceTypes.SIStorage)
        {
            await gameSettings.SelectPackage.ExecuteAsync(new SIStorageParameters { StorageIndex = 0, IsRandom = false });

            var counter = 10;

            var storageInfo = (SIStorageViewModel)siOnline.Content.Content.Data;

            storageInfo.Model.DefaultRestriction = null;

            while (storageInfo.Model.Packages == null && counter > 0)
            {
                counter--;
                await Task.Delay(1000);
            }

            if (storageInfo.Model.Packages == null)
            {
                Assert.Fail("Could not load storage packages");
                return;
            }

            var packageId = Guid.Parse("3cb371d6-aaff-4623-bc3b-382be11d5f9a");

            var package = storageInfo.Model.Packages.FirstOrDefault(p => p.Model.Id == packageId);

            Assert.That(package, Is.Not.Null, "Package not found");

            var storage = storageInfo.Model.CurrentPackage = package;

            storageInfo.LoadStorePackage.Execute(null);
        }
        else
        {
            await gameSettings.SelectPackage.ExecuteAsync(packageSourceType);
        }

        await gameSettings.BeginGame.ExecuteAsync(null);

        Assert.That(gameSettings.IsProgress, Is.False);
        Assert.That(gameSettings.ErrorMessage, Is.Null);

        var siOnlineError = mainViewModel.ActiveView as SIOnlineViewModel;
        Assert.That(siOnlineError, Is.Null, siOnlineError?.Error);

        var game = (GameViewModel?)mainViewModel.ActiveView;

        var tInfo = game!.TInfo;
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
        Assert.That(contentBox2, Is.Null, ((LoginViewModel?)contentBox2?.Data)?.Error);

        var siOnline2 = (SIOnlineViewModel?)mainViewModel.ActiveView;
        siOnline2?.Cancel.Execute(null);
    }

    private static async void TInfo_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        if (e.PropertyName == nameof(TableInfoViewModel.MediaSource))
        {
            var mediaSource = ((TableInfoViewModel)sender).MediaSource;

            if (mediaSource != null)
            {
                var result = await HttpClient.GetAsync(mediaSource.Uri);
                Assert.That(result.IsSuccessStatusCode, Is.True);
            }
        }
        else if (e.PropertyName == nameof(TableInfoViewModel.SoundSource))
        {
            await HttpClient.GetAsync(((TableInfoViewModel)sender).SoundSource?.Uri);
        }
    }
}
