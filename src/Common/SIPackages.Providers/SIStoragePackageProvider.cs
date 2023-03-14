using SIPackages.Providers.Properties;
using SIStorageService.Client;
using System.Net;

namespace SIPackages.Providers;

public sealed class SIStoragePackageProvider : IPackagesProvider, IDisposable
{
    private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

    private readonly ISIStorageServiceClient _siStorageServiceClient;
    private readonly HashSet<string> _downloadedFiles = new();
    private readonly string _storageOriginsPath;
    private readonly string _storageUrl;

    public SIStoragePackageProvider(ISIStorageServiceClient siStorageServiceClient, string storageOriginsPath, string storageUrl)
    {
        _siStorageServiceClient = siStorageServiceClient;
        _storageOriginsPath = storageOriginsPath;
        _storageUrl = storageUrl;
    }

    public void Dispose()
    {
        var exceptionsList = new List<Exception>();

        foreach (var file in _downloadedFiles)
        {
            try
            {
                File.Delete(file);
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

    public async Task<SIDocument> GetPackageAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_storageOriginsPath))
        {
            var fileName = Path.GetTempFileName();

            var packageNameParts = name.Split('\\');
            var escapedName = string.Join("/", packageNameParts.Select(pnp => Uri.EscapeDataString(pnp)));

            var uri = $"{_storageUrl}/{escapedName}";

            using (var response = await HttpClient.GetAsync(uri, cancellationToken))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error while accessing \"{uri}\": {await response.Content.ReadAsStringAsync(cancellationToken)}!");
                }

                using var fs = File.Create(fileName);
                await response.Content.CopyToAsync(fs, cancellationToken);
            }

            _downloadedFiles.Add(fileName);

            return SIDocument.Load(File.OpenRead(fileName));
        }
        else
        {
            var paths = name.Split('\\');
            var packagePath = Path.Combine(paths);
            var packageOriginPath = Path.Combine(_storageOriginsPath, packagePath);

            if (!File.Exists(packageOriginPath))
            {
                throw new Exception(string.Format(Resources.PackageDoesNotExist, name));
            }

            return SIDocument.Load(File.OpenRead(packageOriginPath));
        }
    }

    public async Task<IEnumerable<string>> GetPackagesAsync(CancellationToken cancellationToken = default) =>
        await _siStorageServiceClient.GetPackagesByTagAsync(cancellationToken: cancellationToken);
}
