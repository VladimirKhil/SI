using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SIPackages.Providers
{
    public sealed class SIStoragePackageProvider : IPackagesProvider, IDisposable
    {
        private readonly Services.SI.ISIStorageServiceClient _siStorageServiceClient;
        private readonly HashSet<string> _downloadedFiles = new HashSet<string>();
        private readonly string _storageOriginsPath;
        private readonly string _storageUrl;

        public SIStoragePackageProvider(string serverAddress, string storageOriginsPath, string storageUrl)
        {
            _siStorageServiceClient = new Services.SI.SIStorageServiceClient(serverAddress);
            _storageOriginsPath = storageOriginsPath;
            _storageUrl = storageUrl;
        }

        public void Dispose()
        {
            var exceptionsList = new List<Exception>();
            foreach (var file in _downloadedFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception exc)
                {
                    exceptionsList.Add(exc);
                }
            }

            if (exceptionsList.Any())
            {
                throw new AggregateException(exceptionsList.ToArray());
            }
        }

        public async Task<SIDocument> GetPackageAsync(string name)
        {
            if (string.IsNullOrEmpty(_storageOriginsPath))
            {
                var fileName = Path.GetTempFileName();

                try
                {
                    var request = WebRequest.Create($"{_storageUrl}/{Uri.EscapeDataString(name)}"); // TODO: -> HttpClient
                    using (var response = await request.GetResponseAsync())
                    using (var stream = response.GetResponseStream())
                    {
                        using (var fs = File.Create(fileName))
                        {
                            await stream.CopyToAsync(fs);
                        }
                    }

                    _downloadedFiles.Add(fileName);

                    using (var fs = File.OpenRead(fileName))
                    {
                        return SIDocument.Load(fs);
                    }
                }
                finally
                {
                    File.Delete(fileName);
                }
            }
            else
            {
                var paths = name.Split('\\');
                var packagePath = Path.Combine(paths);
                var packageOriginPath = Path.Combine(_storageOriginsPath, packagePath);

                if (!File.Exists(packageOriginPath))
                {
                    throw new Exception($"Пакет {name} не существует!");
                }

                return SIDocument.Load(File.OpenRead(packageOriginPath));
            }
        }

        public async Task<IEnumerable<string>> GetPackagesAsync() => await _siStorageServiceClient.GetPackagesByTagAsync();
    }
}
