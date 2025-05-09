using SICore.Network.Clients;
using SICore.Network.Contracts;
using SIData;

namespace SICore;

/// <summary>
/// Defines a message handler.
/// </summary>
public abstract class Actor : IDisposable
{
    protected readonly Client _client;

    public IClient Client => _client;

    public abstract ValueTask OnMessageReceivedAsync(Message message);

    // TODO: Actor should be Client's handler and shouldn't have a link to client
    protected Actor(Client client)
    {
        _client = client;

        _client.MessageReceived += OnMessageReceivedAsync;
        _client.Disposed += Client_Disposed;
    }

    private void Client_Disposed()
    {
        try
        {
            Dispose();
        }
        catch (Exception exc)
        {
            Client.CurrentNode.OnError(exc, true);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        _client.MessageReceived -= OnMessageReceivedAsync;
        _client.Disposed -= Client_Disposed;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
