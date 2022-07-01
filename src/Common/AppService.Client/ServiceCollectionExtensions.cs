using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AppService.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAppServiceClient(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<AppServiceClientOptions>(configuration.GetSection(AppServiceClientOptions.ConfigurationSectionName));
            services.AddHttpClient<IAppServiceClient, AppServiceClient>();

            return services;
        }
    }
}
