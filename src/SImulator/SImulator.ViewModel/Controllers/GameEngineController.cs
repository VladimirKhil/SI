using SIEngine;
using SIEngine.Core;
using SIEngine.Rules;
using SImulator.ViewModel.Contracts;
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

    public GameEngineController(SIDocument document) => _document = document;

    public bool OnAccept() => false;

    public void OnAskAnswer(string mode)
    {
        if (GameViewModel == null)
        {
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

    public bool OnSetAnswerer(string mode, string? select, string? stakeVisibility) => false;

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
                if (availableRange != null
                    && availableRange.Maximum == availableRange.Minimum
                    && availableRange.Minimum > 0)
                {
                    GameViewModel.Price = availableRange.Maximum;
                }
                break;

            case StepParameterValues.SetPriceMode_NoRisk:
                // Do nothing
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
        PresentationController.SetQuestionSound(false);
        PresentationController.SetQuestionContentType(QuestionContentType.Void);
        PresentationController.SetStage(TableStage.Question);
        PresentationController.SetSound();

        if (buttonsRequired)
        {
            GameViewModel.StartButtons(); // Buttons are activated in advance for false starts to work
        }
    }

    public void OnAskAnswerStop()
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.State = Model.QuestionState.Normal;
        PresentationController.SetSound();
    }

    public void OnContentStart(IEnumerable<ContentItem> contentItems)
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.ContentItems = contentItems;
        GameViewModel.ActiveMediaCommand = null;
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

    public bool ShouldPlayRound(QuestionSelectionStrategyType questionSelectionStrategyType) => true;
}
