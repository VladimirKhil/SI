using System;
using System.Text;
using System.IO;
using SIPackages;
using System.Threading.Tasks;
using SIGame.ViewModel.Properties;

namespace SIGame.ViewModel.PackageSources
{
    /// <summary>
    /// Следующий источник пакета
    /// </summary>
    internal sealed class NextPackageSource: PackageSource
    {
        private string _fName;

        public override PackageSourceKey Key => new PackageSourceKey { Type = PackageSourceTypes.Next };

        public override string Source => Resources.Next;

        private string FileName
        {
            get
            {
                if (_fName == null)
                {
                    var packages = UserSettings.Default.Packages;

                    var item = packages[0];
                    packages.RemoveAt(0);
                    packages.Add(item);
                    _fName = Path.Combine(Global.PackagesUri, item);
                }

                return _fName;
            }
        }

        public NextPackageSource()
        {
            var packages = UserSettings.Default.Packages;

            if (packages == null)
            {
                packages = new System.Collections.Specialized.StringCollection();
                UserSettings.Default.Packages = packages;
            }

            var dir = new DirectoryInfo(Global.PackagesUri);
            if (dir.Exists)
            {
                var files = dir.GetFiles("*.siq");
                foreach (var file in files)
                {
                    if (!packages.Contains(file.Name))
                        packages.Insert(0, file.Name);
                }
            }
        }

        public override Task<(string, bool)> GetPackageFileAsync()
        {
            var packages = UserSettings.Default.Packages;

            _fName = "";

            if (packages.Count == 0)
            {
                throw new ArgumentException($"{nameof(NextPackageSource)}: no packages!");
            }

            string item = packages[0];
            packages.RemoveAt(0);
            _fName = Path.Combine(Global.PackagesUri, item);
            packages.Add(item);

            return Task.FromResult((_fName, false));
        }

        public override Task<Stream> GetPackageDataAsync() => Task.FromResult((Stream)File.OpenRead(FileName));

        public override string GetPackageName() => Path.GetFileName(FileName);

        public override async Task<byte[]> GetPackageHashAsync()
        {
            var buffer = new byte[1024 * 1024];
            int count;
            using (var sha1 = new System.Security.Cryptography.SHA1Managed())
            {
                using (var stream = File.OpenRead(FileName))
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
            using (var stream = File.OpenRead(FileName))
            {
                using (var doc = SIDocument.Load(stream))
                {
                    return doc.Package.ID;
                }
            }
        }
    }
}
