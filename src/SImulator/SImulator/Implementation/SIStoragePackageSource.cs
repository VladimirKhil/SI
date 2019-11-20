using Services.SI;
using SImulator.Implementation.WinAPI;
using SImulator.ViewModel;
using SImulator.ViewModel.PlatformSpecific;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;

namespace SImulator.Implementation
{
    internal sealed class SIStoragePackageSource : IPackageSource
    {
        private readonly PackageInfo _package;
        private readonly Uri _packageUri;

        public string Name => _package.Description;

        public string Token => "";

        public SIStoragePackageSource(PackageInfo package, Uri packageUri)
        {
            _package = package;
            _packageUri = packageUri;
        }

        public async Task<Stream> GetPackageAsync()
        {
            ProgressDialog progress = null;
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    progress = new ProgressDialog() { Title = MainViewModel.ProductName };

                    progress.SetLine(1, "Загрузка файла…", false);
                    progress.Start(ProgressDialog.ProgressDialogFlags.MarqueeProgress | ProgressDialog.ProgressDialogFlags.NoCancel | ProgressDialog.ProgressDialogFlags.NoMinimize);
                }

                var request = WebRequest.Create(_packageUri);

                var stream = new MemoryStream();
                using (var response = await request.GetResponseAsync())
                using (var s = response.GetResponseStream())
                {
                    await s.CopyToAsync(stream);
                }

                return stream;

            }
            catch (Exception exc)
            {
                if (progress != null)
                {
                    progress.Stop();
                    progress = null;
                }

                MessageBox.Show(string.Format("Ошибка работы с библиотекой вопросов: {0}", exc.ToString()), MainViewModel.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            finally
            {
                if (progress != null)
                    progress.Stop();
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
