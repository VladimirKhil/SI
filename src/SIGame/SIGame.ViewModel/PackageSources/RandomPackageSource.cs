using SICore.PlatformSpecific;
using SIGame.ViewModel.Properties;
using SIPackages;

namespace SIGame.ViewModel.PackageSources;

/// <summary>
/// Случайный набор тем
/// </summary>
internal sealed class RandomPackageSource: PackageSource
{
    private readonly int _packageId = new Random().Next(int.MaxValue);

    public override PackageSourceKey Key => new PackageSourceKey { Type = PackageSourceTypes.Random };

    public override string Source => Resources.RandomThemes;

    public override bool RandomSpecials { get { return true; } }

    public override async Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetTempFileName();

        using (var fs = File.Open(fileName, FileMode.Create, FileAccess.ReadWrite))
        using (var document = await GetPackageCore(fs))
        {
            document.Save();
        }

        return (fileName, true);
    }

    private static Task<SIDocument> GetPackageCore(Stream stream = null)
    {
        var settings = UserSettings.Default.GameSettings.AppSettings;

        return SIPackages.Providers.PackageHelper.GenerateRandomPackageAsync(
            new PackageProvider(Global.PackagesUri),
            Resources.RandomThemes,
            Resources.Mixed,
            Resources.RoundTrailing,
            Resources.GameStage_Final,
            settings.RandomRoundsCount,
            settings.RandomThemesCount,
            settings.RandomQuestionsBasePrice,
            stream);
    }

    public override string GetPackageName() => _packageId.ToString();

    public override Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(BitConverter.GetBytes(_packageId));

    public override string GetPackageId() => _packageId.ToString();

    public override async Task<Stream> GetPackageDataAsync(CancellationToken cancellationToken = default)
    {
        byte[] data;

        using (var ms = new MemoryStream())
        {
            using (var package = await GetPackageCore(ms))
            {
                package.Save();
            }

            data = ms.ToArray();
        }

        return new MemoryStream(data);
    }
}
