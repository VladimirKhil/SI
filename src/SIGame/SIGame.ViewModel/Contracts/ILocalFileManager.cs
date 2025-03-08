namespace SIGame.ViewModel.Contracts;

/// <summary>
/// Downloads files in background and allows to access to them after download.
/// </summary>
internal interface ILocalFileManager : IAsyncDisposable
{
    event Action<Uri, Exception>? Error;

    bool AddFile(Uri mediaUri, Action? onCompleted = null);

    string? TryGetFile(Uri mediaUri);
}
