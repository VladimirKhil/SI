using SICore.BusinessLogic;
using SICore.Network.Clients;
using SIData;
using R = SICore.Properties.Resources;

namespace SICore;

/// <summary>
/// Defines a showman message processor.
/// </summary>
public sealed class Showman : Viewer<IShowmanLogic>
{
    private readonly object _readyLock = new();

    public Showman(Client client, Account personData, bool isHost, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, localizer, data)
    {
        ClientData.PersonDataExtensions.IsRight = new CustomCommand(arg =>
        {
            _viewerActions.SendMessage(Messages.IsRight, "+", arg?.ToString() ?? "1");
            ClearSelections();
        });

        ClientData.PersonDataExtensions.IsWrong = new CustomCommand(arg =>
        {
            _viewerActions.SendMessage(Messages.IsRight, "-");
            ClearSelections();
        });

        ClientData.ShowmanDataExtensions.ChangeSums = new CustomCommand(arg =>
        {
            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                ClientData.Players[i].CanBeSelected = true;
                int num = i;

                ClientData.Players[i].SelectionCallback = player =>
                {
                    ClientData.ShowmanDataExtensions.SelectedPlayer = new Pair { First = num + 1, Second = player.Sum };
                    ClientData.DialogMode = DialogModes.ChangeSum;

                    for (int j = 0; j < ClientData.Players.Count; j++)
                    {
                        ClientData.Players[j].CanBeSelected = false;
                    }

                    ClientData.Hint = LO[nameof(R.HintChangeSum)];
                };
            }

            ClientData.Hint = LO[nameof(R.HintSelectPlayerForSumChange)];
        });

        ClientData.ShowmanDataExtensions.ChangeActivePlayer = new CustomCommand(arg =>
        {
            for (int i = 0; i < ClientData.Players.Count; i++)
            {
                ClientData.Players[i].CanBeSelected = true;
                int num = i;

                ClientData.Players[i].SelectionCallback = player =>
                {
                    _viewerActions.SendMessageWithArgs(Messages.SetChooser, num);
                    ClearSelections();
                };
            }

            ClientData.Hint = LO[nameof(R.HintSelectActivePlayer)];
        });

        ClientData.ShowmanDataExtensions.ChangeSums2 = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(
                Messages.Change,
                ClientData.ShowmanDataExtensions.SelectedPlayer.First,
                ClientData.ShowmanDataExtensions.SelectedPlayer.Second);

