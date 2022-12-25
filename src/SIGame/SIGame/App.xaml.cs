using AppService.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using SI.GameResultService.Client;
using SI.GameServer.Client;
using SICore.PlatformSpecific;
using SIGame.Contracts;
using SIGame.Implementation;
using SIGame.ViewModel;
using SIStorageService.Client;
using SIStorageService.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Utils;
#if !DEBUG
using AppService.Client.Models;
using SICore;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
#endif

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;
        private IConfiguration _configuration;
        private ILogger<App> _logger;

#pragma warning disable IDE0052
        private readonly DesktopCoreManager _coreManager = new();
#pragma warning restore IDE0052

        private readonly DesktopManager _manager = new();

        private static readonly bool UseSignalRConnection = Environment.OSVersion.Version >= new Version(6, 2);

        /// <summary>
        /// Имя приложения
        /// </summary>
        public static string ProductName => "SIGame";

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            _host = new HostBuilder()
#if DEBUG
                .UseEnvironment("Development")
#endif
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    var env = context.HostingEnvironment;

                    configurationBuilder
                        .SetBasePath(context.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

                    _configuration = configurationBuilder.Build();
                })
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    NLog.LogManager.Configuration = new NLogLoggingConfiguration(hostingContext.Configuration.GetSection("NLog"));
                })
                .UseNLog()
                .Build();

            await _host.StartAsync();

            _manager.ServiceProvider = _host.Services;
            _logger = _host.Services.GetRequiredService<ILogger<App>>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddAppServiceClient(_configuration);
            services.AddSIGameServerClient(_configuration);
            services.AddGameResultServiceClient(_configuration);
            services.AddSIStorageServiceClient(_configuration);

            services.AddTransient(typeof(SIStorage));

            services.AddSingleton<IUIThreadExecutor>(_manager);
            services.AddTransient<IErrorManager, ErrorManager>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                UI.Initialize();

                CommonSettings.Default = LoadCommonSettings();
                UserSettings.Default = LoadUserSettings();

                if (UserSettings.Default.Language != null)
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(UserSettings.Default.Language);
                }
                else
                {
                    var currentLanguage = Thread.CurrentThread.CurrentUICulture.Name;
                    UserSettings.Default.Language = currentLanguage == "ru-RU" ? currentLanguage : "en-US";
                }

                if (e.Args.Length > 0)
                {
                    switch (e.Args[0])
                    {
                        case "/logs":
                            UserSettings.Default = LoadUserSettings();
                            GameCommands.OpenLogs.Execute(null);
                            break;

                        case "/feedback":
                            GameCommands.Comment.Execute(null);
                            break;

                        case "/help":
                            GameCommands.Help.Execute(1);
                            break;
                    }

                    Shutdown();
                    return;
                }

                if (Environment.OSVersion.Version < new Version(10, 0))
                {
                    try
                    {
                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                            | SecurityProtocolType.Tls11
                            | SecurityProtocolType.Tls12;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Trace.TraceInformation("Game launched");

                UserSettings.Default.UseSignalRConnection = UseSignalRConnection;
                UserSettings.Default.PropertyChanged += Default_PropertyChanged;

                MainWindow = new MainWindow
                {
                    DataContext = new MainViewModel(CommonSettings.Default, UserSettings.Default, _host.Services)
                };

#if !DEBUG
				if (UserSettings.Default.SearchForUpdates)
				{
					CheckUpdate();
				}

                var errorManager = _host.Services.GetRequiredService<IErrorManager>();
                errorManager.SendDelayedReports();
#endif

                MainWindow.Show();

                if (UserSettings.Default.FullScreen)
                {
                    ((MainWindow)MainWindow).Maximize();
                }
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_IncifficientResources,
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
            catch (System.Windows.Markup.XamlParseException)
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_NetBroken,
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) =>
            _logger.LogError("Common game error: {error}", e.ExceptionObject);

        private void Default_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserSettings.Sound))
            {
                if (!UserSettings.Default.Sound)
                {
                    _manager.PlaySoundInternal();
                }
            }
        }

