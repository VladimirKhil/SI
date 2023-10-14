using SIStorage.Service.Contract;
using SIStorage.Service.Contract.Requests;
using System.Net;

namespace SIPackages.Providers;

/// <summary>
/// Provides packages from SI Storage.
/// </summary>
public sealed class SIStoragePackageProvider : IPackagesProvider, IDisposable
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

    private readonly ISIStorageServiceClient _siStorageServiceClient;
    private readonly Dictionary<string, PackageEntry> _packageCache = new();

    public SIStoragePackageProvider(ISIStorageServiceClient siStorageServiceClient) =>
        _siStorageServiceClient = siStorageServiceClient;

    public async Task<IEnumerable<string>> GetPackagesAsync(CancellationToken cancellationToken = default)
    {
        if (_packageCache.Count == 0)
        {
            var packages = await _siStorageServiceClient.Packages.GetPackagesAsync(
                new PackageFilters { TagIds = new[] { -1 } },
                new PackageSelectionParameters { Count = 1000 },
                cancellationToken);

            foreach (var package in packages.Packages)
            {
                if (package.DirectContentUri == null)
                {
                    continue;
                }

                _packageCache[package.Id.ToString()] = new PackageEntry { Uri = package.DirectContentUri };
            }
        }

        return _packageCache.Keys;
    }

    public async Task<SIDocument> GetPackageAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_packageCache.TryGetValue(name, out var info))
        {
            throw new PackageNotFoundException(name);
        }

        if (info.LocalPath == null)
        {
            var fileName = Path.GetTempFileName();

            using var response = await HttpClient.GetAsync(info.Uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Error while accessing \"{info.Uri}\": {await response.Content.ReadAsStringAsync(cancellationToken)}!");
            }

            using var fs = File.Create(fileName);
            await response.Content.CopyToAsync(fs, cancellationToken);

            info.LocalPath = fileName;
        }

        return SIDocument.Load(File.OpenRead(info.LocalPath));
    }

    public void Dispose()
    {
        var exceptionsList = new List<Exception>();

        foreach (var package in _packageCache)
        {
            try
            {
                if (package.Value.LocalPath != null)
                {
                    File.Delete(package.Value.LocalPath);
                }
            }
            catch (Exception exc)
            {
                exceptionsList.Add(exc);
            }
        }

        if (exceptionsList.Any())
        {
            throw new AggregateException(exceptionsList.ToArray());
        }
    }

    private record struct PackageEntry
    {
        public Uri Uri { get; set; }

        public string? LocalPath { get; set; }
    }
}
