using Notions;
using SICore.BusinessLogic;
using SICore.Connections;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SICore.Results;
using SICore.Special;
using SICore.Utils;
using SIData;
using SIPackages;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Клиент игры
    /// </summary>
    public sealed class Game : Actor<GameData, GameLogic>
    {
        public event Action<GameStages, string> StageChanged;
        public event Action<bool, bool> PersonsChanged;
        public event Action<string> DisconnectRequested;
        public event Action<string, int, int> AdShown;

        private readonly GameActions _gameActions;

        internal void OnStageChanged(GameStages stage, string stageName) => StageChanged?.Invoke(stage, stageName);

        internal void OnAdShown(int adId) => AdShown?.Invoke(LO.Culture.TwoLetterISOLanguageName, adId, ClientData.AllPersons.Values.Count(p => p.IsHuman));

        private IMasterServer MasterServer => (IMasterServer)_client.Server;

        public ComputerAccount[] DefaultPlayers { get; set; }
        public ComputerAccount[] DefaultShowmans { get; set; }

        public Game(Client client, string documentPath, ILocalizer localizer, GameData gameData)
            : base(client, null, localizer, gameData)
        {
            _gameActions = new GameActions(_client, ClientData, LO);
            _logic = CreateLogic(null);

            gameData.DocumentPath = documentPath;
            gameData.Share.Error += Share_Error;
        }

        protected override GameLogic CreateLogic(Account personData) => new GameLogic(this, ClientData, _gameActions, LO);

        public override void Dispose(bool disposing)
        {
            if (ClientData.Share != null)
            {
                ClientData.Share.Error -= Share_Error;
                ClientData.Share.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Запуск игры
        /// </summary>
        /// <param name="settings">Настройки</param>
        public void Run(SIDocument document)
        {
            Client.CurrentServer.SerializationError += CurrentServer_SerializationError;

            _logic.Run(document);
            foreach (var personName in ClientData.AllPersons.Keys)
            {
                if (personName == NetworkConstants.GameName)
                {
                    continue;
                }

                Inform(personName);
            }
        }

        private void CurrentServer_SerializationError(Message message, Exception exc)
        {
            // Это случается при выводе частичного текста. Пытаемся поймать
            try
            {
                var fullText = ClientData.Text ?? "";
                var errorMessage = new StringBuilder("SerializaionError: ")
                    .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(fullText)))
                    .Append('\n')
                    .Append(ClientData.TextLength)
                    .Append('\n')
                    .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Sender)))
                    .Append('\n')
                    .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Receiver)))
                    .Append('\n')
                    .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(message.Text)))
                    .Append('\n')
                    .Append((message.Text ?? "").Length)
                    .Append(' ').Append(ClientData.Settings.AppSettings.ReadingSpeed);

                _client.Server.OnError(new Exception(errorMessage.ToString(), exc), true);
            }
            catch (Exception e)
            {
                _client.Server.OnError(e, true);
            }
        }

        private void Share_Error(Exception exc) => _client.Server.OnError(exc, true);

        /// <summary>
        /// Отправка данных об игре
        /// </summary>
        /// <param name="person">Тот, кого надо проинформировать</param>
        private void Inform(string person = NetworkConstants.Everybody)
        {
            var info = new StringBuilder(Messages.Info2)
                .Append(Message.ArgsSeparatorChar)
                .Append(ClientData.Players.Count)
                .Append(Message.ArgsSeparatorChar);

            AppendAccountExt(ClientData.ShowMan, info);
            info.Append(Message.ArgsSeparatorChar);

            foreach (var player in ClientData.Players)
            {
                AppendAccountExt(player, info);
                info.Append(Message.ArgsSeparatorChar);
            }

            foreach (var viewer in ClientData.Viewers)
            {
                if (!viewer.IsConnected)
                {
                    ClientData.BackLink.LogWarning($"Viewer {viewer.Name} not connected\n" + ClientData.PersonsUpdateHistory);
                    continue;
                }

                AppendAccountExt(viewer, info);
                info.Append(Message.ArgsSeparatorChar);
            }

            var msg = info.ToString().Substring(0, info.Length - 1);

            _gameActions.SendMessage(msg, person);

            // Сообщим об адресах картинок
            if (person != NetworkConstants.Everybody)
            {
                InformPicture(ClientData.ShowMan, person);
                foreach (var item in ClientData.Players)
                {
                    InformPicture(item, person);
                }
            }
            else
            {
                InformPicture(ClientData.ShowMan);
                foreach (var item in ClientData.Players)
                {
                    InformPicture(item);
                }
            }

            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.ReadingSpeed, ClientData.Settings.AppSettings.ReadingSpeed), person);
            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.FalseStart, ClientData.Settings.AppSettings.FalseStart ? "+" : "-"), person);
            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.ButtonBlockingTime, ClientData.Settings.AppSettings.TimeSettings.TimeForBlockingButton), person);

            var maxPressingTime = ClientData.Settings.AppSettings.TimeSettings.TimeForThinkingOnQuestion * 10;
            _gameActions.SendMessageWithArgs(Messages.Timer, 1, "MAXTIME", maxPressingTime);
        }

        private void AppendAccountExt(ViewerAccount account, StringBuilder info)
        {
            info.Append(account.Name);
            info.Append(Message.ArgsSeparatorChar);
            info.Append(account.IsMale ? '+' : '-');
            info.Append(Message.ArgsSeparatorChar);
            info.Append(account.IsConnected ? '+' : '-');
            info.Append(Message.ArgsSeparatorChar);
            info.Append(account.IsHuman ? '+' : '-');
            info.Append(Message.ArgsSeparatorChar);

            info.Append(account is GamePersonAccount person && person.Ready ? '+' : '-');
        }

        public string GetSums()
        {
            var s = new StringBuilder();
            var total = ClientData.Players.Count;
            for (int i = 0; i < total; i++)
            {
                if (s.Length > 0)
                {
                    s.Append(", ");
                }

                s.AppendFormat("{0}: {1}", ClientData.Players[i].Name, ClientData.Players[i].Sum);
            }

            return s.ToString();
        }

        public ConnectionPersonData[] GetInfo()
        {
            var result = new List<ConnectionPersonData>
            {
                new ConnectionPersonData { Name = ClientData.ShowMan.Name, Role = GameRole.Showman, IsOnline = ClientData.ShowMan.IsConnected }
            };

            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                result.Add(new ConnectionPersonData
                {
                    Name = ClientData.Players[i].Name,
                    Role = GameRole.Player,
                    IsOnline = ClientData.Players[i].IsConnected
                });
            }

            for (int i = 0; i < ClientData.Viewers.Count; i++)
            {
                result.Add(new ConnectionPersonData
                {
                    Name = ClientData.Viewers[i].Name,
                    Role = GameRole.Viewer,
                    IsOnline = ClientData.Viewers[i].IsConnected
                });
            }

            return result.ToArray();
        }

        /// <summary>
        /// Присоединить участника к игре
        /// </summary>
        public bool Join(string name, bool isMale, GameRole role, string password, Action connectionAuthenticator, out string message)
        {
            lock (ClientData.TaskLock)
            {
                if (!string.IsNullOrEmpty(ClientData.Settings.NetworkGamePassword)
                    && ClientData.Settings.NetworkGamePassword != password)
                {
                    message = LO[nameof(R.WrongPassword)];
                    return false;
                }

                // Подсоединение к игре
                if (ClientData.AllPersons.ContainsKey(name))
                {
                    message = string.Format(LO[nameof(R.PersonWithSuchNameIsAlreadyInGame)], name);
                    return false;
                }

                var index = -1;
                IEnumerable<ViewerAccount> accountsToSearch = null;
                switch (role)
                {
                    case GameRole.Showman:
                        accountsToSearch = new ViewerAccount[1] { ClientData.ShowMan };
                        break;

                    case GameRole.Player:
                        accountsToSearch = ClientData.Players;
                        if (ClientData.HostName == name) // Подключение организатора
                        {
                            var players = ClientData.Players;
                            for (var i = 0; i < players.Count; i++)
                            {
                                if (players[i].Name == name)
                                {
                                    index = i;
                                    break;
                                }
                            }

                            if (index < 0)
                            {
                                message = LO[nameof(R.PositionNotFoundByIndex)];
                                return false;
                            }
                        }

                        break;

                    default: // Viewer
                        accountsToSearch = ClientData.Viewers.Concat(new ViewerAccount[] { new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true } });
                        break;
                }

                var found = false;

                if (index > -1)
                {
                    var accounts = accountsToSearch.ToArray();

                    var result = CheckAccountNew(role.ToString().ToLower(), name, isMale ? "m" : "f", ref found, index,
                        accounts[index], connectionAuthenticator);

                    if (result.HasValue)
                    {
                        if (!result.Value)
                        {
                            message = LO[nameof(R.PlaceIsOccupied)];
                            return false;
                        }
                        else
                        {
                            found = true;
                        }
                    }
                }
                else
                {
                    foreach (var item in accountsToSearch)
                    {
                        index++;
                        var result = CheckAccountNew(role.ToString().ToLower(), name, isMale ? "m" : "f", ref found, index, item,
                            connectionAuthenticator);

                        if (result.HasValue)
                        {
                            if (!result.Value)
                            {
                                message = LO[nameof(R.PlaceIsOccupied)];
                                return false;
                            }
                            else
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (!found)
                {
                    message = LO[nameof(R.NoFreePlaceForName)];
                    return false;
                }

                message = "";
                return true;
            }
        }

        /// <summary>
        /// Получение сообщения
        /// </summary>
        public override void OnMessageReceived(Message message)
        {
            lock (ClientData.TaskLock)
            {
                if (string.IsNullOrEmpty(message.Text))
                {
                    return;
                }

                var args = message.Text.Split(Message.ArgsSeparatorChar);

                try
                {
                    var res = new StringBuilder();
                    // Действие согласно протоколу
                    switch (args[0])
                    {
                        case Messages.GameInfo:
                            #region GameInfo

                            // Информация о текущей игре для подключающихся по сети
                            res.Append(Messages.GameInfo);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Settings.NetworkGameName);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.HostName);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players.Count);

                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.ShowMan.Name);
                            res.Append(Message.ArgsSeparatorChar).Append(ClientData.ShowMan.IsConnected ? '+' : '-');
                            res.Append(Message.ArgsSeparatorChar).Append('-');

                            for (int i = 0; i < ClientData.Players.Count; i++)
                            {
                                res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players[i].Name);
                                res.Append(Message.ArgsSeparatorChar).Append(ClientData.Players[i].IsConnected ? '+' : '-');
                                res.Append(Message.ArgsSeparatorChar).Append('-');
                            }

                            for (int i = 0; i < ClientData.Viewers.Count; i++)
                            {
                                res.Append(Message.ArgsSeparatorChar).Append(ClientData.Viewers[i].Name);
                                res.Append(Message.ArgsSeparatorChar).Append(ClientData.Viewers[i].IsConnected ? '+' : '-');
                                res.Append(Message.ArgsSeparatorChar).Append('-');
                            }

                            _gameActions.SendMessage(res.ToString(), message.Sender);

                            #endregion
                            break;

                        case Messages.Connect:
                            #region Connect
                            {
                                if (args.Length < 4)
                                {
                                    _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.WrongConnectionParameters)], message.Sender);
                                    return;
                                }

                                if (!string.IsNullOrEmpty(ClientData.Settings.NetworkGamePassword) && (args.Length < 6 || ClientData.Settings.NetworkGamePassword != args[5]))
                                {
                                    _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.WrongPassword)], message.Sender);
                                    return;
                                }

                                var role = args[1];
                                var name = args[2];
                                var sex = args[3];

                                // Подсоединение к игре
                                if (ClientData.AllPersons.ContainsKey(name))
                                {
                                    _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + string.Format(LO[nameof(R.PersonWithSuchNameIsAlreadyInGame)], name), message.Sender);
                                    return;
                                }

                                var index = -1;
                                IEnumerable<ViewerAccount> accountsToSearch = null;
                                switch (role)
                                {
                                    case Constants.Showman:
                                        accountsToSearch = new ViewerAccount[1] { ClientData.ShowMan };
                                        break;

                                    case Constants.Player:
                                        accountsToSearch = ClientData.Players;
                                        if (ClientData.HostName == name) // Подключение организатора
                                        {
                                            var defaultPlayers = ClientData.Settings.Players;
                                            for (int i = 0; i < defaultPlayers.Length; i++)
                                            {
                                                if (defaultPlayers[i].Name == name)
                                                {
                                                    index = i;
                                                    break;
                                                }
                                            }

                                            if (index < 0 || index >= ClientData.Players.Count)
                                            {
                                                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, SystemMessages.Refuse, LO[nameof(R.PositionNotFoundByIndex)]), message.Sender);
                                                return;
                                            }
                                        }

                                        break;

                                    default:
                                        accountsToSearch = ClientData.Viewers.Concat(new ViewerAccount[] { new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true } });
                                        break;
                                }

                                var found = false;

                                if (index > -1)
                                {
                                    var accounts = accountsToSearch.ToArray();

                                    var result = CheckAccount(message, role, name, sex, ref found, index, accounts[index]);
                                    if (result.HasValue)
                                    {
                                        if (!result.Value)
                                            return;
                                    }
                                }
                                else
                                {
                                    foreach (var item in accountsToSearch)
                                    {
                                        index++;
                                        var result = CheckAccount(message, role, name, sex, ref found, index, item);
                                        if (result.HasValue)
                                        {
                                            if (!result.Value)
                                                return;
                                            else
                                                break;
                                        }
                                    }
                                }

                                if (!found)
                                {
                                    _gameActions.SendMessage(SystemMessages.Refuse + Message.ArgsSeparatorChar + LO[nameof(R.NoFreePlaceForName)], message.Sender);
                                }
                            }
                            #endregion
                            break;

                        case SystemMessages.Disconnect:
                            #region Disconnect
                            {
                                if (args.Length < 3 || !ClientData.AllPersons.TryGetValue(args[1], out var account))
                                {
                                    return;
                                }

                                var withError = args[2] == "+";

                                res.Append(LO[account.IsMale ? nameof(R.Disconnected_Male) : nameof(R.Disconnected_Female)])
                                    .Append(' ')
                                    .Append(account.Name);

                                _gameActions.SpecialReplic(res.ToString());

                                _gameActions.SendMessageWithArgs(Messages.Disconnected, account.Name);

                                account.IsConnected = false;

                                if (ClientData.Viewers.Contains(account))
                                {
                                    ClientData.Viewers.Remove(account);
                                    ClientData.OnAllPersonsChanged();
                                }
                                else
                                {
                                    var isBefore = ClientData.Stage == GameStage.Before;
                                    if (account is GamePersonAccount person)
                                    {
                                        person.Name = Constants.FreePlace;
                                        person.Picture = "";
                                        if (isBefore)
                                        {
                                            person.Ready = false;
                                        }
                                    }
                                }

                                if (args[1] == ClientData.HostName && ClientData.Settings.AppSettings.Managed && !_logic.IsRunning)
                                {
                                    ClientData.MoveDirection = 1; // Дальше
                                    _logic.Stop(StopReason.Move);
                                }

                                OnPersonsChanged(false, withError);
                            }
                            #endregion
                            break;

                        case Messages.Info:
                            #region Info

                            Inform(message.Sender);
                            foreach (var item in ClientData.MainPersons)
                            {
                                if (item.Ready)
                                {
                                    _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
                                }
                            }

                            _gameActions.InformStage(message.Sender);
                            _gameActions.InformSums(message.Sender);

                            if (ClientData.Stage == GameStage.Round)
                            {
                                lock (ClientData.TabloInformStageLock)
                                {
                                    if (ClientData.TabloInformStage > 0)
                                    {
                                        _gameActions.InformRoundThemes(message.Sender, false);
                                        if (ClientData.TabloInformStage > 1)
                                        {
                                            _gameActions.InformTablo(message.Sender);
                                        }
                                    }
                                }
                            }
                            else if (ClientData.Stage == GameStage.Before && ClientData.Settings.IsAutomatic)
                            {
                                var leftTimeBeforeStart = Constants.AutomaticGameStartDuration - (int)(DateTime.UtcNow - ClientData.TimerStartTime[2]).TotalSeconds * 10;

                                if (leftTimeBeforeStart > 0)
                                {
                                    _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Timer, 2, MessageParams.Timer_Go, leftTimeBeforeStart, -2), message.Sender);
                                }
                            }

                            #endregion
                            break;

                        case Messages.Config:
                            ProcessConfig(message, args);
                            break;

                        case Messages.First:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.StarterChoosing
                                && message.Sender == ClientData.ShowMan.Name && args.Length > 1)
                            {
                                #region First
                                // Ведущий прислал номер того, кто начнёт игру
                                if (int.TryParse(args[1], out int playerIndex) && playerIndex > -1 && playerIndex < ClientData.Players.Count && ClientData.Players[playerIndex].Flag)
                                {
                                    ClientData.ChooserIndex = playerIndex;
                                    _logic.Stop(StopReason.Decision);
                                }
                                #endregion
                            }
                            break;

                        case Messages.Pause:
                            OnPause(message, args);
                            break;

                        case Messages.Start:
                            if (message.Sender == ClientData.HostName)
                            {
                                StartGame();
                            }
                            break;

                        case Messages.Ready:
                            if (ClientData.Stage == GameStage.Before)
                            {
                                #region Ready
                                // Игрок или ведущий готов приступить к игре
                                res.Append(Messages.Ready).Append(Message.ArgsSeparatorChar);
                                var readyAll = true;
                                var found = false;
                                var toReady = args.Length == 1 || args[1] == "+";
                                foreach (var item in ClientData.MainPersons)
                                {
                                    if (message.Sender == item.Name && (toReady && !item.Ready || !toReady && item.Ready))
                                    {
                                        item.Ready = toReady;
                                        res.Append(message.Sender).Append(Message.ArgsSeparatorChar).Append(toReady ? "+" : "-");
                                        found = true;
                                    }

                                    readyAll = readyAll && item.Ready;
                                }

                                if (found)
                                {
                                    _gameActions.SendMessage(res.ToString());
                                }

                                if (readyAll)
                                {
                                    StartGame();
                                }
                                else if (ClientData.Settings.IsAutomatic)
                                {
                                    if (ClientData.Players.All(player => player.IsConnected))
                                    {
                                        StartGame();
                                    }
                                }

                                #endregion
                            }
                            break;

                        case Messages.Picture:
                            #region Picture
                            {
                                var path = args[1];
                                var person = ClientData.MainPersons.FirstOrDefault(item => message.Sender == item.Name);

                                if (person == null)
                                {
                                    return;
                                }

                                if (args.Length > 2)
                                {
                                    var file = message.Sender + "_" + Path.GetFileName(path);
                                    string uri;
                                    if (!ClientData.Share.ContainsURI(file))
                                    {
                                        var imageData = Convert.FromBase64String(args[2]);
                                        if (imageData.Length > 1024 * 1024)
                                        {
                                            _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Print, ReplicManager.Special(LO[nameof(R.AvatarTooBig)])), message.Sender);
                                            _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.Special.ToString(), LO[nameof(R.AvatarTooBig)]);
                                            return;
                                        }

                                        uri = ClientData.Share.CreateURI(file, imageData, null);
                                    }
                                    else
                                    {
                                        uri = ClientData.Share.MakeURI(file, null);
                                    }

                                    person.Picture = "URI: " + uri;
                                }
                                else
                                {
                                    person.Picture = path;
                                }

                                InformPicture(person);
                            }
                            #endregion
                            break;

                        case Messages.Choice:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.QuestionChoosing &&
                                args.Length == 3 &&
                                ClientData.Chooser != null &&
                                (message.Sender == ClientData.Chooser.Name ||
                                ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                            {
                                #region Choice

                                if (!int.TryParse(args[1], out int i) || !int.TryParse(args[2], out int j))
                                    break;

                                if (i < 0 || i >= ClientData.TInfo.RoundInfo.Count)
                                    break;

                                if (j < 0 || j >= ClientData.TInfo.RoundInfo[i].Questions.Count)
                                    break;

                                if (ClientData.TInfo.RoundInfo[i].Questions[j].IsActive())
                                {
                                    lock (ClientData.ChoiceLock)
                                    {
                                        ClientData.ThemeIndex = i;
                                        ClientData.QuestionIndex = j;
                                    }

                                    if (ClientData.IsOralNow)
                                        _gameActions.SendMessage(Messages.Cancel, message.Sender == ClientData.ShowMan.Name ?
                                            ClientData.Chooser.Name : ClientData.ShowMan.Name);

                                    _logic.Stop(StopReason.Decision);
                                }

                                #endregion
                            }
                            break;

                        case Messages.I:
                            OnI(message);
                            break;

                        case Messages.Pass:
                            OnPass(message);
                            break;

                        case Messages.Answer:
                            OnAnswer(message, args);
                            break;

                        case Messages.Atom:
                            OnAtom();
                            break;

                        case Messages.Report:
                            #region Report
                            if (ClientData.Decision == DecisionType.Reporting)
                            {
                                ClientData.ReportsCount--;
                                if (args.Length > 2)
                                {
                                    if (ClientData.GameResultInfo.Comments.Length > 0)
                                        ClientData.GameResultInfo.Comments += Environment.NewLine;

                                    ClientData.GameResultInfo.Comments += args[2];
                                    ClientData.AcceptedReports++;
                                }

                                if (ClientData.ReportsCount == 0)
                                    _logic.ExecuteImmediate();
                            }
                            break;
                        #endregion

                        case Messages.IsRight:
                            OnIsRight(message, args);
                            break;

                        case Messages.Next:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.NextPersonStakeMaking && message.Sender == ClientData.ShowMan.Name)
                            {
                                #region Next
                                
                                if (args.Length > 1 && int.TryParse(args[1], out int n) && n > -1 && n < ClientData.Players.Count)
                                {
                                    if (ClientData.Players[n].Flag)
                                    {
                                        ClientData.Order[ClientData.OrderIndex] = n;
                                        Logic.CheckOrder(ClientData.OrderIndex);
                                        _logic.Stop(StopReason.Decision);
                                    }
                                }

                                #endregion
                            }
                            break;

                        case Messages.Cat:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.CatGiving
                                && (ClientData.Chooser != null && message.Sender == ClientData.Chooser.Name ||
                                ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                            {
                                #region Cat

                                try
                                {
                                    if (int.TryParse(args[1], out int index) && index > -1 && index < ClientData.Players.Count && ClientData.Players[index].Flag)
                                    {
                                        ClientData.AnswererIndex = index;

                                        if (ClientData.IsOralNow)
                                            _gameActions.SendMessage(Messages.Cancel, message.Sender == ClientData.ShowMan.Name ?
                                                ClientData.Chooser.Name : ClientData.ShowMan.Name);

                                        _logic.Stop(StopReason.Decision);
                                    }
                                }
                                catch (Exception) { }

                                #endregion
                            }
                            break;

                        case Messages.CatCost:
                            OnCatCost(message, args);
                            break;

                        case Messages.Stake:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.AuctionStakeMaking
                                && (ClientData.ActivePlayer != null && message.Sender == ClientData.ActivePlayer.Name
                                || ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                            {
                                #region Stake

                                if (!int.TryParse(args[1], out var stakeType) || stakeType < 0 || stakeType > 3)
                                {
                                    return;
                                }

                                ClientData.StakeType = (StakeMode)stakeType;
                                if (!ClientData.StakeVariants[(int)ClientData.StakeType])
                                {
                                    ClientData.StakeType = null;
                                }
                                else if (ClientData.StakeType == StakeMode.Sum)
                                {
                                    var minimum = ClientData.Stake != -1 ? ClientData.Stake + 100 : ClientData.CurPriceRight + 100;

                                    // TODO: optimize
                                    while (minimum % 100 != 0)
                                    {
                                        minimum++;
                                    }

                                    if (!int.TryParse(args[2], out var stakeSum))
                                    {
                                        ClientData.StakeType = null;
                                        return;
                                    }

                                    if (stakeSum < minimum || stakeSum > ClientData.ActivePlayer.Sum || stakeSum % 100 != 0)
                                    {
                                        ClientData.StakeType = null;
                                        return;
                                    }

                                    ClientData.StakeSum = stakeSum;
                                }

                                if (ClientData.IsOralNow)
                                {
                                    _gameActions.SendMessage(Messages.Cancel, message.Sender == ClientData.ShowMan.Name ?
                                        ClientData.ActivePlayer.Name : ClientData.ShowMan.Name);
                                }

                                _logic.Stop(StopReason.Decision);

                                #endregion
                            }
                            break;

                        case Messages.NextDelete:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.NextPersonFinalThemeDeleting
                                && message.Sender == ClientData.ShowMan.Name)
                            {
                                #region NextDelete

                                if (int.TryParse(args[1], out int n) && n > -1 && n < ClientData.Players.Count && ClientData.Players[n].Flag)
                                {
                                    ClientData.ThemeDeleters.Current.SetIndex(n);
                                    _logic.Stop(StopReason.Decision);
                                }

                                #endregion
                            }
                            break;

                        case Messages.Delete:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.FinalThemeDeleting
                                && ClientData.ActivePlayer != null
                                && (message.Sender == ClientData.ActivePlayer.Name ||
                                ClientData.IsOralNow && message.Sender == ClientData.ShowMan.Name))
                            {
                                #region Delete

                                if (int.TryParse(args[1], out int themeIndex) && themeIndex > -1 && themeIndex < ClientData.TInfo.RoundInfo.Count)
                                {
                                    if (ClientData.TInfo.RoundInfo[themeIndex].Name != QuestionHelper.InvalidThemeName)
                                    {
                                        ClientData.ThemeIndex = themeIndex;

                                        if (ClientData.IsOralNow)
                                        {
                                            _gameActions.SendMessage(Messages.Cancel, message.Sender == ClientData.ShowMan.Name ?
                                                ClientData.ActivePlayer.Name : ClientData.ShowMan.Name);
                                        }

                                        _logic.Stop(StopReason.Decision);
                                    }
                                }

                                #endregion
                            }
                            break;

                        case Messages.FinalStake:
                            if (ClientData.IsWaiting && ClientData.Decision == DecisionType.FinalStakeMaking)
                            {
                                #region FinalStake

                                for (var i = 0; i < ClientData.Players.Count; i++)
                                {
                                    var player = ClientData.Players[i];
                                    if (player.InGame && player.FinalStake == -1 && message.Sender == player.Name)
                                    {
                                        if (int.TryParse(args[1], out int finalStake) && finalStake >= 1 && finalStake <= player.Sum)
                                        {
                                            player.FinalStake = finalStake;
                                            ClientData.NumOfStakers--;

                                            _gameActions.SendMessageWithArgs(Messages.PersonFinalStake, i);
                                        }

                                        break;
                                    }
                                }

                                if (ClientData.NumOfStakers == 0)
                                    _logic.Stop(StopReason.Decision);

                                #endregion
                            }
                            break;

                        case Messages.Apellate:
                            OnApellation(message, args);
                            break;

                        case Messages.Change:
                            OnChanged(message, args);
                            break;

                        case Messages.Move:
                            OnMove(message, args);
                            break;

                        case Messages.Kick:
                            if (message.Sender == ClientData.HostName & args.Length > 1)
                            {
                                var person = args[1];

                                if (!ClientData.AllPersons.TryGetValue(person, out var per))
                                {
                                    return;
                                }

                                if (per.Name == message.Sender)
                                {
                                    _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Print, ReplicManager.System(LO[nameof(R.CannotKickYouself)])), message.Sender);
                                    _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.System.ToString(), LO[nameof(R.CannotKickYouself)]);
                                    return;
                                }

                                if (!per.IsHuman)
                                {
                                    _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Print, ReplicManager.System(LO[nameof(R.CannotKickBots)])), message.Sender);
                                    _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.System.ToString(), LO[nameof(R.CannotKickBots)]);
                                    return;
                                }

                                MasterServer.Kick(person);
                                _gameActions.SpecialReplic(string.Format(LO[nameof(R.Kicked)], message.Sender, person));
                                OnDisconnectRequested(person);
                            }
                            break;

                        case Messages.Ban:
                            if (message.Sender == ClientData.HostName & args.Length > 1)
                            {
                                var person = args[1];

                                if (!ClientData.AllPersons.TryGetValue(person, out var per))
                                {
                                    return;
                                }

                                if (per.Name == message.Sender)
                                {
                                    _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Print, ReplicManager.System(LO[nameof(R.CannotBanYourself)])), message.Sender);
                                    _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.System.ToString(), LO[nameof(R.CannotBanYourself)]);
                                    return;
                                }

                                if (!per.IsHuman)
                                {
                                    _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Print, ReplicManager.System(LO[nameof(R.CannotBanBots)])), message.Sender);
                                    _gameActions.SendMessageWithArgs(Messages.Replic, ReplicCodes.System.ToString(), LO[nameof(R.CannotBanBots)]);
                                    return;
                                }

                                MasterServer.Kick(person, true);
                                _gameActions.SpecialReplic(string.Format(LO[nameof(R.Banned)], message.Sender, person));
                                OnDisconnectRequested(person);
                            }
                            break;

                        case Messages.Mark:
                            if (!ClientData.CanMarkQuestion)
                                break;

                            ClientData.GameResultInfo.MarkedQuestions.Add(new AnswerInfo
                            {
                                Round = _logic.Engine.RoundIndex,
                                Theme = _logic.Engine.ThemeIndex,
                                Question = _logic.Engine.QuestionIndex,
                                Answer = ""
                            });
                            break;
                    }
                }
                catch (Exception exc)
                {
                    Share_Error(new Exception(message.Text, exc));
                }
            }
        }

        private void OnApellation(Message message, string[] args)
        {
            if (!ClientData.AllowAppellation)
            {
                return;
            }

            ClientData.IsAppelationForRightAnswer = args.Length == 1 || args[1] == "+";
            ClientData.AppellationSource = message.Sender;

            ClientData.AppelaerIndex = -1;
            if (ClientData.IsAppelationForRightAnswer)
            {
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    if (ClientData.Players[i].Name == message.Sender)
                    {
                        for (var j = 0; j < ClientData.QuestionHistory.Count; j++)
                        {
                            var index = ClientData.QuestionHistory[j].PlayerIndex;
                            if (index == i)
                            {
                                if (!ClientData.QuestionHistory[j].IsRight)
                                {
                                    ClientData.AppelaerIndex = index;
                                }

                                break;
                            }
                        }

                        break;
                    }
                }
            }
            else
            {
                if (!ClientData.Players.Any(player => player.Name == message.Sender))
                {
                    // Апеллировать могут только игроки
                    return;
                }

                // Утверждение, что ответ неверен
                var count = ClientData.QuestionHistory.Count;
                if (count > 0 && ClientData.QuestionHistory[count - 1].IsRight)
                {
                    ClientData.AppelaerIndex = ClientData.QuestionHistory[count - 1].PlayerIndex;
                }
            }

            if (ClientData.AppelaerIndex != -1)
            {
                // Начата процедура апелляции
                ClientData.AllowAppellation = false;
                _logic.Stop(StopReason.Appellation);
            }
        }

        private void OnCatCost(Message message, string[] args)
        {
            if (!ClientData.IsWaiting || ClientData.Decision != DecisionType.CatCostSetting
                || (ClientData.Answerer == null || message.Sender != ClientData.Answerer.Name)
                && (!ClientData.IsOralNow || message.Sender != ClientData.ShowMan.Name))
            {
                return;
            }

            if (int.TryParse(args[1], out int sum)
                && sum >= ClientData.CatInfo.Minimum
                && sum <= ClientData.CatInfo.Maximum
                && (sum - ClientData.CatInfo.Minimum) % ClientData.CatInfo.Step == 0)
            {
                ClientData.CurPriceRight = sum;
            }

            _logic.Stop(StopReason.Decision);
        }

        private void OnChanged(Message message, string[] args)
        {
            if (message.Sender != ClientData.ShowMan.Name || args.Length != 3)
            {
                return;
            }

            if (!int.TryParse(args[1], out var playerIndex) || !int.TryParse(args[2], out var sum)
                || playerIndex < 1 || playerIndex > ClientData.Players.Count)
            {
                return;
            }

            var player = ClientData.Players[playerIndex - 1];
            player.Sum = sum;

            _gameActions.SpecialReplic($"{ClientData.ShowMan.Name} {LO[nameof(R.Change1)]} {player.Name}{LO[nameof(R.Change3)]} {Notion.FormatNumber(player.Sum)}");
            _gameActions.InformSums();

            _logic.AddHistory($"Sum change: {playerIndex - 1} = {sum}");
        }

        private void OnMove(Message message, string[] args)
        {
            if (message.Sender != ClientData.HostName && message.Sender != ClientData.ShowMan.Name || args.Length <= 1)
            {
                return;
            }

            if (!int.TryParse(args[1], out int direction))
            {
                return;
            }

            if (ClientData.TInfo.Pause && direction == 1)
            {
                OnPauseCore(false);
                return;
            }

            ClientData.MoveDirection = direction;

            switch (direction)
            {
                case -2:
                    if (!_logic.Engine.CanMoveBackRound)
                        return;
                    break;

                case -1:
                    if (!_logic.Engine.CanMoveBack)
                        return;
                    break;

                case 1:
                    if (ClientData.MoveNextBlocked)
                        return;
                    break;

                case 2:
                    if (!_logic.Engine.CanMoveNextRound)
                        return;
                    break;
            }

            _logic.Stop(StopReason.Move);
        }

        private void OnPause(Message message, string[] args)
        {
            if (message.Sender != ClientData.HostName && message.Sender != ClientData.ShowMan.Name || args.Length <= 1)
            {
                return;
            }

            OnPauseCore(args[1] == "+");
        }

        private void OnPauseCore(bool isPauseEnabled)
        {
            var times = new int[3];

            // Владелец игры взял паузу
            if (isPauseEnabled)
            {
                if (ClientData.TInfo.Pause)
                {
                    return;
                }

                ClientData.TInfo.Pause = true;
                ClientData.PauseStartTime = DateTime.UtcNow;

                Logic.AddHistory("Pause activated");

                if (ClientData.IsThinking)
                {
                    var startTime = ClientData.TimerStartTime[1];

                    ClientData.TimeThinking = ClientData.PauseStartTime.Subtract(startTime).TotalMilliseconds / 100;
                }

                if (ClientData.IsPlayingMedia)
                {
                    ClientData.IsPlayingMediaPaused = true;
                    ClientData.IsPlayingMedia = false;
                }

                for (var i = 0; i < 3; i++)
                {
                    times[i] = (int)(ClientData.PauseStartTime.Subtract(ClientData.TimerStartTime[i]).TotalMilliseconds / 100);
                }

                _logic.Stop(StopReason.Pause);
                _gameActions.SpecialReplic(LO[nameof(R.PauseInGame)]);
            }
            else
            {
                if (!ClientData.TInfo.Pause)
                {
                    return;
                }

                ClientData.TInfo.Pause = false;

                var pauseDuration = DateTime.UtcNow.Subtract(ClientData.PauseStartTime);

                for (var i = 0; i < 3; i++)
                {
                    times[i] = (int)(ClientData.PauseStartTime.Subtract(ClientData.TimerStartTime[i]).TotalMilliseconds / 100);
                    ClientData.TimerStartTime[i] = ClientData.TimerStartTime[i].Add(pauseDuration);
                }

                if (ClientData.IsPlayingMediaPaused)
                {
                    ClientData.IsPlayingMediaPaused = false;
                    ClientData.IsPlayingMedia = true;
                }

                if (_logic.StopReason == StopReason.Pause)
                {
                    // Заходим в паузу
                    _logic.AddHistory("Immediate pause resume");
                    _logic.CancelStop();
                }
                else
                {
                    _logic.AddHistory($"Pause resumed ({_logic.PrintOldTasks()} {_logic.StopReason})");

                    _logic.ResumeExecution();
                    if (_logic.StopReason == StopReason.Decision)
                    {
                        _logic.ExecuteImmediate(); // Вдруг уже готово
                    }
                }

                _gameActions.SpecialReplic(LO[nameof(R.GameResumed)]);
            }

            _gameActions.SendMessageWithArgs(Messages.Pause, isPauseEnabled ? '+' : '-', times[0], times[1], times[2]);
        }

        private void OnAtom()
        {
            if (!ClientData.IsPlayingMedia)
            {
                return;
            }

            ClientData.HaveViewedAtom--;
            if (ClientData.HaveViewedAtom == 0)
            {
                _logic.ExecuteImmediate();
            }
            else
            {
                // Иногда кто-то отваливается, и процесс затягивается на 60 секунд. Это недопустимо. Дадим 3 секунды
                _logic.ScheduleExecution(Tasks.MoveNext, 30 + ClientData.Settings.AppSettings.TimeSettings.TimeForMediaDelay * 10, force: true);
            }
        }

        private void OnAnswer(Message message, string[] args)
        {
            if (ClientData.Decision != DecisionType.Answering)
            {
                return;
            }

            if (ClientData.Round != null && ClientData.Round.Type == RoundTypes.Final)
            {
                ClientData.AnswererIndex = -1;
                for (int i = 0; i < ClientData.Players.Count; i++)
                {
                    if (ClientData.Players[i].Name == message.Sender && ClientData.Players[i].InGame)
                    {
                        ClientData.AnswererIndex = i;
                        _gameActions.SendMessageWithArgs(Messages.PersonFinalAnswer, i);
                        break;
                    }
                }

                if (ClientData.AnswererIndex == -1)
                {
                    return;
                }
            }
            else if (!ClientData.IsWaiting || ClientData.Answerer != null && ClientData.Answerer.Name != message.Sender)
            {
                return;
            }

            if (ClientData.Answerer == null)
            {
                return;
            }

            if (!ClientData.Answerer.IsHuman)
            {
                if (args[1] == MessageParams.Answer_Right)
                {
                    ClientData.Answerer.Answer = args[2].Replace(Constants.AnswerPlaceholder, ClientData.Question.GetRightAnswers().FirstOrDefault() ?? "(...)");
                    ClientData.Answerer.AnswerIsWrong = false;
                }
                else
                {
                    ClientData.Answerer.AnswerIsWrong = true;
                    var restwrong = new List<string>();
                    foreach (string wrong in ClientData.Question.Wrong)
                    {
                        if (!ClientData.UsedWrongVersions.Contains(wrong))
                            restwrong.Add(wrong);
                    }

                    var wrongAnswers = LO[nameof(R.WrongAnswer)].Split(';');
                    var wrongCount = restwrong.Count;
                    if (wrongCount == 0)
                    {
                        for (int i = 0; i < wrongAnswers.Length; i++)
                        {
                            if (!ClientData.UsedWrongVersions.Contains(wrongAnswers[i]))
                            {
                                restwrong.Add(wrongAnswers[i]);
                            }
                        }

                        if (!ClientData.UsedWrongVersions.Contains(LO[nameof(R.NoAnswer)]))
                        {
                            restwrong.Add(LO[nameof(R.NoAnswer)]);
                        }
                    }

                    wrongCount = restwrong.Count;
                    if (wrongCount == 0)
                    {
                        restwrong.Add(wrongAnswers[0]);
                        wrongCount = 1;
                    }

                    int wrongIndex = ClientData.Rand.Next(wrongCount);
                    ClientData.UsedWrongVersions.Add(restwrong[wrongIndex]);
                    ClientData.Answerer.Answer = args[2].Replace("#", restwrong[wrongIndex]);
                }

                ClientData.Answerer.Answer = ClientData.Answerer.Answer.GrowFirstLetter();
            }
            else
            {
                if (args[1].Length > 0)
                {
                    ClientData.Answerer.Answer = args[1];
                    ClientData.Answerer.AnswerIsWrong = false;
                }
                else
                {
                    ClientData.Answerer.Answer = LO[nameof(R.IDontKnow)];
                    ClientData.Answerer.AnswerIsWrong = true;
                }
            }

            if (ClientData.Round.Type != RoundTypes.Final)
            {
                _logic.Stop(StopReason.Decision);
            }
        }

        private void OnIsRight(Message message, string[] args)
        {
            if (!ClientData.IsWaiting || args.Length <= 1)
            {
                return;
            }

            if (ClientData.ShowMan != null && message.Sender == ClientData.ShowMan.Name && ClientData.Answerer != null &&
                    (ClientData.Decision == DecisionType.AnswerValidating
                    || ClientData.IsOralNow && ClientData.Decision == DecisionType.Answering))
            {
                ClientData.Decision = DecisionType.AnswerValidating;
                ClientData.Answerer.AnswerIsRight = args[1] == "+";
                ClientData.ShowmanDecision = true;

                _logic.Stop(StopReason.Decision);
                return;
            }

            if (ClientData.Decision == DecisionType.AppellationDecision)
            {
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    if (ClientData.Players[i].Flag && ClientData.Players[i].Name == message.Sender)
                    {
                        ClientData.AppellationAnswersRightReceivedCount += args[1] == "+" ? 1 : 0;
                        ClientData.Players[i].Flag = false;
                        ClientData.AppellationAnswersReceivedCount++;
                        _gameActions.SendMessageWithArgs(Messages.PersonApellated, i);
                    }
                }

                if (ClientData.AppellationAnswersReceivedCount == ClientData.Players.Count - 1)
                {
                    _logic.Stop(StopReason.Decision);
                }
            }
        }

        private void OnPass(Message message)
        {
            if (!ClientData.IsQuestionPlaying)
            {
                return;
            }

            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                var player = ClientData.Players[i];
                if (player.Name == message.Sender && player.CanPress)
                {
                    player.CanPress = false;
                    _gameActions.SendMessageWithArgs(Messages.Pass, i);
                    break;
                }
            }

            if (ClientData.IsThinking && ClientData.Players.All(p => !p.CanPress))
            {
                _logic.ScheduleExecution(Tasks.WaitTry, 3, force: true);
            }
        }

        private void OnI(Message message)
        {
            if (ClientData.Decision != DecisionType.Pressing)
            {
                for (var i = 0; i < ClientData.Players.Count; i++)
                {
                    var player = ClientData.Players[i];
                    if (player.Name == message.Sender)
                    {
                        if (ClientData.Answerer != player)
                        {
                            player.LastBadTryTime = DateTime.UtcNow;
                            _gameActions.SendMessageWithArgs(Messages.WrongTry, i);
                        }

                        break;
                    }
                }

                return;
            }

            var answererIndex = -1;
            for (var i = 0; i < ClientData.Players.Count; i++)
            {
                var player = ClientData.Players[i];
                if (player.Name == message.Sender && player.CanPress
                    && DateTime.UtcNow.Subtract(player.LastBadTryTime).TotalSeconds >= ClientData.Settings.AppSettings.TimeSettings.TimeForBlockingButton)
                {
                    answererIndex = i;
                    break;
                }
            }

            if (answererIndex == -1)
            {
                return;
            }

            if (!ClientData.Settings.AppSettings.UsePingPenalty)
            {
                ClientData.AnswererIndex = answererIndex;
                _logic.AskAnswerDeferred();
                return;
            }

            var penalty = ClientData.Players[answererIndex].PingPenalty;
            var penaltyStartTime = DateTime.UtcNow;
            if (ClientData.IsDeferringAnswer)
            {
                var futureTime = penaltyStartTime.AddMilliseconds(penalty * 100);
                var currentFutureTime = ClientData.PenaltyStartTime.AddMilliseconds(ClientData.Penalty * 100);

                if (futureTime >= currentFutureTime) // Событие произойдёт позже
                {
                    return;
                }
            }

            ClientData.AnswererIndex = answererIndex;
            ClientData.Answerer.PingPenalty = Math.Min(2, ClientData.Answerer.PingPenalty + 1);

            if (penalty == 0)
            {
                _logic.AskAnswerDeferred();
            }
            else
            {
                ClientData.IsDeferringAnswer = true;
                ClientData.PenaltyStartTime = penaltyStartTime;
                ClientData.Penalty = penalty;
                _logic.Stop(StopReason.Wait);
            }
        }

        private void OnDisconnectRequested(string person)
        {
            DisconnectRequested?.Invoke(person);
        }

        /// <summary>
        /// Изменить конфигурацию игры
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void ProcessConfig(Message message, string[] args)
        {
            if (message.Sender != ClientData.HostName || args.Length <= 1)
            {
                return;
            }

            if (!ClientData.AllPersons.TryGetValue(ClientData.HostName, out var host))
            {
                return;
            }

            switch (args[1])
            {
                case MessageParams.Config_AddTable:
                    AddTable(message, host);
                    break;

                case MessageParams.Config_DeleteTable:
                    DeleteTable(message, args, host);
                    break;

                case MessageParams.Config_Free:
                    FreeTable(message, args, host);
                    break;

                case MessageParams.Config_Set:
                    SetPerson(message, args, host);
                    break;

                case MessageParams.Config_ChangeType:
                    if (ClientData.Stage == GameStage.Before && args.Length > 2)
                    {
                        ChangePersonType(args[2], args.Length < 4 ? "" : args[3], host);
                    }
                    break;
            }
        }

        private void AddTable(Message message, Account host)
        {
            if (ClientData.Players.Count >= Constants.MaxPlayers)
            {
                return;
            }

            var newAccount = new ViewerAccount(Constants.FreePlace, false, false) { IsHuman = true };

            ClientData.BeginUpdatePersons("AddTable " + message.Text);

            try
            {
                ClientData.Players.Add(new GamePlayerAccount(newAccount));
                Logic.AddHistory($"Player added (total: {ClientData.Players.Count})");
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            foreach (var item in ClientData.MainPersons)
            {
                if (item.Ready)
                    _gameActions.SendMessage(string.Format("{0}\n{1}", Messages.Ready, item.Name), message.Sender);
            }

            var info = new StringBuilder(Messages.Config).Append(Message.ArgsSeparatorChar)
                .Append(MessageParams.Config_AddTable).Append(Message.ArgsSeparatorChar);

            AppendAccountExt(newAccount, info);

            _gameActions.SendMessage(info.ToString());
            _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Added)], host.IsMale)} {LO[nameof(R.NewGameTable)]}");
            OnPersonsChanged();
        }

        private void DeleteTable(Message message, string[] args, Account host)
        {
            if (args.Length <= 2)
            {
                return;
            }

            var indexStr = args[2];
            if (ClientData.Players.Count <= 2 || !int.TryParse(indexStr, out int index) || index <= -1
                || index >= ClientData.Players.Count)
            {
                return;
            }

            var account = ClientData.Players[index];
            var isOnline = account.IsConnected;

            if (ClientData.Stage != GameStage.Before && account.IsHuman && isOnline)
            {
                return;
            }

            ClientData.BeginUpdatePersons("DeleteTable " + message.Text);

            try
            {
                ClientData.Players.RemoveAt(index);
                Logic.AddHistory($"Player removed at {index}");

                DropPlayerIndex(index);

                if (isOnline && account.IsHuman)
                {
                    ClientData.Viewers.Add(account);
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (!account.IsHuman)
            {
                // Удалить клиента компьютерного игрока
                _client.Server.DeleteClientAsync(account.Name);
            }

            foreach (var item in ClientData.MainPersons)
            {
                if (item.Ready)
                {
                    _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
                }
            }

            _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_DeleteTable, index);
            _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Deleted)], host.IsMale)} {LO[nameof(R.GameTableNumber)]} {index + 1}");

            if (ClientData.Stage == GameStage.Before)
            {
                var readyAll = ClientData.MainPersons.All(p => p.Ready);
                if (readyAll)
                {
                    StartGame();
                }
            }

            OnPersonsChanged();
        }

        private void PlanExecution(Tasks task, double taskTime, int arg = 0)
        {
            Logic.AddHistory($"PlanExecution {task} {taskTime} {arg} ({ClientData.TInfo.Pause})");
            if (ClientData.TInfo.Pause)
            {
                Logic.UpdatePausedTask((int)task, arg, (int)taskTime);
            }
            else
            {
                Logic.ScheduleExecution(task, taskTime, arg);
            }
        }

        private void DropPlayerIndex(int playerIndex)
        {
            if (ClientData.ChooserIndex > playerIndex)
            {
                ClientData.ChooserIndex--;
            }
            else if (ClientData.ChooserIndex == playerIndex)
            {
                // Передадим право выбора игроку с наименьшей суммой
                var minSum = ClientData.Players.Min(p => p.Sum);
                ClientData.ChooserIndex = ClientData.Players.TakeWhile(p => p.Sum != minSum).Count();
            }

            if (ClientData.AnswererIndex > playerIndex)
            {
                ClientData.AnswererIndex--;
            }
            else if (ClientData.AnswererIndex == playerIndex)
            {
                // Сбросим индекс отвечающего
                ClientData.AnswererIndex = -1;

                var nextTask = (Tasks)(ClientData.TInfo.Pause ? Logic.NextTask : Logic.CurrentTask);

                Logic.AddHistory($"AnswererIndex dropped; nextTask = {nextTask};" +
                    $" ClientData.Decision = {ClientData.Decision}; Logic.IsFinalRound() = {Logic.IsFinalRound()}");

                if (ClientData.Decision == DecisionType.Answering && !Logic.IsFinalRound())
                {
                    // Отвечающего удалили. Нужно продвинуть игру дальше
                    Logic.StopWaiting();

                    if (ClientData.IsOralNow)
                    {
                        _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                    }

                    PlanExecution(Tasks.ContinueQuestion, 1);
                }
                else if (nextTask == Tasks.AskRight)
                {
                    // Игрока удалил после того, как он дал ответ. Но ещё не обратились к ведущему
                    PlanExecution(Tasks.ContinueQuestion, 1);
                }
                else if (nextTask == Tasks.CatInfo || nextTask == Tasks.AskCatCost || nextTask == Tasks.WaitCatCost)
                {
                    Logic.Engine.SkipQuestion();
                    PlanExecution(Tasks.MoveNext, 20, 1);
                }
                else if (nextTask == Tasks.AnnounceStake)
                {
                    PlanExecution(Tasks.Announce, 15);
                }
            }

            if (ClientData.AppelaerIndex > playerIndex)
            {
                ClientData.AppelaerIndex--;
            }
            else if (ClientData.AppelaerIndex == playerIndex)
            {
                ClientData.AppelaerIndex = -1;
                Logic.AddHistory($"AppelaerIndex dropped");
            }

            if (ClientData.StakerIndex > playerIndex)
            {
                ClientData.StakerIndex--;
            }
            else if (ClientData.StakerIndex == playerIndex)
            {
                var stakersCount = ClientData.Players.Count(p => p.StakeMaking);
                if (stakersCount == 1)
                {
                    for (int i = 0; i < ClientData.Players.Count; i++)
                    {
                        if (ClientData.Players[i].StakeMaking)
                        {
                            ClientData.StakerIndex = i;
                            Logic.AddHistory($"StakerIndex set to {i}");
                            break;
                        }
                    }
                }
                else
                {
                    ClientData.StakerIndex = -1;
                    Logic.AddHistory("StakerIndex dropped");
                }
            }

            var currentOrder = ClientData.Order;
            if (currentOrder != null && ClientData.Type != null && ClientData.Type.Name == QuestionTypes.Auction)
            {
                ClientData.OrderHistory.Append("Before ").Append(playerIndex).Append(' ')
                    .Append(string.Join(",", currentOrder)).AppendFormat(" {0}", ClientData.OrderIndex).AppendLine();

                var newOrder = new int[ClientData.Players.Count];

                for (int i = 0, j = 0; i < currentOrder.Length; i++)
                {
                    if (currentOrder[i] == playerIndex)
                    {
                        if (ClientData.OrderIndex >= i)
                        {
                            ClientData.OrderIndex--; // -1 - OK
                        }
                    }
                    else
                    {
                        newOrder[j++] = currentOrder[i] - (currentOrder[i] > playerIndex ? 1 : 0);
                        if (j == newOrder.Length)
                        {
                            break;
                        }
                    }
                }

                if (ClientData.OrderIndex == currentOrder.Length - 1)
                {
                    ClientData.OrderIndex = newOrder.Length - 1;
                }

                ClientData.Order = newOrder;

                ClientData.OrderHistory.Append("After ").Append(string.Join(",", newOrder)).AppendFormat(" {0}", ClientData.OrderIndex).AppendLine();

                if (!ClientData.Players.Any(p => p.StakeMaking))
                {
                    Logic.AddHistory($"Last staker dropped");
                    Logic.Engine.SkipQuestion();
                    PlanExecution(Tasks.MoveNext, 20, 1);
                }
                else if (ClientData.OrderIndex == -1 || ClientData.Order[ClientData.OrderIndex] == -1)
                {
                    Logic.AddHistory($"Current staker dropped");
                    if (ClientData.Decision == DecisionType.AuctionStakeMaking || ClientData.Decision == DecisionType.NextPersonStakeMaking)
                    {
                        // Ставящего удалили. Нужно продвинуть игру дальше
                        Logic.StopWaiting();

                        if (ClientData.IsOralNow || ClientData.Decision == DecisionType.NextPersonStakeMaking)
                        {
                            _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                        }

                        PlanExecution(Tasks.AskStake, 20);
                    }
                }
                else if (ClientData.Decision == DecisionType.NextPersonStakeMaking)
                {
                    Logic.StopWaiting();
                    _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                    PlanExecution(Tasks.AskStake, 20);
                }
            }

            if (Logic.IsFinalRound() && ClientData.ThemeDeleters != null)
            {
                ClientData.ThemeDeleters.RemoveAt(playerIndex);
                if (ClientData.ThemeDeleters.IsEmpty())
                {
                    // Удалили вообще всех, кто мог играть в финале. Завершаем раунд
                    if (Logic.Engine.CanMoveNextRound)
                    {
                        Logic.Engine.MoveNextRound();
                    }
                    else
                    {
                        PlanExecution(Tasks.Winner, 10); // Делать нечего. Завершаем игру
                    }
                }
            }

            var newHistory = new List<AnswerResult>();
            for (var i = 0; i < ClientData.QuestionHistory.Count; i++)
            {
                var answerResult = ClientData.QuestionHistory[i];
                if (answerResult.PlayerIndex == playerIndex)
                {
                    continue;
                }

                newHistory.Add(new AnswerResult
                {
                    IsRight = answerResult.IsRight,
                    PlayerIndex = answerResult.PlayerIndex - (answerResult.PlayerIndex > playerIndex ? 1 : 0)
                });
            }

            ClientData.QuestionHistory.Clear();
            ClientData.QuestionHistory.AddRange(newHistory);

            if (!ClientData.IsWaiting)
            {
                return;
            }

            switch (ClientData.Decision)
            {
                case DecisionType.StarterChoosing:
                    // Спросим заново
                    _gameActions.SendMessage(Messages.Cancel, ClientData.ShowMan.Name);
                    _logic.StopWaiting();
                    PlanExecution(Tasks.AskFirst, 20);
                    break;
            }
        }

        private void FreeTable(Message message, string[] args, Account host)
        {
            if (ClientData.Stage != GameStage.Before || args.Length <= 2)
            {
                return;
            }

            var personType = args[2];

            GamePersonAccount account;
            int index = -1;

            var isPlayer = personType == Constants.Player;

            if (isPlayer)
            {
                if (args.Length < 4)
                {
                    return;
                }

                var indexStr = args[3];

                if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
                {
                    return;
                }

                account = ClientData.Players[index];
            }
            else
            {
                account = ClientData.ShowMan;
            }

            if (!account.IsConnected || !account.IsHuman)
            {
                return;
            }

            var newAccount = new Account { IsHuman = true, Name = Constants.FreePlace };

            ClientData.BeginUpdatePersons("FreeTable " + message.Text);

            try
            {
                if (isPlayer)
                {
                    ClientData.Players[index] = new GamePlayerAccount(newAccount);
                }
                else
                {
                    ClientData.ShowMan = new GamePersonAccount(newAccount);
                }

                account.Ready = false;
                ClientData.Viewers.Add(account);
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            foreach (var item in ClientData.MainPersons)
            {
                if (item.Ready)
                {
                    _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}", message.Sender);
                }
            }

            _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_Free, args[2], args[3]);
            _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Free)], host.IsMale)} {account.Name} {LO[nameof(R.FromTable)]}");

            OnPersonsChanged();
        }

        private void SetPerson(Message message, string[] args, Account host)
        {
            if (ClientData.Stage != GameStage.Before || args.Length <= 4)
            {
                return;
            }

            var personType = args[2];
            var replacer = args[4];

            // Кого заменяем
            GamePersonAccount account;
            int index = -1;

            var isPlayer = personType == Constants.Player;

            if (isPlayer)
            {
                var indexStr = args[3];

                if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
                    return;

                account = ClientData.Players[index];
            }
            else
            {
                account = ClientData.ShowMan;
            }

            var oldName = account.Name;
            if (!account.IsHuman)
            {
                if (ClientData.AllPersons.ContainsKey(replacer))
                {
                    return;
                }

                SetComputerPerson(isPlayer, account, replacer);
            }
            else
            {
                SetHumanPerson(isPlayer, account, replacer, index);
            }

            foreach (var item in ClientData.MainPersons)
            {
                if (item.Ready)
                {
                    _gameActions.SendMessage($"{Messages.Ready}{Message.ArgsSeparatorChar}{item.Name}", message.Sender);
                }
            }

            _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_Set, args[2], args[3], args[4], account.IsMale ? '+' : '-');
            _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Replaced)], host.IsMale)} {oldName} {LO[nameof(R.To)]} {replacer}");

            InformPicture(account);
            OnPersonsChanged();
        }

        internal void SetComputerPerson(bool isPlayer, GamePersonAccount account, string replacer)
        {
            // Компьютерный персонаж меняется на другого компьютерного
            if (isPlayer)
            {
                if (DefaultPlayers == null)
                {
                    return;
                }

                var found = false;
                for (int j = 0; j < DefaultPlayers.Length; j++)
                {
                    if (DefaultPlayers[j].Name == replacer)
                    {
                        var name = account.Name;

                        account.Name = replacer;
                        account.IsMale = DefaultPlayers[j].IsMale;
                        account.Picture = DefaultPlayers[j].Picture;

                        _client.Server.ReplaceInfo(name, DefaultPlayers[j]);

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return;
                }
            }
            else
            {
                account.Name = replacer;
                account.Picture = ClientData.BackLink.GetPhotoUri(account.Name);
            }
        }

        internal void SetHumanPerson(bool isPlayer, GamePersonAccount account, string replacer, int index)
        {
            int otherIndex = -1;
            // На кого заменяем
            ViewerAccount otherAccount = null;

            ClientData.BeginUpdatePersons($"SetHumanPerson {account.Name} {account.IsConnected} {replacer} {index}");

            try
            {
                if (ClientData.ShowMan.Name == replacer && ClientData.ShowMan.IsHuman)
                {
                    otherAccount = ClientData.ShowMan;
                    ClientData.ShowMan = new GamePersonAccount(account)
                    {
                        Ready = account.Ready,
                        IsConnected = account.IsConnected
                    };
                }
                else
                {
                    for (var i = 0; i < ClientData.Players.Count; i++)
                    {
                        if (ClientData.Players[i].Name == replacer && ClientData.Players[i].IsHuman)
                        {
                            otherAccount = ClientData.Players[i];
                            ClientData.Players[i] = new GamePlayerAccount(account)
                            {
                                Ready = account.Ready,
                                IsConnected = account.IsConnected
                            };

                            otherIndex = i;
                            break;
                        }
                    }

                    if (otherIndex == -1)
                    {
                        for (var i = 0; i < ClientData.Viewers.Count; i++)
                        {
                            if (ClientData.Viewers[i].Name == replacer) // always IsHuman
                            {
                                otherAccount = ClientData.Viewers[i];
                                otherIndex = i;
                                if (account.IsConnected)
                                {
                                    ClientData.Viewers[i] = new ViewerAccount(account) { IsConnected = true };
                                }
                                else
                                {
                                    ClientData.Viewers.RemoveAt(i);
                                }
                                break;
                            }
                        }
                    }

                    if (otherIndex == -1)
                    {
                        return;
                    }
                }

                // Живой персонаж меняется на другого живого
                var otherPerson = otherAccount as GamePersonAccount;
                if (isPlayer)
                {
                    ClientData.Players[index] = new GamePlayerAccount(otherAccount) { IsConnected = otherAccount.IsConnected };

                    if (otherPerson != null)
                    {
                        ClientData.Players[index].Ready = otherPerson.Ready;
                    }
                }
                else
                {
                    ClientData.ShowMan = new GamePersonAccount(otherAccount) { IsConnected = otherAccount.IsConnected };

                    if (otherPerson != null)
                    {
                        ClientData.ShowMan.Ready = otherPerson.Ready;
                    }
                }

                InformPicture(otherAccount);
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }
        }

        internal void ChangePersonType(string personType, string indexStr, ViewerAccount responsePerson)
        {
            GamePersonAccount account;
            int index = -1;

            var isPlayer = personType == Constants.Player;

            if (isPlayer)
            {
                if (!int.TryParse(indexStr, out index) || index < 0 || index >= ClientData.Players.Count)
                {
                    return;
                }

                account = ClientData.Players[index];
            }
            else
            {
                account = ClientData.ShowMan;
            }

            if (account == null)
            {
                ClientData.BackLink.LogWarning("ChangePersonType: account == null");
                return;
            }

            var oldName = account.Name;

            var newType = !account.IsHuman;
            string newName = "";
            bool newIsMale = true;

            Account newAcc = null;

            ClientData.BeginUpdatePersons($"ChangePersonType {personType} {indexStr}");

            try
            {
                if (account.IsConnected && account.IsHuman)
                {
                    ClientData.Viewers.Add(account);
                }

                if (!account.IsHuman)
                {
                    _client.Server.DeleteClientAsync(account.Name);

                    account.IsHuman = true;
                    newName = account.Name = Constants.FreePlace;
                    account.Picture = "";
                    account.Ready = false;
                    account.IsConnected = false;
                }
                else if (isPlayer)
                {
                    if (DefaultPlayers == null)
                    {
                        return;
                    }

                    var visited = new List<int>();

                    for (var i = 0; i < ClientData.Players.Count; i++)
                    {
                        if (i != index && !ClientData.Players[i].IsHuman)
                        {
                            for (var j = 0; j < DefaultPlayers.Length; j++)
                            {
                                if (DefaultPlayers[j].Name == ClientData.Players[i].Name)
                                {
                                    visited.Add(j);
                                    break;
                                }
                            }
                        }
                    }

                    var rand = ClientData.Rand.Next(DefaultPlayers.Length - visited.Count - 1);
                    while (visited.Contains(rand))
                    {
                        rand++;
                    }

                    var compPlayer = DefaultPlayers[rand];
                    var newAccount = new GamePlayerAccount(account);
                    newAcc = newAccount;

                    newAccount.IsHuman = false;
                    newName = newAccount.Name = compPlayer.Name;
                    newIsMale = newAccount.IsMale = compPlayer.IsMale;
                    newAccount.Picture = compPlayer.Picture;
                    newAccount.IsConnected = true;

                    ClientData.Players[index] = newAccount;

                    var playerClient = new Client(newAccount.Name);
                    var player = new Player(playerClient, compPlayer, false, LO, new ViewerData { BackLink = ClientData.BackLink });
                    playerClient.ConnectToAsync(_client.Server).ContinueWith(t =>
                    {
                        lock (ClientData.TaskLock)
                        {
                            Inform(newAccount.Name);
                        }
                    });
                }
                else
                {
                    if (ClientData.BackLink == null)
                    {
                        throw new InvalidOperationException("ChangePersonType: this.ClientData.BackLink == null");
                    }

                    var newAccount = new GamePersonAccount(account);
                    newAcc = newAccount;

                    newAccount.IsHuman = false;
                    newName = newAccount.Name = DefaultShowmans[0].Name;
                    newIsMale = newAccount.IsMale = true;
                    newAccount.Picture = DefaultShowmans[0].Picture;
                    newAccount.IsConnected = true;

                    ClientData.ShowMan = newAccount;

                    var showmanClient = new Client(newAccount.Name);
                    var showman = new Showman(showmanClient, newAccount, false, LO, new ViewerData { BackLink = ClientData.BackLink });
                    showmanClient.ConnectToAsync(_client.Server).ContinueWith(t =>
                    {
                        lock (ClientData.TaskLock)
                        {
                            Inform(newAccount.Name);
                        }
                    });
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            foreach (var item in ClientData.MainPersons)
            {
                if (item.Ready)
                {
                    _gameActions.SendMessage($"{Messages.Ready}\n{item.Name}");
                }
            }

            _gameActions.SendMessageWithArgs(Messages.Config, MessageParams.Config_ChangeType, personType, index, newType ? '+' : '-', newName, newIsMale ? '+' : '-');

            if (responsePerson != null)
            {
                var newTypeString = newType ? LO[nameof(R.Human)] : LO[nameof(R.Computer)];
                _gameActions.SpecialReplic($"{ClientData.HostName} {ResourceHelper.GetSexString(LO[nameof(R.Sex_Changed)], responsePerson.IsMale)} {LO[nameof(R.PersonType)]} {oldName} {LO[nameof(R.To)]} \"{newTypeString}\"");
            }

            if (newAcc != null)
            {
                InformPicture(newAcc);
            }

            OnPersonsChanged();
        }

        internal void StartGame()
        {
            ClientData.Stage = GameStage.Begin;
            OnStageChanged(GameStages.Started, LO[nameof(R.GameBeginning)]);
            _gameActions.InformStage();
            ClientData.IsOral = ClientData.Settings.AppSettings.Oral && ClientData.ShowMan.IsHuman;
            _logic.ScheduleExecution(Tasks.StartGame, 1, 1);
        }

        private bool? CheckAccount(Message message, string role, string name, string sex, ref bool found, int index, ViewerAccount account)
        {
            if (account.IsConnected)
            {
                return null;
            }

            if (account.Name == name || account.Name == Constants.FreePlace)
            {
                found = true;

                IConnection extServ = null;
                var append = role == "viewer" && account.Name == Constants.FreePlace;
                account.Name = name;
                account.IsMale = sex == "m";
                account.Picture = "";
                account.IsConnected = true;

                lock (_client.Server.ConnectionsSync)
                {
                    extServ = MasterServer.ExternalServers.Where(serv => serv.Id == message.Sender.Substring(1)).FirstOrDefault();
                    if (extServ == null)
                    {
                        return false;
                    }

                    lock (extServ.ClientsSync)
                    {
                        extServ.Clients.Add(name);
                    }

                    extServ.IsAuthenticated = true;
                    extServ.UserName = name;
                }

                if (append)
                {
                    ClientData.Viewers.Add(new ViewerAccount(account) { IsConnected = account.IsConnected });
                }

                ClientData.OnAllPersonsChanged();

                _gameActions.SpecialReplic($"{LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)]} {name}");

                _gameActions.SendMessage(Messages.Accepted, name);
                _gameActions.SendMessageWithArgs(Messages.Connected, role, index, name, sex, "");

                OnPersonsChanged();
            }

            return true;
        }

        private bool? CheckAccountNew(string role, string name, string sex, ref bool found, int index, ViewerAccount account,
            Action connectionAuthenticator)
        {
            if (account.IsConnected)
            {
                return account.Name == name ? false : (bool?)null;
            }

            found = true;

            var append = role == "viewer" && account.Name == Constants.FreePlace;

            account.Name = name;
            account.IsMale = sex == "m";
            account.Picture = "";
            account.IsConnected = true;

            if (append)
            {
                ClientData.Viewers.Add(new ViewerAccount(account) { IsConnected = account.IsConnected });
            }

            ClientData.OnAllPersonsChanged();

            _gameActions.SpecialReplic($"{LO[account.IsMale ? nameof(R.Connected_Male) : nameof(R.Connected_Female)]} {name}");

            _gameActions.SendMessageWithArgs(Messages.Connected, role, index, name, sex, "");

            connectionAuthenticator();

            OnPersonsChanged();

            return true;
        }

        private void OnPersonsChanged(bool joined = true, bool withError = false) => PersonsChanged?.Invoke(joined, withError);

        private void InformPicture(Account account)
        {
            foreach (var personName in ClientData.AllPersons.Keys)
            {
                if (account.Name != personName && personName != NetworkConstants.GameName)
                {
                    InformPicture(account, personName);
                }
            }
        }

        private void InformPicture(Account account, string receiver)
        {
            if (string.IsNullOrEmpty(account.Picture))
            {
                return;
            }

            var link = CreateUri(account.Name, account.Picture, receiver);
            if (link != null)
            {
                _gameActions.SendMessage(string.Join(Message.ArgsSeparator, Messages.Picture, account.Name, link), receiver);
            }
        }

        private string CreateUri(string id, string file, string receiver)
        {
            var local = _client.Server.Contains(receiver);

            if (!Uri.TryCreate(file, UriKind.RelativeOrAbsolute, out Uri uri))
                return null;

            if (!uri.IsAbsoluteUri || uri.Scheme == "file" && !CoreManager.Instance.FileExists(file))
                return null;

            var remote = !local && uri.Scheme == "file";
            var isURI = file.StartsWith("URI: ");

            if (isURI || remote)
            {
                string path = null;
                if (isURI)
                {
                    path = file.Substring(5);
                }
                else
                {
                    var complexName = (id != null ? id + "_" : "") + Path.GetFileName(file);
                    if (!ClientData.Share.ContainsURI(complexName))
                    {
                        path = ClientData.Share.CreateURI(complexName, () =>
                        {
                            var stream = CoreManager.Instance.GetFile(file);
                            return new StreamInfo { Stream = stream, Length = stream.Length };
                        }, null);
                    }
                    else
                    {
                        path = ClientData.Share.MakeURI(complexName, null);
                    }
                }

                return local ? path : path.Replace("http://localhost", "http://" + Constants.GameHost);
            }
            else
            {
                return file;
            }
        }
    }
}
