using SIEngine;
using SIEngine.Core;
using SIEngine.Models;
using SIEngine.Rules;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;

namespace SImulator.ViewModel.Controllers;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class GameEngineController : IQuestionEnginePlayHandler, ISIEnginePlayHandler
{
    /// <summary>
    /// Relative media content group weight on screen.
    /// </summary>
    private const double MediaContentGroupWeight = 5.0;

    public GameViewModel? GameViewModel { get; set; }

    public IPresentationController PresentationController => GameViewModel!.PresentationController;

    private readonly IMediaProvider _mediaProvider;

    private bool? _optionsShown = null;

    public GameEngineController(IMediaProvider mediaProvider) => _mediaProvider = mediaProvider;

    public bool OnAnswerOptions(AnswerOption[] answerOptions, IReadOnlyList<ContentItem[]> screenContentSequence)
    {
        var options = new List<ItemViewModel>();

        foreach (var (label, content) in answerOptions)
        {
            switch (content.Type)
            {
                case ContentTypes.Text:
                    options.Add(new ItemViewModel { Label = label, Content = new ContentViewModel(ContentType.Text, content.Value) });
                    break;

                case ContentTypes.Image:
                    var imageUri = TryGetMediaUri(content);

                    if (imageUri != null)
                    {
                        options.Add(new ItemViewModel
                        {
                            Label = label, 
                            Content = new ContentViewModel(ContentType.Image, imageUri, MediaContentGroupWeight)
                        });
                    }
                    else
                    {
                        options.Add(new ItemViewModel { Label = label, Content = new ContentViewModel(ContentType.Void, "", MediaContentGroupWeight) });
                    }
                    break;

                default:
                    options.Add(new ItemViewModel { Label = label, Content = new ContentViewModel(ContentType.Void, "") });
                    break;
            }
        }

        var optionsArray = options.ToArray();

        if (GameViewModel != null)
        {
            GameViewModel.LocalInfo.LayoutMode = LayoutMode.AnswerOptions;
            GameViewModel.LocalInfo.AnswerOptions.Options = optionsArray;
            _optionsShown = false;
        }

        PresentationController.SetAnswerOptions(optionsArray);
        return false;
    }

    public bool OnAccept()
    {
        GameViewModel?.Accept();
        return false;
    }

    public void OnAskAnswer(string mode)
    {
        if (GameViewModel == null)
        {
            return;
        }

        if (_optionsShown.HasValue && !_optionsShown.Value)
        {
            GameViewModel.Continuation = () =>
            {
                OnAskAnswer(mode);
                return true;
            };

            PresentationController.ShowAnswerOptions();
            _optionsShown = true;
            return;
        }

        // Disabled for run timer command so in false start mode when host enables buttons before media finishes it is still possible to control media being played
        // Consider later will this lead to playing issues
        if (GameViewModel.ActiveMediaCommand == GameViewModel.StopMediaTimer)
        {
            GameViewModel.ActiveMediaCommand = null;
        }

        switch (mode)
        {
            case StepParameterValues.AskAnswerMode_Button:
                GameViewModel.StopThinkingTimer_Executed(0);
                GameViewModel.StartQuestionTimer();
                GameViewModel.AskAnswerButton();
                break;

            case StepParameterValues.AskAnswerMode_Direct:
                GameViewModel.AskAnswerDirect();
                GameViewModel.State = QuestionState.Thinking;
                break;

            default:
                break;
        }
    }

