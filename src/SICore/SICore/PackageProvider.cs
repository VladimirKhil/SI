using SIPackages;
using SIPackages.Providers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SICore.PlatformSpecific
{
    public sealed class PackageProvider: IPackagesProvider
    {
        private readonly string _folder;

        public PackageProvider(string folder)
        {
            _folder = folder;
        }

        public Task<IEnumerable<string>> GetPackages()
        {
            var dir = new DirectoryInfo(_folder);
            return Task.FromResult(dir.EnumerateFiles("*.siq").Select(file => file.Name));
        }

        public Task<SIDocument> GetPackage(string name)
        {
            return Task.FromResult(SIDocument.Load(File.OpenRead(Path.Combine(_folder, name))));
        }
    }
}
