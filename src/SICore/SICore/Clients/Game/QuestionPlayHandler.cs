using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SICore.Clients.Game;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class QuestionPlayHandler : IQuestionEnginePlayHandler
{
    public GameLogic? GameLogic { get; set; }

    private GameData? GameData => GameLogic?.ClientData;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
    {
        if (GameLogic == null || GameData == null)
        {
            return false;
        }

        GameData.QuestionPlayState.AnswerOptions = answerOptions;
        GameData.QuestionPlayState.ScreenContentSequence = screenContentSequence;
        return false;
    }

    public bool OnAccept()
    {
        GameLogic?.AcceptQuestion();
        return true;
    }

    public void OnAskAnswer(string mode)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        if (GameData.QuestionPlayState.AnswerOptions != null && !GameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            GameData.QuestionPlayState.LayoutShown = true;
        }

        if (GameData.QuestionPlayState.AnswerOptions != null && !GameData.QuestionPlayState.AnswerOptionsShown)
        {
            GameLogic.ShowAnswerOptions(() => OnAskAnswer(mode));
            GameData.QuestionPlayState.AnswerOptionsShown = true;
            return;
        }

        GameData.IsQuestionFinished = true;
        GameData.IsPlayingMedia = false;
        GameData.IsPlayingMediaPaused = false;
        GameData.AnswerMode = mode;

        switch (mode)
        {
            case StepParameterValues.AskAnswerMode_Button:
                GameLogic.AskToPress();
                break;

            case StepParameterValues.AskAnswerMode_Direct:
                GameLogic.AskDirectAnswer();
                break;

            default:
                GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
                break;
        }
    }

    public void OnAnswerStart()
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        GameLogic.AddHistory("Appellation opened");

        GameData.IsAnswer = true;
        GameData.AppellationOpened = GameData.Settings.AppSettings.UseApellations;
        GameData.PendingApellation = false;
        GameData.IsPlayingMedia = false;
    }

    public bool OnButtonPressStart()
    {
        if (GameLogic == null || GameData == null)
        {
            return false;
        }

        // TODO: merge somehow with GameLogic.AskToPress() and OnAskAnswer() for buttons
        if (GameData.QuestionPlayState.AnswerOptions != null && !GameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            GameData.QuestionPlayState.LayoutShown = true;
        }

        if (GameData.QuestionPlayState.AnswerOptions != null && !GameData.QuestionPlayState.AnswerOptionsShown)
        {
            GameLogic.ShowAnswerOptions(null);
            GameData.QuestionPlayState.AnswerOptionsShown = true;
            return true;
        }

        GameData.AnswerMode = StepParameterValues.AskAnswerMode_Button;
        GameLogic.OnButtonPressStart();
        return false;
    }

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        if (GameData.QuestionPlayState.AnswerOptions != null && !GameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            GameData.QuestionPlayState.LayoutShown = true;
        }

        if (GameData.IsAnswer && !GameData.IsAnswerSimple)
        {
            GameLogic?.OnComplexAnswer();
            GameData.IsAnswer = false;
        }
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        if (content.Count == 0)
        {
            GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
            return;
        }

        if (content.Count == 1)
        {
            OnQuestionContentItem(content.First());
            return;
        }

        OnQuestionComplexContent(content);
    }

    private void OnQuestionComplexContent(IReadOnlyCollection<ContentItem> content)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        var contentTable = new Dictionary<string, List<ContentItem>>();

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case ContentTypes.Text:
                        case ContentTypes.Image:
                        case ContentTypes.Video:
                        case ContentTypes.Html:
                            break;

                        default:
                            continue;
                    }
                    break;

                case ContentPlacements.Replic:
                    if (contentItem.Type != ContentTypes.Text)
                    {
                        continue;
                    }
                    break;

                case ContentPlacements.Background:
                    if (contentItem.Type != ContentTypes.Audio)
                    {
                        continue;
                    }
                    break;

                default:
                    continue;
            }

            if (!contentTable.TryGetValue(contentItem.Placement, out var contentList))
            {
                contentList = contentTable[contentItem.Placement] = new List<ContentItem>();
            }

            contentList.Add(contentItem);
        }

        GameLogic.OnComplexContent(contentTable);
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        switch (contentItem.Placement)
        {
            case ContentPlacements.Screen:
                switch (contentItem.Type)
                {
                    case ContentTypes.Text:
                        if (GameData.IsAnswerSimple)
                        {
                            GameLogic.OnSimpleAnswer(contentItem.Value);
                            break;
                        }

                        GameLogic.OnContentScreenText(contentItem.Value, contentItem.WaitForFinish, contentItem.Duration);
                        break;

                    case ContentTypes.Image:
                        GameLogic.OnContentScreenImage(contentItem);
                        break;

                    case ContentTypes.Video:
                        GameLogic.OnContentScreenVideo(contentItem);
                        break;

                    case ContentTypes.Html:
                        GameLogic.OnContentScreenHtml(contentItem);
                        break;

                    default:
                        GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
                        break;
                }
                break;

            case ContentPlacements.Replic:
                if (contentItem.Type == ContentTypes.Text)
                {
                    GameLogic.OnContentReplicText(contentItem.Value, contentItem.WaitForFinish, contentItem.Duration);
                }
                else
                {
                    GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
                }
                break;

            case ContentPlacements.Background:
                if (contentItem.Type == ContentTypes.Audio)
                {
                    GameLogic.OnContentBackgroundAudio(contentItem);
                }
                else
                {
                    GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
                }
                break;

            default:
                GameLogic.ScheduleExecution(Tasks.MoveNext, 1);
                break;
        }
    }

    public void OnQuestionStart(bool questionRequiresButtons)
    {
        if (GameLogic == null || GameData == null)
        {
            return;
        }

        GameData.CanMarkQuestion = true;
        GameData.IsDeferringAnswer = false;
        GameData.IsQuestionFinished = false;
        GameData.IsAnswer = false;
        GameData.IsAnswerSimple = false;
        GameData.UseBackgroundAudio = false;
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        switch (mode)
        {
            case StepParameterValues.SetAnswererMode_ByCurrent:
                GameLogic?.SetAnswererByActive(select == StepParameterValues.SetAnswererSelect_Any);
                return true;

            case StepParameterValues.SetAnswererMode_Current:
                GameLogic?.SetAnswererAsActive();
                return true;

            case StepParameterValues.SetAnswererMode_All:
                GameLogic?.SetAnswerersAll();
                return true;

            case StepParameterValues.SetAnswererMode_Stake:
                switch (select)
                {
                    case StepParameterValues.SetAnswererSelect_Highest:
                        switch (stakeVisibility)
                        {
                            case StepParameterValues.SetAnswererStakeVisibility_Visible:
                                GameLogic?.SetAnswererByHighestVisibleStake();
                                return true;

                            default:
                                return false;
                        }

                    case StepParameterValues.SetAnswererSelect_AllPossible:
                        switch (stakeVisibility)
                        {
                            case StepParameterValues.SetAnswererStakeVisibility_Hidden:
                                GameLogic?.SetAnswerersByAllHiddenStakes();
                                return true;

                            default:
                                return false;
                        }

                    default:
                        return false;
                }

            default:
                return false;
        }
    }

    public bool OnAnnouncePrice(NumberSet? availableRange)
    {
        if (availableRange == null)
        {
            return false;
        }

        GameLogic?.OnAnnouncePrice(availableRange);
        return true;
    }

    public bool OnSetPrice(string mode, NumberSet? availableRange)
    {
        switch (mode)
        {
            case StepParameterValues.SetPriceMode_Select:
                if (availableRange == null)
                {
                    return false;
                }

                GameLogic?.OnSelectPrice(availableRange);
                return true;

            case StepParameterValues.SetPriceMode_NoRisk:
                GameLogic?.OnSetNoRiskPrice();
                return true;

            default:
                return false;
        }
    }

    public bool OnSetTheme(string themeName)
    {
        GameLogic?.OnSetTheme(themeName);
        return true;
    }

    public void OnSimpleRightAnswerStart()
    {
        if (GameData == null)
        {
            return;
        }

        GameData.IsAnswerSimple = true;
    }

    public bool OnRightAnswerOption(string rightOptionLabel)
    {
        if (GameLogic == null)
        {
            return false;
        }

        GameLogic.OnRightAnswerOption(rightOptionLabel);
        return true;
    }
}
