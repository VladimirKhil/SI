using Microsoft.Win32;
using Services.SI.ViewModel;
using SIEngine;
using SImulator.Implementation.ButtonManagers;
using SImulator.Implementation.WinAPI;
using SImulator.Model;
using SImulator.ViewModel;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.PlatformSpecific;
using SIPackages.Core;
using SIUI.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using Screen = System.Windows.Forms.Screen;

namespace SImulator.Implementation
{
    /// <summary>
    /// Реализация функций СИмулятора для десктопа
    /// </summary>
    internal sealed class DesktopManager: PlatformManager
    {
        private Window _window = null;
        private PlayersWindow _playersWindow = null;
        private readonly ButtonManagerFactoryDesktop _buttonManager = new ButtonManagerFactoryDesktop();

        private MediaTimeline _mediaTimeline = null;
        private MediaClock _mediaClock = null;
        private MediaPlayer _player = null;

        private ServiceHost _host = null;

        private readonly List<string> _mediaFiles = new List<string>();

        internal static XmlSerializer SettingsSerializer = new XmlSerializer(typeof(AppSettings));

        public override ViewModel.ButtonManagers.ButtonManagerFactory ButtonManagerFactory => _buttonManager;

        public override void CreatePlayersView(object dataContext)
        {
            if (_playersWindow == null)
            {
                _playersWindow = new PlayersWindow { DataContext = dataContext };
                _playersWindow.Show();
            }
        }

        public override void ClosePlayersView()
        {
            if (_playersWindow != null)
            {
                _playersWindow.CanClose = true;
                _playersWindow.Close();
                _playersWindow = null;
            }
        }

        public override Task CreateMainView(object dataContext, int screenNumber)
        {
            var fullScreen = screenNumber < Screen.AllScreens.Length;

            _window = new MainWindow(fullScreen)
            {
                DataContext = dataContext
            };

            if (fullScreen)
            {
                screenNumber = Math.Min(screenNumber, Screen.AllScreens.Length - 1);
                var area = Screen.AllScreens[screenNumber].WorkingArea;
                _window.Left = area.Left;
                _window.Top = area.Top;
                _window.Width = area.Width;
                _window.Height = area.Height;
            }

            _window.Show();

            if (fullScreen)
                _window.WindowState = WindowState.Maximized;

            return Task.CompletedTask;
        }

        public override Task CloseMainView()
        {
            if (_window != null)
            {
                MainWindow.CanClose = true;
                try
                {
                    _window.Close();
                    _window = null;
                }
                finally
                {
                    MainWindow.CanClose = false;
                }
            }

            return Task.CompletedTask;
        }

        public override IScreen[] GetScreens()
        {
            return Screen.AllScreens.Select(screen => new ScreenInfo(screen)).Concat(new ScreenInfo[] { new ScreenInfo(null), new ScreenInfo(null) { IsRemote = true} }).ToArray();
        }

        public override string[] GetLocalComputers()
        {
            var list = new List<string>();

            var current = Dns.GetHostName().ToUpper();
            using (var root = new DirectoryEntry("WinNT:"))
            {
                foreach (DirectoryEntry dom in root.Children)
                {
                    using (dom)
                    {
                        foreach (DirectoryEntry entry in dom.Children)
                        {
                            using (entry)
                            {
                                if (entry.Name != "Schema" && entry.SchemaClassName == "Computer" && entry.Name.ToUpper() != current)
                                {
                                    list.Add(entry.Name);
                                }
                            }
                        }
                    }
                }
            }

            return list.ToArray();
        }

        public override string[] GetComPorts()
        {
            return SerialPort.GetPortNames();
        }

        public override bool IsEscapeKey(ViewModel.Core.GameKey key)
        {
            return (Key)key == Key.Escape;
        }

        public override int GetKeyNumber(ViewModel.Core.GameKey key)
        {
            var key2 = (Key)key;
            int code = -1;
            if (key2 >= Key.D1 && key2 <= Key.D9)
                code = key2 - Key.D1;
            else if (key2 >= Key.NumPad1 && key2 <= Key.NumPad9)
                code = key2 - Key.NumPad1;

            return code;
        }

        public override async Task<IPackageSource> AskSelectPackage(object arg)
        {
            if (arg.ToString() == "0")
            {
                var dialog = new OpenFileDialog { Title = "Выберите пакет вопросов", DefaultExt = ".siq", Filter = "Вопросы СИ|*.siq" };
                if (dialog.ShowDialog().Value)
                {
                    return new FilePackageSource(dialog.FileName);
                }
            }
            else if (arg.ToString() == "1")
            {
                var storage = new SIStorageNew
                {
                    CurrentRestriction = ((App)Application.Current).Settings.Restriction
                };

                storage.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SIStorageNew.CurrentRestriction))
                        ((App)Application.Current).Settings.Restriction = storage.CurrentRestriction;
                };

                storage.Error += exc =>
                {
                    ShowMessage(string.Format("Ошибка работы с библиотекой вопросов: {0}", exc.ToString()), false);
                };

