using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SImulator.Implementation.RemoteBoard;
using SImulator.Properties;
using SImulator.ViewModel;
using SImulator.ViewModel.ButtonManagers;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Core;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.PlatformSpecific;
using SImulator.Views;
using System.Diagnostics;
using System.IO.Ports;
using Utils.Timers;
using Utils.Web;

namespace SImulator.Implementation;

/// <summary>
/// Provides macOS (Avalonia) platform services.
/// </summary>
internal sealed class MacPlatformManager : PlatformManager, IPlatformService
{
    private const string GameSiteUri = "https://vladimirkhil.com/si/game";

    private readonly MacButtonManagerFactory _buttonManagerFactory = new();

    private BoardWindow? _boardWindow;
    private PlayersWindow? _playersWindow;
    private Process? _soundProcess;

    internal Window? MainWindow { get; set; }

    public override ButtonManagerFactory ButtonManagerFactory => _buttonManagerFactory;

    public override void CreatePlayersView(object dataContext)
    {
        if (_playersWindow != null)
        {
            return;
        }

        _playersWindow = new PlayersWindow { DataContext = dataContext };
        _playersWindow.Show();
    }

    public override void ClosePlayersView()
    {
        if (_playersWindow == null)
        {
            return;
        }

        _playersWindow.CanClose = true;
        _playersWindow.Close();
        _playersWindow = null;
    }

    public override async Task CreateMainViewAsync(object dataContext, IDisplayDescriptor screen)
    {
        var interop = dataContext as IWebInterop ?? throw new ArgumentException("Board data context must implement IWebInterop", nameof(dataContext));

        if (screen is BrowserDisplayDescriptor)
        {
            await RemoteBoardHost.Instance.StartAsync(interop);
            return;
        }

        _boardWindow = new BoardWindow(screen as MacScreenDescriptor) { DataContext = dataContext };
        await _boardWindow.StartAsync(interop);
    }

    public override async Task CloseMainViewAsync()
    {
        await RemoteBoardHost.Instance.StopAsync();

        if (_boardWindow != null)
        {
            var window = _boardWindow;
            _boardWindow = null;
            await window.StopAsync();
        }
    }

    public override IDisplayDescriptor[] GetScreens()
    {
        var screens = MainWindow?.Screens.All ?? [];

        return
        [
            .. screens.OrderByDescending(screen => screen.IsPrimary).Select(screen => (IDisplayDescriptor)new MacScreenDescriptor(screen)),
            MacScreenDescriptor.WindowScreen,
            BrowserDisplayDescriptor.Instance,
        ];
    }

    public override string[] GetFonts() => [.. FontManager.Current.SystemFonts.Select(font => font.Name).Order()];

    public override string[] GetLocalComputers() => [];

    public override string[] GetComPorts() => SerialPort.GetPortNames();

    public override bool IsEscapeKey(GameKey key) => (Key)key == Key.Escape;

    public override int GetKeyNumber(GameKey key)
    {
        var avaloniaKey = (Key)key;

        if (avaloniaKey >= Key.D1 && avaloniaKey <= Key.D9)
        {
            return avaloniaKey - Key.D1;
        }

        if (avaloniaKey >= Key.NumPad1 && avaloniaKey <= Key.NumPad9)
        {
            return avaloniaKey - Key.NumPad1;
        }

        return -1;
    }

    public override async Task<IPackageSource?> AskSelectPackageAsync(string arg)
    {
        if (arg == "0")
        {
            var file = await PickFileAsync(Resources.SelectQuestionPackage, new FilePickerFileType(Resources.SIQuestions) { Patterns = ["*.siq"] });
            return file != null ? new FilePackageSource(file) : null;
        }

        if (arg == "1")
        {
            ShowMessage(MacResources.LibraryIsNotSupported, false);
            return null;
        }

        return new FilePackageSource(arg);
    }

    public override Task<string?> AskSelectFileAsync(string header) => PickFileAsync(header, null);

    private async Task<string?> PickFileAsync(string title, FilePickerFileType? fileType)
    {
        if (MainWindow == null)
        {
            return null;
        }

        var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileType != null ? [fileType] : null,
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public override string? AskSelectLogsFolder()
    {
        ShowMessage(MacResources.LogsFolderSelectionIsNotSupported, false);
        return null;
    }

    public string? AskSelectColor() => null;

    public override Task<bool> AskStopGameAsync() => MessageDialog.AskAsync(MainWindow, Resources.FinishGameQuestion);

    public void ShowMessage(string text, bool error = true)
    {
        Trace.TraceInformation(text);
        _ = MessageDialog.ShowAsync(MainWindow, text, error);
    }

    public void NavigateToSite() => OpenUrl(GameSiteUri);

    internal static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
        }
    }

    public override void PlaySound(string name, Action? onFinish = null)
    {
        StopSound();

        if (string.IsNullOrEmpty(name) || !Uri.TryCreate(name, UriKind.RelativeOrAbsolute, out var uri))
        {
            return;
        }

        var source = uri.IsAbsoluteUri && uri.IsFile ? uri.LocalPath : Path.Combine(AppContext.BaseDirectory, "sounds", name);

        if (!File.Exists(source))
        {
            return;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo("/usr/bin/afplay") { ArgumentList = { source }, UseShellExecute = false, CreateNoWindow = true },
                EnableRaisingEvents = true,
            };

            process.Exited += (_, _) =>
            {
                // Report finish only for the sound that played to the end (not stopped or replaced)
                if (_soundProcess == process && process.ExitCode == 0)
                {
                    _soundProcess = null;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => onFinish?.Invoke());
                }
            };

            _soundProcess = process;
            process.Start();
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
        }
    }

    private void StopSound()
    {
        var process = _soundProcess;
        _soundProcess = null;

        try
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    public override IGameLogger CreateGameLogger(string? folder)
    {
        if (folder == null)
        {
            return GameLogger.Create(null);
        }

        if (!Directory.Exists(folder))
        {
            throw new Exception(string.Format(Resources.LogsFolderNotFound, folder));
        }

        return GameLogger.Create(Path.Combine(folder, string.Format("{0}.log", DateTime.Now).Replace(':', '.').Replace('/', '.')));
    }

    public override void ClearMedia() => StopSound();

    public override void InitSettings(AppSettings defaultSettings)
    {
    }

    public override IAnimatableTimer CreateAnimatableTimer() => new AnimatableTimer();
}
