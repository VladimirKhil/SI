using SICore.BusinessLogic;
using SICore.Models;
using SICore.Network.Clients;
using SIData;

namespace SICore;

/// <summary>
/// Defines a showman message processor.
/// </summary>
public sealed class Showman : Viewer
{
    private readonly object _readyLock = new();

    public override GameRole Role => GameRole.Showman;

    public Showman(Client client, Account personData, bool isHost, IViewerLogic logic, ViewerActions viewerActions, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, logic, viewerActions, localizer, data)
    {
        ClientData.PersonDataExtensions.IsRight = new CustomCommand(arg =>
        {
            _viewerActions.SendMessage(Messages.IsRight, "+", arg?.ToString() ?? "1");
            ClearSelections();
        });

        ClientData.PersonDataExtensions.IsWrong = new CustomCommand(arg =>
        {
            _viewerActions.SendMessage(Messages.IsRight, "-", arg?.ToString() ?? "1");
            ClearSelections();
        });

        ClientData.AutoReadyChanged += ClientData_AutoReadyChanged;

        ClientData.PersonDataExtensions.AreAnswersShown = data.Host.AreAnswersShown;
        ClientData.PropertyChanged += ClientData_PropertyChanged;

        ClientData.PersonDataExtensions.SendCatCost = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.CatCost, ClientData.PersonDataExtensions.StakeInfo.Stake);
            ClearSelections();
        });
    }

    private void ClientData_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PersonData.AreAnswersShown))
        {
            ClientData.Host.AreAnswersShown = ClientData.PersonDataExtensions.AreAnswersShown;
        }
    }

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
            _logic.ShowmanLogic.OnInitialized();

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

                case Messages.AskSelectPlayer: // Uncomment later
                    //OnAskSelectPlayer(mparams);
                    //_logic.SelectPlayer();
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

                        _logic.FirstStake();
                        break;

                        #endregion
                    }

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

                        _logic.FirstDelete();
                        break;

                        #endregion
                    }
                case Messages.Hint:
                    {
                        if (mparams.Length < 2)
                        {
                            break;
                        }

                        _logic.OnHint(mparams[1]);
                        break;
                    }
                case Messages.Stage:
                    {
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
                        _logic.SelectQuestion();
                    }
                    else
                    {
                        _logic.ShowmanLogic.ChooseFinalTheme();
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

                    _logic.ShowmanLogic.Cat();
                    break;

                case Messages.AskStake: // Uncomment later
                    //OnAskStake(mparams);
                    //_logic.StakeNew();
                    break;

                case Messages.CatCost:
                    ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                    {
                        Minimum = int.Parse(mparams[1]),
                        Maximum = int.Parse(mparams[2]),
                        Step = int.Parse(mparams[3]),
                        Stake = int.Parse(mparams[1])
                    };

                    _logic.ShowmanLogic.CatCost();
                    break;

                case Messages.Stake:
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

                    _logic.ShowmanLogic.Stake();
                    break;

                case Messages.Stake2:
                    if (mparams.Length < 6
                        || !Enum.TryParse<StakeTypes>(mparams[1], out var stakeTypes)
                        || !int.TryParse(mparams[2], out var minimumStake)
                        || !int.TryParse(mparams[3], out var step)
                        || !int.TryParse(mparams[4], out var maximumStake))
                    {
                        break;
                    }

                    ClientData.PersonDataExtensions.Var[0] = stakeTypes.HasFlag(StakeTypes.Nominal);
                    ClientData.PersonDataExtensions.Var[1] = stakeTypes.HasFlag(StakeTypes.Stake);
                    ClientData.PersonDataExtensions.Var[2] = stakeTypes.HasFlag(StakeTypes.Pass);
                    ClientData.PersonDataExtensions.Var[3] = stakeTypes.HasFlag(StakeTypes.AllIn);

                    ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                    {
                        Minimum = minimumStake,
                        Maximum = maximumStake,
                        Step = step,
                        Stake = minimumStake,
                        PlayerName = mparams[5],
                    };

                    _logic.ShowmanLogic.Stake();
                    break;

                case Messages.Answer:
                    _logic.Answer();
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join(Message.ArgsSeparator, mparams), exc);
        }
    }

    private void OnValidation2(string[] mparams)
    {
        ClientData.PersonDataExtensions.ValidatorName = mparams[1];
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

        ((PersonAccount)ClientData.Me).IsDeciding = false;

        _logic.IsRight(true, mparams[2]);
    }

    private void ClearSelections(bool full = false) => _logic.ClearSelections(full);
}
