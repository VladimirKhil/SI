using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel;

// TODO: show load progress

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

    public async Task<QDocument> LoadAsync(Func<CancellationToken, Task<QDocument>> loader)
    {
        try
        {
            _loadTask = loader(_cancellationTokenSource.Token);

            var qDocument = await _loadTask;

            OnNewItem(qDocument);
            OnClosed();

            return qDocument;
        }
        catch (Exception exc)
        {
            ErrorMessage = $"{Title}: {exc.Message}";
            throw;
        }
    }
}
