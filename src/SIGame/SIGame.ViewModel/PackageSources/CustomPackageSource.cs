using System;
using System.IO;
using SIPackages;
using System.Threading.Tasks;

namespace SIGame.ViewModel.PackageSources
{
    /// <summary>
    /// Специально выбранный источник пакета
    /// </summary>
    internal sealed class CustomPackageSource: PackageSource
    {
        private readonly string _file = null;

        public override PackageSourceKey Key => new PackageSourceKey { Type = PackageSourceTypes.Local, Data = _file };

        public override string Source => _file == null ? null : Path.GetFileName(_file);

        public CustomPackageSource(string file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public override Task<(string, bool)> GetPackageFileAsync() => Task.FromResult((_file, false));

        public override Task<Stream> GetPackageDataAsync() => Task.FromResult((Stream)File.OpenRead(_file));

        public override string GetPackageName() => Source;

        public override async Task<byte[]> GetPackageHashAsync()
        {
            var buffer = new byte[1024 * 1024];
            int count;
            using (var sha1 = new System.Security.Cryptography.SHA1Managed())
            {
                using (var stream = File.OpenRead(_file))
                {
                    while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        sha1.TransformBlock(buffer, 0, count, buffer, 0);                    
                }

                sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                return sha1.Hash;
            }
        }

        public override string GetPackageId()
        {
            using (var stream = File.OpenRead(_file))
            {
                using (var doc = SIDocument.Load(stream))
                {
                    return doc.Package.ID;
                }
            }
        }
    }
}
