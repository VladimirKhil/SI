namespace SIPackages.Providers;

/// <summary>
/// Represents a package provider.
/// </summary>
public interface IPackagesProvider
{
    /// <summary>
    /// Enumerates available packages names.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IEnumerable<string>> GetPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package by name.
    /// </summary>
    /// <param name="name">Package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<SIDocument> GetPackageAsync(string name, CancellationToken cancellationToken = default);
}
