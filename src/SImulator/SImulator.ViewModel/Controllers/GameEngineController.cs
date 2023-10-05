using SIEngine;
using SIEngine.Core;
using SIEngine.Rules;
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
    /// Relative image and video weight on screen.
    /// </summary>
    private const double ImageVideoWeight = 3.0;

    public GameViewModel? GameViewModel { get; set; }

    public IPresentationController PresentationController => GameViewModel!.PresentationController;

    private readonly SIDocument _document;

    private bool? _optionsShown = null;

    public GameEngineController(SIDocument document) => _document = document;

    public bool OnAnswerOptions(AnswerOption[] answerOptions)
    {
        var options = new List<ItemViewModel>();

        foreach (var (label, content) in answerOptions)
        {
            switch (content.Type)
            {
                case AtomTypes.Text:
                    options.Add(new ItemViewModel { Label = label, Content = new ContentViewModel(ContentType.Text, content.Value) });
                    break;

                case AtomTypes.Image:
                    var imageUri = TryGetMediaUri(content);

                    if (imageUri != null)
                    {
                        options.Add(new ItemViewModel
                        {
                            Label = label, 
                            Content = new ContentViewModel(ContentType.Image, imageUri, ImageVideoWeight)
                        });
                    }
                    else
                    {
                        options.Add(new ItemViewModel { Label = label, Content = new ContentViewModel(ContentType.Void, "", ImageVideoWeight) });
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

        GameViewModel.ActiveMediaCommand = null;

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

    public void OnButtonPressStart()
    {
        GameViewModel?.AskAnswerButton();
    }

    public void OnQuestionContent(IReadOnlyCollection<ContentItem> content)
    {
        if (GameViewModel == null)
        {
            return;
        }

        var screenContent = new List<ContentViewModel>();

        foreach (var contentItem in content)
        {
            switch (contentItem.Placement)
            {
                case ContentPlacements.Screen:
                    switch (contentItem.Type)
                    {
                        case AtomTypes.Text:
                            // Show theme name and question price instead of empty text
                            var displayedText =
                                GameViewModel.Settings.Model.FalseStart
                                    || GameViewModel.Settings.Model.ShowTextNoFalstart
                                    || GameViewModel.ActiveRound?.Type == RoundTypes.Final
                                ? contentItem.Value
                                : $"{GameViewModel.CurrentTheme}\n{GameViewModel.Price}";

                            PresentationController.SetText(displayedText); // For simple answer
                            screenContent.Add(new ContentViewModel(ContentType.Text, displayedText));
                            break;

                        case AtomTypes.Image:
                            var imageUri = TryGetMediaUri(contentItem);

                            if (imageUri != null)
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Image, imageUri, ImageVideoWeight));
                            }
                            else
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Void, "", ImageVideoWeight));
                            }
                            break;

                        case AtomTypes.Video:
                            var videoUri = TryGetMediaUri(contentItem);

                            if (videoUri != null)
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Video, videoUri, ImageVideoWeight));
                                PresentationController.SetSound();
                                GameViewModel.InitMedia();
                            }
                            else
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Void, "", ImageVideoWeight));
                            }
                            break;

                        case AtomTypes.Html:
                            var htmlUri = TryGetMediaUri(contentItem);

                            if (htmlUri != null)
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Html, htmlUri, ImageVideoWeight));
                                PresentationController.SetQuestionSound(false);
                                PresentationController.SetSound();
                            }
                            else
                            {
                                screenContent.Add(new ContentViewModel(ContentType.Void, "", ImageVideoWeight));
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                case ContentPlacements.Replic:
                    if (contentItem.Type == AtomTypes.Text)
                    {
                        // Show nothing. The text should be read by showman
                    }
                    break;

                case ContentPlacements.Background:
                    if (contentItem.Type == AtomTypes.AudioNew)
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

        if (buttonsRequired)
        {
            GameViewModel.StartButtons(); // Buttons are activated in advance for false starts to work
        }

        _optionsShown = null;
    }

    public void OnAskAnswerStop()
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

        GameViewModel.OnQuestionStart();
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

    public bool ShouldPlayRound(QuestionSelectionStrategyType questionSelectionStrategyType) => true;
}
