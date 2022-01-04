using Notions;
using SICore.BusinessLogic;
using SICore.Connections;
using SICore.Network;
using SICore.Network.Clients;
using System;
using System.Linq;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore
{
    public sealed class GameActions
    {
        private readonly GameData _gameData;
        private readonly ILocalizer LO;

        public Client Client { get; }

        public GameActions(Client client, GameData gameData, ILocalizer localizer)
        {
            Client = client;
            _gameData = gameData;
            LO = localizer;
        }

        public void SendMessage(string text, string receiver = NetworkConstants.Everybody) => Client.SendMessage(text, true, receiver);

        public void SendMessageWithArgs(params object[] args) => SendMessage(string.Join(Message.ArgsSeparator, args));

        public void SendMessageToWithArgs(string receiver, params object[] args) =>
            SendMessage(string.Join(Message.ArgsSeparator, args), receiver);

        /// <summary>
        /// Вывод в протокол
        /// </summary>
        /// <param name="text">Текст</param>
        public void Print(string text) => SendMessageWithArgs(Messages.Print, text);

        internal void SystemReplic(string text) => UserMessage(MessageTypes.System, text);

        internal void SpecialReplic(string text) => UserMessage(MessageTypes.Special, text);

        internal void ShowmanReplic(string text) => UserMessage(MessageTypes.Replic, text, GameRole.Showman);

        internal void PlayerReplic(int playerIndex, string text) => UserMessage(MessageTypes.Replic, text, GameRole.Player, playerIndex);

        /// <summary>
        /// Пользовательское сообщение
        /// </summary>
        /// <param name="messageType">Тип сообщения</param>
        /// <param name="text">Текст сообщения</param>
        /// <param name="personRole">Роль источника сообщения (для реплик)</param>
        /// <param name="personIndex">Индекс источника сообщения (для реплик игроков)</param>
        internal void UserMessage(MessageTypes messageType, string text, GameRole? personRole = null, int? personIndex = null)
        {
            switch (messageType)
            {
                case MessageTypes.System:
                    Print(ReplicManager.System(text));
                    break;

                case MessageTypes.Special:
                    Print(ReplicManager.Special(text));
                    break;

                case MessageTypes.Replic:
                    if (!personRole.HasValue)
                    {
                        throw new ArgumentNullException(nameof(personRole));
                    }

                    switch (personRole.Value)
                    {
                        case GameRole.Viewer: // Не используется
                            break;

                        case GameRole.Player:
                            if (!personIndex.HasValue)
                            {
                                throw new ArgumentNullException(nameof(personIndex));
                            }

                            Print(Player(personIndex.Value) + ReplicManager.Replic(text));
                            break;

                        case GameRole.Showman:
                            Print(Showman() + ReplicManager.Replic(text));
                            break;
                    }

                    break;
            }

            var person = messageType == MessageTypes.System ? ReplicCodes.System.ToString()
                : messageType == MessageTypes.Special ? ReplicCodes.Special.ToString() :
                (personRole == GameRole.Player ? ReplicCodes.Player + personIndex.Value.ToString() : ReplicCodes.Showman.ToString());

            SendMessageWithArgs(Messages.Replic, person, text);
        }

        /// <summary>
        /// Зритель
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Viewer(string s) => $"<viewer>{ReplicManager.Escape(s)}</viewer>";

        /// <summary>
        /// Игрок
        /// </summary>
        /// <param name="n">Номер игрока</param>
        /// <returns></returns>
        public static string Player(int n) => $"<player>{n}</player>";

        /// <summary>
        /// Ведущий
        /// </summary>
        /// <returns></returns>
        public string Showman() => $"<showman>{ReplicManager.Escape(_gameData.ShowMan.Name)}</showman>";

        /// <summary>
        /// Выдача информации о счёте
        /// </summary>
        /// <param name="person">Кому выдаётся</param>
        public void InformSums(string person = NetworkConstants.Everybody)
        {
            var message = new StringBuilder(Messages.Sums);
            for (var i = 0; i < _gameData.Players.Count; i++)
            {
                message.Append(Message.ArgsSeparatorChar).Append(_gameData.Players[i].Sum);
            }

            SendMessage(message.ToString(), person);
        }

        /// <summary>
        /// Объявить суммы
        /// </summary>
        public void AnnounceSums()
        {
            var s = new StringBuilder(LO[nameof(R.Score)]).Append(": ");
            var total = _gameData.Players.Count;
            for (var i = 0; i < total; i++)
            {
                s.Append(Notion.FormatNumber(_gameData.Players[i].Sum));
                if (i < total - 1)
                {
                    s.Append("; ");
                }
            }

            SystemReplic(s.ToString());
        }

        /// <summary>
        /// Информация о табло
        /// </summary>
        public void InformTablo(string receiver = NetworkConstants.Everybody)
        {
            var message2 = new StringBuilder(Messages.Table);

            for (int i = 0; i < _gameData.TInfo.RoundInfo.Count; i++)
            {
                for (int j = 0; j < _gameData.TInfo.RoundInfo[i].Questions.Count; j++)
                {
                    message2.Append(Message.ArgsSeparatorChar);
                    message2.Append(_gameData.TInfo.RoundInfo[i].Questions[j].Price);
                }
                message2.Append(Message.ArgsSeparatorChar); // Новый формат сообщения предусматривает разделение вопросов одной темы
            }

            SendMessage(message2.ToString(), receiver);
        }

        /// <summary>
        /// Выдача информации о состоянии игры
        /// </summary>
        public void InformStage(string person = NetworkConstants.Everybody, string name = null) =>
            SendMessage(string.Join(Message.ArgsSeparator, Messages.Stage, _gameData.Stage.ToString(), name ?? ""), person);

        internal void InformRoundThemes(string person = NetworkConstants.Everybody, bool play = true)
        {
            var msg = new StringBuilder(Messages.RoundThemes)
                .Append(Message.ArgsSeparatorChar)
                .Append(play ? '+' : '-')
                .Append(Message.ArgsSeparatorChar)
                .Append(string.Join(Message.ArgsSeparator, _gameData.TInfo.RoundInfo.Select(info => info.Name)));

            SendMessage(msg.ToString(), person);
        }
    }
}
