using SICore.BusinessLogic;
using SICore.Models;
using SICore.Network.Clients;
using SIData;
using SIPackages.Core;
using System.Text;

namespace SICore;

/// <summary>
/// Represents a game player.
/// </summary>
public sealed class Player : Viewer
{
    private readonly object _readyLock = new();

    public override GameRole Role => GameRole.Player;

    /// <summary>
    /// Initializes a new instance of <see cref="Player" /> class.
    /// </summary>
    /// <param name="client">Player game network client.</param>
    /// <param name="personData">Player account data.</param>
    /// <param name="isHost">Is the player a game host.</param>
    /// <param name="logic">Player logic.</param>
    /// <param name="viewerActions">Player actions.</param>
    /// <param name="localizer">Resource localizer.</param>
    /// <param name="data">Player game data.</param>
    public Player(Client client, Account personData, bool isHost, IViewerLogic logic, ViewerActions viewerActions, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, logic, viewerActions, localizer, data)
    {       
        ClientData.PersonDataExtensions.SendCatCost = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.CatCost, ClientData.PersonDataExtensions.StakeInfo.Stake);
            Clear();
        });

        ClientData.PersonDataExtensions.SendFinalStake = new CustomCommand(arg =>
        {
            _viewerActions.SendMessageWithArgs(Messages.FinalStake, ClientData.PersonDataExtensions.StakeInfo.Stake);
            Clear();
        });

        ClientData.AutoReadyChanged += ClientData_AutoReadyChanged;
    }

    protected override void Dispose(bool disposing)
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

    private void Clear() => _logic.ClearSelections(true);

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
                _logic.PlayerLogic.OnInitialized();

                if (ClientData.AutoReady)
                {
                    readyCommand.Execute(null);
                }
            }
        }
    }

    /// <summary>
    /// Получение системного сообщения
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

                    #endregion
                    break;

                case Messages.Cancel:
                    Clear();
                    break;

                case Messages.Choose:
                    #region Choose

                    if (mparams[1] == "1")
                    {
                        _logic.SelectQuestion();
                    }
                    else
                    {
                        _logic.PlayerLogic.ChooseFinalTheme();
                    }

                    #endregion
                    break;

                case Messages.Choice:
                    ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                    _logic.OnQuestionSelected();
                    break;

                case Messages.Theme:
                    ClientData.QuestionIndex = -1;
                    _logic.OnTheme(mparams);
                    break;

                case Messages.Question:
                    ClientData.QuestionIndex++;
                    ClientData.PlayerDataExtensions.IsQuestionInProgress = true;
                    break;

                case Messages.Content:
                    _logic.OnQuestionContent();

                    if (ClientData.QuestionType == QuestionTypes.Simple)
                    {
                        _logic.OnEnableButton();
                    }
                    break;

                case Messages.Try:
                    ClientData.PlayerDataExtensions.TryStartTime = DateTimeOffset.UtcNow;
                    _logic.OnCanPressButton();
                    break;

                case Messages.YouTry:
                    _logic.OnEnableButton();
                    _logic.StartThink();
                    break;

                case Messages.EndTry:
                    _logic.OnDisableButton();

                    if (mparams[1] == MessageParams.EndTry_All)
                    {
                        _logic.EndThink();
                    }
                    break;

                case Messages.Answer:
                    _logic.Answer();
                    break;

                case Messages.AskSelectPlayer: // Uncomment later
                    //OnAskSelectPlayer(mparams);
                    //_logic.SelectPlayer();
                    break;

                case Messages.Cat:
                    for (int i = 0; i < ClientData.Players.Count; i++)
                    {
                        ClientData.Players[i].CanBeSelected = mparams[i + 1] == "+";
                        int num = i;

                        ClientData.Players[i].SelectionCallback = player =>
                        {
                            _viewerActions.SendMessageWithArgs(Messages.Cat, num);
                            Clear();
                        };
                    }

                    _logic.PlayerLogic.Cat();
                    break;

                case Messages.AskStake: // Uncomment later
                    //OnAskStake(mparams);
                    //_logic.PlayerLogic.StakeNew();
                    break;

                case Messages.CatCost:
                    ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                    {
                        Minimum = int.Parse(mparams[1]),
                        Maximum = int.Parse(mparams[2]),
                        Step = int.Parse(mparams[3]),
                        Stake = int.Parse(mparams[1])
                    };

                    _logic.PlayerLogic.CatCost();
                    break;

                case Messages.Stake:
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

                    _logic.PlayerLogic.Stake();
                    break;

                case Messages.Stake2:
                    if (mparams.Length < 4
                        || !Enum.TryParse<StakeTypes>(mparams[1], out var stakeTypes)
                        || !int.TryParse(mparams[2], out var minimumStake)
                        || !int.TryParse(mparams[3], out var step))
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
                        Maximum = ((PlayerAccount)ClientData.Me).Sum,
                        Step = step,
                        Stake = minimumStake
                    };

                    _logic.PlayerLogic.Stake();
                    break;

                case Messages.FinalStake:
                    ClientData.PersonDataExtensions.StakeInfo = new StakeInfo
                    {
                        Minimum = 1,
                        Maximum = ((PlayerAccount)ClientData.Me).Sum,
                        Step = 1,
                        Stake = 1
                    };

                    ((PlayerAccount)ClientData.Me).IsDeciding = false;

                    _logic.PlayerLogic.FinalStake();
                    break;

                case Messages.Validation2:
                    OnValidation2(mparams);
                    break;

                case Messages.Person:
                    if (mparams.Length < 4)
                    {
                        break;
                    }

                    var isRight = mparams[1] == "+";

                    if (!int.TryParse(mparams[2], out var playerIndex) ||
                        playerIndex < 0 ||
                        playerIndex >= ClientData.Players.Count)
                    {
                        break;
                    }

                    _logic.OnPlayerOutcome(playerIndex, isRight);
                    break;

                case Messages.Report:
                    var report = new StringBuilder();

                    for (var r = 1; r < mparams.Length; r++)
                    {
                        report.AppendLine(mparams[r]);
                    }

                    ((PlayerAccount)ClientData.Me).IsDeciding = false;
                    _logic.Report(report.ToString());
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join("\n", mparams), exc);
        }
    }

    private void OnValidation2(string[] mparams)
    {
        ClientData.PersonDataExtensions.ValidatorName = mparams[1];
        _ = int.TryParse(mparams[5], out var rightAnswersCount);
        rightAnswersCount = Math.Min(rightAnswersCount, mparams.Length - 6);

        var right = new List<string>();

        for (int i = 0; i < rightAnswersCount; i++)
        {
            right.Add(mparams[6 + i]);
        }

        var wrong = new List<string>();

        for (int i = 6 + rightAnswersCount; i < mparams.Length; i++)
        {
            wrong.Add(mparams[i]);
        }

        ClientData.PersonDataExtensions.Right = right.ToArray();
        ClientData.PersonDataExtensions.Wrong = wrong.ToArray();
        ClientData.PersonDataExtensions.ShowExtraRightButtons = mparams[4] == "+";

        ((PersonAccount)ClientData.Me).IsDeciding = false;
        _logic.IsRight(mparams[3] == "+", mparams[2]);
    }
}
