﻿using SICore.BusinessLogic;
using SICore.Network.Clients;
using SICore.Network.Contracts;
using SIData;
using SIPackages.Core;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Создание клиента
        /// </summary>
        /// <param name="personData">Данные игрока</param>
        /// <param name="isHost">Является ли владельцем игры</param>
        /// <param name="form">Имеет ли форму для вывода</param>
        public Player(Client client, Account personData, bool isHost, IGameManager backLink, ILocalizer localizer, ViewerData data = null)
            : base(client, personData, isHost, backLink, localizer, data)
        {
            ClientData.PlayerDataExtensions.PressGameButton = new CustomCommand(arg =>
            {
                SendMessage(Messages.I);
                DisableGameButton(false);
                ReleaseGameButton();
            }) { CanBeExecuted = true };

            ClientData.PlayerDataExtensions.SendAnswer = new CustomCommand(arg => { SendMessage(Messages.Answer, ClientData.PersonDataExtensions.Answer); Clear(); });
            ClientData.PersonDataExtensions.SendCatCost = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.CatCost, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PersonDataExtensions.SendNominal = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.Stake, 0);
                Clear();
            });

            ClientData.PersonDataExtensions.SendStake = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.Stake, 1, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PersonDataExtensions.SendPass = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.Stake, 2);
                Clear();
            });

            ClientData.PersonDataExtensions.SendVabank = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.Stake, 3);
                Clear();
            });

            ClientData.PersonDataExtensions.SendFinalStake = new CustomCommand(arg =>
            {
                SendMessageWithArgs(Messages.FinalStake, ClientData.PersonDataExtensions.StakeInfo.Stake);
                Clear();
            });

            ClientData.PlayerDataExtensions.Apellate = new CustomCommand(arg =>
            {
                ClientData.PlayerDataExtensions.NumApps--;
                SendMessage(Messages.Apellate, arg.ToString());
            }) { CanBeExecuted = false };

            ClientData.PlayerDataExtensions.Pass = new CustomCommand(arg =>
            {
                SendMessage(Messages.Pass);
            })
            { CanBeExecuted = false };

            ClientData.PersonDataExtensions.IsRight = new CustomCommand(arg => { SendMessage(Messages.IsRight, "+"); Clear(); });
            ClientData.PersonDataExtensions.IsWrong = new CustomCommand(arg => { SendMessage(Messages.IsRight, "-"); Clear(); });

            ClientData.PlayerDataExtensions.Report.Title = LO[nameof(R.ReportTitle)];
            ClientData.PlayerDataExtensions.Report.Subtitle = LO[nameof(R.ReportTip)];

            ClientData.PlayerDataExtensions.Report.SendReport = new CustomCommand(arg => { SendMessage(Messages.Report, "ACCEPT", ClientData.SystemLog.ToString() + ClientData.PlayerDataExtensions.Report.Comment); Clear(); });
            ClientData.PlayerDataExtensions.Report.SendNoReport = new CustomCommand(arg => { SendMessage(Messages.Report, "DECLINE"); Clear(); });

            ClientData.AutoReadyChanged += ClientData_AutoReadyChanged;
        }

        protected override IPlayer CreateLogic(Account personData)
        {
            return personData.IsHuman ?
                (IPlayer)new PlayerHumanLogic(this, ClientData) :
                new PlayerComputerLogic(this, ClientData, (ComputerAccount)personData);
        }

        public override void Dispose()
        {
            ClientData.AutoReadyChanged -= ClientData_AutoReadyChanged;

            base.Dispose();
        }

        void ClientData_AutoReadyChanged()
        {
            lock (_readyLock)
            {
                if (ClientData.Me == null)
                    return;

                var readyCommand = ((PersonAccount)ClientData.Me).BeReadyCommand;
                if (ClientData.AutoReady && readyCommand != null)
                    readyCommand.Execute(null);
            }
        }

        private async void ReleaseGameButton()
        {
            try
            {
                await Task.Delay(3000);
                buttonDisabledByTimer = false;
                EnableGameButton();
            }
            catch
            {

            }
        }

        private void Clear()
        {
            _logic.Clear();
        }

        public override void Init()
        {
            base.Init();

            ClientData.IsPlayer = true;

            lock (_readyLock)
            {
                if (ClientData.Me is PersonAccount personAccount)
                {
                    var readyCommand = personAccount.BeReadyCommand = new CustomCommand(arg => SendMessage(Messages.Ready));
                    personAccount.BeUnReadyCommand = new CustomCommand(arg => SendMessage(Messages.Ready, "-"));
                    _logic.OnInitialized();

                    if (ClientData.AutoReady)
                        readyCommand.Execute(null);
                }
                else
                    ClientData.Hint = LO[nameof(R.HintInitError)];
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

                        if (mparams[1] == "Round")
                        {
                            lock (ClientData.ChoiceLock)
                            {
                                ClientData.QuestionIndex = -1;
                                ClientData.ThemeIndex = -1;
                            }
                        }
                        else if (mparams[1] == "Final")
                        {
                            ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                        }

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
                        if (ClientData._qtype == QuestionTypes.Simple)
                        {
                            buttonDisabledByGame = false;
                            EnableGameButton();

                            if (!ClientData.FalseStart)
                                ClientData.PlayerDataExtensions.MyTry = true;
                        }
                        break;

                    case Messages.YouTry:
                        ClientData.PlayerDataExtensions.MyTry = true;
                        buttonDisabledByGame = false;
                        EnableGameButton();
                        break;

                    case Messages.EndTry:
                        ClientData.PlayerDataExtensions.MyTry = false;
                        DisableGameButton();

                        if (mparams[1] == "A")
                            _logic.EndThink();
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
                            ClientData.Players[i].SelectionCallback = player => { SendMessageWithArgs(Messages.Cat, num); Clear(); };
                        }

                        ClientData.Hint = LO[nameof(R.HintSelectCatPlayer)];

                        _logic.Cat();
                        break;

                    case Messages.CatCost:
                        ClientData.PersonDataExtensions.StakeInfo = new StakeInfo()
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
                            ClientData.PersonDataExtensions.Var[i] = mparams[i + 1] == "+";

                        ClientData.PersonDataExtensions.StakeInfo = new StakeInfo()
                        {
                            Minimum = Int32.Parse(mparams[5]),
                            Maximum = ((PlayerAccount)ClientData.Me).Sum,
                            Step = 100,
                            Stake = Int32.Parse(mparams[5])
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

        private bool buttonDisabledByGame = false;
        private bool buttonDisabledByTimer = false;

        private void DisableGameButton(bool byGame = true)
        {
            ClientData.PlayerDataExtensions.PressGameButton.CanBeExecuted = false;

            if (byGame)
                buttonDisabledByGame = true;
            else
                buttonDisabledByTimer = true;
        }

        private void EnableGameButton()
        {
            if (!buttonDisabledByGame && !buttonDisabledByTimer)
                ClientData.PlayerDataExtensions.PressGameButton.CanBeExecuted = true;
        }

        /// <summary>
        /// Сумма ближайшего преследователя
        /// </summary>
        public int BigSum
        {
            get
            {
                return ClientData.Players.Where(player => player.Name != _client.Name).Max(player => player.Sum);
            }
        }

        /// <summary>
        /// Сумма дальнего преследователя
        /// </summary>
        public int SmallSum
        {
            get
            {
                return ClientData.Players.Where(player => player.Name != _client.Name).Min(player => player.Sum);
            }
        }

        /// <summary>
        /// Собственный счёт
        /// </summary>
        public int MySum
        {
            get
            {
                return ((PlayerAccount)ClientData.Me).Sum;
            }
        }

        /// <summary>
        /// Жмёт на игровую кнопку
        /// </summary>
        internal void PressGameButton()
        {
            SendMessage(Messages.I);
        }

        ///// <summary>
        ///// Апеллирует
        ///// </summary>
        //internal void Apellate()
        //{
        //    SendMessage(Messages.Apellate);
        //    this.ClientData.NumApps--;
        //}
    }
}
