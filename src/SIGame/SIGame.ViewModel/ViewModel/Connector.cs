using SICore;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Servers;
using SIData;
using System.Diagnostics;

namespace SIGame.ViewModel;

public sealed class Connector : IDisposable
{
    private readonly SecondaryNode _server;
    private readonly Client _client;

    private TaskCompletionSource<string[]> _tcs;

    public Connector(SecondaryNode server, Client client)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
        _client = client ?? throw new ArgumentNullException(nameof(client));

        client.MessageReceived += ProcessMessageAsync;
    }

    public Task<string[]> GetGameInfoAsync()
    {
        _tcs = new TaskCompletionSource<string[]>();

        _server.ConnectionsLock.WithLock(async () =>
        {
            if (_server.HostServer == null)
            {
                Trace.TraceError("GetGameInfoAsync: _server.HostServer == null");
                return;
            }

            await _server.HostServer.SendMessageAsync(new Message(Messages.GameInfo, "", ""));
        });

        return _tcs.Task;
    }

    public Task<string[]> JoinGameAsync(string command)
    {
        _tcs = new TaskCompletionSource<string[]>();

        var m = new Message(command, "", "");

        _server.ConnectionsLock.WithLock(async () =>
        {
            if (_server.HostServer == null)
            {
                Trace.TraceError("JoinGameAsync: _server.HostServer == null");
                return;
            }

            await _server.HostServer.SendMessageAsync(m);
        });

        return _tcs.Task;
    }

    private ValueTask ProcessMessageAsync(Message m)
    {
        var text = m.Text?.Split(Message.ArgsSeparatorChar);

        if (text?.Length == 0)
        {
            return default;
        }

        if (_tcs != null)
        {
            switch (text[0])
            {
                case Messages.GameInfo:
                    _tcs.TrySetResult(text);
                    break;

                case Messages.Accepted:
                    _tcs.TrySetResult(text);
                    break;

                case SystemMessages.Refuse:
                    if (text.Length > 1)
                        _tcs.TrySetException(new Exception(text[1]));
                    break;
            }
        }

        return default;
    }

    public void Dispose() => _client.MessageReceived -= ProcessMessageAsync;
}
