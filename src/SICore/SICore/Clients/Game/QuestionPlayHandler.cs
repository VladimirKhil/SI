using SIEngine.Core;
using SIPackages;
using SIPackages.Core;
using System.Xml.Linq;

namespace SICore.Clients.Game;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class QuestionPlayHandler : IQuestionEnginePlayHandler
{
    private readonly GameData _gameData;

    public GameLogic? GameLogic { get; set; }

    public QuestionPlayHandler(GameData gameData) => _gameData = gameData;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
    {
        if (GameLogic == null)
        {
            return false;
        }

        _gameData.QuestionPlayState.AnswerOptions = answerOptions;
        _gameData.QuestionPlayState.ScreenContentSequence = screenContentSequence;
        return false;
    }

    public bool OnAccept()
    {
        GameLogic?.AcceptQuestion();
        return true;
    }

    public void OnAskAnswer(string mode)
    {
        if (GameLogic == null)
        {
            return;
        }

        if (_gameData.QuestionPlayState.AnswerOptions != null && !_gameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            _gameData.QuestionPlayState.LayoutShown = true;
        }

        if (_gameData.QuestionPlayState.AnswerOptions != null && !_gameData.QuestionPlayState.AnswerOptionsShown)
        {
            GameLogic.ShowAnswerOptions(() => OnAskAnswer(mode));
            _gameData.QuestionPlayState.AnswerOptionsShown = true;
            return;
        }

        _gameData.IsQuestionFinished = true;
        _gameData.IsPlayingMedia = false;
        _gameData.IsPlayingMediaPaused = false;
        _gameData.AnswerMode = mode;

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
        if (GameLogic == null)
        {
            return;
        }

        GameLogic.AddHistory("Appellation opened");

        _gameData.IsAnswer = true;
        _gameData.AppellationOpened = _gameData.Settings.AppSettings.UseApellations;
        _gameData.PendingApellation = false;
        _gameData.IsPlayingMedia = false;
    }

    public bool OnButtonPressStart()
    {
        if (GameLogic == null)
        {
            return false;
        }

        // TODO: merge somehow with GameLogic.AskToPress() and OnAskAnswer() for buttons
        if (_gameData.QuestionPlayState.AnswerOptions != null && !_gameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            _gameData.QuestionPlayState.LayoutShown = true;
        }

        if (_gameData.QuestionPlayState.AnswerOptions != null && !_gameData.QuestionPlayState.AnswerOptionsShown)
        {
            GameLogic.ShowAnswerOptions(null);
            _gameData.QuestionPlayState.AnswerOptionsShown = true;
            return true;
        }

        _gameData.AnswerMode = StepParameterValues.AskAnswerMode_Button;
        GameLogic.OnButtonPressStart();
        return false;
    }

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
    {
        if (GameLogic == null)
        {
            return;
        }

        if (_gameData.QuestionPlayState.AnswerOptions != null && !_gameData.QuestionPlayState.LayoutShown)
        {
            GameLogic.OnAnswerOptions();
            _gameData.QuestionPlayState.LayoutShown = true;
        }

        if (_gameData.IsAnswer && !_gameData.IsAnswerSimple)
        {
            GameLogic?.OnComplexAnswer();
            _gameData.IsAnswer = false;
        }
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        if (GameLogic == null)
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
        if (GameLogic == null)
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
        if (GameLogic == null)
        {
            return;
        }

        switch (contentItem.Placement)
        {
            case ContentPlacements.Screen:
                switch (contentItem.Type)
                {
                    case ContentTypes.Text:
                        if (_gameData.IsAnswerSimple)
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

    // TODO: think about merging with GameLogic.InitQuestionState() and QuestionPlayState.Clear()
    public void OnQuestionStart(bool questionRequiresButtons)
    {
        if (GameLogic == null)
        {
            return;
        }

        _gameData.CanMarkQuestion = true;
        _gameData.IsDeferringAnswer = false;
        _gameData.IsQuestionFinished = false;
        _gameData.IsAnswer = false;
        _gameData.IsAnswerSimple = false;
        _gameData.UseBackgroundAudio = false;

        foreach (var player in _gameData.Players)
        {
            player.CanPress = questionRequiresButtons; // Always set true if there will be more complex question types
        }
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        // Enable this if there will be more complex question types
        //foreach (var player in _gameData.Players)
        //{
        //    player.CanPress = false;
        //}

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

    public void OnSimpleRightAnswerStart() => _gameData.IsAnswerSimple = true;

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
