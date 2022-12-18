using SIPackages;
using System.Security.Cryptography;

namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Represents a file-based package source.
/// </summary>
internal sealed class CustomPackageSource : PackageSource
{
    private readonly string _file = null;

    public override PackageSourceKey Key => new() { Type = PackageSourceTypes.Local, Data = _file };

    public override string Source => _file == null ? null : Path.GetFileName(_file);

    public CustomPackageSource(string file) => _file = file ?? throw new ArgumentNullException(nameof(file));

    public override Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((_file, false));

    public override Task<Stream> GetPackageDataAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult((Stream)File.OpenRead(_file));

    public override string GetPackageName() => Source;

    public override async Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[1024 * 1024];
        int count;

        using var sha1 = SHA1.Create();
        using (var stream = File.OpenRead(_file))
        {
            while ((count = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                sha1.TransformBlock(buffer, 0, count, buffer, 0);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return sha1.Hash;
    }

    public override string GetPackageId()
    {
        using var stream = File.OpenRead(_file);
        using var doc = SIDocument.Load(stream);
        return doc.Package.ID;
    }
}
