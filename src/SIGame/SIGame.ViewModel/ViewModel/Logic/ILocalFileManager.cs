namespace SICore.Clients.Viewer;

/// <summary>
/// Downloads files in background and allows to access to them after download.
/// </summary>
internal interface ILocalFileManager : IDisposable
{
    event Action<Uri, Exception>? Error;

    Task StartAsync(CancellationToken token);

    bool AddFile(Uri mediaUri);

    string? TryGetFile(Uri mediaUri);
}
