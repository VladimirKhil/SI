using SIGame.ViewModel.Properties;
using SIStorage.Service.Contract;
using SIStorage.Service.Contract.Requests;
using System.Net;

namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Represents random storage package source.
/// </summary>
public sealed class RandomStoragePackageSource : PackageSource
{
    private static readonly HttpClient Client = new() { DefaultRequestVersion = HttpVersion.Version20 };

    private readonly ISIStorageServiceClient _storageServiceClient;
    private Uri? _packageUri;

    public override PackageSourceKey Key => new() { Type = PackageSourceTypes.RandomServer };

    public override string Source => Resources.RandomServerThemes;

    public RandomStoragePackageSource(ISIStorageServiceClient storageServiceClient) => _storageServiceClient = storageServiceClient;

    public override async Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default)
    {
        if (_packageUri == null)
        {
            _packageUri = await CreateRandomPackageAsync(cancellationToken);
        }

        var response = await Client.GetAsync(_packageUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        var fileName = Path.GetTempFileName();
        using (var fs = File.OpenWrite(fileName))
        using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            await stream.CopyToAsync(fs, 81920 /* default */, cancellationToken);
        }

        return (fileName, true);
    }

    public override async Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default)
    {
        if (_packageUri == null)
        {
            _packageUri = await CreateRandomPackageAsync(cancellationToken);
        }

        return Array.Empty<byte>();
    }

    public override string GetPackageName() => "";

    public override Uri? GetPackageUri() => _packageUri;

    private async Task<Uri> CreateRandomPackageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var languages = await _storageServiceClient.Facets.GetLanguagesAsync(cancellationToken);
            var currentLanguage = Thread.CurrentThread.CurrentUICulture.Name;

            var language = languages.FirstOrDefault(l => l.Code == currentLanguage);

            var package = await _storageServiceClient.Packages.GetRandomPackageAsync(
                new RandomPackageParameters
                {
                    RestrictionIds = new int[] { -1 },
                    LanguageId = language?.Id
                },
                cancellationToken);

            if (package.DirectContentUri == null)
            {
                throw new Exception("Random package generation failed");
            }

            return package.DirectContentUri;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new Exception(Resources.TooMuchRandomPackages, ex);
        }
    }
}
