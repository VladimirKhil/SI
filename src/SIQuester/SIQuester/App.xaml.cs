using AppService.Client;
using AppService.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;
using NLog.Web;
using SIPackages;
using SIQuester.Model;
using SIQuester.Services.Host;
using SIQuester.ViewModel;
using SIQuester.ViewModel.Configuration;
using SIQuester.ViewModel.Contracts;
using SIQuester.ViewModel.Contracts.Host;
using SIQuester.ViewModel.Helpers;
using SIQuester.ViewModel.Services;
using SIStorageService.Client;
using SIStorageService.ViewModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xaml;
#if !DEBUG
using Microsoft.WindowsAPICodePack.Dialogs;
#endif

namespace SIQuester;

/// <summary>
/// Provides interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private bool _useAppService;

    /// <summary>
    /// Имя конфигурационного файла пользовательских настроек
    /// </summary>
    private const string ConfigFileName = "user.config";

    private readonly Implementation.DesktopManager _manager = new();

    public static bool IsWindows8_1OrLater = Environment.OSVersion.Version > new Version(6, 2);

    /// <summary>
    /// Имя приложения
    /// </summary>
    public static string ProductName => Assembly.GetExecutingAssembly().GetName().Name ?? "SIQuester";

    /// <summary>
    /// Директория приложения
    /// </summary>
    public static string StartupPath => AppDomain.CurrentDomain.BaseDirectory;

    private MainViewModel? _mainViewModel;

    private bool _hasError = false;

    private ILogger<App>? _logger;


    private DispatcherTimer? _autoSaveTimer;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        AppSettings.Default = LoadSettings();

        if (!IsWindows8_1OrLater)
        {
            AppSettings.Default.SpellChecking = false;
        }

        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        _host = new HostBuilder()
#if DEBUG
            .UseEnvironment("Development")
#endif
            .ConfigureAppConfiguration((context, configurationBuilder) =>
                configurationBuilder
                    .SetBasePath(context.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true))
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging((hostingContext, logging) =>
            {
                NLog.LogManager.Configuration = new NLogLoggingConfiguration(hostingContext.Configuration.GetSection("NLog"));
            })
            .UseNLog()
            .Build();

        await _host.StartAsync();

        _manager.ServiceProvider = _host.Services;

        var appServiceClientOptions = _host.Services.GetRequiredService<IOptions<AppServiceClientOptions>>().Value;
        _useAppService = appServiceClientOptions.ServiceUri != null;

        if (e.Args.Length > 0)
        {
            if (e.Args[0] == "backup")
            {
                // Бэкап хранилища вопросов
                var folder = e.Args[1];
                Backup(folder);
                return;
            }
            else if (e.Args[0] == "upgrade" && e.Args.Length > 1)
            {
                UpgradePackage(e.Args[1]);
                Current.Shutdown();
                return;
            }
        }

#if !DEBUG
        if (AppSettings.Default.SearchForUpdates)
        {
            SearchForUpdatesAsync();
        }

        SendDelayedReports();
#endif

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("Application started. Version: {version}", Assembly.GetExecutingAssembly().GetName().Version);
    }

    private static void UpgradePackage(string packagePath)
    {
        using var fs = File.Open(packagePath, FileMode.Open);
        using var doc = SIDocument.Load(fs, false);
        doc.Upgrade();
        doc.Save();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            if (AppSettings.Default.Language != null)
            {
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(AppSettings.Default.Language);
            }
            else
            {
                var currentLanguage = Thread.CurrentThread.CurrentUICulture.Name;
                AppSettings.Default.Language = currentLanguage == "ru-RU" ? currentLanguage : "en-US";
            }

            var siStorageClient = _host.Services.GetRequiredService<ISIStorageServiceClient>();
            var clipboardService = _host.Services.GetRequiredService<IClipboardService>();
            var options = _host.Services.GetRequiredService<IOptions<AppOptions>>();
            var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();

            _mainViewModel = new MainViewModel(e.Args, options.Value, siStorageClient, clipboardService, _host.Services, loggerFactory);

            MainWindow = new MainWindow { DataContext = _mainViewModel };
            MainWindow.Show();

            if (AppSettings.Default.AutoSave)
            {
                _autoSaveTimer = new DispatcherTimer(AppSettings.AutoSaveInterval, DispatcherPriority.Background, AutoSave, Dispatcher);
            }
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"{SIQuester.Properties.Resources.AppRunError}: {exc.Message}.\r\n{SIQuester.Properties.Resources.AppWillBeClosed}",
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
        }
    }

    private void AutoSave(object? sender, EventArgs args) => _mainViewModel?.AutoSave();

    private void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        services.AddAppServiceClient(ctx.Configuration);
        services.AddSIStorageServiceClient(ctx.Configuration);
        services.AddChgkServiceClient(ctx.Configuration);
        services.AddSingleton(AppSettings.Default);
        services.AddSingleton<IPackageTemplatesRepository, PackageTemplatesRepository>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddTransient<SIStorage>();
        services.Configure<AppOptions>(ctx.Configuration.GetSection(AppOptions.ConfigurationSectionName));
    }

    /// <summary>
    /// Backups SIStorage to folder. This method is called from the console.
    /// </summary>
    /// <param name="folder">Folder to backup.</param>
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

            var siStorageClient = _host.Services.GetRequiredService<ISIStorageServiceClient>();
            var packages = await siStorageClient.GetPackagesAsync();
            using var client = new HttpClient { DefaultRequestVersion = HttpVersion.Version20 };

            foreach (var package in packages)
            {
                var link = await siStorageClient.GetPackageByGuid2Async(package.Guid);
                var fileName = Path.GetFileName(link.Name);

                var targetFile = Path.Combine(folder, fileName);
                using var stream = await client.GetStreamAsync(link.Uri);
                using var fileStream = File.Create(targetFile);
                await stream.CopyToAsync(fileStream);
            }
        }
        catch (Exception exc)
        {
            Console.Write($"Backup error: {exc}");
            code = 1;
        }
        finally
        {
            Environment.Exit(code);
        }
    }

