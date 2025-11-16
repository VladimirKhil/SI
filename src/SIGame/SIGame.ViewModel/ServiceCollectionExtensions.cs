using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SICore.Contracts;
using SIGame.ViewModel.Contracts;
using SIGame.ViewModel.Services;
using System.Net;

namespace SIGame.ViewModel;

public static class ServiceCollectionExtensions
{
    private const int RetryCount = 5;

    public static IServiceCollection AddSIGame(this IServiceCollection services)
    {
        services.AddHttpClient<ILocalFileManager, LocalFileManager>(
            client =>
            {
                client.DefaultRequestVersion = HttpVersion.Version20;
            })
            .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    RetryCount,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))));

        services.AddSingleton<IGameHost, GameHost>();
        services.AddSingleton<IGameSettingsViewModelFactory, GameSettingsViewModelFactory>();
        services.AddSingleton<MainViewModel>();

        return services;
    }
}
