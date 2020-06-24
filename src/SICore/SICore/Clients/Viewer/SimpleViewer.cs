using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;
using System;

namespace SICore
{
    public sealed class SimpleViewer : Viewer<IViewer>
    {
        /// <summary>
        /// Запуск клиента
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="password">Пароль (необязателен)</param>
        /// <param name="isHuman">Человек ли</param>
        /// <param name="isHost">Является ли владельцем сервера</param>
        /// <param name="form">Форма для интерфейса (если не человек, то null)</param>
        public SimpleViewer(Client client, Account personData, bool isHost, IGameManager backLink, ILocalizer localizer, ViewerData data = null)
            : base(client, personData, isHost, backLink, localizer, data)
        {
            
        }

        protected override IViewer CreateLogic(Account personData)
        {
            if (personData == null)
            {
                throw new ArgumentNullException(nameof(personData));
            }

            return personData.IsHuman ?
                (IViewer)new SimpleViewerHumanLogic(this, ClientData) :
                new SimpleViewerComputerLogic(this, ClientData);
        }
    }
}
