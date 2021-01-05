using SICore.BusinessLogic;
using SICore.Connections;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SIData;
using System;
using System.Threading.Tasks;

namespace SICore
{
    /// <summary>
    /// Обработчик сообщений
    /// </summary>
    /// <typeparam name="D">Тип данных клиента</typeparam>
    /// <typeparam name="L">Тип логики клиента</typeparam>
    public abstract class Actor<D, L> : IActor
        where D : Data, new()
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
        protected abstract L CreateLogic(Account personData);

        public ILocalizer LO { get; protected set; }

        protected Actor(Client client, Account personData, ILocalizer localizer, D data)
        {
            _client = client;
            ClientData = data;

            _client.MessageReceived += OnMessageReceivedAsync;
            _client.Disposed += DisposeAsync;
            _client.InfoReplaced += Client_InfoReplaced;

            LO = localizer;
        }

        private void Client_InfoReplaced(IAccountInfo data) => _logic.SetInfo(data);

        public void AddLog(string s) => _logic.AddLog(s);

        public virtual async ValueTask DisposeAsync(bool disposing)
        {
            _client.MessageReceived -= OnMessageReceivedAsync;
            _client.Disposed -= DisposeAsync;
            _client.InfoReplaced -= Client_InfoReplaced;

            await _logic.DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
    }
}
