using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        public Task<IEnumerable<string>> GetPackagesAsync(CancellationToken cancellationToken = default)
        {
            var dir = new DirectoryInfo(_folder);

            return Task.FromResult(dir.EnumerateDirectories()
                .Where(directory => directory.Name != "Topical") // TODO: remove "Topical"
                .Select(directory => directory.Name));
        }

        public Task<SIDocument> GetPackageAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(SIDocument.Load(Path.Combine(_folder, name)));
    }
}
