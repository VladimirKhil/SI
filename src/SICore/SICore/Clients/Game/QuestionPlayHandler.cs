﻿using SICore.Models;
using SIEngine.Core;
using SIPackages;
using SIPackages.Core;

namespace SICore.Clients.Game;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class QuestionPlayHandler : IQuestionEnginePlayHandler
{
    private readonly GameData _state;
    private GameLogic _controller = null!;

    public GameLogic GameLogic
    {
        get => _controller;
        set => _controller = value;
    }

    public QuestionPlayHandler(GameData state) => _state = state;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
    {
        _state.QuestionPlayState.AnswerOptions = answerOptions;
        _state.QuestionPlayState.ScreenContentSequence = screenContentSequence;
        return false;
    }

    public bool OnNumericAnswerType(int deviation)
    {
        _state.QuestionPlayState.IsNumericAnswer = true;
        _state.QuestionPlayState.NumericAnswerDeviation = deviation;
        _controller.OnNumericAnswer();
        return false;
    }

    public bool OnAccept()
    {
        _controller.AcceptQuestion();
        return true;
    }

    public void OnAskAnswer(string mode)
    {
        if (_state.QuestionPlayState.AnswerOptions != null && !_state.QuestionPlayState.LayoutShown)
        {
            _controller.OnAnswerOptions();
            _state.QuestionPlayState.LayoutShown = true;
        }

        if (_state.QuestionPlayState.AnswerOptions != null && !_state.QuestionPlayState.AnswerOptionsShown)
        {
            _controller.ShowAnswerOptions(() => OnAskAnswer(mode));
            _state.QuestionPlayState.AnswerOptionsShown = true;
            return;
        }

        _state.IsQuestionFinished = true;
        _state.IsPlayingMedia = false;
        _state.IsPlayingMediaPaused = false;
        _state.AnswerMode = mode;

        switch (mode)
        {
            case StepParameterValues.AskAnswerMode_Button:
                _controller.AskToPress();
                break;

            case StepParameterValues.AskAnswerMode_Direct:
                _controller.AskDirectAnswer();
                break;

            default:
                _controller.ScheduleExecution(Tasks.MoveNext, 1);
                break;
        }
    }

    public void OnAnswerStart()
    {
        _controller.AddHistory("Appellation opened");

        _state.QuestionPlayState.IsAnswer = true;
        _state.QuestionPlayState.AppellationState = _state.Settings.AppSettings.UseApellations ? AppellationState.Collecting : AppellationState.None;
        _state.IsPlayingMedia = false;
    }

