using SICore.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;

namespace SICore.Clients.Viewer
{
    /// <summary>
    /// Downloads files in background and allows to access to them after download.
    /// </summary>
    internal sealed class LocalFileManager : IDisposable
    {
        private readonly HttpClient _client = new() { DefaultRequestVersion = HttpVersion.Version20 };

        private readonly string _rootFolder;

        private readonly object _globalLock = new();
        private readonly HashSet<string> _lockedFiles = new();

        public LocalFileManager()
        {
            _rootFolder = Path.Combine(Path.GetTempPath(), "SIGame", Guid.NewGuid().ToString());
        }

        public async void AddFile(Uri uri, Action<Exception> onError)
        {
            var fileName = Path.GetFileName(uri.ToString());
            var localFile = Path.Combine(_rootFolder, fileName);

            if (File.Exists(localFile))
            {
                return;
            }

            lock (_globalLock)
            {
                if (_lockedFiles.Contains(localFile))
                {
                    return;
                }

                _lockedFiles.Add(localFile);
            }

            try
            {
                Directory.CreateDirectory(_rootFolder);

                var response = await _client.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                {
                    onError(new Exception($"{Resources.DownloadFileError}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}"));
                    return;
                }

                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(localFile);
                await responseStream.CopyToAsync(fileStream);
            }
            finally
            {
                lock (_globalLock)
                {
                    _lockedFiles.Remove(localFile);
                }
            }
        }

        public string TryGetFile(Uri uri)
        {
            var fileName = Path.GetFileName(uri.ToString());
            var localFile = Path.Combine(_rootFolder, fileName);

            if (!File.Exists(localFile))
            {
                return null;
            }

            lock (_globalLock)
            {
                if (_lockedFiles.Contains(localFile))
                {
                    return null;
                }
            }

            return localFile;
        }

        public void Dispose()
        {
            try
            {
                _client.Dispose();

                if (Directory.Exists(_rootFolder))
                {
                    Directory.Delete(_rootFolder, true);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("LocalFileManager Dispose error: " + exc);
            }
        }
    }
}
