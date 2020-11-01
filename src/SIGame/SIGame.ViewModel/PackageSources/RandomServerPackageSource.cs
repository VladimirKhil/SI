using SIGame.ViewModel.Properties;
using System;
using System.Threading.Tasks;

namespace SIGame.ViewModel.PackageSources
{
    /// <summary>
    /// Случайный пакет сервера
    /// </summary>
    public sealed class RandomServerPackageSource : PackageSource
    {
        public override PackageSourceKey Key => new PackageSourceKey { Type = PackageSourceTypes.RandomServer };

        public override string Source => Resources.RandomServerThemes;

        public override bool RandomSpecials => true;

        public override Task<(string, bool)> GetPackageFileAsync() => throw new NotImplementedException();

        public override Task<byte[]> GetPackageHashAsync() => Task.FromResult(Array.Empty<byte>());

        public override string GetPackageName() => "";
    }
}
