using SICore.Connections;
using SICore.Network.Configuration;
using SICore.Network.Contracts;
using SIData;
using R = SICore.Network.Properties.Resources;

namespace SICore.Network.Servers;

/// <summary>
/// Defines a main game server node.
/// </summary>
public class MasterNode : Node, IMasterNode
{
    private bool _isDisposed;

    /// <summary>
    /// Links to other nodes.
    /// </summary>
    protected List<IConnection> _connections = new();
    
    /// <summary>
    /// A collection of banned persons.
    /// </summary>
    private readonly Dictionary<string, (string UserName, DateTime BannedUtil)> _banned = new();

    public override bool IsMain => true;

    public override IEnumerable<IConnection> Connections => _connections;

    public IReadOnlyDictionary<string, string> Banned => _banned.ToDictionary(entry => entry.Key, entry => entry.Value.UserName);

    public event Action<string>? Unbanned;

    public MasterNode(NodeConfiguration serverConfiguration, INetworkLocalizer localizer)
        : base(serverConfiguration, localizer)
    {

    }

    public override async ValueTask<bool> AddConnectionAsync(IConnection connection, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return false;
        }

        var address = connection.RemoteAddress;

        if (_banned.TryGetValue(address, out var bannedEntry))
        {
            if (bannedEntry.BannedUtil > DateTime.UtcNow)
            {
                var bannedUntil = bannedEntry.BannedUtil == DateTime.MaxValue
                    ? ""
                    : $" {_localizer[nameof(R.Until)]} {bannedEntry.BannedUtil.ToString(_localizer.Culture)}";

                await connection.SendMessageAsync(
                    new Message(
                        $"{SystemMessages.Refuse}\n{_localizer[nameof(R.ConnectionDenied)]}{bannedUntil}\r\n",
                        NetworkConstants.GameName));

                DropConnection(connection);
                return false;
            }
            else
            {
                _banned.Remove(address);
                Unbanned?.Invoke(address);
            }
        }

        await base.AddConnectionAsync(connection, cancellationToken);

        await ConnectionsLock.WithLockAsync(
            () =>
            {
                _connections.Add(connection);
            },
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Waits and kills the connection. A wait is needed to prevent the client flooding.
    /// </summary>
    /// <remarks>It must be performed in a separate thread (`async void`) not to slow down the common connections listener.</remarks>
    /// <param name="connection">Connection to kill.</param>
    private async void DropConnection(IConnection connection)
    {
        try
        {
            await Task.Delay(4000);
            await connection.DisposeAsync();
        }
        catch (Exception exc)
        {
            OnError(exc, true);
        }
    }

    public override async ValueTask RemoveConnectionAsync(
        IConnection connection,
        bool withError,
        CancellationToken cancellationToken = default)
    {
        await ConnectionsLock.WithLockAsync(
            () =>
            {
                if (_connections.Contains(connection))
                {
                    _connections.Remove(connection);
                }
            },
            cancellationToken);

        await base.RemoveConnectionAsync(connection, withError, cancellationToken);
    }

    public string Kick(string name, bool ban = false)
    {
        IConnection? connectionToClose = null;

        string? address = null;

        ConnectionsLock.WithLock(() =>
        {
            foreach (var connection in _connections)
            {
                if (connection.UserName == name)
                {
                    address = connection.RemoteAddress;

                    if (address.Length > 0)
                    {
                        _banned[address] = (name, ban ? DateTime.MaxValue : DateTime.UtcNow.AddMinutes(5.0));
                    }

                    connectionToClose = connection;
                    break;
                }
            }
        });

        if (connectionToClose != null)
        {
            Connection_ConnectionClosed(connectionToClose, false);
        }

        return address ?? "";
    }

    public void Unban(string name)
    {
        if (_banned.Remove(name))
        {
            Unbanned?.Invoke(name);
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        await ConnectionsLock.TryLockAsync(
            async () =>
            {
                foreach (var connection in _connections)
                {
                    ClearListeners(connection);
                    await connection.DisposeAsync();
                }

                _connections.Clear();
            },
            5000,
            true);

        _isDisposed = true;

        await base.DisposeAsync(disposing);
    }
}
