using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Data;
using System.ComponentModel;
using SIQuester.Model;
using System.ServiceModel;
using System.Net;
using SIQuester.ViewModel.Core;
using Services.SI.ViewModel;
using SIQuester.ViewModel.Properties;

namespace SIQuester.ViewModel
{
    public sealed class ImportSIStorageViewModel: WorkspaceViewModel
    {
        public SIStorageNew Storage { get; } = new SIStorageNew();

        public override string Header
        {
            get { return Resources.SIStorage; }
        }

        private readonly StorageContextViewModel _storageContextViewModel;

        public ImportSIStorageViewModel(StorageContextViewModel storageContextViewModel)
        {
            _storageContextViewModel = storageContextViewModel;

            Storage.Error += OnError;
            Storage.Open();
        }

        public void Select()
        {
            async Task<QDocument> loader()
            {
                var packageUri = await Storage.LoadSelectedPackageUriAsync();

                var ms = new MemoryStream();

                var request = WebRequest.Create(packageUri);
                var response = await request.GetResponseAsync();
                using (var responseStream = response.GetResponseStream())
                {
                    await responseStream.CopyToAsync(ms);
                }

                ms.Position = 0;
                var doc = SIPackages.SIDocument.Load(ms);
                return new QDocument(doc, _storageContextViewModel) { FileName = doc.Package.Name };
            };

            OnNewItem(new DocumentLoaderViewModel(Storage.CurrentPackage.Description, loader));
        }
    }
}
