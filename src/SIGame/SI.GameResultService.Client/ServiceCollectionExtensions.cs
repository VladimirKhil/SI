using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace SI.GameResultService.Client
{
    /// <summary>
    /// Provides an extension method for adding <see cref="IGameResultServiceClient" /> implementation to service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="IGameResultServiceClient" /> implementation to service collection.
        /// </summary>
        /// <remarks>
        /// When no gameresult service Uri has been provided, adds no-op implementation.
        /// </remarks>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">App configuration.</param>
        public static IServiceCollection AddGameResultServiceClient(this IServiceCollection services, IConfiguration configuration)
        {
            var optionsSection = configuration.GetSection(GameResultServiceClientOptions.ConfigurationSectionName);
            services.Configure<GameResultServiceClientOptions>(optionsSection);

            var options = optionsSection.Get<GameResultServiceClientOptions>();
            if (options?.ServiceUri != null)
            {
                services.AddHttpClient<IGameResultServiceClient, GameResultServiceClient>(
                    client =>
                    {
                        client.BaseAddress = options?.ServiceUri;
                        client.DefaultRequestVersion = HttpVersion.Version20;
                    });
            }
            else
            {
                services.AddSingleton<IGameResultServiceClient, NoOpGameResultServiceClient>();
            }

            return services;
        }
    }
}
