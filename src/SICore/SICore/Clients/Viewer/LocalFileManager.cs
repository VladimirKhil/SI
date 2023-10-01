using SICore.Properties;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace SICore.Clients.Viewer;

/// <inheritdoc cref="ILocalFileManager" />
internal sealed class LocalFileManager : ILocalFileManager
{
    private static readonly MD5 Hash = MD5.Create();

    private readonly HttpClient _client = new() { DefaultRequestVersion = HttpVersion.Version20 };

    private readonly string _rootFolder;

    private readonly object _globalLock = new();

    private readonly HashSet<string> _lockedFiles = new();

    private readonly Channel<FileTask> _processingQueue = Channel.CreateUnbounded<FileTask>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

    public event Action<Uri, Exception>? Error;

    public LocalFileManager()
    {
        var socketsHttpHandler = new SocketsHttpHandler
        {
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        _client = new(socketsHttpHandler) { DefaultRequestVersion = HttpVersion.Version20 };
        _rootFolder = Path.Combine(Path.GetTempPath(), "SIGame", Guid.NewGuid().ToString());
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (await _processingQueue.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_processingQueue.Reader.TryRead(out var fileTask))
                {
                    await ProcesFileAsync(fileTask, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public bool AddFile(Uri uri) => _processingQueue.Writer.TryWrite(new FileTask { Uri = uri });

    private async Task ProcesFileAsync(FileTask fileTask, CancellationToken cancellationToken)
    {
        var fileName = GetSafeFileName(fileTask.Uri);
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

            using var response = await _client.GetAsync(fileTask.Uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Error?.Invoke(
                    fileTask.Uri,
                    new Exception($"{Resources.DownloadFileError}: {response.StatusCode} {await response.Content.ReadAsStringAsync(cancellationToken)}"));

                return;
            }

            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = File.Create(localFile);
            await responseStream.CopyToAsync(fileStream, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exc)
        {
            Error?.Invoke(fileTask.Uri, exc);

            try
            {
                if (File.Exists(localFile))
                {
                    File.Delete(localFile);
                }
            }
            catch { }
        }
        finally
        {
            lock (_globalLock)
            {
                _lockedFiles.Remove(localFile);
            }
        }
    }

    public string? TryGetFile(Uri uri)
    {
        var fileName = GetSafeFileName(uri);
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

    private static string GetSafeFileName(Uri uri)
    {
        var fullUri = uri.ToString();
        var extension = Path.GetExtension(fullUri);
        var hashedFileName = Hash.ComputeHash(Encoding.UTF8.GetBytes(fullUri));
        var escapedFileName = Convert.ToBase64String(hashedFileName).Replace('/', '_').Replace('+', '-').Replace("=", "");
        return string.IsNullOrEmpty(extension) ? escapedFileName : Path.ChangeExtension(escapedFileName, extension);
    }

    public void Dispose()
    {
        try
        {
            try
            {
                _processingQueue.Writer.Complete();
            }
            catch (ChannelClosedException)
            {

            }

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

    private readonly struct FileTask
    {
        public Uri Uri { get; init; }
    }
}
