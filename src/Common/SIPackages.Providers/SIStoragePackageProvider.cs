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
    private readonly Dictionary<int, Dictionary<string, PackageEntry>> _packageCache = new();
    private Dictionary<string, int>? _languageCache = null;
    private readonly SemaphoreSlim _packageSemaphore = new(1, 1);

    public SIStoragePackageProvider(ISIStorageServiceClient siStorageServiceClient) =>
        _siStorageServiceClient = siStorageServiceClient;

    public async Task<IEnumerable<string>> GetPackagesAsync(string culture, CancellationToken cancellationToken = default)
    {
        if (_languageCache == null)
        {
            await _packageSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (_languageCache == null)
                {
                    var languages = await _siStorageServiceClient.Facets.GetLanguagesAsync(cancellationToken);
                    _languageCache = languages.ToDictionary(l => l.Code, l => l.Id);
                }
            }
            finally
            {
                _packageSemaphore.Release();
            }
        }

        if (!_languageCache.TryGetValue(culture, out var languageId) || culture == null)
        {
            if (!_languageCache.TryGetValue("en-US", out languageId))
            {
                languageId = -1;
            }
        }

        if (!_packageCache.TryGetValue(languageId, out var localizedCache))
        {
            await _packageSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (!_packageCache.TryGetValue(languageId, out localizedCache))
                {
                    _packageCache[languageId] = localizedCache = new Dictionary<string, PackageEntry>();

                    var packages = await _siStorageServiceClient.Packages.GetPackagesAsync(
                        new PackageFilters { LanguageId = languageId, TagIds = new[] { -1 } },
                        new PackageSelectionParameters { Count = 1000 },
                        cancellationToken);

                    foreach (var package in packages.Packages)
                    {
                        if (package.DirectContentUri == null)
                        {
                            continue;
                        }

                        localizedCache[package.Id.ToString()] = new PackageEntry { Uri = package.DirectContentUri };
                    }
                }
            }
            finally
            {
                _packageSemaphore.Release();
            }
        }

        return localizedCache.Keys;
    }

    public async Task<SIDocument> GetPackageAsync(string culture, string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        int languageId;

        if (_languageCache == null || culture == null)
        {
            languageId = -1;
        }
        else if (!_languageCache.TryGetValue(culture, out languageId))
        {
            if (!_languageCache.TryGetValue("en-US", out languageId))
            {
                languageId = -1;
            }
        }

        if (!_packageCache.TryGetValue(languageId, out var localizedCache))
        {
            throw new PackageNotFoundException(name);
        }

        if (!localizedCache.TryGetValue(name, out var info))
        {
            throw new PackageNotFoundException(name);
        }

        if (info.LocalPath == null)
        {
            await _packageSemaphore.WaitAsync(cancellationToken);

            try
            {
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
            }
            finally
            {
                _packageSemaphore.Release();
            }
        }

        return SIDocument.Load(File.OpenRead(info.LocalPath));
    }

    public void Dispose()
    {
        var exceptionsList = new List<Exception>();

        foreach (var localizedCache in _packageCache.Values)
        {
            foreach (var package in localizedCache.Values)
            {
                try
                {
                    if (package.LocalPath != null)
                    {
                        File.Delete(package.LocalPath);
                    }
                }
                catch (Exception exc)
                {
                    exceptionsList.Add(exc);
                }
            }
        }

        if (exceptionsList.Any())
        {
            throw new AggregateException(exceptionsList.ToArray());
        }
    }

    private record PackageEntry
    {
        public Uri Uri { get; set; }

        public string? LocalPath { get; set; }
    }
}
