using SICore.PlatformSpecific;
using SIGame.Implementation;
using SIGame.ViewModel;
using SIUI.ViewModel.Core;
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

namespace SIGame
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
#pragma warning disable IDE0052 // Удалить непрочитанные закрытые члены
        private readonly DesktopCoreManager _coreManager = new DesktopCoreManager();
#pragma warning restore IDE0052 // Удалить непрочитанные закрытые члены
        private readonly DesktopManager _manager = new DesktopManager();

        /// <summary>
        /// Имя приложения
        /// </summary>
        public static string ProductName => "SIGame";

        /// <summary>
        /// Необходимый заголовок для WebRequest'ов и WebClient'ов
        /// </summary>
        public static string UserAgentHeader = $"{ProductName} {Assembly.GetExecutingAssembly().GetName().Version} ({Environment.OSVersion.VersionString})";

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

                Trace.TraceInformation("Game launched");

                UserSettings.Default.GameServerUri = SIGame.Properties.Settings.Default.GameServerUri;
                UserSettings.Default.PropertyChanged += Default_PropertyChanged;

                MainWindow = new MainWindow { DataContext = new MainViewModel(CommonSettings.Default, UserSettings.Default) };
                
                MainWindow.Show();
                if (UserSettings.Default.FullScreen)
                {
                    ((MainWindow)MainWindow).Maximize();
                }
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show(SIGame.Properties.Resources.Error_IncifficientResources, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            catch (System.Windows.Markup.XamlParseException)
            {
                MessageBox.Show(SIGame.Properties.Resources.Error_NetBroken, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) =>
            Trace.TraceError($"Common game error: {e.ExceptionObject}");

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserSettings.Sound))
            {
                if (!UserSettings.Default.Sound)
                {
                    _manager.PlaySoundInternal();
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (CommonSettings.Default != null)
                SaveCommonSettings(CommonSettings.Default);

            if (UserSettings.Default != null)
                SaveUserSettings(UserSettings.Default);

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
                MessageBox.Show(SIGame.Properties.Resources.Error_IncifficientResourcesForExecution, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (inner is System.Windows.Markup.XamlParseException || inner is NotImplementedException || inner is TypeInitializationException || inner is FileFormatException)
            {
                MessageBox.Show($"{SIGame.Properties.Resources.Error_RuntimeBroken}: {inner.Message}", CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"{SIGame.Properties.Resources.Error_DirectXBroken}: {inner.Message}.", CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (inner is FileLoadException)
            {
                MessageBox.Show(inner.Message, CommonSettings.AppName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = e.Exception.ToString();

            if (message.Contains("System.Windows.Automation") || message.Contains("UIAutomationCore.dll") || message.Contains("UIAutomationTypes"))
            {
                MessageBox.Show(SIGame.Properties.Resources.Error_WindowsAutomationBroken, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (message.Contains("ApplyTaskbarItemInfo") || message.Contains("GetValueFromTemplatedParent") || message.Contains("IsBadSplitPosition")
                || message.Contains("IKeyboardInputProvider.AcquireFocus") || message.Contains("ReleaseOnChannel") || message.Contains("ManifestSignedXml2.GetIdElement"))
            {
                MessageBox.Show(SIGame.Properties.Resources.Error_OSBroken, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (message.Contains("ComputeTypographyAvailabilities") || message.Contains("FontList.get_Item"))
            {
                MessageBox.Show(SIGame.Properties.Resources.Error_Typography, ProductName, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(e.Exception.ToString());
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
        /// <returns></returns>
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
                        return oldSettings;
                }
            }
            catch { }

            return new CommonSettings();
        }

        /// <summary>
        /// Сохранить общие настройки
        /// </summary>
        /// <param name="settings"></param>
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
                        return oldSettings;
                }
            }
            catch { }

            return new UserSettings();
        }

        /// <summary>
        /// Сохранить общие настройки
        /// </summary>
        /// <param name="settings"></param>
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
