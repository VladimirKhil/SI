using AppService.Client;
using AppService.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using NLog.Web;
using SImulator.Implementation;
using SImulator.ViewModel;
using SIStorageService.Client;
using SIStorageService.Client.Models;
using SIStorageService.ViewModel;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Utils;
using Settings = SImulator.ViewModel.Model.AppSettings;

namespace SImulator;

/// <summary>
/// Provides interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

#pragma warning disable IDE0052
    private readonly DesktopManager _manager = new();
#pragma warning restore IDE0052

    /// <summary>
    /// User settings configuration file name.
    /// </summary>
    private const string ConfigFileName = "user.config";

    internal Settings Settings { get; } = LoadSettings();

    private bool _useAppService;

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
    }

    private void ConfigureServices(HostBuilderContext ctx, IServiceCollection services)
    {
        var configuration = ctx.Configuration;

        services.AddAppServiceClient(configuration);
        services.AddSIStorageServiceClient(configuration);

        services.AddTransient(typeof(SIStorage));

        services.AddTransient<CommandWindow>();

        var options = configuration.GetSection(AppServiceClientOptions.ConfigurationSectionName);
        var appServiceClientOptions = options.Get<AppServiceClientOptions>();

        _useAppService = appServiceClientOptions?.ServiceUri != null;
    }

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
            new PackageInfo
            {
                Description = SImulator.Properties.Resources.TestPackage
            },
            new Uri("https://vladimirkhil.com/sistorage/Основные/1.siq"));
#else
        ProcessAsync();
#endif

        MainWindow = new CommandWindow { DataContext = main };
        MainWindow.Show();
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
        }
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

        return new Settings();
    }

#if !DEBUG
    private async void ProcessAsync()
    {
        if (!_useAppService)
        {
            return;
        }

        using var appService = _host.Services.GetRequiredService<IAppServiceClient>();
        try
        {
            // Update application launch counter
            await appService.GetProductAsync("SImulator");

            var delayedErrors = Settings.DelayedErrors;
            while (delayedErrors.Count > 0)
            {
                var error = delayedErrors[0];
                await appService.SendErrorReportAsync("SImulator", error.Error, Version.Parse(error.Version), error.Time);
                delayedErrors.RemoveAt(0);
            }
        }
        catch
        {
        }
    }
#endif

    private async void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
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
        else if (e.Exception is NotImplementedException && e.Exception.Message.Contains("The Source property cannot be set to null"))
        {
            // https://github.com/MicrosoftEdge/WebView2Feedback/issues/1136
            e.Handled = true;
            return;
        }
        else if (e.Exception is System.Windows.Markup.XamlParseException
            || e.Exception is NotImplementedException
            || e.Exception is TypeInitializationException
            || e.Exception is COMException comException && (uint)comException.ErrorCode == 0x88980406)
        {
            MessageBox.Show(
                string.Format(SImulator.Properties.Resources.RuntimeBrokenError, e.Exception),
                MainViewModel.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception is Win32Exception || e.Exception is COMException)
        {
            MessageBox.Show(
                e.Exception.Message,
                MainViewModel.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        else if (e.Exception is InvalidOperationException && e.Exception.Message.Contains("Cannot set Visibility to Visible"))
        {
            // Do nothing
        }
        else if (_useAppService
            && _host != null
            && MessageBox.Show(
                string.Format("Произошла ошибка в приложении: {0}\r\n\r\nПриложение будет закрыто. Отправить информацию разработчику? (просьба также связаться с разработчиком лично, так как ряд ошибок нельзя воспроизвести)", e.Exception.Message),
                MainViewModel.ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            using var appService = _host.Services.GetRequiredService<IAppServiceClient>();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var errorMessage = e.Exception.ToStringDemystified();

            try
            {
                var result = await appService.SendErrorReportAsync("SImulator", errorMessage, version, DateTime.UtcNow);

                switch (result)
                {
                    case ErrorStatus.Fixed:
                        MessageBox.Show(
                            "Эта ошибка исправлена в новой версии программы. Обновитесь, пожалуйста.",
                            MainViewModel.ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;

                    case ErrorStatus.CannotReproduce:
                        MessageBox.Show(
                            "Эта ошибка не воспроизводится. Если вы можете её гарантированно воспроизвести, свяжитесь с автором, пожалуйста.",
                            MainViewModel.ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Не удалось подключиться к серверу при отправке отчёта об ошибке. Отчёт будет отправлен позднее.",
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (Settings.DelayedErrors.Count < 10)
                {
                    Settings.DelayedErrors.Add(
                        new ViewModel.Core.ErrorInfo
                        {
                            Time = DateTime.Now,
                            Error = errorMessage,
                            Version = version?.ToString() ?? ""
                        });
                }
            }
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
