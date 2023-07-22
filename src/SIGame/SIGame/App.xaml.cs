using AppService.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;
using SI.GameServer.Client;
using SIContentService.Client;
using SICore.PlatformSpecific;
using SIGame.Contracts;
using SIGame.Helpers;
using SIGame.Implementation;
using SIGame.ViewModel;
using SIGame.ViewModel.Settings;
using SIStatisticsService.Client;
using SIStorageService.Client;
using SIStorageService.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Utils;
#if UPDATE
using AppService.Client.Models;
using SICore;
using System.IO;
using System.Reflection;
using System.Net.Http;
using System.Threading.Tasks;
#endif

namespace SIGame;

/// <summary>
/// Provides interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private ILogger<App>? _logger;

#pragma warning disable IDE0052
    private readonly DesktopCoreManager _coreManager = new();
#pragma warning restore IDE0052

    private readonly DesktopManager _manager = new();

    private AppState _appState = new();

    private static readonly bool UseSignalRConnection = Environment.OSVersion.Version >= new Version(6, 2);

    /// <summary>
    /// Application name.
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

    private void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        var configuration = ctx.Configuration;

        services.AddAppServiceClient(configuration);
        services.AddSIGameServerClient(configuration);
        services.AddSIStatisticsServiceClient(configuration);
        services.AddSIStorageServiceClient(configuration);
        services.AddSIContentServiceClient(configuration);

        services.AddTransient(typeof(SIStorage));

        services.AddSingleton(_appState);

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

            CommonSettings.Default = SettingsManager.LoadCommonSettings();
            UserSettings.Default = SettingsManager.LoadUserSettings() ?? new UserSettings();
            _appState = SettingsManager.LoadAppState();

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
                DataContext = new MainViewModel(CommonSettings.Default, UserSettings.Default, _appState, _host.Services)
            };

#if UPDATE
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

#if UPDATE
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
    private async Task<AppInfo?> SearchForUpdatesAsync()
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

        var mainViewModel = (MainViewModel)MainWindow.DataContext;

        mainViewModel.StartMenu.UpdateVersion = updateInfo.Version;
        mainViewModel.StartMenu.Update = new CustomCommand(obj => Update_Executed(updateUri));
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
        if (_host != null)
        {
            await _host.StopAsync();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (CommonSettings.Default != null)
        {
            SettingsManager.SaveCommonSettings(CommonSettings.Default);
        }

        if (UserSettings.Default != null)
        {
            SettingsManager.SaveUserSettings(UserSettings.Default);
        }

        SettingsManager.SaveAppState(_appState);

        base.OnExit(e);
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        e.Handled = new ExceptionHandler(_host.Services.GetRequiredService<IErrorManager>()).Handle(e.Exception);

    
}
