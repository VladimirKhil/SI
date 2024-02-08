namespace SICore.Clients.Viewer;

/// <summary>
/// Downloads files in background and allows to access to them after download.
/// </summary>
internal interface ILocalFileManager : IAsyncDisposable
{
    event Action<Uri, Exception>? Error;

    bool AddFile(Uri mediaUri);

    string? TryGetFile(Uri mediaUri);
}
