using SICore;
using SIData;
using SIEngine.Rules;
using SImulator.ViewModel.Properties;
using SIPackages.Core;

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

    public void OnQuestionSelected(int themeIndex, int questionIndex)
    {
        throw new NotImplementedException();
    }

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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void OnTextSpeed(double speed)
    {
        throw new NotImplementedException();
    }

    public void OnTheme(string themeName, int questionCount, bool animate)
    {
        GameViewModel.ShowmanReplic = themeName;
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

    public void SelectQuestion()
    {
        throw new NotImplementedException();
    }

    public void ShowTablo()
    {

    }

    public void OnStage(GameStage stage, string roundName, QuestionSelectionStrategyType? questionSelectionStrategyType)
    {
        if (stage != GameStage.Round || questionSelectionStrategyType == null)
        {
            return;
        }

        GameViewModel.OnRound(roundName, questionSelectionStrategyType.Value);
    }

    public void StopRound()
    {
        throw new NotImplementedException();
    }

    public void TableLoaded()
    {

    }

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
}
