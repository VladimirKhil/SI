using SICore.BusinessLogic;
using SICore.Connections;
using SICore.Network;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SIData;
using SIPackages.Core;
using SIUI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Клиент зрителя
    /// </summary>
    public abstract class Viewer<L> : Actor<ViewerData, L>, IViewerClient
        where L : class, IViewer
    {
        /// <summary>
        /// Является ли владельцем сервера
        /// </summary>
        public bool IsHost { get; private set; }

        public IConnector Connector { get; set; }

        public IViewer MyLogic { get { return _logic; } }

        public event Action<IViewerClient> Switch;
        public event Action StageChanged;
        public event Action<string> Ad;

        public ViewerData MyData => ClientData;

        public string Avatar { get; set; }
        public string ServerPublicPackageUrl { get; set; }
        public string[] ContentPublicBaseUrls { get; set; }

        public event Action PersonConnected;
        public event Action PersonDisconnected;
        public event Action<int, string, string> Timer;

        private void Initialize(bool isHost)
        {
            IsHost = isHost;
            ClientData.MessageSending = msg => Say(msg);

            ClientData.Kick = new CustomCommand(Kick_Executed) { CanBeExecuted = IsHost };
            ClientData.Ban = new CustomCommand(Ban_Executed) { CanBeExecuted = IsHost };

            ClientData.ForceStart = new CustomCommand(ForceStart_Executed) { CanBeExecuted = IsHost };
            ClientData.AddTable = new CustomCommand(AddTable_Executed) { CanBeExecuted = IsHost };
            ClientData.DeleteTable = new CustomCommand(DeleteTable_Executed) { CanBeExecuted = IsHost };

            ClientData.AtomViewed = new CustomCommand(arg => SendMessage(Messages.Atom));
        }

        private void ChangeType_Executed(object arg)
        {
            var account = (PersonAccount)arg;
            var player = account as PlayerAccount;

            SendMessage(Messages.Config, MessageParams.Config_ChangeType, player != null ? "player" : "showman", player != null ? ClientData.Players.IndexOf(player).ToString() : "");
        }

        private void Replace_Executed(PersonAccount person, object arg)
        {
            var account = (Account)arg;
            var player = person as PlayerAccount;

            string index;
            if (player != null)
            {
                var playerIndex = ClientData.Players.IndexOf(player);
                if (playerIndex == -1)
                {
                    return;
                }

                index = playerIndex.ToString();
            }
            else
            {
                index = "";
            }

            SendMessage(Messages.Config, MessageParams.Config_Set, player != null ? "player" : "showman", index, account.Name);
        }

        private void Free_Executed(object arg)
        {
            var account = (PersonAccount)arg;
            var player = account as PlayerAccount;

            var indexString = "";

            if (player != null)
            {
                var index = ClientData.Players.IndexOf(player);
                if (index < 0 || index >= ClientData.Players.Count)
                {
                    AddLog($"Wrong index: {index}" + Environment.NewLine);
                    return;
                }

                indexString = index.ToString();
            }

            SendMessage(Messages.Config, MessageParams.Config_Free, player != null ? "player" : "showman", indexString);
        }

        private void Delete_Executed(object arg)
        {
            var player = (PlayerAccount)arg;

            SendMessage(Messages.Config, MessageParams.Config_DeleteTable, ClientData.Players.IndexOf(player).ToString());
        }

        /// <summary>
        /// Упрощённый клиент (используется в качестве предка)
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="password">Пароль</param>
        /// <param name="isHost">Является ли владельцем сервера</param>
        protected Viewer(Client client, Account personData, bool isHost, IGameManager manager, ILocalizer localizer, ViewerData data = null)
            : base(client, personData, manager, localizer, data)
        {
            if (personData == null)
            {
                throw new ArgumentNullException(nameof(personData));
            }

            Initialize(isHost);

            ClientData.Picture = personData.Picture;
        }

        public void Move(object arg)
        {
            SendMessageWithArgs(Messages.Move, arg);
        }

        private void Kick_Executed(object arg)
        {
            if (!(arg is ViewerAccount person))
                return;

            if (person == ClientData.Me)
            {
                AddLog(LO[nameof(R.CannotKickYouself)] + Environment.NewLine);
                return;
            }

            if (!person.IsHuman)
            {
                AddLog(LO[nameof(R.CannotKickBots)] + Environment.NewLine);
                return;
            }

            SendMessage(Messages.Kick, person.Name);
        }

        private void Ban_Executed(object arg)
        {
            if (!(arg is ViewerAccount person))
                return;

            if (person == ClientData.Me)
            {
                AddLog(LO[nameof(R.CannotBanYourself)] + Environment.NewLine);
                return;
            }

            if (!person.IsHuman)
            {
                AddLog(LO[nameof(R.CannotBanBots)] + Environment.NewLine);
                return;
            }

            SendMessage(Messages.Ban, person.Name);
        }

        private void ForceStart_Executed(object arg)
        {
            SendMessage(Messages.Start);
        }

        private void AddTable_Executed(object arg)
        {
            SendMessage(Messages.Config, MessageParams.Config_AddTable);
        }

        private void DeleteTable_Executed(object arg)
        {
            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                var player = ClientData.Players[i];
                player.CanBeSelected = ClientData.Stage == GameStage.Before || !player.Connected || !player.IsHuman;
                int num = i;
                player.SelectionCallback = p =>
                {
                    SendMessageWithArgs(Messages.Config, MessageParams.Config_DeleteTable, num);
                    Clear();
                };
            }

            ClientData.Hint = LO[nameof(R.DeleteTableHint)];
        }

        private void Clear()
        {
            ClientData.Hint = "";
            ClientData.DialogMode = DialogModes.None;

            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                ClientData.Players[i].CanBeSelected = false;
            }

            ClientData.BackLink.OnFlash(false);
        }

        /// <summary>
        /// Обработка полученного системного сообщения
        /// </summary>
        /// <param name="mparams">Параметры сообщения</param>
        protected virtual void OnSystemMessageReceived(string[] mparams)
        {
            try
            {
                switch (mparams[0])
                {
                    case Messages.Connected:
                        #region Connected
                        {
                            if (mparams[3] == _client.Name)
                            {
                                return;
                            }

                            if (!_client.Server.IsMain)
                            {
                                lock (_client.Server.ConnectionsSync)
                                {
                                    var externalServer = ((ISlaveServer)_client.Server).HostServer;
                                    if (externalServer != null)
                                    {
                                        lock (externalServer.ClientsSync)
                                        {
                                            externalServer.Clients.Add(mparams[3]);
                                        }
                                    }
                                }
                            }

                            var account = new Account(mparams[3], mparams[4] == "m");
                            int.TryParse(mparams[2], out int index);
                            InsertPerson(mparams[1], account, index);

                            PersonConnected?.Invoke();

                            UpdateDeleteTableCommand();

                            break;
                        }
                        #endregion
                    case SystemMessages.Disconnect:
                        #region Disconnect
                        {
                            _logic.Print(ReplicManager.Special(LO[nameof(R.DisconnectMessage)]));
                            if (Connector != null && !Connector.IsReconnecting)
                                _logic.TryConnect(Connector);

                            break;
                        }
                        #endregion
                    case Messages.Disconnected:
                        #region Disconnected
                        {
                            var name = mparams[1];

                            var person = ClientData.AllPersons.FirstOrDefault(account => account.Name == name);
                            if (person != null)
                            {
                                person.Connected = false;
                                person.Name = Constants.FreePlace;
                                person.Picture = "";
                            }

                            var personAccount = person as PersonAccount;
                            if (ClientData.Stage == GameStage.Before && personAccount != null)
                                personAccount.Ready = false;

                            if (personAccount == null)
                            {
                                ClientData.Viewers.Remove(person);
                            }
                            else if (personAccount is PlayerAccount player)
                            {
                                UpdatePlayerCommands(player);
                            }
                            else
                            {
                                UpdateShowmanCommands();
                            }

                            ClientData.OnAllPersonsChanged();

                            if (!_client.Server.IsMain)
                            {
                                lock (_client.Server.ConnectionsSync)
                                {
                                    var externalServer = ((ISlaveServer)_client.Server).HostServer;
                                    if (externalServer != null)
                                    {
                                        lock (externalServer.ClientsSync)
                                        {
                                            if (externalServer.Clients.Contains(name))
                                                externalServer.Clients.Remove(name);
                                        }
                                    }
                                }
                            }

                            PersonDisconnected?.Invoke();

                            UpdateDeleteTableCommand();

                            break;
                        }
                        #endregion

                    case Messages.Info2:
                        ProcessInfo(mparams);
                        break;

                    case Messages.Config:
                        if (ClientData.Me == null)
                            break;

                        ProcessConfig(mparams);
                        break;

                    case Messages.ReadingSpeed:
                        {
                            #region ReadingSpeed
                            if (mparams.Length > 1)
                            {
                                 if (int.TryParse(mparams[1], out int readingSpeed) && readingSpeed > 0)
                                    _logic.OnTextSpeed(1.0 / readingSpeed);
                            }
                            break;
                            #endregion
                        }
                    case Messages.ButtonBlockingTime:
                        if (mparams.Length > 1)
                        {
                            if (int.TryParse(mparams[1], out var buttonBlockingTime) && buttonBlockingTime > 0)
                            {
                                ClientData.ButtonBlockingTime = buttonBlockingTime;
                            }
                        }
                        break;
                    case Messages.PackageId:
                        {
                            if (mparams.Length > 1)
                            {
                                ClientData.PackageId = mparams[1];
                            }
                            break;
                        }
                    case Messages.PackageLogo:
                        {
                            _logic.OnPackageLogo(mparams[1]);
                            break;
                        }
                    case Messages.FalseStart:
                        {
                            if (mparams.Length > 1)
                            {
                                ClientData.FalseStart = mparams[1] != "-";
                            }
                            break;
                        }
                    case Messages.Studia:
                        {
                            #region Studia
                            var studia = mparams[1];

                            if (Connector != null && studia.Contains(Constants.GameHost))
                            {
                                var address = Connector.ServerAddress;
                                if (!string.IsNullOrWhiteSpace(address))
                                {
                                    if (Uri.TryCreate(address, UriKind.Absolute, out Uri hostUri))
                                        studia = studia.Replace(Constants.GameHost, hostUri.Host);
                                }
                            }

                            ClientData.Studia = studia;
                            #endregion
                            break;
                        }
                    case Messages.Print:
                        {
                            #region Print

                            var s = new StringBuilder();

                            for (int i = 0; i < mparams.Length - 1; i++)
                            {
                                if (s.Length > 0)
                                    s.Append(Message.ArgsSeparatorChar);
                                s.Append(mparams[i + 1]);
                            }

                            _logic.Print(s.ToString());
                            Ad?.Invoke(null);
                            break;

                            #endregion
                        }
                    case Messages.Pause:
                        {
                            #region Pause

                            ClientData.TInfo.Pause = mparams[1] == "+";
                            _logic.OnPauseChanged();

                            if (mparams.Length > 4)
                            {
                                var message = ClientData.TInfo.Pause ? "USER_PAUSE" : "USER_RESUME";

                                _logic.OnTimerChanged(0, message, mparams[2], null);
                                _logic.OnTimerChanged(1, message, mparams[3], null);
                                _logic.OnTimerChanged(2, message, mparams[4], null);
                                
                                Timer?.Invoke(0, message, mparams[2]);
                                Timer?.Invoke(1, message, mparams[3]);
                                Timer?.Invoke(2, message, mparams[4]);
                            }

                            break;

                            #endregion
                        }
                    case Messages.Sums:
                        {
                            #region Sums

                            var players = ClientData.Players;
                            var max = Math.Min(players.Count, mparams.Length - 1);

                            for (int i = 0; i < max; i++)
                            {
                                if (int.TryParse(mparams[i + 1], out int sum))
                                    players[i].Sum = sum;
                            }

                            break;

                            #endregion
                        }
                    case Messages.Ready:
                        {
                            #region Ready

                            if (ClientData == null || ClientData.Players.Count == 0)
                                return;

                            var ready = mparams.Length == 2 || mparams[2] == "+";

                            if (mparams[1] == _client.Name)
                                ClientData.IReady = ready;

                            if (ClientData.ShowMan == null)
                                return;

                            var person = ClientData.MainPersons.FirstOrDefault(item => item.Name == mparams[1]);
                            if (person != null)
                                person.Ready = ready;

                            #endregion
                            break;
                        }
                    case Messages.Stage:
                        {
                            #region Stage

                            ClientData.Stage = (GameStage)Enum.Parse(typeof(GameStage), mparams[1]);

                            if (ClientData.Stage != GameStage.Before)
                            {
                                for (int i = 0; i < ClientData.Players.Count; i++)
                                {
                                    ClientData.Players[i].GameStarted = true;
                                }

                                if (ClientData.ShowMan != null)
                                    ClientData.ShowMan.GameStarted = true;

                                ClientData.ForceStart.CanBeExecuted = false;
                            }

                            switch (ClientData.Stage)
                            {
                                case GameStage.Round:
                                    _logic.SetText(mparams[2]);

                                    for (int i = 0; i < ClientData.Players.Count; i++)
                                    {
                                        ClientData.Players[i].InGame = true;
                                        ClientData.Players[i].IsChooser = false;

                                    }

                                    break;

                                case GameStage.Final:
                                    _logic.SetText(mparams[2]);
                                    ClientData._atomType = AtomTypes.Text;
                                    break;

                                case GameStage.After:
                                    ClientData.BackLink.OnGameFinished(ClientData.PackageId);
                                    break;
                            }

                            _logic.Stage();
                            StageChanged?.Invoke();

                            #endregion
                            break;
                        }
                    case Messages.Timer:
                        {
                            var timerIndex = int.Parse(mparams[1]);
                            var timerCommand = mparams[2];

                            _logic.OnTimerChanged(timerIndex, timerCommand, mparams.Length > 3 ? mparams[3] : null, mparams.Length > 4 ? mparams[4] : null);
                            Timer?.Invoke(timerIndex, timerCommand, mparams.Length > 3 ? mparams[3] : null);

                            break;
                        }
                    case Messages.GameThemes:
                        {
                            #region GameThemes

                            for (var i = 1; i < mparams.Length; i++)
                            {
                                ClientData.TInfo.GameThemes.Add(mparams[i]);
                                _logic.Print(ReplicManager.System(mparams[i]));
                            }

                            _logic.GameThemes();

                            #endregion
                            break;
                        }
                    case Messages.RoundThemes:
                        {
                            #region RoundThemes

                            var print = mparams[1] == "+";

                            lock (ClientData.TInfoLock)
                            {
                                ClientData.TInfo.RoundInfo.Clear();
                                for (int i = 2; i < mparams.Length; i++)
                                {
                                    ClientData.TInfo.RoundInfo.Add(new ThemeInfo { Name = mparams[i] });
                                    if (print)
                                        _logic.Print(ReplicManager.System(mparams[i]));
                                }
                            }

                            try
                            {
                                _logic.RoundThemes(print);
                            }
                            catch (InvalidProgramException exc)
                            {
                                ClientData.BackLink.SendError(exc, true);
                            }

                            #endregion
                            break;
                        }
                    case Messages.Theme:
                    case Messages.Question:
                        _logic.SetText(mparams[1]);

                        foreach (var item in ClientData.Players)
                        {
                            item.State = PlayerState.None;
                        }

                        break;
                    case Messages.Table:
                        {
                            #region Tablo2

                            lock (ClientData.TInfoLock)
                            {
                                if (ClientData.TInfo.RoundInfo.Any(t => t.Questions.Any()))
                                    break;

                                var index = 1;
                                for (int i = 0; i < ClientData.TInfo.RoundInfo.Count; i++)
                                {
                                    if (index == mparams.Length)
                                        break;

                                    while (index < mparams.Length && mparams[index].Length > 0) // пустой параметр разделяет темы
                                    {
                                        if (!int.TryParse(mparams[index++], out int price))
                                            price = -1;

                                        ClientData.TInfo.RoundInfo[i].Questions.Add(new QuestionInfo { Price = price });
                                    }

                                    index++;
                                }
                            }

                            _logic.TableLoaded();

                            #endregion
                            break;
                        }
                    case Messages.ShowTable:
                        {
                            #region ShowTablo

                            _logic.ShowTablo();

                            #endregion
                            break;
                        }
                    case Messages.Choice:
                        {
                            #region Choice

                            lock (ClientData.ChoiceLock)
                            lock (ClientData.TInfoLock)
                            {
                                ClientData.ThemeIndex = int.Parse(mparams[1]);
                                ClientData.QuestionIndex = int.Parse(mparams[2]);

                                if (ClientData.ThemeIndex > -1 && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count
                                    && ClientData.QuestionIndex > -1 && ClientData.QuestionIndex < ClientData.TInfo.RoundInfo[ClientData.ThemeIndex].Questions.Count)
                                    ClientData.CurPriceRight = ClientData.CurPriceWrong = ClientData.TInfo.RoundInfo[ClientData.ThemeIndex].Questions[ClientData.QuestionIndex].Price;
                            }

                            foreach (var player in ClientData.Players.ToArray())
                            {
                                player.Pass = false;
                                player.Stake = 0;
                                player.SafeStake = false;
                            }

                            _logic.Choice();

                            #endregion
                            break;
                        }
                    case Messages.QType:
                        {
                            #region QType

                            ClientData._atomType = AtomTypes.Text;
                            ClientData.AtomIndex = -1;
                            ClientData.IsPartial = false;
                            ClientData._qtype = mparams[1];

                            _logic.QType();

                            #endregion
                            break;
                        }

                    case Messages.TextShape:
                        _logic.TextShape(mparams);
                        break;

                    case Messages.Atom:
                        _logic.SetAtom(mparams);
                        break;

                    case Messages.Atom_Second:
                        _logic.SetSecondAtom(mparams);
                        break;

                    case Messages.RightAnswer:
                        _logic.SetRight(mparams[2]);
                        break;

                    case Messages.Resume:
                        _logic.Resume();
                        break;

                    case Messages.Try:
                        {
                            #region Try

                            if (mparams.Length > 1 && mparams[1] == "NF")
                            {
                                // Здесь можно не показывать рамку
                                if (!ClientData.BackLink.ShowBorderOnFalseStart)
                                {
                                    return;
                                }
                            }

                            _logic.Try();

                            #endregion
                            break;
                        }
                    case Messages.EndTry:
                        {
                            #region EndTry

                            if (mparams[1] == "A")
                            {
                                _logic.OnTimerChanged(1, "STOP", "", null);
                                Timer?.Invoke(1, "STOP", "");
                            }

                            _logic.EndTry(mparams[1]);

                            #endregion
                            break;
                        }
                    case Messages.WrongTry:
                        {
                            #region WrongTry
                            var p = int.Parse(mparams[1]);
                            if (p > 0 && p < ClientData.Players.Count)
                            {
                                var player = ClientData.Players[p];
                                if (player.State == PlayerState.None)
                                {
                                    player.State = PlayerState.Lost;
                                    Task.Run(async () =>
                                    {
                                        await Task.Delay(200);
                                        if (player.State == PlayerState.Lost)
                                            player.State = PlayerState.None;
                                    });
                                }
                            }
                            #endregion
                            break;
                        }
                    case Messages.Person:
                        {
                            #region Person

                            if (mparams.Length < 4)
                            {
                                break;
                            }

                            var isRight = mparams[1] == "+";
                            if (!int.TryParse(mparams[2], out var playerIndex)
                                || playerIndex < 0 || playerIndex >= ClientData.Players.Count)
                            {
                                break;
                            }

                            if (!int.TryParse(mparams[3], out var price))
                            {
                                break;
                            }

                            ClientData.CurPriceWrong = ClientData.CurPriceRight = price;

                            _logic.Person(playerIndex, isRight);

                            #endregion
                            break;
                        }
                    case Messages.Pass:
                        {
                            _logic.OnPersonPass(int.Parse(mparams[1]));
                            break;
                        }
                    case Messages.PersonFinalAnswer:
                        {
                            if (mparams.Length > 1 && int.TryParse(mparams[1], out int playerIndex))
                            {
                                _logic.OnPersonFinalAnswer(playerIndex);
                            }
                            break;
                        }
                    case Messages.PersonApellated:
                        {
                            if (int.TryParse(mparams[1], out int playerIndex))
                            {
                                _logic.OnPersonApellated(playerIndex);
                            }
                            break;
                        }
                    case Messages.PersonFinalStake:
                        {
                            if (int.TryParse(mparams[1], out int playerIndex))
                            {
                                _logic.OnPersonFinalStake(playerIndex);
                            }
                            break;
                        }
                    case Messages.PersonStake:
                        {
                            #region PersonStake

                            ClientData._lastStakerNum = int.Parse(mparams[1]);
                            if (ClientData._lastStakerNum < 0 || ClientData._lastStakerNum >= ClientData.Players.Count)
                                return;

                            int stake;
                            if (mparams[2] == "0")
                                stake = -1;
                            else if (mparams[2] == "2")
                                stake = -2;
                            else if (mparams[2] == "3")
                                stake = -3;
                            else
                            {
                                stake = int.Parse(mparams[3]);
                                if (mparams.Length > 4)
                                {
                                    ClientData.Players[ClientData._lastStakerNum].SafeStake = true;
                                }
                            }

                            ClientData.Players[ClientData._lastStakerNum].Stake = stake;

                            #endregion
                            break;
                        }
                    case Messages.Stop:
                        {
                            #region Stop

                            _logic.StopRound();

                            _logic.OnTimerChanged(0, "STOP", "", null);
                            _logic.OnTimerChanged(1, "STOP", "", null);
                            _logic.OnTimerChanged(2, "STOP", "", null);

                            Timer?.Invoke(0, "STOP", "");
                            Timer?.Invoke(1, "STOP", "");
                            Timer?.Invoke(2, "STOP", "");

                            #endregion
                            break;
                        }
                    case Messages.FinalRound:
                        {
                            #region FinalRound

                            for (var i = 0; i < ClientData.Players.Count; i++)
                            {
                                ClientData.Players[i].InGame = i + 1 < mparams.Length && mparams[i + 1] == "+";
                            }

                            ClientData.AtomIndex = -1;
                            ClientData.IsPartial = false;

                            #endregion
                            break;
                        }
                    case Messages.Out:
                        {
                            #region Out

                            lock (ClientData.ChoiceLock)
                            lock (ClientData.TInfoLock)
                            {
                                ClientData.ThemeIndex = int.Parse(mparams[1]);
                                if (ClientData.ThemeIndex > -1 && ClientData.ThemeIndex < ClientData.TInfo.RoundInfo.Count)
                                    _logic.Out(ClientData.ThemeIndex);
                            }

                            #endregion
                            break;
                        }
                    case Messages.Winner:
                        {
                            #region Winner

                            ClientData.Winner = int.Parse(mparams[1]);
                            _logic.Winner();

                            #endregion
                            break;
                        }
                    case Messages.Timeout:
                        {
                            #region Timeout

                            _logic.TimeOut();

                            #endregion
                            break;
                        }
                    case Messages.FinalThink:
                        _logic.FinalThink();
                        break;
                    case Messages.Picture:
                        {
                            #region Picture

                            if (ClientData.Players.Count == 0)
                                return;

                            var per = ClientData.MainPersons.FirstOrDefault(person => person.Name == mparams[1]);
                            if (per != null)
                                _logic.UpdatePicture(per, mparams[2]);

                            #endregion
                            break;
                        }
                    case Messages.SetChooser:
                        {
                            var index = int.Parse(mparams[1]);

                            for (int i = 0; i < ClientData.Players.Count; i++)
                            {
                                ClientData.Players[i].IsChooser = i == index;

                                if (i == index && mparams.Length > 2)
                                {
                                    ClientData.Players[i].State = PlayerState.Press;
                                }
                            }

                            break;
                        }
                    case Messages.Ads:
                        if (mparams.Length > 1)
                        {
                            Ad?.Invoke(mparams[1]);
                        }
                        break;
                }
            }
            catch (Exception exc)
            {
                throw new Exception(string.Join("\n", mparams), exc);
            }
        }

        private void ProcessConfig(string[] mparams)
        {
            switch (mparams[1])
            {
                case MessageParams.Config_AddTable:
                    ClientData.BeginUpdatePersons();
                    try
                    {
                        var account = new PlayerAccount(mparams[2], mparams[3] == "+", mparams[4] == "+", ClientData.Stage != GameStage.Before)
                        {
                            IsHuman = mparams[5] == "+",
                            Ready = mparams[6] == "+",
                            IsExtendedMode = IsHost
                        };

                        CreatePlayerCommands(account);

                        var clone = new List<PlayerAccount>(ClientData.Players)
                        {
                            account
                        };

                        ClientData.Players = clone;

                        UpdateAddTableCommand();
                        UpdateDeleteTableCommand();

                        if (IsHost)
                        {
                            UpdatePlayerCommands(account);

                            var canDelete = ClientData.Players.Count > 2;
                            foreach (var player in ClientData.Players)
                            {
                                player.Delete.CanBeExecuted = canDelete;
                            }
                        }
                    }
                    finally
                    {
                        ClientData.EndUpdatePersons();
                    }
                    break;

                case MessageParams.Config_Free:
                    {
                        if (mparams.Length < 4)
                            break;

                        var personType = mparams[2];
                        var indexString = mparams[3];

                        PersonAccount account;

                        var isPlayer = personType == "player";
                        if (isPlayer)
                        {
                            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
                                break;

                            account = ClientData.Players[index];
                        }
                        else
                        {
                            account = ClientData.ShowMan;
                        }

                        var clone = new List<ViewerAccount>(ClientData.Viewers);
                        var newAccount = new ViewerAccount(account) { Connected = true };

                        clone.Add(newAccount);

                        ClientData.BeginUpdatePersons();
                        try
                        {
                            ClientData.Viewers = clone;

                            account.Name = Constants.FreePlace;
                            account.Connected = false;
                            account.Ready = false;
                            account.Picture = "";
                        }
                        finally
                        {
                            ClientData.EndUpdatePersons();
                        }

                        if (account == ClientData.Me)
                        {
                            // Необходимо самого себя перевести в зрители
                            SwitchToNewType(GameRole.Viewer, newAccount);
                        }

                        UpdateDeleteTableCommand();
                    }

                    break;

                case MessageParams.Config_DeleteTable:
                    {
                        if (mparams.Length < 3)
                            break;

                        var indexString = mparams[2];

                        if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
                            break;

                        PlayerAccount account = null;
                        ViewerAccount newAccount = null;

                        ClientData.BeginUpdatePersons();
                        try
                        {
                            account = ClientData.Players[index];

                            var clone = new List<PlayerAccount>(ClientData.Players);
                            clone.RemoveAt(index);

                            ClientData.Players = clone;

                            UpdateAddTableCommand();

                            if (account.Connected && account.IsHuman)
                            {
                                newAccount = new ViewerAccount(account) { Connected = true };

                                var cloneV = new List<ViewerAccount>(ClientData.Viewers)
                                {
                                    newAccount
                                };

                                ClientData.Viewers = cloneV;
                            }

                            if (IsHost)
                            {
                                var canDelete = ClientData.Players.Count > 2;
                                foreach (var player in ClientData.Players)
                                {
                                    player.Delete.CanBeExecuted = canDelete;
                                }
                            }
                        }
                        finally
                        {
                            ClientData.EndUpdatePersons();
                        }

                        if (account == ClientData.Me && newAccount != null)
                        {
                            // Необходимо самого себя перевести в зрители
                            SwitchToNewType(GameRole.Viewer, newAccount);
                        }

                        UpdateDeleteTableCommand();
                    }
                    break;

                case MessageParams.Config_Set:
                    {
                        if (mparams.Length < 6)
                            break;

                        var personType = mparams[2];
                        var indexString = mparams[3];
                        var replacer = mparams[4];
                        var replacerSex = mparams[5] == "+";

                        PersonAccount account;

                        var isPlayer = personType == "player";
                        if (isPlayer)
                        {
                            if (!int.TryParse(indexString, out int index) || index < 0 || index >= ClientData.Players.Count)
                                break;

                            account = ClientData.Players[index];
                        }
                        else
                        {
                            account = ClientData.ShowMan;
                        }

                        if (!account.IsHuman)
                        {
                            account.Name = replacer;
                            account.IsMale = replacerSex;
                        }
                        else
                        {
                            var name = account.Name;
                            var sex = account.IsMale;
                            var picture = account.Picture;
                            var ready = account.Ready;
                            var isOnline = account.Connected;

                            ViewerAccount other = null;
                            GameRole role = GameRole.Viewer;

                            ClientData.BeginUpdatePersons();
                            try
                            {
                                if (isPlayer && ClientData.ShowMan.Name == replacer)
                                {
                                    // Ведущего сажаем на место игрока
                                    var showman = ClientData.ShowMan;
                                    other = showman;
                                    role = GameRole.Showman;

                                    account.Name = showman.Name;
                                    account.IsMale = showman.IsMale;
                                    account.Picture = showman.Picture;
                                    account.Ready = showman.Ready;
                                    account.Connected = showman.Connected;

                                    showman.Name = name;
                                    showman.IsMale = sex;
                                    showman.Picture = picture;
                                    showman.Ready = ready;
                                    showman.Connected = isOnline;
                                }
                                else
                                {
                                    var found = false;
                                    foreach (var item in ClientData.Players)
                                    {
                                        if (item.Name == replacer)
                                        {
                                            other = item;
                                            role = GameRole.Player;

                                            account.Name = item.Name;
                                            account.IsMale = item.IsMale;
                                            account.Picture = item.Picture;
                                            account.Ready = item.Ready;
                                            account.Connected = item.Connected;

                                            item.Name = name;
                                            item.IsMale = sex;
                                            item.Picture = picture;
                                            item.Ready = ready;
                                            item.Connected = isOnline;

                                            found = true;
                                            break;
                                        }
                                    }

                                    if (!found)
                                    {
                                        foreach (var item in ClientData.Viewers)
                                        {
                                            if (item.Name == replacer)
                                            {
                                                other = item;
                                                role = GameRole.Viewer;

                                                account.Name = item.Name;
                                                account.IsMale = item.IsMale;
                                                account.Picture = item.Picture;
                                                account.Ready = false;

                                                if (isOnline)
                                                {
                                                    item.Name = name;
                                                    item.IsMale = sex;
                                                    item.Picture = picture;
                                                }
                                                else
                                                {
                                                    // место было пустым, зрителя нужо удалить
                                                    ClientData.Viewers.Remove(item);
                                                    account.Connected = true;
                                                }

                                                found = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                ClientData.EndUpdatePersons();
                            }

                            if (account == ClientData.Me)
                            {
                                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;
                                if (newRole != role)
                                {
                                    SwitchToNewType(role, other);
                                }
                                else
                                {
                                    var current = (PersonAccount)ClientData.Me;

                                    ((PersonAccount)other).BeReadyCommand = current.BeReadyCommand;
                                    ((PersonAccount)other).BeUnReadyCommand = current.BeUnReadyCommand;

                                    current.BeReadyCommand = null;
                                    current.BeUnReadyCommand = null;

                                    ClientData.Me = other;
                                }
                            }
                            else if (other == ClientData.Me)
                            {
                                var newRole = isPlayer ? GameRole.Player : GameRole.Showman;
                                if (newRole != role)
                                {
                                    SwitchToNewType(newRole, account);
                                }
                                else
                                {
                                    var current = (PersonAccount)ClientData.Me;

                                    account.BeReadyCommand = current.BeReadyCommand;
                                    account.BeUnReadyCommand = current.BeUnReadyCommand;

                                    current.BeReadyCommand = null;
                                    current.BeUnReadyCommand = null;

                                    ClientData.Me = account;
                                }
                            }
                        }

                        UpdateDeleteTableCommand();
                    }
                    break;

                case MessageParams.Config_ChangeType:
                    {
                        if (mparams.Length < 7)
                            break;

                        var personType = mparams[2];
                        var indexString = mparams[3];
                        var newTypeHuman = mparams[4] == "+";
                        var newName = mparams[5];
                        var newSex = mparams[6] == "+";

                        PersonAccount account;
                        var isPlayer = personType == "player";
                        if (isPlayer)
                        {
                            if (!int.TryParse(indexString, out var index) || index < 0 || index >= ClientData.Players.Count)
                            {
                                break;
                            }

                            account = ClientData.Players[index];
                        }
                        else
                        {
                            account = ClientData.ShowMan;
                        }

                        if (account.IsHuman == newTypeHuman)
                        {
                            break;
                        }

                        if (newTypeHuman)
                        {
                            account.IsHuman = true;
                            account.Name = " ";
                            account.Picture = "";
                            account.Connected = false;
                            account.Ready = false;
                        }
                        else
                        {
                            ClientData.BeginUpdatePersons();
                            ViewerAccount newAccount = null;
                            try
                            {
                                if (account.Connected)
                                {
                                    var clone = new List<ViewerAccount>(ClientData.Viewers);
                                    newAccount = new ViewerAccount(account) { Connected = true };

                                    clone.Add(newAccount);

                                    ClientData.Viewers = clone;
                                }
                                else if (account == ClientData.Me)
                                {
                                    throw new InvalidOperationException("I am not connected!");
                                }

                                //if (isPlayer)
                                //{
                                //    var newAcc = new PlayerAccount(newName, newSex, true, false) { IsHuman = false, IsExtendedMode = this.IsHost };

                                //    newAcc.Free = new CustomCommand(Free_Executed) { CanBeExecuted = account.IsHuman && account.Connected };
                                //    newAcc.Replace = new CustomCommand(arg => Replace_Executed(account, arg)) { CanBeExecuted = true };
                                //    newAcc.Delete = new CustomCommand(Delete_Executed) { CanBeExecuted = this.ClientData.Players.Count > 2 };
                                //    newAcc.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };

                                //    var cloneP = new List<PlayerAccount>(this.ClientData.Players);
                                //    cloneP[index] = newAcc;

                                //    this.ClientData.Players = cloneP;
                                //}
                                //else
                                //{
                                //    var showman = new PersonAccount(newName, newSex, true, false) { IsHuman = false, IsShowman = true, IsExtendedMode = this.IsHost };

                                //    showman.Free = new CustomCommand(Free_Executed) { CanBeExecuted = showman.IsHuman && showman.Connected };
                                //    showman.Replace = new CustomCommand(arg => Replace_Executed(showman, arg)) { CanBeExecuted = showman.IsHuman };
                                //    showman.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };

                                //    this.ClientData.ShowMan = showman;
                                //}

                                account.IsHuman = false;
                                account.Name = newName;
                                account.IsMale = newSex;
                                account.Picture = "";
                                account.Connected = true;
                                account.Ready = false;
                            }
                            finally
                            {
                                ClientData.EndUpdatePersons();
                            }

                            if (account == ClientData.Me)
                            {
                                // Необходимо самого себя перевести в зрители
                                SwitchToNewType(GameRole.Viewer, newAccount);
                            }

                            UpdateDeleteTableCommand();
                        }
                    }
                    break;
            }

            foreach (var item in ClientData.Players)
            {
                UpdatePlayerCommands(item);
            }

            UpdateShowmanCommands();
        }

        /// <summary>
        /// Сменить тип своего аккаунта
        /// </summary>
        /// <param name="role">Целевой тип</param>
        private void SwitchToNewType(GameRole role, ViewerAccount newAccount)
        {
            if (newAccount == null)
            {
                throw new ArgumentNullException(nameof(newAccount));
            }

            ClientData.PersonDataExtensions.IsRight = ClientData.PersonDataExtensions.IsWrong = ClientData.PersonDataExtensions.SendCatCost =
                ClientData.PersonDataExtensions.SendFinalStake =
                ClientData.PlayerDataExtensions.SendAnswer = ClientData.ShowmanDataExtensions.ChangeSums = ClientData.ShowmanDataExtensions.ChangeSums2 =
                ClientData.ShowmanDataExtensions.Manage = ClientData.PersonDataExtensions.SendNominal =
                ClientData.PersonDataExtensions.SendPass = ClientData.PersonDataExtensions.SendStake =
                ClientData.PersonDataExtensions.SendVabank = ClientData.PlayerDataExtensions.Apellate = ClientData.PlayerDataExtensions.PressGameButton = null;

            ClientData.PlayerDataExtensions.Report.SendReport = ClientData.PlayerDataExtensions.Report.SendNoReport = null;

            ClientData.Kick = ClientData.AtomViewed = ClientData.Ban = ClientData.ForceStart = ClientData.AddTable = null;

            ClientData.MessageSending = null;

            // Пересоздадим обработчик и логику
            IViewerClient viewer = null;
            switch (role)
            {
                case GameRole.Viewer:
                    viewer = new SimpleViewer(_client, newAccount, IsHost, ClientData.BackLink, LO, ClientData);
                    break;
                case GameRole.Player:
                    viewer = new Player(_client, newAccount, IsHost, ClientData.BackLink, LO, ClientData);
                    break;
                case GameRole.Showman:
                    viewer = new Showman(_client, newAccount, IsHost, ClientData.BackLink, LO, ClientData);
                    break;
            }

            if (ClientData.Me is PersonAccount current)
            {
                current.BeReadyCommand = current.BeUnReadyCommand = null;
            }

            viewer.Avatar = Avatar;
            viewer.ServerPublicPackageUrl = ServerPublicPackageUrl;
            viewer.ContentPublicBaseUrls = ContentPublicBaseUrls;
            // TODO: Больше ничего не надо переносить в новый IViewerClient?

            ClientData.Me = newAccount;
            viewer.Init();

            Dispose();

            viewer.RecreateCommands();

            Switch?.Invoke(viewer);

            SendPicture();
        }

        private void ProcessInfo(string[] mparams)
        {
            int.TryParse(mparams[1], out int numOfPlayers);
            int numOfViewers = (mparams.Length - 2) / 5 - 1 - numOfPlayers;

            var gameStarted = ClientData.Stage != GameStage.Before;

            int mIndex = 2;
            ClientData.BeginUpdatePersons();
            try
            {
                ClientData.ShowMan = new PersonAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
                {
                    IsShowman = true,
                    IsHuman = mparams[mIndex++] == "+",
                    Ready = mparams[mIndex++] == "+"
                };

                CreateShowmanCommands();

                var newPlayers = new List<PlayerAccount>();
                for (int i = 0; i < numOfPlayers; i++)
                {
                    var account = new PlayerAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+", gameStarted)
                    {
                        IsHuman = mparams[mIndex++] == "+",
                        Ready = mparams[mIndex++] == "+"
                    };

                    newPlayers.Add(account);
                }

                ClientData.Players = newPlayers;

                foreach (var account in ClientData.Players)
                {
                    CreatePlayerCommands(account);
                }

                var newViewers = new List<ViewerAccount>();
                for (int i = 0; i < numOfViewers; i++)
                {
                    newViewers.Add(new ViewerAccount(mparams[mIndex++], mparams[mIndex++] == "+", mparams[mIndex++] == "+")
                    {
                        IsHuman = mparams[mIndex++] == "+"
                    });

                    mIndex++; // пропускаем Ready
                }

                ClientData.Viewers = newViewers;
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            foreach (var account in MyData.MainPersons)
            {
                account.IsExtendedMode = IsHost;
            }

            ClientData.Me = ClientData.AllPersons.FirstOrDefault(item => item.Name == _client.Name);
            if (ClientData.Me != null)
            {
                ClientData.Me.Picture = ClientData.Picture;
            }

            if (!_client.Server.IsMain)
            {
                foreach (var item in ClientData.AllPersons)
                {
                    if (item != ClientData.Me && item.Name != NetworkConstants.GameName && item.Connected)
                    {
                        lock (_client.Server.ConnectionsSync)
                        {
                            var externalServer = ((ISlaveServer)_client.Server).HostServer;
                            if (externalServer != null)
                            {
                                lock (externalServer.ClientsSync)
                                {
                                    externalServer.Clients.Add(item.Name);
                                }
                            }
                        }
                    }
                }
            }

            UpdateAddTableCommand();
            UpdateDeleteTableCommand();

            SendPicture();
        }

        private void UpdateDeleteTableCommand()
        {
            if (ClientData.DeleteTable == null)
            {
                return;
            }

            ClientData.DeleteTable.CanBeExecuted = IsHost && ClientData.Players.Count > 2 && (ClientData.Stage == GameStage.Before || ClientData.Players.Any(p => !p.Connected || !p.IsHuman));
        }

        private void UpdateAddTableCommand()
        {
            if (ClientData.AddTable == null)
            {
                return;
            }

            ClientData.AddTable.CanBeExecuted = IsHost && ClientData.Players.Count < Constants.MaxPlayers;
        }

        public void RecreateCommands()
        {
            CreateShowmanCommands();

            foreach (var item in ClientData.Players)
            {
                CreatePlayerCommands(item);
            }
        }

        private void CreateShowmanCommands()
        {
            var showman = ClientData.ShowMan;

            showman.Free = new CustomCommand(Free_Executed) { CanBeExecuted = showman.IsHuman && showman.Connected };
            showman.Replace = new CustomCommand(arg => Replace_Executed(showman, arg)) { CanBeExecuted = showman.IsHuman };
            showman.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };
        }

        private void CreatePlayerCommands(PlayerAccount player)
        {
            player.Free = new CustomCommand(Free_Executed) { CanBeExecuted = player.IsHuman && player.Connected };
            player.Replace = new CustomCommand(arg => Replace_Executed(player, arg)) { CanBeExecuted = true };
            player.Delete = new CustomCommand(Delete_Executed) { CanBeExecuted = ClientData.Players.Count > 2 };
            player.ChangeType = new CustomCommand(ChangeType_Executed) { CanBeExecuted = true };

            UpdateOthers(player);
        }

        private void UpdatePlayerCommands(PlayerAccount player)
        {
            player.Free.CanBeExecuted = player.IsHuman && player.Connected;
            UpdateOthers(player);

            player.Replace.CanBeExecuted = player.Others.Any();
        }

        private void UpdateOthers(PlayerAccount player)
        {
            player.Others = player.IsHuman ?
                MyData.AllPersons.Where(p => p.IsHuman && p.Connected)
                    .Except(new ViewerAccount[] { player })
                    .ToArray()
                : (Account[])ComputerAccount.GetDefaultPlayers(LO, MyData.BackLink.PhotoUri)
                    .Where(a => !MyData.AllPersons.Any(p => !p.IsHuman && p.Name == a.Name))
                    .ToArray();
        }

        private void UpdateShowmanCommands()
        {
            var showman = MyData.ShowMan;
            if (showman == null || showman.Free == null)
                return;

            showman.Free.CanBeExecuted = showman.IsHuman && showman.Connected;

            showman.Others = showman.IsHuman ?
                    MyData.AllPersons.Where(p => p.IsHuman && p.Connected).Except(new ViewerAccount[] { showman }).ToArray()
                    : new ViewerAccount[0];

            showman.Replace.CanBeExecuted = showman.Others.Any();
        }

        private void InsertPerson(string role, Account account, int index)
        {
            ClientData.BeginUpdatePersons();

            try
            {
                switch (role)
                {
                    case "showman":
                        ClientData.ShowMan = new PersonAccount(account) { IsHuman = true, Connected = true, Ready = false, GameStarted = ClientData.Stage != GameStage.Before, IsShowman = true, IsExtendedMode = IsHost };
                        CreateShowmanCommands();
                        break;

                    case "player":
                        while (index >= ClientData.Players.Count)
                        {
                            var p = new PlayerAccount(Constants.FreePlace, true, false, ClientData.Stage != GameStage.Before) { IsHuman = true, Ready = false, IsExtendedMode = IsHost };

                            CreatePlayerCommands(p);

                            ClientData.Players.Add(p);
                            ClientData.UpdatePlayers();
                        }

                        var player = ClientData.Players[index];

                        player.Name = account.Name;
                        player.Picture = account.Picture;
                        player.IsMale = account.IsMale;
                        player.IsHuman = true;
                        player.Connected = true;
                        player.Ready = false;
                        player.GameStarted = ClientData.Stage != GameStage.Before;
                        break;

                    default:
                        var viewer = new ViewerAccount(account) { IsHuman = true, Connected = true };
                        ClientData.Viewers.Add(viewer);
                        ClientData.UpdateViewers();
                        break;
                }
            }
            finally
            {
                ClientData.EndUpdatePersons();
            }

            if (ClientData.Stage == GameStage.Before)
            {
                UpdateShowmanCommands();

                foreach (var item in ClientData.Players)
                {
                    UpdatePlayerCommands(item);
                }
            }
        }

        /// <summary>
        /// Получение сообщения
        /// </summary>
        public override void OnMessageReceived(Message message)
        {
            if (message.IsSystem)
            {
                OnSystemMessageReceived(message.Text.Split('\n'));
            }
            else
            {
                _logic.ReceiveText(message);
            }
        }

        /// <summary>
        /// Заполнить данные аккаунта на основании присланной информации
        /// </summary>
        /// <param name="knownAccount"></param>
        /// <param name="name"></param>
        /// <param name="sex"></param>
        /// <param name="connected"></param>
        private void SetAccountData(ViewerAccount knownAccount, string name, string sex, string connected)
        {
            knownAccount.Name = name;
            knownAccount.IsMale = sex == "+";
            knownAccount.Connected = connected == "+";
        }

        /// <summary>
        /// Сказать в чат
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <param name="whom">Кому</param>
        /// <param name="isPrivate">Приватно ли</param>
        internal void Say(string text, string whom = NetworkConstants.Everybody, bool isPrivate = false)
        {
            if (whom != NetworkConstants.Everybody)
                text = string.Format("{0}, {1}", whom, text);

            if (!isPrivate)
                whom = NetworkConstants.Everybody;
            else
                text = string.Format("({0}) {1}", LO[nameof(R.Private)], text);

            _client.SendMessage(text, false, whom, isPrivate);
            _logic.ReceiveText(new Message(text, _client.Name, whom, false, isPrivate));
        }

        /// <summary>
        /// Отправить сообщение всем
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        public void SendMessage(string text)
        {
            _client.SendMessage(text, receiver: NetworkConstants.GameName);
        }

        public void SendMessage(params string[] args)
        {
            _client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);
        }

        public void SendMessageWithArgs(params object[] args)
        {
            _client.SendMessage(string.Join(Message.ArgsSeparator, args), receiver: NetworkConstants.GameName);
        }

        public void Pause()
        {
            SendMessage(Messages.Pause, ClientData.TInfo.Pause ? "-" : "+");
        }

        /// <summary>
        /// Выдаёт информацию о расположении своей картинки
        /// </summary>
        /// <param name="path"></param>
        public void SendPicture()
        {
            if (Avatar != null)
            {
                SendMessage(Messages.Picture, Avatar);
                return;
            }

            if (!string.IsNullOrEmpty(ClientData.Picture))
            {
                if (!Uri.TryCreate(ClientData.Picture, UriKind.RelativeOrAbsolute, out Uri uri))
                    return;

                if (!uri.IsAbsoluteUri)
                    return;

                if (uri.Scheme == "file" && !_client.Server.Contains(NetworkConstants.GameName)) // Нужно передать локальный файл по сети
                {
                    byte[] data = null;
                    try
                    {
                        data = CoreManager.Instance.GetData(ClientData.Picture);
                    }
                    catch (Exception exc)
                    {
                        ClientData.BackLink.SendError(exc, true);
                    }

                    if (data == null)
                        return;

                    SendMessage(Messages.Picture, ClientData.Picture, Convert.ToBase64String(data));
                }
                else
                    SendMessage(Messages.Picture, ClientData.Picture);
            }
        }

        /// <summary>
        /// Получить информацию об игре
        /// </summary>
        public void GetInfo() => SendMessage(Messages.Info);

        public void Rename(string name)
        {
            _client.Name = name;
        }

        public virtual void Init()
        {
            ClientData.IsPlayer = false;
        }
    }
}
