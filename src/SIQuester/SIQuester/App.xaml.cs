#if !DEBUG
using Microsoft.WindowsAPICodePack.Dialogs;
#endif
using SIQuester.Model;
using SIQuester.ViewModel;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace SIQuester
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Имя конфигурационного файла пользовательских настроек
        /// </summary>
        private const string ConfigFileName = "user.config";

        private readonly Implementation.DesktopManager _manager = new Implementation.DesktopManager();

        /// <summary>
        /// Используется ли версия Windows от Vista и выше
        /// </summary>
        public static bool IsVistaOrLater = Environment.OSVersion.Version.Major >= 6;

        public static bool IsWindows8_1OrLater = Environment.OSVersion.Version > new Version(6, 2);

        /// <summary>
        /// Имя приложения
        /// </summary>
        public static string ProductName => Assembly.GetExecutingAssembly().GetName().Name;

        /// <summary>
        /// Директория приложения
        /// </summary>
        public static string StartupPath => AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// Путь к исполняемому файлу приложения
        /// </summary>
        public static string ExecutablePath => Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Необходимый заголовок для WebRequest'ов и WebClient'ов
        /// </summary>
        public static string UserAgentHeader => $"{ProductName} {Assembly.GetExecutingAssembly().GetName().Version} ({Environment.OSVersion.VersionString})";
        
        private bool _hasError = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                AppSettings.Default = LoadSettings();

                if (!IsWindows8_1OrLater)
                    AppSettings.Default.SpellChecking = false;

                if (e.Args.Length > 0)
                {
                    if (e.Args[0] == "backup")
                    {
                        // Бэкап хранилища вопросов
                        var folder = e.Args[1];
                        Backup(folder);
                        return;
                    }
                }

#if !DEBUG
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
#endif

                MainWindow = new MainWindow { DataContext = new MainViewModel(e.Args) };
                MainWindow.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка при запуске программы: {exc.Message}.\r\nПрограмма будет закрыта. При повторном возникновении этой ошибки обратитесь к разработчику.", ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async void Backup(string folder)
        {
            int code = 0;
            try
            {
                var directoryInfo = new DirectoryInfo(folder);
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }

                var service = new Services.SI.SIStorageServiceClient();
                var packages = await service.GetPackagesAsync();
                using (var client = new HttpClient())
                {
                    foreach (var package in packages)
                    {
                        var uri = await service.GetPackageByIDAsync(package.ID);
                        var fileName = Path.GetFileName(Uri.UnescapeDataString(uri.ToString()));

                        var targetFile = Path.Combine(folder, fileName);
                        using (var stream = await client.GetStreamAsync(uri))
                        using (var fileStream = File.Create(targetFile))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.Write(exc.ToString());
                code = 1;
            }
            finally
            {
                Environment.Exit(code);
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (_hasError)
                return;

            _hasError = true;

            if (e.Exception is OutOfMemoryException)
            {
                MessageBox.Show("Недостаточно памяти для выполнения программы!", ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception is Win32Exception || e.Exception is NotImplementedException || e.Exception.ToString().Contains("VerifyNotClosing"))
            {
                if (e.Exception.Message != "Параметр задан неверно")
                    MessageBox.Show(string.Format("Ошибка выполнения программы: {0}!", e.Exception.Message), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception is InvalidOperationException && e.Exception.Message.Contains("Идет завершение работы объекта Application"))
            {
                // Это нормально, ничего не сделаешь
            }
            else if (e.Exception is BadImageFormatException)
            {
                MessageBox.Show(string.Format("Ошибка запуска программы: {0}!", e.Exception.Message), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception.ToString().Contains("MediaPlayerState.OpenMedia"))
            {
                MessageBox.Show(string.Format("Некорректный адрес мультимедиа. Программа аварийно завершена с ошибкой: {0}!", e.Exception.Message), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception is COMException || e.Exception.ToString().Contains("UpdateTaskbarProgressState") || e.Exception.ToString().Contains("FindNameInTemplateContent"))
            {
                MessageBox.Show(string.Format("Ошибка выполнения программы: {0}!", e.ToString()), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception.ToString().Contains("MahApps.Metro"))
            {
                // Ничего не сделаешь
            }
            else if (e.Exception is InvalidOperationException invalidOperationException
                && (invalidOperationException.Message.Contains("Невозможно выполнить эту операцию, когда привязка отсоединена")
                || invalidOperationException.Message.Contains("Cannot perform this operation when binding is detached")))
            {
                MessageBox.Show(invalidOperationException.Message, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (e.Exception.Message.Contains("System.Windows.Automation")
                || e.Exception.Message.Contains("UIAutomationCore.dll")
                || e.Exception.Message.Contains("UIAutomationTypes"))
            {
                MessageBox.Show(
                    "Ошибка старта приложения, связанная с Windows Automation." +
                    " Попробуйте установить обновления для своей операционной системы, для .NET Framework 4" +
                    " или установить библиотеку API Windows Automation (ссылка находится на странице приложения).",
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            else
            {
                var exception = e.Exception;
                var message = new StringBuilder();
                var systemMessage = new StringBuilder();
                var version = Assembly.GetExecutingAssembly().GetName().Version;

                while (exception != null)
                {
                    message.AppendLine(exception.Message).AppendLine();
                    systemMessage.AppendLine(exception.ToString()).AppendLine();
                    exception = exception.InnerException;
                }

                var errorInfo = new ErrorInfo { Time = DateTime.Now, Version = version, Error = systemMessage.ToString() };
#if !DEBUG
            if (App.IsVistaOrLater)
            {
                    using (var dialog = new TaskDialog { Caption = App.ProductName, InstructionText = "Серьёзный сбой в приложении. Отправить отчёт автору?", Text = message.ToString().Trim(), Icon = TaskDialogStandardIcon.Warning, StandardButtons = TaskDialogStandardButtons.Ok })
                    {
                        dialog.Show();
                    }
            }
            else
#endif
                {
                    MessageBox.Show($"{SIQuester.Properties.Resources.SendErrorHeader}{Environment.NewLine}{Environment.NewLine}{message.ToString().Trim()}", ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }

            e.Handled = true;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (AppSettings.Default != null)
                {
                    SaveSettings(AppSettings.Default);
                }
                _manager.Dispose();
            }
            catch (Exception exc)
            {
                MessageBox.Show(string.Format("Ошибка сохранения настроек при выходе: {0}.", exc), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnExit(e);
        }

        private static void SaveSettings(AppSettings settings)
        {
            try
            {
                if (Monitor.TryEnter(ConfigFileName, 2000))
                {
                    try
                    {
                        using (var file = IsolatedStorageFile.GetUserStoreForAssembly())
                        {
                            using (var stream = new IsolatedStorageFileStream(ConfigFileName, FileMode.Create, file))
                            {
                                settings.Save(stream);
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(ConfigFileName);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Ошибка при сохранении настроек программы: " + exc.Message, AppSettings.ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Загрузить пользовательские настройки
        /// </summary>
        /// <returns></returns>
        public static AppSettings LoadSettings()
        {
            try
            {
                using (var file = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (file.FileExists(ConfigFileName) && Monitor.TryEnter(ConfigFileName, 2000))
                    {
                        try
                        {
                            using (var stream = file.OpenFile(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                return AppSettings.Load(stream);
                            }
                        }
                        catch { }
                        finally
                        {
                            Monitor.Exit(ConfigFileName);
                        }
                    }
                }
            }
            catch { }

            return new AppSettings();
        }
    }
}
