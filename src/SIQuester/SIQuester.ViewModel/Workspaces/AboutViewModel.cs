using SIQuester.ViewModel.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace SIQuester.ViewModel
{
    /// <summary>
    /// Информация о программе
    /// </summary>
    public sealed class AboutViewModel: WorkspaceViewModel
    {
        private const string _LicensesFolder = "Licenses";

        public override string Header => Resources.About;

        /// <summary>
        /// Версия программы
        /// </summary>
        public string Version
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        /// <summary>
        /// Перейти на сайт программы
        /// </summary>
        public ICommand OpenSite { get; private set; }
        public ICommand OpenIcons { get; private set; }
        public ICommand OpenIcons2 { get; private set; }

        private string _licenses;

        public string Licenses
        {
            get => _licenses;
            set
            {
                _licenses = value;
                OnPropertyChanged();
            }
        }

        public AboutViewModel()
        {
            OpenSite = new SimpleCommand(arg => GoToUrl("http://vladimirkhil.com/si/siquester"));
            OpenIcons = new SimpleCommand(arg => GoToUrl("http://www.famfamfam.com/lab/icons/silk"));
            OpenIcons2 = new SimpleCommand(arg => GoToUrl("http://modernuiicons.com"));

            LoadLicenses();
        }

        private async void LoadLicenses()
        {
            try
            {
                var licensesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _LicensesFolder);
                var licenseText = new StringBuilder();
                foreach (var file in new DirectoryInfo(licensesFolder).EnumerateFiles())
                {
                    licenseText.AppendLine(File.ReadAllText(file.FullName)).AppendLine();
                }

                Licenses = licenseText.ToString();
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }

        private void GoToUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception exc)
            {
                OnError(exc);
            }
        }
    }
}
