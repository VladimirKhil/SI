using SICore;
using SIGame.ViewModel.Properties;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SIGame.ViewModel
{
    public sealed class SINetworkViewModel: ConnectionDataViewModel
    {
        private static readonly Regex AddressRegex = new Regex(@"^(?<host>(\d{1,3}\.){3}\d{1,3})\:(?<port>\d+)$");

        protected override bool IsOnline => false;

        private ConnectionGameData _gameData = null;

        public ConnectionGameData GameData
        {
            get => _gameData;
            set
            {
                if (_gameData != value)
                {
                    _gameData = value;
                    OnPropertyChanged();
                    if (value != null)
                        UpdateJoinCommand(value.Persons);
                }
            }
        }

        public string Address
        {
            get => _model.Address;
            set
            {
                var newValue = value.Trim();

                Error = "";
                if (_model.Address != newValue)
                {
                    Connect.CanBeExecuted = false;
                    _model.Address = newValue;
                    OnPropertyChanged();
                    var match = AddressRegex.Match(newValue);

                    if (!match.Success || !int.TryParse(match.Groups["port"].Value, out int val))
                    {
                        Error = Resources.WrongAddressFormat;
                        return;
                    }

                    Connect.CanBeExecuted = true;
                }
            }
        }

        /// <summary>
        /// Подключиться к конкретному серверу
        /// </summary>
        public CustomCommand Connect { get; private set; }

        private bool _connected = false;

        public bool Connected
        {
            get => _connected;
            set { if (_connected != value) { _connected = value; OnPropertyChanged(); } }
        }

        public CustomCommand Cancel { get; set; }

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public SINetworkViewModel(ConnectionData connectionData, CommonSettings commonSettings, UserSettings userSettings)
            : base(connectionData, commonSettings, userSettings)
        {

        }

        protected override void Initialize()
        {
            base.Initialize();

            Connect = new CustomCommand(Connect_Executed);

            var match = AddressRegex.Match(Address);
            Connect.CanBeExecuted = match.Success && int.TryParse(match.Groups["port"].Value, out int val);
        }

        protected override void Prepare(GameSettingsViewModel gameSettings)
        {
            base.Prepare(gameSettings);

            gameSettings.NetworkGameType = NetworkGameType.DirectConnection;
        }

        private async void Connect_Executed(object arg)
        {
            var match = AddressRegex.Match(_model.Address);
            ServerAddress = "http://" + _model.Address;
            await ConnectToServerAsync(match.Groups["host"].Value, int.Parse(match.Groups["port"].Value));
        }

        private async Task ConnectToServerAsync(string address, int port)
        {
            await InitServerAndClientAsync(address, port);

            IsProgress = true;
            Connect.CanBeExecuted = false;
            Error = "";
            Connected = false;

            try
            {
                await ConnectCoreAsync(false);

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var gameInfo = await _connector.GetGameInfoAsync();

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                GameData = ConnectionGameData.Create(gameInfo);
                Connected = true;
            }
            catch (Exception exc)
            {
                try
                {
                    Error = exc.Message;
                    FullError = exc.ToString();
                    await _server.DisposeAsync();

                    if (_connector != null)
                    {
                        _connector.Dispose();
                        _connector = null;
                    }
                }
                catch { }
            }
            finally
            {
                IsProgress = false;
                Connect.CanBeExecuted = true;
            }
        }

        public override ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();

            return base.DisposeAsync();
        }
    }
}
