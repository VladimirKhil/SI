using Services.SI.ViewModel;
using SIQuester.ViewModel.Properties;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SIQuester.ViewModel
{
    public sealed class ImportSIStorageViewModel: WorkspaceViewModel
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public SIStorageNew Storage { get; } = new SIStorageNew();

        public override string Header => Resources.SIStorage;

        private readonly StorageContextViewModel _storageContextViewModel;

        public ImportSIStorageViewModel(StorageContextViewModel storageContextViewModel)
        {
            _storageContextViewModel = storageContextViewModel;

            Storage.Error += OnError;
            Storage.Open();
        }

        public void Select()
        {
            if (Storage.CurrentPackage == null)
            {
                return;
            }

            async Task<QDocument> loader()
            {
                var packageUri = await Storage.LoadSelectedPackageUriAsync();

                var ms = new MemoryStream();

                using var response = await HttpClient.GetAsync(packageUri);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }

                await response.Content.CopyToAsync(ms);

                ms.Position = 0;
                var doc = SIPackages.SIDocument.Load(ms);

                return new QDocument(doc, _storageContextViewModel) { FileName = doc.Package.Name };
            };

            OnNewItem(new DocumentLoaderViewModel(Storage.CurrentPackage.Description, loader));
        }
    }
}