    public bool OnButtonPressStart()
    {
        GameViewModel?.AskAnswerButton();
        return false;
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        if (GameViewModel == null)
        {
            return;
        }

        var textToShow =
            GameViewModel.Settings.Model.FalseStart || GameViewModel.Settings.Model.ShowTextNoFalstart
            ? null
            : $"{GameViewModel.CurrentTheme}\n{GameViewModel.Price}";

        if (GameViewModel.Settings.Model.OralText)
        {
            // Make all text content items to be replicas
            content = content.Select(item =>
            {
                if (item.Type == ContentTypes.Text)
                {
                    return new ContentItem
                    {
                        Type = ContentTypes.Text,
                        Value = item.Value,
                        Duration = item.Duration,
                        WaitForFinish = item.WaitForFinish,
                        Placement = ContentPlacements.Replic,
                        IsRef = item.IsRef
                    };
                }

                return item;
            }).ToList();
        }

        var hasMedia = PresentationController.OnQuestionContent(content, TryGetMediaUri, textToShow);

        if (hasMedia)
        {
            GameViewModel.InitMedia();
        }

        GameViewModel.ActiveContent = content;

        var moveToContent = GameViewModel.MoveToContent;
        moveToContent.ExecutionContext.Clear();

        var canMoveToContent = true;

        foreach (var item in content)
        {
            if (canMoveToContent)
            {
                moveToContent.ExecutionContext.Add(item);
            }

            canMoveToContent = item.WaitForFinish;
        }

        moveToContent.OnCanBeExecutedChanged();
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        if (GameViewModel == null)
        {
            return false;
        }

        GameViewModel.IsCommonPrice = true;
        GameViewModel.QuestionForAll = false;

        switch (mode)
        {
            case StepParameterValues.SetAnswererMode_Stake
                when select == StepParameterValues.SetAnswererSelect_Highest
                    && stakeVisibility == StepParameterValues.SetAnswererStakeVisibility_Visible:
                return GameViewModel.OnSetAnswererByHighestStake();
            
            case StepParameterValues.SetAnswererMode_Stake
                when select == StepParameterValues.SetAnswererSelect_AllPossible
                    && stakeVisibility == StepParameterValues.SetAnswererStakeVisibility_Hidden:
                GameViewModel.IsCommonPrice = false;
                GameViewModel.QuestionForAll = true;
                return GameViewModel.OnAskHiddenStakes();
            
            case StepParameterValues.SetAnswererMode_ByCurrent:
                return GameViewModel.OnSetAnswererDirectly(select == StepParameterValues.SetAnswererSelect_Any);
            
            case StepParameterValues.SetAnswererMode_Current:
                return GameViewModel.OnSetAnswererAsCurrent();
            
            case StepParameterValues.SetAnswererMode_All:
                GameViewModel.QuestionForAll = true;
                break;
        }

        return false;
    }

    public bool OnAnnouncePrice(NumberSet availableRange) => false;

    public bool OnSetPrice(string mode, NumberSet? availableRange)
    {
        if (GameViewModel == null)
        {
            return false;
        }

        switch (mode)
        {
            case StepParameterValues.SetPriceMode_Select:
                if (availableRange != null)
                {
                    if (availableRange.Maximum == availableRange.Minimum && availableRange.Minimum > 0)
                    {
                        GameViewModel.Price = availableRange.Maximum;
                    }
                    else
                    {
                        return GameViewModel.SelectStake(availableRange);
                    }
                }
                break;

            case StepParameterValues.SetPriceMode_Multiply:
                GameViewModel.OnSetMultiplyPrice();
                break;

            default:
                // Do nothing
                break;
        }

        return false;
    }

    public bool OnSetTheme(string themeName)
    {
        if (GameViewModel != null)
        {
            GameViewModel.CurrentTheme = themeName;
        }
        
        return false;
    }

    private string? TryGetMediaUri(ContentItem contentItem)
    {
        var media = _mediaProvider.TryGetMedia(contentItem);
        return media?.Uri?.OriginalString;
    }

    public void OnQuestionStart(bool buttonsRequired, Action skipQuestionCallback)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.OnQuestionStart();

        if (buttonsRequired)
        {
            GameViewModel.StartButtons(); // Buttons are activated in advance for false starts to work
        }

