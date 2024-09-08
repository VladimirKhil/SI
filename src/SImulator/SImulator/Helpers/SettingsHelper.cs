using Polly;
using SImulator.Implementation;
using SImulator.ViewModel;
using SImulator.ViewModel.Model;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace SImulator.Helpers;

internal static class SettingsHelper
{
    private const string SettingsFolderName = "Settings";

    /// <summary>
    /// Manufacturer name.
    /// </summary>
    private const string ManufacturerName = "Khil-soft";

    /// <summary>
    /// Application name.
    /// </summary>
    private const string ProductName = "SImulator";

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ManufacturerName,
        ProductName,
        SettingsFolderName);

    /// <summary>
    /// User settings configuration file name.
    /// </summary>
    private const string ConfigFileName = "user.config";

    /// <summary>
    /// Loads user settings.
    /// </summary>
    internal static AppSettings LoadSettings()
    {
        try
        {
            var settingsFile = Path.Combine(SettingsFolder, ConfigFileName);

            if (File.Exists(settingsFile))
            {
                try
                {
                    using var stream = File.Open(settingsFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return JsonSerializer.Deserialize<AppSettings>(stream) ?? LoadSettingsOld();
                }
                catch { }
            }
        }
        catch { }

        return LoadSettingsOld();
    }

    /// <summary>
    /// Loads user settings.
    /// </summary>
    private static AppSettings LoadSettingsOld()
    {
        try
        {
            using var file = IsolatedStorageFile.GetUserStoreForAssembly();

            if (file.FileExists(ConfigFileName) && Monitor.TryEnter(ConfigFileName, 2000))
            {
                try
                {
                    using var stream = file.OpenFile(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return AppSettings.Load(stream, DesktopManager.SettingsSerializer);
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

    internal static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var settingsFile = Path.Combine(SettingsFolder, ConfigFileName);

            var retryPolicy = Policy
                .Handle<IOException>(exc => exc.HResult == -2147024864) // File being used by another process
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(1.5, retryAttempt)));

            retryPolicy.Execute(() =>
            {
                using var stream = File.Create(settingsFile);
                JsonSerializer.Serialize(stream, settings);
            });
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"{Properties.Resources.SavingSettingsError}: {exc.Message}",
                MainViewModel.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
    }
}