#if !DEBUG
        private async void CheckUpdate()
        {
            try
            {
                var updateInfo = await SearchForUpdatesAsync();

                if (updateInfo != null)
                {
                    SearchForUpdatesFinished(updateInfo);
                }
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Update error: {error}", exc.Message);

                MessageBox.Show(
                    string.Format(SIGame.Properties.Resources.UpdateException, exc.Message),
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Произвести поиск обновлений
        /// </summary>
        /// <returns>Нужно ли завершить приложение для выполнения обновления</returns>
        private async Task<AppInfo> SearchForUpdatesAsync()
        {
            using var appService = _host.Services.GetRequiredService<IAppServiceClient>();

            var assembly = Assembly.GetAssembly(typeof(MainViewModel));

            if (assembly == null)
            {
                throw new Exception("assembly == null");
            }

            var currentVersion = assembly.GetName().Version;
            var product = await appService.GetProductAsync("SI");

            if (product?.Version > currentVersion)
            {
                return product;
            }

            return null;
        }

        private void SearchForUpdatesFinished(AppInfo updateInfo)
        {
            var updateUri = updateInfo.Uri;

            ((MainViewModel)MainWindow.DataContext).UpdateVersion = updateInfo.Version;
            ((MainViewModel)MainWindow.DataContext).Update = new CustomCommand(obj => Update_Executed(updateUri));
        }

        private bool _isUpdating = false;

        private async void Update_Executed(Uri updateUri)
        {
            if (_isUpdating)
            {
                return;
            }

            _isUpdating = true;

            try
            {
                var localFile = Path.Combine(Path.GetTempPath(), "SIGame.Setup.exe");

                using (var httpClient = new HttpClient { DefaultRequestVersion = HttpVersion.Version20 })
                using (var stream = await httpClient.GetStreamAsync(updateUri))
                using (var fs = File.Create(localFile))
                {
                    await stream.CopyToAsync(fs);
                }

                Process.Start(localFile, "/passive");
                Current.Shutdown();
            }
            catch (Exception exc)
            {
                _isUpdating = false;
                MessageBox.Show(exc.Message, ProductName, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
#endif

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (CommonSettings.Default != null)
            {
                SaveCommonSettings(CommonSettings.Default);
            }

            if (UserSettings.Default != null)
            {
                SaveUserSettings(UserSettings.Default);
            }

            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var inner = e.Exception;

            while (inner.InnerException != null)
            {
                inner = inner.InnerException;
            }

            if (inner is OutOfMemoryException)
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_IncifficientResourcesForExecution,
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return;
            }

            if (inner is System.Windows.Markup.XamlParseException
                || inner is System.Xaml.XamlParseException
                || inner is NotImplementedException
                || inner is TypeInitializationException
                || inner is FileFormatException
                || inner is SEHException)
            {
                MessageBox.Show(
                    $"{SIGame.Properties.Resources.Error_RuntimeBroken}: {inner.Message}",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                return;
            }

            if (inner is FileNotFoundException)
            {
                MessageBox.Show(
                    $"{SIGame.Properties.Resources.Error_FilesBroken}: {inner.Message}. {SIGame.Properties.Resources.TryReinstallApp}.",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (inner is COMException)
            {
                MessageBox.Show(
                    $"{SIGame.Properties.Resources.Error_DirectXBroken}: {inner.Message}.",
                    CommonSettings.AppName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (inner is FileLoadException
                || inner is IOException
                || inner is ArgumentOutOfRangeException && inner.Message.Contains("capacity"))
            {
                MessageBox.Show(inner.Message, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = e.Exception.ToString();

            if (message.Contains("System.Windows.Automation")
                || message.Contains("UIAutomationCore.dll")
                || message.Contains("UIAutomationTypes"))
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_WindowsAutomationBroken,
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (message.Contains("ApplyTaskbarItemInfo")
                || message.Contains("GetValueFromTemplatedParent")
                || message.Contains("IsBadSplitPosition")
                || message.Contains("IKeyboardInputProvider.AcquireFocus")
                || message.Contains("ReleaseOnChannel")
                || message.Contains("ManifestSignedXml2.GetIdElement"))
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_OSBroken,
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (message.Contains("ComputeTypographyAvailabilities") || message.Contains("FontList.get_Item"))
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.Error_Typography,
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            if (message.Contains("WpfXamlLoader.TransformNodes"))
            {
                MessageBox.Show(
                    SIGame.Properties.Resources.AppBroken,
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            var errorManager = _host.Services.GetRequiredService<IErrorManager>();
            errorManager.SendErrorReport(e.Exception);
            e.Handled = true;
        }

        /// <summary>
        /// Имя файла общих настроек
        /// </summary>
        internal const string CommonConfigFileName = "app.config";

        /// <summary>
        /// Имя файла персональных настроек
        /// </summary>
        internal static string UserConfigFileName = "user.config";

        public const string SettingsFolderName = "Settings";

        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CommonSettings.ManufacturerEn,
            CommonSettings.AppNameEn,
            SettingsFolderName);

        /// <summary>
        /// Загрузить общие настройки
        /// </summary>
        public static CommonSettings LoadCommonSettings()
        {
            try
            {
                var commonSettingsFile = Path.Combine(SettingsFolder, CommonConfigFileName);

                if (File.Exists(commonSettingsFile) && Monitor.TryEnter(CommonConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = File.Open(commonSettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return CommonSettings.Load(stream);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            $"{SIGame.Properties.Resources.Error_SettingsLoading}: {exc.Message}. {SIGame.Properties.Resources.DefaultSettingsWillBeUsed}",
                            ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                    finally
                    {
                        Monitor.Exit(CommonConfigFileName);
                    }
                }

                using var file = IsolatedStorageFile.GetMachineStoreForAssembly();

                if (file.FileExists(CommonConfigFileName) && Monitor.TryEnter(CommonConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = file.OpenFile(CommonConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return CommonSettings.Load(stream);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            $"{SIGame.Properties.Resources.Error_SettingsLoading}: {exc.Message}. {SIGame.Properties.Resources.DefaultSettingsWillBeUsed}",
                            ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                    finally
                    {
                        Monitor.Exit(CommonConfigFileName);
                    }
                }
                else
                {
                    var oldSettings = CommonSettings.LoadOld(CommonConfigFileName);

                    if (oldSettings != null)
                    {
                        return oldSettings;
                    }
                }
            }
            catch { }

            return new CommonSettings();
        }

        /// <summary>
        /// Сохранить общие настройки
        /// </summary>
        private static void SaveCommonSettings(CommonSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                var commonSettingsFile = Path.Combine(SettingsFolder, CommonConfigFileName);

                if (Monitor.TryEnter(CommonConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = File.Create(commonSettingsFile);
                        settings.Save(stream);
                    }
                    finally
                    {
                        Monitor.Exit(CommonConfigFileName);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"{SIGame.Properties.Resources.Error_SettingsSaving}: {exc.Message}",
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Загрузить общие настройки
        /// </summary>
        /// <returns></returns>
        public static UserSettings LoadUserSettings()
        {
            try
            {
                var userSettingsFile = Path.Combine(SettingsFolder, UserConfigFileName);

                if (File.Exists(userSettingsFile) && Monitor.TryEnter(UserConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = File.Open(userSettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return UserSettings.Load(stream);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            $"{SIGame.Properties.Resources.Error_SettingsLoading}: {exc.Message}. {SIGame.Properties.Resources.DefaultSettingsWillBeUsed}",
                            ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);
                    }
                    finally
                    {
                        Monitor.Exit(UserConfigFileName);
                    }
                }

                using var file = IsolatedStorageFile.GetUserStoreForAssembly();

                if (file.FileExists(UserConfigFileName) && Monitor.TryEnter(UserConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = file.OpenFile(UserConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return UserSettings.Load(stream);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(
                            $"{SIGame.Properties.Resources.Error_SettingsLoading}: {exc.Message}. {SIGame.Properties.Resources.DefaultSettingsWillBeUsed}",
                            ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation);

                    }
                    finally
                    {
                        Monitor.Exit(UserConfigFileName);
                    }
                }
                else
                {
                    var oldSettings = UserSettings.LoadOld(UserConfigFileName);

                    if (oldSettings != null)
                    {
                        return oldSettings;
                    }
                }
            }
            catch { }

            return new UserSettings();
        }

        /// <summary>
        /// Сохранить общие настройки
        /// </summary>
        private static void SaveUserSettings(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsFolder);
                var userSettingsFile = Path.Combine(SettingsFolder, UserConfigFileName);

                if (Monitor.TryEnter(UserConfigFileName, 2000))
                {
                    try
                    {
                        using var stream = File.Create(userSettingsFile);
                        settings.Save(stream);
                    }
                    finally
                    {
                        Monitor.Exit(UserConfigFileName);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"{SIGame.Properties.Resources.Error_SettingsSaving}: {exc.Message}", ProductName, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}
