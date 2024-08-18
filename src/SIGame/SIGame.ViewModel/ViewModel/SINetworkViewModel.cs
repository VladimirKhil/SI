using SICore;
using SICore.Network.Clients;
using SICore.Network.Configuration;
using SICore.Network.Servers;
using SICore.Network;
using SIData;
using SIGame.ViewModel.Models;
using SIGame.ViewModel.Properties;
using SIUI.ViewModel;
using System.Text.RegularExpressions;
using Utils.Commands;
using System.Windows.Input;

namespace SIGame.ViewModel;

/// <summary>
/// Provides a view model for network (direct) connection.
/// </summary>
public sealed class SINetworkViewModel : ConnectionDataViewModel
{
    private static readonly Regex AddressRegex = new(@"^(?<host>(\d{1,3}\.){3}\d{1,3})\:(?<port>\d+)$");

    private Connector? _connector;

    protected override bool IsOnline => false;

    private ConnectionGameData? _gameData = null;

    public ConnectionGameData? GameData
    {
        get => _gameData;
        set
        {
            if (_gameData != value)
            {
                _gameData = value;
                OnPropertyChanged();

                if (value != null)
                {
                    UpdateJoinCommand(value.Persons);
                }
            }
        }
    }

    /// <summary>
    /// Host address.
    /// </summary>
    public string Address
    {
        get => _model.Address;
        set
        {
            var newValue = value.Trim();

            Error = "";
            if (_model.Address != newValue)
            {
                _model.Address = newValue;
                OnPropertyChanged();

                var match = AddressRegex.Match(newValue);
                if (!match.Success || !int.TryParse(match.Groups["port"].Value, out _))
                {
                    Error = Resources.WrongAddressFormat;
                    Connect.CanBeExecuted = false;
                    return;
                }

                Connect.CanBeExecuted = true;
            }
        }
    }

    /// <summary>
    /// Connect to the host specified by the direct address.
    /// </summary>
    public SimpleCommand Connect { get; private set; }

    private bool _connected = false;

    public bool Connected
    {
        get => _connected;
        set { if (_connected != value) { _connected = value; OnPropertyChanged(); } }
    }

    public ICommand Cancel { get; set; }

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public SINetworkViewModel(ConnectionData connectionData, CommonSettings commonSettings, UserSettings userSettings, SettingsViewModel settingsViewModel)
        : base(connectionData, commonSettings, userSettings, settingsViewModel)
    {

    }

    protected override void Initialize()
    {
        base.Initialize();

        Connect = new SimpleCommand(Connect_Executed);

        var match = AddressRegex.Match(Address);
        Connect.CanBeExecuted = match.Success && int.TryParse(match.Groups["port"].Value, out int val);
    }

    private async Task InitServerAndClientAsync(string address, int port)
    {
        if (_node != null)
        {
            await _node.DisposeAsync();
            _node = null;
        }

        _client = new Client(Human.Name);

        _node = new TcpSlaveServer(
            port,
            address,
            NodeConfiguration.Default,
            new NetworkLocalizer(Thread.CurrentThread.CurrentUICulture.Name));

        _client.ConnectTo(_node);
    }

    protected override void Prepare(GameSettingsViewModel gameSettings)
    {
        base.Prepare(gameSettings);

        gameSettings.NetworkGameType = NetworkGameType.DirectConnection;
    }

    private async void Connect_Executed(object? arg)
    {
        var match = AddressRegex.Match(_model.Address);

        if (!match.Success || !int.TryParse(match.Groups["port"].Value, out var port))
        {
            Error = Resources.WrongAddressFormat;
            return;
        }

        ServerAddress = "http://" + _model.Address;

        await ConnectToServerAsync(match.Groups["host"].Value, port);
    }

    private async Task ConnectCoreAsync(bool upgrade)
    {
        await _node.ConnectAsync(upgrade);

        _connector?.Dispose();

        _connector = new Connector(_node, _client);
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
                await _node.DisposeAsync();
                _node = null;

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

    public override async Task<GameViewModel?> JoinGameCoreAsync(
        GameInfo? gameInfo,
        GameRole role,
        bool isHost = false,
        CancellationToken cancellationToken = default)
    {
        var name = Human.Name;

        var sex = Human.IsMale ? 'm' : 'f';
        var command = $"{Messages.Connect}\n{role.ToString().ToLowerInvariant()}\n{name}\n{sex}\n{-1}";

        _ = await _connector.JoinGameAsync(command);

        _connector.Dispose();
        _connector = null;

        return await JoinGameCompletedAsync(role, isHost, cancellationToken);
    }

    public override ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        _connector?.Dispose();

        return base.DisposeAsync();
    }
}
