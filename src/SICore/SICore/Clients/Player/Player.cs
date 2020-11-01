using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using R = SICore.Properties.Resources;

namespace SICore
{
    /// <summary>
    /// Клиент игрока
    /// </summary>
    public sealed class Player : Viewer<IPlayer>
    {
        private readonly object _readyLock = new object();

        private bool _buttonDisabledByGame = false;
        private bool _buttonDisabledByTimer = false;

        /// <summary>
        /// Создание клиента
        /// </summary>
        /// <param name="personData">Данные игрока</param>
        /// <param name="isHost">Является ли владельцем игры</param>
        /// <param name="form">Имеет ли форму для вывода</param>
        public Player(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
            : base(client, personData, isHost, localizer, data)
        {
            ClientData.PlayerDataExtensions.PressGameButton = new CustomCommand(arg =>
            {
                _viewerActions.SendMessage(Messages.I);
                DisableGameButton(false);
                ReleaseGameButtonAsync();
            }) { CanBeExecuted = true };

            ClientData.PlayerDataExtensions.SendAnswer = new CustomCommand(arg => { _viewerActions.SendMessage(Messages.Answer, ClientData.PersonDataExtensions.Answer); Clear(); });
            ClientData.PersonDataExtensions.SendCatCost = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.CatCost, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PersonDataExtensions.SendNominal = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.Stake, 0);
                Clear();
            });