                try
                {
                    storage.Open();

                    var packageStoreWindow = new PackageStoreWindow { DataContext = storage };
                    var package = packageStoreWindow.ShowDialog().Value ? storage.CurrentPackage : null;

                    if (package == null)
                        return null;

                    var uri = await storage.LoadSelectedPackageUriAsync();
                    return new SIStoragePackageSource(package, uri);
                }
                catch (Exception exc)
                {
                    ShowMessage(string.Format("Ошибка работы с библиотекой вопросов: {0}", exc.ToString()), false);
                    return null;
                }
            }
            else
            {
                return new FilePackageSource(arg.ToString());
            }

            return null;
        }

        public override string AskSelectColor()
        {
            var diag = new System.Windows.Forms.ColorDialog();
            if (diag.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = diag.Color;
                var convertedColor = Color.FromRgb(color.R, color.G, color.B);
                return convertedColor.ToString();
            }

            return null;
        }

        public override Task<string> AskSelectLogo()
        {
            var dialog = new OpenFileDialog() { Title = "Выберите изображение-заставку" };
            if (dialog.ShowDialog().Value)
                return Task.FromResult(dialog.FileName);

            return Task.FromResult<string>(null);
        }

        public override Task<string> AskSelectVideo()
        {
            var dialog = new OpenFileDialog() { Title = "Выберите заставочный видеофайл" };
            if (dialog.ShowDialog().Value)
                return Task.FromResult(dialog.FileName);

            return Task.FromResult<string>(null);
        }

        public override string AskSelectLogsFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Выберите папку для записи логов" })
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }

            return null;
        }

        public override Task<bool> AskStopGame()
        {
            return Task.FromResult(MessageBox.Show("Завершить игру?", MainViewModel.ProductName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
        }

        public override void ShowMessage(string text, bool error = true)
        {
            MessageBox.Show(text, MainViewModel.ProductName, MessageBoxButton.OK, error ? MessageBoxImage.Error : MessageBoxImage.Exclamation);
        }

        public override void NavigateToSite()
        {
            Process.Start("http://vladimirkhil.com");
        }

        public override void PlaySound(string name, Action onFinish)
        {
            if (name == null)
            {
                if (_mediaClock != null && _mediaClock.CurrentState == System.Windows.Media.Animation.ClockState.Active)
                    _mediaClock.Controller.Stop();

                return;
            }

            var source = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds"), name);

            if (_mediaTimeline == null)
            {
                _mediaTimeline = new MediaTimeline();
                _player = new MediaPlayer();
                _mediaTimeline.Completed += (sender, e) => onFinish();
            }
            else
            {
                if (_mediaClock.CurrentState == System.Windows.Media.Animation.ClockState.Active)
                    _mediaClock.Controller.Stop();
            }

            _mediaTimeline.Source = new Uri(source, UriKind.RelativeOrAbsolute);
            _mediaClock = _mediaTimeline.CreateClock();
            _player.Clock = _mediaClock;

            _mediaClock.Controller.Begin();
        }

        public override ILogger CreateLogger(string folder)
        {
            if (folder == null)
                return Logger.Create(null);

            if (!Directory.Exists(folder))
                throw new Exception(string.Format("Папка для записи логов \"{0}\" не найдена", folder));

            return Logger.Create(Path.Combine(folder, string.Format("{0}.log", DateTime.Now).Replace(':', '.')));
        }

        public override void CreateServer(Type contract, int port, int screenIndex)
        {
            _host = new ServiceHost(new RemoteGameUIServer { ScreenIndex = screenIndex }, new Uri(string.Format("net.tcp://localhost:{0}", port)));
            _host.AddServiceEndpoint(contract, MainViewModel.GetBinding(), "simulator");

            _host.Open();
        }

        public override void CloseServer()
        {
            _host.Close();
        }

        public override async Task<IMedia> PrepareMedia(IMedia media)
        {
            if (media.GetStream == null) // Это ссылка на внешний файл
                return media;

            // Это сам файл
            var fileName = Path.Combine(Path.GetTempPath(), new Random().Next() + media.Uri);
            var streamInfo = media.GetStream();
            if (streamInfo == null)
                return null;

            try
            {
                using (streamInfo.Stream)
                {
                    using (var fs = File.Create(fileName))
                    {
                        await streamInfo.Stream.CopyToAsync(fs);
                    }
                }
            }
            catch (IOException exc)
            {
                ShowMessage(exc.Message);
                return null;
            }

            _mediaFiles.Add(fileName);

            return new Media(fileName);
        }

        public override void ClearMedia()
        {
            foreach (var file in _mediaFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch (Exception exc)
                {
                    ShowMessage(string.Format("Ошибка удаления файла: {0}", exc.Message));
                }
            }

            if (_mediaClock != null)
            {
                if (_mediaClock.CurrentState == System.Windows.Media.Animation.ClockState.Active)
                    _mediaClock.Controller.Stop();

                _mediaTimeline = null;
                _mediaClock = null;
                _player = null;
            }
        }

        public override T GetCallback<T>()
        {
            return OperationContext.Current.GetCallbackChannel<T>();
        }

        public override void InitSettings(Model.AppSettings defaultSettings)
        {
            
        }

        public override IExtendedGameHost CreateGameHost(EngineBase engine)
        {
            return new GameHostClient(engine);
        }
    }
}
