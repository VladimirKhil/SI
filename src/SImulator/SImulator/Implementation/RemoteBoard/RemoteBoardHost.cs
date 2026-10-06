using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Utils.Commands;
using Utils.Web;

namespace SImulator.Implementation.RemoteBoard;

/// <summary>
/// Manages the lifetime of the external browser game board and exposes it to the UI.
/// </summary>
public sealed class RemoteBoardHost : INotifyPropertyChanged
{
    private const int Port = 8090;

    public static RemoteBoardHost Instance { get; } = new();

    private readonly SemaphoreSlim _lock = new(1, 1);
    private RemoteBoardServer? _server;

    /// <summary>
    /// Is the board server running.
    /// </summary>
    public bool IsRunning => _server != null;

    /// <summary>
    /// Board URL (contains the session access token).
    /// </summary>
    public string? Url => _server?.Url;

    /// <summary>
    /// Opens the board in the default browser.
    /// </summary>
    public ICommand Open { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private RemoteBoardHost() => Open = new SimpleCommand(_ => OpenBrowser());

    /// <summary>
    /// Starts the board server for the game and opens the board in the browser.
    /// </summary>
    public async Task StartAsync(IWebInterop interop)
    {
        await _lock.WaitAsync();

        try
        {
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }

            _server = await RemoteBoardServer.StartAsync(interop, Port);
        }
        finally
        {
            _lock.Release();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Url));
        }

        OpenBrowser();
    }

    /// <summary>
    /// Stops the board server and waits until the port is released.
    /// </summary>
    public async Task StopAsync()
    {
        await _lock.WaitAsync();

        try
        {
            if (_server != null)
            {
                await _server.DisposeAsync();
                _server = null;
            }
        }
        finally
        {
            _lock.Release();
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Url));
        }
    }

    private void OpenBrowser()
    {
        var url = Url;

        if (url == null)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exc)
        {
            Trace.TraceError(exc.ToString());
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
