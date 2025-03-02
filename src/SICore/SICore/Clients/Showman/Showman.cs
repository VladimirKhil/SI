using SICore.Contracts;
using SICore.Network.Clients;
using SIData;

namespace SICore;

/// <summary>
/// Defines a showman message processor.
/// </summary>
public sealed class Showman : Viewer
{
    public override GameRole Role => GameRole.Showman;

    public Showman(Client client, Account personData, bool isHost, IViewerLogic logic, ViewerActions viewerActions, ILocalizer localizer, ViewerData data)
        : base(client, personData, isHost, logic, viewerActions, localizer, data)
    { }

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
                case Messages.Cancel:
                    Logic.ClearSelections(true);
                    break;

                case Messages.AskSelectPlayer:
                    OnAskSelectPlayer(mparams);
                    break;

                case Messages.AskValidate:
                    if (mparams.Length < 3
                        || !int.TryParse(mparams[1], out var playerIndex)
                        || playerIndex < 0
                        || playerIndex >= ClientData.Players.Count)
                    {
                        break;
                    }

                    Logic.ValidateAnswer(playerIndex, mparams[2]);
                    break;

                case Messages.Validation2:
                    OnValidation2(mparams);
                    break;

                case Messages.Hint:
                    if (mparams.Length < 2)
                    {
                        break;
                    }

                    Logic.OnHint(mparams[1]);
                    break;

                case Messages.QuestionAnswers:
                    OnQuestionAnswers(mparams);
                    break;

                case Messages.Stage:
                    for (var i = 0; i < ClientData.Players.Count; i++)
                    {
                        ClientData.Players[i].CanBeSelected = false;
                    }
                    break;

                // Oral game commands (showman performs actions announced by players)
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

                case Messages.AskStake:
                    OnAskStake(mparams);
                    break;

                case Messages.Answer:
                    Logic.Answer();
                    break;
            }
        }
        catch (Exception exc)
        {
            throw new Exception(string.Join(Message.ArgsSeparator, mparams), exc);
        }
    }

    private void OnQuestionAnswers(string[] mparams)
    {
        if (mparams.Length < 3)
        {
            return;
        }

        _ = int.TryParse(mparams[1], out var rightAnswersCount);
        rightAnswersCount = Math.Min(rightAnswersCount, mparams.Length - 2);

        var right = new List<string>();

        for (var i = 0; i < rightAnswersCount; i++)
        {
            right.Add(mparams[2 + i]);
        }

        var wrong = new List<string>();

        for (var i = 2 + rightAnswersCount; i < mparams.Length; i++)
        {
            wrong.Add(mparams[i]);
        }

        ClientData.PersonDataExtensions.Right = right.ToArray();
        ClientData.PersonDataExtensions.Wrong = wrong.ToArray();
    }

    private void OnValidation2(string[] mparams)
    {
        if (mparams.Length < 6)
        {
            return;
        }

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

        var me = (PersonAccount?)ClientData.Me;

        if (me != null)
        {
            me.IsDeciding = false;
        }

        Logic.IsRight(mparams[1], true, mparams[2]);
    }
}
