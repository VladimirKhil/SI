using SIStorageService.Client.Models;

namespace SIStorageService.Client
{
    /// <summary>
    /// Defines a SI Storage client.
    /// </summary>
    public interface ISIStorageServiceClient
    {
        /// <summary>
        /// Gets identifiers of packages containing provided tag.
        /// </summary>
        /// <param name="tagId">Tag identifier to search. If omitted, all packages are returned.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Found package identifiers.</returns>
        Task<string[]> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets package by its unique identifier.
        /// </summary>
        /// <param name="packageGuid">Package unique identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Found package.</returns>
        Task<PackageLink> GetPackageByGuid2Async(string packageGuid, CancellationToken cancellationToken = default);
    }
}
