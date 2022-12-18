using SIGame.ViewModel.Properties;

namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Случайный пакет сервера
/// </summary>
public sealed class RandomServerPackageSource : PackageSource
{
    public override PackageSourceKey Key => new PackageSourceKey { Type = PackageSourceTypes.RandomServer };

    public override string Source => Resources.RandomServerThemes;

    public override bool RandomSpecials => true;

    public override Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public override Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());

    public override string GetPackageName() => "";
}
