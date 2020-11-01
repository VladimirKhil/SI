using SICore.BusinessLogic;
using SICore.Connections;
using SICore.Network;
using SICore.Network.Clients;

namespace SICore
{
    public sealed class ViewerActions
    {
        private readonly ViewerData _viewerData;

        public ILocalizer LO { get; }

        public Client Client { get; }

        public ViewerActions(Client client, ViewerData viewerData, ILocalizer localizer)
        {
            Client = client;
            _viewerData = viewerData;
            LO = localizer;
        }

        /// <summary>
        /// Отправить сообщение всем
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        public void SendMessage(string text) => Client.SendMessage(text, receiver: NetworkConstants.GameName);

        public void SendMessage(params string[] args) => Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

        public void SendMessageWithArgs(params object[] args) => Client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);

        /// <summary>
        /// Жмёт на игровую кнопку
        /// </summary>
        internal void PressGameButton() => SendMessage(Messages.I);

        public void Rename(string name)
        {
            Client.Name = name;

            if (_viewerData.AllPersons.TryGetValue(_viewerData.Name, out var viewerAccount))
            {
                viewerAccount.Name = name;
                _viewerData.AllPersons[name] = viewerAccount;
                _viewerData.AllPersons.Remove(_viewerData.Name);
            }

            _viewerData.Name = name;
        }
    }
}
