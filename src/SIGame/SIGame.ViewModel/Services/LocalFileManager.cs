using SIGame.ViewModel.Contracts;
using System.Diagnostics;
using System.Threading.Channels;
using Utils;

namespace SIGame.ViewModel.Services;

/// <inheritdoc cref="ILocalFileManager" />
internal sealed class LocalFileManager : ILocalFileManager
{
    internal const string HttpClientName = "LocalFileManager";

    private readonly IHttpClientFactory _clientFactory;

    private readonly Task _localFileManagerTask;
    private readonly CancellationTokenSource _cancellation = new();

    private readonly string _rootFolder;

    private readonly object _globalLock = new();

    private readonly HashSet<string> _lockedFiles = new();

    private int _disposed;

    private readonly Channel<FileTask> _processingQueue = Channel.CreateUnbounded<FileTask>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = true
        });

    public event Action<Uri, Exception>? Error;

    public LocalFileManager(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
        _rootFolder = Path.Combine(Path.GetTempPath(), "SIGame", Guid.NewGuid().ToString());
        _localFileManagerTask = StartAsync(_cancellation.Token);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            while (await _processingQueue.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (_processingQueue.Reader.TryRead(out var fileTask))
                {
                    if (await ProcesFileAsync(fileTask, cancellationToken).ConfigureAwait(false))
                    {
                        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public bool AddFile(Uri uri, Action? onCompleted = null, CancellationToken cancellationToken = default) =>
        !cancellationToken.IsCancellationRequested &&
        _processingQueue.Writer.TryWrite(new FileTask { Uri = uri, OnCompleted = onCompleted, CancellationToken = cancellationToken });

    private async Task<bool> ProcesFileAsync(FileTask fileTask, CancellationToken cancellationToken)
    {
        if (fileTask.CancellationToken.IsCancellationRequested)
        {
            return false;
        }

        var fileName = FilePathHelper.GetSafeFileName(fileTask.Uri);
        var localFile = Path.Combine(_rootFolder, fileName);
        var canceled = false;

        try
        {
            if (File.Exists(localFile))
            {
                return false;
            }

            lock (_globalLock)
            {
                if (_lockedFiles.Contains(localFile))
                {
                    return false;
                }

                _lockedFiles.Add(localFile);
            }

            try
            {
                Directory.CreateDirectory(_rootFolder);

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, fileTask.CancellationToken);
                var client = _clientFactory.CreateClient(HttpClientName);

                using var response = await client.GetAsync(fileTask.Uri, linkedCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    Error?.Invoke(
                        fileTask.Uri,
                        new Exception($"{SIGame.ViewModel.Properties.Resources.DownloadFileError}: {response.StatusCode} {await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false)}"));

                    return true;
                }

                using var responseStream = await response.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
                using var fileStream = File.Create(localFile);
                await responseStream.CopyToAsync(fileStream, linkedCts.Token).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || fileTask.CancellationToken.IsCancellationRequested)
            {
                canceled = true;

                try
                {
                    if (File.Exists(localFile))
                    {
                        File.Delete(localFile);
                    }
                }
                catch { }

                return false;
            }
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

                return true;
            }
            finally
            {
                lock (_globalLock)
                {
                    _lockedFiles.Remove(localFile);
                }
            }
        }
        finally
        {
            if (!canceled && !fileTask.CancellationToken.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                try { fileTask.OnCompleted?.Invoke(); } catch { /* Ignore */ }
            }
        }
    }

    public string? TryGetFile(Uri uri)
    {
        var fileName = FilePathHelper.GetSafeFileName(uri);
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

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        try
        {
            _cancellation.Cancel();
            await _localFileManagerTask.ConfigureAwait(false);
            _cancellation.Dispose();

            try
            {
                _processingQueue.Writer.Complete();
            }
            catch (ChannelClosedException)
            {

            }

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

        public Action? OnCompleted { get; init; }

        public CancellationToken CancellationToken { get; init; }
    }
}