            ClientData.PersonDataExtensions.SendStake = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.Stake, 1, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PersonDataExtensions.SendPass = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.Stake, 2);
                Clear();
            });

            ClientData.PersonDataExtensions.SendVabank = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.Stake, 3);
                Clear();
            });

            ClientData.PersonDataExtensions.SendFinalStake = new CustomCommand(arg =>
            {
                _viewerActions.SendMessageWithArgs(Messages.FinalStake, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PlayerDataExtensions.Apellate = new CustomCommand(arg =>
            {
                ClientData.PlayerDataExtensions.NumApps--;
                _viewerActions.SendMessage(Messages.Apellate, arg.ToString());
            }) { CanBeExecuted = false };

            ClientData.PlayerDataExtensions.Pass = new CustomCommand(arg =>
            {
                _viewerActions.SendMessage(Messages.Pass);
            })
            { CanBeExecuted = false };

            ClientData.PersonDataExtensions.IsRight = new CustomCommand(arg => { _viewerActions.SendMessage(Messages.IsRight, "+"); Clear(); });
            ClientData.PersonDataExtensions.IsWrong = new CustomCommand(arg => { _viewerActions.SendMessage(Messages.IsRight, "-"); Clear(); });

            ClientData.PlayerDataExtensions.Report.Title = LO[nameof(R.ReportTitle)];
            ClientData.PlayerDataExtensions.Report.Subtitle = LO[nameof(R.ReportTip)];

            ClientData.PlayerDataExtensions.Report.SendReport = new CustomCommand(arg => { _viewerActions.SendMessage(Messages.Report, "ACCEPT", ClientData.SystemLog.ToString() + ClientData.PlayerDataExtensions.Report.Comment); Clear(); });
            ClientData.PlayerDataExtensions.Report.SendNoReport = new CustomCommand(arg => { _viewerActions.SendMessage(Messages.Report, "DECLINE"); Clear(); });

            ClientData.AutoReadyChanged += ClientData_AutoReadyChanged;
        }

        protected override IPlayer CreateLogic(Account personData)
        {
            return personData.IsHuman ?
                (IPlayer)new PlayerHumanLogic(ClientData, _viewerActions, LO) :
                new PlayerComputerLogic(ClientData, (ComputerAccount)personData, _viewerActions);
        }

        public override void Dispose(bool disposing)
        {
            ClientData.AutoReadyChanged -= ClientData_AutoReadyChanged;

            base.Dispose(disposing);
        }

        private void ClientData_AutoReadyChanged()
        {
            lock (_readyLock)
            {
                if (ClientData.Me == null)
                {
                    return;
                }

                var readyCommand = ((PersonAccount)ClientData.Me).BeReadyCommand;
                if (ClientData.AutoReady && readyCommand != null)
                {
                    readyCommand.Execute(null);
                }
            }
        }

        private async void ReleaseGameButtonAsync()
        {
            try
            {
                await Task.Delay(ClientData.ButtonBlockingTime * 1000);
                _buttonDisabledByTimer = false;
                EnableGameButton();
            }
            catch (Exception exc)
            {
                Client.CurrentServer.OnError(exc, true);
            }
        }

        private void Clear() => _logic.Clear();

        public override void Init()
        {
            base.Init();

            ClientData.IsPlayer = true;

            lock (_readyLock)
            {
                if (ClientData.Me is PersonAccount personAccount)
                {
                    var readyCommand = personAccount.BeReadyCommand = new CustomCommand(arg => _viewerActions.SendMessage(Messages.Ready));
                    personAccount.BeUnReadyCommand = new CustomCommand(arg => _viewerActions.SendMessage(Messages.Ready, "-"));
                    _logic.OnInitialized();

                    if (ClientData.AutoReady)
                    {
                        readyCommand.Execute(null);
                    }
                }
                else
                {
                    ClientData.Hint = LO[nameof(R.HintInitError)];
                }
            }
        }

        /// <summary>
        /// Получение системного сообщения
        /// </summary>
        protected override void OnSystemMessageReceived(string[] mparams)
        {
            base.OnSystemMessageReceived(mparams);

            try
            {
                switch (mparams[0])
                {
                    case Messages.Info2:
                        Init();
                        break;

                    case Messages.Stage:
                        #region STAGE

                        if (mparams.Length == 0)
                        {
                            break;
                        }

                        if (mparams[1] == nameof(GameStage.Round))
                        {
                            lock (ClientData.ChoiceLock)
                            {
                                ClientData.QuestionIndex = -1;
                                ClientData.ThemeIndex = -1;
                            }

                            Clear();
                        }
                        else if (mparams[1] == nameof(GameStage.Final))
                        {
                            ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                        }

                        ClientData.PlayerDataExtensions.Apellate.CanBeExecuted = false;

                        #endregion
                        break;

                    case Messages.Cancel:
                        Clear();
                        break;

                    case Messages.Choose:
                        #region Choose

                        if (mparams[1] == "1")
                        {
                            _logic.ChooseQuest();
                            ClientData.Hint = LO[nameof(R.HintSelectQuestion)];
                        }
                        else
                        {
                            _logic.ChooseFinalTheme();
                            ClientData.Hint = LO[nameof(R.HintSelectTheme)];
                        }

                        #endregion
                        break;

                    case Messages.Choice:
                        ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                        ClientData.PlayerDataExtensions.Apellate.CanBeExecuted = false;
                        break;

                    case Messages.Theme:
                        ClientData.QuestionIndex = -1;
                        break;

                    case Messages.Question:
                        ClientData.QuestionIndex++;
                        ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                        break;

                    case Messages.Atom:
                        if (ClientData.QuestionType == QuestionTypes.Simple)
                        {
                            _buttonDisabledByGame = false;
                            EnableGameButton();

                            if (!ClientData.FalseStart)
                                ClientData.PlayerDataExtensions.MyTry = true;
                        }
                        break;

                    case Messages.Try:
                        ClientData.PlayerDataExtensions.Pass.CanBeExecuted = true;
                        ClientData.PlayerDataExtensions.Apellate.CanBeExecuted = false;
                        break;

                    case Messages.YouTry:
                        ClientData.PlayerDataExtensions.MyTry = true;
                        _buttonDisabledByGame = false;
                        EnableGameButton();
                        _logic.StartThink();
                        break;

                    case Messages.EndTry:
                        ClientData.PlayerDataExtensions.MyTry = false;
                        DisableGameButton();

                        if (mparams[1] == MessageParams.EndTry_All)
                        {
                            _logic.EndThink();

                            ClientData.PlayerDataExtensions.Apellate.CanBeExecuted = ClientData.PlayerDataExtensions.NumApps > 0;
                            ClientData.PlayerDataExtensions.Pass.CanBeExecuted = false;
                        }
                        break;

                    case Messages.Answer:
                        ClientData.PersonDataExtensions.Answer = "";
                        ClientData.DialogMode = DialogModes.Answer;

                        ((PlayerAccount)ClientData.Me).IsDeciding = false;

                        _logic.Answer();
                        break;

                    case Messages.Cat:
                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].CanBeSelected = mparams[i + 1] == "+";
                            int num = i;
                            ClientData.Players[i].SelectionCallback = player => { _viewerActions.SendMessageWithArgs(Messages.Cat, num); Clear(); };
                        }

                        ClientData.Hint = LO[nameof(R.HintSelectCatPlayer)];

                        _logic.Cat();
                        break;

                    case Messages.CatCost:
                        ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                        {
                            Minimum = int.Parse(mparams[1]),
                            Maximum = int.Parse(mparams[2]),
                            Step = int.Parse(mparams[3]),
                            Stake = int.Parse(mparams[1])
                        };

                        _logic.CatCost();
                        break;

                    case Messages.Stake:
                        ClientData.PersonDataExtensions.SendNominal.CanBeExecuted = mparams[1] == "+";
                        ClientData.PersonDataExtensions.SendStake.CanBeExecuted = mparams[2] == "+";
                        ClientData.PersonDataExtensions.SendPass.CanBeExecuted = mparams[3] == "+";
                        ClientData.PersonDataExtensions.SendVabank.CanBeExecuted = mparams[4] == "+";
                        for (int i = 0; i < 4; i++)
                        {
                            ClientData.PersonDataExtensions.Var[i] = mparams[i + 1] == "+";
                        }

                        ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                        {
                            Minimum = int.Parse(mparams[5]),
                            Maximum = ((PlayerAccount)ClientData.Me).Sum,
                            Step = 100,
                            Stake = int.Parse(mparams[5])
                        };

                        _logic.Stake();
                        break;

                    case Messages.FinalStake:
                        ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                        {
                            Minimum = 1,
                            Maximum = ((PlayerAccount)ClientData.Me).Sum,
                            Step = 1,
                            Stake = 1
                        };

                        ClientData.Hint = LO[nameof(R.HintMakeAStake)];
                        ClientData.DialogMode = DialogModes.FinalStake;
                        ((PlayerAccount)ClientData.Me).IsDeciding = false;

                        _logic.FinalStake();
                        break;

                    case Messages.IsRight:
                        {
                            ClientData.PersonDataExtensions.Answer = mparams[1];
                            var right = new List<string>();

                            int L = mparams.Length;

                            for (int i = 2; i < L; i++)
                                right.Add(mparams[i]);

                            ClientData.PersonDataExtensions.Right = right.ToArray();
                        }
                        break;

                    case Messages.Wrong:
                        {
                            var wrong = new List<string>();
                            var length = mparams.Length;

                            for (int i = 2; i < length; i++)
                                wrong.Add(mparams[i]);

                            ClientData.PersonDataExtensions.Wrong = wrong.ToArray();

                            _logic.IsRight(length < 2 || mparams[1] != "-");

                            ClientData.Hint = LO[nameof(R.HintCheckAnswer)];
                            ClientData.DialogMode = DialogModes.AnswerValidation;
                            ((PlayerAccount)ClientData.Me).IsDeciding = false;
                        }
                        break;

                    case Messages.Person:
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

                        _logic.PersonAnswered(playerIndex, isRight);
                        break;

                    case Messages.Connected:
                        if (mparams[3] == _client.Name)
                        {
                            return;
                        }
                        _logic.Connected(mparams[3]);
                        break;

                    //case Messages.Tablo:
                    case Messages.Table:
                        {
                            #region Tablo2

                            _logic.Table();

                            #endregion
                            break;
                        }

                    case Messages.RoundThemes:
                        {
                            #region RoundThemes

                            if (ClientData.Stage == GameStage.Final)
                                _logic.FinalThemes();

                            #endregion
                            break;
                        }

                    case Messages.Report:
                        var report = new StringBuilder();
                        for (int r = 1; r < mparams.Length; r++)
                        {
                            report.AppendLine(mparams[r]);
                        }

                        ClientData.PlayerDataExtensions.Report.Report = report.ToString();
                        ClientData.DialogMode = DialogModes.Report;
                        ((PlayerAccount)ClientData.Me).IsDeciding = false;
                        _logic.Report();
                        break;
                }
            }
            catch (Exception exc)
            {
                throw new Exception(string.Join("\n", mparams), exc);
            }
        }

        private void DisableGameButton(bool byGame = true)
        {
            ClientData.PlayerDataExtensions.PressGameButton.CanBeExecuted = false;

            if (byGame)
                _buttonDisabledByGame = true;
            else
                _buttonDisabledByTimer = true;
        }

        private void EnableGameButton()
        {
            if (!_buttonDisabledByGame && !_buttonDisabledByTimer)
            {
                ClientData.PlayerDataExtensions.PressGameButton.CanBeExecuted = true;
            }
        }
    }
}
