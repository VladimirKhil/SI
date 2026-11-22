namespace SIGame.ViewModel.Contracts;

/// <summary>
/// Downloads files in background and allows to access to them after download.
/// </summary>
internal interface ILocalFileManager : IAsyncDisposable, IDisposable
{
    event Action<Uri, Exception>? Error;

    bool AddFile(Uri mediaUri, Action? onCompleted = null, CancellationToken cancellationToken = default);

    string? TryGetFile(Uri mediaUri);
}
