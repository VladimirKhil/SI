using SImulator.Implementation;
using SImulator.ViewModel;
using SIUI.ViewModel.Core;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;
using Settings = SImulator.ViewModel.Model.AppSettings;

namespace SImulator
{
    /// <summary>
    /// Provides interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
#pragma warning disable IDE0052
        private readonly DesktopManager _manager = new();
#pragma warning restore IDE0052

        /// <summary>
        /// User settings configuration file name.
        /// </summary>
        private const string ConfigFileName = "user.config";

        internal Settings Settings { get; } = LoadSettings();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            UI.Initialize();

            var main = new MainViewModel(Settings);

            if (e.Args.Length > 0)
            {
                main.PackageSource = new FilePackageSource(e.Args[0]);
            }

#if DEBUG
            main.PackageSource = new SIStoragePackageSource(
                new Services.SI.PackageInfo
                {
                    Description = SImulator.Properties.Resources.TestPackage
                },
                new Uri("https://vladimirkhil.com/sistorage/Основные/1.siq"));
#endif

            MainWindow = new CommandWindow { DataContext = main };
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {            
            SaveSettings(Settings);           

            base.OnExit(e);
        }

        private static void SaveSettings(Settings settings)
        {
            try
            {
                if (Monitor.TryEnter(ConfigFileName, 2000))
                {
                    try
                    {
                        using var file = IsolatedStorageFile.GetUserStoreForAssembly();
                        using var stream = new IsolatedStorageFileStream(ConfigFileName, FileMode.Create, file);
                        settings.Save(stream, DesktopManager.SettingsSerializer);
                    }
                    finally
                    {
                        Monitor.Exit(ConfigFileName);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"{SImulator.Properties.Resources.SavingSettingsError}: {exc.Message}",
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Loads user settings.
        /// </summary>
        public static Settings LoadSettings()
        {
            try
            {
                using var file = IsolatedStorageFile.GetUserStoreForAssembly();
                if (file.FileExists(ConfigFileName) && Monitor.TryEnter(ConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = file.OpenFile(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return Settings.Load(stream, DesktopManager.SettingsSerializer);
                    }
                    catch { }
                    finally
                    {
                        Monitor.Exit(ConfigFileName);
                    }
                }
            }
            catch { }

            return Settings.Create();
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var msg = e.Exception.ToString();

            if (msg.Contains("WmClose")) // Normal closing, it's ok
            {
                return;
            }

            if (e.Exception is OutOfMemoryException)
            {
                MessageBox.Show(
                    SImulator.Properties.Resources.OutOfMemoryError,
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else if (e.Exception is IOException ioException && IsDiskFullError(ioException))
            {
                MessageBox.Show(
                    SImulator.Properties.Resources.DiskFullError,
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else if (e.Exception is System.Windows.Markup.XamlParseException || e.Exception is NotImplementedException)
            {
                MessageBox.Show(
                    string.Format(SImulator.Properties.Resources.RuntimeBrokenError, e.Exception),
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                MessageBox.Show(
                    string.Format(SImulator.Properties.Resources.CommonAppError, e.Exception.Message),
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            e.Handled = true;
            Shutdown();
        }

        private static bool IsDiskFullError(Exception ex)
        {
            const int HR_ERROR_HANDLE_DISK_FULL = unchecked((int)0x80070027);
            const int HR_ERROR_DISK_FULL = unchecked((int)0x80070070);

            return ex.HResult == HR_ERROR_HANDLE_DISK_FULL
                || ex.HResult == HR_ERROR_DISK_FULL;
        }
    }
}
