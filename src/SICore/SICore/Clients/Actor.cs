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
public abstract class Actor<D, L> : IActor
    where D : Data
    where L : class, ILogic
{
    protected Client _client;

    /// <summary>
    /// Логика клиента
    /// </summary>
    protected L _logic;

    /// <summary>
    /// Данные клиента
    /// </summary>
    public D ClientData { get; private set; }

    public L Logic => _logic;

    public IClient Client => _client;

    public abstract ValueTask OnMessageReceivedAsync(Message message);

    public ILocalizer LO { get; protected set; }

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
            Client.CurrentServer.OnError(exc, true);
        }
    }

    public void AddLog(string s) => _logic.AddLog(s);

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
