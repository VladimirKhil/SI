using SImulator.ViewModel.PlatformSpecific;
using SIStorage.Service.Contract.Models;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SImulator.Implementation;

/// <summary>
/// Defines a SIStorage-based package source.
/// </summary>
internal sealed class SIStoragePackageSource : IPackageSource
{
    private readonly Package _package;

    private static readonly HttpClient _client = new() { DefaultRequestVersion = HttpVersion.Version20 };

    public string Name => _package.Name ?? "";

    public string Token => "";

    public SIStoragePackageSource(Package package) => _package = package;

    public async Task<(string filePath, bool isTemporary)> GetPackageFileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync(_package.ContentUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        var fileName = Path.GetTempFileName();

        using (var stream = File.OpenWrite(fileName))
        using (var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
        {
            await responseStream.CopyToAsync(stream, cancellationToken);
        }

        return (fileName, true);
    }

    public override string ToString() => Name;
}
