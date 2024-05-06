using SIEngine;
using SIEngine.Core;
using SImulator.ViewModel.Contracts;
using SImulator.ViewModel.Model;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Controllers;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class GameEngineController : IQuestionEnginePlayHandler, ISIEnginePlayHandler
{
    /// <summary>
    /// Minimum weight for the small content.
    /// </summary>
    private const double SmallContentWeight = 1.0;

    /// <summary>
    /// Relative media content group weight on screen.
    /// </summary>
    private const double MediaContentGroupWeight = 5.0;

    /// <summary>
    /// Length of text having weight of 1.
    /// </summary>
    private const int TextLengthWithBasicWeight = 80;

    public GameViewModel? GameViewModel { get; set; }

    public IPresentationController PresentationController => GameViewModel!.PresentationController;

    private readonly SIDocument _document;

    private bool? _optionsShown = null;

    public GameEngineController(SIDocument document) => _document = document;

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
                GameViewModel.StartQuestionTimer();
                GameViewModel.AskAnswerButton();
                break;

            case StepParameterValues.AskAnswerMode_Direct:
                GameViewModel.AskAnswerDirect();
                GameViewModel.State = Model.QuestionState.Thinking;
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

        var screenContent = new List<ContentGroup>();
        ContentGroup? currentGroup = null;

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case ContentTypes.Text:
                            if (currentGroup != null)
                            {
                                currentGroup.Init();
                                screenContent.Add(currentGroup);
                                currentGroup = null;
                            }

                            // Show theme name and question price instead of empty text
                            var displayedText =
                                GameViewModel.Settings.Model.FalseStart
                                    || GameViewModel.Settings.Model.ShowTextNoFalstart
                                    || GameViewModel.ActiveRound?.Type == RoundTypes.Final
                                ? contentItem.Value
                                : $"{GameViewModel.CurrentTheme}\n{GameViewModel.Price}";

                            PresentationController.SetText(displayedText); // For simple answer
                            
                            var groupWeight = Math.Max(
                                SmallContentWeight,
                                Math.Min(MediaContentGroupWeight, (double)displayedText.Length / TextLengthWithBasicWeight));

                            var group = new ContentGroup { Weight = groupWeight };
                            group.Content.Add(new ContentViewModel(ContentType.Text, displayedText));
                            screenContent.Add(group);
                            break;

                        case ContentTypes.Image:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var imageUri = TryGetMediaUri(contentItem);

                            if (imageUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Image, imageUri));
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        case ContentTypes.Video:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var videoUri = TryGetMediaUri(contentItem);

                            if (videoUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Video, videoUri));
                                PresentationController.SetSound();
                                GameViewModel.InitMedia();
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        case ContentTypes.Html:
                            currentGroup ??= new ContentGroup { Weight = MediaContentGroupWeight };
                            var htmlUri = TryGetMediaUri(contentItem);

                            if (htmlUri != null)
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Html, htmlUri));
                                PresentationController.SetQuestionSound(false);
                                PresentationController.SetSound();
                            }
                            else
                            {
                                currentGroup.Content.Add(new ContentViewModel(ContentType.Void, ""));
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case ContentPlacements.Replic:
                    if (contentItem.Type == ContentTypes.Text)
                    {
                        // Show nothing. The text should be read by showman
                    }
                    break;

                case ContentPlacements.Background:
                    if (contentItem.Type == ContentTypes.Audio)
                    {
                        PresentationController.SetQuestionSound(true);

                        var audioUri = TryGetMediaUri(contentItem);

                        PresentationController.SetSound();

                        if (audioUri != null)
                        {
                            PresentationController.SetMedia(new MediaSource(audioUri), true);
                            GameViewModel.InitMedia();
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        if (currentGroup != null)
        {
            currentGroup.Init();
            screenContent.Add(currentGroup);
        }

        if (screenContent.Any())
        {
            PresentationController.SetScreenContent(screenContent);
        }

        GameViewModel.ActiveContent = content;
    }

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        if (GameViewModel == null)
        {
            return false;
        }

        if (mode == StepParameterValues.SetAnswererMode_Stake
            && select == StepParameterValues.SetAnswererSelect_Highest
            && stakeVisibility == StepParameterValues.SetAnswererStakeVisibility_Visible)
        {
            return GameViewModel.OnSetAnswererByHighestStake();
        }
        else if (mode == StepParameterValues.SetAnswererMode_ByCurrent)
        {
            return GameViewModel.OnSetAnswererDirectly(select == StepParameterValues.SetAnswererSelect_Any);
        }
        else if (mode == StepParameterValues.SetAnswererMode_Current)
        {
            return GameViewModel.OnSetAnswererAsCurrent();
        }

        return false;
    }

    public bool OnAnnouncePrice(NumberSet? availableRange) => false;

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

            case StepParameterValues.SetPriceMode_NoRisk:
                GameViewModel.OnSetNoRiskPrice();
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
        var media = _document.TryGetMedia(contentItem);
        return media?.Uri?.OriginalString;
    }

    public void OnQuestionStart(bool buttonsRequired)
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
        PresentationController.SetSound();
    }

    public void OnContentStart(IEnumerable<ContentItem> contentItems)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.OnContentStart();
        PresentationController.SetQuestionSound(false);
        PresentationController.SetQuestionContentType(QuestionContentType.Void);
        PresentationController.SetStage(TableStage.Question);
        PresentationController.SetSound();

        GameViewModel.ContentItems = contentItems;
        GameViewModel.ActiveMediaCommand = null;
        GameViewModel.DecisionMode = DecisionMode.None;
    }

    public void OnSimpleRightAnswerStart()
    {
        if (GameViewModel == null)
        {
            return;
        }

        PresentationController.SetQuestionSound(false);
        PresentationController.SetQuestionContentType(QuestionContentType.Void);
        PresentationController.SetStage(TableStage.Answer);
        PresentationController.SetSound();
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

    public bool ShouldPlayQuestionForAll() => true;

    public void OnRoundThemes(IReadOnlyList<Theme> themes, IRoundTableController tableController) => GameViewModel?.OnRoundThemes(themes);

    public void AskForQuestionSelection(IReadOnlyCollection<(int, int)> options, Action<int, int> selectCallback) =>
        PresentationController.SelectionCallback = selectCallback;

    public void CancelQuestionSelection() => PresentationController.SelectionCallback = null;

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameViewModel?.OnQuestionSelected(themeIndex, questionIndex);

    public void OnFinalThemes(IReadOnlyList<Theme> themes, bool willPlayAllThemes, bool isFirstPlay) => GameViewModel?.OnFinalThemes(themes);

    public void AskForThemeDelete(Action<int> deleteCallback) =>
        PresentationController.DeletionCallback = deleteCallback;

    public void OnThemeDeleted(int themeIndex) => GameViewModel?.OnThemeDeleted(themeIndex);

    public void OnThemeSelected(int themeIndex) => GameViewModel?.OnThemeSelected(themeIndex);

    public void OnTheme(Theme theme) => GameViewModel?.OnTheme(theme);

    public void OnQuestion(Question question) => GameViewModel?.OnQuestion(question);
}
