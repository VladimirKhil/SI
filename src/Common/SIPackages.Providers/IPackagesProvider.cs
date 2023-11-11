namespace SIPackages.Providers;

/// <summary>
/// Represents a packages provider.
/// </summary>
public interface IPackagesProvider
{
    /// <summary>
    /// Enumerates available packages names.
    /// </summary>
    /// <param name="culture">Packages culture.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<string>> GetPackagesAsync(string culture, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package by name.
    /// </summary>
    /// <param name="culture">Packages culture.</param>
    /// <param name="name">Package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SIDocument> GetPackageAsync(string culture, string name, CancellationToken cancellationToken = default);
}
