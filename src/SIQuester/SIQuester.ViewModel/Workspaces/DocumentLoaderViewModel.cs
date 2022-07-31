using SIQuester.ViewModel.Properties;
using System;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public sealed class DocumentLoaderViewModel : WorkspaceViewModel
    {
        public override string Header => Resources.DocumentLoading;

        public string Title { get; private set; }

        private string _errorMessage;

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public DocumentLoaderViewModel(string title, Func<Task<QDocument>> loader, Action onSuccess = null)
        {
            Title = title;

            Load(loader, onSuccess);
        }

        private async void Load(Func<Task<QDocument>> loader, Action onSuccess = null)
        {
            try
            {
                var qDocument = await loader();
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
