using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SIGame.ViewModel.PackageSources
{
    /// <summary>
    /// Источник пакета, использующий поток данных
    /// </summary>
    internal sealed class SIStoragePackageSource: PackageSource
    {
        private readonly Uri _packageUri = null;
        private readonly int _id;
        private readonly string _name;
        private readonly string _packageID;

        public override PackageSourceKey Key =>
            new PackageSourceKey
            {
                Type = PackageSourceTypes.SIStorage,
                Data = _packageUri.AbsoluteUri,
                ID = _id,
                Name = _name,
                PackageID = _packageID
            };

        public SIStoragePackageSource(Uri packageUri, int id, string name, string packageID)
        {
            _packageUri = packageUri;
            _id = id;
            _name = name;
            _packageID = packageID;
        }

        public override string Source => $"*{_name}";

        public override async Task<(string, bool)> GetPackageFileAsync(CancellationToken cancellationToken = default)
        {
            // TODO: rewrite to HttpClient
            var request = (HttpWebRequest)WebRequest.Create(_packageUri);
            request.UserAgent = $"{CommonSettings.AppName} {Assembly.GetExecutingAssembly().GetName().Version} ({Environment.OSVersion.VersionString})";

            var response = await request.GetResponseAsync();

            var fileName = Path.GetTempFileName();
            using (var fs = File.OpenWrite(fileName))
            using (var stream = response.GetResponseStream())
            {
                await stream.CopyToAsync(fs, 81920 /* default */, cancellationToken); 
            }

            return (fileName, true);
        }

        public override string GetPackageName() => null;

        public override Task<byte[]> GetPackageHashAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());

        public override string GetPackageId() => _packageID;
    }
}
