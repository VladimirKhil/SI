using Microsoft.Extensions.Logging;
using SIQuester.ViewModel.Properties;
using SIStorageService.ViewModel;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public sealed class ImportSIStorageViewModel : WorkspaceViewModel
    {
        private static readonly HttpClient HttpClient = new() { DefaultRequestVersion = HttpVersion.Version20 };

        public SIStorage Storage { get; }

        public override string Header => Resources.SIStorage;

        private readonly StorageContextViewModel _storageContextViewModel;

        public bool IsProgress => Storage.IsLoading || Storage.IsLoadingPackages;

        private readonly ILoggerFactory _loggerFactory;

        public ImportSIStorageViewModel(
            StorageContextViewModel storageContextViewModel,
            SIStorage siStorage,
            ILoggerFactory loggerFactory)
        {
            _storageContextViewModel = storageContextViewModel;
            _loggerFactory = loggerFactory;

            Storage = siStorage;

            Storage.Error += OnError;
            Storage.PropertyChanged += Storage_PropertyChanged;
            Storage.Open();
        }

        private void Storage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SIStorage.IsLoading) || e.PropertyName == nameof(SIStorage.IsLoadingPackages))
            {
                OnPropertyChanged(nameof(IsProgress));
            }
        }

        public void Select()
        {
            if (Storage.CurrentPackage == null)
            {
                return;
            }

            async Task<QDocument> loader(CancellationToken cancellationToken)
            {
                var packageLink = await Storage.LoadSelectedPackageUriAsync(cancellationToken);

                var ms = new MemoryStream();

                using var response = await HttpClient.GetAsync(packageLink.Uri, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync(cancellationToken));
                }

                await response.Content.CopyToAsync(ms, cancellationToken);

                ms.Position = 0;
                var doc = SIPackages.SIDocument.Load(ms);

                return new QDocument(doc, _storageContextViewModel, _loggerFactory) { FileName = doc.Package.Name };
            };

            OnNewItem(new DocumentLoaderViewModel(Storage.CurrentPackage.Description, loader));
        }
    }
}
