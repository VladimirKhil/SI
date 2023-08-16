using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

// TODO: show load progress

/// <summary>
/// Defines a view model that displays document load process.
/// </summary>
public sealed class DocumentLoaderViewModel : WorkspaceViewModel
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task<QDocument>? _loadTask;
    
    public override string Header => Resources.DocumentLoading;

    public string Title { get; private set; }

    public DocumentLoaderViewModel(string title) => Title = title;

    protected override void Dispose(bool disposing)
    {
        _cancellationTokenSource.Cancel();
        // TODO: await _loadTask if not null
        _cancellationTokenSource.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>
    /// Loads the document.
    /// </summary>
    /// <param name="loader">Loader that should return the document.</param>
    public async Task<QDocument> LoadAsync(Func<CancellationToken, Task<QDocument>> loader)
    {
        try
        {
            _loadTask = loader(_cancellationTokenSource.Token);

            var document = await _loadTask;

            OnNewItem(document);
            OnClosed();

            return document;
        }
        catch (Exception exc)
        {
            ErrorMessage = $"{Title}: {exc.Message}";
            throw;
        }
    }
}