        _optionsShown = null;
        PresentationController.OnQuestionStart();
    }

    public void OnAnswerStart()
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.State = QuestionState.Normal;
        GameViewModel.OnRightAnswer();
        PresentationController.OnAnswerStart();
    }

    public void OnContentStart(IReadOnlyList<ContentItem> contentItems, Action<int> moveToContentCallback)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.OnContentStart();
        PresentationController.OnContentStart();

        GameViewModel.ContentItems = contentItems;
        GameViewModel.ActiveMediaCommand = null;
        GameViewModel.DecisionMode = DecisionMode.None;
        GameViewModel.MoveToContentCallback = moveToContentCallback;
    }

    public void OnSimpleRightAnswerStart()
    {
        if (GameViewModel == null)
        {
            return;
        }

        PresentationController.SetSimpleAnswer();
        GameViewModel.OnRightAnswer();
    }

    public bool OnRightAnswerOption(string rightOptionLabel)
    {
        if (GameViewModel == null)
        {
            return false;
        }

        PresentationController.SetQuestionSound(false);
        PresentationController.SetSound();
        GameViewModel.OnRightAnswer();

        var answerOptions = GameViewModel.LocalInfo.AnswerOptions.Options;

        for (var answerIndex = 0; answerIndex < answerOptions.Length; answerIndex++)
        {
            if (answerOptions[answerIndex].Label == rightOptionLabel)
            {
                answerOptions[answerIndex].State = ItemState.Right;
                PresentationController.SetAnswerState(answerIndex, ItemState.Right);
                break;
            }
            else if (answerOptions[answerIndex].State == ItemState.Active)
            {
                answerOptions[answerIndex].State = ItemState.Normal;
            }
        }

        return true;
    }

    public bool ShouldPlayRoundWithRemovableThemes() => true;

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController) => GameViewModel?.OnRoundThemes(themes);

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback)
    {
        if (GameViewModel == null)
        {
            return;
        }

        PresentationController.SelectionCallback = selectCallback;
        PresentationController.SetRoundTable();
        GameViewModel.LocalInfo.TStage = TableStage.RoundTable;
    }

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameViewModel?.OnQuestionSelected(themeIndex, questionIndex);

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay) => GameViewModel?.OnFinalThemes(themes);

    public void AskForThemeDelete(Action<int> deleteCallback) => PresentationController.DeletionCallback = deleteCallback;

    public void OnThemeDeleted(int themeIndex) => GameViewModel?.OnThemeDeleted(themeIndex);

    public void OnThemeSelected(int themeIndex, int questionIndex) => GameViewModel?.OnThemeSelected(themeIndex, questionIndex);

    public void OnTheme(Theme theme) => GameViewModel?.OnTheme(theme);

    public void OnQuestion(Question question) => GameViewModel?.OnQuestion(question);

    public void OnRound(Round round, QuestionSelectionStrategyType strategyType) => GameViewModel?.OnRound(round, strategyType);

    public void OnRoundEnd(RoundEndReason reason)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.LogScore();

        switch (reason)
        {
            case RoundEndReason.Timeout:
                GameViewModel.OnEndRoundTimeout();
                break;

            default:
                PresentationController.SetSound();
                break;
        }

        GameViewModel.OnEndRound();
    }

    public void OnRoundSkip(QuestionSelectionStrategyType strategyType)
    {
        
    }

    public void OnQuestionRestored(int themeIndex, int questionIndex, int price)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.LocalInfo.RoundInfo[themeIndex].Questions[questionIndex].Price = price;
        PresentationController.RestoreQuestion(themeIndex, questionIndex, price);
    }

    public void OnQuestionType(string typeName, bool isDefault) => GameViewModel?.PlayQuestionType(typeName, isDefault);

    public bool OnQuestionEnd() => GameViewModel == null || GameViewModel.OnQuestionEnd();

    public void OnPackage(Package package) => GameViewModel?.OnPackage(package);

    public void OnPackageEnd() => GameViewModel?.OnEndGame();

    public void OnGameThemes(IEnumerable<string> themes) => GameViewModel?.OnGameThemes(themes);
}
