using SIGame.ViewModel.Contracts;
using System.Diagnostics;
using System.Threading.Channels;
using Utils;

namespace SIGame.ViewModel.Services;

/// <inheritdoc cref="ILocalFileManager" />
internal sealed class LocalFileManager : ILocalFileManager
{
    private readonly HttpClient _client;

    private readonly Task _localFileManagerTask;
    private readonly CancellationTokenSource _cancellation = new();

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

    public LocalFileManager(HttpClient client)
    {
        _client = client;
        _rootFolder = Path.Combine(Path.GetTempPath(), "SIGame", Guid.NewGuid().ToString());
        _localFileManagerTask = StartAsync(_cancellation.Token);
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
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public bool AddFile(Uri uri, Action? onCompleted = null) =>
        _processingQueue.Writer.TryWrite(new FileTask { Uri = uri, OnCompleted = onCompleted });

    private async Task ProcesFileAsync(FileTask fileTask, CancellationToken cancellationToken)
    {
        var fileName = FilePathHelper.GetSafeFileName(fileTask.Uri);
        var localFile = Path.Combine(_rootFolder, fileName);

        try
        {
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
                        new Exception($"{SIGame.ViewModel.Properties.Resources.DownloadFileError}: {response.StatusCode} {await response.Content.ReadAsStringAsync(cancellationToken)}"));

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
        finally
        {
            try { fileTask.OnCompleted?.Invoke(); } catch { /* Ignore */ }
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cancellation.Cancel();
            await _localFileManagerTask;
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
    }
}
