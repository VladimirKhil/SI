using SImulator.ViewModel.PlatformSpecific;
using SIStorageService.Client.Models;
using System;
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
    private readonly PackageInfo _package;
    private readonly Uri _packageUri;

    private static readonly HttpClient _client = new() { DefaultRequestVersion = HttpVersion.Version20 };

    public string Name => _package.Description ?? "";

    public string Token => "";

    public SIStoragePackageSource(PackageInfo package, Uri packageUri)
    {
        _package = package;
        _packageUri = packageUri;
    }

    public async Task<(string filePath, bool isTemporary)> GetPackageFileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _client.GetAsync(_packageUri, cancellationToken);

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