#if !DEBUG
    private async void SendDelayedReports()
    {
        if (!_useAppService)
        {
            return;
        }

        using var appService = _host.Services.GetRequiredService<IAppServiceClient>();

        try
        {
            while (AppSettings.Default.DelayedErrors.Count > 0)
            {
                var error = AppSettings.Default.DelayedErrors[0];
                var errorInfo = (Model.ErrorInfo)XamlServices.Load(error);

                await appService.SendErrorReportAsync("SIQuester", errorInfo.Error, errorInfo.Version, errorInfo.Time);
                AppSettings.Default.DelayedErrors.RemoveAt(0);
            }
        }
        catch
        {
        }
    }

    private async void SearchForUpdatesAsync()
    {
        var close = await SearchForUpdatesNewAsync();
        if (close)
        {
            Shutdown();
        }
    }

    /// <summary>
    /// Произвести поиск и установку обновлений
    /// </summary>
    /// <returns>Нужно ли завершить приложение для выполнения обновления</returns>
    private async Task<bool> SearchForUpdatesNewAsync(CancellationToken cancellationToken = default)
    {
        if (!_useAppService)
        {
            return false;
        }

        using var appService = _host.Services.GetRequiredService<IAppServiceClient>();

        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            var product = await appService.GetProductAsync("SIQuester", cancellationToken);

            if (product.Version > currentVersion)
            {
                _logger.LogInformation(
                    "Update detected. Current version: {currentVersion}. Product version: {productVersion}. Product uri: {productUri}",
                    currentVersion,
                    product.Version,
                    product.Uri);

                var updateUri = product.Uri;

                var localFile = Path.Combine(Path.GetTempPath(), "setup.exe");

                using (var httpClient = new HttpClient { DefaultRequestVersion = HttpVersion.Version20 })
                using (var stream = await httpClient.GetStreamAsync(updateUri, cancellationToken))
                using (var fs = File.Create(localFile))
                {
                    await stream.CopyToAsync(fs, cancellationToken);
                }

                try
                {
                    Process.Start(localFile, "/passive");
                }
                catch (Win32Exception)
                {
                    Thread.Sleep(10000); // Иногда проверяется антивирусом и не сразу запускается
                    Process.Start(localFile);
                }

                return true;
            }

        }
        catch (Exception exc)
        {
            MessageBox.Show(
                string.Format(SIQuester.Properties.Resources.UpdateException, exc.Message),
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        return false;
    }

#endif

    private async void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_hasError)
        {
            return;
        }

        _hasError = true;

        _logger?.LogError(e.Exception, "Application error: {message}", e.Exception.Message);

        if (e.Exception is OutOfMemoryException)
        {
            MessageBox.Show(
                SIQuester.Properties.Resources.NotEnoughMemoryToStartApp,
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception is Win32Exception || e.Exception is NotImplementedException || e.Exception.ToString().Contains("VerifyNotClosing"))
        {
            if (e.Exception.Message != SIQuester.Properties.Resources.InvalidParameter)
            {
                MessageBox.Show(
                    string.Format("{0}: {1}", SIQuester.Properties.Resources.CommonAppError, e.Exception.Message),
                    ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        else if (e.Exception is InvalidOperationException
            && e.Exception.Message.Contains(SIQuester.Properties.Resources.ApplicationIsClosing))
        {
            // Это нормально, ничего не сделаешь
        }
        else if (e.Exception is BadImageFormatException
            || e.Exception is ArgumentException && e.Exception.Message.Contains("Rect..ctor")
            || e.Exception is NullReferenceException && e.Exception.Message.Contains("UpdateTaskbarThumbButtons"))
        {
            MessageBox.Show(
                string.Format("{0}: {1}", SIQuester.Properties.Resources.AppRunError, e.Exception.Message),
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception.ToString().Contains("MediaPlayerState.OpenMedia"))
        {
            MessageBox.Show(
                string.Format(
                    "{0}: {1}",
                    SIQuester.Properties.Resources.WrongMultimediaAddress,
                    e.Exception.Message),
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception is COMException
            || e.Exception.ToString().Contains("UpdateTaskbarProgressState")
            || e.Exception.ToString().Contains("FindNameInTemplateContent"))
        {
            MessageBox.Show(
                string.Format("{0}: {1}", SIQuester.Properties.Resources.CommonAppError, e.ToString()),
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception.ToString().Contains("MahApps.Metro"))
        {
            // Ничего не сделаешь
        }
        else if (e.Exception.ToString().Contains("StoryFragments part failed to load"))
        {
            // https://learn.microsoft.com/en-us/answers/questions/1129597/wpf-apps-crash-on-windows-1011-after-windows-updat.html
            e.Handled = true;
            return;
        }
        else if (e.Exception is InvalidOperationException invalidOperationException
            && (invalidOperationException.Message.Contains(SIQuester.Properties.Resources.BindingDetachedError)
            || invalidOperationException.Message.Contains("Cannot perform this operation when binding is detached")))
        {
            MessageBox.Show(invalidOperationException.Message, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        else if (e.Exception.Message.Contains("System.Windows.Automation")
            || e.Exception.Message.Contains("UIAutomationCore.dll")
            || e.Exception.Message.Contains("UIAutomationTypes"))
        {
            MessageBox.Show(
                SIQuester.Properties.Resources.WindowsAutomationError,
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
                if (systemMessage.Length > 0)
                {
                    systemMessage.AppendLine().AppendLine("======").AppendLine();
                }

                message.AppendLine(exception.Message).AppendLine();
                systemMessage.AppendLine(exception.ToStringDemystified());
                exception = exception.InnerException;
            }

            var errorInfo = new Model.ErrorInfo { Time = DateTime.Now, Version = version, Error = systemMessage.ToString() };
#if !DEBUG
            var dialog = new TaskDialog
            {
                Caption = ProductName,
                InstructionText = SIQuester.Properties.Resources.SendErrorHeader,
                Text = message.ToString().Trim(),
                Icon = TaskDialogStandardIcon.Warning,
                StandardButtons = TaskDialogStandardButtons.Yes | TaskDialogStandardButtons.No
            };

            if (dialog.Show() == TaskDialogResult.Yes)
            {
                await SendMessageAsync(errorInfo);
            }
#else
            if (MessageBox.Show(
                $"{SIQuester.Properties.Resources.SendErrorHeader}{Environment.NewLine}{Environment.NewLine}{message.ToString().Trim()}",
                ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                await SendMessageAsync(errorInfo);
            }
#endif
        }

        e.Handled = true;
        Shutdown();
    }

    private async Task SendMessageAsync(Model.ErrorInfo errorInfo)
    {
        if (!_useAppService)
        {
            return;
        }

        using var appService = _host.Services.GetRequiredService<IAppServiceClient>();

        try
        {
            var result = await appService.SendErrorReportAsync("SIQuester", errorInfo.Error, errorInfo.Version, errorInfo.Time);

            switch (result)
            {
                case ErrorStatus.Fixed:
                    MessageBox.Show("Эта ошибка исправлена в новой версии программы. Обновитесь, пожалуйста.", ProductName);
                    break;

                case ErrorStatus.CannotReproduce:
                    MessageBox.Show(
                        "Эта ошибка не воспроизводится. Если вы можете её гарантированно воспроизвести, свяжитесь с автором, пожалуйста.",
                        ProductName);
                    break;
            }
        }
        catch
        {
            MessageBox.Show("Не удалось подключиться к серверу при отправке отчёта об ошибке. Отчёт будет отправлен позднее.", ProductName);
            if (AppSettings.Default.DelayedErrors.Count < 10)
            {
                AppSettings.Default.DelayedErrors.Add(XamlServices.Save(errorInfo));
            }
        }
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        _logger?.LogInformation("Application exited");

        _autoSaveTimer?.Stop();
        _mainViewModel?.Dispose();

        if (_host != null)
        {
            await _host.StopAsync();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("OnExit");

            if (AppSettings.Default != null)
            {
                SaveSettings(AppSettings.Default);
            }

            _manager.Dispose();
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                string.Format("{0}: {1}", SIQuester.Properties.Resources.SettingsSavingError, exc),
                ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                    using var file = IsolatedStorageFile.GetUserStoreForAssembly();
                    using var stream = new IsolatedStorageFileStream(ConfigFileName, FileMode.Create, file);
                    settings.Save(stream);
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
                $"{SIQuester.Properties.Resources.SettingsSavingError}: {exc.Message}",
                AppSettings.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
    }

    /// <summary>
    /// Загрузить пользовательские настройки
    /// </summary>
    public static AppSettings LoadSettings()
    {
        try
        {
            using var file = IsolatedStorageFile.GetUserStoreForAssembly();

            if (file.FileExists(ConfigFileName) && Monitor.TryEnter(ConfigFileName, 2000))
            {
                try
                {
                    using var stream = file.OpenFile(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return AppSettings.Load(stream);
                }
                catch { }
                finally
                {
                    Monitor.Exit(ConfigFileName);
                }
            }
        }
        catch { }

        return new AppSettings();
    }
}
