using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SI.GameServer.Client.Discovery;
using System.Net;
using System.Net.Http.Headers;

namespace SI.GameServer.Client;

/// <summary>
/// Allows to add <see cref="IGameServerClient" /> and <see cref="IGameServerClientFactory" /> implementations to service collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IGameServerClient" /> and <see cref="IGameServerClientFactory" /> implementations to service collection.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">App configuration.</param>
    public static IServiceCollection AddSIGameServerClient(this IServiceCollection services, IConfiguration configuration)
    {
        var optionsSection = configuration.GetSection(GameServerClientOptions.ConfigurationSectionName);
        services.Configure<GameServerClientOptions>(optionsSection);

        services.AddTransient<IGameServerClient, GameServerClient>();
        services.AddSingleton<IGameServerClientFactory, GameServerClientFactory>();

        var options = optionsSection.Get<GameServerClientOptions>() ?? new GameServerClientOptions();

        services.AddHttpClient<IGameServerLocator, GameServerLocator>(
            client =>
            {
                client.BaseAddress = options.ServiceDiscoveryUri;
                client.DefaultRequestVersion = HttpVersion.Version20;

                if (options.Culture != null)
                {
                    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(options.Culture));
                }
            });

        return services;
    }
}
