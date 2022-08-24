using SIQuester.ViewModel.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    // TODO: show load progress

    public sealed class DocumentLoaderViewModel : WorkspaceViewModel
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        
        public override string Header => Resources.DocumentLoading;

        public string Title { get; private set; }

        public DocumentLoaderViewModel(string title, Func<CancellationToken, Task<QDocument>> loader, Action onSuccess = null)
        {
            Title = title;

            Load(loader, onSuccess);
        }

        protected override void Dispose(bool disposing)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            base.Dispose(disposing);
        }

        private async void Load(Func<CancellationToken, Task<QDocument>> loader, Action onSuccess = null)
        {
            try
            {
                var qDocument = await loader(_cancellationTokenSource.Token);
                onSuccess?.Invoke();

                OnNewItem(qDocument);
                OnClosed();
            }
            catch (Exception exc)
            {
                ErrorMessage = $"{Title}: {exc.Message}";
            }
        }
    }
}