            ClearSelections();
        });

        ClientData.ShowmanDataExtensions.Manage = new CustomCommand(Manage_Executed);
        ClientData.ShowmanDataExtensions.ManageTable = new CustomCommand(ManageTable_Executed) { CanBeExecuted = ClientData.TInfo.Pause };

        ClientData.AutoReadyChanged += ClientData_AutoReadyChanged;

        ClientData.PersonDataExtensions.AreAnswersShown = data.BackLink.AreAnswersShown;
        ClientData.PropertyChanged += ClientData_PropertyChanged;

        ClientData.PersonDataExtensions.SendCatCost = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.CatCost, ClientData.PersonDataExtensions.StakeInfo.Stake);
            ClearSelections();
        });

        ClientData.PersonDataExtensions.SendNominal = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.Stake, 0);
            ClearSelections();
        });

        ClientData.PersonDataExtensions.SendStake = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.Stake, 1, ClientData.PersonDataExtensions.StakeInfo.Stake);
            ClearSelections();
        });

        ClientData.PersonDataExtensions.SendPass = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.Stake, 2);
            ClearSelections();
        });

        ClientData.PersonDataExtensions.SendVabank = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.Stake, 3);
            ClearSelections();
        });
    }

    protected override IShowmanLogic CreateLogic(Account personData) =>
        personData.IsHuman ?
            new ShowmanHumanLogic(ClientData, null, _viewerActions, LO) :
            new ShowmanComputerLogic(ClientData, _viewerActions, (ComputerAccount)personData);

    private void ClientData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonData.AreAnswersShown))
        {
            ClientData.BackLink.AreAnswersShown = ClientData.PersonDataExtensions.AreAnswersShown;
        }
    }

    private void Manage_Executed(object? arg)
    {
        ClientData.DialogMode = DialogModes.Manage;
    }

    private void ManageTable_Executed(object? arg) => _logic.ManageTable();

    void ClientData_AutoReadyChanged()
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

    public override void Init()
    {
        base.Init();

        lock (_readyLock)
        {
            var readyCommand = ((PersonAccount)ClientData.Me).BeReadyCommand = new CustomCommand(arg => _viewerActions.SendMessage(Messages.Ready));
            ((PersonAccount)ClientData.Me).BeUnReadyCommand = new CustomCommand(arg => _viewerActions.SendMessage(Messages.Ready, "-"));
            _logic.OnInitialized();

            if (ClientData.AutoReady)
            {
                readyCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Получение сообщения
    /// </summary>
    protected override async ValueTask OnSystemMessageReceivedAsync(string[] mparams)
    {
        await base.OnSystemMessageReceivedAsync(mparams);

        try
        {
            switch (mparams[0])
            {
                case Messages.Info2:
                    Init();
                    break;

                case Messages.Cancel:                     
                    ClearSelections(true);
                    break;

                case Messages.First:
                    {
                        #region First

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].CanBeSelected = i + 1 < mparams.Length && mparams[i + 1] == "+";
                            int num = i;
                            ClientData.Players[i].SelectionCallback = player => { _viewerActions.SendMessageWithArgs(Messages.First, num); ClearSelections(); };
                        }

                        if (ClientData.Speaker != null)
                        {
                            ClientData.Speaker.Replic = "";
                        }

                        ClientData.Hint = LO[nameof(R.HintSelectStarter)];

                        _logic.StarterChoose();
                        break;

                        #endregion
                    }
                case Messages.FirstStake:
                    {
                        #region FirstStake

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].CanBeSelected = i + 1 < mparams.Length && mparams[i + 1] == "+";
                            int num = i;
                            ClientData.Players[i].SelectionCallback = player => { _viewerActions.SendMessageWithArgs(Messages.Next, num); ClearSelections(); };
                        }

                        if (ClientData.Speaker != null)
                            ClientData.Speaker.Replic = "";
                        
                        ClientData.Hint = LO[nameof(R.HintSelectStaker)];

                        _logic.FirstStake();
                        break;

                        #endregion
                    }

                case Messages.Validation:
                    OnValidation(mparams);
                    break;

                case Messages.Validation2:
                    OnValidation2(mparams);
                    break;

                case Messages.FirstDelete:
                    {
                        #region FirstDelete

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].CanBeSelected = i + 1 < mparams.Length && mparams[i + 1] == "+";
                            int num = i;
                            ClientData.Players[i].SelectionCallback = player => { _viewerActions.SendMessageWithArgs(Messages.NextDelete, num); ClearSelections(); };
                        }

                        if (ClientData.Speaker != null)
                        {
                            ClientData.Speaker.Replic = "";
                        }

                        ClientData.Hint = LO[nameof(R.HintThemeDeleter)];

                        _logic.FirstDelete();
                        break;

                        #endregion
                    }
                case Messages.Hint:
                    {
                        ClientData.Hint = $"{LO[nameof(R.RightAnswer)].ToUpperInvariant()} : {mparams[1]}";
                        break;
                    }
                case Messages.Stage:
                    {
                        ClientData.Hint = "";

                        for (int i = 0; i < ClientData.Players.Count; i++)
                        {
                            ClientData.Players[i].CanBeSelected = false;
                        }
                        break;
                    }

                // Oral game commands (the showman performs actions announced by players)
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

                case Messages.Cat:
                    for (int i = 0; i < ClientData.Players.Count; i++)
                    {
                        ClientData.Players[i].CanBeSelected = mparams[i + 1] == "+";
                        int num = i;
                        ClientData.Players[i].SelectionCallback = player => { _viewerActions.SendMessageWithArgs(Messages.Cat, num); ClearSelections(); };
                    }

                    ClientData.Hint = LO[nameof(R.HintSelectCatPlayerForPlayer)];

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

                    for (var i = 0; i < 4; i++)
                    {
                        ClientData.PersonDataExtensions.Var[i] = mparams[i + 1] == "+";
                    }

                    ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                    {
                        Minimum = int.Parse(mparams[5]),
                        Maximum = mparams.Length >= 7 ? int.Parse(mparams[6]) : int.Parse(mparams[5]),
                        Step = 100,
                        Stake = int.Parse(mparams[5]),
                        PlayerName = mparams.Length >= 8 ? mparams[7] : null,
                    };

                    _logic.Stake();
                    break;

                case Messages.Pause:
                    OnPause(mparams);
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join(Message.ArgsSeparator, mparams), exc);
        }
    }

    private void OnPause(string[] mparams)
    {
        var isPaused = mparams[1] == "+";
        var manageTableCommand = ClientData.ShowmanDataExtensions.ManageTable;

        if (manageTableCommand != null)
        {
            manageTableCommand.CanBeExecuted = isPaused;
        }

        if (!isPaused)
        {
            _logic.ManageTable(false);
        }
    }

    private void OnValidation(string[] mparams)
    {
        ClientData.PersonDataExtensions.ValidatorName = mparams[1];
        ClientData.PersonDataExtensions.Answer = mparams[2];
        _ = int.TryParse(mparams[4], out var rightAnswersCount);
        rightAnswersCount = Math.Min(rightAnswersCount, mparams.Length - 5);

        var right = new List<string>();

        for (int i = 0; i < rightAnswersCount; i++)
        {
            right.Add(mparams[5 + i]);
        }

        var wrong = new List<string>();

        for (int i = 5 + rightAnswersCount; i < mparams.Length; i++)
        {
            wrong.Add(mparams[i]);
        }

        ClientData.PersonDataExtensions.Right = right.ToArray();
        ClientData.PersonDataExtensions.Wrong = wrong.ToArray();

        ClientData.Hint = LO[nameof(R.HintCheckAnswer)];
        ClientData.DialogMode = DialogModes.AnswerValidation;
        ((PersonAccount)ClientData.Me).IsDeciding = false;
        _logic.IsRight();
    }

    private void OnValidation2(string[] mparams)
    {
        ClientData.PersonDataExtensions.ValidatorName = mparams[1];
        ClientData.PersonDataExtensions.Answer = mparams[2];
        _ = int.TryParse(mparams[5], out var rightAnswersCount);
        rightAnswersCount = Math.Min(rightAnswersCount, mparams.Length - 6);

        var right = new List<string>();

        for (var i = 0; i < rightAnswersCount; i++)
        {
            right.Add(mparams[6 + i]);
        }

        var wrong = new List<string>();

        for (var i = 6 + rightAnswersCount; i < mparams.Length; i++)
        {
            wrong.Add(mparams[i]);
        }

        ClientData.PersonDataExtensions.Right = right.ToArray();
        ClientData.PersonDataExtensions.Wrong = wrong.ToArray();
        ClientData.PersonDataExtensions.ShowExtraRightButtons = mparams[4] == "+";

        ClientData.Hint = LO[nameof(R.HintCheckAnswer)];
        ClientData.DialogMode = DialogModes.AnswerValidation;
        ((PersonAccount)ClientData.Me).IsDeciding = false;

        _logic.IsRight();
    }

    private void ClearSelections(bool full = false) => _logic.ClearSelections(full);
}
