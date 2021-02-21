using Services.SI;
using SI.GameServer.Client;
using SICore;
using SIGame.ViewModel.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SIGame.ViewModel
{
    public sealed class LoginViewModel : INotifyPropertyChanged, IDisposable
    {
        private const int ClientProtocolVersion = 1;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _isDisposed = false;

        private readonly object _loginLock = new object();

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

        internal event Action<string, IGameServerClient> Entered;

        private readonly UserSettings _userSettings;

        public LoginViewModel(UserSettings userSettings)
        {
            _userSettings = userSettings;

            Enter = new AsyncCommand(Enter_ExecutedAsync);
            ShowFullError = new CustomCommand(ShowFullError_Executed);
        }

        private void ShowFullError_Executed(object arg) =>
            PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);

        private async Task Enter_ExecutedAsync(object arg)
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

            NewServerInfo serverInfo;
            try
            {
                serverInfo = await GetGameServerUriAsync();
            }
            catch (Exception exc)
            {
                Error = $"{Resources.CannotGetServerAddress} {exc.Message}";
                return;
            }

            try
            {
                if (serverInfo.ProtocolVersion > ClientProtocolVersion)
                {
                    IsProgress = false;
                    Enter.CanBeExecuted = true;
                    Error = Resources.ObsoleClient;
                    return;
                }

                var gameServerClientOptions = new GameServerClientOptions
                {
                    ServiceUri = serverInfo.Uri + (serverInfo.Uri.EndsWith("/") ? "" : "/")
                };

                client = new GameServerClient(gameServerClientOptions, PlatformSpecific.PlatformManager.Instance);

                await client.OpenAsync(_login, _cancellationTokenSource.Token);

                Entered?.Invoke(_login, client);
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

        private async Task<NewServerInfo> GetGameServerUriAsync()
        {
            if (!string.IsNullOrEmpty(_userSettings.GameServerUri))
            {
                return new NewServerInfo
                {
                    Uri = _userSettings.GameServerUri,
                    ProtocolVersion = ClientProtocolVersion
                };
            }

            var siStorageServiceClient = new SIStorageServiceClient();
            var uris = await siStorageServiceClient.GetGameServersUrisAsync(_cancellationTokenSource.Token);

            return uris?.FirstOrDefault();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null) =>
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
}
