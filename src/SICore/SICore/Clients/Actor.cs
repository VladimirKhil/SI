using SICore.BusinessLogic;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SIData;

namespace SICore;

/// <summary>
/// Обработчик сообщений
/// </summary>
/// <typeparam name="D">Тип данных клиента</typeparam>
/// <typeparam name="L">Тип логики клиента</typeparam>
public abstract class Actor<D> : IActor
    where D : Data
{
    protected Client _client;

    /// <summary>
    /// Данные клиента
    /// </summary>
    public D ClientData { get; private set; }

    public IClient Client => _client;

    public abstract ValueTask OnMessageReceivedAsync(Message message);

    public ILocalizer LO { get; protected set; }

    // TODO: Actor should be Client's handler and do not have a link to the client
    protected Actor(Client client, ILocalizer localizer, D data)
    {
        _client = client;
        ClientData = data;

        _client.MessageReceived += OnMessageReceivedAsync;
        _client.Disposed += Client_Disposed;

        LO = localizer;
    }

    private void Client_Disposed()
    {
        try
        {
            Dispose();
            ClientData.EventLog.Append("Client disposed");
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
