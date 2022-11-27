using SIPackages.Core;
using SIPackages.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SIPackages.Containers
{
    internal sealed class FolderSIPackageContainer : ISIPackageContainer
    {
        private readonly string _folder;

        public FolderSIPackageContainer(string folder) => _folder = folder;

        public ISIPackageContainer CopyTo(Stream stream, bool close, out bool isNew) => throw new NotImplementedException();

        internal static ISIPackageContainer Create(string folder) => new FolderSIPackageContainer(folder);

        internal static ISIPackageContainer Open(string folder) => new FolderSIPackageContainer(folder);

        public void CreateStream(string name, string contentType)
        {
            using (File.Create(Path.Combine(_folder, name))) { }
        }

        public void CreateStream(string category, string name, string contentType)
        {
            Directory.CreateDirectory(Path.Combine(_folder, category));
            using (File.Create(Path.Combine(_folder, category, name))) { }
        }

        public async Task CreateStreamAsync(
            string category,
            string name,
            string contentType,
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(Path.Combine(_folder, category));

            using var fs = File.Create(Path.Combine(_folder, category, name));

            await stream.CopyToAsync(fs, cancellationToken);
        }

        public void DeleteStream(string category, string name) => throw new NotImplementedException();

        public void Dispose() { }

        public void Flush() { }

        public string[] GetEntries(string category)
        {
            var directoryInfo = new DirectoryInfo(Path.Combine(_folder, category));

            if (!directoryInfo.Exists)
            {
                return Array.Empty<string>();
            }

            return directoryInfo.GetFiles().Select(file => file.Name).ToArray();
        }

        public StreamInfo? GetStream(string name, bool read = true)
        {
            var file = new FileInfo(Path.Combine(_folder, name));

            if (!file.Exists)
            {
                return null;
            }

            return new StreamInfo { Length = file.Length, Stream = read ? file.OpenRead() : file.Open(FileMode.Open) };
        }

        public StreamInfo? GetStream(string category, string name, bool read = true)
        {
            if (name.Length > ZipHelper.MaxFileNameLength)
            {
                name = ZipHelper.CalculateHash(name);
            }

            return GetStream(Path.Combine(category, name), read);
        }

        public long GetStreamLength(string name)
        {
            var file = new FileInfo(Path.Combine(_folder, name));

            if (!file.Exists)
            {
                return -1;
            }

            return file.Length;
        }

        public long GetStreamLength(string category, string name)
        {
            if (name.Length > ZipHelper.MaxFileNameLength)
            {
                name = ZipHelper.CalculateHash(name);
            }

            return GetStreamLength(Path.Combine(category, name));
        }

        public string[] GetFilteredEntries()
        {
            var filteredFile = new FileInfo(Path.Combine(_folder, "filtered.txt"));
            
            if (!filteredFile.Exists)
            {
                return Array.Empty<string>();
            }

            return File.ReadAllLines(filteredFile.FullName);
        }
    }
}
