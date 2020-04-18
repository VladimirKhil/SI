using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIPackages.Providers
{
    public sealed class FolderPackageProvider : IPackagesProvider
    {
        private readonly string _folder;

        public FolderPackageProvider(string folder)
        {
            _folder = folder;
        }

        public Task<IEnumerable<string>> GetPackages()
        {
            var dir = new DirectoryInfo(_folder);
            return Task.FromResult(dir.EnumerateDirectories()
                .Where(directory => directory.Name != "Topical") // TODO: remove "Topical"
                .Select(directory => directory.Name));
        }

        public Task<SIDocument> GetPackage(string name) => Task.FromResult(SIDocument.Load(Path.Combine(_folder, name)));
    }
}
