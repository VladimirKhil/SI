using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace SIStorageService.Client;

/// <summary>
/// Provides an extension method for adding <see cref="ISIStorageServiceClient" /> implementation to service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ISIStorageServiceClient" /> implementation to service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">App configuration.</param>
    public static IServiceCollection AddSIStorageServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(SIStorageClientOptions.ConfigurationSectionName);
        services.Configure<SIStorageClientOptions>(optionsSection);

        var options = optionsSection.Get<SIStorageClientOptions>();

        services.AddHttpClient<ISIStorageServiceClient, SIStorageServiceClient>(
            client =>
            {
                client.BaseAddress = options?.ServiceUri;
                client.DefaultRequestVersion = HttpVersion.Version20;
            });

        return services;
    }
}