    public bool OnButtonPressStart()
    {
        // TODO: merge somehow with GameLogic.AskToPress() and OnAskAnswer() for buttons
        if (_state.QuestionPlayState.AnswerOptions != null && !_state.QuestionPlayState.LayoutShown)
        {
            _controller.OnAnswerOptions();
            _state.QuestionPlayState.LayoutShown = true;
        }

        if (_state.QuestionPlayState.AnswerOptions != null && !_state.QuestionPlayState.AnswerOptionsShown)
        {
            _controller.ShowAnswerOptions(null);
            _state.QuestionPlayState.AnswerOptionsShown = true;
            return true;
        }

        _state.AnswerMode = StepParameterValues.AskAnswerMode_Button;
        _controller.OnButtonPressStart();
        return false;
    }

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
    {
        if (_state.QuestionPlayState.AnswerOptions != null && !_state.QuestionPlayState.LayoutShown)
        {
            _controller.OnAnswerOptions();
            _state.QuestionPlayState.LayoutShown = true;
        }

        if (_state.QuestionPlayState.IsAnswer && !_state.QuestionPlayState.IsAnswerSimple && !_state.QuestionPlayState.IsAnswerAnnounced)
        {
            _controller.OnComplexAnswer();
            _state.QuestionPlayState.IsAnswerAnnounced = true;
        }
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        if (content.Count == 0)
        {
            _controller.ScheduleExecution(Tasks.MoveNext, 1);
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

        _controller.OnComplexContent(contentTable);
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        switch (contentItem.Placement)
        {
            case ContentPlacements.Screen:
                switch (contentItem.Type)
                {
                    case ContentTypes.Text:
                        if (_state.QuestionPlayState.IsAnswerSimple)
                        {
                            _controller.OnSimpleAnswer(contentItem.Value);
                            break;
                        }

                        _controller.OnContentScreenText(contentItem.Value, contentItem.WaitForFinish, contentItem.Duration);
                        break;

                    case ContentTypes.Image:
                        _controller.OnContentScreenImage(contentItem);
                        break;

                    case ContentTypes.Video:
                        _controller.OnContentScreenVideo(contentItem);
                        break;

                    case ContentTypes.Html:
                        _controller.OnContentScreenHtml(contentItem);
                        break;

                    default:
                        _controller.ScheduleExecution(Tasks.MoveNext, 1);
                        break;
                }
                break;

            case ContentPlacements.Replic:
                if (contentItem.Type == ContentTypes.Text)
                {
                    _controller.OnContentReplicText(contentItem.Value, contentItem.WaitForFinish, contentItem.Duration);
                }
                else
                {
                    _controller.ScheduleExecution(Tasks.MoveNext, 1);
                }
                break;

            case ContentPlacements.Background:
                if (contentItem.Type == ContentTypes.Audio)
                {
                    _controller.OnContentBackgroundAudio(contentItem);
                }
                else
                {
                    _controller.ScheduleExecution(Tasks.MoveNext, 1);
                }
                break;

            default:
                _controller.ScheduleExecution(Tasks.MoveNext, 1);
                break;
        }
    }

    // TODO: think about merging with GameLogic.InitQuestionState() and QuestionPlayState.Clear()
    public void OnQuestionStart(bool questionRequiresButtons, Action skipQuestionCallback)
    {
        _state.SkipQuestion = skipQuestionCallback;
        _state.CanMarkQuestion = true;
        _state.IsDeferringAnswer = false;
        _state.IsQuestionFinished = false;
        _state.UseBackgroundAudio = false;
        _state.QuestionPlayState.UseButtons = questionRequiresButtons;

        foreach (var player in _state.Players)
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
                _controller.SetAnswererByActive(select == StepParameterValues.SetAnswererSelect_Any);
                return true;

            case StepParameterValues.SetAnswererMode_Current:
                _controller.SetAnswererAsActive();
                return true;

            case StepParameterValues.SetAnswererMode_All:
                _controller.SetAnswerersAll();
                return true;

            case StepParameterValues.SetAnswererMode_Stake:
                switch (select)
                {
                    case StepParameterValues.SetAnswererSelect_Highest:
                        switch (stakeVisibility)
                        {
                            case StepParameterValues.SetAnswererStakeVisibility_Visible:
                                _controller.SetAnswererByHighestVisibleStake();
                                return true;

                            default:
                                return false;
                        }

                    case StepParameterValues.SetAnswererSelect_AllPossible:
                        switch (stakeVisibility)
                        {
                            case StepParameterValues.SetAnswererStakeVisibility_Hidden:
                                _controller.SetAnswerersByAllHiddenStakes();
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

    public bool OnAnnouncePrice(NumberSet availableRange)
    {
        _controller.OnAnnouncePrice(availableRange);
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

                _controller.OnSelectPrice(availableRange);
                return true;

            case StepParameterValues.SetPriceMode_Multiply:
                _controller.OnMultiplyPrice();
                return true;

            default:
                return false;
        }
    }

    public bool OnSetTheme(string themeName)
    {
        _controller.OnSetTheme(themeName);
        return true;
    }

    public void OnSimpleRightAnswerStart() => _state.QuestionPlayState.IsAnswerSimple = true;

    public bool OnRightAnswerOption(string rightOptionLabel)
    {
        _controller.OnRightAnswerOption(rightOptionLabel);
        return true;
    }
}
