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
    private readonly ISIStorageServiceClient _storageServiceClient;
    private Uri? _packageUri;

    public override PackageSourceKey Key => new() { Type = PackageSourceTypes.RandomServer };

    public override string Source => Resources.RandomServerThemes;

    public override bool RandomSpecials => true;

    public RandomStoragePackageSource(ISIStorageServiceClient storageServiceClient) => _storageServiceClient = storageServiceClient;

    public override Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

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
            var package = await _storageServiceClient.Packages.GetRandomPackageAsync(
                new RandomPackageParameters
                {
                    RestrictionIds = new int[] { -1 }
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
