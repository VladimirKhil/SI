using Services.SI;
using SICore;
using SIGame.ViewModel.Properties;
using SIGame.ViewModel.ViewModel.Data;
using System;
using System.ComponentModel;
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
            get { return _login; }
            set { _login = value; OnPropertyChanged(); }
        }

        private string _error = "";

        public string Error
        {
            get { return _error; }
            set { if (_error != value) { _error = value; OnPropertyChanged(); } }
        }

        private string _fullError;

        public string FullError
        {
            get { return _fullError; }
            set { if (_fullError != value) { _fullError = value; OnPropertyChanged(); } }
        }

        public ICommand ShowFullError { get; private set; }

        private bool _isProgress;

        public bool IsProgress
        {
            get { return _isProgress; }
            set { _isProgress = value; OnPropertyChanged(); }
        }
        
        public IAsyncCommand Enter { get; set; }

        internal event Action<string, IGameServerClient> Entered;

        public LoginViewModel()
        {
            Enter = new AsyncCommand(Enter_Executed);
            ShowFullError = new CustomCommand(ShowFullError_Executed);
        }

        private void ShowFullError_Executed(object arg)
        {
            PlatformSpecific.PlatformManager.Instance.ShowMessage(FullError, PlatformSpecific.MessageType.Warning, true);
        }

        private async Task Enter_Executed(object arg)
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

            try
            {
                NewServerInfo[] uris;
                try
                {
                    var siService = new SIStorageServiceClient();
                    uris = await siService.GetGameServersUrisAsync(_cancellationTokenSource.Token);
                }
                catch
                {
                    uris = null;
                }
                
                var uri = uris?.FirstOrDefault() ?? new NewServerInfo { Uri = GameServerClientNew.GameServerPredefinedUri, ProtocolVersion = ClientProtocolVersion };

                if (uri == null)
                {
                    Error = Resources.CannotGetServerAddress;
                    return;
                }

                if (uri.ProtocolVersion > ClientProtocolVersion)
                {
                    IsProgress = false;
                    Enter.CanBeExecuted = true;
                    Error = Resources.ObsoleClient;
                    return;
                }

                IGameServerClient client = new GameServerClientNew(uri.Uri);

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
            }
            catch (Exception exc)
            {
                Error = exc.Message;
                FullError = exc.ToString();
            }
            finally
            {
                lock (_loginLock)
                {
                    IsProgress = false;
                }

                Enter.CanBeExecuted = true;
            }
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
