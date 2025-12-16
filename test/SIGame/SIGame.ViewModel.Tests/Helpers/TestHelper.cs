using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SI.GameServer.Client;
using SIData;
using SIGame.ViewModel;
using SIGame.ViewModel.Settings;
using SIGame.ViewModel.Tests.Mocks;
using SIStatisticsService.Client;
using SIStorage.Service.Client;

namespace SIGame.ViewModel.Tests.Helpers;

/// <summary>
/// Helper class for creating test instances and service providers.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a service provider configured for testing.
    /// </summary>
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Add configuration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ServerUri", "https://test.server.com" },
                { "StorageServiceUri", "https://test.storage.com" },
                { "StatisticsServiceUri", "https://test.statistics.com" }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Add logging
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();

        // Add platform-specific services
        services.AddSingleton<IUIThreadExecutor, TestUIThreadExecutor>();

        // Add common settings
        var commonSettings = new CommonSettings();
        commonSettings.Humans2.Add(new HumanAccount { Name = "TestPlayer", BirthDate = DateTime.Now });
        services.AddSingleton(commonSettings);

        // Add user settings
        var userSettings = new UserSettings();
        services.AddSingleton(userSettings);

        // Add app state
        var appState = new AppState();
        services.AddSingleton(appState);

        // Add game server client using extension method
        services.AddSIGameServerClient(configuration);

        // Add storage service client using extension method
        services.AddSIStorageServiceClient(configuration);

        // Add statistics service client using extension method
        services.AddSIStatisticsServiceClient(configuration);

        // Add SIGame services
        services.AddSIGame();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a MainViewModel instance for testing.
    /// </summary>
    public static MainViewModel CreateMainViewModel(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MainViewModel>();
    }
}
