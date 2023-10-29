using Microsoft.Extensions.DependencyInjection;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Services;
using SIStorageService.ViewModel;

namespace SIQuester.ViewModel;

/// <summary>
/// Allows to register SIQuester view model in <see cref="IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers SIQuester view model in <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">Services collection.</param>
    public static IServiceCollection AddSIQuester(this IServiceCollection services)
    {
        services.AddSingleton<IPackageTemplatesRepository, PackageTemplatesRepository>();
        services.AddSingleton<StorageViewModel>();
        services.AddSingleton<StorageContextViewModel>();
        services.AddSingleton<IDocumentViewModelFactory, DocumentViewModelFactory>();

        return services;
    }
}
