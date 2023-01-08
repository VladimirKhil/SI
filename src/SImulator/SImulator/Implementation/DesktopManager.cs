using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SImulator.Implementation.ButtonManagers;
using SImulator.Properties;
using SImulator.ViewModel;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SIStorageService.ViewModel;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using Utils;
using Screen = System.Windows.Forms.Screen;

namespace SImulator.Implementation;

/// <summary>
/// Provides desktop implementation of SImulator API.
/// </summary>
internal sealed class DesktopManager : PlatformManager
{
    private const string GameSiteUri = "https://vladimirkhil.com";

    public IServiceProvider ServiceProvider { get; set; }

    private Window? _window = null;
    private PlayersWindow? _playersWindow = null;
    private readonly ButtonManagerFactoryDesktop _buttonManager = new();

    private MediaTimeline? _mediaTimeline = null;
    private MediaClock? _mediaClock = null;
    private MediaPlayer? _player = null;

    internal static XmlSerializer SettingsSerializer { get; } = new(typeof(AppSettings));

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

    public override Task CreateMainViewAsync(object dataContext, int screenNumber)
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
        {
            _window.WindowState = WindowState.Maximized;
        }

        return Task.CompletedTask;
    }

    public override Task CloseMainViewAsync()
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

    public override IScreen[] GetScreens() =>
        Screen.AllScreens.Select(screen => new ScreenInfo(screen))
            .Concat(new ScreenInfo[] { new ScreenInfo(null) })
            .ToArray();

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

    public override string[] GetComPorts() => SerialPort.GetPortNames();

    public override bool IsEscapeKey(GameKey key) => (Key)key == Key.Escape;

    public override int GetKeyNumber(GameKey key)
    {
        var key2 = (Key)key;
        int code = -1;

        if (key2 >= Key.D1 && key2 <= Key.D9)
        {
            code = key2 - Key.D1;
        }
        else if (key2 >= Key.NumPad1 && key2 <= Key.NumPad9)
        {
            code = key2 - Key.NumPad1;
        }

        return code;
    }

    public override async Task<IPackageSource?> AskSelectPackageAsync(object arg)
    {
        if (arg.ToString() == "0")
        {
            var dialog = new OpenFileDialog
            {
                Title = Resources.SelectQuestionPackage,
                DefaultExt = ".siq",
                Filter = $"{Resources.SIQuestions}|*.siq"
            };

            var dialogResult = dialog.ShowDialog();

            if (dialogResult.HasValue && dialogResult.Value)
            {
                return new FilePackageSource(dialog.FileName);
            }
        }
        else if (arg.ToString() == "1")
        {
            var storage = ServiceProvider.GetRequiredService<SIStorage>();
            storage.CurrentRestriction = ((App)Application.Current).Settings.Restriction;

            storage.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SIStorage.CurrentRestriction))
                {
                    ((App)Application.Current).Settings.Restriction = storage.CurrentRestriction;
                }
            };

            storage.Error += (exc, message) =>
            {
                ShowMessage(string.Format(Resources.SIStorageError, $"{message} {exc}"), false);
            };

            try
            {
                await storage.OpenAsync();

                var packageStoreWindow = new PackageStoreWindow { DataContext = storage };
                var dialogResult = packageStoreWindow.ShowDialog();

                var package = dialogResult.HasValue && dialogResult.Value ? storage.CurrentPackage : null;

                if (package == null)
                {
                    return null;
                }

                var link = await storage.LoadSelectedPackageUriAsync();
                return new SIStoragePackageSource(package, link.Uri);
            }
            catch (Exception exc)
            {
                ShowMessage(string.Format(Resources.SIStorageError, exc.ToString()), false);
                return null;
            }
        }
        else
        {
            return new FilePackageSource(arg.ToString());
        }

        return null;
    }

    public override string? AskSelectColor()
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

    public override Task<string?> AskSelectFileAsync(string header)
    {
        var dialog = new OpenFileDialog { Title = header };

        if (dialog.ShowDialog().Value)
        {
            return Task.FromResult(dialog.FileName);
        }

        return Task.FromResult<string?>(null);
    }

    public override string? AskSelectLogsFolder()
    {
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = Resources.SelectLogsFolder })
        {
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
        }

        return null;
    }

    public override Task<bool> AskStopGameAsync() =>
        Task.FromResult(MessageBox.Show(
            Resources.FinishGameQuestion,
            MainViewModel.ProductName,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes);

    public override void ShowMessage(string text, bool error = true) =>
        MessageBox.Show(text, MainViewModel.ProductName, MessageBoxButton.OK, error ? MessageBoxImage.Error : MessageBoxImage.Exclamation);

    public override void NavigateToSite()
    {
        try
        {
            Browser.Open(GameSiteUri);
        }
        catch (Exception exc)
        {
            ShowMessage(string.Format(Resources.NavigateToSiteError, GameSiteUri, exc.Message));
        }
    }

    public override void PlaySound(string name, Action onFinish)
    {
        if (string.IsNullOrEmpty(name))
        {
            StopSound();
            return;
        }

        if (!Uri.TryCreate(name, UriKind.RelativeOrAbsolute, out var uri))
        {
            return;
        }

        var source = uri.IsAbsoluteUri && uri.IsFile ? name : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", name);

        if (uri.IsAbsoluteUri && uri.IsFile && !File.Exists(source))
        {
            StopSound();
            return;
        }

        if (_mediaTimeline == null)
        {
            _mediaTimeline = new MediaTimeline();
            _player = new MediaPlayer();
            _mediaTimeline.Completed += (sender, e) => onFinish();
        }
        else
        {
            StopSound();
        }

        _mediaTimeline.Source = new Uri(source, UriKind.RelativeOrAbsolute);
        _mediaClock = _mediaTimeline.CreateClock();
        _player.Clock = _mediaClock;

        _mediaClock.Controller.Begin();
    }

    private void StopSound()
    {
        if (_mediaClock != null && _mediaClock.CurrentState == System.Windows.Media.Animation.ClockState.Active)
        {
            _mediaClock.Controller.Stop();
        }
    }

    public override ILogger CreateLogger(string? folder)
    {
        if (folder == null)
        {
            return Logger.Create(null);
        }

        if (!Directory.Exists(folder))
        {
            throw new Exception(string.Format(Resources.LogsFolderNotFound, folder));
        }

        return Logger.Create(Path.Combine(folder, string.Format("{0}.log", DateTime.Now).Replace(':', '.')));
    }

    public override void ClearMedia()
    {
        if (_mediaClock == null)
        {
            return;
        }

        if (_mediaClock.CurrentState == System.Windows.Media.Animation.ClockState.Active)
        {
            _mediaClock.Controller.Stop();
        }

        _mediaTimeline = null;
        _mediaClock = null;
        _player = null;
    }

    public override void InitSettings(AppSettings defaultSettings)
    {
        
    }
}
