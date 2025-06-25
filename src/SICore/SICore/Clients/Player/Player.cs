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
    public override GameRole Role => GameRole.Player;

    /// <summary>
    /// Initializes a new instance of <see cref="Player" /> class.
    /// </summary>
    /// <param name="client">Player game network client.</param>
    /// <param name="personData">Player account data.</param>
    /// <param name="logic">Player logic.</param>
    /// <param name="viewerActions">Player actions.</param>
    /// <param name="data">Player game data.</param>
    public Player(
        Client client,
        Account personData,
        IPersonController logic,
        ViewerActions viewerActions,
        ViewerData data)
        : base(client, personData, logic, viewerActions, data)
    { }

    protected override async ValueTask OnSystemMessageReceivedAsync(string[] mparams)
    {
        await base.OnSystemMessageReceivedAsync(mparams);
        
        switch (mparams[0])
        {
            case Messages.Stage:
                #region STAGE

                if (mparams.Length == 0)
                {
                    break;
                }

                if (mparams[1] == nameof(GameStage.Round))
                {
                    ClientData.QuestionIndex = -1;
                    ClientData.ThemeIndex = -1;

                    Logic.ClearSelections(true);
                }

                #endregion
                break;

            case Messages.Cancel:
                Logic.ClearSelections(true);
                break;

            case Messages.Choose:
                #region Choose

                if (mparams[1] == "1")
                {
                    Logic.SelectQuestion();
                }
                else
                {
                    Logic.DeleteTheme();
                }

                #endregion
                break;

            case Messages.Choice:
                Logic.OnQuestionSelected();
                break;

            case Messages.Theme:
            case Messages.ThemeInfo:
                ClientData.QuestionIndex = -1;
                Logic.OnTheme(mparams);
                break;

            case Messages.Question:
                ClientData.QuestionIndex++;
                break;

            case Messages.Content:
                if (ClientData.QuestionType == QuestionTypes.Simple)
                {
                    Logic.OnEnableButton();
                }
                break;

            case Messages.Try:
                ClientData.TryStartTime = DateTimeOffset.UtcNow;
                Logic.OnCanPressButton();
                break;

            case Messages.YouTry:
                Logic.OnEnableButton();
                Logic.StartThink();
                break;

            case Messages.EndTry:
                Logic.OnDisableButton();

                if (mparams[1] == MessageParams.EndTry_All)
                {
                    Logic.EndThink();
                }
                break;

            case Messages.Answer:
                Logic.Answer();
                break;

            case Messages.AskSelectPlayer:
                OnAskSelectPlayer(mparams);
                break;

            case Messages.AskStake:
                OnAskStake(mparams);
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

                Logic.OnPlayerOutcome(playerIndex, isRight);
                break;

            case Messages.Report:
                var report = new StringBuilder();

                for (var r = 1; r < mparams.Length; r++)
                {
                    report.AppendLine(mparams[r]);
                }

                var me = (PersonAccount?)ClientData.Me;

                if (me != null)
                {
                    me.IsDeciding = false;
                }

                Logic.Report(report.ToString());
                break;
        }
    }

    private void OnValidation2(string[] mparams)
    {
        if (mparams.Length < 7)
        {
            return;
        }

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

        ClientData.Right = right.ToArray();
        ClientData.Wrong = wrong.ToArray();
        ClientData.ShowExtraRightButtons = mparams[4] == "+";

        var me = (PersonAccount?)ClientData.Me;

        if (me != null)
        {
            me.IsDeciding = false;
        }

        Logic.IsRight(mparams[1], mparams[3] == "+", mparams[2]);
    }
}
