using SI.GameServer.Client;
using SICore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Utils.Commands;

namespace SIGame.ViewModel;

public sealed class LoginViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isDisposed = false;

    private readonly object _loginLock = new();

    private string _login;

    public string Login
    {
        get => _login;
        set { _login = value; OnPropertyChanged(); }
    }

    private string _error = "";

    public string Error
    {
        get => _error;
        set { if (_error != value) { _error = value; OnPropertyChanged(); } }
    }

    private string _fullError;

    public string FullError
    {
        get => _fullError;
        set { if (_fullError != value) { _fullError = value; OnPropertyChanged(); } }
    }

    public ICommand ShowFullError { get; private set; }

    private bool _isProgress;

    public bool IsProgress
    {
        get => _isProgress;
        set { _isProgress = value; OnPropertyChanged(); }
    }

    public IAsyncCommand Enter { get; set; }

    internal event Func<string, IGameServerClient, Task>? Entered;

    private readonly IGameServerClientFactory _gameServerClientFactory;

    public LoginViewModel(IGameServerClientFactory gameServerClientFactory)
    {
        _gameServerClientFactory = gameServerClientFactory;

        Enter = new AsyncCommand(Enter_ExecutedAsync);
        ShowFullError = new CustomCommand(ShowFullError_Executed);
    }

    private void ShowFullError_Executed(object? arg) =>
        PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);

    private async Task Enter_ExecutedAsync(object? arg)
    {
        lock (_loginLock)
        {
            if (IsProgress)
            {
                return;
            }

            IsProgress = true;
        }
        
        Error = "";
        FullError = null;
        Enter.CanBeExecuted = false;

        IGameServerClient client = null;

        try
        {
            client = await _gameServerClientFactory.CreateClientAsync(_cancellationTokenSource.Token);

            await client.OpenAsync(_login, _cancellationTokenSource.Token);

            await Entered?.Invoke(_login, client);
        }
        catch (HttpRequestException exc)
        {
            var message = new StringBuilder();

            var innerExc = exc.InnerException ?? exc;

            while (innerExc != null)
            {
                if (message.Length > 0)
                {
                    message.AppendLine();
                }

                message.Append(innerExc.Message);
                innerExc = innerExc.InnerException;
            }

            Error = message.ToString();

            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
        catch (Exception exc)
        {
            Error = exc.Message;
            FullError = exc.ToString();

            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
        finally
        {
            IsProgress = false; // no lock required
            Enter.CanBeExecuted = true;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        _isDisposed = true;
    }
}
