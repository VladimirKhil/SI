using Newtonsoft.Json;
using SIStorageService.Client.Models;
using System.Net.Sockets;
using System.Text;

namespace SIStorageService.Client;

// TODO: Implement retries via Polly

/// <inheritdoc cref="ISIStorageServiceClient" />
public sealed class SIStorageServiceClient : ISIStorageServiceClient
{
    private static readonly JsonSerializer Serializer = new();
    private readonly HttpClient _client;

    public SIStorageServiceClient(HttpClient client)
    {
        _client = client;
    }

    public Task<Package[]?> GetAllPackagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<Package[]>("Packages", cancellationToken);

    public Task<PackageCategory[]?> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<PackageCategory[]>("Categories", cancellationToken);

    public Task<Package[]?> GetPackagesByCategoryAndRestrictionAsync(int categoryID, string restriction, CancellationToken cancellationToken = default) =>
        GetAsync<Package[]>($"Packages?categoryID={categoryID}&restriction={Uri.EscapeDataString(restriction)}", cancellationToken);

    public Task<Uri?> GetPackageByIDAsync(int packageID, CancellationToken cancellationToken = default) =>
        GetAsync<Uri>($"Package?packageID={packageID}", cancellationToken);

    public Task<PackageLink?> GetPackage2ByIDAsync(int packageID, CancellationToken cancellationToken = default) =>
        GetAsync<PackageLink>($"Package2?packageID={packageID}", cancellationToken);

    public Task<Uri?> GetPackageByGuidAsync(string packageGuid, CancellationToken cancellationToken = default) =>
        GetAsync<Uri>($"PackageByGuid?packageGuid={packageGuid}", cancellationToken);

    public Task<PackageLink?> GetPackageByGuid2Async(string packageGuid, CancellationToken cancellationToken = default) =>
        GetAsync<PackageLink>($"PackageByGuid2?packageGuid={packageGuid}", cancellationToken);

    public Task<string?> GetPackageNameByGuidAsync(string packageGuid, CancellationToken cancellationToken = default) =>
        GetAsync<string>($"packages/{packageGuid}/name", cancellationToken);

    public Task<string[]?> GetPackagesByTagAsync(int? tagId = null, CancellationToken cancellationToken = default)
    {
        var queryString = new StringBuilder();

        if (tagId.HasValue)
        {
            queryString.Append("tagId=").Append(tagId.Value);
        }

        var packageFilter = queryString.Length > 0 ? $"?{queryString}" : "";

        return GetAsync<string[]>($"PackagesByTag{packageFilter}", cancellationToken);
    }

    [Obsolete]
    public Task<NewServerInfo[]?> GetGameServersUrisAsync(CancellationToken cancellationToken = default) =>
        GetAsync<NewServerInfo[]>("servers", cancellationToken);

    public Task<NamedObject[]?> GetAuthorsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<NamedObject[]>("Authors", cancellationToken);

    public Task<NamedObject[]?> GetPublishersAsync(CancellationToken cancellationToken = default) =>
        GetAsync<NamedObject[]>("Publishers", cancellationToken);

    public Task<NamedObject[]?> GetTagsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<NamedObject[]>("Tags", cancellationToken);

    public Task<PackageInfo[]?> GetPackagesAsync(
        int? tagId = null,
        int difficultyRelation = 0,
        int difficulty = 1,
        int? publisherId = null,
        int? authorId = null,
        string? restriction = null,
        PackageSortMode sortMode = PackageSortMode.Name,
        bool sortAscending = true,
        CancellationToken cancellationToken = default)
    {
        var queryString = new StringBuilder();

        if (tagId.HasValue)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("tagId=").Append(tagId.Value);
        }

        if (difficultyRelation > 0)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("difficultyRelation=").Append(difficultyRelation);
        }

        if (difficulty > 1)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("difficulty=").Append(difficulty);
        }

        if (publisherId.HasValue)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("publisherId=").Append(publisherId.Value);
        }

        if (authorId.HasValue)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("authorId=").Append(authorId.Value);
        }

        if (restriction != null)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("restriction=").Append(Uri.EscapeDataString(restriction));
        }

        if (sortMode != PackageSortMode.Name)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("sortMode=").Append((int)sortMode);
        }

        if (!sortAscending)
        {
            if (queryString.Length > 0)
                queryString.Append('&');

            queryString.Append("sortAscending=false");
        }

        return GetAsync<PackageInfo[]>($"FilteredPackages{(queryString.Length > 0 ? $"?{queryString}" : "")}", cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var responseMessage = await _client.GetAsync(request, cancellationToken);

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"GetAsync {request} error ({responseMessage.StatusCode}): " +
                    $"{await responseMessage.Content.ReadAsStringAsync(cancellationToken)}");
            }
            
            using var responseStream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(responseStream);

            return (T?)Serializer.Deserialize(reader, typeof(T));
        }
        catch (SocketException exc)
        {
            throw new Exception($"SIStorage exception accessing uri {request}: {exc.ErrorCode}", exc);
        }
    }
}
