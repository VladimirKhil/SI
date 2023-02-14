using SIEngine.Core;
using SImulator.ViewModel.Contracts;
using SIPackages;
using SIPackages.Core;
using SIUI.ViewModel;
using SIUI.ViewModel.Core;

namespace SImulator.ViewModel.Controllers;

/// <inheritdoc cref="IQuestionEnginePlayHandler" />
internal sealed class GameEngineController : IQuestionEnginePlayHandler
{
    public GameViewModel? GameViewModel { get; set; }

    public IPresentationController PresentationController => GameViewModel.PresentationController;

    private readonly string _packageFolder;

    public GameEngineController(string packageFolder)
    {
        _packageFolder = packageFolder;
    }

    public void OnAccept()
    {

    }

    public void OnAskAnswer(string mode)
    {
        switch (mode)
        {
            case StepParameterValues.AskAnswerMode_Button:
                PresentationController.SetQuestionStyle(QuestionStyle.WaitingForPress);
                GameViewModel?.AskAnswerButtons();
                break;

            case StepParameterValues.AskAnswerMode_Direct:
                GameViewModel?.AskAnswerDirect();
                break;

            default:
                break;
        }
    }

    public void OnQuestionContentItem(ContentItem contentItem)
    {
        if (GameViewModel == null)
        {
            return;
        }

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

                        PresentationController.SetText(displayedText);
                        PresentationController.SetQuestionContentType(QuestionContentType.Text);
                        break;

                    case AtomTypes.Image:
                        var resultImage = SetMedia(contentItem, SIDocument.ImagesStorageName);

                        if (resultImage)
                        {
                            PresentationController.SetQuestionContentType(QuestionContentType.Image);
                        }
                        else
                        {
                            PresentationController?.SetQuestionContentType(QuestionContentType.None);
                        }
                        break;

                    case AtomTypes.Video:
                        var result = SetMedia(contentItem, SIDocument.VideoStorageName);

                        if (result)
                        {
                            PresentationController.SetQuestionContentType(QuestionContentType.Video);
                            PresentationController.SetSound();
                            GameViewModel.InitMedia();
                        }
                        else
                        {
                            PresentationController?.SetQuestionContentType(QuestionContentType.None);
                        }
                        break;

                    default:
                        PresentationController?.SetQuestionContentType(QuestionContentType.None);
                        break;
                }
                break;

            case ContentPlacements.Replic:
                if (contentItem.Type == AtomTypes.Text)
                {
                    // Show nothing. The text should be read by the showman
                }
                break;

            case ContentPlacements.Background:
                if (contentItem.Type == AtomTypes.AudioNew)
                {
                    PresentationController.SetQuestionSound(true);

                    var result = SetMedia(contentItem, SIDocument.AudioStorageName, true);

                    PresentationController.SetSound();

                    if (result)
                    {
                        GameViewModel.InitMedia();
                    }
                }
                break;

            default:
                break;
        }
    }

    public void OnSetAnswerer(string mode, string? select, string? stakeVisibility)
    {
        // Do nothing
    }

    public void OnSetPrice(string mode, NumberSet? availableRange)
    {
        if (GameViewModel == null)
        {
            return;
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
                break;

            default:
                break;
        }

    }

    public void OnSetTheme(string themeName)
    {
        GameViewModel.CurrentTheme = themeName;
    }

    private bool SetMedia(ContentItem contentItem, string category, bool background = false)
    {
        if (!contentItem.IsRef)
        {
            PresentationController.SetMedia(new MediaSource(contentItem.Value), background);
            return true;
        }

        var localFile = Path.Combine(_packageFolder, category, contentItem.Value);

        if (!File.Exists(localFile))
        {
            return false;
        }

        PresentationController.SetMedia(new MediaSource(localFile), background);
        return true;
    }

    public void OnQuestionStart()
    {
        if (GameViewModel == null)
        {
            return;
        }

        GameViewModel.LocalInfo.TStage = TableStage.Question;
        PresentationController.SetStage(TableStage.Question);
    }

    public void OnSimpleRightAnswerStart()
    {
        if (GameViewModel == null)
        {
            return;
        }

        PresentationController.SetStage(TableStage.Answer);
        PresentationController.SetQuestionStyle(QuestionStyle.Normal);
        PresentationController.SetSound();
        GameViewModel.OnRightAnswer();
    }
}
