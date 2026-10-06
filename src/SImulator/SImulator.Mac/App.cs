using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using SImulator.Implementation;
using SImulator.ViewModel;
using SImulator.Views;
using System.Globalization;
using Utils;

namespace SImulator;

/// <summary>
/// macOS (Avalonia) SImulator application.
/// </summary>
internal sealed class App : Application
{
    private readonly MacPlatformManager _manager = new();

    internal ViewModel.Model.AppSettings Settings { get; } = SettingsStorage.Load();

    public override void Initialize()
    {
        Name = MainViewModel.ProductName;
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        UI.Initialize();

        if (Settings.Language != null)
        {
            CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Language);
        }
        else
        {
            var currentLanguage = CultureInfo.CurrentUICulture.Name;
            Settings.Language = currentLanguage.StartsWith("ru") ? "ru-RU" : "en-US";
            CultureInfo.CurrentUICulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Language);
        }

        // Ports below 1024 require root on macOS
        if (Settings.WebPort < 1024)
        {
            Settings.WebPort = 8080;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The window must exist before the view model: screens are enumerated through it
            var window = new MainWindow();
            _manager.MainWindow = window;

            var main = new MainViewModel(Settings, _manager);

            if (desktop.Args is { Length: > 0 } args)
            {
                main.PackageSource = new FilePackageSource(args[0]);
            }

            window.DataContext = main;
            desktop.MainWindow = window;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            desktop.Exit += (_, _) =>
            {
                _manager.ClearMedia();
                SettingsStorage.Save(Settings);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
