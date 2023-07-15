using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Services;
using System.Net;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Provides an extension method for adding <see cref="IChgkDbClient" /> implementation to service collection.
/// </summary>
public static class ChgkDbClientExtensions
{
    /// <summary>
    /// Adds <see cref="IChgkDbClient" /> implementation to service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">App configuration.</param>
    public static IServiceCollection AddChgkServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(ChgkDbClientOptions.ConfigurationSectionName);
        services.Configure<ChgkDbClientOptions>(optionsSection);

        var options = optionsSection.Get<ChgkDbClientOptions>();

        services.AddHttpClient<IChgkDbClient, ChgkDbClient>(
            client =>
            {
                if (options != null)
                {
                    client.BaseAddress = options.ServiceUri;
                    client.Timeout = options.Timeout;
                }

                client.DefaultRequestVersion = HttpVersion.Version20;

            })
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        options?.RetryCount ?? ChgkDbClientOptions.DefaultRetryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt))));

        return services;
    }
}
