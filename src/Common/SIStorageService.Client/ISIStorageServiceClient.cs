using SIStorageService.Client.Models;

namespace SIStorageService.Client;

/// <summary>
/// Defines a SI Storage client.
/// </summary>
public interface ISIStorageServiceClient
{
    /// <summary>
    /// Gets well-known package publishers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<NamedObject[]?> GetPublishersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets well-known package tags.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<NamedObject[]?> GetTagsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets packages by filter.
    /// </summary>
    /// <param name="tagId">Package tag identifier.</param>
    /// <param name="difficultyRelation">Shpuld package difficulty be greater than threshold (0) or less than it (otherwise).</param>
    /// <param name="difficulty">Package difficulty threshold.</param>
    /// <param name="publisherId">Package publisher identifier.</param>
    /// <param name="authorId">Package author identifier.</param>
    /// <param name="restriction">Package restriction.</param>
    /// <param name="sortMode">Packages sort mode.</param>
    /// <param name="sortAscending">Packages sort direction.</param>
    /// <returns>Found packages.</returns>
    Task<PackageInfo[]?> GetPackagesAsync(
        int? tagId = null,
        int difficultyRelation = 0,
        int difficulty = 1,
        int? publisherId = null,
        int? authorId = null,
        string? restriction = null,
        PackageSortMode sortMode = PackageSortMode.Name,
        bool sortAscending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets identifiers of packages containing provided tag.
    /// </summary>
    /// <param name="tagId">Tag identifier to search. If omitted, all packages are returned.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Found package names.</returns>
    Task<string[]?> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package by its unique identifier.
    /// </summary>
    /// <param name="packageGuid">Package unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Found package.</returns>
    Task<PackageLink?> GetPackageByGuid2Async(string packageGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets package uri by identifier.
    /// </summary>
    /// <param name="packageID">Package identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Uri?> GetPackageByIDAsync(int packageID, CancellationToken cancellationToken = default);
}
