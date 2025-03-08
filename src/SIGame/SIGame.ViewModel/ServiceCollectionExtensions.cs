using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
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
        
        return services;
    }
}
