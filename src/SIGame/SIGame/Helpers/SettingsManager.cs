using SIGame.ViewModel.Settings;
using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace SIGame.Helpers;

internal static class SettingsManager
{
    /// <summary>
    /// Common config file name.
    /// </summary>
    internal const string CommonConfigFileName = "app.config";

    /// <summary>
    /// User config file name.
    /// </summary>
    internal static string UserConfigFileName = "user.config";

    /// <summary>
    /// Application state file name.
    /// </summary>
    internal static string AppStateFileName = "appstate.json";

    public const string SettingsFolderName = "Settings";

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        CommonSettings.ManufacturerEn,
        CommonSettings.AppNameEn,
        SettingsFolderName);

    public static AppState LoadAppState()
    {
        try
        {
            var appStateFile = Path.Combine(SettingsFolder, AppStateFileName);

            if (!File.Exists(appStateFile))
            {
                return new AppState();
            }

            using var stream = File.Open(appStateFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            return JsonSerializer.Deserialize<AppState>(stream) ?? new AppState();
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"{Properties.Resources.Error_SettingsLoading}: {exc.Message}. {Properties.Resources.DefaultSettingsWillBeUsed}",
                AppConstants.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);

            return new AppState();
        }
    }

    internal static void SaveAppState(AppState appState)
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var appStateFile = Path.Combine(SettingsFolder, AppStateFileName);

            using var stream = File.Create(appStateFile);
            JsonSerializer.Serialize(stream, appState);
        }
        catch (Exception exc)
        {
            MessageBox.Show(
                $"{Properties.Resources.Error_SettingsSaving}: {exc.Message}",
                AppConstants.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
    }

    /// <summary>
    /// Loads common config.
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
                        $"{Properties.Resources.Error_SettingsLoading}: {exc.Message}. {Properties.Resources.DefaultSettingsWillBeUsed}",
                        AppConstants.ProductName,
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
                        $"{Properties.Resources.Error_SettingsLoading}: {exc.Message}. {Properties.Resources.DefaultSettingsWillBeUsed}",
                        AppConstants.ProductName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation);
                }
                finally
                {
                    Monitor.Exit(CommonConfigFileName);
                }
            }
        }
        catch { }

        return new CommonSettings();
    }

    /// <summary>
    /// Saves common config.
    /// </summary>
    internal static void SaveCommonSettings(CommonSettings settings)
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
                $"{Properties.Resources.Error_SettingsSaving}: {exc.Message}",
                AppConstants.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
    }

    /// <summary>
    /// Loads user config.
    /// </summary>
    public static UserSettings? LoadUserSettings()
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
                        $"{Properties.Resources.Error_SettingsLoading}: {exc.Message}. {Properties.Resources.DefaultSettingsWillBeUsed}",
                        AppConstants.ProductName,
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
                        $"{Properties.Resources.Error_SettingsLoading}: {exc.Message}. {Properties.Resources.DefaultSettingsWillBeUsed}",
                        AppConstants.ProductName,
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
    /// Saves user config.
    /// </summary>
    internal static void SaveUserSettings(UserSettings settings)
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
            MessageBox.Show(
                $"{Properties.Resources.Error_SettingsSaving}: {exc.Message}",
                AppConstants.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
    }
}
