using AppRegistryService.Client;
using AppRegistryService.Contract;
using AppRegistryService.Contract.Models;
using AppRegistryService.Contract.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Extensions.Logging;
using NLog.Web;
using SImulator.Implementation;
using SImulator.ViewModel;
using SImulator.ViewModel.Core;
using SIStorage.Service.Client;
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

#if DEBUG
using SIStorage.Service.Contract.Models;
#endif

namespace SImulator;

/// <summary>
/// Provides interaction logic for App.xaml.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Unique application identifier.
    /// </summary>
    internal static readonly Guid AppId = Guid.Parse("ea8635a0-183e-47d6-944c-50643e32d689");

    private IHost? _host;

#pragma warning disable IDE0052
    private readonly DesktopManager _manager = new();
#pragma warning restore IDE0052

    /// <summary>
    /// User settings configuration file name.
    /// </summary>
    private const string ConfigFileName = "user.config";

    internal Settings Settings { get; } = LoadSettings();

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

        services.AddAppRegistryServiceClient(configuration);
        services.AddSIStorageServiceClient(configuration);

        services.AddSingleton(typeof(StorageViewModel));

        services.AddTransient<CommandWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        UI.Initialize();

        if (Settings.Language != null)
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Settings.Language);
        }
        else
        {
            var currentLanguage = Thread.CurrentThread.CurrentUICulture.Name;
            Settings.Language = currentLanguage == "ru-RU" ? currentLanguage : "en-US";
        }

        var main = new MainViewModel(Settings);

        if (e.Args.Length > 0)
        {
            main.PackageSource = new FilePackageSource(e.Args[0]);
        }

#if DEBUG
        main.PackageSource = new SIStoragePackageSource(
            new Package
            {
                Name = SImulator.Properties.Resources.TestPackage,
                ContentUri = new Uri("https://vladimirkhil.com/sistorage/packages/d8faa1a4-2a6f-4103-b298-fd15c6ee3ea6.siq")
            });
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
    private async void ProcessAsync(CancellationToken cancellationToken = default)
    {
        EnsureThat.EnsureArg.IsNotNull(_host);

        var appRegistryClient = _host.Services.GetRequiredService<IAppRegistryServiceClient>();

        try
        {
            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception("No app version found");

            // Update application launch counter
            await appRegistryClient.Apps.PostAppUsageAsync(
                AppId,
                new AppUsageInfo(currentVersion, Environment.OSVersion.Version, RuntimeInformation.OSArchitecture),
                cancellationToken);

            var delayedErrors = Settings.DelayedErrors;

            while (delayedErrors.Count > 0)
            {
                var error = delayedErrors[0];

                await appRegistryClient.Apps.SendAppErrorReportAsync(
                    AppId,
                    new AppErrorRequest(Version.Parse(error.Version), Environment.OSVersion.Version, RuntimeInformation.OSArchitecture)
                    {
                        ErrorMessage = error.Error,
                        ErrorTime = error.Time
                    },
                    cancellationToken);

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
        e.Handled = true;

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
            || e.Exception is COMException comException && (uint)comException.ErrorCode == 0x88980406
            || e.Exception.Message.Contains("UpdateTaskbarProgressState()"))
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
        else if (_host != null
            && MessageBox.Show(
                string.Format(SImulator.Properties.Resources.ErrorSendConfirm, e.Exception.Message),
                MainViewModel.ProductName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var appRegistryServiceClient = _host.Services.GetRequiredService<IAppRegistryServiceClient>();
            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
            var errorMessage = e.Exception.ToStringDemystified();

            try
            {
                var result = await appRegistryServiceClient.Apps.SendAppErrorReportAsync(
                    AppId,
                    new AppErrorRequest(version, Environment.OSVersion.Version, RuntimeInformation.OSArchitecture)
                    {
                        ErrorMessage = errorMessage,
                        ErrorTime = DateTimeOffset.UtcNow,
                    });

                switch (result)
                {
                    case ErrorStatus.Fixed:
                        MessageBox.Show(
                            SImulator.Properties.Resources.ErrorFixed,
                            MainViewModel.ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;

                    case ErrorStatus.CannotReproduce:
                        MessageBox.Show(
                            SImulator.Properties.Resources.ErrorCannotReproduce,
                            MainViewModel.ProductName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    SImulator.Properties.Resources.ErrorConnectionError,
                    MainViewModel.ProductName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (Settings.DelayedErrors.Count < 10)
                {
                    Settings.DelayedErrors.Add(
                        new ErrorInfo
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
