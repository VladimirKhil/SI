using SICore;
using SICore.Models;
using SIData;
using SIEngine.Rules;
using SImulator.ViewModel.Model;
using SImulator.ViewModel.Properties;
using SIPackages;
using SIPackages.Core;
using SIUI.Model;
using System.Diagnostics;

namespace SImulator.ViewModel.Controllers;

/// <summary>
/// Provides new version of game controller.
/// </summary>
internal sealed class GameController : IPersonController
{
    public GameViewModel GameViewModel { get; set; } = null!;

    private readonly ViewerActions _viewerActions;

    public GameController(ViewerActions actions) => _viewerActions = actions;

    public bool CanSwitchType => false;

    public void AddLog(string message)
    {
        Trace.TraceInformation(message);
    }

    public void OnThemeComments(string comments) => GameViewModel.PresentationController.OnThemeComments(comments);

    public void OnQuestionSelected(int themeIndex, int questionIndex) => GameViewModel.OnQuestionSelected(themeIndex, questionIndex);

    public void OnContent(string placement, List<ContentInfo> content)
    {
        GameViewModel.OnContentStart();
        GameViewModel.PresentationController.OnContentStart();

        switch (placement)
        {
            case ContentPlacements.Screen:
                var contentItems = content.Select(ci => new ContentItem
                {
                    Type = ci.Type,
                    Value = ci.Uri,
                    IsRef = true,
                    Placement = ContentPlacements.Screen
                }).ToList();

                GameViewModel.ContentItems = contentItems;
                GameViewModel.ActiveMediaCommand = null;
                GameViewModel.DecisionMode = DecisionMode.None;

                GameViewModel.PresentationController.OnQuestionContent(contentItems, TryGetMediaUri, "");
                break;
        }
    }

    private string? TryGetMediaUri(ContentItem contentItem) => contentItem.Value;

    public void ClearSelections(bool full = false)
    {
        
    }

    public void DeleteTheme()
    {
        throw new NotImplementedException();
    }

    public void EndThink()
    {
        throw new NotImplementedException();
    }

    public void EndTry(string text)
    {
        
    }

    public void FinalThink()
    {
        throw new NotImplementedException();
    }

    public void OnGameThemes(IEnumerable<string> themes) => GameViewModel.OnGameThemes(themes);

    public void IsRight(string name, bool voteForRight, string answer)
    {
        throw new NotImplementedException();
    }

    public void MakeStake()
    {
        throw new NotImplementedException();
    }

    public void OnPauseChanged(bool isPaused)
    {

    }

    public void OnPersonApellated(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonFinalAnswer(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonFinalStake(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPersonPass(int playerIndex)
    {
        throw new NotImplementedException();
    }

    public void OnPlayerOutcome(int playerIndex, bool isRight)
    {
        throw new NotImplementedException();
    }

    public void OnReplic(string personCode, string text)
    {
        if (personCode == "s")
        {
            GameViewModel.ShowmanReplic = text;
        }
    }

    public void OnPackageAuthors(IEnumerable<string> authors) =>
        GameViewModel.ShowmanReplic = $"{Resources.PackageAuthors}: {string.Join(", ", authors)}";

    public void OnPackage(string packageName, string? logoUri)
    {
        MediaInfo? packageLogo = logoUri != null ? new MediaInfo(logoUri) : null;
        GameViewModel.OnPackage(packageName, packageLogo);
    }

    public void OnPackageComments(string comments) => GameViewModel.ShowmanReplic = $"{Resources.PackageComments}: {comments}";

    public void OnRightAnswer(string answer)
    {
        GameViewModel.State = QuestionState.Normal;
        GameViewModel.OnRightAnswer();
        GameViewModel.PresentationController.OnSimpleRightAnswer(answer);
    }

    public void OnRightAnswerStart(string answer)
    {
        GameViewModel.State = QuestionState.Normal;
        GameViewModel.OnRightAnswer();
        GameViewModel.PresentationController.OnComplexRightAnswer(answer);
    }

    public void RoundThemes(List<string> themes, ThemesPlayMode playMode) 
    {
        if (playMode == ThemesPlayMode.OneByOne)
        {
            GameViewModel.OnRoundThemes(themes);
        }
    }

    public void OnTheme(string themeName, string themeComments, int questionCount, bool animate)
    {
        GameViewModel.ShowmanReplic = "";
        GameViewModel.LocalInfo.Text = themeName;
        GameViewModel.LocalInfo.TStage = SIUI.ViewModel.TableStage.Theme;
        GameViewModel.PresentationController.SetTheme(themeName, animate);
    }

    public void OnTimeChanged()
    {
        throw new NotImplementedException();
    }

    public void OnTimerChanged(int timerIndex, string timerCommand, string arg, string? person = null)
    {
        if (timerIndex == 1 && timerCommand == "MAXTIME" && int.TryParse(arg, out var maxTime))
        {
            GameViewModel.RoundTimeMax = maxTime;
            return;
        }

        switch (timerIndex)
        {
            case 0:
                if (timerCommand == "GO")
                {
                    GameViewModel.RunRoundTimer.Execute(0); return;
                }

                break;

            case 1:
                if (timerCommand == "RESUME")
                {
                    GameViewModel.RunQuestionTimer.Execute(0); return;
                }

                break;

            case 2:
                if (timerCommand == "GO")
                {
                    return;
                }

                break;
        }
    }

    public void Out(int themeIndex)
    {
        throw new NotImplementedException();
    }

    public void PrintGreeting()
    {
        throw new NotImplementedException();
    }

    public void ReceiveText(Message m)
    {
        throw new NotImplementedException();
    }

    public void Report(string report)
    {
        throw new NotImplementedException();
    }

    public void Resume()
    {
        throw new NotImplementedException();
    }

    public void SelectQuestion() => GameViewModel.PresentationController.AskToSelectQuestion();

    public void ShowTablo()
    {
        GameViewModel.PresentationController.SelectionCallback = (themeIndex, questionIndex) =>
        {
            _viewerActions.SelectQuestion(themeIndex, questionIndex);
        };

        GameViewModel.PresentationController.SetRoundTable();
        GameViewModel.LocalInfo.TStage = SIUI.ViewModel.TableStage.RoundTable;
    }

    public void OnStage(bool informOnly, GameStage stage, string stageName, int stageIndex, QuestionSelectionStrategyType? questionSelectionStrategyType)
    {
        if (stage != GameStage.Round || questionSelectionStrategyType == null)
        {
            return;
        }

        GameViewModel.OnRound(stageName, questionSelectionStrategyType.Value);
    }

    public void StopRound()
    {
        throw new NotImplementedException();
    }

    public void TableLoaded(List<ThemeInfo> table) => GameViewModel.LoadTable(table);

    public void TimeOut()
    {
        throw new NotImplementedException();
    }

    public void UpdatePicture(Account account, string path)
    {
        throw new NotImplementedException();
    }

    public void ValidateAnswer(int playerIndex, string answer)
    {
        throw new NotImplementedException();
    }

    public void Try(bool questionNotFinished) => GameViewModel.AskAnswerButton();
}
