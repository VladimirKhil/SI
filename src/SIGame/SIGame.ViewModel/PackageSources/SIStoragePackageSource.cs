using System.Net;

namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Источник пакета, использующий поток данных
/// </summary>
internal sealed class SIStoragePackageSource : PackageSource
{
    private readonly Uri _packageUri = null;
    private readonly int _id;
    private readonly string _name;
    private readonly string _packageId;

    private static readonly HttpClient Client = new HttpClient { DefaultRequestVersion = HttpVersion.Version20 };

    public override PackageSourceKey Key =>
        new PackageSourceKey
        {
            Type = PackageSourceTypes.SIStorage,
            Data = _packageUri.AbsoluteUri,
            ID = _id,
            Name = _name,
            PackageID = _packageId
        };

    public SIStoragePackageSource(Uri packageUri, int id, string name, string packageId)
    {
        _packageUri = packageUri;
        _id = id;
        _name = name;
        _packageId = packageId;
    }

    public override string Source => $"*{_name}";

    public override async Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default)
    {
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

    public override string GetPackageName() => null;

    public override Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    public override string GetPackageId() => _packageId;
}
