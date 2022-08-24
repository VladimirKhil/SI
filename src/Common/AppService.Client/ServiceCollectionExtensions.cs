using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace AppService.Client
{
    /// <summary>
    /// Provides an extension method for adding <see cref="IAppServiceClient" /> implementation to service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds <see cref="IAppServiceClient" /> implementation to service collection.
        /// </summary>
        /// <remarks>
        /// When no gameresult service Uri has been provided, ads no-op implementation.
        /// </remarks>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">App configuration.</param>
        public static IServiceCollection AddAppServiceClient(this IServiceCollection services, IConfiguration configuration)
        {
            var optionsSection = configuration.GetSection(AppServiceClientOptions.ConfigurationSectionName);
            services.Configure<AppServiceClientOptions>(optionsSection);

            var options = optionsSection.Get<AppServiceClientOptions>();

            if (options?.ServiceUri != null)
            {
                services.AddHttpClient<IAppServiceClient, AppServiceClient>(
                    client =>
                    {
                        client.BaseAddress = options?.ServiceUri;
                        client.DefaultRequestVersion = HttpVersion.Version20;
                    });
            }
            else
            {
                services.AddSingleton<IAppServiceClient, NoOpAppServiceClient>();
            }

            return services;
        }
    }
}
